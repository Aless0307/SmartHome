package com.smarthome.service;

import com.mongodb.client.MongoCollection;
import com.mongodb.client.MongoCursor;
import com.mongodb.client.result.DeleteResult;
import com.mongodb.client.result.UpdateResult;
import com.smarthome.database.MongoDBConnection;
import com.smarthome.model.Device;
import org.bson.Document;
import org.bson.types.ObjectId;

import java.util.ArrayList;
import java.util.List;

import static com.mongodb.client.model.Filters.*;

/**
 * Servicio para operaciones CRUD de dispositivos
 */
public class DeviceService {
    
    private static final String COLLECTION_NAME = "dispositivos";
    private MongoCollection<Document> collection;
    
    public DeviceService() {
        this.collection = MongoDBConnection.getInstance()
                .getCollection(COLLECTION_NAME);
    }
    
    /**
     * Crear un nuevo dispositivo
     */
    public Device create(Device device) {
        Document doc = device.toDocument();
        collection.insertOne(doc);
        device.setId(doc.getObjectId("_id"));
        System.out.println("[OK] Dispositivo creado: " + device.getName());
        return device;
    }
    
    /**
     * Obtener dispositivo por ID
     */
    public Device findById(String id) {
        try {
            Document doc = collection.find(eq("_id", new ObjectId(id))).first();
            return Device.fromDocument(doc);
        } catch (Exception e) {
            System.err.println("Error al buscar dispositivo: " + e.getMessage());
            return null;
        }
    }
    
    /**
     * Obtener todos los dispositivos
     */
    public List<Device> findAll() {
        List<Device> devices = new ArrayList<>();
        try (MongoCursor<Document> cursor = collection.find().iterator()) {
            while (cursor.hasNext()) {
                devices.add(Device.fromDocument(cursor.next()));
            }
        }
        return devices;
    }
    
    /**
     * Obtener dispositivos por casa
     */
    public List<Device> findByHouseId(String houseId) {
        List<Device> devices = new ArrayList<>();
        try (MongoCursor<Document> cursor = collection.find(eq("houseId", houseId)).iterator()) {
            while (cursor.hasNext()) {
                devices.add(Device.fromDocument(cursor.next()));
            }
        }
        return devices;
    }
    
    /**
     * Obtener dispositivos por habitación
     */
    public List<Device> findByRoom(String room) {
        List<Device> devices = new ArrayList<>();
        try (MongoCursor<Document> cursor = collection.find(eq("room", room)).iterator()) {
            while (cursor.hasNext()) {
                devices.add(Device.fromDocument(cursor.next()));
            }
        }
        return devices;
    }
    
    /**
     * Obtener dispositivos por tipo
     */
    public List<Device> findByType(String type) {
        List<Device> devices = new ArrayList<>();
        try (MongoCursor<Document> cursor = collection.find(eq("type", type)).iterator()) {
            while (cursor.hasNext()) {
                devices.add(Device.fromDocument(cursor.next()));
            }
        }
        return devices;
    }
    
    /**
     * Actualizar dispositivo completo
     */
    public boolean update(Device device) {
        try {
            UpdateResult result = collection.replaceOne(
                eq("_id", device.getId()),
                device.toDocument()
            );
            return result.getModifiedCount() > 0;
        } catch (Exception e) {
            System.err.println("Error al actualizar: " + e.getMessage());
            return false;
        }
    }
    
    /**
     * Actualizar estado de un dispositivo (on/off)
     */
    public boolean updateStatus(String deviceId, boolean status) {
        try {
            UpdateResult result = collection.updateOne(
                eq("_id", new ObjectId(deviceId)),
                new Document("$set", new Document("status", status)
                    .append("lastUpdate", System.currentTimeMillis()))
            );
            System.out.println("Dispositivo " + deviceId + " -> status: " + status);
            return result.getModifiedCount() > 0;
        } catch (Exception e) {
            System.err.println("Error al actualizar status: " + e.getMessage());
            return false;
        }
    }
    
    /**
     * Actualizar valor de un dispositivo (brillo, temperatura, etc)
     */
    public boolean updateValue(String deviceId, int value) {
        try {
            UpdateResult result = collection.updateOne(
                eq("_id", new ObjectId(deviceId)),
                new Document("$set", new Document("value", value)
                    .append("lastUpdate", System.currentTimeMillis()))
            );
            return result.getModifiedCount() > 0;
        } catch (Exception e) {
            System.err.println("Error al actualizar valor: " + e.getMessage());
            return false;
        }
    }
    
