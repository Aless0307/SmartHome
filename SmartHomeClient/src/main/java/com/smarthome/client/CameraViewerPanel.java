package com.smarthome.client;

import javax.swing.*;
import javax.swing.border.*;
import java.awt.*;
import java.awt.event.*;
import java.awt.image.*;
import java.io.*;
import java.net.*;
import javax.imageio.*;
import java.util.*;
import java.util.List;

/**
 * ═══════════════════════════════════════════════════════════════
 * PANEL DE CÁMARAS DE SEGURIDAD - Visor de Streaming
 * ═══════════════════════════════════════════════════════════════
 * 
 * Panel que muestra feeds en vivo de las cámaras de seguridad
 * usando streaming MJPEG desde el servidor.
 * 
 * Características:
 * - Streaming MJPEG en tiempo real
 * - Vista múltiple (grid) o individual (fullscreen)
 * - Controles: Play/Pause, Snapshot, Fullscreen
 * - Indicadores de estado
 */
public class CameraViewerPanel extends JPanel {
    
    // Colores del tema
    private Color bgDark = new Color(26, 26, 46);
    private Color bgCard = new Color(30, 41, 59);
    private Color accent = new Color(168, 85, 247);  // Púrpura
    private Color textPrimary = Color.WHITE;
    private Color textSecondary = new Color(148, 163, 184);
    
    // Configuración
    private String serverHost = "localhost";
    private int streamPort = 8081;
    
    // Componentes
    private JPanel headerPanel;
    private JPanel camerasGridPanel;
    private JPanel fullscreenPanel;
    private JLabel fullscreenImageLabel;
    private String fullscreenCameraId = null;
    
    // Cámaras y sus viewers
    private Map<String, CameraFeedPanel> cameraFeeds = new LinkedHashMap<>();
    
    // Lista de cámaras conocidas (se actualiza dinámicamente)
    private List<String> cameraIds = new ArrayList<>();
    
    // Referencias
    private JFrame parentFrame;
    
    public CameraViewerPanel(JFrame parent, String host) {
        this.parentFrame = parent;
        this.serverHost = host;
        
        setLayout(new BorderLayout());
        setBackground(bgDark);
        
        createUI();
    }
    
    private void createUI() {
        // ═══════════════════════════════════════════════════════════
        // HEADER
        // ═══════════════════════════════════════════════════════════
        headerPanel = new JPanel(new BorderLayout());
        headerPanel.setBackground(bgCard);
        headerPanel.setBorder(BorderFactory.createEmptyBorder(10, 15, 10, 15));
        
        JLabel titleLabel = new JLabel("📹 Cámaras de Seguridad - Vista en Vivo");
        titleLabel.setFont(new Font("Segoe UI", Font.BOLD, 16));
        titleLabel.setForeground(textPrimary);
        headerPanel.add(titleLabel, BorderLayout.WEST);
        
        JPanel controlsPanel = new JPanel(new FlowLayout(FlowLayout.RIGHT, 10, 0));
        controlsPanel.setBackground(bgCard);
        
        JButton refreshBtn = new JButton("🔄 Detectar Cámaras");
        refreshBtn.addActionListener(e -> detectCameras());
        controlsPanel.add(refreshBtn);
        
        JButton startAllBtn = new JButton("▶ Iniciar Todas");
        startAllBtn.setBackground(new Color(74, 222, 128));
        startAllBtn.setForeground(Color.BLACK);
        startAllBtn.addActionListener(e -> startAllStreams());
        controlsPanel.add(startAllBtn);
        
        JButton stopAllBtn = new JButton("⏹ Detener Todas");
        stopAllBtn.setBackground(new Color(239, 68, 68));
        stopAllBtn.setForeground(Color.WHITE);
        stopAllBtn.addActionListener(e -> stopAllStreams());
        controlsPanel.add(stopAllBtn);
        
        headerPanel.add(controlsPanel, BorderLayout.EAST);
        add(headerPanel, BorderLayout.NORTH);
        
        // ═══════════════════════════════════════════════════════════
        // GRID DE CÁMARAS
        // ═══════════════════════════════════════════════════════════
        camerasGridPanel = new JPanel(new GridLayout(0, 2, 10, 10));
        camerasGridPanel.setBackground(bgDark);
        camerasGridPanel.setBorder(BorderFactory.createEmptyBorder(10, 10, 10, 10));
        
        JScrollPane scrollPane = new JScrollPane(camerasGridPanel);
        scrollPane.setBorder(null);
        scrollPane.getViewport().setBackground(bgDark);
        scrollPane.getVerticalScrollBar().setUnitIncrement(20);
        add(scrollPane, BorderLayout.CENTER);
        
        // ═══════════════════════════════════════════════════════════
        // PANEL FULLSCREEN (oculto inicialmente)
        // ═══════════════════════════════════════════════════════════
        fullscreenPanel = new JPanel(new BorderLayout());
        fullscreenPanel.setBackground(Color.BLACK);
        fullscreenPanel.setVisible(false);
        
        fullscreenImageLabel = new JLabel();
        fullscreenImageLabel.setHorizontalAlignment(SwingConstants.CENTER);
        fullscreenPanel.add(fullscreenImageLabel, BorderLayout.CENTER);
        
        JPanel fullscreenControls = new JPanel(new FlowLayout(FlowLayout.CENTER));
        fullscreenControls.setBackground(new Color(0, 0, 0, 200));
        
        JButton exitFullscreenBtn = new JButton("✕ Salir de Pantalla Completa");
        exitFullscreenBtn.addActionListener(e -> exitFullscreen());
        fullscreenControls.add(exitFullscreenBtn);
        
        fullscreenPanel.add(fullscreenControls, BorderLayout.SOUTH);
        
        // Detectar cámaras al iniciar
        SwingUtilities.invokeLater(this::detectCameras);
    }
    
