package com.smarthome.server;

import com.sun.net.httpserver.HttpServer;
import com.sun.net.httpserver.HttpHandler;
import com.sun.net.httpserver.HttpExchange;
import com.smarthome.database.MongoDBConnection;
import com.smarthome.service.*;
import com.smarthome.model.*;
import com.smarthome.security.JwtUtil;
import com.smarthome.protocol.JsonMessage;

import java.io.*;
import java.net.InetSocketAddress;
import java.util.List;
import java.util.Map;
import java.util.HashMap;

/**
 * SERVIDOR REST - Smart Home - API HTTP para control de dispositivos y debug
 * Puerto: 8080
 */
public class RestServer {
    
    private static final int PORT = 8080;
    private HttpServer server;
    
    // Servicios
    private UserService userService;
    private DeviceService deviceService;
    private HouseService houseService;
    private EnergyService energyService;
    private ActivityService activityService;
    
    public void start() throws IOException {
        // Inicializar MongoDB
        System.out.println("[CONN] Conectando a MongoDB...");
        MongoDBConnection.getInstance();
        
        // Inicializar servicios
        userService = new UserService();
        deviceService = new DeviceService();
        houseService = new HouseService();
        energyService = new EnergyService();
        activityService = ActivityService.getInstance();
        
        // Iniciar muestreo de energia (cada 30 segundos)
        energyService.startSampling();
        
        // Crear servidor HTTP
        server = HttpServer.create(new InetSocketAddress(PORT), 0);
        
        // Registrar endpoints
        server.createContext("/", new HomeHandler());
        server.createContext("/api/devices", new DevicesHandler());
        server.createContext("/api/device", new DeviceHandler());
        server.createContext("/api/rooms", new RoomsHandler());
        server.createContext("/api/users", new UsersHandler());
        server.createContext("/api/login", new LoginHandler());
        server.createContext("/api/register", new RegisterHandler());
        server.createContext("/api/control", new ControlHandler());
        server.createContext("/api/energy", new EnergyHandler());
        server.createContext("/api/activity", new ActivityHandler());
        
        server.setExecutor(null);
        server.start();
        
        System.out.println("\n=======================================================");
        System.out.println("  [REST] SMART HOME - Servidor REST");
        System.out.println("  [NET] Puerto: " + PORT);
        System.out.println("  [DB] MongoDB: Conectado");
        System.out.println("  [DEV] Dispositivos: " + deviceService.count());
        System.out.println("=======================================================");
        System.out.println("\n[INFO] Endpoints disponibles:");
        System.out.println("  GET  http://localhost:" + PORT + "/              - Panel de control");
        System.out.println("  GET  http://localhost:" + PORT + "/api/devices   - Lista dispositivos");
        System.out.println("  GET  http://localhost:" + PORT + "/api/device?id=X - Info dispositivo");
        System.out.println("  GET  http://localhost:" + PORT + "/api/rooms     - Lista habitaciones");
        System.out.println("  GET  http://localhost:" + PORT + "/api/users     - Lista usuarios");
        System.out.println("  POST http://localhost:" + PORT + "/api/login     - Login");
        System.out.println("  POST http://localhost:" + PORT + "/api/register  - Registro");
        System.out.println("  POST http://localhost:" + PORT + "/api/control   - Controlar dispositivo");
        System.out.println("  GET  http://localhost:" + PORT + "/api/energy    - Estadisticas de energia");
        System.out.println("  GET  http://localhost:" + PORT + "/api/activity  - Historial de actividad");
        System.out.println("\n[OK] Servidor listo...");
    }
    
    public void stop() {
        if (energyService != null) {
            energyService.stopSampling();
        }
        if (server != null) {
            server.stop(0);
            MongoDBConnection.getInstance().close();
            System.out.println("[STOP] Servidor REST detenido");
        }
    }
    
    // ═══════════════════════════════════════════════════════════
    // HANDLERS
    // ═══════════════════════════════════════════════════════════
    
    /**
     * Panel de control HTML
     */
    class HomeHandler implements HttpHandler {
        @Override
        public void handle(HttpExchange exchange) throws IOException {
            String html = generateDashboardHTML();
            sendResponse(exchange, 200, "text/html", html);
        }
    }
    
