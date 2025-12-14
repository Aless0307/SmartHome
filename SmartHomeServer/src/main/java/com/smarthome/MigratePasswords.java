package com.smarthome;

import com.mongodb.client.MongoCollection;
import com.mongodb.client.MongoCursor;
import com.smarthome.database.MongoDBConnection;
import com.smarthome.security.PasswordUtils;
import org.bson.Document;
import org.bson.types.ObjectId;

/**
 * Script para migrar contraseñas existentes a formato hasheado
 * Ejecutar una sola vez despues de actualizar el codigo
 */
public class MigratePasswords {
    
    public static void main(String[] args) {
        System.out.println("=== Migracion de contraseñas a hash ===\n");
        
        MongoCollection<Document> users = MongoDBConnection.getInstance()
            .getCollection("usuarios");
        
        int total = 0;
        int migrated = 0;
        int alreadyHashed = 0;
        
        try (MongoCursor<Document> cursor = users.find().iterator()) {
            while (cursor.hasNext()) {
                Document doc = cursor.next();
                total++;
                
                String username = doc.getString("username");
                String password = doc.getString("password");
                ObjectId id = doc.getObjectId("_id");
                
                if (password == null) {
                    System.out.println("[SKIP] " + username + " - sin password");
                    continue;
                }
                
                if (PasswordUtils.isHashed(password)) {
                    System.out.println("[OK] " + username + " - ya tiene hash");
                    alreadyHashed++;
                    continue;
                }
                
                // Migrar a hash
                String hashedPassword = PasswordUtils.createHash(password);
                users.updateOne(
                    new Document("_id", id),
                    new Document("$set", new Document("password", hashedPassword))
                );
                
                System.out.println("[MIGRADO] " + username + " - password hasheado");
                migrated++;
            }
        }
        
        System.out.println("\n=== Resumen ===");
        System.out.println("Total usuarios: " + total);
        System.out.println("Ya hasheados: " + alreadyHashed);
        System.out.println("Migrados: " + migrated);
        System.out.println("\nMigracion completada!");
    }
}