    /**
     * Detectar cámaras disponibles desde el servidor
     */
    public void detectCameras() {
        new Thread(() -> {
            try {
                URL url = new URL("http://" + serverHost + ":" + streamPort + "/camera/list");
                HttpURLConnection conn = (HttpURLConnection) url.openConnection();
                conn.setRequestMethod("GET");
                conn.setConnectTimeout(3000);
                
                if (conn.getResponseCode() == 200) {
                    BufferedReader reader = new BufferedReader(
                        new InputStreamReader(conn.getInputStream()));
                    StringBuilder response = new StringBuilder();
                    String line;
                    while ((line = reader.readLine()) != null) {
                        response.append(line);
                    }
                    reader.close();
                    
                    // Parsear JSON simple: {"cameras":["cam1","cam2"]}
                    String json = response.toString();
                    parseCameraList(json);
                }
                conn.disconnect();
                
            } catch (Exception e) {
                System.out.println("⚠️ No se pudieron detectar cámaras: " + e.getMessage());
                // Usar cámaras por defecto
                SwingUtilities.invokeLater(() -> {
                    addDefaultCameras();
                });
            }
        }).start();
    }
    
    private void parseCameraList(String json) {
        // Parseo simple de {"cameras":["cam1","cam2"]}
        cameraIds.clear();
        
        int start = json.indexOf("[");
        int end = json.indexOf("]");
        if (start != -1 && end != -1) {
            String camerasStr = json.substring(start + 1, end);
            String[] parts = camerasStr.split(",");
            for (String part : parts) {
                String id = part.replace("\"", "").trim();
                if (!id.isEmpty()) {
                    cameraIds.add(id);
                }
            }
        }
        
        SwingUtilities.invokeLater(this::updateCameraGrid);
    }
    
    private void addDefaultCameras() {
        cameraIds.clear();
        cameraIds.add("cam_entrada");
        cameraIds.add("cam_jardin");
        cameraIds.add("cam_garage");
        updateCameraGrid();
    }
    
    /**
     * Actualizar grid de cámaras
     */
    private void updateCameraGrid() {
        camerasGridPanel.removeAll();
        cameraFeeds.clear();
        
        for (String cameraId : cameraIds) {
            CameraFeedPanel feedPanel = new CameraFeedPanel(cameraId);
            cameraFeeds.put(cameraId, feedPanel);
            camerasGridPanel.add(feedPanel);
        }
        
        // Si no hay cámaras, mostrar mensaje
        if (cameraIds.isEmpty()) {
            JLabel noCamera = new JLabel("No hay cámaras disponibles. Inicia Unity y el streaming.", 
                                        SwingConstants.CENTER);
            noCamera.setForeground(textSecondary);
            noCamera.setFont(new Font("Segoe UI", Font.ITALIC, 14));
            camerasGridPanel.add(noCamera);
        }
        
        camerasGridPanel.revalidate();
        camerasGridPanel.repaint();
    }
    
    /**
     * Iniciar todos los streams
     */
    public void startAllStreams() {
        for (CameraFeedPanel feed : cameraFeeds.values()) {
            feed.startStream();
        }
    }
    
    /**
     * Detener todos los streams
     */
    public void stopAllStreams() {
        for (CameraFeedPanel feed : cameraFeeds.values()) {
            feed.stopStream();
        }
    }
    
    /**
     * Mostrar cámara en fullscreen
     */
    public void showFullscreen(String cameraId) {
        // TODO: Implementar vista fullscreen
    }
    
    /**
     * Salir de fullscreen
     */
    public void exitFullscreen() {
        fullscreenCameraId = null;
        fullscreenPanel.setVisible(false);
    }
    
    /**
     * Limpiar recursos
     */
    public void cleanup() {
        stopAllStreams();
    }
    
