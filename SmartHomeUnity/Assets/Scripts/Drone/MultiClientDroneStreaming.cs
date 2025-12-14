using UnityEngine;
using Unity.RenderStreaming;
using System.Collections.Generic;

/// <summary>
/// Componente principal que integra Unity Render Streaming con el sistema de drones multi-cliente
/// Debe colocarse en un GameObject vacío en la escena junto con los componentes de Render Streaming
/// 
/// SETUP en Unity:
/// 1. Crear GameObject vacío "DroneStreamingManager"
/// 2. Añadir componente RenderStreaming
/// 3. Añadir componente SignalingManager (HTTP o WebSocket)
/// 4. Añadir este componente (MultiClientDroneStreaming)
/// 5. Asignar el prefab del dron
/// 6. Ejecutar el Signaling Server incluido con el paquete
/// </summary>
public class MultiClientDroneStreaming : MonoBehaviour
{
    [Header("=== Prefab del Dron ===")]
    [Tooltip("Prefab que contiene: Modelo 3D, Cámara, DroneController")]
    [SerializeField] private GameObject dronePrefab;
    
    [Header("=== Configuración de Spawn ===")]
    [SerializeField] private Vector3 spawnCenter = new Vector3(0, 10, 0);
    [SerializeField] private float spawnRadius = 5f;
    
    [Header("=== Configuración de Video ===")]
    [SerializeField] private int streamWidth = 1920;
    [SerializeField] private int streamHeight = 1080;
    [SerializeField] private int frameRate = 60;
    
    [Header("=== Límites del Área ===")]
    [SerializeField] private Vector3 minBounds = new Vector3(-50, 1, -50);
    [SerializeField] private Vector3 maxBounds = new Vector3(50, 30, 50);
    
    [Header("=== Debug ===")]
    [SerializeField] private bool showDebugInfo = true;
    
    // Colores para cada dron
    private Color[] droneColors = new Color[]
    {
        new Color(1f, 0.2f, 0.2f),    // Rojo
        new Color(0.2f, 0.6f, 1f),    // Azul
        new Color(0.2f, 1f, 0.2f),    // Verde
        new Color(1f, 1f, 0.2f),      // Amarillo
        new Color(1f, 0.2f, 1f),      // Magenta
        new Color(0.2f, 1f, 1f),      // Cyan
        new Color(1f, 0.6f, 0.2f),    // Naranja
        new Color(0.6f, 0.2f, 1f)     // Púrpura
    };
    
    // Diccionario de clientes conectados
    private Dictionary<string, ClientDroneConnection> clientConnections = new Dictionary<string, ClientDroneConnection>();
    private int colorIndex = 0;
    
    [System.Serializable]
    public class ClientDroneConnection
    {
        public string connectionId;
        public GameObject droneObject;
        public DroneController controller;
        public Camera droneCamera;
        public RenderTexture renderTexture;
        public Color droneColor;
        
        // Input del cliente
        public float horizontal;
        public float vertical;
        public float elevation;
        public float rotation;
    }
    
    void Start()
    {
        if (dronePrefab == null)
        {
            Debug.LogError("[MultiClientDrone] ¡FALTA ASIGNAR EL PREFAB DEL DRON!");
            return;
        }
        
        Debug.Log("===========================================");
        Debug.Log("[MultiClientDrone] Sistema Multi-Dron Iniciado");
        Debug.Log($"  Resolución Stream: {streamWidth}x{streamHeight}");
        Debug.Log($"  Frame Rate: {frameRate} fps");
        Debug.Log($"  Centro de Spawn: {spawnCenter}");
        Debug.Log("===========================================");
    }
    
    void Update()
    {
        // Actualizar input de cada dron
        foreach (var kvp in clientConnections)
        {
            var conn = kvp.Value;
            if (conn.controller != null)
            {
                conn.controller.SetInput(
                    conn.horizontal,
                    conn.vertical,
                    conn.elevation,
                    conn.rotation
                );
            }
        }
    }
    
