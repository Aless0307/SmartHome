package com.smarthome.server;

import java.io.*;
import java.net.*;
import java.util.concurrent.*;
import java.util.*;
import com.smarthome.protocol.JsonMessage;
import com.smarthome.database.MongoDBConnection;
import com.smarthome.service.*;
import com.smarthome.model.*;
import com.smarthome.security.JwtUtil;

/**
 * ===============================================================
 * SERVIDOR TCP - Smart Home
 * Fase 3: Autenticación + Control de dispositivos con MongoDB
 * 
 * Protocolo JSON:
 * - Entrada: {"action": "COMANDO", "param1": "valor1", ...}
 * - Salida: {"status": "OK/ERROR", "message": "...", ...}
 * ===============================================================
 */
public class TcpServer {
    
    private static final int PORT = 5000;
    private static final int MAX_CLIENTS = 10;
    
    private ServerSocket serverSocket;
    private volatile boolean running = true;
    private ExecutorService threadPool;
    private int clientCount = 0;
    
    // Singleton para acceso desde RestServer
    private static TcpServer instance;
    
    // Servicios de base de datos
    private UserService userService;
    private DeviceService deviceService;
    private HouseService houseService;
    
    // Clientes conectados (para broadcast)
    private Map<Integer, ClientHandler> connectedClients = new ConcurrentHashMap<>();
    
    // Servidor UDP para notificaciones broadcast
    private UdpServer udpServer;
    private Thread udpThread;
    
    // Servidor REST para API HTTP
    private RestServer restServer;
    private Thread restThread;
    
    public TcpServer() {
        instance = this;
    }
    
    public static TcpServer getInstance() {
        return instance;
    }
    
    /**
     * Inicia el servidor TCP
     */
    public void start() {
        try {
            // Inicializar conexión a MongoDB
            System.out.println("[CONN] Conectando a MongoDB...");
            MongoDBConnection.getInstance();
            
            // Inicializar servicios
            userService = new UserService();
            deviceService = new DeviceService();
            houseService = new HouseService();
            
            // Iniciar servidor UDP en hilo separado
            udpServer = new UdpServer();
            udpThread = new Thread(udpServer, "UDP-Server");
            udpThread.setDaemon(true);
            udpThread.start();
            
            // Iniciar servidor REST en hilo separado
            restServer = new RestServer();
            restThread = new Thread(() -> {
                try {
                    restServer.start();
                } catch (Exception e) {
                    System.err.println("[ERROR] Error iniciando REST: " + e.getMessage());
                }
            }, "REST-Server");
            restThread.setDaemon(true);
            restThread.start();
            
            // Iniciar servidor de streaming de cámaras
            CameraStreamServer.getInstance().start();
            
            // Pequeña pausa para que los otros servidores inicien
            Thread.sleep(500);
            
            // Crear el socket del servidor TCP
            serverSocket = new ServerSocket(PORT);
            threadPool = Executors.newFixedThreadPool(MAX_CLIENTS);
            
            System.out.println("\n=======================================================");
            System.out.println("  [HOME] SMART HOME - Servidor Completo");
            System.out.println("  [NET] TCP Puerto: " + PORT + " (Control principal)");
            System.out.println("  [UDP] UDP Puerto: 5001 (Notificaciones broadcast)");
            System.out.println("  [WEB] REST Puerto: 8080 (API HTTP)");
            System.out.println("  [CAM] Stream Puerto: 8081 (Cámaras HTTP) / 8082 (UDP frames)");
            System.out.println("  [POOL] Pool de hilos: " + MAX_CLIENTS + " máximo");
            System.out.println("  [DB]  MongoDB: Conectado");
            System.out.println("  [DEV] Dispositivos: " + deviceService.count());
            System.out.println("  [USERS] Usuarios: " + userService.count());
            System.out.println("  [WAIT] Esperando conexiones...");
            System.out.println("=======================================================");
            
            // Bucle principal
            while (running) {
                try {
                    Socket clientSocket = serverSocket.accept();
                    clientCount++;
                    
                    String clientInfo = clientSocket.getInetAddress().getHostAddress() 
                                      + ":" + clientSocket.getPort();
                    System.out.println("\n[OK] Cliente #" + clientCount + " conectado: " + clientInfo);
                    
                    ClientHandler handler = new ClientHandler(clientSocket, clientCount);
                    connectedClients.put(clientCount, handler);
                    threadPool.execute(handler);
                    
                } catch (IOException e) {
                    if (running) {
                        System.err.println("[ERROR] Error aceptando conexión: " + e.getMessage());
                    }
                }
            }
            
        } catch (Exception e) {
            System.err.println("[ERROR] Error iniciando servidor: " + e.getMessage());
            e.printStackTrace();
        }
    }
    