    // ═══════════════════════════════════════════════════════════════
    // CLASE INTERNA: Panel individual para cada cámara
    // ═══════════════════════════════════════════════════════════════
    
    private class CameraFeedPanel extends JPanel {
        private String cameraId;
        private JLabel imageLabel;
        private JLabel statusLabel;
        private JLabel fpsLabel;
        private JButton playPauseBtn;
        private JButton fullscreenBtn;
        private JButton snapshotBtn;
        
        private volatile boolean streaming = false;
        private Thread streamThread;
        private BufferedImage currentFrame;
        private int frameCount = 0;
        private long lastFpsTime = System.currentTimeMillis();
        private int currentFps = 0;
        
        public CameraFeedPanel(String cameraId) {
            this.cameraId = cameraId;
            
            setLayout(new BorderLayout());
            setBackground(bgCard);
            setBorder(BorderFactory.createCompoundBorder(
                BorderFactory.createLineBorder(accent.darker(), 1),
                BorderFactory.createEmptyBorder(8, 8, 8, 8)
            ));
            setPreferredSize(new Dimension(320, 280));
            
            createFeedUI();
        }
        
        private void createFeedUI() {
            // Header con nombre y estado
            JPanel header = new JPanel(new BorderLayout());
            header.setBackground(bgCard);
            
            JLabel nameLabel = new JLabel("📹 " + formatCameraName(cameraId));
            nameLabel.setFont(new Font("Segoe UI", Font.BOLD, 13));
            nameLabel.setForeground(textPrimary);
            header.add(nameLabel, BorderLayout.WEST);
            
            statusLabel = new JLabel("⏹ Detenido");
            statusLabel.setFont(new Font("Segoe UI", Font.PLAIN, 11));
            statusLabel.setForeground(Color.GRAY);
            header.add(statusLabel, BorderLayout.EAST);
            
            add(header, BorderLayout.NORTH);
            
            // Área de imagen
            imageLabel = new JLabel();
            imageLabel.setHorizontalAlignment(SwingConstants.CENTER);
            imageLabel.setBackground(Color.BLACK);
            imageLabel.setOpaque(true);
            imageLabel.setPreferredSize(new Dimension(300, 180));
            
            // Texto inicial
            imageLabel.setText("Click ▶ para iniciar");
            imageLabel.setForeground(Color.GRAY);
            imageLabel.setFont(new Font("Segoe UI", Font.ITALIC, 12));
            
            // Click para fullscreen
            imageLabel.addMouseListener(new MouseAdapter() {
                @Override
                public void mouseClicked(MouseEvent e) {
                    if (e.getClickCount() == 2 && streaming) {
                        showFullscreen(cameraId);
                    }
                }
            });
            
            add(imageLabel, BorderLayout.CENTER);
            
            // Panel de controles
            JPanel controls = new JPanel(new FlowLayout(FlowLayout.LEFT, 5, 5));
            controls.setBackground(bgCard);
            
            playPauseBtn = new JButton("▶");
            playPauseBtn.setFont(new Font("Segoe UI", Font.BOLD, 14));
            playPauseBtn.setPreferredSize(new Dimension(45, 30));
            playPauseBtn.setBackground(new Color(74, 222, 128));
            playPauseBtn.setForeground(Color.BLACK);
            playPauseBtn.setFocusPainted(false);
            playPauseBtn.setToolTipText("Iniciar/Pausar stream");
            playPauseBtn.addActionListener(e -> toggleStream());
            controls.add(playPauseBtn);
            
            snapshotBtn = new JButton("📷");
            snapshotBtn.setPreferredSize(new Dimension(45, 30));
            snapshotBtn.setToolTipText("Guardar captura");
            snapshotBtn.addActionListener(e -> saveSnapshot());
            controls.add(snapshotBtn);
            
            fullscreenBtn = new JButton("⛶");
            fullscreenBtn.setPreferredSize(new Dimension(45, 30));
            fullscreenBtn.setToolTipText("Pantalla completa");
            fullscreenBtn.addActionListener(e -> showFullscreen(cameraId));
            controls.add(fullscreenBtn);
            
            fpsLabel = new JLabel("0 FPS");
            fpsLabel.setFont(new Font("Segoe UI", Font.PLAIN, 10));
            fpsLabel.setForeground(textSecondary);
            controls.add(fpsLabel);
            
            add(controls, BorderLayout.SOUTH);
        }
        
        private String formatCameraName(String id) {
            // Convertir cam_entrada -> Cámara Entrada
            return id.replace("cam_", "Cámara ")
                    .replace("_", " ")
                    .substring(0, 1).toUpperCase() + 
                   id.replace("cam_", "Cámara ")
                    .replace("_", " ")
                    .substring(1);
        }
        
