package com.smarthome.client;

import java.io.*;
import java.net.*;
import java.util.concurrent.*;
import java.util.function.Consumer;

/**
 * Cliente TCP para conectar al Smart Home Server
 * Maneja la comunicación con el servidor
 */
public class TcpClient {
    
    private String host;
    private int port;
    private Socket socket;
    private PrintWriter output;
    private BufferedReader input;
    private volatile boolean connected = false;
    private volatile boolean running = false;
    
    // Listener para mensajes del servidor
    private Consumer<String> messageListener;
    
    // Hilo para escuchar mensajes
    private Thread listenerThread;
    
    public TcpClient(String host, int port) {
        this.host = host;
        this.port = port;
    }
    
    /**
     * Conectar al servidor
     */
    public boolean connect() {
        try {
            socket = new Socket(host, port);
            output = new PrintWriter(socket.getOutputStream(), true);
            input = new BufferedReader(new InputStreamReader(socket.getInputStream()));
            connected = true;
            running = true;
            
            // Iniciar hilo para escuchar mensajes
            startListening();
            
            System.out.println("✅ Conectado a " + host + ":" + port);
            return true;
            
        } catch (IOException e) {
            System.err.println("❌ Error conectando: " + e.getMessage());
            return false;
        }
    }
    
    /**
     * Inicia el hilo que escucha mensajes del servidor
     */
    private void startListening() {
        listenerThread = new Thread(() -> {
            try {
                String line;
                while (running && (line = input.readLine()) != null) {
                    final String message = line;
                    if (messageListener != null) {
                        // Ejecutar en el hilo de eventos de Swing
                        javax.swing.SwingUtilities.invokeLater(() -> 
                            messageListener.accept(message)
                        );
                    }
                }
            } catch (IOException e) {
                if (running) {
                    System.err.println("Error leyendo: " + e.getMessage());
                }
            }
            connected = false;
        });
        listenerThread.setDaemon(true);
        listenerThread.start();
    }
    
    /**
     * Enviar mensaje al servidor
     */
    public void send(String message) {
        if (connected && output != null) {
            output.println(message);
        }
    }
    
    /**
     * Enviar acción JSON
     */
    public void sendAction(String action) {
        send("{\"action\": \"" + action + "\"}");
    }
    
    /**
     * Login
     */
    public void login(String username, String password) {
        send("{\"action\": \"LOGIN\", \"username\": \"" + username + "\", \"password\": \"" + password + "\"}");
    }
    
    /**
     * Obtener dispositivos
     */
    public void getDevices() {
        sendAction("GET_DEVICES");
    }
    
    /**
     * Obtener dispositivos por habitación
     */
    public void getDevicesByRoom(String room) {
        send("{\"action\": \"GET_DEVICES\", \"room\": \"" + room + "\"}");
    }
    
    /**
     * Obtener habitaciones
     */
    public void getRooms() {
        sendAction("GET_ROOMS");
    }
    
    /**
     * Controlar dispositivo
     */
    public void controlDevice(String deviceId, String command) {
        send("{\"action\": \"DEVICE_CONTROL\", \"deviceId\": \"" + deviceId + "\", \"command\": \"" + command + "\"}");
    }
    
    /**
     * Controlar dispositivo con valor
     */
    public void controlDevice(String deviceId, String command, String value) {
        send("{\"action\": \"DEVICE_CONTROL\", \"deviceId\": \"" + deviceId + "\", \"command\": \"" + command + "\", \"value\": \"" + value + "\"}");
    }
    
    /**
     * Controlar dispositivo - color
     */
    public void setDeviceColor(String deviceId, String color) {
        send("{\"action\": \"DEVICE_CONTROL\", \"deviceId\": \"" + deviceId + "\", \"command\": \"SET_COLOR\", \"color\": \"" + color + "\"}");
    }
    
    /**
     * Enviar comando a speaker (PLAY, PAUSE, STOP, NEXT, PREV, número de pista)
     */
    public void sendSpeakerCommand(String deviceId, String command) {
        send("{\"action\": \"DEVICE_CONTROL\", \"deviceId\": \"" + deviceId + "\", \"command\": \"SPEAKER_CMD\", \"speakerCommand\": \"" + command + "\"}");
    }
    
    /**
     * Establecer listener de mensajes
     */
    public void setMessageListener(Consumer<String> listener) {
        this.messageListener = listener;
    }
    
    /**
     * Verificar si está conectado
     */
    public boolean isConnected() {
        return connected;
    }
    
    /**
     * Desconectar
     */
    public void disconnect() {
        running = false;
        connected = false;
        try {
            if (output != null) {
                send("{\"action\": \"DISCONNECT\"}");
            }
            if (socket != null && !socket.isClosed()) {
                socket.close();
            }
        } catch (IOException e) {
            // Ignorar
        }
        System.out.println("🔌 Desconectado");
    }
}
