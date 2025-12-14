using UnityEngine;
using Unity.RenderStreaming;
using Unity.WebRTC;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Recibe input de MÚLTIPLES clientes via WebRTC DataChannel
/// Cada cliente controla su propio dron identificado por connectionId
/// 
/// IMPORTANTE: Este script busca agresivamente PeerConnections y configura
/// handlers para DataChannels entrantes. También escucha eventos del SignalingManager.
/// </summary>
public class DroneInputReceiver : MonoBehaviour
{
    [Header("Configuración Multi-Cliente")]
    [SerializeField] private GameObject dronePrefab;
    [SerializeField] private Transform droneSpawnPoint;
    [SerializeField] private float spawnSpacing = 5f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugGUI = true;
    [SerializeField] private bool logAllMessages = true; // Habilitado por defecto para debug
    
    // Mapeo de connectionId -> DroneController
    private Dictionary<string, DroneController> clientDrones = new Dictionary<string, DroneController>();
    
    // Mapeo de connectionId -> último input
    private Dictionary<string, DroneInputState> clientInputs = new Dictionary<string, DroneInputState>();
    
    // Stats
    private int totalMessagesReceived;
    private int activeClients;
    private DateTime lastMessageTime;
    private string statusMessage = "Iniciando búsqueda...";
    
    // Tracking de PeerConnections
    private HashSet<int> configuredPeerConnections = new HashSet<int>();
    
    // DataChannels activos
    private List<RTCDataChannel> activeDataChannels = new List<RTCDataChannel>();
    
