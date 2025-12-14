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
    
    // Track de conexiones activas
    private HashSet<string> activeConnections = new HashSet<string>();
    private HashSet<string> processedConnections = new HashSet<string>();
    
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
}
