package com.smarthome;

import com.smarthome.database.MongoDBConnection;
import com.smarthome.model.Device;
import com.smarthome.service.DeviceService;
import com.smarthome.service.HouseService;
import com.smarthome.model.House;

/**
 * Script para agregar cámaras de seguridad
 */
public class AddCameras {
    
    public static void main(String[] args) {
        System.out.println("╔════════════════════════════════════════════╗");
        System.out.println("║   AGREGAR CÁMARAS DE SEGURIDAD - SMART HOME ║");
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
            
            System.out.println("📹 Creando cámaras de seguridad...\n");
            
            // Cámara 1 - Entrada
            Device cam1 = new Device("Cámara Entrada", "camera", "entrada");
            cam1.setHouseId(houseId);
            cam1.setStatus(true);  // Encendida
            cam1.setValue(0);      // 0 = luz IR apagada, 100 = luz IR encendida
            cam1.setColor("");     // Usado para comandos especiales
            deviceService.create(cam1);
            System.out.println("   ✅ Cámara Entrada [entrada] - 📹 ON | 💡 IR OFF");
            
            // Cámara 2 - Jardín
            Device cam2 = new Device("Cámara Jardín", "camera", "jardin");
            cam2.setHouseId(houseId);
            cam2.setStatus(true);  // Encendida
            cam2.setValue(0);      // Luz IR apagada
            cam2.setColor("");
            deviceService.create(cam2);
            System.out.println("   ✅ Cámara Jardín [jardin] - 📹 ON | 💡 IR OFF");
            
            // Cámara 3 - Garage
            Device cam3 = new Device("Cámara Garage", "camera", "garage");
            cam3.setHouseId(houseId);
            cam3.setStatus(true);  // Encendida
            cam3.setValue(0);      // Luz IR apagada
            cam3.setColor("");
            deviceService.create(cam3);
            System.out.println("   ✅ Cámara Garage [garage] - 📹 ON | 💡 IR OFF");
            
            // Mostrar dispositivos
            System.out.println("\n📱 DISPOSITIVOS TOTALES: " + deviceService.count());
            System.out.println("\n📹 CÁMARAS:");
            for (Device d : deviceService.findAll()) {
                if ("camera".equals(d.getType())) {
                    String status = d.isStatus() ? "📹 ON" : "📹 OFF";
                    String light = d.getValue() > 0 ? "💡 IR ON" : "💡 IR OFF";
                    System.out.println("   - " + d.getName() + " [" + d.getRoom() + "] " + status + " | " + light);
                }
            }
            
            System.out.println("\n✅ ¡Cámaras agregadas correctamente!");
            System.out.println("\n📌 Notas:");
            System.out.println("   - status: true = cámara encendida, false = apagada");
            System.out.println("   - value: 0 = luz IR apagada, 100 = luz IR encendida");
            System.out.println("   - color: usado para comandos especiales (CMD:RECORD_START, etc)");
            
        } catch (Exception e) {
            System.err.println("❌ Error: " + e.getMessage());
            e.printStackTrace();
        } finally {
            MongoDBConnection.getInstance().close();
        }
    }
}
