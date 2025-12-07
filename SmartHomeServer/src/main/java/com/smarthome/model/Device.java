package com.smarthome.model;

import org.bson.Document;
import org.bson.types.ObjectId;
import java.util.ArrayList;
import java.util.List;

/**
 * Modelo de Dispositivo del hogar inteligente
 * Tipos: light, thermostat, door, camera, sensor, speaker, etc.
 */
public class Device {
    
    private ObjectId id;
    private String name;           // "Luz Sala", "Termostato Principal"
    private String type;           // light, thermostat, door, camera, sensor
    private String room;           // sala, cocina, habitacion1, garage
    private String houseId;        // Casa a la que pertenece
    private boolean status;        // on/off
    private int value;             // valor (brillo 0-100, temperatura, etc)
    private String color;          // Para luces RGB: "#FF5733"
    private long lastUpdate;
    
    public Device() {
        this.lastUpdate = System.currentTimeMillis();
        this.status = false;
        this.value = 0;
    }
    
    public Device(String name, String type, String room) {
        this();
        this.name = name;
        this.type = type;
        this.room = room;
    }
    
    // Convertir a Document de MongoDB
    public Document toDocument() {
        Document doc = new Document();
        if (id != null) {
            doc.append("_id", id);
        }
        doc.append("name", name)
           .append("type", type)
           .append("room", room)
           .append("houseId", houseId)
           .append("status", status)
           .append("value", value)
           .append("color", color)
           .append("lastUpdate", lastUpdate);
        return doc;
    }
    
    // Crear desde Document de MongoDB
    public static Device fromDocument(Document doc) {
        if (doc == null) return null;
        
        Device device = new Device();
        device.id = doc.getObjectId("_id");
        device.name = doc.getString("name");
        device.type = doc.getString("type");
        device.room = doc.getString("room");
        device.houseId = doc.getString("houseId");
        Boolean st = doc.getBoolean("status");
        device.status = st != null ? st : false;
        Integer val = doc.getInteger("value");
        device.value = val != null ? val : 0;
        device.color = doc.getString("color");
        Long update = doc.getLong("lastUpdate");
        device.lastUpdate = update != null ? update : 0;
        return device;
    }
    
    // Convertir a JSON simple para enviar al cliente
    public String toJson() {
        StringBuilder sb = new StringBuilder();
        sb.append("{");
        sb.append("\"id\":\"").append(id != null ? id.toString() : "").append("\",");
        sb.append("\"name\":\"").append(name != null ? name : "").append("\",");
        sb.append("\"type\":\"").append(type != null ? type : "").append("\",");
        sb.append("\"room\":\"").append(room != null ? room : "").append("\",");
        sb.append("\"status\":").append(status).append(",");
        sb.append("\"value\":").append(value).append(",");
        sb.append("\"color\":\"").append(color != null ? color : "").append("\"");
        sb.append("}");
        return sb.toString();
    }
    
    // Getters y Setters
    public ObjectId getId() { return id; }
    public void setId(ObjectId id) { this.id = id; }
    
    public String getIdString() { 
        return id != null ? id.toString() : null; 
    }
    
    public String getName() { return name; }
    public void setName(String name) { this.name = name; }
    
    public String getType() { return type; }
    public void setType(String type) { this.type = type; }
    
    public String getRoom() { return room; }
    public void setRoom(String room) { this.room = room; }
    
    public String getHouseId() { return houseId; }
    public void setHouseId(String houseId) { this.houseId = houseId; }
    
    public boolean isStatus() { return status; }
    public void setStatus(boolean status) { 
        this.status = status;
        this.lastUpdate = System.currentTimeMillis();
    }
    
    public int getValue() { return value; }
    public void setValue(int value) { 
        this.value = value;
        this.lastUpdate = System.currentTimeMillis();
    }
    
    public String getColor() { return color; }
    public void setColor(String color) { 
        this.color = color;
        this.lastUpdate = System.currentTimeMillis();
    }
    
    public long getLastUpdate() { return lastUpdate; }
    
    @Override
    public String toString() {
        return "Device{" +
                "id=" + id +
                ", name='" + name + '\'' +
                ", type='" + type + '\'' +
                ", room='" + room + '\'' +
                ", status=" + status +
                ", value=" + value +
                '}';
    }
}
