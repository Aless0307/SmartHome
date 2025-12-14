using UnityEngine;
using Unity.RenderStreaming;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Puente entre Unity Render Streaming y el sistema de drones multi-cliente
/// Este script detecta conexiones y crea/destruye drones automáticamente
/// </summary>
public class DroneStreamingBridge : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private MultiClientDroneStreaming droneManager;
    [SerializeField] private VideoStreamSender videoStreamSender;
    [SerializeField] private SignalingManager signalingManager;
    
    private Broadcast broadcast;
    private HashSet<string> knownConnections = new HashSet<string>();
    private Dictionary<string, string> streamToClientId = new Dictionary<string, string>();
    private int lastStreamCount = 0;
    private float logTimer = 0f;
    private bool hasTriedAddStream = false;
    
    void Start()
    {
        broadcast = GetComponent<Broadcast>();
        
        if (droneManager == null)
        {
            droneManager = GetComponent<MultiClientDroneStreaming>();
        }
        
        if (videoStreamSender == null)
        {
            videoStreamSender = GetComponent<VideoStreamSender>();
        }
        
        if (signalingManager == null)
        {
            signalingManager = GetComponent<SignalingManager>();
        }
        
        // Log de estado inicial
        Debug.Log("[DroneStreamingBridge] ✅ Inicializado - Esperando conexiones...");
        Debug.Log($"[DroneStreamingBridge] Broadcast: {(broadcast != null ? "OK" : "NULL")}");
        Debug.Log($"[DroneStreamingBridge] SignalingManager: {(signalingManager != null ? "OK" : "NULL")}");
        Debug.Log($"[DroneStreamingBridge] DroneManager: {(droneManager != null ? "OK" : "NULL")}");
        Debug.Log($"[DroneStreamingBridge] VideoStreamSender: {(videoStreamSender != null ? "OK" : "NULL")}");
        
        // Asegurarse de que el VideoStreamSender está añadido al Broadcast
        if (broadcast != null && videoStreamSender != null)
        {
            EnsureVideoStreamInBroadcast();
        }
    }
    
    void EnsureVideoStreamInBroadcast()
    {
        // Intentar añadir el VideoStreamSender a los streams del Broadcast
        try
        {
            // Verificar si ya está añadido
            bool alreadyAdded = false;
            foreach (var stream in broadcast.Streams)
            {
                if (stream == videoStreamSender)
                {
                    alreadyAdded = true;
                    break;
                }
            }
            
            if (!alreadyAdded)
            {
                // Intentar añadir el stream usando reflexión
                var addMethod = broadcast.GetType().GetMethod("AddComponent");
                if (addMethod != null)
                {
                    addMethod.Invoke(broadcast, new object[] { videoStreamSender });
                    Debug.Log("[DroneStreamingBridge] ✅ VideoStreamSender añadido al Broadcast via AddComponent");
                }
                else
                {
                    Debug.Log("[DroneStreamingBridge] ℹ️ AddComponent no disponible, el VideoStreamSender debe añadirse manualmente en el Inspector");
                }
            }
            else
            {
                Debug.Log("[DroneStreamingBridge] ✅ VideoStreamSender ya está en el Broadcast");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[DroneStreamingBridge] ⚠️ Error verificando streams: {e.Message}");
        }
    }
    
    void Update()
    {
        // Log periódico cada 5 segundos
        logTimer += Time.deltaTime;
        if (logTimer >= 5f)
        {
            logTimer = 0f;
            LogStatus();
        }
        
        // Monitorear conexiones reales de clientes (no streams)
        MonitorRealConnections();
    }
    
    /// <summary>
    /// Monitorear conexiones reales usando el SignalingManager o Broadcast
    /// </summary>
    private void MonitorRealConnections()
    {
        if (broadcast == null) return;
        
        // Obtener connectionIds reales de los peers conectados
        var realConnectionIds = GetRealConnectionIds();
        
        // Detectar nuevas conexiones
        foreach (var connId in realConnectionIds)
        {
            if (!knownConnections.Contains(connId))
            {
                knownConnections.Add(connId);
                Debug.Log($"[DroneStreamingBridge] 🔗 Nueva conexión REAL detectada: {connId}");
                OnNewConnection(connId);
            }
        }
        
        // Detectar desconexiones
        var disconnected = knownConnections.Where(id => !realConnectionIds.Contains(id)).ToList();
        foreach (var connId in disconnected)
        {
            knownConnections.Remove(connId);
            Debug.Log($"[DroneStreamingBridge] 🔌 Desconexión: {connId}");
            OnDisconnection(connId);
        }
    }
    
    /// <summary>
    /// Obtener los IDs de conexión reales de los peers
    /// </summary>
    private HashSet<string> GetRealConnectionIds()
    {
        var connectionIds = new HashSet<string>();
        
        try
        {
            // Método 1: Obtener del mapa de PeerConnections del Broadcast
            var type = broadcast.GetType();
            var baseType = type.BaseType; // SignalingHandlerBase
            
            if (baseType != null)
            {
                var pcMapField = baseType.GetField("m_peerConnectionMap", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (pcMapField != null)
                {
                    var pcMap = pcMapField.GetValue(broadcast) as System.Collections.IDictionary;
                    if (pcMap != null)
                    {
                        foreach (var key in pcMap.Keys)
                        {
                            connectionIds.Add(key.ToString());
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[DroneStreamingBridge] Error obteniendo connectionIds: {e.Message}");
        }
        
        return connectionIds;
    }
    
    void LogStatus()
    {
        if (broadcast != null)
        {
            var streams = broadcast.Streams.ToList();
            Debug.Log($"[DroneStreamingBridge] 📊 Status - Streams: {streams.Count}, Known: {knownConnections.Count}");
        }
    }
    
    /// <summary>
    /// Cuando se detecta una nueva conexión
    /// </summary>
    private void OnNewConnection(string connectionId)
    {
        Debug.Log($"[DroneStreamingBridge] 🚁 Creando dron para: {connectionId}");
        
        if (droneManager != null)
        {
            // Crear dron para este cliente
            var droneConnection = droneManager.OnClientConnect(connectionId);
            
            if (droneConnection != null && droneConnection.droneCamera != null)
            {
                // Asignar la cámara del dron al Video Stream Sender
                if (videoStreamSender != null)
                {
                    SetVideoStreamCamera(droneConnection.droneCamera);
                    Debug.Log($"[DroneStreamingBridge] 📹 Cámara del dron asignada al stream");
                }
            }
        }
        else
        {
            Debug.LogWarning("[DroneStreamingBridge] ⚠️ DroneManager es null, no se puede crear dron");
        }
    }
    
    /// <summary>
    /// Cuando se detecta una desconexión
    /// </summary>
    private void OnDisconnection(string clientId)
    {
        Debug.Log($"[DroneStreamingBridge] 🔌 Desconectando cliente: {clientId}");
        
        if (droneManager != null && clientId != null)
        {
            droneManager.OnClientDisconnect(clientId);
        }
    }
    
    /// <summary>
    /// Asignar la cámara al VideoStreamSender
    /// </summary>
    private void SetVideoStreamCamera(Camera camera)
    {
        if (videoStreamSender == null) return;
        
        // Intentar usar el método SetSource si existe
        var setSourceMethod = videoStreamSender.GetType().GetMethod("SetSource");
        if (setSourceMethod != null)
        {
            try
            {
                setSourceMethod.Invoke(videoStreamSender, new object[] { camera });
                Debug.Log("[DroneStreamingBridge] ✅ Cámara asignada via SetSource");
                return;
            }
            catch { }
        }
        
        // Intentar con campo serializado m_Camera
        var cameraField = videoStreamSender.GetType().GetField("m_Camera", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (cameraField != null)
        {
            try
            {
                cameraField.SetValue(videoStreamSender, camera);
                Debug.Log("[DroneStreamingBridge] ✅ Cámara asignada via m_Camera");
                return;
            }
            catch { }
        }
        
        // Intentar con propiedad source
        var sourceProperty = videoStreamSender.GetType().GetProperty("source");
        if (sourceProperty != null && sourceProperty.CanWrite)
        {
            try
            {
                sourceProperty.SetValue(videoStreamSender, camera);
                Debug.Log("[DroneStreamingBridge] ✅ Cámara asignada via source property");
                return;
            }
            catch { }
        }
        
        Debug.LogWarning("[DroneStreamingBridge] ⚠️ No se pudo asignar la cámara automáticamente.");
    }
}
