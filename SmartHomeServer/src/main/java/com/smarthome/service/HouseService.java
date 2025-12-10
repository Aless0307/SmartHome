package com.smarthome.service;

import com.mongodb.client.MongoCollection;
import com.mongodb.client.MongoCursor;
import com.mongodb.client.result.DeleteResult;
import com.smarthome.database.MongoDBConnection;
import com.smarthome.model.House;
import org.bson.Document;
import org.bson.types.ObjectId;

import java.util.ArrayList;
import java.util.List;

import static com.mongodb.client.model.Filters.*;

/**
 * Servicio para operaciones CRUD de casas
 */
public class HouseService {
    
    private static final String COLLECTION_NAME = "casas";
    private MongoCollection<Document> collection;
    
    public HouseService() {
        this.collection = MongoDBConnection.getInstance()
                .getCollection(COLLECTION_NAME);
    }
    
    /**
     * Crear una nueva casa
     */
    public House create(House house) {
        Document doc = house.toDocument();
        collection.insertOne(doc);
        house.setId(doc.getObjectId("_id"));
        System.out.println("[OK] Casa creada: " + house.getName());
        return house;
    }
    
    /**
     * Buscar por ID
     */
    public House findById(String id) {
        try {
            Document doc = collection.find(eq("_id", new ObjectId(id))).first();
            return House.fromDocument(doc);
        } catch (Exception e) {
            return null;
        }
    }
    
    /**
     * Buscar por propietario
     */
    public House findByOwnerId(String ownerId) {
        Document doc = collection.find(eq("ownerId", ownerId)).first();
        return House.fromDocument(doc);
    }
    
    /**
     * Obtener todas las casas
     */
    public List<House> findAll() {
        List<House> houses = new ArrayList<>();
        try (MongoCursor<Document> cursor = collection.find().iterator()) {
            while (cursor.hasNext()) {
                houses.add(House.fromDocument(cursor.next()));
            }
        }
        return houses;
    }
    
    /**
     * Actualizar casa
     */
    public boolean update(House house) {
        try {
            collection.replaceOne(eq("_id", house.getId()), house.toDocument());
            return true;
        } catch (Exception e) {
            return false;
        }
    }
    
    /**
     * Agregar habitación
     */
    public boolean addRoom(String houseId, String room) {
        try {
            collection.updateOne(
                eq("_id", new ObjectId(houseId)),
                new Document("$addToSet", new Document("rooms", room))
            );
            return true;
        } catch (Exception e) {
            return false;
        }
    }
    
    /**
     * Eliminar casa
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
     * Contar casas
     */
    public long count() {
        return collection.countDocuments();
    }
}