    /**
     * GET /api/devices - Lista todos los dispositivos
     * GET /api/devices?room=sala - Filtra por habitación
     * GET /api/devices?type=light - Filtra por tipo
     */
    class DevicesHandler implements HttpHandler {
        @Override
        public void handle(HttpExchange exchange) throws IOException {
            setCorsHeaders(exchange);
            
            if ("OPTIONS".equals(exchange.getRequestMethod())) {
                exchange.sendResponseHeaders(204, -1);
                return;
            }
            
            Map<String, String> params = parseQuery(exchange.getRequestURI().getQuery());
            List<Device> devices;
            
            if (params.containsKey("room")) {
                devices = deviceService.findByRoom(params.get("room"));
            } else if (params.containsKey("type")) {
                devices = deviceService.findByType(params.get("type"));
            } else {
                devices = deviceService.findAll();
            }
            
            String json = devicesToJson(devices);
            sendResponse(exchange, 200, "application/json", json);
        }
    }
    
    /**
     * GET /api/device?id=xxx - Info de un dispositivo
     */
    class DeviceHandler implements HttpHandler {
        @Override
        public void handle(HttpExchange exchange) throws IOException {
            setCorsHeaders(exchange);
            
            if ("OPTIONS".equals(exchange.getRequestMethod())) {
                exchange.sendResponseHeaders(204, -1);
                return;
            }
            
            Map<String, String> params = parseQuery(exchange.getRequestURI().getQuery());
            String id = params.get("id");
            
            if (id == null) {
                sendResponse(exchange, 400, "application/json", 
                    "{\"error\": \"Falta parámetro id\"}");
                return;
            }
            
            Device device = deviceService.findById(id);
            if (device == null) {
                sendResponse(exchange, 404, "application/json", 
                    "{\"error\": \"Dispositivo no encontrado\"}");
                return;
            }
            
            sendResponse(exchange, 200, "application/json", device.toJson());
        }
    }
    
    /**
     * GET /api/rooms - Lista habitaciones
     */
    class RoomsHandler implements HttpHandler {
        @Override
        public void handle(HttpExchange exchange) throws IOException {
            setCorsHeaders(exchange);
            
            if ("OPTIONS".equals(exchange.getRequestMethod())) {
                exchange.sendResponseHeaders(204, -1);
                return;
            }
            
            List<House> houses = houseService.findAll();
            if (houses.isEmpty()) {
                sendResponse(exchange, 404, "application/json", 
                    "{\"error\": \"No hay casas\"}");
                return;
            }
            
            House house = houses.get(0);
            StringBuilder json = new StringBuilder();
            json.append("{\"house\": \"").append(house.getName()).append("\", \"rooms\": [");
            
            List<String> rooms = house.getRooms();
            for (int i = 0; i < rooms.size(); i++) {
                if (i > 0) json.append(",");
                json.append("\"").append(rooms.get(i)).append("\"");
            }
            json.append("]}");
            
            sendResponse(exchange, 200, "application/json", json.toString());
        }
    }
    
    /**
     * GET /api/users - Lista usuarios
     */
    class UsersHandler implements HttpHandler {
        @Override
        public void handle(HttpExchange exchange) throws IOException {
            setCorsHeaders(exchange);
            
            if ("OPTIONS".equals(exchange.getRequestMethod())) {
                exchange.sendResponseHeaders(204, -1);
                return;
            }
            
            List<User> users = userService.findAll();
            StringBuilder json = new StringBuilder("[");
            
            for (int i = 0; i < users.size(); i++) {
                if (i > 0) json.append(",");
                User u = users.get(i);
                json.append("{\"username\": \"").append(u.getUsername())
                    .append("\", \"email\": \"").append(u.getEmail())
                    .append("\", \"role\": \"").append(u.getRole())
                    .append("\"}");
            }
            json.append("]");
            
            sendResponse(exchange, 200, "application/json", json.toString());
        }
    }
    
    /**
     * POST /api/login - Login
     * Body: {"username": "admin", "password": "admin123"}
     */
    class LoginHandler implements HttpHandler {
        @Override
        public void handle(HttpExchange exchange) throws IOException {
            setCorsHeaders(exchange);
            
            if ("OPTIONS".equals(exchange.getRequestMethod())) {
                exchange.sendResponseHeaders(204, -1);
                return;
            }
            
            if (!"POST".equals(exchange.getRequestMethod())) {
                sendResponse(exchange, 405, "application/json", 
                    "{\"error\": \"Método no permitido\"}");
                return;
            }
            
            String body = readBody(exchange);
            Map<String, String> data = parseJsonSimple(body);
            
            String username = data.get("username");
            String password = data.get("password");
            
            if (username == null || password == null) {
                sendResponse(exchange, 400, "application/json", 
                    "{\"error\": \"Faltan credenciales\"}");
                return;
            }
            
            User user = userService.login(username, password);
            if (user != null) {
                // Registrar actividad de login
                String clientIp = exchange.getRemoteAddress().getAddress().getHostAddress();
                activityService.logLogin(username, clientIp);
                
                // Generar token JWT
                String token = JwtUtil.generateToken(user.getUsername(), user.getRole());
                String json = "{\"status\": \"OK\", \"username\": \"" + user.getUsername() + 
                              "\", \"role\": \"" + user.getRole() + 
                              "\", \"token\": \"" + token + 
                              "\", \"tokenType\": \"JWT\"}";
                sendResponse(exchange, 200, "application/json", json);
            } else {
                sendResponse(exchange, 401, "application/json", 
                    "{\"error\": \"Credenciales inválidas\"}");
            }
        }
    }
    
