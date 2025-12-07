package com.smarthome.model;

import org.bson.Document;
import org.bson.types.ObjectId;
import java.util.ArrayList;
import java.util.List;

/**
 * Modelo de Casa inteligente
 * Representa una casa con sus habitaciones y dispositivos
 */
public class House {
    
    private ObjectId id;
    private String name;           // "Casa Principal"
    private String address;        // Dirección
    private String ownerId;        // Usuario propietario
    private List<String> rooms;    // Lista de habitaciones
    private long createdAt;
    
    public House() {
        this.createdAt = System.currentTimeMillis();
        this.rooms = new ArrayList<>();
    }
    
    public House(String name, String address) {
        this();
        this.name = name;
        this.address = address;
    }
    
    // Habitaciones por defecto para una casa
    public void addDefaultRooms() {
        rooms.add("sala");
        rooms.add("cocina");
        rooms.add("habitacion1");
        rooms.add("habitacion2");
        rooms.add("baño");
        rooms.add("garage");
        rooms.add("jardin");
    }
    
    // Convertir a Document de MongoDB
    public Document toDocument() {
        Document doc = new Document();
        if (id != null) {
            doc.append("_id", id);
        }
        doc.append("name", name)
           .append("address", address)
           .append("ownerId", ownerId)
           .append("rooms", rooms)
           .append("createdAt", createdAt);
        return doc;
    }
    
    // Crear desde Document de MongoDB
    @SuppressWarnings("unchecked")
    public static House fromDocument(Document doc) {
        if (doc == null) return null;
        
        House house = new House();
        house.id = doc.getObjectId("_id");
        house.name = doc.getString("name");
        house.address = doc.getString("address");
        house.ownerId = doc.getString("ownerId");
        List<String> roomList = (List<String>) doc.get("rooms");
        house.rooms = roomList != null ? roomList : new ArrayList<>();
        Long created = doc.getLong("createdAt");
        house.createdAt = created != null ? created : 0;
        return house;
    }
    
    // Getters y Setters
    public ObjectId getId() { return id; }
    public void setId(ObjectId id) { this.id = id; }
    
    public String getIdString() { 
        return id != null ? id.toString() : null; 
    }
    
    public String getName() { return name; }
    public void setName(String name) { this.name = name; }
    
    public String getAddress() { return address; }
    public void setAddress(String address) { this.address = address; }
    
    public String getOwnerId() { return ownerId; }
    public void setOwnerId(String ownerId) { this.ownerId = ownerId; }
    
    public List<String> getRooms() { return rooms; }
    public void setRooms(List<String> rooms) { this.rooms = rooms; }
    
    public void addRoom(String room) { 
        if (!rooms.contains(room)) {
            rooms.add(room); 
        }
    }
    
    public long getCreatedAt() { return createdAt; }
    
    @Override
    public String toString() {
        return "House{" +
                "id=" + id +
                ", name='" + name + '\'' +
                ", address='" + address + '\'' +
                ", rooms=" + rooms +
                '}';
    }
}