    /**
     * Actualizar color de una luz
     */
    public boolean updateColor(String deviceId, String color) {
        try {
            UpdateResult result = collection.updateOne(
                eq("_id", new ObjectId(deviceId)),
                new Document("$set", new Document("color", color)
                    .append("lastUpdate", System.currentTimeMillis()))
            );
            // Usar matchedCount para comandos repetidos (CMD:PLAY, CMD:PLAY)
            return result.getMatchedCount() > 0;
        } catch (Exception e) {
            System.err.println("Error al actualizar color: " + e.getMessage());
            return false;
        }
    }
    
    /**
     * Actualizar lista de tracks de un speaker
     */
    public boolean updateTracks(String deviceId, java.util.List<String> tracks) {
        try {
            UpdateResult result = collection.updateOne(
                eq("_id", new ObjectId(deviceId)),
                new Document("$set", new Document("tracks", tracks)
                    .append("lastUpdate", System.currentTimeMillis()))
            );
            System.out.println("Speaker " + deviceId + " -> tracks: " + tracks);
            return result.getModifiedCount() > 0;
        } catch (Exception e) {
            System.err.println("Error al actualizar tracks: " + e.getMessage());
            return false;
        }
    }
    
    /**
     * Eliminar dispositivo
     */
    public boolean delete(String id) {
        try {
            DeleteResult result = collection.deleteOne(eq("_id", new ObjectId(id)));
            return result.getDeletedCount() > 0;
        } catch (Exception e) {
            System.err.println("Error al eliminar: " + e.getMessage());
            return false;
        }
    }
    
    /**
     * Eliminar todos los dispositivos de una casa
     */
    public long deleteByHouseId(String houseId) {
        DeleteResult result = collection.deleteMany(eq("houseId", houseId));
        return result.getDeletedCount();
    }
    
    /**
     * Contar dispositivos
     */
    public long count() {
        return collection.countDocuments();
    }
    
    /**
     * Crear dispositivos de prueba para una casa
     */
    public void createTestDevices(String houseId) {
        // Luces
        Device luz1 = new Device("Luz Principal", "light", "sala");
        luz1.setHouseId(houseId);
        luz1.setColor("#FFFFFF");
        create(luz1);
        
        Device luz2 = new Device("Luz Cocina", "light", "cocina");
        luz2.setHouseId(houseId);
        luz2.setColor("#FFE4B5");
        create(luz2);
        
        Device luz3 = new Device("Luz Habitación", "light", "habitacion1");
        luz3.setHouseId(houseId);
        luz3.setColor("#E6E6FA");
        create(luz3);
        
        // Termostato
        Device termo = new Device("Termostato Central", "thermostat", "sala");
        termo.setHouseId(houseId);
        termo.setValue(22); // 22°C
        create(termo);
        
        // Puertas
        Device puerta = new Device("Puerta Principal", "door", "sala");
        puerta.setHouseId(houseId);
        create(puerta);
        
        Device garage = new Device("Puerta Garage", "door", "garage");
        garage.setHouseId(houseId);
        create(garage);
        
        // Cámara
        Device camara = new Device("Cámara Entrada", "camera", "jardin");
        camara.setHouseId(houseId);
        create(camara);
        
        // Sensor
        Device sensor = new Device("Sensor Movimiento", "sensor", "sala");
        sensor.setHouseId(houseId);
        create(sensor);
        
        System.out.println("[OK] Dispositivos de prueba creados: " + count());
    }
    
    // Main para probar
    public static void main(String[] args) {
        System.out.println("=== Test DeviceService ===\n");
        
        DeviceService service = new DeviceService();
        
        // Limpiar dispositivos anteriores (para pruebas)
        System.out.println("Dispositivos actuales: " + service.count());
        
        // Crear dispositivo de prueba
        Device testDevice = new Device("Luz Test", "light", "sala");
        testDevice.setHouseId("test-house-123");
        testDevice.setColor("#FF0000");
        
        Device created = service.create(testDevice);
        System.out.println("Creado: " + created);
        
        // Buscar por ID
        Device found = service.findById(created.getIdString());
        System.out.println("Encontrado: " + found);
        
        // Actualizar status
        service.updateStatus(created.getIdString(), true);
        found = service.findById(created.getIdString());
        System.out.println("Después de encender: status=" + found.isStatus());
        
        // Actualizar valor
        service.updateValue(created.getIdString(), 75);
        found = service.findById(created.getIdString());
        System.out.println("Después de valor: value=" + found.getValue());
        
        // Listar todos
        System.out.println("\nTodos los dispositivos:");
        for (Device d : service.findAll()) {
            System.out.println("  - " + d);
        }
        
        // Eliminar
        boolean deleted = service.delete(created.getIdString());
        System.out.println("\nEliminado: " + deleted);
        System.out.println("Total después de eliminar: " + service.count());
        
        // Cerrar conexión
        MongoDBConnection.getInstance().close();
    }
}