    /**
     * POST /api/register - Registro de nuevo usuario
     * Body: {"username": "nuevo", "password": "pass123", "email": "email@test.com"}
     */
    class RegisterHandler implements HttpHandler {
        @Override
        public void handle(HttpExchange exchange) throws IOException {
            setCorsHeaders(exchange);
            
            if ("OPTIONS".equals(exchange.getRequestMethod())) {
                exchange.sendResponseHeaders(204, -1);
                return;
            }
            
            if (!"POST".equals(exchange.getRequestMethod())) {
                sendResponse(exchange, 405, "application/json", 
                    "{\"error\": \"Método no permitido\"}");
                return;
            }
            
            String body = readBody(exchange);
            Map<String, String> data = parseJsonSimple(body);
            
            String username = data.get("username");
            String password = data.get("password");
            String email = data.get("email");
            
            // Validaciones
            if (username == null || username.trim().isEmpty()) {
                sendResponse(exchange, 400, "application/json", 
                    "{\"error\": \"El nombre de usuario es requerido\"}");
                return;
            }
            
            if (password == null || password.length() < 6) {
                sendResponse(exchange, 400, "application/json", 
                    "{\"error\": \"La contraseña debe tener al menos 6 caracteres\"}");
                return;
            }
            
            if (email == null || !email.contains("@")) {
                sendResponse(exchange, 400, "application/json", 
                    "{\"error\": \"Email inválido\"}");
                return;
            }
            
            // Verificar si ya existe
            if (userService.findByUsername(username) != null) {
                sendResponse(exchange, 409, "application/json", 
                    "{\"error\": \"El nombre de usuario ya existe\"}");
                return;
            }
            
            if (userService.findByEmail(email) != null) {
                sendResponse(exchange, 409, "application/json", 
                    "{\"error\": \"El email ya está registrado\"}");
                return;
            }
            
            // Crear usuario
            User newUser = new User(username, password, email);
            User created = userService.create(newUser);
            
            if (created != null) {
                // Registrar actividad
                String clientIp = exchange.getRemoteAddress().getAddress().getHostAddress();
                activityService.logRegister(username, clientIp);
                
                String json = "{\"status\": \"OK\", \"message\": \"Usuario registrado exitosamente\", " +
                              "\"username\": \"" + created.getUsername() + "\"}";
                sendResponse(exchange, 201, "application/json", json);
            } else {
                sendResponse(exchange, 500, "application/json", 
                    "{\"error\": \"Error al crear usuario\"}");
            }
        }
    }
    
    /**
     * GET /api/activity - Historial de actividad
     * Params: ?limit=50&user=admin&action=LOGIN
     */
    class ActivityHandler implements HttpHandler {
        @Override
        public void handle(HttpExchange exchange) throws IOException {
            setCorsHeaders(exchange);
            
            if ("OPTIONS".equals(exchange.getRequestMethod())) {
                exchange.sendResponseHeaders(204, -1);
                return;
            }
            
            Map<String, String> params = parseQuery(exchange.getRequestURI().getQuery());
            int limit = 50;
            try {
                if (params.containsKey("limit")) {
                    limit = Integer.parseInt(params.get("limit"));
                }
            } catch (NumberFormatException e) {
                // Usar default
            }
            
            java.util.List<ActivityLog> logs;
            
            // Filtrar por usuario si se especifica
            String filterUser = params.get("user");
            String filterAction = params.get("action");
            String filterDevice = params.get("device");
            
            if (filterUser != null && !filterUser.isEmpty()) {
                logs = activityService.getByUser(filterUser, limit);
            } else if (filterAction != null && !filterAction.isEmpty()) {
                logs = activityService.getByAction(filterAction, limit);
            } else if (filterDevice != null && !filterDevice.isEmpty()) {
                logs = activityService.getByDevice(filterDevice, limit);
            } else {
                logs = activityService.getRecent(limit);
            }
            
            // Construir JSON
            StringBuilder json = new StringBuilder("{\"count\": ").append(logs.size()).append(", \"logs\": [");
            for (int i = 0; i < logs.size(); i++) {
                if (i > 0) json.append(",");
                json.append(logs.get(i).toJson());
            }
            json.append("]}");
            
            sendResponse(exchange, 200, "application/json", json.toString());
        }
    }
    