    /**
     * Envía un mensaje a todos los clientes conectados
     */
    public void broadcast(JsonMessage message) {
        for (ClientHandler client : connectedClients.values()) {
            if (client.isLoggedIn()) {
                client.sendResponse(message);
            }
        }
    }
    
    /**
     * ===============================================================
     * CLASE INTERNA: Manejador de Cliente
     * ===============================================================
     */
    private class ClientHandler implements Runnable {
        
        private Socket clientSocket;
        private int clientId;
        private PrintWriter output;
        private BufferedReader input;
        
        // Datos de sesión
        private User currentUser = null;
        private String sessionToken = null;
        
        public ClientHandler(Socket socket, int id) {
            this.clientSocket = socket;
            this.clientId = id;
        }
        
        public boolean isLoggedIn() {
            return currentUser != null;
        }
        
        public User getCurrentUser() {
            return currentUser;
        }
        
        @Override
        public void run() {
            String threadName = Thread.currentThread().getName();
            System.out.println("[POOL] Cliente #" + clientId + " asignado a: " + threadName);
            
            try {
                handleClient();
            } catch (Exception e) {
                System.err.println("[ERROR] Error en cliente #" + clientId + ": " + e.getMessage());
            } finally {
                connectedClients.remove(clientId);
            }
        }
        
        private void handleClient() {
            try {
                input = new BufferedReader(
                    new InputStreamReader(clientSocket.getInputStream())
                );
                output = new PrintWriter(clientSocket.getOutputStream(), true);
                
                // Mensaje de bienvenida
                sendResponse(new JsonMessage()
                    .put("status", "OK")
                    .put("action", "CONNECTED")
                    .put("message", "Bienvenido al Smart Home Server")
                    .put("clientId", clientId));
                
                // Leer mensajes
                String line;
                while ((line = input.readLine()) != null) {
                    System.out.println("[MSG] [Cliente #" + clientId + "] " + line);
                    processMessage(line);
                }
                
                System.out.println("[CONN] Cliente #" + clientId + " desconectado");
                
                input.close();
                output.close();
                clientSocket.close();
                
            } catch (IOException e) {
                System.err.println("[ERROR] Error cliente #" + clientId + ": " + e.getMessage());
            }
        }
        
        /**
         * Procesa un mensaje JSON y ejecuta la acción correspondiente
         */
        private void processMessage(String jsonStr) {
            try {
                JsonMessage request = JsonMessage.parse(jsonStr);
                String action = request.getString("action");
                
                if (action == null) {
                    sendResponse(JsonMessage.error("Falta el campo 'action'"));
                    return;
                }
                
                switch (action.toUpperCase()) {
                    
                    case "PING":
                        handlePing();
                        break;
                    
                    case "LOGIN":
                        handleLogin(request);
                        break;
                    
                    case "REGISTER":
                        handleRegister(request);
                        break;
                    
                    case "GET_DEVICES":
                        handleGetDevices(request);
                        break;
                    
                    case "GET_DEVICE":
                        handleGetDevice(request);
                        break;
                    
                    case "DEVICE_CONTROL":
                        handleDeviceControl(request);
                        break;
                    
                    case "GET_ROOMS":
                        handleGetRooms();
                        break;
                    
                    case "SET_TRACKS":
                        handleSetTracks(request);
                        break;
                    
                    case "LOGOUT":
                        handleLogout();
                        break;
                    
                    case "DISCONNECT":
                        handleDisconnect();
                        break;
                    
                    default:
                        sendResponse(JsonMessage.error("Acción desconocida: " + action));
                }
                
            } catch (Exception e) {
                System.err.println("[ERROR] Error procesando: " + e.getMessage());
                sendResponse(JsonMessage.error("Error: " + e.getMessage()));
            }
        }
        
