using UnityEngine;
using Unity.RenderStreaming;
using Unity.WebRTC;
using System;

/// <summary>
/// Receptor de input via DataChannel usando IDataChannel
/// 
/// SETUP:
/// 1. Agregar este componente al GameObject que tiene el Broadcast
/// 2. En el Broadcast, agregar este componente a la lista de "Handlers"  
/// 3. Configurar Label = "input"
/// 4. IsLocal = true (Unity crea el canal, navegador lo recibe y usa)
/// </summary>
public class DroneInputChannel : MonoBehaviour, IDataChannel
{
    [Header("Channel Config")]
    [SerializeField] private string label = "input";
    [SerializeField] private bool isLocal = true; // Unity crea el canal
    
    [Header("Drone")]
    [SerializeField] private DroneController droneController;
    
    [Header("Debug")]
    [SerializeField] private bool logMessages = true;
    
    // IDataChannel implementation
    public string Label => label;
    public bool IsLocal => isLocal;
    public string ConnectionId { get; private set; }
    public RTCDataChannel Channel { get; private set; }
    public bool IsConnected => Channel != null && Channel.ReadyState == RTCDataChannelState.Open;
    
    // Evento requerido por IDataChannel
    public event Action<string, RTCDataChannel> OnStartedChannel;
    public event Action<string> OnStoppedChannel;
    
    private int messagesReceived = 0;
    private DateTime lastMessageTime;
    private string lastInput = "";
    private string status = "Esperando conexión...";
    private string currentConnectionId = "";
    
    void Start()
    {
        Debug.Log($"[DroneInputChannel] ====================================");
        Debug.Log($"[DroneInputChannel] 🚁 DRONE INPUT CHANNEL INICIADO");
        Debug.Log($"[DroneInputChannel]    Label: '{label}'");
        Debug.Log($"[DroneInputChannel]    IsLocal: {isLocal}");
        Debug.Log($"[DroneInputChannel] ====================================");
        
        // No buscar DroneController aquí - lo buscaremos dinámicamente
        // basándonos en el connectionId del mensaje
    }
    
    private float logTimer = 0f;
    void Update()
    {
        logTimer += Time.deltaTime;
        if (logTimer >= 5f)
        {
            logTimer = 0f;
            Debug.Log($"[DroneInputChannel] 📊 Status - Channel: {(Channel != null ? Channel.ReadyState.ToString() : "null")}, Msgs: {messagesReceived}, Connected: {IsConnected}");
        }
    }
    
    /// <summary>
    /// Llamado por Unity Render Streaming cuando se establece el canal
    /// </summary>
    public void SetChannel(string connectionId, RTCDataChannel channel)
    {
        SetChannelInternal(connectionId, channel);
    }
    
    /// <summary>
    /// Implementación de IDataChannel.SetChannel con SignalingEventData
    /// Este es llamado por el Broadcast cuando hay una nueva conexión
    /// </summary>
    public void SetChannel(SignalingEventData data)
    {
        Debug.Log($"[DroneInputChannel] 📡📡📡 SetChannel(SignalingEventData) LLAMADO 📡📡📡");
        Debug.Log($"[DroneInputChannel]    ConnectionId: {data.connectionId}");
        
        ConnectionId = data.connectionId;
        currentConnectionId = data.connectionId;
        
        // Buscar el DataChannel en la PeerConnection asociada a esta conexión
        StartCoroutine(FindAndConfigureDataChannel(data.connectionId));
    }
    
