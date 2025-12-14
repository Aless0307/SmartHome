using UnityEngine;
using Unity.RenderStreaming;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Gestiona las conexiones de streaming WebRTC para múltiples drones
/// Integra Unity Render Streaming con el sistema de drones
/// </summary>
public class DroneStreamManager : MonoBehaviour
{
    [Header("Render Streaming")]
    // RenderStreaming es una clase estática en algunas versiones o un componente en otras.
    // Si da error CS0723, es porque en tu versión es estática y no necesitas una referencia.
    // [SerializeField] private RenderStreaming renderStreaming; 
    [SerializeField] private SignalingManager signalingManager;
    
    [Header("Stream Settings")]
    [SerializeField] private int streamWidth = 1920;
    [SerializeField] private int streamHeight = 1080;
    [SerializeField] private int frameRate = 60;
    [SerializeField] private ulong bitrate = 10000000; // 10 Mbps
    
    [Header("References")]
    [SerializeField] private DroneSpawner droneSpawner;
    
    // Diccionario de streams activos por conexión
    private Dictionary<string, DroneStreamConnection> activeConnections = new Dictionary<string, DroneStreamConnection>();
    
    // Singleton
    public static DroneStreamManager Instance { get; private set; }
    
    [System.Serializable]
    public class DroneStreamConnection
    {
        public string connectionId;
        public VideoStreamSender videoSender;
        public AudioStreamSender audioSender;
        public InputReceiver inputReceiver;
        public RenderTexture renderTexture;
        public DroneSpawner.DroneInstance droneInstance;
    }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Obtener referencias si no están asignadas
        /*
        if (renderStreaming == null)
        {
            renderStreaming = FindObjectOfType<RenderStreaming>();
        }
        */
        
        if (signalingManager == null)
        {
            signalingManager = FindObjectOfType<SignalingManager>();
        }
        
        if (droneSpawner == null)
        {
            droneSpawner = FindObjectOfType<DroneSpawner>();
        }
        
        // Suscribirse a eventos de DroneSpawner
        if (droneSpawner != null)
        {
            droneSpawner.OnDroneSpawned += OnDroneSpawned;
            droneSpawner.OnDroneDespawned += OnDroneDespawned;
        }
        