        // ===========================================================
        // HANDLERS DE ACCIONES
        // ===========================================================
        
        private void handlePing() {
            sendResponse(new JsonMessage()
                .put("status", "OK")
                .put("action", "PONG")
                .put("timestamp", System.currentTimeMillis())
                .put("loggedIn", currentUser != null));
        }
        
        private void handleLogin(JsonMessage request) {
            String username = request.getString("username");
            String password = request.getString("password");
            
            if (username == null || password == null) {
                sendResponse(JsonMessage.error("Faltan credenciales"));
                return;
            }
            
            // Verificar en la base de datos
            User user = userService.login(username, password);
            
            if (user != null) {
                currentUser = user;
                // Generar token JWT real
                sessionToken = JwtUtil.generateToken(user.getUsername(), user.getRole());
                
                System.out.println("[USER] Login exitoso: " + username + " (Cliente #" + clientId + ")");
                System.out.println("[AUTH] JWT generado para: " + username);
                
                sendResponse(new JsonMessage()
                    .put("status", "OK")
                    .put("action", "LOGIN_SUCCESS")
                    .put("username", user.getUsername())
                    .put("role", user.getRole())
                    .put("houseId", user.getHouseId())
                    .put("token", sessionToken)
                    .put("tokenType", "JWT")
                    .put("message", "Sesión iniciada correctamente"));
            } else {
                sendResponse(new JsonMessage()
                    .put("status", "ERROR")
                    .put("action", "LOGIN_FAILED")
                    .put("message", "Usuario o contraseña incorrectos"));
            }
        }
        
        private void handleRegister(JsonMessage request) {
            String username = request.getString("username");
            String password = request.getString("password");
            String email = request.getString("email");
            
            if (username == null || password == null) {
                sendResponse(JsonMessage.error("Faltan campos obligatorios"));
                return;
            }
            
            // Verificar si ya existe
            if (userService.findByUsername(username) != null) {
                sendResponse(JsonMessage.error("El usuario ya existe"));
                return;
            }
            
            // Crear usuario
            User newUser = new User(username, password, email != null ? email : "");
            
            // Asignar la casa existente
            List<House> houses = houseService.findAll();
            if (!houses.isEmpty()) {
                newUser.setHouseId(houses.get(0).getIdString());
            }
            
            User created = userService.create(newUser);
            
            if (created != null) {
                sendResponse(new JsonMessage()
                    .put("status", "OK")
                    .put("action", "REGISTER_SUCCESS")
                    .put("username", created.getUsername())
                    .put("message", "Usuario registrado correctamente"));
            } else {
                sendResponse(JsonMessage.error("Error al crear usuario"));
            }
        }
        
        private void handleGetDevices(JsonMessage request) {
            if (!requireLogin()) return;
            
            String room = request.getString("room");
            String type = request.getString("type");
            
            List<Device> devices;
            
            if (room != null) {
                devices = deviceService.findByRoom(room);
            } else if (type != null) {
                devices = deviceService.findByType(type);
            } else {
                devices = deviceService.findByHouseId(currentUser.getHouseId());
            }
            
            // Construir lista JSON
            StringBuilder devicesJson = new StringBuilder("[");
            for (int i = 0; i < devices.size(); i++) {
                if (i > 0) devicesJson.append(",");
                devicesJson.append(devices.get(i).toJson());
            }
            devicesJson.append("]");
            
            sendResponse(new JsonMessage()
                .put("status", "OK")
                .put("action", "DEVICES_LIST")
                .put("count", devices.size())
                .put("devices", devicesJson.toString()));
        }
        
        private void handleGetDevice(JsonMessage request) {
            if (!requireLogin()) return;
            
            String deviceId = request.getString("deviceId");
            if (deviceId == null) {
                sendResponse(JsonMessage.error("Falta deviceId"));
                return;
            }
            
            Device device = deviceService.findById(deviceId);
            if (device == null) {
                sendResponse(JsonMessage.error("Dispositivo no encontrado"));
                return;
            }
            
            sendResponse(new JsonMessage()
                .put("status", "OK")
                .put("action", "DEVICE_INFO")
                .put("device", device.toJson()));
        }
        
