package com.smarthome.client;

import javax.swing.*;
import javax.swing.border.*;
import java.awt.*;
import java.awt.event.*;
import java.util.*;
import java.util.List;

/**
 * ═══════════════════════════════════════════════════════════════
 * SMART HOME CLIENT - Interfaz Gráfica
 * Cliente Java con GUI para controlar dispositivos del hogar
 * ═══════════════════════════════════════════════════════════════
 */
public class SmartHomeClientGUI extends JFrame {
    
    // Conexión
    private TcpClient client;
    private String currentUser = null;
    private String currentToken = null;
    private String serverHost = "localhost";
    
    // Componentes de login
    private JPanel loginPanel;
    private JTextField hostField;
    private JTextField portField;
    private JTextField usernameField;
    private JPasswordField passwordField;
    private JButton connectButton;
    private JLabel statusLabel;
    
    // Componentes principales
    private JPanel mainPanel;
    private JPanel devicesPanel;
    private JComboBox<String> roomFilter;
    private JLabel userLabel;
    private JTextArea logArea;
    
    // Panel de cámaras
    private CameraViewerPanel cameraViewerPanel;
    private JDialog cameraDialog;
    
    // Datos
    private List<Map<String, String>> devices = new ArrayList<>();
    private List<String> rooms = new ArrayList<>();
    
    // Colores del tema
    private Color bgDark = new Color(26, 26, 46);
    private Color bgCard = new Color(30, 41, 59);
    private Color accent = new Color(74, 222, 128);
    private Color textPrimary = Color.WHITE;
    private Color textSecondary = new Color(148, 163, 184);
    
    public SmartHomeClientGUI() {
        setTitle("🏠 Smart Home Client");
        setDefaultCloseOperation(JFrame.EXIT_ON_CLOSE);
        setSize(1000, 700);
        setLocationRelativeTo(null);
        
        // Look and feel
        try {
            UIManager.setLookAndFeel(UIManager.getSystemLookAndFeelClassName());
        } catch (Exception e) {}
        
        // Crear paneles
        createLoginPanel();
        createMainPanel();
        
        // Mostrar login primero
        setContentPane(loginPanel);
        
        // Listener para cerrar
        addWindowListener(new WindowAdapter() {
            @Override
            public void windowClosing(WindowEvent e) {
                if (client != null) {
                    client.disconnect();
                }
            }
        });
    }
    
    /**
     * Panel de Login
     */
    private void createLoginPanel() {
        loginPanel = new JPanel(new GridBagLayout());
        loginPanel.setBackground(bgDark);
        
        JPanel formPanel = new JPanel();
        formPanel.setLayout(new BoxLayout(formPanel, BoxLayout.Y_AXIS));
        formPanel.setBackground(bgCard);
        formPanel.setBorder(BorderFactory.createCompoundBorder(
            BorderFactory.createLineBorder(new Color(255,255,255,30), 1),
            BorderFactory.createEmptyBorder(40, 50, 40, 50)
        ));
        
        // Título
        JLabel titleLabel = new JLabel("🏠 Smart Home");
        titleLabel.setFont(new Font("Segoe UI", Font.BOLD, 32));
        titleLabel.setForeground(textPrimary);
        titleLabel.setAlignmentX(Component.CENTER_ALIGNMENT);
        formPanel.add(titleLabel);
        
        JLabel subtitleLabel = new JLabel("Control del Hogar Inteligente");
        subtitleLabel.setFont(new Font("Segoe UI", Font.PLAIN, 14));
        subtitleLabel.setForeground(textSecondary);
        subtitleLabel.setAlignmentX(Component.CENTER_ALIGNMENT);
        formPanel.add(subtitleLabel);
        
        formPanel.add(Box.createVerticalStrut(30));
        
        // Servidor
        JPanel serverPanel = new JPanel(new FlowLayout(FlowLayout.CENTER));
        serverPanel.setBackground(bgCard);
        
        hostField = createStyledTextField("localhost", 12);
        portField = createStyledTextField("5000", 5);
        
        serverPanel.add(createLabel("Servidor:"));
        serverPanel.add(hostField);
        serverPanel.add(createLabel(":"));
        serverPanel.add(portField);
        formPanel.add(serverPanel);
        
        formPanel.add(Box.createVerticalStrut(15));
        
        // Usuario
        usernameField = createStyledTextField("admin", 20);
        JPanel userPanel = createFieldPanel("👤 Usuario:", usernameField);
        formPanel.add(userPanel);
        
        formPanel.add(Box.createVerticalStrut(10));
        
        // Contraseña
        passwordField = new JPasswordField("admin123", 20);
        styleTextField(passwordField);
        JPanel passPanel = createFieldPanel("🔒 Contraseña:", passwordField);
        formPanel.add(passPanel);
        
        formPanel.add(Box.createVerticalStrut(25));
        
        // Botón conectar
        connectButton = new JButton("Conectar");
        connectButton.setFont(new Font("Segoe UI", Font.BOLD, 14));
        connectButton.setBackground(accent);
        connectButton.setForeground(Color.BLACK);
        connectButton.setOpaque(true);
        connectButton.setFocusPainted(false);
        connectButton.setBorderPainted(false);
        connectButton.setContentAreaFilled(true);
        connectButton.setCursor(Cursor.getPredefinedCursor(Cursor.HAND_CURSOR));
        connectButton.setAlignmentX(Component.CENTER_ALIGNMENT);
        connectButton.setPreferredSize(new Dimension(200, 45));
        connectButton.setMinimumSize(new Dimension(200, 45));
        connectButton.setMaximumSize(new Dimension(200, 45));
        connectButton.addActionListener(e -> doConnect());
        formPanel.add(connectButton);
        
        formPanel.add(Box.createVerticalStrut(15));
        
        // Estado
        statusLabel = new JLabel(" ");
        statusLabel.setFont(new Font("Segoe UI", Font.PLAIN, 12));
        statusLabel.setForeground(textSecondary);
        statusLabel.setAlignmentX(Component.CENTER_ALIGNMENT);
        formPanel.add(statusLabel);
        
        loginPanel.add(formPanel);
    }
    
    /**
     * Panel Principal
     */
    private void createMainPanel() {
        mainPanel = new JPanel(new BorderLayout());
        mainPanel.setBackground(bgDark);
        
        // Header
        JPanel headerPanel = new JPanel(new BorderLayout());
        headerPanel.setBackground(bgCard);
        headerPanel.setBorder(BorderFactory.createEmptyBorder(15, 20, 15, 20));
        
        JLabel logoLabel = new JLabel("🏠 Smart Home Control");
        logoLabel.setFont(new Font("Segoe UI", Font.BOLD, 20));
        logoLabel.setForeground(textPrimary);
        headerPanel.add(logoLabel, BorderLayout.WEST);
        
        JPanel headerRight = new JPanel(new FlowLayout(FlowLayout.RIGHT));
        headerRight.setBackground(bgCard);
        
        userLabel = new JLabel("👤 Usuario");
        userLabel.setForeground(textPrimary);
        headerRight.add(userLabel);
        
        JButton logoutButton = new JButton("Salir");
        logoutButton.addActionListener(e -> doLogout());
        headerRight.add(logoutButton);
        
        headerPanel.add(headerRight, BorderLayout.EAST);
        mainPanel.add(headerPanel, BorderLayout.NORTH);
        
        // Centro - Dispositivos
        JPanel centerPanel = new JPanel(new BorderLayout());
        centerPanel.setBackground(bgDark);
        centerPanel.setBorder(BorderFactory.createEmptyBorder(20, 20, 20, 20));
        
        // Filtros
        JPanel filterPanel = new JPanel(new FlowLayout(FlowLayout.LEFT));
        filterPanel.setBackground(bgDark);
        
        JLabel filterLabel = new JLabel("Habitación:");
        filterLabel.setForeground(textPrimary);
        filterPanel.add(filterLabel);
        
        roomFilter = new JComboBox<>();
        roomFilter.addItem("Todas");
        roomFilter.addActionListener(e -> filterDevices());
        filterPanel.add(roomFilter);
        
        JButton refreshButton = new JButton("🔄 Actualizar");
        refreshButton.addActionListener(e -> {
            if (client != null && client.isConnected()) {
                client.getDevices();
            }
        });
        filterPanel.add(refreshButton);
        
        centerPanel.add(filterPanel, BorderLayout.NORTH);
        
        // Grid de dispositivos - usar BoxLayout para mejor control
        devicesPanel = new JPanel();
        devicesPanel.setLayout(new BoxLayout(devicesPanel, BoxLayout.Y_AXIS));
        devicesPanel.setBackground(bgDark);
        
        JScrollPane scrollPane = new JScrollPane(devicesPanel);
        scrollPane.setBorder(null);
        scrollPane.getViewport().setBackground(bgDark);
        // Aumentar velocidad del scroll con rueda del mouse
        scrollPane.getVerticalScrollBar().setUnitIncrement(25);
        scrollPane.getVerticalScrollBar().setBlockIncrement(100);
        // También aumentar velocidad de scroll horizontal por si acaso
        scrollPane.getHorizontalScrollBar().setUnitIncrement(25);
        centerPanel.add(scrollPane, BorderLayout.CENTER);
        
        mainPanel.add(centerPanel, BorderLayout.CENTER);
        
        // Log panel (abajo)
        JPanel logPanel = new JPanel(new BorderLayout());
        logPanel.setBackground(bgCard);
        logPanel.setBorder(BorderFactory.createEmptyBorder(10, 20, 10, 20));
        logPanel.setPreferredSize(new Dimension(0, 120));
        
        JLabel logLabel = new JLabel("📋 Log de eventos:");
        logLabel.setForeground(textSecondary);
        logPanel.add(logLabel, BorderLayout.NORTH);
        
        logArea = new JTextArea();
        logArea.setBackground(new Color(15, 23, 42));
        logArea.setForeground(accent);
        logArea.setFont(new Font("Consolas", Font.PLAIN, 11));
        logArea.setEditable(false);
        
        JScrollPane logScroll = new JScrollPane(logArea);
        logScroll.setBorder(null);
        logPanel.add(logScroll, BorderLayout.CENTER);
        
        mainPanel.add(logPanel, BorderLayout.SOUTH);
    }
    
    /**
     * Conectar al servidor
     */
    private void doConnect() {
        String host = hostField.getText().trim();
        serverHost = host; // Guardar para el visor de cámaras
        int port = Integer.parseInt(portField.getText().trim());
        String username = usernameField.getText().trim();
        String password = new String(passwordField.getPassword());
        
        statusLabel.setText("Conectando...");
        statusLabel.setForeground(Color.YELLOW);
        connectButton.setEnabled(false);
        
        // Conectar en otro hilo
        new Thread(() -> {
            client = new TcpClient(host, port);
            client.setMessageListener(this::handleMessage);
            
            if (client.connect()) {
                // Esperar mensaje de bienvenida y hacer login
                try { Thread.sleep(500); } catch (Exception e) {}
                client.login(username, password);
            } else {
                SwingUtilities.invokeLater(() -> {
                    statusLabel.setText("❌ Error de conexión");
                    statusLabel.setForeground(Color.RED);
                    connectButton.setEnabled(true);
                });
            }
        }).start();
    }
    
