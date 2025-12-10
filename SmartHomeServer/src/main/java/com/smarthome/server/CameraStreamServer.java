package com.smarthome.server;

import com.sun.net.httpserver.HttpServer;
import com.sun.net.httpserver.HttpHandler;
import com.sun.net.httpserver.HttpExchange;

import java.io.*;
import java.net.*;
import java.util.*;
import java.util.concurrent.*;

/**
 * ═══════════════════════════════════════════════════════════════
 * SERVIDOR DE STREAMING DE CÁMARAS - MJPEG sobre HTTP
 * ═══════════════════════════════════════════════════════════════
 * 
 * Recibe frames JPEG de Unity via TCP (HD) o UDP (legacy) y los 
 * transmite a clientes Java via HTTP MJPEG streaming.
 * 
 * Tecnologías:
 * - HTTP Server (REST/Streaming)
 * - TCP Socket (frames HD de Unity)
 * - UDP Socket (frames legacy de Unity)
 * - Multithreading (ThreadPool)
 * - Protocolo MJPEG
 */
public class CameraStreamServer {
    
    // Configuración
    private static final int HTTP_PORT = 8081;      // Puerto para streaming HTTP
    private static final int UDP_PORT = 8082;       // Puerto para recibir frames de Unity (legacy)
    private static final int TCP_PORT = 8083;       // Puerto TCP para frames HD de Unity
    private static final int MAX_FRAME_SIZE = 2000000; // 2MB máximo por frame (HD)
    
    // Servidor HTTP
    private HttpServer httpServer;
    
    // Socket UDP para recibir frames (legacy)
    private DatagramSocket udpSocket;
    
    // Socket TCP para recibir frames HD
    private ServerSocket tcpServerSocket;
    
    private volatile boolean running = false;
    
    // Almacén de frames por cámara (cameraId -> último frame JPEG)
    private ConcurrentHashMap<String, byte[]> cameraFrames = new ConcurrentHashMap<>();
    
    // Lista de clientes conectados por cámara
    private ConcurrentHashMap<String, List<OutputStream>> cameraClients = new ConcurrentHashMap<>();
    
    // Thread pool
    private ExecutorService executor = Executors.newCachedThreadPool();
    
    // Singleton
    private static CameraStreamServer instance;
    
    public static CameraStreamServer getInstance() {
        if (instance == null) {
            instance = new CameraStreamServer();
        }
        return instance;
    }
    
    /**
     * Iniciar servidor de streaming
     */
    public void start() {
        try {
            // Iniciar servidor HTTP para streaming
            httpServer = HttpServer.create(new InetSocketAddress(HTTP_PORT), 0);
            
            // Endpoint para stream de cámara específica: /camera/stream?id=cam_entrada
            httpServer.createContext("/camera/stream", new CameraStreamHandler());
            
            // Endpoint para obtener un solo frame: /camera/frame?id=cam_entrada
            httpServer.createContext("/camera/frame", new SingleFrameHandler());
            
            // Endpoint para listar cámaras disponibles
            httpServer.createContext("/camera/list", new CameraListHandler());
            
            // Endpoint de status
            httpServer.createContext("/camera/status", exchange -> {
                String response = "{\"status\":\"OK\",\"cameras\":" + cameraFrames.size() + "}";
                exchange.getResponseHeaders().set("Content-Type", "application/json");
                exchange.sendResponseHeaders(200, response.length());
                exchange.getResponseBody().write(response.getBytes());
                exchange.getResponseBody().close();
            });
            
            httpServer.setExecutor(executor);
            httpServer.start();
            
            System.out.println("  [CAMERA] Stream Server HTTP en puerto: " + HTTP_PORT);
            
            // Iniciar receptor UDP para frames de Unity (legacy, baja resolución)
            startUdpReceiver();
            
            // Iniciar receptor TCP para frames HD de Unity
            startTcpReceiver();
            
            running = true;
            
        } catch (Exception e) {
            System.err.println("[ERROR] Error iniciando CameraStreamServer: " + e.getMessage());
            e.printStackTrace();
        }
    }
    
    /**
     * Iniciar receptor UDP para frames de Unity
     */
    private void startUdpReceiver() {
        executor.submit(() -> {
            try {
                udpSocket = new DatagramSocket(UDP_PORT);
                System.out.println("  [UDP] Camera Receiver en puerto: " + UDP_PORT);
                
                byte[] buffer = new byte[MAX_FRAME_SIZE];
                
                while (running) {
                    try {
                        DatagramPacket packet = new DatagramPacket(buffer, buffer.length);
                        udpSocket.receive(packet);
                        
                        // Parsear el paquete: formato "CAMERA_ID:JPEG_DATA"
                        processFrame(packet.getData(), packet.getLength());
                        
                    } catch (SocketException e) {
                        if (running) {
                            System.err.println("UDP Socket error: " + e.getMessage());
                        }
                    }
                }
            } catch (Exception e) {
                System.err.println("[ERROR] Error en UDP receiver: " + e.getMessage());
            }
        });
    }
    
