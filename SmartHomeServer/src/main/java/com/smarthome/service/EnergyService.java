package com.smarthome.service;

import com.mongodb.client.MongoCollection;
import com.mongodb.client.MongoCursor;
import com.mongodb.client.model.Sorts;
import com.smarthome.database.MongoDBConnection;
import com.smarthome.model.Device;
import com.smarthome.model.EnergyLog;
import org.bson.Document;

import java.util.*;
import java.util.concurrent.*;

import static com.mongodb.client.model.Filters.*;

/**
 * Servicio para gestionar el consumo electrico
 * Usa muestreo periodico para registrar consumo de dispositivos encendidos
 */
public class EnergyService {
    
    private static final String COLLECTION_NAME = "energy_logs";
    private static final int INTERVALO_MUESTREO_SEGUNDOS = 5; // Cada 5 segundos
    
    private MongoCollection<Document> collection;
    private DeviceService deviceService;
    private ScheduledExecutorService scheduler;
    
    // Cache de watts actuales por dispositivo (para mostrar en tiempo real)
    private Map<String, Double> deviceCurrentWatts = new ConcurrentHashMap<>();
    
    // Singleton
    private static EnergyService instance;
    
    public static EnergyService getInstance() {
        if (instance == null) {
            instance = new EnergyService();
        }
        return instance;
    }
    
    public EnergyService() {
        this.collection = MongoDBConnection.getInstance()
                .getCollection(COLLECTION_NAME);
        this.deviceService = new DeviceService();
    }
    
    /**
     * Iniciar el muestreo periodico de consumo
     * Llamar esto cuando inicie el servidor
     */
    public void startSampling() {
        if (scheduler != null && !scheduler.isShutdown()) {
            return; // Ya esta corriendo
        }
        
        scheduler = Executors.newSingleThreadScheduledExecutor();
        scheduler.scheduleAtFixedRate(
            this::sampleAllDevices, 
            10, // Delay inicial de 10 segundos
            INTERVALO_MUESTREO_SEGUNDOS, 
            TimeUnit.SECONDS
        );
        
        System.out.println("[ENERGY] Muestreo iniciado - cada " + INTERVALO_MUESTREO_SEGUNDOS + " segundos");
    }
    
    /**
     * Detener el muestreo
     */
    public void stopSampling() {
        if (scheduler != null) {
            scheduler.shutdown();
            System.out.println("[ENERGY] Muestreo detenido");
        }
    }
    
    /**
     * Muestrear todos los dispositivos y registrar consumo de los encendidos
     */
    private void sampleAllDevices() {
        try {
            List<Device> devices = deviceService.findAll();
            int encendidos = 0;
            double totalWatts = 0;
            StringBuilder nombresActivos = new StringBuilder();
            
            deviceCurrentWatts.clear();
            
            for (Device device : devices) {
                String tipo = device.getType();
                
                // Ignorar porton (solo consume por accion)
                if ("door".equals(tipo)) {
                    continue;
                }
                
                // Verificar si esta encendido segun el tipo
                boolean estaEncendido = isDeviceConsuming(device);
                
                if (estaEncendido) {
                    double watts = EnergyLog.calculateWatts(tipo, "ON", device.getValue());
                    
                    // Registrar consumo por el intervalo de muestreo
                    EnergyLog log = new EnergyLog(
                        device.getIdString(), 
                        device.getName(), 
                        tipo, 
                        "SAMPLE"
                    );
                    log.setWattsConsumed(watts);
                    log.setDuration(INTERVALO_MUESTREO_SEGUNDOS * 1000L); // en milisegundos
                    
                    // Usar houseId del dispositivo, o "default" si es null
                    String houseId = device.getHouseId();
                    if (houseId == null || houseId.isEmpty()) {
                        houseId = "default";
                    }
                    log.setHouseId(houseId);
                    
                    collection.insertOne(log.toDocument());
                    
                    // Guardar en cache para mostrar en tiempo real
                    deviceCurrentWatts.put(device.getName(), watts);
                    
                    // Agregar nombre a la lista
                    if (encendidos > 0) nombresActivos.append(", ");
                    nombresActivos.append(device.getName()).append("(").append(String.format("%.0f", watts)).append("W)");
                    
                    encendidos++;
                    totalWatts += watts;
                }
            }
            
            if (encendidos > 0) {
                System.out.printf("[ENERGY] Muestreo: %d dispositivos, %.1fW total -> %s%n", 
                    encendidos, totalWatts, nombresActivos.toString());
            }
            
        } catch (Exception e) {
            System.err.println("[ENERGY] Error en muestreo: " + e.getMessage());
        }
    }
    