        public void toggleStream() {
            if (streaming) {
                stopStream();
            } else {
                startStream();
            }
        }
        
        public void startStream() {
            if (streaming) return;
            
            streaming = true;
            playPauseBtn.setText("⏸");
            playPauseBtn.setBackground(new Color(239, 68, 68));
            statusLabel.setText("🔴 En vivo");
            statusLabel.setForeground(new Color(239, 68, 68));
            imageLabel.setText("");
            
            streamThread = new Thread(this::streamLoop, "Camera-" + cameraId);
            streamThread.setDaemon(true);
            streamThread.start();
        }
        
        public void stopStream() {
            streaming = false;
            playPauseBtn.setText("▶");
            playPauseBtn.setBackground(new Color(74, 222, 128));
            statusLabel.setText("⏹ Detenido");
            statusLabel.setForeground(Color.GRAY);
            fpsLabel.setText("0 FPS");
            
            if (streamThread != null) {
                streamThread.interrupt();
            }
        }
        
        /**
         * Loop de streaming - obtiene frames del servidor
         */
        private void streamLoop() {
            while (streaming) {
                try {
                    // Obtener un frame del servidor
                    URL url = new URL("http://" + serverHost + ":" + streamPort + 
                                     "/camera/frame?id=" + cameraId);
                    HttpURLConnection conn = (HttpURLConnection) url.openConnection();
                    conn.setRequestMethod("GET");
                    conn.setConnectTimeout(2000);
                    conn.setReadTimeout(2000);
                    
                    if (conn.getResponseCode() == 200) {
                        InputStream in = conn.getInputStream();
                        BufferedImage img = ImageIO.read(in);
                        in.close();
                        
                        if (img != null) {
                            currentFrame = img;
                            updateImage(img);
                            updateFps();
                        }
                    } else {
                        // Sin frame disponible, mostrar mensaje
                        SwingUtilities.invokeLater(() -> {
                            if (currentFrame == null) {
                                imageLabel.setIcon(null);
                                imageLabel.setText("Esperando señal...");
                            }
                        });
                    }
                    conn.disconnect();
                    
                    // Delay entre frames (ajustar para ~10-15 FPS)
                    Thread.sleep(80);
                    
                } catch (InterruptedException e) {
                    break;
                } catch (Exception e) {
                    // Error de conexión
                    SwingUtilities.invokeLater(() -> {
                        imageLabel.setIcon(null);
                        imageLabel.setText("Sin conexión");
                        imageLabel.setForeground(Color.RED);
                    });
                    
                    try {
                        Thread.sleep(1000);
                    } catch (InterruptedException ie) {
                        break;
                    }
                }
            }
        }
        
        private void updateImage(BufferedImage img) {
            SwingUtilities.invokeLater(() -> {
                // Escalar imagen para que quepa
                int labelWidth = imageLabel.getWidth();
                int labelHeight = imageLabel.getHeight();
                
                if (labelWidth > 0 && labelHeight > 0) {
                    Image scaled = img.getScaledInstance(labelWidth, labelHeight, 
                                                        Image.SCALE_FAST);
                    imageLabel.setIcon(new ImageIcon(scaled));
                    imageLabel.setText("");
                }
            });
        }
        
        private void updateFps() {
            frameCount++;
            long now = System.currentTimeMillis();
            if (now - lastFpsTime >= 1000) {
                currentFps = frameCount;
                frameCount = 0;
                lastFpsTime = now;
                
                SwingUtilities.invokeLater(() -> {
                    fpsLabel.setText(currentFps + " FPS");
                });
            }
        }
        
        private void saveSnapshot() {
            if (currentFrame == null) {
                JOptionPane.showMessageDialog(this, 
                    "No hay imagen para guardar", "Aviso", 
                    JOptionPane.WARNING_MESSAGE);
                return;
            }
            
            JFileChooser chooser = new JFileChooser();
            chooser.setSelectedFile(new File(cameraId + "_" + 
                System.currentTimeMillis() + ".jpg"));
            
            if (chooser.showSaveDialog(this) == JFileChooser.APPROVE_OPTION) {
                try {
                    File file = chooser.getSelectedFile();
                    if (!file.getName().toLowerCase().endsWith(".jpg")) {
                        file = new File(file.getAbsolutePath() + ".jpg");
                    }
                    ImageIO.write(currentFrame, "jpg", file);
                    JOptionPane.showMessageDialog(this, 
                        "Captura guardada: " + file.getName(), 
                        "Éxito", JOptionPane.INFORMATION_MESSAGE);
                } catch (IOException e) {
                    JOptionPane.showMessageDialog(this, 
                        "Error guardando: " + e.getMessage(), 
                        "Error", JOptionPane.ERROR_MESSAGE);
                }
            }
        }
        
        public BufferedImage getCurrentFrame() {
            return currentFrame;
        }
    }
}