    private System.Collections.IEnumerator FindAndConfigureDataChannel(string connectionId)
    {
        Debug.Log($"[DroneInputChannel] 🔍 Buscando DataChannel para connectionId: {connectionId}");
        
        // Dar tiempo para que el DataChannel se establezca
        float timeout = 10f;
        float elapsed = 0f;
        
        while (elapsed < timeout && Channel == null)
        {
            // Buscar en Broadcasts que podrían tener la PeerConnection
            var broadcasts = FindObjectsByType<Broadcast>(FindObjectsSortMode.None);
            foreach (var broadcast in broadcasts)
            {
                var pc = FindPeerConnectionForConnection(broadcast, connectionId);
                if (pc != null)
                {
                    Debug.Log($"[DroneInputChannel] ✅ PeerConnection encontrada para: {connectionId}");
                    
                    // Configurar handler para DataChannel entrante
                    var existingHandler = pc.OnDataChannel;
                    pc.OnDataChannel = (channel) =>
                    {
                        Debug.Log($"[DroneInputChannel] 📡 DataChannel recibido: '{channel.Label}'");
                        existingHandler?.Invoke(channel);
                        
                        if (channel.Label == label)
                        {
                            Debug.Log($"[DroneInputChannel] ✅ Canal '{label}' ENCONTRADO!");
                            SetChannelInternal(connectionId, channel);
                        }
                    };
                    
                    // Revisar si ya hay DataChannels
                    var dataChannels = GetDataChannelsFromPeerConnection(pc);
                    foreach (var dc in dataChannels)
                    {
                        if (dc.Label == label)
                        {
                            Debug.Log($"[DroneInputChannel] ✅ Canal '{label}' ya existía!");
                            SetChannelInternal(connectionId, dc);
                            yield break;
                        }
                    }
                }
            }
            
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        
        if (Channel == null)
        {
            Debug.LogWarning($"[DroneInputChannel] ⚠️ No se encontró DataChannel '{label}' después de {timeout}s");
        }
    }
    
    private RTCPeerConnection FindPeerConnectionForConnection(Broadcast broadcast, string connectionId)
    {
        try
        {
            var type = broadcast.GetType();
            var baseType = type.BaseType; // SignalingHandlerBase
            
            if (baseType != null)
            {
                var pcField = baseType.GetField("m_peerConnectionMap", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (pcField != null)
                {
                    var pcMap = pcField.GetValue(broadcast) as System.Collections.IDictionary;
                    if (pcMap != null && pcMap.Contains(connectionId))
                    {
                        // El valor es un PeerConnection wrapper
                        var wrapper = pcMap[connectionId];
                        if (wrapper != null)
                        {
                            // Buscar el campo peer dentro del wrapper
                            var wrapperType = wrapper.GetType();
                            var peerField = wrapperType.GetField("peer", 
                                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                            
                            if (peerField != null)
                            {
                                return peerField.GetValue(wrapper) as RTCPeerConnection;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[DroneInputChannel] Error buscando PeerConnection: {ex.Message}");
        }
        
        return null;
    }
    
    private System.Collections.Generic.List<RTCDataChannel> GetDataChannelsFromPeerConnection(RTCPeerConnection pc)
    {
        var channels = new System.Collections.Generic.List<RTCDataChannel>();
        
        try
        {
            var type = pc.GetType();
            var fields = type.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(RTCDataChannel))
                {
                    var channel = field.GetValue(pc) as RTCDataChannel;
                    if (channel != null)
                    {
                        channels.Add(channel);
                    }
                }
                else if (typeof(System.Collections.IEnumerable).IsAssignableFrom(field.FieldType))
                {
                    var enumerable = field.GetValue(pc) as System.Collections.IEnumerable;
                    if (enumerable != null)
                    {
                        foreach (var item in enumerable)
                        {
                            if (item is RTCDataChannel dc)
                            {
                                channels.Add(dc);
                            }
                        }
                    }
                }
            }
        }
        catch { }
        
        return channels;
    }
    
    private void SetChannelInternal(string connectionId, RTCDataChannel channel)
    {
        if (channel == null)
        {
            Debug.LogWarning("[DroneInputChannel] SetChannel llamado con channel null");
            return;
        }
        
        ConnectionId = connectionId;
        currentConnectionId = connectionId;
        Channel = channel;
        
        Debug.Log($"[DroneInputChannel] 📡📡📡 SetChannelInternal LLAMADO 📡📡📡");
        Debug.Log($"[DroneInputChannel]    ConnectionId: {connectionId}");
        Debug.Log($"[DroneInputChannel]    Channel Label: {channel.Label}");
        Debug.Log($"[DroneInputChannel]    Channel State: {channel.ReadyState}");
        Debug.Log($"[DroneInputChannel]    Channel Id: {channel.Id}");
        
        // Configurar eventos del canal
        channel.OnOpen = () =>
        {
            status = $"✅ Conectado: {connectionId.Substring(0, Math.Min(8, connectionId.Length))}";
            Debug.Log($"[DroneInputChannel] ✅✅✅ CANAL ABIERTO ✅✅✅");
            Debug.Log($"[DroneInputChannel]    ReadyState ahora: {channel.ReadyState}");
            OnStartedChannel?.Invoke(connectionId, channel);
        };
        
        channel.OnClose = () =>
        {
            status = "Desconectado";
            Debug.Log($"[DroneInputChannel] ❌ Canal cerrado");
            OnStoppedChannel?.Invoke(connectionId);
            Channel = null;
        };
        
        channel.OnMessage = (bytes) =>
        {
            Debug.Log($"[DroneInputChannel] 📨📨📨 OnMessage DISPARADO - {bytes.Length} bytes 📨📨📨");
            
            string message = System.Text.Encoding.UTF8.GetString(bytes);
            messagesReceived++;
            lastMessageTime = DateTime.Now;
            
            if (logMessages)
            {
                Debug.Log($"[DroneInputChannel] 📩 Mensaje: {message}");
            }
            
            ProcessInputMessage(message);
        };
        
        Debug.Log($"[DroneInputChannel] ✅ Handlers OnOpen/OnClose/OnMessage CONFIGURADOS");
        
        // Si ya está abierto
        if (channel.ReadyState == RTCDataChannelState.Open)
        {
            status = $"✅ Ya conectado: {connectionId.Substring(0, Math.Min(8, connectionId.Length))}";
            Debug.Log($"[DroneInputChannel] ✅ Canal ya estaba ABIERTO - listo para recibir");
            OnStartedChannel?.Invoke(connectionId, channel);
        }
    }
    
    private void ProcessInputMessage(string message)
    {
        try
        {
            if (!message.Contains("\"type\":\"input\"")) return;
            
            // Extraer connectionId del mensaje
            string connectionId = ExtractString(message, "connectionId");
            
            float h = ExtractFloat(message, "horizontal");
            float v = ExtractFloat(message, "vertical");
            float e = ExtractFloat(message, "elevation");
            float r = ExtractFloat(message, "rotation");
            
            lastInput = $"H:{h:F1} V:{v:F1} E:{e:F1} R:{r:F1}";
            
            // Buscar el dron correcto para este connectionId
            DroneController targetDrone = FindDroneForConnection(connectionId);
            
            if (targetDrone != null)
            {
                targetDrone.SetInput(h, v, e, r);
                
                // Log solo cuando hay movimiento real
                if (h != 0 || v != 0 || e != 0 || r != 0)
                {
                    Debug.Log($"[DroneInputChannel] 🎯 INPUT APLICADO al dron '{targetDrone.name}': {lastInput}");
                }
            }
            else
            {
                Debug.LogWarning($"[DroneInputChannel] ⚠️ No se encontró dron para conexión: {connectionId}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DroneInputChannel] Error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Buscar el DroneController correcto para una conexión
    /// </summary>
    private DroneController FindDroneForConnection(string connectionId)
    {
        // Primero, buscar en MultiClientDroneStreaming
        var multiClient = FindFirstObjectByType<MultiClientDroneStreaming>();
        if (multiClient != null)
        {
            var drone = multiClient.GetDroneForConnection(connectionId);
            if (drone != null)
            {
                Debug.Log($"[DroneInputChannel] 🎯 Dron encontrado via MultiClientDroneStreaming: {drone.name}");
                return drone;
            }
        }
        
        // Si no hay MultiClientDroneStreaming, buscar dron con nombre que contenga parte del connectionId
        string shortId = connectionId.Length > 8 ? connectionId.Substring(0, 8) : connectionId;
        var allDrones = FindObjectsByType<DroneController>(FindObjectsSortMode.None);
        
        foreach (var drone in allDrones)
        {
            if (drone.name.Contains(shortId))
            {
                Debug.Log($"[DroneInputChannel] 🎯 Dron encontrado por nombre: {drone.name}");
                return drone;
            }
        }
        
        // Como fallback, usar el primer dron disponible (pero advertir)
        if (allDrones.Length > 0)
        {
            Debug.LogWarning($"[DroneInputChannel] ⚠️ Usando primer dron como fallback: {allDrones[0].name}");
            return allDrones[0];
        }
        
        return null;
    }
    
    private string ExtractString(string json, string key)
    {
        string searchKey = $"\"{key}\":\"";
        int startIndex = json.IndexOf(searchKey);
        if (startIndex < 0) return "";
        
        startIndex += searchKey.Length;
        int endIndex = json.IndexOf("\"", startIndex);
        if (endIndex < 0) return "";
        
        return json.Substring(startIndex, endIndex - startIndex);
    }
    
    private float ExtractFloat(string json, string key)
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
    
    /// <summary>
    /// Enviar mensaje al navegador
    /// </summary>
    public void SendToClient(string message)
    {
        if (IsConnected)
        {
            Channel.Send(System.Text.Encoding.UTF8.GetBytes(message));
        }
    }
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, Screen.height - 160, 280, 150));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("🎮 DRONE INPUT CHANNEL", new GUIStyle(GUI.skin.label) 
        { 
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.cyan }
        });
        
        GUILayout.Label($"Estado: {status}");
        GUILayout.Label($"Label: {label}");
        GUILayout.Label($"Conectado: {IsConnected}");
        GUILayout.Label($"Mensajes: {messagesReceived}");
        GUILayout.Label($"Input: {lastInput}");
        
        bool active = (DateTime.Now - lastMessageTime).TotalSeconds < 1f;
        GUI.color = active ? Color.green : Color.gray;
        GUILayout.Label(active ? "● Recibiendo" : "○ Sin datos");
        GUI.color = Color.white;
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
