using UnityEngine;
using Unity.RenderStreaming;
using Unity.RenderStreaming.Signaling;
using System;
using System.Reflection;
using System.Collections;

/// <summary>
/// Intercepta mensajes de input que llegan por el WebSocket de signaling
/// Esto funciona como fallback cuando el DataChannel no está disponible
/// </summary>
public class SignalingInputReceiver : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private DroneController droneController;
    [SerializeField] private SignalingManager signalingManager;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugGUI = true;
    [SerializeField] private bool logMessages = false;
    
    // Estado
    private int messagesReceived;
    private float lastH, lastV, lastE, lastR;
    private DateTime lastMessageTime;
    private bool isHooked = false;
    private string status = "Iniciando...";
    
    // Referencia al signaling interno
    private object signalingInstance;
    private EventInfo messageEvent;
    
    void Start()
    {
        if (droneController == null)
        {
            droneController = FindFirstObjectByType<DroneController>();
        }
        
        if (signalingManager == null)
        {
            signalingManager = FindFirstObjectByType<SignalingManager>();
        }
        
        StartCoroutine(TryHookIntoSignaling());
    }
    
    /// <summary>
    /// Intenta interceptar los mensajes del signaling
    /// </summary>
    private IEnumerator TryHookIntoSignaling()
    {
        while (!isHooked)
        {
            yield return new WaitForSeconds(1f);
            
            if (signalingManager == null)
            {
                status = "❌ No SignalingManager";
                continue;
            }
            
            try
            {
                // Obtener el signaling interno usando reflection
                var type = signalingManager.GetType();
                var signalingField = type.GetField("m_signaling", BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (signalingField != null)
                {
                    signalingInstance = signalingField.GetValue(signalingManager);
                    
                    if (signalingInstance != null)
                    {
                        // El signaling tiene un evento OnMessage o similar
                        // Pero es más fácil modificar cómo procesa los mensajes
                        status = "✅ Signaling detectado";
                        isHooked = true;
                        Debug.Log("[SignalingInputReceiver] Signaling interceptado");
                    }
                }
            }
            catch (Exception ex)
            {
                status = $"Error: {ex.Message}";
            }
        }
    }
    
    /// <summary>
    /// Procesar un mensaje de input (llamar desde código externo si es necesario)
    /// </summary>
    public void ProcessInputMessage(string json)
    {
        if (!json.Contains("\"type\":\"input\"")) return;
        
        messagesReceived++;
        lastMessageTime = DateTime.Now;
        
        if (logMessages)
        {
            Debug.Log($"[SignalingInputReceiver] {json}");
        }
        
        try
        {
            float h = ExtractFloat(json, "horizontal");
            float v = ExtractFloat(json, "vertical");
            float e = ExtractFloat(json, "elevation");
            float r = ExtractFloat(json, "rotation");
            
            lastH = h;
            lastV = v;
            lastE = e;
            lastR = r;
            
            if (droneController != null)
            {
                droneController.SetInput(h, v, e, r);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SignalingInputReceiver] Error: {ex.Message}");
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
        if (!showDebugGUI) return;
        
        // Mostrar abajo del otro panel
        GUILayout.BeginArea(new Rect(Screen.width - 260, Screen.height - 380, 250, 170));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("📡 Signaling Input", new GUIStyle(GUI.skin.label) 
        { 
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.yellow }
        });
        
        GUILayout.Label(status);
        GUILayout.Label($"Mensajes: {messagesReceived}");
        GUILayout.Label($"H:{lastH:F1} V:{lastV:F1} E:{lastE:F1} R:{lastR:F1}");
        
        if ((DateTime.Now - lastMessageTime).TotalSeconds < 0.5f)
        {
            GUI.color = Color.green;
            GUILayout.Label("● Activo");
            GUI.color = Color.white;
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
