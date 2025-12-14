using UnityEngine;
using Unity.RenderStreaming;
using Unity.WebRTC;
using System;
using System.Collections.Generic;

/// <summary>
/// Recibe input via DataChannel usando el sistema de Unity Render Streaming
/// IMPORTANTE: Agregar este componente al mismo GameObject que tiene el Broadcast/SingleConnection
/// </summary>
public class DataChannelInputReceiver : MonoBehaviour, IDataChannel
{
    [Header("Configuración")]
    [SerializeField] private string channelLabel = "input";
    [SerializeField] private DroneController droneController;
    
    [Header("Debug")]
    [SerializeField] private bool logMessages = true;
    
    // IDataChannel implementation
    public string Label => channelLabel;
    public RTCDataChannel Channel { get; private set; }
    public bool IsLocal => false;
    public bool IsConnected => Channel != null && Channel.ReadyState == RTCDataChannelState.Open;
    public string ConnectionId { get; private set; }
    
    // Stats
    private int messagesReceived = 0;
    private string lastMessage = "";
    private DateTime lastReceiveTime;
    private string statusText = "Esperando conexión...";
    
    // Diccionario para multi-cliente
    private Dictionary<string, DroneController> clientDrones = new Dictionary<string, DroneController>();
    
    void Start()
    {
        // Buscar DroneController si no está asignado
        if (droneController == null)
        {
            droneController = FindFirstObjectByType<DroneController>();
        }
    }
    
    /// <summary>
    /// Llamado por Unity Render Streaming cuando se establece el DataChannel (nueva API)
    /// </summary>
    public void SetChannel(SignalingEventData data)
    {
        SetChannel(data.connectionId, data.channel);
    }
    
    /// <summary>
    /// Llamado por Unity Render Streaming cuando se establece el DataChannel
    /// </summary>
    public void SetChannel(string connectionId, RTCDataChannel channel)
    {
        if (channel == null) return;
        
        ConnectionId = connectionId;
        Channel = channel;
        statusText = $"Canal configurado: {channel.Label}";
        
        channel.OnOpen = () =>
        {
            statusText = "✅ Canal ABIERTO";
        };
        
        channel.OnClose = () =>
        {
            statusText = "Canal cerrado";
            Channel = null;
        };
        
        channel.OnMessage = (bytes) =>
        {
            string message = System.Text.Encoding.UTF8.GetString(bytes);
            ProcessMessage(connectionId, message);
        };
        
        if (channel.ReadyState == RTCDataChannelState.Open)
        {
            statusText = "✅ Canal ABIERTO (ya estaba)";
        }
    }
    
    private void ProcessMessage(string connectionId, string message)
    {
        messagesReceived++;
        lastMessage = message;
        lastReceiveTime = DateTime.Now;
        
        try
        {
            // Parsear input
            if (message.Contains("\"type\":\"input\""))
            {
                float h = ExtractFloat(message, "horizontal");
                float v = ExtractFloat(message, "vertical");
                float e = ExtractFloat(message, "elevation");
                float r = ExtractFloat(message, "rotation");
                
                // Aplicar al dron
                if (droneController != null)
                {
                    droneController.SetInput(h, v, e, r);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DataChannelInputReceiver] Error: {ex.Message}");
        }
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
        GUILayout.BeginArea(new Rect(10, Screen.height - 180, 300, 170));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("📡 DATA CHANNEL INPUT", new GUIStyle(GUI.skin.label) 
        { 
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.cyan }
        });
        
        GUILayout.Label($"Status: {statusText}");
        GUILayout.Label($"Channel: {(Channel != null ? Channel.Label : "ninguno")}");
        GUILayout.Label($"State: {(Channel != null ? Channel.ReadyState.ToString() : "N/A")}");
        GUILayout.Label($"Mensajes: {messagesReceived}");
        
        bool receiving = (DateTime.Now - lastReceiveTime).TotalSeconds < 1f;
        GUI.color = receiving ? Color.green : Color.gray;
        GUILayout.Label(receiving ? "● Recibiendo" : "○ Sin datos");
        GUI.color = Color.white;
        
        if (!string.IsNullOrEmpty(lastMessage) && lastMessage.Length > 50)
        {
            GUILayout.Label($"Último: {lastMessage.Substring(0, 50)}...");
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
