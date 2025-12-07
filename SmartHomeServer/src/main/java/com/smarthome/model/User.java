package com.smarthome.model;

import org.bson.Document;
import org.bson.types.ObjectId;

/**
 * Modelo de Usuario para el sistema domótico
 */
public class User {
    
    private ObjectId id;
    private String username;
    private String password; // En producción: hash
    private String email;
    private String role; // "admin", "user", "guest"
    private String houseId; // Casa asociada
    private long createdAt;
    
    public User() {
        this.createdAt = System.currentTimeMillis();
        this.role = "user";
    }
    
    public User(String username, String password, String email) {
        this();
        this.username = username;
        this.password = password;
        this.email = email;
    }
    
    // Convertir a Document de MongoDB
    public Document toDocument() {
        Document doc = new Document();
        if (id != null) {
            doc.append("_id", id);
        }
        doc.append("username", username)
           .append("password", password)
           .append("email", email)
           .append("role", role)
           .append("houseId", houseId)
           .append("createdAt", createdAt);
        return doc;
    }
    
    // Crear desde Document de MongoDB
    public static User fromDocument(Document doc) {
        if (doc == null) return null;
        
        User user = new User();
        user.id = doc.getObjectId("_id");
        user.username = doc.getString("username");
        user.password = doc.getString("password");
        user.email = doc.getString("email");
        user.role = doc.getString("role");
        user.houseId = doc.getString("houseId");
        Long created = doc.getLong("createdAt");
        user.createdAt = created != null ? created : 0;
        return user;
    }
    
    // Getters y Setters
    public ObjectId getId() { return id; }
    public void setId(ObjectId id) { this.id = id; }
    
    public String getUsername() { return username; }
    public void setUsername(String username) { this.username = username; }
    
    public String getPassword() { return password; }
    public void setPassword(String password) { this.password = password; }
    
    public String getEmail() { return email; }
    public void setEmail(String email) { this.email = email; }
    
    public String getRole() { return role; }
    public void setRole(String role) { this.role = role; }
    
    public String getHouseId() { return houseId; }
    public void setHouseId(String houseId) { this.houseId = houseId; }
    
    public long getCreatedAt() { return createdAt; }
    
    @Override
    public String toString() {
        return "User{" +
                "id=" + id +
                ", username='" + username + '\'' +
                ", email='" + email + '\'' +
                ", role='" + role + '\'' +
                ", houseId='" + houseId + '\'' +
                '}';
    }
}
