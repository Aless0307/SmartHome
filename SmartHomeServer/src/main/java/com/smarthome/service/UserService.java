package com.smarthome.service;

import com.mongodb.client.MongoCollection;
import com.mongodb.client.MongoCursor;
import com.mongodb.client.result.DeleteResult;
import com.smarthome.database.MongoDBConnection;
import com.smarthome.model.User;
import org.bson.Document;
import org.bson.types.ObjectId;

import java.util.ArrayList;
import java.util.List;

import static com.mongodb.client.model.Filters.*;

/**
 * Servicio para operaciones CRUD de usuarios
 */
public class UserService {
    
    private static final String COLLECTION_NAME = "usuarios";
    private MongoCollection<Document> collection;
    
    public UserService() {
        this.collection = MongoDBConnection.getInstance()
                .getCollection(COLLECTION_NAME);
    }
    
    /**
     * Crear un nuevo usuario
     */
    public User create(User user) {
        // Verificar si el username ya existe
        if (findByUsername(user.getUsername()) != null) {
            System.err.println("❌ Usuario ya existe: " + user.getUsername());
            return null;
        }
        
        Document doc = user.toDocument();
        collection.insertOne(doc);
        user.setId(doc.getObjectId("_id"));
        System.out.println("✅ Usuario creado: " + user.getUsername());
        return user;
    }
    
    /**
     * Buscar por ID
     */
    public User findById(String id) {
        try {
            Document doc = collection.find(eq("_id", new ObjectId(id))).first();
            return User.fromDocument(doc);
        } catch (Exception e) {
            return null;
        }
    }
    
    /**
     * Buscar por username
     */
    public User findByUsername(String username) {
        Document doc = collection.find(eq("username", username)).first();
        return User.fromDocument(doc);
    }
    
    /**
     * Buscar por email
     */
    public User findByEmail(String email) {
        Document doc = collection.find(eq("email", email)).first();
        return User.fromDocument(doc);
    }
    
    /**
     * Login - verificar credenciales
     */
    public User login(String username, String password) {
        Document doc = collection.find(
            and(eq("username", username), eq("password", password))
        ).first();
        
        if (doc != null) {
            System.out.println("✅ Login exitoso: " + username);
            return User.fromDocument(doc);
        }
        System.out.println("❌ Login fallido: " + username);
        return null;
    }
    
    /**
     * Obtener todos los usuarios
     */
    public List<User> findAll() {
        List<User> users = new ArrayList<>();
        try (MongoCursor<Document> cursor = collection.find().iterator()) {
            while (cursor.hasNext()) {
                users.add(User.fromDocument(cursor.next()));
            }
        }
        return users;
    }
    
    /**
     * Actualizar usuario
     */
    public boolean update(User user) {
        try {
            collection.replaceOne(eq("_id", user.getId()), user.toDocument());
            return true;
        } catch (Exception e) {
            return false;
        }
    }
    
    /**
     * Asignar casa a usuario
     */
    public boolean assignHouse(String userId, String houseId) {
        try {
            collection.updateOne(
                eq("_id", new ObjectId(userId)),
                new Document("$set", new Document("houseId", houseId))
            );
            return true;
        } catch (Exception e) {
            return false;
        }
    }
    
    /**
     * Eliminar usuario
     */
    public boolean delete(String id) {
        try {
            DeleteResult result = collection.deleteOne(eq("_id", new ObjectId(id)));
            return result.getDeletedCount() > 0;
        } catch (Exception e) {
            return false;
        }
    }
    
    /**
     * Contar usuarios
     */
    public long count() {
        return collection.countDocuments();
    }
}
