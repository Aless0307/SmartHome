package com.smarthome.server;

import java.io.*;
import java.net.*;
import java.util.*;
import java.util.concurrent.*;
import com.smarthome.protocol.JsonMessage;

/**
 * ═══════════════════════════════════════════════════════════════
 * SERVIDOR UDP - Smart Home
 * Actividad 1.4: Servidor UDP para notificaciones broadcast
 * 
 * Este servidor:
 * - Escucha en puerto UDP 5001
 * - Registra clientes que quieren recibir notificaciones
 * - Envía broadcast a todos los clientes registrados
 * 
 * Uso: Los clientes se registran enviando {"action": "REGISTER"}
 *      y luego reciben notificaciones de cambios en dispositivos
 * ═══════════════════════════════════════════════════════════════
 */
public class UdpServer implements Runnable {
    
    // Puerto UDP
    private static final int PORT = 5001;
    
    // Tamaño del buffer para recibir datos
    private static final int BUFFER_SIZE = 1024;
    
    private DatagramSocket socket;
    private volatile boolean running = true;
    
    // Lista de clientes registrados para recibir notificaciones
    // Key: "IP:Puerto", Value: InetSocketAddress
    private Map<String, InetSocketAddress> registeredClients;
    
    // Instancia singleton para acceso desde TcpServer
    private static UdpServer instance;
    
    public UdpServer() {
        this.registeredClients = new ConcurrentHashMap<>();
        instance = this;
    }
    
    /**
     * Obtiene la instancia del servidor UDP
     */
    public static UdpServer getInstance() {
        return instance;
    }
    
    @Override
    public void run() {
        try {
            socket = new DatagramSocket(PORT);
            System.out.println("  📢 UDP Server escuchando en puerto: " + PORT);
            
            byte[] buffer = new byte[BUFFER_SIZE];
            
            while (running) {
                try {
                    // Preparar paquete para recibir datos
                    DatagramPacket packet = new DatagramPacket(buffer, buffer.length);
                    
                    // Esperar datos (bloqueante)
                    socket.receive(packet);
                    
                    // Procesar el mensaje recibido
                    String message = new String(packet.getData(), 0, packet.getLength());
                    InetAddress clientAddress = packet.getAddress();
                    int clientPort = packet.getPort();
                    
                    System.out.println("📨 [UDP] De " + clientAddress.getHostAddress() + ":" + clientPort + " -> " + message);
                    
                    // Procesar el mensaje
                    processMessage(message, clientAddress, clientPort);
                    
                } catch (IOException e) {
                    if (running) {
                        System.err.println("[ERROR] [UDP] Error recibiendo: " + e.getMessage());
                    }
                }
            }
            
        } catch (SocketException e) {
            System.err.println("[ERROR] [UDP] Error iniciando servidor: " + e.getMessage());
        }
    }
    
    /**
     * Procesa un mensaje UDP recibido
     */
    private void processMessage(String message, InetAddress address, int port) {
        try {
            JsonMessage request = JsonMessage.parse(message);
            String action = request.getString("action");
            
            if (action == null) {
                sendTo(address, port, JsonMessage.error("Falta 'action'"));
                return;
            }
            
            String clientKey = address.getHostAddress() + ":" + port;
            
            switch (action.toUpperCase()) {
                
                case "REGISTER":
                    // Registrar cliente para recibir notificaciones
                    registeredClients.put(clientKey, new InetSocketAddress(address, port));
                    System.out.println("[OK] [UDP] Cliente registrado: " + clientKey + " (Total: " + registeredClients.size() + ")");
                    
                    sendTo(address, port, new JsonMessage()
                        .put("status", "OK")
                        .put("action", "REGISTERED")
                        .put("message", "Registrado para notificaciones"));
                    break;
                
                case "UNREGISTER":
                    // Desregistrar cliente
                    registeredClients.remove(clientKey);
                    System.out.println("[CONN] [UDP] Cliente desregistrado: " + clientKey);
                    
                    sendTo(address, port, new JsonMessage()
                        .put("status", "OK")
                        .put("action", "UNREGISTERED")
                        .put("message", "Desregistrado de notificaciones"));
                    break;
                
                case "PING":
                    // Responder ping
                    sendTo(address, port, new JsonMessage()
                        .put("status", "OK")
                        .put("action", "PONG")
                        .put("timestamp", System.currentTimeMillis()));
                    break;
                
                case "LIST_CLIENTS":
                    // Listar clientes registrados (para debug)
                    sendTo(address, port, new JsonMessage()
                        .put("status", "OK")
                        .put("action", "CLIENT_LIST")
                        .put("count", registeredClients.size())
                        .put("clients", registeredClients.keySet().toString()));
                    break;
                
                default:
                    sendTo(address, port, JsonMessage.error("Acción desconocida: " + action));
            }
            
        } catch (Exception e) {
            System.err.println("[ERROR] [UDP] Error procesando: " + e.getMessage());
            sendTo(address, port, JsonMessage.error("Error: " + e.getMessage()));
        }
    }
    
