using UnityEngine;
using Unity.RenderStreaming;
using System.Collections.Generic;

/// <summary>
/// Conecta los eventos de conexión WebRTC con el sistema multi-dron
/// Cada cliente que se conecta obtiene su propio dron
/// 
/// SETUP:
/// 1. Añadir este script al mismo GameObject que tiene SignalingManager
/// 2. Asignar referencias en el Inspector
/// 3. Cuando un cliente se conecta, se crea un dron nuevo
/// 4. Cuando se desconecta, se destruye
/// </summary>
public class MultiDroneConnectionHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SignalingManager signalingManager;
    [SerializeField] private Broadcast broadcast;
    
    [Header("Alternative - DroneSpawner")]
    [SerializeField] private DroneSpawner droneSpawner;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    [Header("Polling Settings")]
    [SerializeField] private float pollingInterval = 1f; // Revisar cada segundo
    
    // Track de conexiones activas
    private HashSet<string> activeConnections = new HashSet<string>();
    private HashSet<string> processedConnections = new HashSet<string>();
    private float lastPollTime = 0f;
    
    private void Start()
    {
        // Auto-encontrar referencias si no están asignadas
        if (signalingManager == null)
        {
            signalingManager = FindFirstObjectByType<SignalingManager>();
        }
        
        if (broadcast == null)
        {
            broadcast = FindFirstObjectByType<Broadcast>();
        }
        
        if (droneSpawner == null)
        {
            droneSpawner = FindFirstObjectByType<DroneSpawner>();
        }
        
        // Suscribirse a eventos del Broadcast
        SubscribeToBroadcastEvents();
        
        if (showDebugLogs)
        {
            Debug.Log("[MultiDroneConnectionHandler] ✅ Inicializado");
            Debug.Log($"  SignalingManager: {(signalingManager != null ? "✓" : "✗")}");
            Debug.Log($"  Broadcast: {(broadcast != null ? "✓" : "✗")}");
            Debug.Log($"  DroneSpawner: {(droneSpawner != null ? "✓" : "✗")}");
        }
    }
    
    private void SubscribeToBroadcastEvents()
    {
        if (broadcast == null)
        {
            Debug.LogWarning("[MultiDroneConnectionHandler] ⚠️ No se encontró Broadcast, usando polling");
            return;
        }
        
        // Intentar suscribirse a eventos del Broadcast usando reflexión
        var type = broadcast.GetType();
        bool subscribed = false;
        
        foreach (var eventInfo in type.GetEvents())
        {
            string name = eventInfo.Name.ToLower();
            
            if (name.Contains("addconnection") || name.Contains("onadd") || 
                (name.Contains("connect") && !name.Contains("disconnect")))
            {
                try
                {
                    // Intentar varias firmas de delegado
                    TrySubscribeEvent(eventInfo, "OnBroadcastConnect");
                    subscribed = true;
                }
                catch (System.Exception e)
                {
                    Debug.Log($"[MultiDroneConnectionHandler] ⚠️ {eventInfo.Name}: {e.Message}");
                }
            }
            
            if (name.Contains("deleteconnection") || name.Contains("ondelete") || name.Contains("disconnect"))
            {
                try
                {
                    TrySubscribeEvent(eventInfo, "OnBroadcastDisconnect");
                    subscribed = true;
                }
                catch (System.Exception e)
                {
                    Debug.Log($"[MultiDroneConnectionHandler] ⚠️ {eventInfo.Name}: {e.Message}");
                }
            }
        }
        
        if (!subscribed)
        {
            Debug.Log("[MultiDroneConnectionHandler] 📡 Usando modo polling para detectar conexiones");
        }
    }
    
    private void TrySubscribeEvent(System.Reflection.EventInfo eventInfo, string methodName)
    {
        var methods = typeof(MultiDroneConnectionHandler).GetMethods(
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.Instance);
        
        foreach (var method in methods)
        {
            if (method.Name == methodName)
            {
                try
                {
                    var handler = System.Delegate.CreateDelegate(eventInfo.EventHandlerType, this, method);
                    eventInfo.AddEventHandler(broadcast, handler);
                    Debug.Log($"[MultiDroneConnectionHandler] 📡 Suscrito a: {eventInfo.Name}");
                    return;
                }
                catch { }
            }
        }
    }
    
    // Múltiples firmas para compatibilidad
    private void OnBroadcastConnect(string connectionId) => OnNewConnection(connectionId);
    private void OnBroadcastConnect(SignalingEventData data) => OnNewConnection(data.connectionId);
    private void OnBroadcastDisconnect(string connectionId) => OnConnectionClosed(connectionId);
    private void OnBroadcastDisconnect(SignalingEventData data) => OnConnectionClosed(data.connectionId);
    
    private void OnNewConnection(string connectionId)
    {
        if (string.IsNullOrEmpty(connectionId)) return;
        if (processedConnections.Contains(connectionId)) return;
        
        processedConnections.Add(connectionId);
        activeConnections.Add(connectionId);
        
        if (showDebugLogs)
        {
            Debug.Log($"[MultiDroneConnectionHandler] 🚁 NUEVO CLIENTE: {connectionId}");
        }
        
        if (droneSpawner != null)
        {
            droneSpawner.SpawnDrone(connectionId);
        }
        else
        {
            Debug.LogWarning("[MultiDroneConnectionHandler] ⚠️ No hay DroneSpawner!");
        }
    }
    
    private void OnConnectionClosed(string connectionId)
    {
        if (string.IsNullOrEmpty(connectionId)) return;
        if (!activeConnections.Contains(connectionId)) return;
        
        activeConnections.Remove(connectionId);
        
        if (showDebugLogs)
        {
            Debug.Log($"[MultiDroneConnectionHandler] 👋 CLIENTE DESCONECTADO: {connectionId}");
        }
        
        if (droneSpawner != null)
        {
            droneSpawner.DespawnDrone(connectionId);
        }
    }
    
    /// <summary>
    /// Método público para conectar manualmente (útil para testing)
    /// </summary>
    public void ManualConnect(string connectionId)
    {
        OnNewConnection(connectionId);
    }
    
    /// <summary>
    /// Método público para desconectar manualmente (útil para testing)
    /// </summary>
    public void ManualDisconnect(string connectionId)
    {
        OnConnectionClosed(connectionId);
    }
    
    /// <summary>
    /// Obtener número de conexiones activas
    /// </summary>
    public int GetActiveConnectionCount()
    {
        return activeConnections.Count;
    }
    
    private void Update()
    {
        // Polling para detectar desconexiones
        if (Time.time - lastPollTime >= pollingInterval)
        {
            lastPollTime = Time.time;
            CheckForDisconnections();
        }
    }
    
    /// <summary>
    /// Revisar si alguna conexión se ha cerrado
    /// </summary>
    private void CheckForDisconnections()
    {
        if (broadcast == null) return;
        if (activeConnections.Count == 0) return;
        
        // Obtener conexiones actuales del Broadcast usando reflexión
        HashSet<string> currentConnections = GetBroadcastConnections();
        
        // Encontrar conexiones que ya no existen
        List<string> disconnected = new List<string>();
        foreach (string connId in activeConnections)
        {
            if (!currentConnections.Contains(connId))
            {
                disconnected.Add(connId);
            }
        }
        
        // Procesar desconexiones
        foreach (string connId in disconnected)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[MultiDroneConnectionHandler] 🔴 Conexión perdida detectada: {connId}");
            }
            OnConnectionClosed(connId);
        }
    }
    
    /// <summary>
    /// Obtener las conexiones activas del Broadcast usando reflexión
    /// </summary>
    private HashSet<string> GetBroadcastConnections()
    {
        HashSet<string> connections = new HashSet<string>();
        
        if (broadcast == null) return connections;
        
        var type = broadcast.GetType();
        var bindingFlags = System.Reflection.BindingFlags.NonPublic | 
                          System.Reflection.BindingFlags.Public | 
                          System.Reflection.BindingFlags.Instance;
        
        // Buscar campos que contengan las conexiones
        foreach (var field in type.GetFields(bindingFlags))
        {
            string name = field.Name.ToLower();
            if (name.Contains("connection") || name.Contains("peer") || name.Contains("client"))
            {
                var value = field.GetValue(broadcast);
                if (value != null)
                {
                    // Si es un diccionario
                    if (value is System.Collections.IDictionary dict)
                    {
                        foreach (var key in dict.Keys)
                        {
                            if (key != null)
                                connections.Add(key.ToString());
                        }
                    }
                    // Si es una lista o colección
                    else if (value is System.Collections.IEnumerable enumerable && !(value is string))
                    {
                        foreach (var item in enumerable)
                        {
                            if (item != null)
                            {
                                // Intentar obtener connectionId del item
                                var itemType = item.GetType();
                                var connIdProp = itemType.GetProperty("connectionId") ?? 
                                                itemType.GetProperty("ConnectionId");
                                if (connIdProp != null)
                                {
                                    var id = connIdProp.GetValue(item);
                                    if (id != null)
                                        connections.Add(id.ToString());
                                }
                                else
                                {
                                    connections.Add(item.ToString());
                                }
                            }
                        }
                    }
                }
            }
        }
        
        return connections;
    }
}
