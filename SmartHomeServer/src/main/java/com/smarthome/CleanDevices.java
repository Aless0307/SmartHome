package com.smarthome;

import com.smarthome.database.MongoDBConnection;
import com.smarthome.model.Device;
import com.smarthome.service.DeviceService;
import java.util.List;
import java.util.Arrays;
import java.util.HashSet;
import java.util.Set;

/**
 * Script para limpiar dispositivos duplicados
 */
public class CleanDevices {
    
    public static void main(String[] args) {
        System.out.println("╔════════════════════════════════════════════╗");
        System.out.println("║      LIMPIAR DISPOSITIVOS - SMART HOME     ║");
        System.out.println("╚════════════════════════════════════════════╝\n");
        
        // Dispositivos que queremos MANTENER (sin duplicados)
        List<String> keepDevices = Arrays.asList(
            "Puerta Garage",
            "TV Sala", 
            "Lavadora",
            "Clima Cuarto 2",
            "Clima Cuarto",
            "Clima Recámara Alta",
            "Clima Cocina",
            "Clima Sala",
            "Luz Sala 1",
            "Luz Sala 2",
            "Luz Cocina",
            "Luz Recámara Alta",
            "Luz Cuarto 1",
            "Luz Abajo",
            "Luz Cuarto Atrás"
        );
        
        try {
            // Conectar a MongoDB
            MongoDBConnection.getInstance();
            
            DeviceService deviceService = new DeviceService();
            
            System.out.println("📱 DISPOSITIVOS ANTES: " + deviceService.count());
            
            // Eliminar duplicados - mantener solo la primera ocurrencia de cada nombre
            Set<String> seen = new HashSet<>();
            
            for (Device d : deviceService.findAll()) {
                if (!keepDevices.contains(d.getName()) || seen.contains(d.getName())) {
                    System.out.println("   ❌ Eliminando: " + d.getName() + " (duplicado o no deseado)");
                    deviceService.delete(d.getIdString());
                } else {
                    System.out.println("   ✅ Manteniendo: " + d.getName());
                    seen.add(d.getName());
                }
            }
            
            // Mostrar resultado
            System.out.println("\n📱 DISPOSITIVOS DESPUÉS: " + deviceService.count());
            for (Device d : deviceService.findAll()) {
                String status = d.isStatus() ? "🟢 ON" : "🔴 OFF";
                System.out.println("   - " + d.getName() + " [" + d.getType() + "] " + 
                                   d.getRoom() + " " + status);
            }
            
            System.out.println("\n✅ ¡Limpieza completada!");
            
        } catch (Exception e) {
            System.err.println("❌ Error: " + e.getMessage());
            e.printStackTrace();
        } finally {
            MongoDBConnection.getInstance().close();
        }
    }
}