    /**
     * POST /api/control - Controlar dispositivo
     * Body: {"deviceId": "xxx", "command": "ON|OFF|TOGGLE|SET_VALUE|SET_COLOR", "value": "..."}
     * Header: Authorization: Bearer <JWT_TOKEN>
     */
    class ControlHandler implements HttpHandler {
        @Override
        public void handle(HttpExchange exchange) throws IOException {
            setCorsHeaders(exchange);
            
            if ("OPTIONS".equals(exchange.getRequestMethod())) {
                exchange.sendResponseHeaders(204, -1);
                return;
            }
            
            if (!"POST".equals(exchange.getRequestMethod())) {
                sendResponse(exchange, 405, "application/json", 
                    "{\"error\": \"Método no permitido\"}");
                return;
            }
            
            // Validar JWT (opcional para REST, pero buena práctica)
            String authHeader = exchange.getRequestHeaders().getFirst("Authorization");
            String token = null;
            String username = "anonymous";
            
            if (authHeader != null && authHeader.startsWith("Bearer ")) {
                token = authHeader.substring(7);
                if (!JwtUtil.validateToken(token)) {
                    sendResponse(exchange, 401, "application/json", 
                        "{\"error\": \"Token inválido o expirado\"}");
                    return;
                }
                username = JwtUtil.getUsername(token);
                System.out.println("[AUTH] REST autenticado: " + username);
            }
            
            String body = readBody(exchange);
            Map<String, String> data = parseJsonSimple(body);
            
            String deviceId = data.get("deviceId");
            String command = data.get("command");
            
            if (deviceId == null || command == null) {
                sendResponse(exchange, 400, "application/json", 
                    "{\"error\": \"Faltan deviceId o command\"}");
                return;
            }
            
            Device device = deviceService.findById(deviceId);
            if (device == null) {
                sendResponse(exchange, 404, "application/json", 
                    "{\"error\": \"Dispositivo no encontrado\"}");
                return;
            }
            
            // Guardar tipo para logging de porton
            String tipoDispositivo = device.getType();
            
            boolean success = false;
            
            switch (command.toUpperCase()) {
                case "ON":
                    success = deviceService.updateStatus(deviceId, true);
                    // Si es porton, registrar accion
                    if (success && "door".equals(tipoDispositivo)) {
                        energyService.logDoorAction(device, "ABRIR");
                    }
                    break;
                case "OFF":
                    success = deviceService.updateStatus(deviceId, false);
                    // Si es porton, registrar accion
                    if (success && "door".equals(tipoDispositivo)) {
                        energyService.logDoorAction(device, "CERRAR");
                    }
                    break;
                case "TOGGLE":
                    boolean nuevoEstado = !device.isStatus();
                    success = deviceService.updateStatus(deviceId, nuevoEstado);
                    // Si es porton, registrar accion
                    if (success && "door".equals(tipoDispositivo)) {
                        energyService.logDoorAction(device, nuevoEstado ? "ABRIR" : "CERRAR");
                    }
                    break;
                case "SET_VALUE":
                    String value = data.get("value");
                    if (value != null) {
                        int nuevoValor = Integer.parseInt(value);
                        success = deviceService.updateValue(deviceId, nuevoValor);
                    }
                    break;
                case "SET_COLOR":
                    String color = data.get("value");
                    if (color != null) {
                        success = deviceService.updateColor(deviceId, color);
                    }
                    break;
                case "SPEAKER_CMD":
                    // Comandos especiales para speaker: PLAY, PAUSE, STOP, NEXT, PREV
                    String speakerCmd = data.get("value");
                    System.out.println("[REST] SPEAKER_CMD value=" + speakerCmd);
                    if (speakerCmd != null && !speakerCmd.isEmpty()) {
                        // Enviar comando via color con prefijo CMD:
                        String cmdColor = "CMD:" + speakerCmd.toUpperCase();
                        System.out.println("[REST] Enviando color=" + cmdColor + " a " + deviceId);
                        success = deviceService.updateColor(deviceId, cmdColor);
                        System.out.println("[REST] updateColor success=" + success);
                    } else {
                        System.out.println("[REST] SPEAKER_CMD sin value!");
                    }
                    break;
            }
            
            if (success) {
                Device updated = deviceService.findById(deviceId);
                System.out.println("[REST] Control -> " + command + " -> " + device.getName());
                
                // Registrar actividad
                boolean isOn = updated.isStatus();
                String details = command;
                if (data.get("value") != null) {
                    details += ": " + data.get("value");
                }
                if (command.equals("ON") || command.equals("OFF") || command.equals("TOGGLE")) {
                    activityService.logDeviceControl(username, deviceId, device.getName(), 
                        device.getType(), isOn, null);
                } else {
                    activityService.logDeviceChange(username, deviceId, device.getName(), 
                        device.getType(), details);
                }
                
                // Crear mensaje de broadcast
                JsonMessage broadcastMsg = new JsonMessage()
                    .put("status", "OK")
                    .put("action", "DEVICE_CHANGED")
                    .put("deviceId", deviceId)
                    .put("source", "REST")
                    .put("device", updated.toJson());
                
                // Broadcast TCP a clientes conectados (Unity)
                TcpServer tcpServer = TcpServer.getInstance();
                if (tcpServer != null) {
                    tcpServer.broadcast(broadcastMsg);
                    System.out.println("[REST] Broadcast TCP enviado a Unity");
                }
                
                // Broadcast UDP a clientes registrados
                UdpServer udpServer = UdpServer.getInstance();
                if (udpServer != null) {
                    udpServer.broadcast(broadcastMsg);
                }
                
                // Respuesta con estado actualizado para la web
                String response = "{\"status\": \"OK\", " +
                    "\"deviceId\": \"" + deviceId + "\", " +
                    "\"newStatus\": " + updated.isStatus() + ", " +
                    "\"newValue\": " + updated.getValue() + "}";
                sendResponse(exchange, 200, "application/json", response);
            } else {
                sendResponse(exchange, 500, "application/json", 
                    "{\"error\": \"Error actualizando dispositivo\"}");
            }
        }
    }
    
