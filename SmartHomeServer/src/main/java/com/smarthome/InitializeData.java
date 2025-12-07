package com.smarthome;

import com.smarthome.database.MongoDBConnection;
import com.smarthome.model.*;
import com.smarthome.service.*;

/**
 * Script para inicializar datos de prueba en MongoDB
 * Crea: 1 casa, 1 usuario admin, dispositivos de ejemplo
 */
public class InitializeData {
    
    public static void main(String[] args) {
        System.out.println("╔════════════════════════════════════════════╗");
        System.out.println("║   INICIALIZACIÓN DE DATOS - SMART HOME     ║");
        System.out.println("╚════════════════════════════════════════════╝\n");
        
        try {
            // Conectar a MongoDB
            MongoDBConnection.getInstance();
            
            // Servicios
            HouseService houseService = new HouseService();
            UserService userService = new UserService();
            DeviceService deviceService = new DeviceService();
            
            // ==================== CREAR CASA ====================
            System.out.println("\n📍 Creando casa...");
            
            // Verificar si ya existe
            if (houseService.count() > 0) {
                System.out.println("⚠️  Ya existe una casa. Usando la existente.");
            } else {
                House house = new House("Casa Smart", "Calle Principal #123");
                house.addDefaultRooms();
                houseService.create(house);
            }
            
            House myHouse = houseService.findAll().get(0);
            System.out.println("Casa: " + myHouse);
            
            // ==================== CREAR USUARIO ADMIN ====================
            System.out.println("\n👤 Creando usuario admin...");
            
            User admin = userService.findByUsername("admin");
            if (admin != null) {
                System.out.println("⚠️  Usuario admin ya existe.");
            } else {
                admin = new User("admin", "admin123", "admin@smarthome.com");
                admin.setRole("admin");
                admin.setHouseId(myHouse.getIdString());
                userService.create(admin);
            }
            System.out.println("Admin: " + userService.findByUsername("admin"));
            
            // ==================== CREAR DISPOSITIVOS ====================
            System.out.println("\n💡 Creando dispositivos...");
            
            if (deviceService.count() > 0) {
                System.out.println("⚠️  Ya existen " + deviceService.count() + " dispositivos.");
                System.out.println("¿Deseas eliminarlos y crear nuevos? (Los datos actuales se perderán)");
            } else {
                deviceService.createTestDevices(myHouse.getIdString());
            }
            
            // ==================== MOSTRAR RESUMEN ====================
            System.out.println("\n╔════════════════════════════════════════════╗");
            System.out.println("║              RESUMEN DE DATOS              ║");
            System.out.println("╚════════════════════════════════════════════╝");
            
            System.out.println("\n🏠 CASA: " + myHouse.getName());
            System.out.println("   Habitaciones: " + myHouse.getRooms());
            
            System.out.println("\n👥 USUARIOS: " + userService.count());
            for (User u : userService.findAll()) {
                System.out.println("   - " + u.getUsername() + " (" + u.getRole() + ")");
            }
            
            System.out.println("\n📱 DISPOSITIVOS: " + deviceService.count());
            for (Device d : deviceService.findAll()) {
                String status = d.isStatus() ? "🟢 ON" : "🔴 OFF";
                System.out.println("   - " + d.getName() + " [" + d.getType() + "] " + 
                                   d.getRoom() + " " + status);
            }
            
            System.out.println("\n✅ ¡Datos inicializados correctamente!");
            System.out.println("\nCredenciales de prueba:");
            System.out.println("   Usuario: admin");
            System.out.println("   Password: admin123");
            
        } catch (Exception e) {
            System.err.println("❌ Error: " + e.getMessage());
            e.printStackTrace();
        } finally {
            MongoDBConnection.getInstance().close();
        }
    }
}
