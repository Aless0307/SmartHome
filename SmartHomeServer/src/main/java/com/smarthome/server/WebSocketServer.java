package com.smarthome.server;

import java.io.*;
import java.net.*;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.util.*;
import java.util.concurrent.*;
import com.smarthome.protocol.JsonMessage;

/**
 * ===============================================================
 * SERVIDOR WEBSOCKET - Smart Home
 * Bridge entre navegadores web y el sistema UDP de notificaciones
 * 
 * Este servidor:
 * - Acepta conexiones WebSocket de navegadores (puerto 5002)
 * - Se registra como cliente UDP para recibir broadcasts
 * - Reenvía los broadcasts UDP a todos los clientes WebSocket
 * 
 * Esto permite que los navegadores reciban actualizaciones en
 * tiempo real sin necesidad de polling.
 * ===============================================================
 */
public class WebSocketServer implements Runnable {
    
    private static final int WS_PORT = 5002;
    private static final String WEBSOCKET_GUID = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
    
    private ServerSocket serverSocket;
    private volatile boolean running = true;
    
    // Clientes WebSocket conectados
    private Set<WebSocketClient> clients = ConcurrentHashMap.newKeySet();
    
    // Cliente UDP para recibir broadcasts
    private DatagramSocket udpSocket;
    private static final int UDP_PORT = 5001;
    
    // Instancia singleton
    private static WebSocketServer instance;
    
    public WebSocketServer() {
        instance = this;
    }
    
    public static WebSocketServer getInstance() {
        return instance;
    }
    
    @Override
    public void run() {
        try {
            // Iniciar servidor WebSocket
            serverSocket = new ServerSocket(WS_PORT);
            System.out.println("  [WS] WebSocket Server escuchando en puerto: " + WS_PORT);
            
            // Iniciar hilo para escuchar UDP broadcasts
            startUdpListener();
            
            // Aceptar conexiones WebSocket
            while (running) {
                try {
                    Socket clientSocket = serverSocket.accept();
                    System.out.println("[WS] Nueva conexión desde: " + clientSocket.getInetAddress());
                    
                    // Manejar handshake y cliente en un hilo separado
                    WebSocketClient client = new WebSocketClient(clientSocket, this);
                    new Thread(client).start();
                    
                } catch (IOException e) {
                    if (running) {
                        System.err.println("[WS] Error aceptando conexión: " + e.getMessage());
                    }
                }
            }
            
        } catch (IOException e) {
            System.err.println("[WS] Error iniciando servidor: " + e.getMessage());
        }
    }
    
    /**
     * Inicia un listener UDP para recibir broadcasts del UdpServer
     */
    private void startUdpListener() {
        new Thread(() -> {
            try {
                // Crear socket UDP en puerto aleatorio
                udpSocket = new DatagramSocket();
                InetAddress serverAddr = InetAddress.getByName("127.0.0.1");
                
                // Registrarse con el servidor UDP
                String registerMsg = "{\"action\":\"REGISTER\"}";
                byte[] registerData = registerMsg.getBytes();
                DatagramPacket registerPacket = new DatagramPacket(
                    registerData, registerData.length, serverAddr, UDP_PORT);
                udpSocket.send(registerPacket);
                System.out.println("[WS] Registrado con servidor UDP para broadcasts");
                
                // Escuchar broadcasts
                byte[] buffer = new byte[4096];
                while (running) {
                    try {
                        DatagramPacket packet = new DatagramPacket(buffer, buffer.length);
                        udpSocket.receive(packet);
                        
                        String message = new String(packet.getData(), 0, packet.getLength());
                        System.out.println("[WS] UDP recibido: " + message.substring(0, Math.min(50, message.length())) + "...");
                        
                        // Reenviar a todos los clientes WebSocket
                        broadcastToWebSockets(message);
                        
                    } catch (IOException e) {
                        if (running) {
                            System.err.println("[WS] Error recibiendo UDP: " + e.getMessage());
                        }
                    }
                }
                
            } catch (IOException e) {
                System.err.println("[WS] Error en UDP listener: " + e.getMessage());
            }
        }).start();
    }
    
    /**
     * Registra un cliente WebSocket
     */
    public void registerClient(WebSocketClient client) {
        clients.add(client);
        System.out.println("[WS] Cliente registrado. Total: " + clients.size());
    }
    
    /**
     * Desregistra un cliente WebSocket
     */
    public void unregisterClient(WebSocketClient client) {
        clients.remove(client);
        System.out.println("[WS] Cliente desconectado. Total: " + clients.size());
    }
    
    /**
     * Envía un mensaje a todos los clientes WebSocket conectados
     */
    public void broadcastToWebSockets(String message) {
        if (clients.isEmpty()) {
            return;
        }
        
        int sent = 0;
        for (WebSocketClient client : clients) {
            try {
                client.sendMessage(message);
                sent++;
            } catch (Exception e) {
                System.err.println("[WS] Error enviando a cliente: " + e.getMessage());
            }
        }
        
        System.out.println("[WS] Broadcast enviado a " + sent + "/" + clients.size() + " clientes WebSocket");
    }
    
    /**
     * Broadcast directo desde el servidor (sin pasar por UDP)
     */
    public void broadcast(JsonMessage message) {
        broadcastToWebSockets(message.toString());
    }
    
    public void stop() {
        running = false;
        try {
            if (serverSocket != null) serverSocket.close();
            if (udpSocket != null) udpSocket.close();
        } catch (IOException e) {
            // Ignorar
        }
    }
    
    /**
     * ===============================================================
     * CLASE INTERNA: Cliente WebSocket individual
     * ===============================================================
     */
    public static class WebSocketClient implements Runnable {
        
