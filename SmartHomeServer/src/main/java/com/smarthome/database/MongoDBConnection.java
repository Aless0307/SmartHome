package com.smarthome.database;

import com.mongodb.client.MongoClient;
import com.mongodb.client.MongoClients;
import com.mongodb.client.MongoDatabase;
import com.mongodb.client.MongoCollection;
import org.bson.Document;

/**
 * Clase singleton para manejar la conexión a MongoDB Atlas
 */
public class MongoDBConnection {
    
    private static MongoDBConnection instance;
    private MongoClient mongoClient;
    private MongoDatabase database;
    
    // URI de conexión a MongoDB Atlas
    private static final String CONNECTION_URI = "mongodb+srv://alessandroah77:Aless180307$@cluster1.zszwi.mongodb.net/?retryWrites=true&w=majority";
    private static final String DATABASE_NAME = "PR";
    
    private MongoDBConnection() {
        try {
            System.out.println("Conectando a MongoDB Atlas...");
            mongoClient = MongoClients.create(CONNECTION_URI);
            database = mongoClient.getDatabase(DATABASE_NAME);
            System.out.println("[OK] Conexión a MongoDB establecida - Base de datos: " + DATABASE_NAME);
        } catch (Exception e) {
            System.err.println("[ERROR] Error al conectar a MongoDB: " + e.getMessage());
            throw new RuntimeException("No se pudo conectar a MongoDB", e);
        }
    }
    
    /**
     * Obtener la instancia única de la conexión
     */
    public static synchronized MongoDBConnection getInstance() {
        if (instance == null) {
            instance = new MongoDBConnection();
        }
        return instance;
    }
    
    /**
     * Obtener la base de datos
     */
    public MongoDatabase getDatabase() {
        return database;
    }
    
    /**
     * Obtener una colección específica
     */
    public MongoCollection<Document> getCollection(String collectionName) {
        return database.getCollection(collectionName);
    }
    
    /**
     * Cerrar la conexión
     */
    public void close() {
        if (mongoClient != null) {
            mongoClient.close();
            System.out.println("Conexión a MongoDB cerrada");
        }
    }
    
    /**
     * Verificar la conexión haciendo ping
     */
    public boolean testConnection() {
        try {
            database.runCommand(new Document("ping", 1));
            System.out.println("[OK] Ping a MongoDB exitoso");
            return true;
        } catch (Exception e) {
            System.err.println("[ERROR] Error en ping: " + e.getMessage());
            return false;
        }
    }
    
    // Main para probar la conexión
    public static void main(String[] args) {
        System.out.println("=== Test de Conexión MongoDB ===\n");
        
        try {
            MongoDBConnection conn = MongoDBConnection.getInstance();
            
            if (conn.testConnection()) {
                System.out.println("\n[OK] ¡Conexión exitosa a MongoDB Atlas!");
                
                // Listar colecciones existentes
                System.out.println("\nColecciones en la base de datos '" + DATABASE_NAME + "':");
                for (String name : conn.getDatabase().listCollectionNames()) {
                    System.out.println("  - " + name);
                }
            }
            
            conn.close();
            
        } catch (Exception e) {
            System.err.println("[ERROR] Error: " + e.getMessage());
            e.printStackTrace();
        }
    }
}