    /**
     * Determinar si un dispositivo esta consumiendo energia
     * Logica especifica por tipo
     */
    private boolean isDeviceConsuming(Device device) {
        String tipo = device.getType();
        boolean status = device.isStatus();
        String color = device.getColor(); // Para speaker, contiene el comando
        
        switch (tipo) {
            case "light":
                // Luz encendida = status true
                return status;
                
            case "tv":
                // TV mostrada = status TRUE = consume energia
                return status;
                
            case "speaker":
                // Speaker solo consume si esta reproduciendo (no pausado ni detenido)
                // Si color contiene PAUSE o STOP, no esta consumiendo
                if (color != null && (color.contains("PAUSE") || color.contains("STOP"))) {
                    return false;
                }
                // Solo consume si status true Y esta reproduciendo
                return status && color != null && color.contains("PLAY");
                
            case "ac":
                // Clima encendido = status true
                return status;
                
            case "washer":
                // Lavadora encendida = status true
                return status;
                
            case "camera":
                // Camaras solo consumen si estan activas (status true)
                return status;
                
            default:
                return status;
        }
    }
    
    /**
     * Registrar accion puntual del porton (abre/cierra)
     * El porton consume 200W por 5 segundos cada vez que se acciona
     */
    public void logDoorAction(Device device, String action) {
        EnergyLog log = new EnergyLog(
            device.getIdString(), 
            device.getName(), 
            device.getType(), 
            "ACTION"
        );
        // Porton: 200W por 5 segundos = 0.000278 kWh
        log.setWattsConsumed(EnergyLog.WATTS_DOOR_ACTION);
        log.setDuration(5000); // 5 segundos en ms
        log.setHouseId(device.getHouseId());
        
        collection.insertOne(log.toDocument());
        System.out.printf("[ENERGY] Porton %s: 200W x 5s = %.4f kWh%n", 
            action, log.getKwhConsumed());
    }
    
    /**
     * Obtener consumo total en kWh para un periodo (sin filtrar por casa)
     */
    public double getTotalConsumption(String houseId, long desde, long hasta) {
        double totalKwh = 0;
        
        // No filtramos por houseId para simplificar
        try (MongoCursor<Document> cursor = collection.find(
                and(
                    gte("timestamp", desde),
                    lte("timestamp", hasta)
                )
            ).iterator()) {
            
            while (cursor.hasNext()) {
                EnergyLog log = EnergyLog.fromDocument(cursor.next());
                totalKwh += log.getKwhConsumed();
            }
        }
        
        return totalKwh;
    }
    
    /**
     * Obtener consumo por dispositivo
     */
    public Map<String, Double> getConsumptionByDevice(String houseId, long desde, long hasta) {
        Map<String, Double> consumoByDevice = new HashMap<>();
        
        try (MongoCursor<Document> cursor = collection.find(
                and(
                    gte("timestamp", desde),
                    lte("timestamp", hasta)
                )
            ).iterator()) {
            
            while (cursor.hasNext()) {
                EnergyLog log = EnergyLog.fromDocument(cursor.next());
                String key = log.getDeviceName();
                double current = consumoByDevice.getOrDefault(key, 0.0);
                consumoByDevice.put(key, current + log.getKwhConsumed());
            }
        }
        
        return consumoByDevice;
    }
    