    /**
     * GET /api/energy - Obtener estadisticas de consumo energetico
     * Params:
     *   - type: summary | byDevice | byType | byHour | byDay | current | logs
     *   - houseId: ID de la casa (opcional, usa primera si no se proporciona)
     *   - desde: timestamp inicio (para rangos)
     *   - hasta: timestamp fin (para rangos)
     *   - dias: numero de dias para byDay (default 7)
     *   - limit: limite de logs a retornar (default 50)
     */
    class EnergyHandler implements HttpHandler {
        @Override
        public void handle(HttpExchange exchange) throws IOException {
            setCorsHeaders(exchange);
            
            if ("OPTIONS".equals(exchange.getRequestMethod())) {
                exchange.sendResponseHeaders(204, -1);
                return;
            }
            
            Map<String, String> params = parseQuery(exchange.getRequestURI().getQuery());
            String type = params.getOrDefault("type", "summary");
            
            // Obtener houseId (usar primera casa si no se proporciona)
            String houseId = params.get("houseId");
            if (houseId == null) {
                List<House> houses = houseService.findAll();
                if (!houses.isEmpty()) {
                    houseId = houses.get(0).getIdString();
                } else {
                    sendResponse(exchange, 404, "application/json", 
                        "{\"error\": \"No hay casas registradas\"}");
                    return;
                }
            }
            
            // Timestamps para rangos (default: ultimas 24 horas)
            long ahora = System.currentTimeMillis();
            long desde = params.containsKey("desde") ? 
                Long.parseLong(params.get("desde")) : 
                ahora - (24 * 60 * 60 * 1000);
            long hasta = params.containsKey("hasta") ? 
                Long.parseLong(params.get("hasta")) : ahora;
            
            StringBuilder json = new StringBuilder();
            
            switch (type) {
                case "summary":
                    // Resumen general: consumo total, actual, costo estimado
                    double totalKwh = energyService.getTotalConsumption(houseId, desde, hasta);
                    double currentWatts = energyService.getCurrentPowerUsage();
                    double costoPorKwh = 1.20; // Precio promedio en MXN
                    double costoEstimado = energyService.calculateCost(totalKwh, costoPorKwh);
                    
                    json.append("{")
                        .append("\"totalKwh\": ").append(String.format(java.util.Locale.US, "%.4f", totalKwh)).append(",")
                        .append("\"currentWatts\": ").append(String.format(java.util.Locale.US, "%.1f", currentWatts)).append(",")
                        .append("\"costoEstimado\": ").append(String.format(java.util.Locale.US, "%.2f", costoEstimado)).append(",")
                        .append("\"precioPorKwh\": ").append(costoPorKwh).append(",")
                        .append("\"moneda\": \"MXN\",")
                        .append("\"periodoDesde\": ").append(desde).append(",")
                        .append("\"periodoHasta\": ").append(hasta)
                        .append("}");
                    break;
                    
                case "byDevice":
                    // Consumo por dispositivo
                    Map<String, Double> byDevice = energyService.getConsumptionByDevice(houseId, desde, hasta);
                    json.append("{\"consumoByDevice\": {");
                    int i = 0;
                    for (Map.Entry<String, Double> entry : byDevice.entrySet()) {
                        if (i++ > 0) json.append(",");
                        json.append("\"").append(entry.getKey()).append("\": ")
                            .append(String.format(java.util.Locale.US, "%.4f", entry.getValue()));
                    }
                    json.append("}}");
                    break;
                    
                case "byType":
                    // Consumo por tipo de dispositivo
                    Map<String, Double> byType = energyService.getConsumptionByType(houseId, desde, hasta);
                    json.append("{\"consumoByType\": {");
                    int j = 0;
                    for (Map.Entry<String, Double> entry : byType.entrySet()) {
                        if (j++ > 0) json.append(",");
                        json.append("\"").append(entry.getKey()).append("\": ")
                            .append(String.format(java.util.Locale.US, "%.4f", entry.getValue()));
                    }
                    json.append("}}");
                    break;
                    
                case "byHour":
                    // Consumo por hora (ultimas 24h)
                    Map<Integer, Double> byHour = energyService.getConsumptionByHour(houseId);
                    json.append("{\"consumoByHour\": {");
                    for (int h = 0; h < 24; h++) {
                        if (h > 0) json.append(",");
                        json.append("\"").append(h).append("\": ")
                            .append(String.format(java.util.Locale.US, "%.4f", byHour.get(h)));
                    }
                    json.append("}}");
                    break;
                    
                case "byDay":
                    // Consumo por dia
                    int dias = params.containsKey("dias") ? 
                        Integer.parseInt(params.get("dias")) : 7;
                    Map<String, Double> byDay = energyService.getConsumptionByDay(houseId, dias);
                    json.append("{\"consumoByDay\": {");
                    int k = 0;
                    for (Map.Entry<String, Double> entry : byDay.entrySet()) {
                        if (k++ > 0) json.append(",");
                        json.append("\"").append(entry.getKey()).append("\": ")
                            .append(String.format(java.util.Locale.US, "%.4f", entry.getValue()));
                    }
                    json.append("}}");
                    break;
                    
                case "current":
                    // Dispositivos actualmente consumiendo
                    Map<String, Double> activeDevices = energyService.getActiveDevicesConsumption();
                    double totalWatts = 0;
                    json.append("{\"dispositivosActivos\": {");
                    int m = 0;
                    for (Map.Entry<String, Double> entry : activeDevices.entrySet()) {
                        if (m++ > 0) json.append(",");
                        json.append("\"").append(entry.getKey()).append("\": ")
                            .append(String.format(java.util.Locale.US, "%.1f", entry.getValue()));
                        totalWatts += entry.getValue();
                    }
                    json.append("}, \"totalWatts\": ").append(String.format(java.util.Locale.US, "%.1f", totalWatts)).append("}");
                    break;
                    
                case "logs":
                    // Ultimos eventos de energia
                    int limit = params.containsKey("limit") ? 
                        Integer.parseInt(params.get("limit")) : 50;
                    java.util.List<com.smarthome.model.EnergyLog> logs = 
                        energyService.getRecentLogs(houseId, limit);
                    json.append("{\"logs\": [");
                    for (int l = 0; l < logs.size(); l++) {
                        if (l > 0) json.append(",");
                        json.append(logs.get(l).toJson());
                    }
                    json.append("]}");
                    break;
                    
                default:
                    sendResponse(exchange, 400, "application/json", 
                        "{\"error\": \"Tipo de consulta no valido. Use: summary, byDevice, byType, byHour, byDay, current, logs\"}");
                    return;
            }
            
            sendResponse(exchange, 200, "application/json", json.toString());
        }
    }
    