    /**
     * Manejar mensajes del servidor
     */
    private void handleMessage(String json) {
        log("← " + json);
        
        try {
            Map<String, String> data = parseJsonSimple(json);
            String action = data.get("action");
            String status = data.get("status");
            
            if (action == null) return;
            
            switch (action) {
                case "CONNECTED":
                    log("✅ Conectado al servidor");
                    break;
                    
                case "LOGIN_SUCCESS":
                    currentUser = data.get("username");
                    currentToken = data.get("token");
                    log("✅ Login exitoso: " + currentUser);
                    
                    SwingUtilities.invokeLater(() -> {
                        userLabel.setText("👤 " + currentUser + " (" + data.get("role") + ")");
                        setContentPane(mainPanel);
                        revalidate();
                        repaint();
                    });
                    
                    // Obtener datos iniciales
                    client.getRooms();
                    client.getDevices();
                    break;
                    
                case "LOGIN_FAILED":
                    log("❌ Login fallido");
                    SwingUtilities.invokeLater(() -> {
                        statusLabel.setText("❌ Usuario o contraseña incorrectos");
                        statusLabel.setForeground(Color.RED);
                        connectButton.setEnabled(true);
                    });
                    break;
                    
                case "ROOMS_LIST":
                    String roomsStr = data.get("rooms");
                    parseRooms(roomsStr);
                    break;
                    
                case "DEVICES_LIST":
                    // Extraer devices directamente del JSON original
                    String devicesStr = extractDevicesFromJson(json);
                    parseDevices(devicesStr);
                    break;
                    
                case "DEVICE_UPDATED":
                case "DEVICE_CHANGED":
                    String changedBy = data.get("changedBy");
                    if (changedBy != null && !changedBy.equals(currentUser)) {
                        log("📢 Dispositivo cambiado por: " + changedBy);
                        // Solo recargar si el cambio fue de OTRO usuario
                        // Para evitar destruir streams de cámara activos
                        client.getDevices();
                    }
                    // Si el cambio fue del usuario actual, NO recargar
                    // Los botones ya actualizaron la UI localmente
                    break;
                    
                case "AUTH_REQUIRED":
                    log("⚠️ Autenticación requerida");
                    break;
            }
            
        } catch (Exception e) {
            log("Error parseando: " + e.getMessage());
        }
    }
    
    /**
     * Parsear lista de habitaciones
     */
    private void parseRooms(String roomsJson) {
        rooms.clear();
        if (roomsJson == null) return;
        
        // Formato: ["sala","cocina",...]
        roomsJson = roomsJson.replace("[", "").replace("]", "").replace("\"", "");
        String[] parts = roomsJson.split(",");
        for (String room : parts) {
            if (!room.trim().isEmpty()) {
                rooms.add(room.trim());
            }
        }
        
        SwingUtilities.invokeLater(() -> {
            roomFilter.removeAllItems();
            roomFilter.addItem("Todas");
            for (String room : rooms) {
                roomFilter.addItem(room);
            }
        });
    }
    
    /**
     * Extraer el array de devices directamente del JSON
     */
    private String extractDevicesFromJson(String json) {
        // Buscar "devices":"[...
        int start = json.indexOf("\"devices\":\"");
        if (start == -1) {
            start = json.indexOf("\"devices\": \"");
            if (start == -1) return "";
            start += 12;
        } else {
            start += 11;
        }
        
        // Encontrar el cierre - buscar ]" considerando que puede haber ] dentro de strings
        int end = start;
        int brackets = 0;
        boolean foundStart = false;
        
        for (int i = start; i < json.length(); i++) {
            char c = json.charAt(i);
            
            if (c == '[' || (c == '\\' && i+1 < json.length() && json.charAt(i+1) == '[')) {
                if (c == '\\') i++;
                brackets++;
                foundStart = true;
            } else if (c == ']' || (c == '\\' && i+1 < json.length() && json.charAt(i+1) == ']')) {
                if (c == '\\') i++;
                brackets--;
                if (foundStart && brackets == 0) {
                    end = i + 1;
                    break;
                }
            }
        }
        
        if (end > start) {
            String result = json.substring(start, end);
            // Limpiar escapes
            result = result.replace("\\\"", "\"");
            result = result.replace("\\\\", "\\");
            return result;
        }
        
        return "";
    }
    
    /**
     * Parsear lista de dispositivos
     */
    private void parseDevices(String devicesJson) {
        devices.clear();
        if (devicesJson == null || devicesJson.isEmpty()) return;
        
        // El JSON viene como "[{...},{...}]" con escapes - primero limpiar
        // Remover comillas externas si las hay
        devicesJson = devicesJson.trim();
        if (devicesJson.startsWith("\"") && devicesJson.endsWith("\"")) {
            devicesJson = devicesJson.substring(1, devicesJson.length() - 1);
        }
        
        // Limpiar escapes
        devicesJson = devicesJson.replace("\\\"", "\"");
        devicesJson = devicesJson.replace("\\\\", "\\");
        devicesJson = devicesJson.trim();
        
        // Quitar corchetes externos
        if (devicesJson.startsWith("[")) devicesJson = devicesJson.substring(1);
        if (devicesJson.endsWith("]")) devicesJson = devicesJson.substring(0, devicesJson.length() - 1);
        
        // Ahora tenemos: {device1},{device2},...
        // Dividir manualmente por los objetos
        List<String> deviceJsons = new ArrayList<>();
        int depth = 0;
        StringBuilder current = new StringBuilder();
        
        for (int i = 0; i < devicesJson.length(); i++) {
            char c = devicesJson.charAt(i);
            
            if (c == '{') {
                depth++;
                current.append(c);
            } else if (c == '}') {
                depth--;
                current.append(c);
                if (depth == 0) {
                    deviceJsons.add(current.toString().trim());
                    current = new StringBuilder();
                }
            } else if (depth > 0) {
                current.append(c);
            }
            // Ignorar comas y espacios fuera de objetos
        }
        
        // Parsear cada dispositivo
        for (String deviceJson : deviceJsons) {
            if (!deviceJson.isEmpty()) {
                Map<String, String> deviceData = parseDeviceJson(deviceJson);
                if (deviceData.containsKey("id") && deviceData.containsKey("name")) {
                    devices.add(deviceData);
                }
            }
        }
        
        log("Dispositivos parseados: " + devices.size());
        SwingUtilities.invokeLater(this::updateDevicesPanel);
    }
    
    /**
     * Parsear un dispositivo individual
     */
    private Map<String, String> parseDeviceJson(String json) {
        Map<String, String> data = new HashMap<>();
        if (json == null || json.isEmpty()) return data;
        
        json = json.trim();
        if (json.startsWith("{")) json = json.substring(1);
        if (json.endsWith("}")) json = json.substring(0, json.length() - 1);
        
        // Formato: "key":"value","key2":"value2" o "key":number
        String[] parts = json.split(",(?=\\s*\")");
        
        for (String part : parts) {
            part = part.trim();
            int colonIdx = part.indexOf(':');
            if (colonIdx > 0) {
                String key = part.substring(0, colonIdx).trim().replace("\"", "");
                String value = part.substring(colonIdx + 1).trim();
                // Quitar comillas del valor si las tiene
                if (value.startsWith("\"") && value.endsWith("\"")) {
                    value = value.substring(1, value.length() - 1);
                }
                data.put(key, value);
            }
        }
        
        return data;
    }
    