    /**
     * Envía un mensaje a un cliente específico
     */
    private void sendTo(InetAddress address, int port, JsonMessage message) {
        try {
            byte[] data = message.toString().getBytes();
            DatagramPacket packet = new DatagramPacket(data, data.length, address, port);
            socket.send(packet);
        } catch (IOException e) {
            System.err.println("[ERROR] [UDP] Error enviando a " + address + ":" + port);
        }
    }
    
    /**
     * ═══════════════════════════════════════════════════════════════
     * BROADCAST - Envía mensaje a TODOS los clientes registrados
     * Este método será llamado desde TcpServer cuando haya cambios
     * ═══════════════════════════════════════════════════════════════
     */
    public void broadcast(JsonMessage message) {
        if (registeredClients.isEmpty()) {
            System.out.println("📢 [UDP] No hay clientes registrados para broadcast");
            return;
        }
        
        byte[] data = message.toString().getBytes();
        int sent = 0;
        
        for (InetSocketAddress clientAddr : registeredClients.values()) {
            try {
                DatagramPacket packet = new DatagramPacket(
                    data, data.length, 
                    clientAddr.getAddress(), 
                    clientAddr.getPort()
                );
                socket.send(packet);
                sent++;
            } catch (IOException e) {
                System.err.println("[ERROR] [UDP] Error enviando broadcast a " + clientAddr);
            }
        }
        
        System.out.println("📢 [UDP] Broadcast enviado a " + sent + "/" + registeredClients.size() + " clientes");
    }
    
    /**
     * Envía notificación de cambio de dispositivo
     */
    public void notifyDeviceChange(String deviceId, String command, String status) {
        JsonMessage notification = new JsonMessage()
            .put("action", "DEVICE_CHANGED")
            .put("deviceId", deviceId)
            .put("command", command)
            .put("status", status)
            .put("timestamp", System.currentTimeMillis());
        
        broadcast(notification);
    }
    
    /**
     * Obtiene el número de clientes registrados
     */
    public int getClientCount() {
        return registeredClients.size();
    }
    
    /**
     * Detiene el servidor UDP
     */
    public void stop() {
        running = false;
        if (socket != null && !socket.isClosed()) {
            socket.close();
        }
        System.out.println("  📢 UDP Server detenido");
    }
    
    /**
     * Método main para probar el servidor UDP independientemente
     */
    public static void main(String[] args) {
        System.out.println("═══════════════════════════════════════════════════════");
        System.out.println("  [HOME] SMART HOME - Servidor UDP (Standalone)");
        System.out.println("═══════════════════════════════════════════════════════");
        
        UdpServer server = new UdpServer();
        
        Runtime.getRuntime().addShutdownHook(new Thread(() -> {
            System.out.println("\n[WARN]  Cerrando servidor UDP...");
            server.stop();
        }));
        
        server.run();
    }
}