        private void handleDeviceControl(JsonMessage request) {
            if (!requireLogin()) return;
            
            String deviceId = request.getString("deviceId");
            String command = request.getString("command");
            
            if (deviceId == null || command == null) {
                sendResponse(JsonMessage.error("Faltan deviceId o command"));
                return;
            }
            
            Device device = deviceService.findById(deviceId);
            if (device == null) {
                sendResponse(JsonMessage.error("Dispositivo no encontrado"));
                return;
            }
            
            boolean success = false;
            
            switch (command.toUpperCase()) {
                case "ON":
                    success = deviceService.updateStatus(deviceId, true);
                    break;
                    
                case "OFF":
                    success = deviceService.updateStatus(deviceId, false);
                    break;
                    
                case "TOGGLE":
                    success = deviceService.updateStatus(deviceId, !device.isStatus());
                    break;
                    
                case "SET_VALUE":
                    String valueStr = request.getString("value");
                    if (valueStr != null) {
                        try {
                            int value = Integer.parseInt(valueStr);
                            success = deviceService.updateValue(deviceId, value);
                        } catch (NumberFormatException e) {
                            sendResponse(JsonMessage.error("Valor inválido"));
                            return;
                        }
                    }
                    break;
                    
                case "SET_COLOR":
                    String color = request.getString("color");
                    if (color != null) {
                        success = deviceService.updateColor(deviceId, color);
                    }
                    break;
                    
                case "SPEAKER_CMD":
                    // Comando especial para bocinas (PLAY, PAUSE, STOP, NEXT, PREV, número de pista)
                    String speakerCmd = request.getString("speakerCommand");
                    if (speakerCmd != null) {
                        // Guardar el comando en el campo "color" temporalmente para enviarlo a Unity
                        // Unity leerá este campo y ejecutará el comando
                        success = deviceService.updateColor(deviceId, "CMD:" + speakerCmd);
                        System.out.println("🔊 Speaker comando: " + speakerCmd);
                    }
                    break;
                    
                default:
                    sendResponse(JsonMessage.error("Comando desconocido: " + command));
                    return;
            }
            
            if (success) {
                // Obtener dispositivo actualizado
                Device updated = deviceService.findById(deviceId);
                
                System.out.println("🎮 " + currentUser.getUsername() + " -> " + 
                                   command + " -> " + device.getName());
                
                JsonMessage response = new JsonMessage()
                    .put("status", "OK")
                    .put("action", "DEVICE_UPDATED")
                    .put("deviceId", deviceId)
                    .put("command", command)
                    .put("device", updated.toJson());
                
                // Responder al cliente
                sendResponse(response);
                
                // Notificar a otros clientes (broadcast)
                JsonMessage broadcastMsg = new JsonMessage()
                    .put("status", "OK")
                    .put("action", "DEVICE_CHANGED")
                    .put("deviceId", deviceId)
                    .put("changedBy", currentUser.getUsername())
                    .put("device", updated.toJson());
                
                // Broadcast TCP a clientes conectados
                broadcast(broadcastMsg);
                
                // Broadcast UDP a clientes registrados
                if (udpServer != null) {
                    udpServer.broadcast(broadcastMsg);
                }
                
            } else {
                sendResponse(JsonMessage.error("Error actualizando dispositivo"));
            }
        }
        
        private void handleGetRooms() {
            if (!requireLogin()) return;
            
            House house = houseService.findById(currentUser.getHouseId());
            if (house == null) {
                sendResponse(JsonMessage.error("Casa no encontrada"));
                return;
            }
            
            // Construir lista de habitaciones
            StringBuilder roomsJson = new StringBuilder("[");
            List<String> rooms = house.getRooms();
            for (int i = 0; i < rooms.size(); i++) {
                if (i > 0) roomsJson.append(",");
                roomsJson.append("\"").append(rooms.get(i)).append("\"");
            }
            roomsJson.append("]");
            
            sendResponse(new JsonMessage()
                .put("status", "OK")
                .put("action", "ROOMS_LIST")
                .put("houseName", house.getName())
                .put("rooms", roomsJson.toString()));
        }
        