    /// <summary>
    /// Llamar cuando un nuevo cliente se conecta
    /// (Conectar desde SignalingManager.OnConnect)
    /// </summary>
    public ClientDroneConnection OnClientConnect(string connectionId)
    {
        if (clientConnections.ContainsKey(connectionId))
        {
            Debug.LogWarning($"[MultiClientDrone] Cliente {connectionId} ya conectado");
            return clientConnections[connectionId];
        }
        
        Debug.Log($"[MultiClientDrone] 🚁 Nuevo cliente: {connectionId}");
        
        // Calcular posición de spawn
        Vector3 spawnPos = CalculateSpawnPosition();
        
        // Instanciar dron
        GameObject droneObj = Instantiate(dronePrefab, spawnPos, Quaternion.identity);
        droneObj.name = $"Drone_{connectionId.Substring(0, 8)}";
        
        // Configurar controlador
        DroneController controller = droneObj.GetComponent<DroneController>();
        if (controller == null)
        {
            controller = droneObj.AddComponent<DroneController>();
        }
        
        // Obtener color único
        Color color = droneColors[colorIndex % droneColors.Length];
        colorIndex++;
        
        // Configurar dron
        controller.SetOwner(connectionId);
        controller.SetDroneColor(color);
        controller.SetBoundaries(minBounds, maxBounds);
        
        // Crear RenderTexture para el stream
        RenderTexture rt = new RenderTexture(streamWidth, streamHeight, 24, RenderTextureFormat.BGRA32);
        rt.name = $"DroneRT_{connectionId.Substring(0, 8)}";
        rt.Create();
        
        // Obtener/crear cámara
        Camera droneCam = droneObj.GetComponentInChildren<Camera>();
        if (droneCam == null)
        {
            GameObject camObj = new GameObject("DroneCamera");
            camObj.transform.SetParent(droneObj.transform);
            camObj.transform.localPosition = new Vector3(0, 0.3f, -0.5f);
            camObj.transform.localRotation = Quaternion.Euler(10, 0, 0);
            droneCam = camObj.AddComponent<Camera>();
            droneCam.fieldOfView = 90;
        }
        
        // Asignar RenderTexture a la cámara
        droneCam.targetTexture = rt;
        
        // Desactivar AudioListener si hay más de un dron
        AudioListener listener = droneObj.GetComponentInChildren<AudioListener>();
        if (listener != null && clientConnections.Count > 0)
        {
            listener.enabled = false;
        }
        
        // Crear y guardar conexión
        ClientDroneConnection conn = new ClientDroneConnection
        {
            connectionId = connectionId,
            droneObject = droneObj,
            controller = controller,
            droneCamera = droneCam,
            renderTexture = rt,
            droneColor = color
        };
        
        clientConnections[connectionId] = conn;
        
        Debug.Log($"[MultiClientDrone] ✅ Dron creado - Color: {ColorToHex(color)} - Total: {clientConnections.Count}");
        
        return conn;
    }
    
    /// <summary>
    /// Llamar cuando un cliente se desconecta
    /// (Conectar desde SignalingManager.OnDisconnect)
    /// </summary>
    public void OnClientDisconnect(string connectionId)
    {
        if (!clientConnections.TryGetValue(connectionId, out ClientDroneConnection conn))
        {
            Debug.LogWarning($"[MultiClientDrone] Cliente {connectionId} no encontrado");
            return;
        }
        
        Debug.Log($"[MultiClientDrone] 👋 Cliente desconectado: {connectionId}");
        
        // Liberar RenderTexture
        if (conn.renderTexture != null)
        {
            conn.renderTexture.Release();
            Destroy(conn.renderTexture);
        }
        
        // Destruir dron
        if (conn.droneObject != null)
        {
            Destroy(conn.droneObject);
        }
        
        clientConnections.Remove(connectionId);
        
        Debug.Log($"[MultiClientDrone] ❌ Dron destruido - Quedan: {clientConnections.Count}");
    }
    