    // Colores para identificar drones
    private Color[] droneColors = new Color[]
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        Color.magenta,
        Color.cyan,
        new Color(1f, 0.5f, 0f), // Naranja
        new Color(0.5f, 0f, 1f)  // Púrpura
    };
    private int colorIndex = 0;
    
    [System.Serializable]
    private class DroneInputState
    {
        public float horizontal;
        public float vertical;
        public float elevation;
        public float rotation;
        public DateTime lastUpdate;
    }
    
    void Start()
    {
        Debug.Log("[DroneInputReceiver] ========================================");
        Debug.Log("[DroneInputReceiver] 🎮 INICIANDO SISTEMA DE INPUT MULTI-CLIENTE");
        Debug.Log("[DroneInputReceiver] ========================================");
        
        // Buscar prefab si no está asignado
        if (dronePrefab == null)
        {
            // Intentar encontrar un dron existente para usar como referencia
            var existingDrone = FindFirstObjectByType<DroneController>();
            if (existingDrone != null)
            {
                Debug.Log($"[DroneInputReceiver] ✅ Dron existente encontrado: {existingDrone.name}");
                // Registrar el dron existente como el del primer cliente
                clientDrones["default"] = existingDrone;
            }
            else
            {
                Debug.LogWarning("[DroneInputReceiver] ⚠️ No hay dronePrefab ni dron existente");
            }
        }
        
        if (droneSpawnPoint == null)
        {
            droneSpawnPoint = transform;
        }
        
        // Iniciar monitoreo agresivo
        StartCoroutine(AggressiveMonitor());
        
        // Subscribir a eventos de SignalingManager
        SubscribeToSignalingEvents();
    }
    
    void SubscribeToSignalingEvents()
    {
        var signalingManager = FindFirstObjectByType<SignalingManager>();
        if (signalingManager != null)
        {
            Debug.Log("[DroneInputReceiver] ✅ SignalingManager encontrado, intentando suscribir...");
            
            // Buscar evento interno via reflexión
            try
            {
                var type = signalingManager.GetType();
                var internalField = type.GetField("m_internal", BindingFlags.NonPublic | BindingFlags.Instance);
                if (internalField != null)
                {
                    var internalObj = internalField.GetValue(signalingManager);
                    if (internalObj != null)
                    {
                        Debug.Log("[DroneInputReceiver] 🔗 Accediendo a SignalingManagerInternal...");
                        // Buscar peerConnections dictionary
                        var internalType = internalObj.GetType();
                        var pcField = internalType.GetField("m_peerConnections", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (pcField != null)
                        {
                            Debug.Log("[DroneInputReceiver] 📋 Campo m_peerConnections encontrado");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DroneInputReceiver] Reflexión limitada: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning("[DroneInputReceiver] ⚠️ SignalingManager no encontrado");
        }
    }
    
    /// <summary>
    /// Monitoreo agresivo de PeerConnections
    /// </summary>
    private IEnumerator AggressiveMonitor()
    {
        yield return new WaitForSeconds(0.5f);
        
        int scanCount = 0;
        while (true)
        {
            scanCount++;
            
            int prevCount = configuredPeerConnections.Count;
            
            // Buscar en todos los componentes posibles
            SearchInSignalingManager();
            SearchInBroadcasts();
            SearchInSingleConnections();
            SearchInStreamHandlers();
            
            // Log periódico
            if (scanCount % 20 == 0) // Cada 5 segundos
            {
                Debug.Log($"[DroneInputReceiver] 📊 Estado: PeerConnections={configuredPeerConnections.Count}, DataChannels={activeDataChannels.Count}, Mensajes={totalMessagesReceived}");
            }
            
            // Si encontramos nuevas conexiones
            if (configuredPeerConnections.Count > prevCount)
            {
                statusMessage = $"✅ {configuredPeerConnections.Count} conexiones monitoreadas";
            }
            
            // Actualizar contador de clientes activos
            activeClients = clientDrones.Count;
            
            yield return new WaitForSeconds(0.25f);
        }
    }
    
    private void SearchInSignalingManager()
    {
        var signalingManager = FindFirstObjectByType<SignalingManager>();
        if (signalingManager == null) return;
        
        try
        {
            var type = signalingManager.GetType();
            var internalField = type.GetField("m_internal", BindingFlags.NonPublic | BindingFlags.Instance);
            if (internalField != null)
            {
                var internalObj = internalField.GetValue(signalingManager);
                if (internalObj != null)
                {
                    ExtractPeerConnectionsFromObject(internalObj, "SignalingInternal");
                }
            }
        }
        catch { }
    }
    
    private void SearchInBroadcasts()
    {
        var broadcasts = FindObjectsByType<Broadcast>(FindObjectsSortMode.None);
        foreach (var broadcast in broadcasts)
        {
            ExtractPeerConnectionsFromHandler(broadcast);
        }
    }
    
    private void SearchInSingleConnections()
    {
        var singleConnections = FindObjectsByType<SingleConnection>(FindObjectsSortMode.None);
        foreach (var sc in singleConnections)
        {
            ExtractPeerConnectionsFromHandler(sc);
        }
    }
    
    private void SearchInStreamHandlers()
    {
        // Buscar VideoStreamSender
        var videoSenders = FindObjectsByType<VideoStreamSender>(FindObjectsSortMode.None);
        foreach (var sender in videoSenders)
        {
            ExtractPeerConnectionsFromHandler(sender);
        }
    }
    
    private void ExtractPeerConnectionsFromHandler(MonoBehaviour handler)
    {
        if (handler == null) return;
        ExtractPeerConnectionsFromObject(handler, handler.name);
    }
    
    private void ExtractPeerConnectionsFromObject(object obj, string sourceName)
    {
        if (obj == null) return;
        
        try
        {
            var type = obj.GetType();
            int depth = 0;
            while (type != null && type != typeof(object) && depth < 5)
            {
                SearchFieldsForPeerConnection(obj, type, sourceName);
                type = type.BaseType;
                depth++;
            }
        }
        catch { }
    }
    
    private void SearchFieldsForPeerConnection(object handler, Type type, string sourceName)
    {
        var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var field in fields)
        {
            try
            {
                var value = field.GetValue(handler);
                if (value == null) continue;
                
                // RTCPeerConnection directo
                if (value is RTCPeerConnection pc)
                {
                    ConfigurePeerConnection(pc, $"{sourceName}.{field.Name}");
                }
                // Diccionario que podría contener PeerConnections
                else if (value is System.Collections.IDictionary dict)
                {
                    foreach (System.Collections.DictionaryEntry entry in dict)
                    {
                        if (entry.Value is RTCPeerConnection dictPc)
                        {
                            ConfigurePeerConnection(dictPc, $"{sourceName}[{entry.Key}]");
                        }
                        // También buscar en objetos dentro del diccionario
                        else if (entry.Value != null)
                        {
                            ExtractPeerConnectionsFromObject(entry.Value, $"{sourceName}[{entry.Key}]");
                        }
                    }
                }
                // Lista/Array
                else if (value is System.Collections.IList list)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        var item = list[i];
                        if (item is RTCPeerConnection listPc)
                        {
                            ConfigurePeerConnection(listPc, $"{sourceName}[{i}]");
                        }
                    }
                }
            }
            catch { }
        }
    }
    
    private void ConfigurePeerConnection(RTCPeerConnection pc, string name)
    {
        int hash = pc.GetHashCode();
        if (configuredPeerConnections.Contains(hash)) return;
        
        configuredPeerConnections.Add(hash);
        
        Debug.Log($"[DroneInputReceiver] ==========================================");
        Debug.Log($"[DroneInputReceiver] 🔗 NUEVA PeerConnection: {name}");
        Debug.Log($"[DroneInputReceiver]    ConnectionState: {pc.ConnectionState}");
        Debug.Log($"[DroneInputReceiver]    IceConnectionState: {pc.IceConnectionState}");
        Debug.Log($"[DroneInputReceiver] ==========================================");
        
        // Guardar el handler existente si lo hay
        var existingHandler = pc.OnDataChannel;
        
        // Configurar nuevo handler
        pc.OnDataChannel = (channel) =>
        {
            Debug.Log($"[DroneInputReceiver] 📡📡📡 DataChannel RECIBIDO: '{channel.Label}' 📡📡📡");
            
            // Llamar handler existente si lo hay
            existingHandler?.Invoke(channel);
            
            // Configurar para recibir input
            SetupInputChannel(channel);
        };
        
        Debug.Log($"[DroneInputReceiver] ✅ OnDataChannel configurado para: {name}");
    }
    
    private void SetupInputChannel(RTCDataChannel channel)
    {
        Debug.Log($"[DroneInputReceiver] 🔧 Configurando canal: '{channel.Label}'");
        
        // Agregar a lista de canales activos
        if (!activeDataChannels.Contains(channel))
        {
            activeDataChannels.Add(channel);
        }
        
        channel.OnOpen = () =>
        {
            statusMessage = $"✅ Canal '{channel.Label}' ABIERTO";
            Debug.Log($"[DroneInputReceiver] ✅✅✅ DataChannel ABIERTO: '{channel.Label}' ✅✅✅");
        };
        
        channel.OnClose = () =>
        {
            Debug.Log($"[DroneInputReceiver] Canal cerrado: '{channel.Label}'");
            activeDataChannels.Remove(channel);
        };
        
        channel.OnMessage = (bytes) =>
        {
            string msg = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log($"[DroneInputReceiver] 📩📩📩 MENSAJE RECIBIDO: {msg} 📩📩📩");
            ProcessMessage(msg);
        };
        
        // Si ya está abierto
        if (channel.ReadyState == RTCDataChannelState.Open)
        {
            statusMessage = $"✅ Canal '{channel.Label}' ya estaba abierto";
            Debug.Log($"[DroneInputReceiver] ✅ Canal ya estaba ABIERTO: '{channel.Label}'");
        }
    }
    
    /// <summary>
    /// Procesar mensaje - AHORA CON SOPORTE MULTI-CLIENTE
    /// </summary>
    private void ProcessMessage(string message)
    {
        totalMessagesReceived++;
        lastMessageTime = DateTime.Now;
        
        if (logAllMessages)
        {
            Debug.Log($"[DroneInputReceiver] 📩 {message}");
        }
        
        try
        {
            if (!message.Contains("\"type\":\"input\"")) return;
            
            // Extraer connectionId del mensaje
            string connectionId = ExtractString(message, "connectionId");
            if (string.IsNullOrEmpty(connectionId))
            {
                connectionId = "default"; // Fallback para compatibilidad
            }
            
            float h = ExtractFloat(message, "horizontal");
            float v = ExtractFloat(message, "vertical");
            float e = ExtractFloat(message, "elevation");
            float r = ExtractFloat(message, "rotation");
            
            // Obtener o crear dron para este cliente
            DroneController drone = GetOrCreateDroneForClient(connectionId);
            
            if (drone != null)
            {
                drone.SetInput(h, v, e, r);
                
                // Log solo si hay movimiento real (no todo ceros)
                if (h != 0 || v != 0 || e != 0 || r != 0)
                {
                    Debug.Log($"[DroneInputReceiver] 🎯 INPUT APLICADO a '{drone.name}': H={h} V={v} E={e} R={r}");
                }
            }
            else
            {
                Debug.LogWarning($"[DroneInputReceiver] ⚠️ NO HAY DRON para cliente: {connectionId}");
            }
            
            // Guardar estado del input
            if (!clientInputs.ContainsKey(connectionId))
            {
                clientInputs[connectionId] = new DroneInputState();
            }
            clientInputs[connectionId].horizontal = h;
            clientInputs[connectionId].vertical = v;
            clientInputs[connectionId].elevation = e;
            clientInputs[connectionId].rotation = r;
            clientInputs[connectionId].lastUpdate = DateTime.Now;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DroneInputReceiver] Error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Obtener dron existente o crear uno nuevo para el cliente
    /// </summary>
    private DroneController GetOrCreateDroneForClient(string connectionId)
    {
        // Si ya existe para este connectionId, retornarlo
        if (clientDrones.TryGetValue(connectionId, out DroneController existingDrone))
        {
            return existingDrone;
        }
        
        // NUEVO: Si hay un dron registrado como "default", asignarlo a este cliente
        if (clientDrones.TryGetValue("default", out DroneController defaultDrone) && defaultDrone != null)
        {
            clientDrones[connectionId] = defaultDrone;
            clientDrones.Remove("default"); // Remover la entrada "default"
            Debug.Log($"[DroneInputReceiver] ✅ Cliente '{connectionId.Substring(0, Mathf.Min(8, connectionId.Length))}' asignado a dron 'default'");
            return defaultDrone;
        }
        
        // Buscar dron existente en la escena
        var existingInScene = FindFirstObjectByType<DroneController>();
        if (existingInScene != null && !clientDrones.ContainsValue(existingInScene))
        {
            clientDrones[connectionId] = existingInScene;
            Debug.Log($"[DroneInputReceiver] ✅ Cliente '{connectionId.Substring(0, Mathf.Min(8, connectionId.Length))}' asignado a dron en escena: {existingInScene.name}");
            return existingInScene;
        }
        
        // Crear nuevo dron si tenemos prefab
        if (dronePrefab != null)
        {
            Vector3 spawnPos = droneSpawnPoint.position + new Vector3(clientDrones.Count * spawnSpacing, 0, 0);
            GameObject newDroneObj = Instantiate(dronePrefab, spawnPos, Quaternion.identity);
            newDroneObj.name = $"Drone_Client_{connectionId.Substring(0, Mathf.Min(8, connectionId.Length))}";
            
            DroneController newDrone = newDroneObj.GetComponent<DroneController>();
            if (newDrone != null)
            {
                // Asignar color único
                Color droneColor = droneColors[colorIndex % droneColors.Length];
                colorIndex++;
                newDrone.SetDroneColor(droneColor);
                newDrone.SetOwner(connectionId);
                
                clientDrones[connectionId] = newDrone;
                Debug.Log($"[DroneInputReceiver] 🚁 Nuevo dron creado para cliente: {connectionId}");
                
                return newDrone;
            }
        }
        
        // Fallback: usar el primer dron disponible
        if (clientDrones.Count > 0)
        {
            var firstDrone = new List<DroneController>(clientDrones.Values)[0];
            clientDrones[connectionId] = firstDrone;
            return firstDrone;
        }
        
        Debug.LogWarning($"[DroneInputReceiver] No hay dron disponible para cliente: {connectionId}");
        return null;
    }
    
    /// <summary>
    /// Limpiar cliente desconectado
    /// </summary>
    public void OnClientDisconnected(string connectionId)
    {
        if (clientDrones.TryGetValue(connectionId, out DroneController drone))
        {
            if (drone != null && dronePrefab != null) // Solo destruir si fue spawneado
            {
                Destroy(drone.gameObject);
            }
            clientDrones.Remove(connectionId);
            clientInputs.Remove(connectionId);
            Debug.Log($"[DroneInputReceiver] Cliente desconectado: {connectionId}");
        }
    }
    
    private string ExtractString(string json, string key)
    {
        string searchKey = $"\"{key}\":\"";
        int startIndex = json.IndexOf(searchKey);
        if (startIndex == -1) return null;
        
        startIndex += searchKey.Length;
        int endIndex = json.IndexOf("\"", startIndex);
        if (endIndex == -1) return null;
        
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
        
        if (float.TryParse(valueStr, System.Globalization.NumberStyles.Float, 
            System.Globalization.CultureInfo.InvariantCulture, out float result))
        {
            return result;
        }
        return 0;
    }
    
    void OnGUI()
    {
        if (!showDebugGUI) return;
        
        GUILayout.BeginArea(new Rect(Screen.width - 280, Screen.height - 250, 270, 240));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("🎮 MULTI-CLIENT DRONE INPUT", new GUIStyle(GUI.skin.label) 
        { 
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.cyan }
        });
        
        GUILayout.Label(statusMessage);
        GUILayout.Label($"Clientes activos: {activeClients}");
        GUILayout.Label($"Mensajes totales: {totalMessagesReceived}");
        GUILayout.Label($"PeerConnections: {configuredPeerConnections.Count}");
        
        GUILayout.Space(5);
        
        // Mostrar estado de cada cliente
        foreach (var kvp in clientInputs)
        {
            var state = kvp.Value;
            string clientId = kvp.Key.Length > 8 ? kvp.Key.Substring(0, 8) + "..." : kvp.Key;
            bool active = (DateTime.Now - state.lastUpdate).TotalSeconds < 1f;
            
            GUI.color = active ? Color.green : Color.gray;
            GUILayout.Label($"  {clientId}: H:{state.horizontal:F1} V:{state.vertical:F1}");
            GUI.color = Color.white;
        }
        
        if ((DateTime.Now - lastMessageTime).TotalSeconds < 0.5f)
        {
            GUI.color = Color.green;
            GUILayout.Label("● Recibiendo datos");
            GUI.color = Color.white;
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