        private void handleSetTracks(JsonMessage request) {
            // No requiere login, Unity puede enviar tracks sin autenticarse
            String deviceId = request.getString("deviceId");
            String tracksJson = request.getString("tracks");
            
            if (deviceId == null || tracksJson == null) {
                sendResponse(JsonMessage.error("Faltan deviceId o tracks"));
                return;
            }
            
            // El tracks viene como: [\"Newspaper\",\"Track2\"]
            // Necesitamos limpiarlo
            java.util.List<String> tracks = new java.util.ArrayList<>();
            
            // Primero reemplazar las comillas escapadas por un marcador temporal
            String cleaned = tracksJson.trim();
            cleaned = cleaned.replace("\\\"", "\u0001"); // Marcador temporal
            
            // Quitar corchetes
            if (cleaned.startsWith("[")) cleaned = cleaned.substring(1);
            if (cleaned.endsWith("]")) cleaned = cleaned.substring(0, cleaned.length() - 1);
            
            if (!cleaned.isEmpty()) {
                // Dividir por coma
                String[] parts = cleaned.split(",");
                for (String part : parts) {
                    String track = part.trim();
                    // Restaurar comillas y quitarlas
                    track = track.replace("\u0001", "\"");
                    // Quitar comillas externas
                    if (track.startsWith("\"")) track = track.substring(1);
                    if (track.endsWith("\"")) track = track.substring(0, track.length() - 1);
                    if (!track.isEmpty()) {
                        tracks.add(track);
                    }
                }
            }
            
            System.out.println("[TRACKS] Guardando " + tracks.size() + " tracks: " + tracks);
            boolean success = deviceService.updateTracks(deviceId, tracks);
            
            if (success) {
                sendResponse(new JsonMessage()
                    .put("status", "OK")
                    .put("action", "TRACKS_UPDATED")
                    .put("deviceId", deviceId)
                    .put("trackCount", tracks.size()));
            } else {
                sendResponse(JsonMessage.error("Error actualizando tracks"));
            }
        }
        
        private void handleLogout() {
            if (currentUser != null) {
                System.out.println("👋 Logout: " + currentUser.getUsername());
            }
            currentUser = null;
            sessionToken = null;
            
            sendResponse(new JsonMessage()
                .put("status", "OK")
                .put("action", "LOGOUT_SUCCESS")
                .put("message", "Sesión cerrada"));
        }
        
        private void handleDisconnect() throws IOException {
            sendResponse(new JsonMessage()
                .put("status", "OK")
                .put("action", "GOODBYE")
                .put("message", "Hasta luego!"));
            clientSocket.close();
        }
        
        /**
         * Verifica que el usuario esté logueado y que el token JWT sea válido
         */
        private boolean requireLogin() {
            if (currentUser == null) {
                sendResponse(new JsonMessage()
                    .put("status", "ERROR")
                    .put("action", "AUTH_REQUIRED")
                    .put("message", "Debes iniciar sesión primero"));
                return false;
            }
            
            // Validar que el token JWT siga siendo válido
            if (sessionToken == null || !JwtUtil.validateToken(sessionToken)) {
                // Token expirado o inválido, cerrar sesión
                currentUser = null;
                sessionToken = null;
                sendResponse(new JsonMessage()
                    .put("status", "ERROR")
                    .put("action", "TOKEN_EXPIRED")
                    .put("message", "Tu sesión ha expirado, inicia sesión nuevamente"));
                return false;
            }
            
            return true;
        }
        
        /**
         * Envía una respuesta JSON al cliente
         */
        public void sendResponse(JsonMessage response) {
            if (output != null) {
                output.println(response.toString());
            }
        }
    }
    
    /**
     * Detiene el servidor
     */
    public void stop() {
        running = false;
        try {
            if (threadPool != null) {
                threadPool.shutdown();
                if (!threadPool.awaitTermination(5, TimeUnit.SECONDS)) {
                    threadPool.shutdownNow();
                }
            }
            
            if (serverSocket != null && !serverSocket.isClosed()) {
                serverSocket.close();
            }
            
            MongoDBConnection.getInstance().close();
            System.out.println("\n🛑 Servidor detenido");
            
        } catch (IOException | InterruptedException e) {
            System.err.println("[ERROR] Error cerrando servidor: " + e.getMessage());
        }
    }
    
    public static void main(String[] args) {
        TcpServer server = new TcpServer();
        
        Runtime.getRuntime().addShutdownHook(new Thread(() -> {
            System.out.println("\n[WARN]  Cerrando servidor...");
            server.stop();
        }));
        
        server.start();
    }
}