    /**
     * Procesar frame recibido de Unity
     * Formato: "cameraId|jpegBytes"
     */
    private void processFrame(byte[] data, int length) {
        try {
            // Buscar el separador "|"
            int separatorIndex = -1;
            for (int i = 0; i < Math.min(50, length); i++) {
                if (data[i] == '|') {
                    separatorIndex = i;
                    break;
                }
            }
            
            if (separatorIndex == -1) return;
            
            // Extraer cameraId
            String cameraId = new String(data, 0, separatorIndex);
            
            // Extraer JPEG data
            int jpegLength = length - separatorIndex - 1;
            byte[] jpegData = new byte[jpegLength];
            System.arraycopy(data, separatorIndex + 1, jpegData, 0, jpegLength);
            
            // Guardar frame
            cameraFrames.put(cameraId, jpegData);
            
            // Enviar a clientes conectados
            broadcastFrame(cameraId, jpegData);
            
        } catch (Exception e) {
            // Ignorar frames mal formados
        }
    }
    
    /**
     * Iniciar receptor TCP para frames HD de Unity
     * Protocolo: [4 bytes length][cameraId|jpegData]
     */
    private void startTcpReceiver() {
        executor.submit(() -> {
            try {
                tcpServerSocket = new ServerSocket(TCP_PORT);
                System.out.println("  [TCP] Camera Receiver HD en puerto: " + TCP_PORT);
                
                while (running) {
                    try {
                        Socket clientSocket = tcpServerSocket.accept();
                        System.out.println("[TCP] Unity camera conectada desde: " + 
                            clientSocket.getInetAddress());
                        
                        // Manejar cada conexión en un hilo separado
                        executor.submit(() -> handleTcpClient(clientSocket));
                        
                    } catch (SocketException e) {
                        if (running) {
                            System.err.println("TCP Accept error: " + e.getMessage());
                        }
                    }
                }
            } catch (Exception e) {
                System.err.println("[ERROR] Error en TCP receiver: " + e.getMessage());
            }
        });
    }
    
    /**
     * Manejar cliente TCP (Unity camera sender)
     * Protocolo: [4 bytes = frame length][frame data = cameraId|jpegBytes]
     */
    private void handleTcpClient(Socket socket) {
        try {
            socket.setTcpNoDelay(true);
            socket.setReceiveBufferSize(MAX_FRAME_SIZE);
            
            DataInputStream in = new DataInputStream(new BufferedInputStream(socket.getInputStream()));
            
            while (running && !socket.isClosed()) {
                try {
                    // Leer longitud del frame (4 bytes, big-endian)
                    int frameLength = in.readInt();
                    
                    if (frameLength <= 0 || frameLength > MAX_FRAME_SIZE) {
                        System.err.println("[ERROR] Frame length invalido: " + frameLength);
                        continue;
                    }
                    
                    // Leer datos del frame
                    byte[] frameData = new byte[frameLength];
                    in.readFully(frameData);
                    
                    // Procesar frame (mismo formato que UDP)
                    processFrame(frameData, frameLength);
                    
                } catch (EOFException e) {
                    // Cliente desconectado
                    break;
                } catch (IOException e) {
                    if (running) {
                        System.err.println("TCP read error: " + e.getMessage());
                    }
                    break;
                }
            }
        } catch (Exception e) {
            System.err.println("[ERROR] Error en TCP client handler: " + e.getMessage());
        } finally {
            try {
                socket.close();
            } catch (IOException e) {}
            System.out.println("[TCP] Unity camera desconectada");
        }
    }
    
    /**
     * Enviar frame a todos los clientes conectados a esta cámara
     */
    private void broadcastFrame(String cameraId, byte[] jpegData) {
        List<OutputStream> clients = cameraClients.get(cameraId);
        if (clients == null || clients.isEmpty()) return;
        
        // MJPEG boundary format
        String boundary = "--boundary\r\n";
        String header = "Content-Type: image/jpeg\r\n" +
                       "Content-Length: " + jpegData.length + "\r\n\r\n";
        
        List<OutputStream> toRemove = new ArrayList<>();
        
        for (OutputStream out : clients) {
            try {
                out.write(boundary.getBytes());
                out.write(header.getBytes());
                out.write(jpegData);
                out.write("\r\n".getBytes());
                out.flush();
            } catch (IOException e) {
                toRemove.add(out);
            }
        }
        
        // Remover clientes desconectados
        clients.removeAll(toRemove);
    }
    
