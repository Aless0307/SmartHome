package com.smarthome.service;

import com.mongodb.client.MongoCollection;
import com.mongodb.client.MongoCursor;
import com.mongodb.client.result.DeleteResult;
import com.smarthome.database.MongoDBConnection;
import com.smarthome.model.User;
import com.smarthome.security.InputValidator;
import com.smarthome.security.PasswordUtils;
import com.smarthome.security.RateLimiter;
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
     * Crear un nuevo usuario con password hasheado
     */
    public User create(User user) {
        // Validar entrada
        String validationError = InputValidator.validateUserInput(
            user.getUsername(), 
            user.getPassword(), 
            user.getEmail()
        );
        if (validationError != null) {
            System.err.println("[ERROR] Validacion fallida: " + validationError);
            return null;
        }
        
        // Sanitizar username y email
        user.setUsername(InputValidator.sanitize(user.getUsername()));
        if (user.getEmail() != null) {
            user.setEmail(InputValidator.sanitize(user.getEmail()));
        }
        
        // Verificar si el username ya existe
        if (findByUsername(user.getUsername()) != null) {
            System.err.println("[ERROR] Usuario ya existe: " + user.getUsername());
            return null;
        }
        
        // Hashear password antes de guardar
        String hashedPassword = PasswordUtils.createHash(user.getPassword());
        user.setPassword(hashedPassword);
        
        Document doc = user.toDocument();
        collection.insertOne(doc);
        user.setId(doc.getObjectId("_id"));
        System.out.println("[OK] Usuario creado: " + user.getUsername());
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
     * Login - verificar credenciales con rate limiting y password hashing
     */
    public User login(String username, String password) {
        return login(username, password, null);
    }
    
    /**
     * Login con IP para rate limiting
     */
    public User login(String username, String password, String clientIp) {
        // Rate limiting por IP o username
        String rateLimitKey = clientIp != null ? clientIp : username;
        RateLimiter limiter = RateLimiter.getInstance();
        
        if (limiter.isBlocked(rateLimitKey)) {
            long remaining = limiter.getBlockTimeRemaining(rateLimitKey);
            System.err.println("[SECURITY] Usuario bloqueado: " + rateLimitKey + 
                             " (esperar " + remaining + "s)");
            return null;
        }
        
        // Sanitizar entrada
        username = InputValidator.sanitize(username);
        
        // Buscar usuario por username
        Document doc = collection.find(eq("username", username)).first();
        
        if (doc != null) {
            String storedPassword = doc.getString("password");
            
            // Verificar password (soporta hash y texto plano para migracion)
            if (PasswordUtils.verifyPassword(password, storedPassword)) {
                System.out.println("[OK] Login exitoso: " + username);
                limiter.clearAttempts(rateLimitKey);
                
                // Migrar password antiguo a hash si es necesario
                if (!PasswordUtils.isHashed(storedPassword)) {
                    migratePasswordToHash(doc.getObjectId("_id"), password);
                }
                
                return User.fromDocument(doc);
            }
        }
        
        // Login fallido - registrar intento
        limiter.recordFailedAttempt(rateLimitKey);
        int remaining = limiter.getRemainingAttempts(rateLimitKey);
        System.out.println("[ERROR] Login fallido: " + username + 
                          " (intentos restantes: " + remaining + ")");
        return null;
    }
    
    /**
     * Migra un password antiguo (texto plano) a hash
     */
    private void migratePasswordToHash(ObjectId userId, String plainPassword) {
        try {
            String hashedPassword = PasswordUtils.createHash(plainPassword);
            collection.updateOne(
                eq("_id", userId),
                new Document("$set", new Document("password", hashedPassword))
            );
            System.out.println("[OK] Password migrado a hash para usuario: " + userId);
        } catch (Exception e) {
            System.err.println("[ERROR] Error al migrar password: " + e.getMessage());
        }
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
