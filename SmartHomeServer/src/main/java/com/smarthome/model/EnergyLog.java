package com.smarthome.model;

import org.bson.Document;
import org.bson.types.ObjectId;

/**
 * Modelo para registrar eventos de consumo electrico
 * Guarda cuando un dispositivo se enciende/apaga o realiza una accion
 */
public class EnergyLog {
    
    private ObjectId id;
    private String deviceId;       // ID del dispositivo
    private String deviceName;     // Nombre para mostrar
    private String deviceType;     // Tipo de dispositivo
    private String eventType;      // "ON", "OFF", "ACTION"
    private double wattsConsumed;  // Watts consumidos en este evento
    private long timestamp;        // Cuando ocurrio
    private long duration;         // Duracion en milisegundos (para eventos continuos)
    private String houseId;        // Casa
    
    // Watts estimados por tipo de dispositivo
    public static final int WATTS_LIGHT = 15;           // LED promedio
    public static final int WATTS_LIGHT_MAX = 60;       // Luz a maximo brillo
    public static final int WATTS_TV = 120;             // TV cuando esta mostrada
    public static final int WATTS_SPEAKER = 25;         // Speaker reproduciendo
    public static final int WATTS_CAMERA = 15;          // Camara activa
    public static final int WATTS_AC = 1500;            // Clima encendido
    public static final int WATTS_DOOR_ACTION = 200;    // Motor porton por accion (5 segundos aprox)
    public static final int WATTS_WASHER = 500;         // Lavadora en ciclo
    
    public EnergyLog() {
        this.timestamp = System.currentTimeMillis();
    }
    
    public EnergyLog(String deviceId, String deviceName, String deviceType, String eventType) {
        this();
        this.deviceId = deviceId;
        this.deviceName = deviceName;
        this.deviceType = deviceType;
        this.eventType = eventType;
        this.wattsConsumed = calculateWatts(deviceType, eventType, 0);
    }
    
    /**
     * Calcula los watts consumidos segun el tipo de dispositivo y evento
     * @param deviceType Tipo de dispositivo
     * @param eventType Tipo de evento (ON, OFF, ACTION)
     * @param brightnessOrValue Valor de brillo o intensidad (0-6000 para luces)
     */
    public static double calculateWatts(String deviceType, String eventType, int brightnessOrValue) {
        // Si es OFF, no consume
        if ("OFF".equals(eventType)) {
            return 0;
        }
        
        switch (deviceType) {
            case "light":
                // Consumo proporcional al brillo (0-6000 -> 5-60W)
                double brigtnessFactor = brightnessOrValue / 6000.0;
                return WATTS_LIGHT + (WATTS_LIGHT_MAX - WATTS_LIGHT) * brigtnessFactor;
                
            case "tv":
                // TV consume solo cuando esta mostrada (status=false significa mostrada)
                return WATTS_TV;
                
            case "speaker":
                // Speaker consume cuando reproduce
                return WATTS_SPEAKER;
                
            case "camera":
                return WATTS_CAMERA;
                
            case "ac":
                return WATTS_AC;
                
            case "door":
                // Porton consume por accion (motor), no continuo
                if ("ACTION".equals(eventType)) {
                    // Consumo de 5 segundos de motor a 200W = 0.000278 kWh
                    return WATTS_DOOR_ACTION;
                }
                return 0;
                
            case "washer":
                return WATTS_WASHER;
                
            default:
                return 10; // Consumo minimo por defecto
        }
    }
    
    /**
     * Calcula kWh consumidos dado watts y duracion en milisegundos
     */
    public double getKwhConsumed() {
        if (duration <= 0) {
            // Para acciones instantaneas (porton), asumimos 5 segundos
            if ("ACTION".equals(eventType)) {
                return (wattsConsumed * 5.0) / 3600000.0; // 5 segundos en kWh
            }
            return 0;
        }
        // kWh = (Watts * horas)
        double hours = duration / 3600000.0;
        return wattsConsumed * hours;
    }
    
    // Convertir a Document de MongoDB
    public Document toDocument() {
        Document doc = new Document();
        if (id != null) {
            doc.append("_id", id);
        }
        doc.append("deviceId", deviceId)
           .append("deviceName", deviceName)
           .append("deviceType", deviceType)
           .append("eventType", eventType)
           .append("wattsConsumed", wattsConsumed)
           .append("timestamp", timestamp)
           .append("duration", duration)
           .append("houseId", houseId);
        return doc;
    }
    
    // Crear desde Document de MongoDB
    public static EnergyLog fromDocument(Document doc) {
        if (doc == null) return null;
        
        EnergyLog log = new EnergyLog();
        log.id = doc.getObjectId("_id");
        log.deviceId = doc.getString("deviceId");
        log.deviceName = doc.getString("deviceName");
        log.deviceType = doc.getString("deviceType");
        log.eventType = doc.getString("eventType");
        Double watts = doc.getDouble("wattsConsumed");
        log.wattsConsumed = watts != null ? watts : 0;
        Long ts = doc.getLong("timestamp");
        log.timestamp = ts != null ? ts : 0;
        Long dur = doc.getLong("duration");
        log.duration = dur != null ? dur : 0;
        log.houseId = doc.getString("houseId");
        return log;
    }
    
    // Getters y Setters
    public ObjectId getId() { return id; }
    public void setId(ObjectId id) { this.id = id; }
    
    public String getDeviceId() { return deviceId; }
    public void setDeviceId(String deviceId) { this.deviceId = deviceId; }
    
    public String getDeviceName() { return deviceName; }
    public void setDeviceName(String deviceName) { this.deviceName = deviceName; }
    
    public String getDeviceType() { return deviceType; }
    public void setDeviceType(String deviceType) { this.deviceType = deviceType; }
    
    public String getEventType() { return eventType; }
    public void setEventType(String eventType) { this.eventType = eventType; }
    
    public double getWattsConsumed() { return wattsConsumed; }
    public void setWattsConsumed(double wattsConsumed) { this.wattsConsumed = wattsConsumed; }
    
    public long getTimestamp() { return timestamp; }
    public void setTimestamp(long timestamp) { this.timestamp = timestamp; }
    
    public long getDuration() { return duration; }
    public void setDuration(long duration) { this.duration = duration; }
    
    public String getHouseId() { return houseId; }
    public void setHouseId(String houseId) { this.houseId = houseId; }
    
    /**
     * Convertir a JSON string para respuestas REST
     */
    public String toJson() {
        return String.format(java.util.Locale.US,
            "{\"deviceId\": \"%s\", \"deviceName\": \"%s\", \"deviceType\": \"%s\", " +
            "\"eventType\": \"%s\", \"wattsConsumed\": %.1f, \"timestamp\": %d, " +
            "\"duration\": %d, \"kwhConsumed\": %.6f}",
            deviceId, deviceName, deviceType, eventType, 
            wattsConsumed, timestamp, duration, getKwhConsumed()
        );
    }
    
    @Override
    public String toString() {
        return String.format("EnergyLog[%s %s: %.1fW @ %d]", 
            deviceName, eventType, wattsConsumed, timestamp);
    }
}