    // ═══════════════════════════════════════════════════════════
    // UTILIDADES
    // ═══════════════════════════════════════════════════════════
    
    private void setCorsHeaders(HttpExchange exchange) {
        exchange.getResponseHeaders().add("Access-Control-Allow-Origin", "*");
        exchange.getResponseHeaders().add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        exchange.getResponseHeaders().add("Access-Control-Allow-Headers", "Content-Type, Authorization");
    }
    
    private void sendResponse(HttpExchange exchange, int code, String contentType, String body) throws IOException {
        exchange.getResponseHeaders().set("Content-Type", contentType + "; charset=UTF-8");
        byte[] bytes = body.getBytes("UTF-8");
        exchange.sendResponseHeaders(code, bytes.length);
        OutputStream os = exchange.getResponseBody();
        os.write(bytes);
        os.close();
    }
    
    private String readBody(HttpExchange exchange) throws IOException {
        BufferedReader br = new BufferedReader(new InputStreamReader(exchange.getRequestBody()));
        StringBuilder sb = new StringBuilder();
        String line;
        while ((line = br.readLine()) != null) {
            sb.append(line);
        }
        return sb.toString();
    }
    
    private Map<String, String> parseQuery(String query) {
        Map<String, String> params = new HashMap<>();
        if (query != null) {
            for (String param : query.split("&")) {
                String[] pair = param.split("=");
                if (pair.length == 2) {
                    params.put(pair[0], pair[1]);
                }
            }
        }
        return params;
    }
    