    /**
     * Actualizar panel de dispositivos
     */
    private void updateDevicesPanel() {
        devicesPanel.removeAll();
        
        String selectedRoom = (String) roomFilter.getSelectedItem();
        
        // Separar por tipo de dispositivo
        List<Map<String, String>> climas = new ArrayList<>();
        List<Map<String, String>> luces = new ArrayList<>();
        List<Map<String, String>> speakers = new ArrayList<>();
        List<Map<String, String>> cameras = new ArrayList<>();
        List<Map<String, String>> otrosDispositivos = new ArrayList<>();
        
        for (Map<String, String> device : devices) {
            String type = device.get("type");
            if ("ac".equals(type)) {
                climas.add(device);
            } else if ("light".equals(type)) {
                luces.add(device);
            } else if ("speaker".equals(type)) {
                speakers.add(device);
            } else if ("camera".equals(type)) {
                cameras.add(device);
            } else {
                otrosDispositivos.add(device);
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // SECCIÓN DE OTROS DISPOSITIVOS (Portón, TV, Lavadora) - Compactos en una fila
        // ═══════════════════════════════════════════════════════════
        List<Map<String, String>> otrosFiltrados = new ArrayList<>();
        for (Map<String, String> device : otrosDispositivos) {
            String room = device.get("room");
            if (selectedRoom == null || selectedRoom.equals("Todas") || selectedRoom.equals(room)) {
                otrosFiltrados.add(device);
            }
        }
        
        if (!otrosFiltrados.isEmpty()) {
            // Título de sección
            JPanel otrosHeader = new JPanel(new FlowLayout(FlowLayout.LEFT));
            otrosHeader.setBackground(bgDark);
            otrosHeader.setMaximumSize(new Dimension(Integer.MAX_VALUE, 35));
            JLabel otrosTitle = new JLabel("🏠 DISPOSITIVOS");
            otrosTitle.setFont(new Font("Segoe UI", Font.BOLD, 14));
            otrosTitle.setForeground(accent);
            otrosHeader.add(otrosTitle);
            devicesPanel.add(otrosHeader);
            
            // Panel grid para dispositivos compactos (3 columnas)
            JPanel otrosGrid = new JPanel(new GridLayout(0, 3, 10, 10));
            otrosGrid.setBackground(bgDark);
            otrosGrid.setBorder(BorderFactory.createEmptyBorder(5, 0, 10, 0));
            otrosGrid.setMaximumSize(new Dimension(Integer.MAX_VALUE, 130));
            
            for (Map<String, String> device : otrosFiltrados) {
                otrosGrid.add(createCompactDeviceCard(device));
            }
            
            devicesPanel.add(otrosGrid);
        }
        
        // ═══════════════════════════════════════════════════════════
        // SECCIÓN DE LUCES
        // ═══════════════════════════════════════════════════════════
        if (!luces.isEmpty() && (selectedRoom == null || selectedRoom.equals("Todas"))) {
            // Título de sección
            JPanel luzHeader = new JPanel(new FlowLayout(FlowLayout.LEFT));
            luzHeader.setBackground(bgDark);
            luzHeader.setMaximumSize(new Dimension(Integer.MAX_VALUE, 35));
            JLabel luzTitle = new JLabel("💡 LUCES");
            luzTitle.setFont(new Font("Segoe UI", Font.BOLD, 14));
            luzTitle.setForeground(new Color(250, 204, 21)); // Amarillo
            luzHeader.add(luzTitle);
            devicesPanel.add(luzHeader);
            
            // Panel para luces en grid 2x4
            JPanel lucesGrid = new JPanel(new GridLayout(0, 2, 10, 10));
            lucesGrid.setBackground(bgDark);
            lucesGrid.setBorder(BorderFactory.createEmptyBorder(5, 0, 10, 0));
            
            for (Map<String, String> luz : luces) {
                lucesGrid.add(createLuzCard(luz));
            }
            
            // Wrapper para que el grid no se expanda infinitamente
            JPanel lucesWrapper = new JPanel(new BorderLayout());
            lucesWrapper.setBackground(bgDark);
            lucesWrapper.add(lucesGrid, BorderLayout.NORTH);
            devicesPanel.add(lucesWrapper);
        } else if (!luces.isEmpty()) {
            // Si hay filtro de habitación, mostrar solo luces de esa habitación
            JPanel lucesGrid = new JPanel(new GridLayout(0, 2, 10, 10));
            lucesGrid.setBackground(bgDark);
            for (Map<String, String> luz : luces) {
                String room = luz.get("room");
                if (selectedRoom != null && selectedRoom.equals(room)) {
                    lucesGrid.add(createLuzCard(luz));
                }
            }
            if (lucesGrid.getComponentCount() > 0) {
                devicesPanel.add(lucesGrid);
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // SECCIÓN DE SPEAKERS (Echo Dot, Alexa, etc)
        // ═══════════════════════════════════════════════════════════
        if (!speakers.isEmpty() && (selectedRoom == null || selectedRoom.equals("Todas"))) {
            // Título de sección
            JPanel speakerHeader = new JPanel(new FlowLayout(FlowLayout.LEFT));
            speakerHeader.setBackground(bgDark);
            speakerHeader.setMaximumSize(new Dimension(Integer.MAX_VALUE, 35));
            JLabel speakerTitle = new JLabel("🔊 BOCINAS INTELIGENTES");
            speakerTitle.setFont(new Font("Segoe UI", Font.BOLD, 14));
            speakerTitle.setForeground(new Color(0, 188, 212)); // Cyan Alexa
            speakerHeader.add(speakerTitle);
            devicesPanel.add(speakerHeader);
            
            // Panel para speakers
            JPanel speakersPanel = new JPanel(new GridLayout(0, 1, 10, 10));
            speakersPanel.setBackground(bgDark);
            speakersPanel.setBorder(BorderFactory.createEmptyBorder(5, 0, 10, 0));
            
            for (Map<String, String> speaker : speakers) {
                speakersPanel.add(createSpeakerCard(speaker));
            }
            
            // Wrapper
            JPanel speakersWrapper = new JPanel(new BorderLayout());
            speakersWrapper.setBackground(bgDark);
            speakersWrapper.add(speakersPanel, BorderLayout.NORTH);
            devicesPanel.add(speakersWrapper);
        }
        
        // ═══════════════════════════════════════════════════════════
        // SECCIÓN DE CLIMAS
        // ═══════════════════════════════════════════════════════════
        if (!climas.isEmpty() && (selectedRoom == null || selectedRoom.equals("Todas"))) {
            // Título de sección
            JPanel climaHeader = new JPanel(new FlowLayout(FlowLayout.LEFT));
            climaHeader.setBackground(bgDark);
            climaHeader.setMaximumSize(new Dimension(Integer.MAX_VALUE, 35));
            JLabel climaTitle = new JLabel("❄️ CLIMAS");
            climaTitle.setFont(new Font("Segoe UI", Font.BOLD, 14));
            climaTitle.setForeground(new Color(56, 189, 248)); // Azul claro
            climaHeader.add(climaTitle);
            devicesPanel.add(climaHeader);
            
            // Panel para climas en grid 3x2
            JPanel climasGrid = new JPanel(new GridLayout(0, 3, 10, 10));
            climasGrid.setBackground(bgDark);
            climasGrid.setBorder(BorderFactory.createEmptyBorder(5, 0, 10, 0));
            
            for (Map<String, String> clima : climas) {
                climasGrid.add(createClimaCard(clima));
            }
            
            // Wrapper para que el grid no se expanda infinitamente
            JPanel climasWrapper = new JPanel(new BorderLayout());
            climasWrapper.setBackground(bgDark);
            climasWrapper.add(climasGrid, BorderLayout.NORTH);
            devicesPanel.add(climasWrapper);
        } else if (!climas.isEmpty()) {
            // Si hay filtro de habitación, mostrar solo climas de esa habitación
            JPanel climasGrid = new JPanel(new GridLayout(0, 3, 10, 10));
            climasGrid.setBackground(bgDark);
            for (Map<String, String> clima : climas) {
                String room = clima.get("room");
                if (selectedRoom != null && selectedRoom.equals(room)) {
                    climasGrid.add(createClimaCard(clima));
                }
            }
            if (climasGrid.getComponentCount() > 0) {
                devicesPanel.add(climasGrid);
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // SECCIÓN DE CÁMARAS DE SEGURIDAD
        // ═══════════════════════════════════════════════════════════
        if (!cameras.isEmpty() && (selectedRoom == null || selectedRoom.equals("Todas"))) {
            // Título de sección
            JPanel cameraHeader = new JPanel(new FlowLayout(FlowLayout.LEFT));
            cameraHeader.setBackground(bgDark);
            cameraHeader.setMaximumSize(new Dimension(Integer.MAX_VALUE, 35));
            JLabel cameraTitle = new JLabel("📹 CÁMARAS DE SEGURIDAD");
            cameraTitle.setFont(new Font("Segoe UI", Font.BOLD, 14));
            cameraTitle.setForeground(new Color(168, 85, 247)); // Púrpura
            cameraHeader.add(cameraTitle);
            devicesPanel.add(cameraHeader);
            
            // Panel para cámaras en grid 3 columnas
            JPanel camerasGrid = new JPanel(new GridLayout(0, 3, 10, 10));
            camerasGrid.setBackground(bgDark);
            camerasGrid.setBorder(BorderFactory.createEmptyBorder(5, 0, 10, 0));
            
            for (Map<String, String> camera : cameras) {
                camerasGrid.add(createCameraCard(camera));
            }
            
            // Wrapper
            JPanel camerasWrapper = new JPanel(new BorderLayout());
            camerasWrapper.setBackground(bgDark);
            camerasWrapper.add(camerasGrid, BorderLayout.NORTH);
            devicesPanel.add(camerasWrapper);
        } else if (!cameras.isEmpty()) {
            // Si hay filtro de habitación, mostrar solo cámaras de esa habitación
            JPanel camerasGrid = new JPanel(new GridLayout(0, 3, 10, 10));
            camerasGrid.setBackground(bgDark);
            for (Map<String, String> camera : cameras) {
                String room = camera.get("room");
                if (selectedRoom != null && selectedRoom.equals(room)) {
                    camerasGrid.add(createCameraCard(camera));
                }
            }
            if (camerasGrid.getComponentCount() > 0) {
                devicesPanel.add(camerasGrid);
            }
        }
        
        // Agregar espacio flexible al final para que no se estiren los componentes
        devicesPanel.add(Box.createVerticalGlue());
        
        devicesPanel.revalidate();
        devicesPanel.repaint();
    }
    
    /**
     * Crear tarjeta compacta para dispositivo general (portón, TV, lavadora)
     */
    private JPanel createCompactDeviceCard(Map<String, String> device) {
        String id = device.get("id");
        String name = device.get("name");
        String type = device.get("type");
        boolean status = "true".equals(device.get("status"));
        
        // Para puertas: status=true significa cerrado
        boolean isDoor = "door".equals(type);
        boolean isTV = "tv".equals(type);
        boolean isAppliance = "appliance".equals(type);
        boolean displayStatus = isDoor ? !status : status;
        
        JPanel card = new JPanel();
        card.setLayout(new BoxLayout(card, BoxLayout.Y_AXIS));
        card.setBackground(bgCard);
        card.setBorder(BorderFactory.createCompoundBorder(
            BorderFactory.createLineBorder(new Color(255,255,255,20), 1),
            BorderFactory.createEmptyBorder(8, 10, 8, 10)
        ));
        card.setMaximumSize(new Dimension(Integer.MAX_VALUE, 100));
        
        // Icono y nombre
        String icon = getDeviceIcon(type);
        JLabel nameLabel = new JLabel(icon + " " + name);
        nameLabel.setFont(new Font("Segoe UI", Font.BOLD, 12));
        nameLabel.setForeground(textPrimary);
        nameLabel.setAlignmentX(Component.LEFT_ALIGNMENT);
        card.add(nameLabel);
        
        // Estado compacto
        String statusOn = isDoor ? "Abierto" : (isTV ? "Mostrada" : "Encendido");
        String statusOff = isDoor ? "Cerrado" : (isTV ? "Escondida" : "Apagado");
        JLabel statusLabel = new JLabel("● " + (displayStatus ? statusOn : statusOff));
        statusLabel.setFont(new Font("Segoe UI", Font.PLAIN, 10));
        statusLabel.setForeground(displayStatus ? accent : Color.GRAY);
        statusLabel.setAlignmentX(Component.LEFT_ALIGNMENT);
        card.add(statusLabel);
        
        card.add(Box.createVerticalStrut(5));
        
        // Botones compactos
        JPanel btnPanel = new JPanel(new FlowLayout(FlowLayout.LEFT, 3, 0));
        btnPanel.setBackground(bgCard);
        btnPanel.setAlignmentX(Component.LEFT_ALIGNMENT);
        
        String onLabel = isDoor ? "Abrir" : (isTV ? "Esconder" : (isAppliance ? "ON" : "ON"));
        String offLabel = isDoor ? "Cerrar" : (isTV ? "Mostrar" : (isAppliance ? "OFF" : "OFF"));
        
        JButton onBtn = new JButton(onLabel);
        onBtn.setFont(new Font("Segoe UI", Font.BOLD, 9));
        onBtn.setPreferredSize(new Dimension(55, 22));
        onBtn.setBackground(accent);
        onBtn.setForeground(Color.BLACK);
        onBtn.setFocusPainted(false);
        onBtn.addActionListener(e -> {
            client.controlDevice(id, (isDoor || isTV) ? "OFF" : "ON");
        });
        btnPanel.add(onBtn);
        
        JButton offBtn = new JButton(offLabel);
        offBtn.setFont(new Font("Segoe UI", Font.BOLD, 9));
        offBtn.setPreferredSize(new Dimension(55, 22));
        offBtn.setBackground(new Color(239, 68, 68));
        offBtn.setForeground(Color.WHITE);
        offBtn.setFocusPainted(false);
        offBtn.addActionListener(e -> {
            client.controlDevice(id, (isDoor || isTV) ? "ON" : "OFF");
        });
        btnPanel.add(offBtn);
        
        card.add(btnPanel);
        
        return card;
    }
    
    /**
     * Crear tarjeta especial para luz con controles de brillo y color
     */
    private JPanel createLuzCard(Map<String, String> device) {
        String id = device.get("id");
        String name = device.get("name");
        String room = device.get("room");
        boolean status = "true".equals(device.get("status"));
        String valueStr = device.get("value");
        String color = device.get("color");
        int value = 3000; // default - intensidad estándar en Unity
        try { 
            if (valueStr != null && !valueStr.equals("0")) {
                value = Integer.parseInt(valueStr);
            }
        } catch (Exception e) {}
        // Clampear el valor para que esté dentro del rango del slider (0-6000)
        value = Math.max(0, Math.min(6000, value));
        
        JPanel card = new JPanel();
        card.setLayout(new BoxLayout(card, BoxLayout.Y_AXIS));
        card.setBackground(new Color(45, 45, 30)); // Tono cálido para luces
        card.setBorder(BorderFactory.createCompoundBorder(
            BorderFactory.createLineBorder(new Color(250, 204, 21, 80), 1),
            BorderFactory.createEmptyBorder(12, 15, 12, 15)
        ));
        
        // Nombre corto
        String shortName = name.replace("Luz ", "");
        JLabel nameLabel = new JLabel("💡 " + shortName);
        nameLabel.setFont(new Font("Segoe UI", Font.BOLD, 13));
        nameLabel.setForeground(Color.WHITE);
        nameLabel.setAlignmentX(Component.LEFT_ALIGNMENT);
        card.add(nameLabel);
        
        // Habitación
        JLabel roomLabel = new JLabel(room);
        roomLabel.setFont(new Font("Segoe UI", Font.PLAIN, 10));
        roomLabel.setForeground(textSecondary);
        roomLabel.setAlignmentX(Component.LEFT_ALIGNMENT);
        card.add(roomLabel);
        
        card.add(Box.createVerticalStrut(6));
        
        // Estado y color actual
        JPanel statusPanel = new JPanel(new FlowLayout(FlowLayout.LEFT, 3, 0));
        statusPanel.setBackground(new Color(45, 45, 30));
        statusPanel.setAlignmentX(Component.LEFT_ALIGNMENT);
        
        JLabel statusDot = new JLabel("●");
        statusDot.setForeground(status ? new Color(250, 204, 21) : Color.GRAY);
        statusPanel.add(statusDot);
        
        JLabel statusText = new JLabel(status ? "Encendida" : "Apagada");
        statusText.setFont(new Font("Segoe UI", Font.PLAIN, 11));
        statusText.setForeground(textSecondary);
        statusPanel.add(statusText);
        
        // Mostrar color actual si hay
        if (color != null && !color.isEmpty()) {
            JLabel colorBox = new JLabel(" ■");
            colorBox.setFont(new Font("Segoe UI", Font.BOLD, 14));
            try {
                colorBox.setForeground(Color.decode(color));
            } catch (Exception e) {
                colorBox.setForeground(Color.WHITE);
            }
            statusPanel.add(colorBox);
        }
        
        card.add(statusPanel);
        card.add(Box.createVerticalStrut(8));
        
        // Botones ON/OFF
        JPanel btnPanel = new JPanel(new FlowLayout(FlowLayout.LEFT, 5, 0));
        btnPanel.setBackground(new Color(45, 45, 30));
        btnPanel.setAlignmentX(Component.LEFT_ALIGNMENT);
        
        JButton onBtn = new JButton("ON");
        onBtn.setFont(new Font("Segoe UI", Font.BOLD, 10));
        onBtn.setPreferredSize(new Dimension(45, 22));
        onBtn.setBackground(new Color(250, 204, 21));
        onBtn.setForeground(Color.BLACK);
        onBtn.setFocusPainted(false);
        onBtn.addActionListener(e -> {
            client.controlDevice(id, "ON");
            log("💡 Encendiendo: " + name);
        });
        btnPanel.add(onBtn);
        
        JButton offBtn = new JButton("OFF");
        offBtn.setFont(new Font("Segoe UI", Font.BOLD, 10));
        offBtn.setPreferredSize(new Dimension(45, 22));
        offBtn.setBackground(new Color(100, 100, 100));
        offBtn.setForeground(Color.WHITE);
        offBtn.setFocusPainted(false);
        offBtn.addActionListener(e -> {
            client.controlDevice(id, "OFF");
            log("💡 Apagando: " + name);
        });
        btnPanel.add(offBtn);
        
        card.add(btnPanel);
        card.add(Box.createVerticalStrut(8));
        
        // ═══════════════════════════════════════════════════════════
        // SLIDER DE BRILLO
        // ═══════════════════════════════════════════════════════════
        JPanel brilloPanel = new JPanel(new FlowLayout(FlowLayout.LEFT, 5, 0));
        brilloPanel.setBackground(new Color(45, 45, 30));
        brilloPanel.setAlignmentX(Component.LEFT_ALIGNMENT);
        
        JLabel brilloLabel = new JLabel("☀");
        brilloLabel.setForeground(new Color(250, 204, 21));
        brilloPanel.add(brilloLabel);
        
        JSlider brilloSlider = new JSlider(0, 6000, value);
        brilloSlider.setPreferredSize(new Dimension(80, 20));
        brilloSlider.setBackground(new Color(45, 45, 30));
        brilloSlider.setForeground(Color.WHITE);
        brilloSlider.setMajorTickSpacing(1500);
        
        // Campo de texto editable para intensidad
        JTextField brilloField = new JTextField(String.valueOf(value), 5);
        brilloField.setPreferredSize(new Dimension(50, 22));
        brilloField.setMaximumSize(new Dimension(50, 22));
        brilloField.setBackground(new Color(30, 30, 20));
        brilloField.setForeground(Color.WHITE);
        brilloField.setCaretColor(Color.WHITE);
        brilloField.setFont(new Font("Segoe UI", Font.PLAIN, 11));
        brilloField.setHorizontalAlignment(JTextField.CENTER);
        brilloField.setBorder(BorderFactory.createLineBorder(new Color(100, 100, 80), 1));
        
        brilloSlider.addChangeListener(e -> {
            int val = brilloSlider.getValue();
            brilloField.setText(String.valueOf(val));
            if (!brilloSlider.getValueIsAdjusting()) {
                client.controlDevice(id, "SET_VALUE", String.valueOf(val));
                log("💡 Intensidad " + name + ": " + val);
            }
        });
        
        // Al presionar Enter en el campo de texto
        brilloField.addActionListener(e -> {
            try {
                int val = Integer.parseInt(brilloField.getText().trim());
                val = Math.max(0, Math.min(6000, val));
                brilloSlider.setValue(val);
                brilloField.setText(String.valueOf(val));
                client.controlDevice(id, "SET_VALUE", String.valueOf(val));
                log("💡 Intensidad " + name + ": " + val);
            } catch (NumberFormatException ex) {
                brilloField.setText(String.valueOf(brilloSlider.getValue()));
            }
        });
        
        brilloPanel.add(brilloSlider);
        brilloPanel.add(brilloField);
        card.add(brilloPanel);
        
        card.add(Box.createVerticalStrut(6));
        
        // ═══════════════════════════════════════════════════════════
        // BOTONES DE COLORES PREDEFINIDOS
        // ═══════════════════════════════════════════════════════════
        JPanel colorPanel = new JPanel(new FlowLayout(FlowLayout.LEFT, 3, 0));
        colorPanel.setBackground(new Color(45, 45, 30));
        colorPanel.setAlignmentX(Component.LEFT_ALIGNMENT);
        
        // Colores predefinidos - más brillantes y visibles
        String[][] colores = {
            {"#FFFFFF", "Blanco"},
            {"#FFCC00", "Cálido"},
            {"#FF3333", "Rojo"},
            {"#00FFFF", "Cyan"},
            {"#3399FF", "Azul"},
            {"#33FF33", "Verde"},
            {"#FF66FF", "Rosa"}
        };
        
        for (String[] colorData : colores) {
            String hexColor = colorData[0];
            String colorName = colorData[1];
            
            JButton colorBtn = new JButton();
            colorBtn.setPreferredSize(new Dimension(22, 22));
            colorBtn.setBackground(Color.decode(hexColor));
            colorBtn.setOpaque(true);
            colorBtn.setContentAreaFilled(true);
            colorBtn.setBorderPainted(true);
            colorBtn.setBorder(BorderFactory.createCompoundBorder(
                BorderFactory.createLineBorder(Color.WHITE, 1),
                BorderFactory.createLineBorder(Color.decode(hexColor), 2)
            ));
            colorBtn.setFocusPainted(false);
            colorBtn.setToolTipText(colorName);
            colorBtn.addActionListener(e -> {
                client.setDeviceColor(id, hexColor);
                log("🎨 Color " + name + ": " + colorName);
            });
            colorPanel.add(colorBtn);
        }
        
        // Botón para selector de color personalizado
        JButton customColorBtn = new JButton("...");
        customColorBtn.setPreferredSize(new Dimension(25, 20));
        customColorBtn.setFont(new Font("Segoe UI", Font.BOLD, 9));
        customColorBtn.setBackground(new Color(100, 100, 100));
        customColorBtn.setForeground(Color.WHITE);
        customColorBtn.setFocusPainted(false);
        customColorBtn.setToolTipText("Color personalizado");
        customColorBtn.addActionListener(e -> {
            Color c = JColorChooser.showDialog(this, "Seleccionar color para " + name, Color.WHITE);
            if (c != null) {
                String hex = String.format("#%02x%02x%02x", c.getRed(), c.getGreen(), c.getBlue());
                client.setDeviceColor(id, hex);
                log("🎨 Color personalizado " + name + ": " + hex);
            }
        });
        colorPanel.add(customColorBtn);
        
        card.add(colorPanel);
        
        return card;
    }
    
    /**
     * Crear tarjeta para bocina inteligente (Echo Dot)
     */
    private JPanel createSpeakerCard(Map<String, String> device) {
        String id = device.get("id");
        String name = device.get("name");
        String room = device.get("room");
        boolean status = "true".equals(device.get("status")); // true = reproduciendo
        String valueStr = device.get("value");
        int volume = 80;
        try {
            if (valueStr != null) volume = Integer.parseInt(valueStr);
        } catch (Exception e) {}
        volume = Math.max(0, Math.min(100, volume));
        
        Color alexaCyan = new Color(0, 188, 212);
        Color bgSpeaker = new Color(20, 40, 50);
        
        JPanel card = new JPanel();
        card.setLayout(new BoxLayout(card, BoxLayout.Y_AXIS));
        card.setBackground(bgSpeaker);
        card.setBorder(BorderFactory.createCompoundBorder(
            BorderFactory.createLineBorder(alexaCyan.darker(), 1),
            BorderFactory.createEmptyBorder(15, 20, 15, 20)
        ));
        
        // ═══════════════════════════════════════════════════════════
        // ENCABEZADO
        // ═══════════════════════════════════════════════════════════
        JPanel headerPanel = new JPanel(new BorderLayout());
        headerPanel.setBackground(bgSpeaker);
        headerPanel.setAlignmentX(Component.LEFT_ALIGNMENT);
        
        JLabel nameLabel = new JLabel("🔊 " + name);
        nameLabel.setFont(new Font("Segoe UI", Font.BOLD, 16));
        nameLabel.setForeground(Color.WHITE);
        headerPanel.add(nameLabel, BorderLayout.WEST);
        
        // Estado
        JLabel statusLabel = new JLabel(status ? "▶️ Reproduciendo" : "⏹️ Detenido");
        statusLabel.setFont(new Font("Segoe UI", Font.PLAIN, 12));
        statusLabel.setForeground(status ? alexaCyan : Color.GRAY);
        headerPanel.add(statusLabel, BorderLayout.EAST);
        
        card.add(headerPanel);
        card.add(Box.createVerticalStrut(5));
        
        // Habitación
        JLabel roomLabel = new JLabel("📍 " + room);
        roomLabel.setFont(new Font("Segoe UI", Font.PLAIN, 11));
        roomLabel.setForeground(textSecondary);
        roomLabel.setAlignmentX(Component.LEFT_ALIGNMENT);
        card.add(roomLabel);
        
        card.add(Box.createVerticalStrut(12));
        
        // ═══════════════════════════════════════════════════════════
        // CONTROLES DE REPRODUCCIÓN
        // ═══════════════════════════════════════════════════════════
        JPanel controlPanel = new JPanel(new FlowLayout(FlowLayout.CENTER, 10, 0));
        controlPanel.setBackground(bgSpeaker);
        controlPanel.setAlignmentX(Component.LEFT_ALIGNMENT);
        
        // Botón Anterior
        JButton prevBtn = new JButton("⏮");
        prevBtn.setFont(new Font("Segoe UI", Font.BOLD, 14));
        prevBtn.setPreferredSize(new Dimension(45, 35));
        prevBtn.setBackground(new Color(60, 60, 60));
        prevBtn.setForeground(Color.WHITE);
        prevBtn.setFocusPainted(false);
        prevBtn.setToolTipText("Anterior");
        prevBtn.addActionListener(e -> {
            client.sendSpeakerCommand(id, "PREV");
            log("🔊 " + name + ": ⏮ Anterior");
        });
        controlPanel.add(prevBtn);
        
        // Botón Play/Pause (grande)
        JButton playBtn = new JButton(status ? "⏸" : "▶");
        playBtn.setFont(new Font("Segoe UI", Font.BOLD, 18));
        playBtn.setPreferredSize(new Dimension(60, 40));
        playBtn.setBackground(status ? new Color(239, 68, 68) : alexaCyan);
        playBtn.setForeground(Color.WHITE);
        playBtn.setFocusPainted(false);
        playBtn.setToolTipText(status ? "Pausar" : "Reproducir");
        playBtn.addActionListener(e -> {
            if (status) {
                client.controlDevice(id, "OFF");
                log("🔊 " + name + ": ⏸ Pausar");
            } else {
                client.controlDevice(id, "ON");
                log("🔊 " + name + ": ▶ Reproducir");
            }
        });
        controlPanel.add(playBtn);
        
        // Botón Siguiente
        JButton nextBtn = new JButton("⏭");
        nextBtn.setFont(new Font("Segoe UI", Font.BOLD, 14));
        nextBtn.setPreferredSize(new Dimension(45, 35));
        nextBtn.setBackground(new Color(60, 60, 60));
        nextBtn.setForeground(Color.WHITE);
        nextBtn.setFocusPainted(false);
        nextBtn.setToolTipText("Siguiente");
        nextBtn.addActionListener(e -> {
            client.sendSpeakerCommand(id, "NEXT");
            log("🔊 " + name + ": ⏭ Siguiente");
        });
        controlPanel.add(nextBtn);
        
        // Botón Stop
        JButton stopBtn = new JButton("⏹");
        stopBtn.setFont(new Font("Segoe UI", Font.BOLD, 14));
        stopBtn.setPreferredSize(new Dimension(45, 35));
        stopBtn.setBackground(new Color(100, 40, 40));
        stopBtn.setForeground(Color.WHITE);
        stopBtn.setFocusPainted(false);
        stopBtn.setToolTipText("Detener");
        stopBtn.addActionListener(e -> {
            client.sendSpeakerCommand(id, "STOP");
            log("🔊 " + name + ": ⏹ Detener");
        });
        controlPanel.add(stopBtn);
        
        card.add(controlPanel);
        card.add(Box.createVerticalStrut(12));
        
        // ═══════════════════════════════════════════════════════════
        // VOLUMEN
        // ═══════════════════════════════════════════════════════════
        JPanel volumePanel = new JPanel(new FlowLayout(FlowLayout.LEFT, 8, 0));
        volumePanel.setBackground(bgSpeaker);
        volumePanel.setAlignmentX(Component.LEFT_ALIGNMENT);
        
        JLabel volIcon = new JLabel("🔉");
        volIcon.setFont(new Font("Segoe UI", Font.PLAIN, 14));
        volumePanel.add(volIcon);
        
        JSlider volumeSlider = new JSlider(0, 100, volume);
        volumeSlider.setPreferredSize(new Dimension(200, 25));
        volumeSlider.setBackground(bgSpeaker);
        volumeSlider.setForeground(alexaCyan);
        
        JLabel volumeLabel = new JLabel(volume + "%");
        volumeLabel.setFont(new Font("Segoe UI", Font.BOLD, 12));
        volumeLabel.setForeground(Color.WHITE);
        volumeLabel.setPreferredSize(new Dimension(40, 20));
        
        volumeSlider.addChangeListener(e -> {
            int vol = volumeSlider.getValue();
            volumeLabel.setText(vol + "%");
            if (!volumeSlider.getValueIsAdjusting()) {
                client.controlDevice(id, "SET_VALUE", String.valueOf(vol));
                log("🔊 " + name + ": Volumen " + vol + "%");
            }
        });
        
        volumePanel.add(volumeSlider);
        volumePanel.add(volumeLabel);
        card.add(volumePanel);
        
        card.add(Box.createVerticalStrut(10));
        
        // ═══════════════════════════════════════════════════════════
        // SELECTOR DE PISTA
        // ═══════════════════════════════════════════════════════════
        JPanel trackPanel = new JPanel(new FlowLayout(FlowLayout.LEFT, 8, 0));
        trackPanel.setBackground(bgSpeaker);
        trackPanel.setAlignmentX(Component.LEFT_ALIGNMENT);
        
        JLabel trackIcon = new JLabel("🎵");
        trackPanel.add(trackIcon);
        
        // Lista de pistas (hardcodeadas, podrías obtenerlas del servidor)
        String[] tracks = {"Pista 1", "Pista 2", "Pista 3", "Pista 4", "Pista 5"};
        JComboBox<String> trackCombo = new JComboBox<>(tracks);
        trackCombo.setPreferredSize(new Dimension(150, 25));
        trackCombo.setBackground(new Color(40, 40, 40));
        trackCombo.setForeground(Color.WHITE);
        trackCombo.addActionListener(e -> {
            int trackIndex = trackCombo.getSelectedIndex();
            client.sendSpeakerCommand(id, String.valueOf(trackIndex));
            log("🔊 " + name + ": Cargando " + tracks[trackIndex]);
        });
        trackPanel.add(trackCombo);
        
        card.add(trackPanel);
        
        return card;
    }
    
    /**
     * Crear tarjeta compacta para clima
     */
    private JPanel createClimaCard(Map<String, String> device) {
        String id = device.get("id");
        String name = device.get("name");
        String room = device.get("room");
        boolean status = "true".equals(device.get("status"));
        
        JPanel card = new JPanel();
        card.setLayout(new BoxLayout(card, BoxLayout.Y_AXIS));
        card.setBackground(new Color(30, 58, 95)); // Azul oscuro para climas
        card.setBorder(BorderFactory.createCompoundBorder(
            BorderFactory.createLineBorder(new Color(56, 189, 248, 50), 1),
            BorderFactory.createEmptyBorder(10, 15, 10, 15)
        ));
        
        // Nombre corto (quitar "Clima ")
        String shortName = name.replace("Clima ", "");
        JLabel nameLabel = new JLabel("❄️ " + shortName);
        nameLabel.setFont(new Font("Segoe UI", Font.BOLD, 13));
        nameLabel.setForeground(Color.WHITE);
        nameLabel.setAlignmentX(Component.LEFT_ALIGNMENT);
        card.add(nameLabel);
        
        // Estado
        JPanel statusPanel = new JPanel(new FlowLayout(FlowLayout.LEFT, 3, 0));
        statusPanel.setBackground(new Color(30, 58, 95));
        statusPanel.setAlignmentX(Component.LEFT_ALIGNMENT);
        
        JLabel statusDot = new JLabel("●");
        statusDot.setForeground(status ? accent : Color.GRAY);
        statusPanel.add(statusDot);
        
        JLabel statusText = new JLabel(status ? "Encendido" : "Apagado");
        statusText.setFont(new Font("Segoe UI", Font.PLAIN, 11));
        statusText.setForeground(textSecondary);
        statusPanel.add(statusText);
        card.add(statusPanel);
        
        card.add(Box.createVerticalStrut(8));
        
        // Botones compactos
        JPanel btnPanel = new JPanel(new FlowLayout(FlowLayout.LEFT, 5, 0));
        btnPanel.setBackground(new Color(30, 58, 95));
        btnPanel.setAlignmentX(Component.LEFT_ALIGNMENT);
        
        JButton onBtn = new JButton("ON");
        onBtn.setFont(new Font("Segoe UI", Font.BOLD, 10));
        onBtn.setPreferredSize(new Dimension(50, 24));
        onBtn.setBackground(accent);
        onBtn.setForeground(Color.BLACK);
        onBtn.setFocusPainted(false);
        onBtn.addActionListener(e -> {
            client.controlDevice(id, "ON");
            log("❄️ Encendiendo: " + name);
        });
        btnPanel.add(onBtn);
        
        JButton offBtn = new JButton("OFF");
        offBtn.setFont(new Font("Segoe UI", Font.BOLD, 10));
        offBtn.setPreferredSize(new Dimension(50, 24));
        offBtn.setBackground(new Color(239, 68, 68));
        offBtn.setForeground(Color.WHITE);
        offBtn.setFocusPainted(false);
        offBtn.addActionListener(e -> {
            client.controlDevice(id, "OFF");
            log("❄️ Apagando: " + name);
        });
        btnPanel.add(offBtn);
        
        card.add(btnPanel);
        
        return card;
    }
    
    /**
     * Crear tarjeta de cámara de seguridad con streaming en vivo
     */
    private JPanel createCameraCard(Map<String, String> device) {
        String id = device.get("id");
        String name = device.get("name");
        String room = device.get("room");
        boolean status = "true".equals(device.get("status")); // true = cámara encendida
        String valueStr = device.get("value");
        int lightValue = 0;
        try {
            if (valueStr != null) lightValue = Integer.parseInt(valueStr);
        } catch (Exception e) {}
        boolean lightOn = lightValue > 0;
        
        // Obtener cameraId del nombre (ej: "Cámara Entrada" -> "cam_entrada")
        String cameraId = name.toLowerCase()
            .replace("cámara ", "cam_")
            .replace("á", "a")
            .replace("é", "e")
            .replace("í", "i")
            .replace("ó", "o")
            .replace("ú", "u")
            .replace(" ", "_");
        
        Color cameraPurple = new Color(168, 85, 247);
        Color bgCamera = new Color(35, 25, 55);
        
        JPanel card = new JPanel();
        card.setLayout(new BoxLayout(card, BoxLayout.Y_AXIS));
        card.setBackground(bgCamera);
        card.setBorder(BorderFactory.createCompoundBorder(
            BorderFactory.createLineBorder(cameraPurple.darker(), 1),
            BorderFactory.createEmptyBorder(10, 12, 10, 12)
        ));
        
        // ═══════════════════════════════════════════════════════════
        // ENCABEZADO CON NOMBRE Y ESTADO
        // ═══════════════════════════════════════════════════════════
        JPanel headerPanel = new JPanel(new BorderLayout());
        headerPanel.setBackground(bgCamera);
        headerPanel.setAlignmentX(Component.LEFT_ALIGNMENT);
        headerPanel.setMaximumSize(new Dimension(Integer.MAX_VALUE, 25));
        
        JLabel nameLabel = new JLabel("📹 " + name);
        nameLabel.setFont(new Font("Segoe UI", Font.BOLD, 12));
        nameLabel.setForeground(Color.WHITE);
        headerPanel.add(nameLabel, BorderLayout.WEST);
        
        // Indicador de estado (punto verde/rojo)
        JLabel statusDot = new JLabel("●");
        statusDot.setFont(new Font("Segoe UI", Font.BOLD, 14));
        statusDot.setForeground(status ? new Color(74, 222, 128) : new Color(239, 68, 68));
        headerPanel.add(statusDot, BorderLayout.EAST);
        
        card.add(headerPanel);
        card.add(Box.createVerticalStrut(3));
        
        // Ubicación pequeña
        JLabel roomLabel = new JLabel("📍 " + room);
        roomLabel.setFont(new Font("Segoe UI", Font.PLAIN, 9));
        roomLabel.setForeground(textSecondary);
        roomLabel.setAlignmentX(Component.LEFT_ALIGNMENT);
        card.add(roomLabel);
        
        card.add(Box.createVerticalStrut(5));
        
        // ═══════════════════════════════════════════════════════════
        // ÁREA DE VIDEO EN VIVO
        // ═══════════════════════════════════════════════════════════
        JLabel videoLabel = new JLabel();
        videoLabel.setHorizontalAlignment(SwingConstants.CENTER);
        videoLabel.setVerticalAlignment(SwingConstants.CENTER);
        videoLabel.setBackground(Color.BLACK);
        videoLabel.setOpaque(true);
        videoLabel.setPreferredSize(new Dimension(280, 160));
        videoLabel.setMinimumSize(new Dimension(200, 120));
        videoLabel.setMaximumSize(new Dimension(400, 220));
        videoLabel.setText("▶ Click Play");
        videoLabel.setForeground(Color.GRAY);
        videoLabel.setFont(new Font("Segoe UI", Font.ITALIC, 11));
        videoLabel.setAlignmentX(Component.LEFT_ALIGNMENT);
        videoLabel.setBorder(BorderFactory.createLineBorder(new Color(60, 60, 60), 1));
        
        card.add(videoLabel);
        card.add(Box.createVerticalStrut(8));
        
        // ═══════════════════════════════════════════════════════════
        // CONTROLES DE VIDEO
        // ═══════════════════════════════════════════════════════════
        JPanel videoControlPanel = new JPanel(new FlowLayout(FlowLayout.LEFT, 4, 0));
        videoControlPanel.setBackground(bgCamera);
        videoControlPanel.setAlignmentX(Component.LEFT_ALIGNMENT);
        
        // Estado del streaming
        final boolean[] isStreaming = {false};
        final Thread[] streamThread = {null};
        final JLabel[] fpsLabelRef = {null};
        
        // Botón Play/Pause
        JButton playBtn = new JButton("▶");
        playBtn.setFont(new Font("Segoe UI", Font.BOLD, 12));
        playBtn.setPreferredSize(new Dimension(40, 26));
        playBtn.setBackground(new Color(74, 222, 128));
        playBtn.setForeground(Color.BLACK);
        playBtn.setFocusPainted(false);
        playBtn.setToolTipText("Iniciar/Pausar stream");
        
        // Botón Maximizar
        JButton maxBtn = new JButton("⛶");
        maxBtn.setFont(new Font("Segoe UI", Font.PLAIN, 12));
        maxBtn.setPreferredSize(new Dimension(35, 26));
        maxBtn.setBackground(new Color(60, 60, 80));
        maxBtn.setForeground(Color.WHITE);
        maxBtn.setFocusPainted(false);
        maxBtn.setToolTipText("Pantalla completa");
        
        // FPS Label
        JLabel fpsLabel = new JLabel("-- FPS");
        fpsLabel.setFont(new Font("Segoe UI", Font.PLAIN, 9));
        fpsLabel.setForeground(textSecondary);
        fpsLabelRef[0] = fpsLabel;
        
        // Acción Play/Pause
        playBtn.addActionListener(e -> {
            if (!isStreaming[0]) {
                // Iniciar streaming
                isStreaming[0] = true;
                playBtn.setText("⏸");
                playBtn.setBackground(new Color(239, 68, 68));
                videoLabel.setText("");
                
                streamThread[0] = new Thread(() -> {
                    int frameCount = 0;
                    long lastFpsTime = System.currentTimeMillis();
                    
                    while (isStreaming[0]) {
                        try {
                            java.net.URL url = new java.net.URL("http://" + serverHost + ":8081/camera/frame?id=" + cameraId);
                            java.net.HttpURLConnection conn = (java.net.HttpURLConnection) url.openConnection();
                            conn.setConnectTimeout(2000);
                            conn.setReadTimeout(2000);
                            
                            if (conn.getResponseCode() == 200) {
                                java.io.InputStream in = conn.getInputStream();
                                java.awt.image.BufferedImage img = javax.imageio.ImageIO.read(in);
                                in.close();
                                
                                if (img != null) {
                                    // Escalar imagen
                                    int w = videoLabel.getWidth();
                                    int h = videoLabel.getHeight();
                                    if (w > 0 && h > 0) {
                                        Image scaled = img.getScaledInstance(w, h, Image.SCALE_FAST);
                                        SwingUtilities.invokeLater(() -> {
                                            videoLabel.setIcon(new ImageIcon(scaled));
                                            videoLabel.setText("");
                                        });
                                    }
                                    
                                    // FPS
                                    frameCount++;
                                    long now = System.currentTimeMillis();
                                    if (now - lastFpsTime >= 1000) {
                                        int fps = frameCount;
                                        frameCount = 0;
                                        lastFpsTime = now;
                                        SwingUtilities.invokeLater(() -> fpsLabelRef[0].setText(fps + " FPS"));
                                    }
                                }
                            }
                            conn.disconnect();
                            Thread.sleep(50); // ~20 FPS max
                            
                        } catch (InterruptedException ie) {
                            break;
                        } catch (Exception ex) {
                            SwingUtilities.invokeLater(() -> {
                                videoLabel.setIcon(null);
                                videoLabel.setText("Sin señal");
                            });
                            try { Thread.sleep(1000); } catch (Exception ignored) {}
                        }
                    }
                }, "CameraStream-" + cameraId);
                streamThread[0].setDaemon(true);
                streamThread[0].start();
                
            } else {
                // Detener streaming
                isStreaming[0] = false;
                playBtn.setText("▶");
                playBtn.setBackground(new Color(74, 222, 128));
                if (streamThread[0] != null) {
                    streamThread[0].interrupt();
                }
                videoLabel.setIcon(null);
                videoLabel.setText("▶ Click Play");
                fpsLabelRef[0].setText("-- FPS");
            }
        });
        
        // Acción Maximizar - abrir ventana grande
        maxBtn.addActionListener(e -> {
            openFullscreenCamera(name, cameraId, serverHost);
        });
        
        videoControlPanel.add(playBtn);
        videoControlPanel.add(maxBtn);
        videoControlPanel.add(Box.createHorizontalStrut(5));
        videoControlPanel.add(fpsLabel);
        
        card.add(videoControlPanel);
        card.add(Box.createVerticalStrut(6));
        
        // ═══════════════════════════════════════════════════════════
        // CONTROLES DE DISPOSITIVO (Cámara ON/OFF, Luz IR)
        // ═══════════════════════════════════════════════════════════
        JPanel deviceControlPanel = new JPanel(new FlowLayout(FlowLayout.LEFT, 4, 0));
        deviceControlPanel.setBackground(bgCamera);
        deviceControlPanel.setAlignmentX(Component.LEFT_ALIGNMENT);
        
        // Estado mutable para actualizar UI sin recargar
        final boolean[] camStatus = {status};
        final boolean[] irStatus = {lightOn};
        
        // Botón ON/OFF Cámara
        JButton camToggleBtn = new JButton(status ? "📴 OFF" : "📹 ON");
        camToggleBtn.setFont(new Font("Segoe UI", Font.BOLD, 9));
        camToggleBtn.setPreferredSize(new Dimension(55, 24));
        camToggleBtn.setBackground(status ? new Color(239, 68, 68) : new Color(74, 222, 128));
        camToggleBtn.setForeground(status ? Color.WHITE : Color.BLACK);
        camToggleBtn.setFocusPainted(false);
        camToggleBtn.setToolTipText("Encender/Apagar cámara en Unity");
        
        // Botón toggle luz IR
        JButton lightToggleBtn = new JButton(lightOn ? "🔦 OFF" : "💡 ON");
        lightToggleBtn.setFont(new Font("Segoe UI", Font.BOLD, 9));
        lightToggleBtn.setPreferredSize(new Dimension(55, 24));
        lightToggleBtn.setBackground(lightOn ? new Color(100, 80, 20) : new Color(60, 60, 60));
        lightToggleBtn.setForeground(Color.WHITE);
        lightToggleBtn.setFocusPainted(false);
        lightToggleBtn.setToolTipText("Encender/Apagar luz infrarroja");
        
        // Indicador de luz
        JLabel lightIndicator = new JLabel(lightOn ? "💡" : "");
        lightIndicator.setFont(new Font("Segoe UI", Font.PLAIN, 12));
        
        // Acción Cámara ON/OFF - Actualiza visualmente sin recargar
        camToggleBtn.addActionListener(e -> {
            boolean newStatus = !camStatus[0];
            camStatus[0] = newStatus;
            
            // Actualizar UI inmediatamente
            camToggleBtn.setText(newStatus ? "📴 OFF" : "📹 ON");
            camToggleBtn.setBackground(newStatus ? new Color(239, 68, 68) : new Color(74, 222, 128));
            camToggleBtn.setForeground(newStatus ? Color.WHITE : Color.BLACK);
            statusDot.setForeground(newStatus ? new Color(74, 222, 128) : new Color(239, 68, 68));
            
            // Enviar comando al servidor
            client.controlDevice(id, newStatus ? "ON" : "OFF");
            log("📹 " + name + ": " + (newStatus ? "Encendiendo" : "Apagando"));
        });
        
        // Acción Luz IR - Actualiza visualmente sin recargar
        lightToggleBtn.addActionListener(e -> {
            boolean newLightStatus = !irStatus[0];
            irStatus[0] = newLightStatus;
            
            // Actualizar UI inmediatamente
            lightToggleBtn.setText(newLightStatus ? "🔦 OFF" : "💡 ON");
            lightToggleBtn.setBackground(newLightStatus ? new Color(100, 80, 20) : new Color(60, 60, 60));
            lightIndicator.setText(newLightStatus ? "💡" : "");
            
            // Enviar comando al servidor
            int newValue = newLightStatus ? 100 : 0;
            client.controlDevice(id, "SET_VALUE", String.valueOf(newValue));
            log("📹 " + name + ": Luz IR " + (newLightStatus ? "ON" : "OFF"));
        });
        
        deviceControlPanel.add(camToggleBtn);
        deviceControlPanel.add(lightToggleBtn);
        deviceControlPanel.add(lightIndicator);
        
        card.add(deviceControlPanel);
        
        return card;
    }
    
    /**
     * Escalar imagen con alta calidad usando Graphics2D
     */
    private java.awt.image.BufferedImage scaleImageHighQuality(java.awt.image.BufferedImage src, int targetW, int targetH) {
        java.awt.image.BufferedImage scaled = new java.awt.image.BufferedImage(targetW, targetH, java.awt.image.BufferedImage.TYPE_INT_RGB);
        java.awt.Graphics2D g2d = scaled.createGraphics();
        g2d.setRenderingHint(java.awt.RenderingHints.KEY_INTERPOLATION, java.awt.RenderingHints.VALUE_INTERPOLATION_BICUBIC);
        g2d.setRenderingHint(java.awt.RenderingHints.KEY_RENDERING, java.awt.RenderingHints.VALUE_RENDER_QUALITY);
        g2d.setRenderingHint(java.awt.RenderingHints.KEY_ANTIALIASING, java.awt.RenderingHints.VALUE_ANTIALIAS_ON);
        g2d.drawImage(src, 0, 0, targetW, targetH, null);
        g2d.dispose();
        return scaled;
    }
    
    /**
     * Abrir cámara en ventana de pantalla completa
     */
    private void openFullscreenCamera(String cameraName, String cameraId, String host) {
        JDialog fullscreenDialog = new JDialog(this, "📹 " + cameraName + " - Pantalla Completa", false);
        fullscreenDialog.setSize(1280, 800);
        fullscreenDialog.setLocationRelativeTo(this);
        fullscreenDialog.getContentPane().setBackground(Color.BLACK);
        fullscreenDialog.setLayout(new BorderLayout());
        
        // Área de video grande
        JLabel bigVideoLabel = new JLabel();
        bigVideoLabel.setHorizontalAlignment(SwingConstants.CENTER);
        bigVideoLabel.setBackground(Color.BLACK);
        bigVideoLabel.setOpaque(true);
        bigVideoLabel.setFont(new Font("Segoe UI", Font.BOLD, 16));
        bigVideoLabel.setForeground(Color.GRAY);
        bigVideoLabel.setText("Conectando...");
        fullscreenDialog.add(bigVideoLabel, BorderLayout.CENTER);
        
        // Panel de controles abajo
        JPanel controlBar = new JPanel(new FlowLayout(FlowLayout.CENTER, 20, 10));
        controlBar.setBackground(new Color(30, 30, 30));
        
        JLabel fpsLabel = new JLabel("-- FPS");
        fpsLabel.setForeground(Color.WHITE);
        fpsLabel.setFont(new Font("Segoe UI", Font.BOLD, 14));
        controlBar.add(fpsLabel);
        
        JLabel resLabel = new JLabel("");
        resLabel.setForeground(new Color(150, 150, 150));
        resLabel.setFont(new Font("Segoe UI", Font.PLAIN, 11));
        controlBar.add(resLabel);
        
        JButton closeBtn = new JButton("✕ Cerrar");
        closeBtn.setBackground(new Color(239, 68, 68));
        closeBtn.setForeground(Color.WHITE);
        closeBtn.setFont(new Font("Segoe UI", Font.BOLD, 12));
        controlBar.add(closeBtn);
        
        fullscreenDialog.add(controlBar, BorderLayout.SOUTH);
        
        // Streaming thread
        final boolean[] running = {true};
        Thread streamThread = new Thread(() -> {
            int frameCount = 0;
            long lastFpsTime = System.currentTimeMillis();
            
            while (running[0]) {
                try {
                    java.net.URL url = new java.net.URL("http://" + host + ":8081/camera/frame?id=" + cameraId);
                    java.net.HttpURLConnection conn = (java.net.HttpURLConnection) url.openConnection();
                    conn.setConnectTimeout(3000);
                    conn.setReadTimeout(3000);
                    
                    if (conn.getResponseCode() == 200) {
                        java.io.InputStream in = conn.getInputStream();
                        java.awt.image.BufferedImage img = javax.imageio.ImageIO.read(in);
                        in.close();
                        
                        if (img != null) {
                            final int imgW = img.getWidth();
                            final int imgH = img.getHeight();
                            int w = bigVideoLabel.getWidth();
                            int h = bigVideoLabel.getHeight();
                            if (w > 0 && h > 0) {
                                // Mantener aspect ratio
                                double imgRatio = (double) imgW / imgH;
                                double labelRatio = (double) w / h;
                                int newW, newH;
                                if (imgRatio > labelRatio) {
                                    newW = w;
                                    newH = (int) (w / imgRatio);
                                } else {
                                    newH = h;
                                    newW = (int) (h * imgRatio);
                                }
                                // Usar escalado de alta calidad
                                java.awt.image.BufferedImage scaled = scaleImageHighQuality(img, newW, newH);
                                SwingUtilities.invokeLater(() -> {
                                    bigVideoLabel.setIcon(new ImageIcon(scaled));
                                    bigVideoLabel.setText("");
                                    resLabel.setText(imgW + "x" + imgH + " → " + newW + "x" + newH);
                                });
                            }
                            
                            frameCount++;
                            long now = System.currentTimeMillis();
                            if (now - lastFpsTime >= 1000) {
                                int fps = frameCount;
                                frameCount = 0;
                                lastFpsTime = now;
                                SwingUtilities.invokeLater(() -> fpsLabel.setText(fps + " FPS"));
                            }
                        }
                    }
                    conn.disconnect();
                    Thread.sleep(33); // ~30 FPS
                    
                } catch (Exception ex) {
                    SwingUtilities.invokeLater(() -> {
                        bigVideoLabel.setIcon(null);
                        bigVideoLabel.setText("Sin señal - Esperando...");
                    });
                    try { Thread.sleep(1000); } catch (Exception ignored) {}
                }
            }
        }, "FullscreenStream-" + cameraId);
        streamThread.setDaemon(true);
        streamThread.start();
        
        // Cerrar
        closeBtn.addActionListener(e -> {
            running[0] = false;
            fullscreenDialog.dispose();
        });
        
        fullscreenDialog.addWindowListener(new java.awt.event.WindowAdapter() {
            @Override
            public void windowClosing(java.awt.event.WindowEvent e) {
                running[0] = false;
            }
        });
        
        fullscreenDialog.setVisible(true);
    }
    
    /**
     * Crear tarjeta de dispositivo
     */
    private JPanel createDeviceCard(Map<String, String> device) {
        JPanel card = new JPanel();
        card.setLayout(new BoxLayout(card, BoxLayout.Y_AXIS));
        card.setBackground(bgCard);
        card.setBorder(BorderFactory.createCompoundBorder(
            BorderFactory.createLineBorder(new Color(255,255,255,20), 1),
            BorderFactory.createEmptyBorder(15, 15, 15, 15)
        ));
        
        String id = device.get("id");
        String name = device.get("name");
        String type = device.get("type");
        String room = device.get("room");
        boolean status = "true".equals(device.get("status"));
        String value = device.get("value");
        String color = device.get("color");
        
        // Icono y nombre
        String icon = getDeviceIcon(type);
        JLabel nameLabel = new JLabel(icon + " " + name);
        nameLabel.setFont(new Font("Segoe UI", Font.BOLD, 14));
        nameLabel.setForeground(textPrimary);
        nameLabel.setAlignmentX(Component.LEFT_ALIGNMENT);
        card.add(nameLabel);
        
        // Tipo y habitación
        JLabel infoLabel = new JLabel(type + " • " + room);
        infoLabel.setFont(new Font("Segoe UI", Font.PLAIN, 11));
        infoLabel.setForeground(textSecondary);
        infoLabel.setAlignmentX(Component.LEFT_ALIGNMENT);
        card.add(infoLabel);
        
        card.add(Box.createVerticalStrut(10));
        
        // Estado
        JPanel statusPanel = new JPanel(new FlowLayout(FlowLayout.LEFT, 5, 0));
        statusPanel.setBackground(bgCard);
        statusPanel.setAlignmentX(Component.LEFT_ALIGNMENT);
        
        // Tipos especiales
        boolean isAppliance = "appliance".equals(type);
        boolean isTV = "tv".equals(type);
        boolean isDoor = "door".equals(type);
        // Para puertas: status=true significa cerrado, status=false significa abierto (invertir)
        boolean displayStatus = isDoor ? !status : status;
        
        JLabel statusDot = new JLabel("●");
        statusDot.setForeground(displayStatus ? accent : Color.GRAY);
        statusPanel.add(statusDot);
        
        // Texto del estado según tipo
        String statusOn = isDoor ? "Abierto" : (isTV ? "Mostrada" : "Encendido");
        String statusOff = isDoor ? "Cerrado" : (isTV ? "Escondida" : "Apagado");
        JLabel statusText = new JLabel(displayStatus ? statusOn : statusOff);
        statusText.setForeground(textPrimary);
        statusPanel.add(statusText);
        
        if (value != null && !"0".equals(value) && !value.isEmpty()) {
            String unit = "thermostat".equals(type) ? "°C" : "%";
            JLabel valueLabel = new JLabel(" | " + value + unit);
            valueLabel.setForeground(textSecondary);
            statusPanel.add(valueLabel);
        }
        
        if (color != null && !color.isEmpty()) {
            JLabel colorBox = new JLabel("  ■");
            try {
                colorBox.setForeground(Color.decode(color));
            } catch (Exception e) {
                colorBox.setForeground(Color.WHITE);
            }
            statusPanel.add(colorBox);
        }
        
        card.add(statusPanel);
        card.add(Box.createVerticalStrut(10));
        
        // Botones de control
        JPanel buttonsPanel = new JPanel(new FlowLayout(FlowLayout.LEFT, 5, 0));
        buttonsPanel.setBackground(bgCard);
        buttonsPanel.setAlignmentX(Component.LEFT_ALIGNMENT);
        
        // Texto diferente para puertas, TV y electrodomésticos
        // isDoor, isTV, isAppliance ya definidos arriba
        
        // Etiquetas: Puertas=Abrir/Cerrar, TV=Esconder/Mostrar, Appliance=Encender/Apagar, Otros=ON/OFF
        String onLabel = isDoor ? "Abrir" : (isTV ? "Esconder" : (isAppliance ? "Encender" : "ON"));
        String offLabel = isDoor ? "Cerrar" : (isTV ? "Mostrar" : (isAppliance ? "Apagar" : "OFF"));
        
        JButton onBtn = new JButton(onLabel);
        onBtn.setBackground(accent);
        onBtn.setForeground(Color.BLACK);
        onBtn.setFocusPainted(false);
        onBtn.addActionListener(e -> {
            // Para puertas y TV: botón verde envía OFF, para otros: ON envía ON
            client.controlDevice(id, (isDoor || isTV) ? "OFF" : "ON");
            log(isDoor ? "→ Abriendo: " + name : (isTV ? "→ Escondiendo: " + name : (isAppliance ? "→ Encendiendo: " + name : "→ ON: " + name)));
        });
        buttonsPanel.add(onBtn);
        
        JButton offBtn = new JButton(offLabel);
        offBtn.setBackground(new Color(239, 68, 68));
        offBtn.setForeground(Color.WHITE);
        offBtn.setFocusPainted(false);
        offBtn.addActionListener(e -> {
            // Para puertas y TV: botón rojo envía ON, para otros: OFF envía OFF
            client.controlDevice(id, (isDoor || isTV) ? "ON" : "OFF");
            log(isDoor ? "→ Cerrando: " + name : (isTV ? "→ Mostrando: " + name : (isAppliance ? "→ Apagando: " + name : "→ OFF: " + name)));
        });
        buttonsPanel.add(offBtn);
        
        // Slider para valor (luces y termostato)
        if ("light".equals(type) || "thermostat".equals(type)) {
            JButton valueBtn = new JButton("Val");
            valueBtn.setFont(new Font("Segoe UI", Font.BOLD, 11));
            valueBtn.setPreferredSize(new Dimension(45, 28));
            valueBtn.setBackground(new Color(59, 130, 246));
            valueBtn.setForeground(Color.WHITE);
            valueBtn.setFocusPainted(false);
            valueBtn.setToolTipText("thermostat".equals(type) ? "Cambiar temperatura" : "Cambiar brillo");
            valueBtn.addActionListener(e -> {
                String input = JOptionPane.showInputDialog(this, 
                    "thermostat".equals(type) ? "Temperatura (°C):" : "Brillo (0-100):", 
                    value);
                if (input != null && !input.isEmpty()) {
                    client.controlDevice(id, "SET_VALUE", input);
                    log("→ SET_VALUE: " + name + " = " + input);
                }
            });
            buttonsPanel.add(valueBtn);
        }
        
        // Color para luces
        if ("light".equals(type)) {
            JButton colorBtn = new JButton("RGB");
            colorBtn.setFont(new Font("Segoe UI", Font.BOLD, 10));
            colorBtn.setPreferredSize(new Dimension(45, 28));
            colorBtn.setBackground(new Color(168, 85, 247));
            colorBtn.setForeground(Color.WHITE);
            colorBtn.setFocusPainted(false);
            colorBtn.setToolTipText("Cambiar color");
            colorBtn.addActionListener(e -> {
                Color c = JColorChooser.showDialog(this, "Seleccionar color", Color.WHITE);
                if (c != null) {
                    String hex = String.format("#%02x%02x%02x", c.getRed(), c.getGreen(), c.getBlue());
                    client.setDeviceColor(id, hex);
                    log("→ SET_COLOR: " + name + " = " + hex);
                }
            });
            buttonsPanel.add(colorBtn);
        }
        
        card.add(buttonsPanel);
        
        return card;
    }
    
    /**
     * Filtrar dispositivos por habitación
     */
    private void filterDevices() {
        updateDevicesPanel();
    }
    
    /**
     * Logout
     */
    private void doLogout() {
        // Cerrar panel de cámaras si está abierto
        if (cameraDialog != null && cameraDialog.isVisible()) {
            cameraViewerPanel.cleanup();
            cameraDialog.dispose();
        }
        
        if (client != null) {
            client.disconnect();
        }
        currentUser = null;
        currentToken = null;
        devices.clear();
        rooms.clear();
        
        statusLabel.setText(" ");
        connectButton.setEnabled(true);
        setContentPane(loginPanel);
        revalidate();
        repaint();
    }
    
    /**
     * Abrir visor de cámaras en ventana separada
     */
    private void openCameraViewer() {
        if (cameraDialog != null && cameraDialog.isVisible()) {
            cameraDialog.toFront();
            return;
        }
        
        cameraDialog = new JDialog(this, "📹 Cámaras de Seguridad - En Vivo", false);
        cameraDialog.setSize(800, 600);
        cameraDialog.setLocationRelativeTo(this);
        
        cameraViewerPanel = new CameraViewerPanel(this, serverHost);
        cameraDialog.add(cameraViewerPanel);
        
        cameraDialog.addWindowListener(new java.awt.event.WindowAdapter() {
            @Override
            public void windowClosing(java.awt.event.WindowEvent e) {
                cameraViewerPanel.cleanup();
            }
        });
        
        cameraDialog.setVisible(true);
        log("📹 Abriendo visor de cámaras...");
    }
    
    /**
     * Log de eventos
     */
    private void log(String message) {
        SwingUtilities.invokeLater(() -> {
            logArea.append(message + "\n");
            logArea.setCaretPosition(logArea.getDocument().getLength());
        });
    }
    
    // ═══════════════════════════════════════════════════════════
    // UTILIDADES
    // ═══════════════════════════════════════════════════════════
    
    private JTextField createStyledTextField(String text, int columns) {
        JTextField field = new JTextField(text, columns);
        styleTextField(field);
        return field;
    }
    
    private void styleTextField(JTextField field) {
        field.setBackground(new Color(15, 23, 42));
        field.setForeground(textPrimary);
        field.setCaretColor(textPrimary);
        field.setBorder(BorderFactory.createCompoundBorder(
            BorderFactory.createLineBorder(new Color(255,255,255,30)),
            BorderFactory.createEmptyBorder(8, 10, 8, 10)
        ));
    }
    
    private JLabel createLabel(String text) {
        JLabel label = new JLabel(text);
        label.setForeground(textSecondary);
        return label;
    }
    
    private JPanel createFieldPanel(String labelText, JTextField field) {
        JPanel panel = new JPanel(new FlowLayout(FlowLayout.CENTER));
        panel.setBackground(bgCard);
        panel.add(createLabel(labelText));
        panel.add(field);
        return panel;
    }
    
    private String getDeviceIcon(String type) {
        if (type == null) return "📱";
        switch (type) {
            case "light": return "💡";
            case "thermostat": return "🌡️";
            case "door": return "🚪";
            case "camera": return "📹";
            case "sensor": return "📡";
            case "tv": return "📺";
            case "appliance": return "🧺";
            case "speaker": return "🔊";
            case "ac": return "❄️";
            default: return "📱";
        }
    }
    
    private Map<String, String> parseJsonSimple(String json) {
        Map<String, String> data = new HashMap<>();
        if (json == null || json.isEmpty()) return data;
        
        // Limpiar el JSON - remover escapes
        json = json.replace("\\\"", "\"");
        json = json.replace("\\\\", "\\");
        json = json.trim();
        
        if (json.startsWith("{")) json = json.substring(1);
        if (json.endsWith("}")) json = json.substring(0, json.length() - 1);
        
        // Usar regex simple para extraer key:value
        // Buscar patrones "key":value o "key":"value"
        int i = 0;
        while (i < json.length()) {
            // Saltar espacios
            while (i < json.length() && Character.isWhitespace(json.charAt(i))) i++;
            if (i >= json.length()) break;
            
            // Buscar inicio de key (comilla)
            if (json.charAt(i) != '"') { i++; continue; }
            i++; // saltar comilla
            
            // Leer key
            StringBuilder key = new StringBuilder();
            while (i < json.length() && json.charAt(i) != '"') {
                key.append(json.charAt(i));
                i++;
            }
            i++; // saltar comilla de cierre
            
            // Saltar hasta :
            while (i < json.length() && json.charAt(i) != ':') i++;
            i++; // saltar :
            
            // Saltar espacios
            while (i < json.length() && Character.isWhitespace(json.charAt(i))) i++;
            if (i >= json.length()) break;
            
            // Leer value
            StringBuilder value = new StringBuilder();
            char first = json.charAt(i);
            
            if (first == '"') {
                // String value
                i++; // saltar comilla
                while (i < json.length() && json.charAt(i) != '"') {
                    value.append(json.charAt(i));
                    i++;
                }
                i++; // saltar comilla de cierre
            } else if (first == '[' || first == '{') {
                // Array u objeto - leer hasta el cierre correspondiente
                int depth = 0;
                char open = first;
                char close = first == '[' ? ']' : '}';
                do {
                    char c = json.charAt(i);
                    if (c == open) depth++;
                    if (c == close) depth--;
                    value.append(c);
                    i++;
                } while (i < json.length() && depth > 0);
            } else {
                // Número, boolean, null
                while (i < json.length() && json.charAt(i) != ',' && json.charAt(i) != '}') {
                    value.append(json.charAt(i));
                    i++;
                }
            }
            
            String keyStr = key.toString().trim();
            String valueStr = value.toString().trim();
            if (!keyStr.isEmpty()) {
                data.put(keyStr, valueStr);
            }
            
            // Saltar coma si hay
            while (i < json.length() && (json.charAt(i) == ',' || Character.isWhitespace(json.charAt(i)))) i++;
        }
        
        return data;
    }
    
    /**
     * Main
     */
    public static void main(String[] args) {
        SwingUtilities.invokeLater(() -> {
            SmartHomeClientGUI gui = new SmartHomeClientGUI();
            gui.setVisible(true);
        });
    }
}
