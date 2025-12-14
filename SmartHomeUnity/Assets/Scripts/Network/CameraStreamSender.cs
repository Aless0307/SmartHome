using UnityEngine;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// ═══════════════════════════════════════════════════════════════
/// CAMERA STREAM SENDER - Envía frames HD de cámaras al servidor Java
/// ═══════════════════════════════════════════════════════════════
/// 
/// OPTIMIZADO: Solo hace stream de cámaras encendidas.
/// Cada cámara tiene su propio stream independiente.
/// Cuando se apaga una cámara, se detiene su stream.
/// 
/// Características:
/// - Stream individual por cámara (ahorra recursos)
/// - Solo envía frames de cámaras encendidas
/// - FPS reducido para mejor rendimiento
/// - Reconexión automática
/// </summary>
public class CameraStreamSender : MonoBehaviour
{
    [Header("Configuración de Red")]
    [Tooltip("IP del servidor Java")]
    public string serverIP = "127.0.0.1";
    
    [Tooltip("Puerto TCP del servidor de streaming HD")]
    public int serverPort = 8083;

    [Header("Configuración de Streaming")]
    [Tooltip("Frames por segundo a enviar (menor = menos lag)")]
    [Range(1, 30)]
    public int targetFPS = 15;
    
    [Tooltip("Calidad JPEG (1-100)")]
    [Range(1, 100)]
    public int jpegQuality = 75;
    
    [Tooltip("Ancho máximo del stream")]
    public int maxStreamWidth = 1280;
    
    [Tooltip("Alto máximo del stream")]
    public int maxStreamHeight = 720;

    [Header("Estado")]
    public bool isConnected = false;
    public int activeStreams = 0;
    public string connectionStatus = "Desconectado";
    
    [Header("Control")]
    [Tooltip("Intentar reconectar automáticamente")]
    public bool autoReconnect = true;
    
    [Tooltip("Intervalo entre intentos de reconexión (segundos)")]
    public float reconnectInterval = 5f;

    // TCP Client (compartido entre todas las cámaras)
    private TcpClient tcpClient;
    private NetworkStream networkStream;
    private BinaryWriter binaryWriter;
    private readonly object writeLock = new object();
    
    // Cámaras y sus estados de streaming
    private Dictionary<SecurityCamera, CameraStreamState> cameraStates = new Dictionary<SecurityCamera, CameraStreamState>();
    
    // Control de conexión
    private Coroutine connectionCoroutine;
    private Coroutine streamCoroutine;
    
    // Singleton
    private static CameraStreamSender _instance;
    public static CameraStreamSender Instance => _instance;
    
    // Estado de streaming por cámara
    private class CameraStreamState
    {
        public Texture2D texture;
        public bool wasOn;
        public float lastFrameTime;
    }

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Esperar más tiempo para que los dispositivos se carguen del servidor
        // Los dispositivos vienen de MongoDB y tardan unos segundos
        Invoke("DetectCameras", 4f);
        
