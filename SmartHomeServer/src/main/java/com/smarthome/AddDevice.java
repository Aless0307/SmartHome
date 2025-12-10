package com.smarthome;

import com.smarthome.database.MongoDBConnection;
import com.smarthome.model.Device;
import com.smarthome.service.DeviceService;
import com.smarthome.service.HouseService;
import com.smarthome.model.House;

/**
 * Script para agregar dispositivos adicionales
 */
public class AddDevice {
    
    public static void main(String[] args) {
        System.out.println("╔════════════════════════════════════════════╗");
        System.out.println("║      AGREGAR ECHO DOT - SMART HOME         ║");
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
            
            // Crear Echo Dot (bocina inteligente)
            System.out.println("🔊 Creando Echo Dot...\n");
            
            Device echoDot = new Device("Echo Dot", "speaker", "sala");
            echoDot.setHouseId(houseId);
            echoDot.setStatus(false); // Apagado (no reproduciendo)
            echoDot.setValue(80);     // Volumen 80%
            deviceService.create(echoDot);
            System.out.println("   [OK] Echo Dot [sala] - Volumen: 80%");
            
            // Mostrar dispositivos
            System.out.println("\n[DEV] DISPOSITIVOS TOTALES: " + deviceService.count());
            for (Device d : deviceService.findAll()) {
                String status = d.isStatus() ? "🟢 ON" : "🔴 OFF";
                String extra = "";
                if ("speaker".equals(d.getType())) {
                    extra = " | Vol: " + d.getValue() + "%";
                }
                System.out.println("   - " + d.getName() + " [" + d.getType() + "] " + 
                                   d.getRoom() + " " + status + extra);
            }
            
            System.out.println("\n[OK] ¡Echo Dot agregado correctamente!");
            
        } catch (Exception e) {
            System.err.println("[ERROR] Error: " + e.getMessage());
            e.printStackTrace();
        } finally {
            MongoDBConnection.getInstance().close();
        }
    }
}
