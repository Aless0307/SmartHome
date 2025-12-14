package com.smarthome.service;

import com.mongodb.client.MongoCollection;
import com.mongodb.client.MongoCursor;
import com.mongodb.client.model.Sorts;
import com.smarthome.database.MongoDBConnection;
import com.smarthome.model.ActivityLog;
import org.bson.Document;

import java.util.ArrayList;
import java.util.List;

import static com.mongodb.client.model.Filters.*;

/**
 * Servicio para registrar y consultar actividad de usuarios
 */
public class ActivityService {
    
    private static final String COLLECTION_NAME = "activity_logs";
    private MongoCollection<Document> collection;
    
    // Singleton
    private static ActivityService instance;
    
    public static ActivityService getInstance() {
        if (instance == null) {
            instance = new ActivityService();
        }
        return instance;
    }
    
    public ActivityService() {
        this.collection = MongoDBConnection.getInstance()
                .getCollection(COLLECTION_NAME);
    }
    
    /**
     * Registrar una actividad
     */
    public void log(ActivityLog activity) {
        try {
            collection.insertOne(activity.toDocument());
            System.out.println("[ACTIVITY] " + activity.toString());
        } catch (Exception e) {
            System.err.println("[ACTIVITY] Error guardando: " + e.getMessage());
        }
    }
    
    /**
     * Registrar login
     */
    public void logLogin(String username, String ipAddress) {
        ActivityLog log = new ActivityLog(username, ActivityLog.ACTION_LOGIN);
        log.setIpAddress(ipAddress);
        log(log);
    }
    
    /**
     * Registrar logout
     */
    public void logLogout(String username) {
        ActivityLog log = new ActivityLog(username, ActivityLog.ACTION_LOGOUT);
        log(log);
    }
    
    /**
     * Registrar registro de nuevo usuario
     */
    public void logRegister(String username, String ipAddress) {
        ActivityLog log = new ActivityLog(username, ActivityLog.ACTION_REGISTER);
        log.setIpAddress(ipAddress);
        log(log);
    }
    
    /**
     * Registrar control de dispositivo
     */
    public void logDeviceControl(String username, String deviceId, String deviceName, 
                                  String deviceType, boolean turnedOn, String details) {
        String action = turnedOn ? ActivityLog.ACTION_DEVICE_ON : ActivityLog.ACTION_DEVICE_OFF;
        ActivityLog log = new ActivityLog(username, action, deviceId, deviceName, details);
        log.setDeviceType(deviceType);
        log(log);
    }
    
    /**
     * Registrar cambio en dispositivo (brillo, color, etc.)
     */
    public void logDeviceChange(String username, String deviceId, String deviceName, 
                                 String deviceType, String details) {
        ActivityLog log = new ActivityLog(username, ActivityLog.ACTION_DEVICE_CHANGE, 
                                          deviceId, deviceName, details);
        log.setDeviceType(deviceType);
        log(log);
    }
    
    /**
     * Registrar ejecución de rutina
     */
    public void logRoutineExec(String username, String routineName) {
        ActivityLog log = new ActivityLog(username, ActivityLog.ACTION_ROUTINE_EXEC);
        log.setDetails(routineName);
        log(log);
    }
    
    /**
     * Obtener últimas N actividades
     */
    public List<ActivityLog> getRecent(int limit) {
        List<ActivityLog> logs = new ArrayList<>();
        try (MongoCursor<Document> cursor = collection.find()
                .sort(Sorts.descending("timestamp"))
                .limit(limit)
                .iterator()) {
            while (cursor.hasNext()) {
                logs.add(ActivityLog.fromDocument(cursor.next()));
            }
        }
        return logs;
    }
    
    /**
     * Obtener actividades de un usuario
     */
    public List<ActivityLog> getByUser(String username, int limit) {
        List<ActivityLog> logs = new ArrayList<>();
        try (MongoCursor<Document> cursor = collection.find(eq("username", username))
                .sort(Sorts.descending("timestamp"))
                .limit(limit)
                .iterator()) {
            while (cursor.hasNext()) {
                logs.add(ActivityLog.fromDocument(cursor.next()));
            }
        }
        return logs;
    }
    
    /**
     * Obtener actividades por tipo de acción
     */
    public List<ActivityLog> getByAction(String action, int limit) {
        List<ActivityLog> logs = new ArrayList<>();
        try (MongoCursor<Document> cursor = collection.find(eq("action", action))
                .sort(Sorts.descending("timestamp"))
                .limit(limit)
                .iterator()) {
            while (cursor.hasNext()) {
                logs.add(ActivityLog.fromDocument(cursor.next()));
            }
        }
        return logs;
    }
    
    /**
     * Obtener actividades de un dispositivo
     */
    public List<ActivityLog> getByDevice(String deviceId, int limit) {
        List<ActivityLog> logs = new ArrayList<>();
        try (MongoCursor<Document> cursor = collection.find(eq("deviceId", deviceId))
                .sort(Sorts.descending("timestamp"))
                .limit(limit)
                .iterator()) {
            while (cursor.hasNext()) {
                logs.add(ActivityLog.fromDocument(cursor.next()));
            }
        }
        return logs;
    }
    
    /**
     * Obtener actividades en un rango de tiempo
     */
    public List<ActivityLog> getByTimeRange(long desde, long hasta, int limit) {
        List<ActivityLog> logs = new ArrayList<>();
        try (MongoCursor<Document> cursor = collection.find(
                and(gte("timestamp", desde), lte("timestamp", hasta)))
                .sort(Sorts.descending("timestamp"))
                .limit(limit)
                .iterator()) {
            while (cursor.hasNext()) {
                logs.add(ActivityLog.fromDocument(cursor.next()));
            }
        }
        return logs;
    }
    
    /**
     * Contar actividades
     */
    public long count() {
        return collection.countDocuments();
    }
    
    /**
     * Limpiar actividades antiguas (más de X días)
     */
    public long cleanOld(int dias) {
        long limite = System.currentTimeMillis() - ((long) dias * 24 * 60 * 60 * 1000);
        return collection.deleteMany(lt("timestamp", limite)).getDeletedCount();
    }
}