    private Map<String, String> parseJsonSimple(String json) {
        Map<String, String> data = new HashMap<>();
        if (json == null || json.isEmpty()) return data;
        
        json = json.trim();
        if (json.startsWith("{")) json = json.substring(1);
        if (json.endsWith("}")) json = json.substring(0, json.length() - 1);
        
        String[] pairs = json.split(",");
        for (String pair : pairs) {
            String[] kv = pair.split(":");
            if (kv.length == 2) {
                String key = kv[0].trim().replace("\"", "");
                String value = kv[1].trim().replace("\"", "");
                data.put(key, value);
            }
        }
        return data;
    }
    
    private String devicesToJson(List<Device> devices) {
        StringBuilder json = new StringBuilder("[");
        for (int i = 0; i < devices.size(); i++) {
            if (i > 0) json.append(",");
            json.append(devices.get(i).toJson());
        }
        json.append("]");
        return json.toString();
    }
    
    /**
     * Genera el HTML del panel de control
     */
    private String generateDashboardHTML() {
        List<Device> devices = deviceService.findAll();
        
        StringBuilder html = new StringBuilder();
        html.append("<!DOCTYPE html><html><head>");
        html.append("<meta charset='UTF-8'>");
        html.append("<title>🏠 Smart Home Control</title>");
        html.append("<style>");
        html.append("* { box-sizing: border-box; margin: 0; padding: 0; }");
        html.append("body { font-family: 'Segoe UI', sans-serif; background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%); min-height: 100vh; color: white; padding: 20px; }");
        html.append(".header { text-align: center; padding: 20px; margin-bottom: 30px; }");
        html.append(".header h1 { font-size: 2.5em; margin-bottom: 10px; }");
        html.append(".stats { display: flex; justify-content: center; gap: 30px; margin-bottom: 30px; }");
        html.append(".stat { background: rgba(255,255,255,0.1); padding: 15px 30px; border-radius: 10px; text-align: center; }");
        html.append(".stat-value { font-size: 2em; font-weight: bold; color: #4ade80; }");
        html.append(".devices { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 20px; max-width: 1200px; margin: 0 auto; }");
        html.append(".device { background: rgba(255,255,255,0.05); border-radius: 15px; padding: 20px; border: 1px solid rgba(255,255,255,0.1); transition: all 0.3s; }");
        html.append(".device:hover { transform: translateY(-5px); box-shadow: 0 10px 30px rgba(0,0,0,0.3); }");
        html.append(".device-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 15px; }");
        html.append(".device-name { font-size: 1.2em; font-weight: 600; }");
        html.append(".device-type { font-size: 0.8em; background: rgba(255,255,255,0.1); padding: 3px 10px; border-radius: 20px; }");
        html.append(".device-room { color: #94a3b8; font-size: 0.9em; margin-bottom: 15px; }");
        html.append(".device-status { display: flex; align-items: center; gap: 10px; margin-bottom: 15px; }");
        html.append(".status-dot { width: 12px; height: 12px; border-radius: 50%; }");
        html.append(".status-on { background: #4ade80; box-shadow: 0 0 10px #4ade80; }");
        html.append(".status-off { background: #6b7280; }");
        html.append(".device-controls { display: flex; gap: 10px; }");
        html.append(".btn { padding: 8px 16px; border: none; border-radius: 8px; cursor: pointer; font-weight: 600; transition: all 0.2s; }");
        html.append(".btn-on { background: #4ade80; color: #000; }");
        html.append(".btn-off { background: #ef4444; color: white; }");
        html.append(".btn:hover { transform: scale(1.05); }");
        html.append(".value-display { background: rgba(255,255,255,0.1); padding: 5px 15px; border-radius: 5px; }");
        html.append(".color-preview { width: 30px; height: 30px; border-radius: 5px; border: 2px solid white; }");
        html.append(".icon { font-size: 1.5em; }");
        html.append("</style></head><body>");
        
        // Header
        html.append("<div class='header'>");
        html.append("<h1>🏠 Smart Home Control</h1>");
        html.append("<p>Panel de control en tiempo real</p>");
        html.append("</div>");
        
        // Stats
        long onCount = devices.stream().filter(Device::isStatus).count();
        html.append("<div class='stats'>");
        html.append("<div class='stat'><div class='stat-value'>").append(devices.size()).append("</div><div>Dispositivos</div></div>");
        html.append("<div class='stat'><div class='stat-value'>").append(onCount).append("</div><div>Encendidos</div></div>");
        html.append("<div class='stat'><div class='stat-value'>").append(devices.size() - onCount).append("</div><div>Apagados</div></div>");
        html.append("</div>");
        
        // Devices grid
        html.append("<div class='devices'>");
        
        for (Device d : devices) {
            String icon = getDeviceIcon(d.getType());
            String statusClass = d.isStatus() ? "status-on" : "status-off";
            String statusText = d.isStatus() ? "Encendido" : "Apagado";
            
            html.append("<div class='device'>");
            html.append("<div class='device-header'>");
            html.append("<span class='device-name'>").append(icon).append(" ").append(d.getName()).append("</span>");
            html.append("<span class='device-type'>").append(d.getType()).append("</span>");
            html.append("</div>");
            html.append("<div class='device-room'>📍 ").append(d.getRoom()).append("</div>");
            html.append("<div class='device-status'>");
            html.append("<span class='status-dot ").append(statusClass).append("'></span>");
            html.append("<span>").append(statusText).append("</span>");
            
            if (d.getValue() > 0) {
                html.append("<span class='value-display'>").append(d.getValue());
                if ("thermostat".equals(d.getType())) {
                    html.append("°C");
                } else {
                    html.append("%");
                }
                html.append("</span>");
            }
            
            if (d.getColor() != null && !d.getColor().isEmpty()) {
                html.append("<span class='color-preview' style='background:").append(d.getColor()).append("'></span>");
            }
            
            html.append("</div>");
            html.append("<div class='device-controls'>");
            html.append("<button class='btn btn-on' onclick='control(\"").append(d.getIdString()).append("\", \"ON\")'>ON</button>");
            html.append("<button class='btn btn-off' onclick='control(\"").append(d.getIdString()).append("\", \"OFF\")'>OFF</button>");
            html.append("</div>");
            html.append("</div>");
        }
        
        html.append("</div>");
        
        // JavaScript
        html.append("<script>");
        html.append("function control(id, cmd) {");
        html.append("  fetch('/api/control', {");
        html.append("    method: 'POST',");
        html.append("    headers: {'Content-Type': 'application/json'},");
        html.append("    body: JSON.stringify({deviceId: id, command: cmd})");
        html.append("  }).then(r => r.json()).then(d => { console.log(d); location.reload(); });");
        html.append("}");
        html.append("setTimeout(() => location.reload(), 5000);"); // Auto-refresh
        html.append("</script>");
        
        html.append("</body></html>");
        return html.toString();
    }
    
    private String getDeviceIcon(String type) {
        switch (type) {
            case "light": return "💡";
            case "thermostat": return "🌡️";
            case "door": return "🚪";
            case "camera": return "📹";
            case "sensor": return "📡";
            case "speaker": return "🔊";
            default: return "📱";
        }
    }
    
    public static void main(String[] args) {
        RestServer server = new RestServer();
        
        Runtime.getRuntime().addShutdownHook(new Thread(() -> {
            System.out.println("\n[WARN] Cerrando servidor...");
            server.stop();
        }));
        
        try {
            server.start();
        } catch (IOException e) {
            System.err.println("[ERROR] Error: " + e.getMessage());
            e.printStackTrace();
        }
    }
}
