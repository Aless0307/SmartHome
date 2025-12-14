using UnityEngine;
using Unity.RenderStreaming;
using Unity.WebRTC;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Intercepta DataChannels en PeerConnections existentes
/// AGREGAR A: Mismo GameObject que DroneStreamingBridge o DroneStreamingSystem
/// 
/// FUNCIONAMIENTO:
/// 1. Busca SignalingManager activo
/// 2. Usa reflexión para acceder a las PeerConnections internas
/// 3. Escucha OnDataChannel en cada PeerConnection
/// 4. Procesa mensajes de input
/// </summary>
public class WebRTCDataChannelListener : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private DroneController droneController;
    
    [Header("Debug")]
    [SerializeField] private bool logMessages = true;
    
    // Tracking
    private SignalingManager signalingManager;
    private HashSet<int> monitoredConnections = new HashSet<int>();
    private List<RTCDataChannel> activeChannels = new List<RTCDataChannel>();
    
    // Stats
    private int messagesReceived = 0;
    private DateTime lastReceiveTime;
    private string statusText = "Buscando conexiones...";
    
    void Start()
    {
        Debug.Log("[WebRTCListener] 🔍 Iniciando búsqueda de DataChannels...");
        
        if (droneController == null)
        {
            droneController = FindFirstObjectByType<DroneController>();
            if (droneController != null)
            {
                Debug.Log($"[WebRTCListener] ✅ Dron encontrado: {droneController.name}");
            }
        }
        
        StartCoroutine(MonitorConnections());
    }
    
    IEnumerator MonitorConnections()
    {
        // Esperar a que el sistema esté listo
        yield return new WaitForSeconds(1f);
        
        while (true)
        {
            // Buscar SignalingManager
            if (signalingManager == null)
            {
                signalingManager = FindFirstObjectByType<SignalingManager>();
                if (signalingManager != null)
                {
                    Debug.Log("[WebRTCListener] ✅ SignalingManager encontrado");
                    HookIntoSignalingManager();
                }
            }
            
            // Buscar Broadcasts
            SearchBroadcasts();
            
            // Buscar StreamReceiverManager
            SearchStreamManagers();
            
            yield return new WaitForSeconds(0.5f);
        }
    }
    
    void HookIntoSignalingManager()
    {
        if (signalingManager == null) return;
        
        try
        {
            // El SignalingManager tiene un evento OnDataChannel interno
            // Intentar acceder via reflexión
            var managerType = signalingManager.GetType();
            var internalField = managerType.GetField("m_internal", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (internalField != null)
            {
                var internalObj = internalField.GetValue(signalingManager);
                if (internalObj != null)
                {
                    Debug.Log("[WebRTCListener] 🔗 Accediendo a SignalingManagerInternal...");
                    SearchForPeerConnections(internalObj);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[WebRTCListener] No pudo acceder internals: {ex.Message}");
        }
    }
    
    void SearchBroadcasts()
    {
        var broadcasts = FindObjectsByType<Broadcast>(FindObjectsSortMode.None);
        foreach (var broadcast in broadcasts)
        {
            SearchForPeerConnections(broadcast);
        }
    }
    
    void SearchStreamManagers()
    {
        // Buscar cualquier componente que pueda tener PeerConnections
        var handlers = FindObjectsByType<StreamReceiverManager>(FindObjectsSortMode.None);
        foreach (var handler in handlers)
        {
            SearchForPeerConnections(handler);
        }
    }
    
    void SearchForPeerConnections(object obj)
    {
        if (obj == null) return;
        
        var type = obj.GetType();
        
        // Buscar en todos los campos
        while (type != null && type != typeof(object))
        {
            var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                try
                {
                    var value = field.GetValue(obj);
                    
                    // RTCPeerConnection directo
                    if (value is RTCPeerConnection pc)
                    {
                        TryMonitorPeerConnection(pc, $"{type.Name}.{field.Name}");
                    }
                    // Diccionario con PeerConnections
                    else if (value is IDictionary dict)
                    {
                        foreach (DictionaryEntry entry in dict)
                        {
                            if (entry.Value is RTCPeerConnection dictPc)
                            {
                                TryMonitorPeerConnection(dictPc, $"{type.Name}[{entry.Key}]");
                            }
                            // Buscar dentro del valor también
                            SearchForPeerConnections(entry.Value);
                        }
                    }
                    // Lista con objetos que pueden tener PeerConnections
                    else if (value is IEnumerable<object> list)
                    {
                        foreach (var item in list)
                        {
                            SearchForPeerConnections(item);
                        }
                    }
                }
                catch { }
            }
            
            type = type.BaseType;
        }
    }
    
    void TryMonitorPeerConnection(RTCPeerConnection pc, string name)
    {
        if (pc == null) return;
        
        int hash = pc.GetHashCode();
        if (monitoredConnections.Contains(hash)) return;
        
        monitoredConnections.Add(hash);
        statusText = $"Monitoreando: {name}";
        Debug.Log($"[WebRTCListener] 🔗 Nueva PeerConnection: {name}");
        Debug.Log($"[WebRTCListener]    State: {pc.ConnectionState}, Ice: {pc.IceConnectionState}");
        
        // Hook OnDataChannel
        var existingHandler = pc.OnDataChannel;
        pc.OnDataChannel = (channel) =>
        {
            Debug.Log($"[WebRTCListener] 📡 DataChannel recibido: '{channel.Label}'");
            
            // Llamar handler existente si lo hay
            existingHandler?.Invoke(channel);
            
            // Configurar nuestro handler
            SetupDataChannel(channel);
        };
        
        Debug.Log($"[WebRTCListener] ✅ OnDataChannel configurado para: {name}");
    }
    
    void SetupDataChannel(RTCDataChannel channel)
    {
        if (channel == null) return;
        
        activeChannels.Add(channel);
        
        channel.OnOpen = () =>
        {
            statusText = $"✅ Canal '{channel.Label}' abierto";
            Debug.Log($"[WebRTCListener] ✅ DataChannel ABIERTO: {channel.Label}");
        };
        
        channel.OnClose = () =>
        {
            Debug.Log($"[WebRTCListener] DataChannel cerrado: {channel.Label}");
            activeChannels.Remove(channel);
        };
        
        channel.OnMessage = (bytes) =>
        {
            string message = System.Text.Encoding.UTF8.GetString(bytes);
            ProcessMessage(message);
        };
        
        // Si ya está abierto
        if (channel.ReadyState == RTCDataChannelState.Open)
        {
            statusText = $"✅ Canal '{channel.Label}' ya abierto";
            Debug.Log($"[WebRTCListener] Canal ya estaba abierto: {channel.Label}");
        }
    }
    
    void ProcessMessage(string message)
    {
        messagesReceived++;
        lastReceiveTime = DateTime.Now;
        
        if (logMessages)
        {
            Debug.Log($"[WebRTCListener] 📩 {message}");
        }
        
        try
        {
            if (message.Contains("\"type\":\"input\""))
            {
                float h = ExtractFloat(message, "horizontal");
                float v = ExtractFloat(message, "vertical");
                float e = ExtractFloat(message, "elevation");
                float r = ExtractFloat(message, "rotation");
                
                if (droneController != null)
                {
                    droneController.SetInput(h, v, e, r);
                    
                    if (logMessages)
                    {
                        Debug.Log($"[WebRTCListener] 🎯 Input: H={h:F2} V={v:F2} E={e:F2} R={r:F2}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[WebRTCListener] Error: {ex.Message}");
        }
    }
    
    float ExtractFloat(string json, string key)
    {
        string searchKey = $"\"{key}\":";
        int startIndex = json.IndexOf(searchKey);
        if (startIndex == -1) return 0;
        
        startIndex += searchKey.Length;
        int endIndex = json.IndexOfAny(new char[] { ',', '}' }, startIndex);
        if (endIndex == -1) return 0;
        
        string valueStr = json.Substring(startIndex, endIndex - startIndex).Trim();
        float.TryParse(valueStr, System.Globalization.NumberStyles.Float, 
            System.Globalization.CultureInfo.InvariantCulture, out float result);
        return result;
    }
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 280, 150));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("📡 WEBRTC DATA LISTENER", new GUIStyle(GUI.skin.label) 
        { 
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.yellow }
        });
        
        GUILayout.Label($"Estado: {statusText}");
        GUILayout.Label($"PeerConnections: {monitoredConnections.Count}");
        GUILayout.Label($"DataChannels: {activeChannels.Count}");
        GUILayout.Label($"Mensajes: {messagesReceived}");
        
        bool active = (DateTime.Now - lastReceiveTime).TotalSeconds < 1f;
        GUI.color = active ? Color.green : Color.gray;
        GUILayout.Label(active ? "● Recibiendo" : "○ Sin datos");
        GUI.color = Color.white;
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