    /// <summary>
    /// Recibir input de un cliente específico
    /// (Conectar desde InputReceiver)
    /// </summary>
    public void OnClientInput(string connectionId, float horizontal, float vertical, float elevation, float rotation)
    {
        if (clientConnections.TryGetValue(connectionId, out ClientDroneConnection conn))
        {
            conn.horizontal = horizontal;
            conn.vertical = vertical;
            conn.elevation = elevation;
            conn.rotation = rotation;
        }
    }
    
    /// <summary>
    /// Obtener la RenderTexture de un cliente para enviar por VideoStreamSender
    /// </summary>
    public RenderTexture GetClientRenderTexture(string connectionId)
    {
        if (clientConnections.TryGetValue(connectionId, out ClientDroneConnection conn))
        {
            return conn.renderTexture;
        }
        return null;
    }
    
    /// <summary>
    /// Obtener el DroneController de un cliente específico
    /// </summary>
    public DroneController GetDroneForConnection(string connectionId)
    {
        if (clientConnections.TryGetValue(connectionId, out ClientDroneConnection conn))
        {
            return conn.controller;
        }
        return null;
    }
    
    /// <summary>
    /// Obtener información de todos los drones activos
    /// </summary>
    public List<DroneInfo> GetActiveDrones()
    {
        List<DroneInfo> drones = new List<DroneInfo>();
        
        foreach (var kvp in clientConnections)
        {
            var conn = kvp.Value;
            if (conn.droneObject != null)
            {
                drones.Add(new DroneInfo
                {
                    id = kvp.Key,
                    position = conn.droneObject.transform.position,
                    rotation = conn.droneObject.transform.eulerAngles,
                    color = ColorToHex(conn.droneColor)
                });
            }
        }
        
        return drones;
    }
    
    [System.Serializable]
    public class DroneInfo
    {
        public string id;
        public Vector3 position;
        public Vector3 rotation;
        public string color;
    }
    
    /// <summary>
    /// Respawnear un dron
    /// </summary>
    public void RespawnClient(string connectionId)
    {
        if (clientConnections.TryGetValue(connectionId, out ClientDroneConnection conn))
        {
            Vector3 newPos = CalculateSpawnPosition();
            conn.controller?.Teleport(newPos);
            Debug.Log($"[MultiClientDrone] 🔄 Respawn: {connectionId} -> {newPos}");
        }
    }
    
    private Vector3 CalculateSpawnPosition()
    {
        float angle = clientConnections.Count * (360f / 8f) * Mathf.Deg2Rad;
        float x = spawnCenter.x + Mathf.Cos(angle) * spawnRadius;
        float z = spawnCenter.z + Mathf.Sin(angle) * spawnRadius;
        return new Vector3(x, spawnCenter.y, z);
    }
    
    private string ColorToHex(Color color)
    {
        return $"#{ColorUtility.ToHtmlStringRGB(color)}";
    }
    
    void OnDestroy()
    {
        // Limpiar todo
        foreach (var kvp in clientConnections)
        {
            if (kvp.Value.renderTexture != null)
            {
                kvp.Value.renderTexture.Release();
            }
            if (kvp.Value.droneObject != null)
            {
                Destroy(kvp.Value.droneObject);
            }
        }
        clientConnections.Clear();
    }
    
    void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label($"🚁 DRONES ACTIVOS: {clientConnections.Count}");
        GUILayout.Label($"📺 Stream: {streamWidth}x{streamHeight} @ {frameRate}fps");
        
        foreach (var kvp in clientConnections)
        {
            var conn = kvp.Value;
            if (conn.droneObject != null)
            {
                Vector3 pos = conn.droneObject.transform.position;
                GUILayout.Label($"  • {conn.droneObject.name}: ({pos.x:F1}, {pos.y:F1}, {pos.z:F1})");
            }
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
    
    void OnDrawGizmos()
    {
        // Dibujar área de spawn
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(spawnCenter, spawnRadius);
        
        // Dibujar límites
        Gizmos.color = Color.yellow;
        Vector3 center = (minBounds + maxBounds) / 2f;
        Vector3 size = maxBounds - minBounds;
        Gizmos.DrawWireCube(center, size);
    }
}
