using UnityEngine;
using Unity.RenderStreaming;
using System;

/// <summary>
/// Recibe input del navegador via DataChannel y controla el dron
/// </summary>
public class DroneDataChannelHandler : MonoBehaviour
{
    [Header("Drone")]
    [SerializeField] private DroneController droneController;
    
    [Header("Debug")]
    [SerializeField] private bool logMessages = true;
    
    private int messagesReceived = 0;
    private DateTime lastReceiveTime;
    private string statusText = "Inicializando...";
    
    private InputChannelReceiverBase inputReceiver;
    
    void Start()
    {
        Debug.Log("[DroneDataChannel] 🎮 Iniciado");
        
        // Buscar DroneController
        if (droneController == null)
        {
            droneController = FindFirstObjectByType<DroneController>();
            if (droneController != null)
                Debug.Log($"[DroneDataChannel] ✅ Dron encontrado: {droneController.name}");
            else
                Debug.LogWarning("[DroneDataChannel] ⚠️ No se encontró DroneController");
        }
        
        // Buscar InputChannelReceiverBase para suscribirse a eventos
        inputReceiver = GetComponent<InputChannelReceiverBase>();
        if (inputReceiver != null)
        {
            Debug.Log("[DroneDataChannel] ✅ InputChannelReceiver encontrado");
        }
    }
    
    /// <summary>
    /// Llamar este método cuando se reciba un mensaje del DataChannel
    /// </summary>
    public void OnMessageReceived(byte[] bytes)
    {
        string message = System.Text.Encoding.UTF8.GetString(bytes);
        ProcessMessage(message);
    }
    
    /// <summary>
    /// Llamar este método cuando se reciba un mensaje como string
    /// </summary>
    public void OnMessageReceived(string message)
    {
        ProcessMessage(message);
    }
    
    private void ProcessMessage(string message)
    {
        messagesReceived++;
        lastReceiveTime = DateTime.Now;
        
        if (logMessages)
        {
            Debug.Log($"[DroneDataChannel] 📩 {message}");
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
                    statusText = $"✅ Input: H={h:F1} V={v:F1}";
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DroneDataChannel] Error: {ex.Message}");
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
        
        float.TryParse(valueStr, System.Globalization.NumberStyles.Float, 
            System.Globalization.CultureInfo.InvariantCulture, out float result);
        return result;
    }
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, Screen.height - 120, 250, 110));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("🎮 DRONE INPUT CHANNEL", new GUIStyle(GUI.skin.label) 
        { 
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.cyan }
        });
        
        GUILayout.Label($"Estado: {statusText}");
        GUILayout.Label($"Mensajes: {messagesReceived}");
        
        bool active = (DateTime.Now - lastReceiveTime).TotalSeconds < 1f;
        GUI.color = active ? Color.green : Color.gray;
        GUILayout.Label(active ? "● Recibiendo datos" : "○ Sin datos recientes");
        GUI.color = Color.white;
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
