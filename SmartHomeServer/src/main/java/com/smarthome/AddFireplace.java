package com.smarthome;

import com.smarthome.database.MongoDBConnection;
import com.smarthome.model.Device;
import com.smarthome.service.DeviceService;
import com.smarthome.service.HouseService;
import com.smarthome.model.House;

/**
 * Script para agregar la Chimenea
 */
public class AddFireplace {
    
    public static void main(String[] args) {
        System.out.println("╔════════════════════════════════════════════╗");
        System.out.println("║      AGREGAR CHIMENEA - SMART HOME         ║");
        System.out.println("╚════════════════════════════════════════════╝\n");
        
        try {
            // Conectar a MongoDB
            MongoDBConnection.getInstance();
            
            // Servicios
            HouseService houseService = new HouseService();
            DeviceService deviceService = new DeviceService();
            
            // Obtener casa
            House myHouse = houseService.findAll().get(0);
            String houseId = myHouse.getIdString();
            
            // Crear Chimenea
            System.out.println("🔥 Creando Chimenea...\n");
            
            Device fireplace = new Device("Chimenea", "fireplace", "sala");
            fireplace.setHouseId(houseId);
            fireplace.setStatus(false);  // Apagada
            fireplace.setValue(0);       // No consume energía (decorativo)
            fireplace.setColor("#FF4500"); // Color fuego (naranja-rojo)
            deviceService.create(fireplace);
            System.out.println("   [OK] Chimenea [sala] - Decorativa, sin consumo");
            
            // Mostrar dispositivos
            System.out.println("\n[DEV] DISPOSITIVOS TOTALES: " + deviceService.count());
            for (Device d : deviceService.findAll()) {
                String status = d.isStatus() ? "🟢 ON" : "🔴 OFF";
                System.out.println("   - " + d.getName() + " [" + d.getType() + "] " + 
                                   d.getRoom() + " " + status);
            }
            
            System.out.println("\n🔥 ¡Chimenea agregada correctamente!");
            
        } catch (Exception e) {
            System.err.println("[ERROR] Error: " + e.getMessage());
            e.printStackTrace();
        } finally {
            MongoDBConnection.getInstance().close();
        }
    }
}