        private Socket socket;
        private WebSocketServer server;
        private InputStream in;
        private OutputStream out;
        private volatile boolean connected = false;
        
        public WebSocketClient(Socket socket, WebSocketServer server) {
            this.socket = socket;
            this.server = server;
        }
        
        @Override
        public void run() {
            try {
                in = socket.getInputStream();
                out = socket.getOutputStream();
                
                // Realizar handshake WebSocket
                if (performHandshake()) {
                    connected = true;
                    server.registerClient(this);
                    
                    // Escuchar mensajes del cliente
                    listenForMessages();
                }
                
            } catch (Exception e) {
                System.err.println("[WS] Error en cliente: " + e.getMessage());
            } finally {
                disconnect();
            }
        }
        
        /**
         * Realiza el handshake HTTP -> WebSocket
         */
        private boolean performHandshake() throws IOException {
            BufferedReader reader = new BufferedReader(new InputStreamReader(in));
            
            String line;
            String websocketKey = null;
            
            // Leer headers HTTP
            while ((line = reader.readLine()) != null && !line.isEmpty()) {
                if (line.startsWith("Sec-WebSocket-Key:")) {
                    websocketKey = line.substring(19).trim();
                }
            }
            
            if (websocketKey == null) {
                System.err.println("[WS] No se encontró Sec-WebSocket-Key");
                return false;
            }
            
            // Generar accept key
            String acceptKey = generateAcceptKey(websocketKey);
            
            // Enviar respuesta de handshake
            String response = "HTTP/1.1 101 Switching Protocols\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Accept: " + acceptKey + "\r\n" +
                "\r\n";
            
            out.write(response.getBytes());
            out.flush();
            
            System.out.println("[WS] Handshake completado");
            return true;
        }
        
        /**
         * Genera la clave de aceptación WebSocket
         */
        private String generateAcceptKey(String key) {
            try {
                String concat = key + WEBSOCKET_GUID;
                MessageDigest sha1 = MessageDigest.getInstance("SHA-1");
                byte[] hash = sha1.digest(concat.getBytes(StandardCharsets.UTF_8));
                return Base64.getEncoder().encodeToString(hash);
            } catch (Exception e) {
                return "";
            }
        }
        
        /**
         * Escucha mensajes del cliente WebSocket
         */
        private void listenForMessages() {
            try {
                while (connected) {
                    // Leer frame WebSocket
                    int firstByte = in.read();
                    if (firstByte == -1) break;
                    
                    int secondByte = in.read();
                    if (secondByte == -1) break;
                    
                    boolean masked = (secondByte & 0x80) != 0;
                    int length = secondByte & 0x7F;
                    
                    // Leer longitud extendida si es necesario
                    if (length == 126) {
                        length = (in.read() << 8) | in.read();
                    } else if (length == 127) {
                        // No soportamos mensajes tan largos
                        length = 0;
                        for (int i = 0; i < 8; i++) in.read();
                    }
                    
                    // Leer mask key
                    byte[] maskKey = new byte[4];
                    if (masked) {
                        in.read(maskKey);
                    }
                    
                    // Leer payload
                    byte[] payload = new byte[length];
                    int read = 0;
                    while (read < length) {
                        int r = in.read(payload, read, length - read);
                        if (r == -1) break;
                        read += r;
                    }
                    
                    // Decodificar si está enmascarado
                    if (masked) {
                        for (int i = 0; i < payload.length; i++) {
                            payload[i] ^= maskKey[i % 4];
                        }
                    }
                    
                    String message = new String(payload, StandardCharsets.UTF_8);
                    
                    // Verificar si es close frame
                    int opcode = firstByte & 0x0F;
                    if (opcode == 0x8) {
                        // Close frame
                        break;
                    }
                    
                    // Procesar mensaje
                    if (!message.isEmpty()) {
                        processMessage(message);
                    }
                }
            } catch (IOException e) {
                // Conexión cerrada
            }
        }
        
        /**
         * Procesa un mensaje recibido del cliente
         */
        private void processMessage(String message) {
            try {
                JsonMessage json = JsonMessage.parse(message);
                String action = json.getString("action");
                
                if ("PING".equals(action)) {
                    sendMessage("{\"action\":\"PONG\",\"timestamp\":" + System.currentTimeMillis() + "}");
                }
                // Otros mensajes se pueden manejar aquí
                
            } catch (Exception e) {
                // Mensaje no JSON, ignorar
            }
        }
        
        /**
         * Envía un mensaje al cliente WebSocket
         */
        public synchronized void sendMessage(String message) throws IOException {
            if (!connected) return;
            
            byte[] payload = message.getBytes(StandardCharsets.UTF_8);
            int length = payload.length;
            
            // Construir frame WebSocket
            ByteArrayOutputStream frame = new ByteArrayOutputStream();
            
            // Primer byte: FIN + opcode (text)
            frame.write(0x81);
            
            // Segundo byte: longitud (sin mask para servidor)
            if (length <= 125) {
                frame.write(length);
            } else if (length <= 65535) {
                frame.write(126);
                frame.write((length >> 8) & 0xFF);
                frame.write(length & 0xFF);
            } else {
                frame.write(127);
                for (int i = 7; i >= 0; i--) {
                    frame.write((length >> (8 * i)) & 0xFF);
                }
            }
            
            // Payload
            frame.write(payload);
            
            out.write(frame.toByteArray());
            out.flush();
        }
        
        /**
         * Desconecta el cliente
         */
        public void disconnect() {
            connected = false;
            server.unregisterClient(this);
            try {
                socket.close();
            } catch (IOException e) {
                // Ignorar
            }
        }
    }
}