    /**
     * Obtener consumo por tipo de dispositivo
     */
    public Map<String, Double> getConsumptionByType(String houseId, long desde, long hasta) {
        Map<String, Double> consumoByType = new HashMap<>();
        
        try (MongoCursor<Document> cursor = collection.find(
                and(
                    gte("timestamp", desde),
                    lte("timestamp", hasta)
                )
            ).iterator()) {
            
            while (cursor.hasNext()) {
                EnergyLog log = EnergyLog.fromDocument(cursor.next());
                String key = log.getDeviceType();
                double current = consumoByType.getOrDefault(key, 0.0);
                consumoByType.put(key, current + log.getKwhConsumed());
            }
        }
        
        return consumoByType;
    }
    
    /**
     * Obtener consumo por hora del dia (ultimas 24 horas)
     */
    public Map<Integer, Double> getConsumptionByHour(String houseId) {
        Map<Integer, Double> consumoByHour = new HashMap<>();
        for (int i = 0; i < 24; i++) {
            consumoByHour.put(i, 0.0);
        }
        
        long ahora = System.currentTimeMillis();
        long hace24h = ahora - (24 * 60 * 60 * 1000);
        
        try (MongoCursor<Document> cursor = collection.find(
                gte("timestamp", hace24h)
            ).iterator()) {
            
            Calendar cal = Calendar.getInstance();
            while (cursor.hasNext()) {
                EnergyLog log = EnergyLog.fromDocument(cursor.next());
                cal.setTimeInMillis(log.getTimestamp());
                int hora = cal.get(Calendar.HOUR_OF_DAY);
                double current = consumoByHour.get(hora);
                consumoByHour.put(hora, current + log.getKwhConsumed());
            }
        }
        
        return consumoByHour;
    }
    
    /**
     * Obtener consumo diario
     */
    public Map<String, Double> getConsumptionByDay(String houseId, int dias) {
        Map<String, Double> consumoByDay = new LinkedHashMap<>();
        
        long ahora = System.currentTimeMillis();
        long hace = ahora - ((long) dias * 24 * 60 * 60 * 1000);
        
        try (MongoCursor<Document> cursor = collection.find(
                gte("timestamp", hace)
            ).sort(Sorts.ascending("timestamp")).iterator()) {
            
            Calendar cal = Calendar.getInstance();
            java.text.SimpleDateFormat sdf = new java.text.SimpleDateFormat("dd/MM");
            
            while (cursor.hasNext()) {
                EnergyLog log = EnergyLog.fromDocument(cursor.next());
                cal.setTimeInMillis(log.getTimestamp());
                String dia = sdf.format(cal.getTime());
                double current = consumoByDay.getOrDefault(dia, 0.0);
                consumoByDay.put(dia, current + log.getKwhConsumed());
            }
        }
        
        return consumoByDay;
    }
    
    /**
     * Obtener los ultimos N eventos de energia
     */
    public List<EnergyLog> getRecentLogs(String houseId, int limit) {
        List<EnergyLog> logs = new ArrayList<>();
        
        try (MongoCursor<Document> cursor = collection.find()
                .sort(Sorts.descending("timestamp"))
                .limit(limit)
                .iterator()) {
            
            while (cursor.hasNext()) {
                logs.add(EnergyLog.fromDocument(cursor.next()));
            }
        }
        
        return logs;
    }
    
    /**
     * Obtener consumo actual estimado (dispositivos encendidos ahora)
     */
    public double getCurrentPowerUsage() {
        double totalWatts = 0;
        for (Double watts : deviceCurrentWatts.values()) {
            totalWatts += watts;
        }
        return totalWatts;
    }
    
    /**
     * Obtener lista de dispositivos actualmente consumiendo
     */
    public Map<String, Double> getActiveDevicesConsumption() {
        return new HashMap<>(deviceCurrentWatts);
    }
    
    /**
     * Calcular costo estimado
     */
    public double calculateCost(double kWh, double precioPorKwh) {
        return kWh * precioPorKwh;
    }
    
    /**
     * Limpiar logs antiguos
     */
    public long cleanOldLogs(int diasAntiguedad) {
        long limite = System.currentTimeMillis() - ((long) diasAntiguedad * 24 * 60 * 60 * 1000);
        return collection.deleteMany(lt("timestamp", limite)).getDeletedCount();
    }
    
    /**
     * Contar total de logs
     */
    public long count() {
        return collection.countDocuments();
    }
}