    /**
     * Handler para streaming MJPEG continuo
     */
    private class CameraStreamHandler implements HttpHandler {
        @Override
        public void handle(HttpExchange exchange) throws IOException {
            // Obtener ID de cámara
            String query = exchange.getRequestURI().getQuery();
            String cameraId = getQueryParam(query, "id");
            
            if (cameraId == null) {
                sendError(exchange, 400, "Missing camera id parameter");
                return;
            }
            
            System.out.println("[STREAM] Cliente conectado: " + cameraId);
            
            // Headers para MJPEG streaming
            exchange.getResponseHeaders().set("Content-Type", "multipart/x-mixed-replace; boundary=boundary");
            exchange.getResponseHeaders().set("Cache-Control", "no-cache");
            exchange.getResponseHeaders().set("Connection", "keep-alive");
            exchange.getResponseHeaders().set("Access-Control-Allow-Origin", "*");
            exchange.sendResponseHeaders(200, 0);
            
            OutputStream out = exchange.getResponseBody();
            
            // Registrar cliente
            cameraClients.computeIfAbsent(cameraId, k -> 
                Collections.synchronizedList(new ArrayList<>())).add(out);
            
            // Mantener conexión abierta hasta que el cliente desconecte
            try {
                while (running) {
                    Thread.sleep(1000);
                    // El broadcasting se hace en processFrame()
                }
            } catch (Exception e) {
                // Cliente desconectado
            } finally {
                List<OutputStream> clients = cameraClients.get(cameraId);
                if (clients != null) {
                    clients.remove(out);
                }
                System.out.println("[STREAM] Cliente desconectado: " + cameraId);
            }
        }
    }
    
    /**
     * Handler para obtener un solo frame
     */
    private class SingleFrameHandler implements HttpHandler {
        @Override
        public void handle(HttpExchange exchange) throws IOException {
            String query = exchange.getRequestURI().getQuery();
            String cameraId = getQueryParam(query, "id");
            
            if (cameraId == null) {
                sendError(exchange, 400, "Missing camera id parameter");
                return;
            }
            
            byte[] frame = cameraFrames.get(cameraId);
            
            if (frame == null) {
                sendError(exchange, 404, "No frame available for camera: " + cameraId);
                return;
            }
            
            exchange.getResponseHeaders().set("Content-Type", "image/jpeg");
            exchange.getResponseHeaders().set("Access-Control-Allow-Origin", "*");
            exchange.sendResponseHeaders(200, frame.length);
            exchange.getResponseBody().write(frame);
            exchange.getResponseBody().close();
        }
    }
    
    /**
     * Handler para listar cámaras disponibles
     */
    private class CameraListHandler implements HttpHandler {
        @Override
        public void handle(HttpExchange exchange) throws IOException {
            StringBuilder json = new StringBuilder("{\"cameras\":[");
            boolean first = true;
            for (String cameraId : cameraFrames.keySet()) {
                if (!first) json.append(",");
                json.append("\"").append(cameraId).append("\"");
                first = false;
            }
            json.append("]}");
            
            String response = json.toString();
            exchange.getResponseHeaders().set("Content-Type", "application/json");
            exchange.getResponseHeaders().set("Access-Control-Allow-Origin", "*");
            exchange.sendResponseHeaders(200, response.length());
            exchange.getResponseBody().write(response.getBytes());
            exchange.getResponseBody().close();
        }
    }
    
    /**
     * Extraer parámetro de query string
     */
    private String getQueryParam(String query, String param) {
        if (query == null) return null;
        for (String pair : query.split("&")) {
            String[] kv = pair.split("=");
            if (kv.length == 2 && kv[0].equals(param)) {
                return kv[1];
            }
        }
        return null;
    }
    
    /**
     * Enviar error HTTP
     */
    private void sendError(HttpExchange exchange, int code, String message) throws IOException {
        String response = "{\"error\":\"" + message + "\"}";
        exchange.getResponseHeaders().set("Content-Type", "application/json");
        exchange.sendResponseHeaders(code, response.length());
        exchange.getResponseBody().write(response.getBytes());
        exchange.getResponseBody().close();
    }
    
    /**
     * Detener servidor
     */
    public void stop() {
        running = false;
        if (httpServer != null) {
            httpServer.stop(0);
        }
        if (udpSocket != null) {
            udpSocket.close();
        }
        if (tcpServerSocket != null) {
            try {
                tcpServerSocket.close();
            } catch (IOException e) {}
        }
        executor.shutdownNow();
    }
    
    /**
     * Obtener frame actual de una cámara (para uso interno)
     */
    public byte[] getFrame(String cameraId) {
        return cameraFrames.get(cameraId);
    }
    
    /**
     * Verificar si hay frames disponibles
     */
    public boolean hasFrames() {
        return !cameraFrames.isEmpty();
    }
    
    public int getHttpPort() {
        return HTTP_PORT;
    }
    
    public int getUdpPort() {
        return UDP_PORT;
    }
    
    public int getTcpPort() {
        return TCP_PORT;
    }
}
