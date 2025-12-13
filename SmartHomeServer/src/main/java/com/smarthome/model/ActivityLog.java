package com.smarthome.model;

import org.bson.Document;
import org.bson.types.ObjectId;

/**
 * Modelo para registrar actividad de usuarios
 * Guarda quién hizo qué y cuándo
 */
public class ActivityLog {
    
    private ObjectId id;
    private String username;        // Usuario que realizó la acción
    private String action;          // Tipo de acción: LOGIN, LOGOUT, DEVICE_CONTROL, etc.
    private String deviceId;        // ID del dispositivo (si aplica)
    private String deviceName;      // Nombre del dispositivo (si aplica)
    private String deviceType;      // Tipo de dispositivo (si aplica)
    private String details;         // Detalles adicionales: "Encendido", "Apagado", etc.
    private String ipAddress;       // IP desde donde se realizó
    private long timestamp;         // Momento de la acción
    
    // Tipos de acciones
    public static final String ACTION_LOGIN = "LOGIN";
    public static final String ACTION_LOGOUT = "LOGOUT";
    public static final String ACTION_REGISTER = "REGISTER";
    public static final String ACTION_DEVICE_ON = "DEVICE_ON";
    public static final String ACTION_DEVICE_OFF = "DEVICE_OFF";
    public static final String ACTION_DEVICE_CHANGE = "DEVICE_CHANGE";
    public static final String ACTION_ROUTINE_EXEC = "ROUTINE_EXEC";
    
    public ActivityLog() {
        this.timestamp = System.currentTimeMillis();
    }
    
    public ActivityLog(String username, String action) {
        this();
        this.username = username;
        this.action = action;
    }
    
    public ActivityLog(String username, String action, String deviceId, String deviceName, String details) {
        this();
        this.username = username;
        this.action = action;
        this.deviceId = deviceId;
        this.deviceName = deviceName;
        this.details = details;
    }
    
    // Convertir a Document de MongoDB
    public Document toDocument() {
        Document doc = new Document();
        if (id != null) {
            doc.append("_id", id);
        }
        doc.append("username", username)
           .append("action", action)
           .append("deviceId", deviceId)
           .append("deviceName", deviceName)
           .append("deviceType", deviceType)
           .append("details", details)
           .append("ipAddress", ipAddress)
           .append("timestamp", timestamp);
        return doc;
    }
    
    // Crear desde Document de MongoDB
    public static ActivityLog fromDocument(Document doc) {
        if (doc == null) return null;
        
        ActivityLog log = new ActivityLog();
        log.id = doc.getObjectId("_id");
        log.username = doc.getString("username");
        log.action = doc.getString("action");
        log.deviceId = doc.getString("deviceId");
        log.deviceName = doc.getString("deviceName");
        log.deviceType = doc.getString("deviceType");
        log.details = doc.getString("details");
        log.ipAddress = doc.getString("ipAddress");
        Long ts = doc.getLong("timestamp");
        log.timestamp = ts != null ? ts : 0;
        return log;
    }
    
    // Convertir a JSON para respuestas REST
    public String toJson() {
        return String.format(java.util.Locale.US,
            "{\"id\": \"%s\", \"username\": \"%s\", \"action\": \"%s\", " +
            "\"deviceId\": %s, \"deviceName\": %s, \"deviceType\": %s, " +
            "\"details\": %s, \"ipAddress\": %s, \"timestamp\": %d}",
            id != null ? id.toHexString() : "",
            username != null ? username : "",
            action != null ? action : "",
            deviceId != null ? "\"" + deviceId + "\"" : "null",
            deviceName != null ? "\"" + deviceName + "\"" : "null",
            deviceType != null ? "\"" + deviceType + "\"" : "null",
            details != null ? "\"" + details + "\"" : "null",
            ipAddress != null ? "\"" + ipAddress + "\"" : "null",
            timestamp
        );
    }
    
    // Descripción legible de la acción
    public String getReadableDescription() {
        switch (action) {
            case ACTION_LOGIN:
                return "inició sesión";
            case ACTION_LOGOUT:
                return "cerró sesión";
            case ACTION_REGISTER:
                return "se registró";
            case ACTION_DEVICE_ON:
                return "encendió " + (deviceName != null ? deviceName : "dispositivo");
            case ACTION_DEVICE_OFF:
                return "apagó " + (deviceName != null ? deviceName : "dispositivo");
            case ACTION_DEVICE_CHANGE:
                return "modificó " + (deviceName != null ? deviceName : "dispositivo") + 
                       (details != null ? " (" + details + ")" : "");
            case ACTION_ROUTINE_EXEC:
                return "ejecutó rutina" + (details != null ? " " + details : "");
            default:
                return action + (details != null ? ": " + details : "");
        }
    }
    
    // Getters y Setters
    public ObjectId getId() { return id; }
    public void setId(ObjectId id) { this.id = id; }
    
    public String getUsername() { return username; }
    public void setUsername(String username) { this.username = username; }
    
    public String getAction() { return action; }
    public void setAction(String action) { this.action = action; }
    
    public String getDeviceId() { return deviceId; }
    public void setDeviceId(String deviceId) { this.deviceId = deviceId; }
    
    public String getDeviceName() { return deviceName; }
    public void setDeviceName(String deviceName) { this.deviceName = deviceName; }
    
    public String getDeviceType() { return deviceType; }
    public void setDeviceType(String deviceType) { this.deviceType = deviceType; }
    
    public String getDetails() { return details; }
    public void setDetails(String details) { this.details = details; }
    
    public String getIpAddress() { return ipAddress; }
    public void setIpAddress(String ipAddress) { this.ipAddress = ipAddress; }
    
    public long getTimestamp() { return timestamp; }
    public void setTimestamp(long timestamp) { this.timestamp = timestamp; }
    
    @Override
    public String toString() {
        return String.format("[%s] %s %s", 
            new java.text.SimpleDateFormat("dd/MM HH:mm").format(new java.util.Date(timestamp)),
            username, 
            getReadableDescription());
    }
}