        // Iniciar sistema de streaming después de detectar cámaras
        Invoke("StartStreamSystem", 5f);
    }

    /// <summary>
    /// Detectar todas las SecurityCamera en la escena
    /// </summary>
    public void DetectCameras()
    {
        cameraStates.Clear();
        
        SecurityCamera[] foundCameras = FindObjectsByType<SecurityCamera>(FindObjectsSortMode.None);
        
        foreach (var cam in foundCameras)
        {
            // Calcular tamaño de streaming manteniendo aspect ratio
            float aspectRatio = (float)cam.renderWidth / cam.renderHeight;
            int streamWidth = Mathf.Min(cam.renderWidth, maxStreamWidth);
            int streamHeight = Mathf.RoundToInt(streamWidth / aspectRatio);
            
            if (streamHeight > maxStreamHeight)
            {
                streamHeight = maxStreamHeight;
                streamWidth = Mathf.RoundToInt(streamHeight * aspectRatio);
            }
            
            var state = new CameraStreamState
            {
                texture = new Texture2D(streamWidth, streamHeight, TextureFormat.RGB24, false),
                wasOn = cam.isCameraOn, // Inicializar con estado REAL de la cámara
                lastFrameTime = 0
            };
            
            cameraStates[cam] = state;
            Debug.Log($"📹 {cam.cameraName}: Preparado para stream {streamWidth}x{streamHeight} (estado: {(cam.isCameraOn ? "ON" : "OFF")})");
        }
        
        Debug.Log($"📹 CameraStreamSender: {cameraStates.Count} cámaras detectadas (stream on-demand)");
    }

    /// <summary>
    /// Iniciar el sistema de streaming
    /// </summary>
    public void StartStreamSystem()
    {
        if (streamCoroutine != null) return;
        
        Debug.Log("📹 Sistema de streaming iniciado (on-demand)");
        streamCoroutine = StartCoroutine(StreamSystemCoroutine());
    }

    /// <summary>
    /// Detener el sistema de streaming
    /// </summary>
    public void StopStreamSystem()
    {
        if (streamCoroutine != null)
        {
            StopCoroutine(streamCoroutine);
            streamCoroutine = null;
        }
        
        if (connectionCoroutine != null)
        {
            StopCoroutine(connectionCoroutine);
            connectionCoroutine = null;
        }
        
        Disconnect();
        Debug.Log("📹 Sistema de streaming detenido");
    }

    /// <summary>
    /// Coroutine principal del sistema de streaming
    /// </summary>
    private IEnumerator StreamSystemCoroutine()
    {
        float frameInterval = 1f / targetFPS;
        
        while (true)
        {
            int streaming = 0;
            
            // Verificar cada cámara
            foreach (var kvp in cameraStates)
            {
                SecurityCamera cam = kvp.Key;
                CameraStreamState state = kvp.Value;
                
                if (cam == null) continue;
                
                // Detectar cambio de estado
                bool isOn = cam.isCameraOn;
                
                if (isOn != state.wasOn)
                {
                    if (isOn)
                    {
                        Debug.Log($"📹 {cam.cameraName}: Iniciando stream");
                    }
                    else
                    {
                        Debug.Log($"📹 {cam.cameraName}: Deteniendo stream");
                    }
                    state.wasOn = isOn;
                }
                
                // Solo hacer stream si la cámara está encendida
                if (isOn && cam.GetRenderTexture() != null)
                {
                    float currentTime = Time.time;
                    
                    // Control de FPS por cámara
                    if (currentTime - state.lastFrameTime >= frameInterval)
                    {
                        state.lastFrameTime = currentTime;
                        
                        // Asegurar conexión
                        if (!isConnected)
                        {
                            TryConnect();
                        }
                        
                        // Capturar y enviar frame
                        if (isConnected)
                        {
                            CaptureAndSendFrame(cam, state);
                        }
                    }
                    
                    streaming++;
                }
            }
            
            activeStreams = streaming;
            
            // Si no hay cámaras activas, desconectar para ahorrar recursos
            if (streaming == 0 && isConnected)
            {
                // Mantener conexión por un momento por si se enciende otra cámara
                yield return new WaitForSeconds(5f);
                
                // Verificar de nuevo
                bool anyOn = false;
                foreach (var kvp in cameraStates)
                {
                    if (kvp.Key != null && kvp.Key.isCameraOn)
                    {
                        anyOn = true;
                        break;
                    }
                }
                
                if (!anyOn && isConnected)
                {
                    Debug.Log("📹 No hay cámaras activas, desconectando...");
                    Disconnect();
                }
            }
            
            yield return null;
        }
    }

    /// <summary>
    /// Intentar conectar al servidor
    /// </summary>
    private void TryConnect()
    {
        if (isConnected) return;
        
        try
        {
            if (tcpClient != null)
            {
                try { tcpClient.Close(); } catch { }
            }
            
            tcpClient = new TcpClient();
            tcpClient.NoDelay = true;
            tcpClient.SendBufferSize = 1024 * 1024; // 1MB buffer
            tcpClient.SendTimeout = 1000;
            
            connectionStatus = "Conectando...";
            
            // Conectar sincrónicamente (rápido en localhost)
            tcpClient.Connect(serverIP, serverPort);
            
            networkStream = tcpClient.GetStream();
            binaryWriter = new BinaryWriter(networkStream);
            
            isConnected = true;
            connectionStatus = $"Conectado";
            Debug.Log($"📹 ✓ Conectado al servidor TCP {serverIP}:{serverPort}");
        }
        catch (Exception e)
        {
            connectionStatus = $"Sin conexión";
            isConnected = false;
            // No spamear logs
        }
    }

    /// <summary>
    /// Desconectar del servidor
    /// </summary>
    private void Disconnect()
    {
        isConnected = false;
        
        lock (writeLock)
        {
            try { binaryWriter?.Close(); } catch { }
            try { networkStream?.Close(); } catch { }
            try { tcpClient?.Close(); } catch { }
            
            binaryWriter = null;
            networkStream = null;
            tcpClient = null;
        }
        
        connectionStatus = "Desconectado";
    }

    /// <summary>
    /// Capturar y enviar frame de una cámara
    /// </summary>
    private void CaptureAndSendFrame(SecurityCamera cam, CameraStreamState state)
    {
        try
        {
            RenderTexture rt = cam.GetRenderTexture();
            if (rt == null || state.texture == null) return;
            
            // Crear RenderTexture temporal escalado
            int w = state.texture.width;
            int h = state.texture.height;
            RenderTexture scaledRT = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
            scaledRT.filterMode = FilterMode.Bilinear;
            
            // Copiar y escalar
            Graphics.Blit(rt, scaledRT);
            
            // Leer pixels
            RenderTexture.active = scaledRT;
            state.texture.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            state.texture.Apply();
            RenderTexture.active = null;
            
            // Liberar
            RenderTexture.ReleaseTemporary(scaledRT);
            
            // Convertir a JPEG
            byte[] jpegData = state.texture.EncodeToJPG(jpegQuality);
            
            // Enviar
            SendFrame(cam.cameraId, jpegData);
        }
        catch (Exception e)
        {
            // Silenciar errores frecuentes
        }
    }

    /// <summary>
    /// Enviar frame al servidor
    /// </summary>
    private void SendFrame(string cameraId, byte[] jpegData)
    {
        if (!isConnected || binaryWriter == null) return;
        
        lock (writeLock)
        {
            try
            {
                byte[] idBytes = System.Text.Encoding.UTF8.GetBytes(cameraId + "|");
                int totalLength = idBytes.Length + jpegData.Length;
                
                // Longitud (4 bytes, big-endian)
                byte[] lengthBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(totalLength));
                binaryWriter.Write(lengthBytes);
                binaryWriter.Write(idBytes);
                binaryWriter.Write(jpegData);
                binaryWriter.Flush();
            }
            catch (Exception)
            {
                Disconnect();
            }
        }
    }

    void OnDestroy()
    {
        StopStreamSystem();
        
        // Liberar texturas
        foreach (var state in cameraStates.Values)
        {
            if (state.texture != null) Destroy(state.texture);
        }
        cameraStates.Clear();
    }

    void OnApplicationQuit()
    {
        StopStreamSystem();
    }

    /// <summary>
    /// Actualizar configuración de FPS
    /// </summary>
    public void SetTargetFPS(int fps)
    {
        targetFPS = Mathf.Clamp(fps, 1, 30);
    }

    /// <summary>
    /// Actualizar calidad JPEG
    /// </summary>
    public void SetJpegQuality(int quality)
    {
        jpegQuality = Mathf.Clamp(quality, 1, 100);
    }
}
