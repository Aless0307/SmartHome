using UnityEngine;
using Unity.RenderStreaming;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Handler de conexiones que integra con Unity Render Streaming
/// Este componente debe estar en el mismo objeto que los componentes de Render Streaming
/// 
/// CONFIGURACIÓN:
/// 1. Añadir a un GameObject con RenderStreaming y SignalingManager
/// 2. El Broadcast componente manejará múltiples streams automáticamente
/// </summary>
public class DroneConnectionHandler : MonoBehaviour
{
    [Header("=== Multi-Client Streaming ===")]
    [SerializeField] private MultiClientDroneStreaming droneManager;
    
    [Header("=== Render Streaming Components ===")]
    [SerializeField] private SignalingManager signalingManager;
    
    [Header("=== Stream Settings ===")]
    [SerializeField] private bool autoStartStreaming = true;
    
    // Diccionario para rastrear conexiones y sus streams
    private Dictionary<string, StreamConnection> streamConnections = new Dictionary<string, StreamConnection>();
    
    private class StreamConnection
    {
        public string connectionId;
        public VideoStreamSender videoSender;
        public AudioStreamSender audioSender;
        public bool isActive;
    }
    
    void Start()
    {
        if (droneManager == null)
        {
            droneManager = FindObjectOfType<MultiClientDroneStreaming>();
        }
        
        if (signalingManager == null)
        {
            signalingManager = GetComponent<SignalingManager>();
        }
        
        // Suscribirse a eventos de signaling si están disponibles
        if (signalingManager != null)
        {
            // Los eventos dependen de la versión del paquete
            // signalingManager.OnConnect += OnPeerConnect;
            // signalingManager.OnDisconnect += OnPeerDisconnect;
        }
        
        Debug.Log("[DroneConnectionHandler] Inicializado y esperando conexiones...");
    }
    
    /// <summary>
    /// Llamar cuando se conecta un peer (desde signaling)
    /// </summary>
    public void OnPeerConnect(string connectionId)
    {
        Debug.Log($"[DroneConnectionHandler] Peer conectado: {connectionId}");
        
        if (droneManager != null)
        {
            // Crear dron para el cliente
            var droneConn = droneManager.OnClientConnect(connectionId);
            
            if (droneConn != null)
            {
                // Crear stream para este cliente
                StartCoroutine(SetupStreamForClient(connectionId, droneConn));
            }
        }
    }
    
    /// <summary>
    /// Llamar cuando se desconecta un peer
    /// </summary>
    public void OnPeerDisconnect(string connectionId)
    {
        Debug.Log($"[DroneConnectionHandler] Peer desconectado: {connectionId}");
        
        // Limpiar stream
        if (streamConnections.TryGetValue(connectionId, out StreamConnection conn))
        {
            if (conn.videoSender != null)
            {
                Destroy(conn.videoSender.gameObject);
            }
            streamConnections.Remove(connectionId);
        }
        
        // Destruir dron
        if (droneManager != null)
        {
            droneManager.OnClientDisconnect(connectionId);
        }
    }
    
    /// <summary>
    /// Configurar el stream de video para un cliente
    /// </summary>
    private IEnumerator SetupStreamForClient(string connectionId, MultiClientDroneStreaming.ClientDroneConnection droneConn)
    {
        yield return new WaitForEndOfFrame();
        
        // Crear objeto para el VideoStreamSender
        GameObject streamObj = new GameObject($"Stream_{connectionId.Substring(0, 8)}");
        streamObj.transform.SetParent(transform);
        
        // Añadir VideoStreamSender
        VideoStreamSender videoSender = streamObj.AddComponent<VideoStreamSender>();
        
        // Configurar el sender con la RenderTexture del dron
        if (droneConn.renderTexture != null)
        {
            // Intentar asignar la fuente de video usando reflexión para máxima compatibilidad
            bool assigned = false;
            var type = videoSender.GetType();
            
            // 1. Intentar propiedad 'source'
            var prop = type.GetProperty("source");
            if (prop != null && prop.CanWrite) {
                try { prop.SetValue(videoSender, droneConn.renderTexture); assigned = true; } catch {}
            }
            
            // 2. Intentar método SetSource (si existe)
            if (!assigned) {
                var method = type.GetMethod("SetSource", new System.Type[] { typeof(RenderTexture) });
                if (method != null) {
                    try { method.Invoke(videoSender, new object[] { droneConn.renderTexture }); assigned = true; } catch {}
                }
            }

            // 3. Intentar campos privados/públicos
            if (!assigned) {
                foreach (var field in type.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
                {
                    if (field.FieldType == typeof(Texture) || field.FieldType == typeof(RenderTexture) || field.Name == "m_source")
                    {
                        try { field.SetValue(videoSender, droneConn.renderTexture); assigned = true; break; } catch {}
                    }
                }
            }
            
            if (assigned) Debug.Log($"[DroneConnectionHandler] ✅ Source asignado a VideoStreamSender para {connectionId}");
            else Debug.LogWarning($"[DroneConnectionHandler] ⚠️ No se pudo asignar source a VideoStreamSender para {connectionId}");
        }
        
        StreamConnection streamConn = new StreamConnection
        {
            connectionId = connectionId,
            videoSender = videoSender,
            isActive = true
        };
        
        streamConnections[connectionId] = streamConn;
        
        Debug.Log($"[DroneConnectionHandler] Stream configurado para {connectionId}");
    }
    
    /// <summary>
    /// Procesar input recibido de un cliente
    /// Llamar desde el data channel del WebRTC
    /// </summary>
    public void ProcessClientInput(string connectionId, string jsonInput)
    {
        try
        {
            var input = JsonUtility.FromJson<DroneInput>(jsonInput);
            
            if (droneManager != null)
            {
                droneManager.OnClientInput(
                    connectionId,
                    input.horizontal,
                    input.vertical,
                    input.elevation,
                    input.rotation
                );
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DroneConnectionHandler] Error procesando input: {e.Message}");
        }
    }
    
    [System.Serializable]
    private class DroneInput
    {
        public float horizontal;
        public float vertical;
        public float elevation;
        public float rotation;
    }
    
    /// <summary>
    /// Obtener lista de drones activos (para enviar a clientes)
    /// </summary>
    public string GetDronesListJson()
    {
        if (droneManager == null) return "[]";
        
        var drones = droneManager.GetActiveDrones();
        return JsonUtility.ToJson(new DronesList { drones = drones });
    }
    
    [System.Serializable]
    private class DronesList
    {
        public List<MultiClientDroneStreaming.DroneInfo> drones;
    }
    
    void OnDestroy()
    {
        // Limpiar todas las conexiones
        foreach (var conn in streamConnections.Values.ToList())
        {
            if (conn.videoSender != null)
            {
                Destroy(conn.videoSender.gameObject);
            }
        }
        streamConnections.Clear();
    }
}