        Debug.Log("[DroneStreamManager] Sistema de streaming inicializado");
        Debug.Log($"[DroneStreamManager] Resolución: {streamWidth}x{streamHeight} @ {frameRate}fps");
    }
    
    /// <summary>
    /// Llamado cuando un nuevo cliente se conecta
    /// Esta función debe ser llamada por el sistema de signaling
    /// </summary>
    public void OnClientConnected(string connectionId)
    {
        Debug.Log($"[DroneStreamManager] Cliente conectado: {connectionId}");
        
        // Crear dron para el cliente
        var droneInstance = droneSpawner.SpawnDrone(connectionId);
        
        if (droneInstance == null)
        {
            Debug.LogError($"[DroneStreamManager] No se pudo crear dron para {connectionId}");
            return;
        }
        
        // Configurar streaming para este dron
        StartCoroutine(SetupStreamingForDrone(connectionId, droneInstance));
    }
    
    /// <summary>
    /// Llamado cuando un cliente se desconecta
    /// </summary>
    public void OnClientDisconnected(string connectionId)
    {
        Debug.Log($"[DroneStreamManager] Cliente desconectado: {connectionId}");
        
        // Limpiar conexión de streaming
        CleanupConnection(connectionId);
        
        // Destruir dron
        droneSpawner.DespawnDrone(connectionId);
    }
    
    /// <summary>
    /// Configurar el streaming de video para un dron
    /// </summary>
    private IEnumerator SetupStreamingForDrone(string connectionId, DroneSpawner.DroneInstance droneInstance)
    {
        yield return new WaitForEndOfFrame();
        
        // Crear RenderTexture para la cámara del dron
        RenderTexture rt = new RenderTexture(streamWidth, streamHeight, 24, RenderTextureFormat.BGRA32);
        rt.Create();
        
        // Asignar RenderTexture a la cámara del dron
        if (droneInstance.droneCamera != null)
        {
            droneInstance.droneCamera.targetTexture = rt;
        }
        
        // Crear VideoStreamSender para este dron
        GameObject streamObj = new GameObject($"StreamSender_{connectionId}");
        streamObj.transform.SetParent(droneInstance.droneObject.transform);
        
        VideoStreamSender videoSender = streamObj.AddComponent<VideoStreamSender>();
        // Configurar video sender
        if (rt != null)
        {
            // Intentar asignar la fuente de video usando reflexión para máxima compatibilidad
            bool assigned = false;
            var type = videoSender.GetType();
            
            // 1. Intentar propiedad 'source'
            var prop = type.GetProperty("source");
            if (prop != null && prop.CanWrite) {
                try { prop.SetValue(videoSender, rt); assigned = true; } catch {}
            }
            
            // 2. Intentar método SetSource (si existe)
            if (!assigned) {
                var method = type.GetMethod("SetSource", new System.Type[] { typeof(RenderTexture) });
                if (method != null) {
                    try { method.Invoke(videoSender, new object[] { rt }); assigned = true; } catch {}
                }
            }

            // 3. Intentar campos privados/públicos
            if (!assigned) {
                foreach (var field in type.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
                {
                    if (field.FieldType == typeof(Texture) || field.FieldType == typeof(RenderTexture) || field.Name == "m_source")
                    {
                        try { field.SetValue(videoSender, rt); assigned = true; break; } catch {}
                    }
                }
            }
            
            if (assigned) Debug.Log($"[DroneStreamManager] ✅ Source asignado a VideoStreamSender para {connectionId}");
            else Debug.LogWarning($"[DroneStreamManager] ⚠️ No se pudo asignar source a VideoStreamSender para {connectionId}");
        }
        
        // Crear InputReceiver para recibir controles
        InputReceiver inputReceiver = streamObj.AddComponent<InputReceiver>();
        
        // Crear conexión
        DroneStreamConnection connection = new DroneStreamConnection
        {
            connectionId = connectionId,
            videoSender = videoSender,
            inputReceiver = inputReceiver,
            renderTexture = rt,
            droneInstance = droneInstance
        };
        
        activeConnections[connectionId] = connection;
        
        Debug.Log($"[DroneStreamManager] Streaming configurado para {connectionId}");
    }
    
    /// <summary>
    /// Limpiar recursos de una conexión
    /// </summary>
    private void CleanupConnection(string connectionId)
    {
        if (!activeConnections.TryGetValue(connectionId, out DroneStreamConnection connection))
        {
            return;
        }
        
        // Liberar RenderTexture
        if (connection.renderTexture != null)
        {
            connection.renderTexture.Release();
            Destroy(connection.renderTexture);
        }
        
        activeConnections.Remove(connectionId);
        
        Debug.Log($"[DroneStreamManager] Conexión limpiada: {connectionId}");
    }
    
    /// <summary>
    /// Callback cuando se crea un dron
    /// </summary>
    private void OnDroneSpawned(DroneSpawner.DroneInstance drone)
    {
        Debug.Log($"[DroneStreamManager] Dron spawneado: {drone.connectionId}");
    }
    
    /// <summary>
    /// Callback cuando se destruye un dron
    /// </summary>
    private void OnDroneDespawned(DroneSpawner.DroneInstance drone)
    {
        Debug.Log($"[DroneStreamManager] Dron despawneado: {drone.connectionId}");
    }
    
    /// <summary>
    /// Procesar input recibido del cliente
    /// </summary>
    public void ProcessInput(string connectionId, Vector2 movement, float elevation, float rotation)
    {
        if (activeConnections.TryGetValue(connectionId, out DroneStreamConnection connection))
        {
            if (connection.droneInstance?.controller != null)
            {
                connection.droneInstance.controller.SetInput(
                    movement.x,
                    movement.y,
                    elevation,
                    rotation
                );
            }
        }
    }
    
    /// <summary>
    /// Obtener estadísticas de streaming
    /// </summary>
    public string GetStats()
    {
        return $"Conexiones activas: {activeConnections.Count}\n" +
               $"Resolución: {streamWidth}x{streamHeight}\n" +
               $"Frame Rate: {frameRate} fps\n" +
               $"Bitrate: {bitrate / 1000000} Mbps";
    }
    
    void OnDestroy()
    {
        // Limpiar todas las conexiones
        List<string> keys = new List<string>(activeConnections.Keys);
        foreach (string key in keys)
        {
            CleanupConnection(key);
        }
        
        // Desuscribirse de eventos
        if (droneSpawner != null)
        {
            droneSpawner.OnDroneSpawned -= OnDroneSpawned;
            droneSpawner.OnDroneDespawned -= OnDroneDespawned;
        }
    }
}
