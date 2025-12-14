using UnityEngine;
using Unity.RenderStreaming;
using System.Reflection;
using System.Linq;

/// <summary>
/// Script de diagnóstico detallado para Unity Render Streaming
/// Muestra toda la configuración actual y detecta problemas
/// </summary>
public class RenderStreamingDebug : MonoBehaviour
{
    private SignalingManager signalingManager;
    private Broadcast broadcast;
    private float checkInterval = 2f;
    private float timer = 0f;
    
    void Start()
    {
        signalingManager = FindFirstObjectByType<SignalingManager>();
        broadcast = FindFirstObjectByType<Broadcast>();
        
        Debug.Log("╔════════════════════════════════════════════════════════════╗");
        Debug.Log("║      UNITY RENDER STREAMING - DIAGNÓSTICO COMPLETO         ║");
        Debug.Log("╚════════════════════════════════════════════════════════════╝");
        
        CheckSignalingManager();
        CheckBroadcast();
        CheckVideoStreamSender();
        
        Debug.Log("════════════════════════════════════════════════════════════");
    }
    
    void CheckSignalingManager()
    {
        Debug.Log("\n📡 SIGNALING MANAGER:");
        
        if (signalingManager == null)
        {
            Debug.LogError("  ❌ NO ENCONTRADO - Añade SignalingManager al GameObject");
            return;
        }
        
        Debug.Log($"  ✅ Encontrado en: {signalingManager.gameObject.name}");
        Debug.Log($"  • Run On Awake: {signalingManager.runOnAwake}");
        
        // Intentar obtener la URL del signaling
        var signalingField = signalingManager.GetType().GetField("m_signaling", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (signalingField != null)
        {
            var signaling = signalingField.GetValue(signalingManager);
            Debug.Log($"  • Signaling Type: {signaling?.GetType().Name ?? "NULL"}");
        }
        
        // Verificar handlers
        var handlersField = signalingManager.GetType().GetField("m_handlers",
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (handlersField != null)
        {
            var handlers = handlersField.GetValue(signalingManager) as System.Collections.IList;
            if (handlers != null)
            {
                Debug.Log($"  • Handlers registrados: {handlers.Count}");
                foreach (var handler in handlers)
                {
                    Debug.Log($"    - {handler?.GetType().Name ?? "NULL"} ({handler})");
                }
                
                if (handlers.Count == 0)
                {
                    Debug.LogError("  ⚠️ NO HAY HANDLERS - Añade Broadcast a 'Signaling Handler List'");
                }
            }
        }
    }
    
    void CheckBroadcast()
    {
        Debug.Log("\n📺 BROADCAST:");
        
        if (broadcast == null)
        {
            Debug.LogError("  ❌ NO ENCONTRADO - Añade Broadcast al GameObject");
            return;
        }
        
        Debug.Log($"  ✅ Encontrado en: {broadcast.gameObject.name}");
        
        // Verificar streams
        var streams = broadcast.Streams.ToList();
        Debug.Log($"  • Streams configurados: {streams.Count}");
        
        foreach (var stream in streams)
        {
            Debug.Log($"    - {stream?.GetType().Name}: {stream}");
        }
        
        if (streams.Count == 0)
        {
            Debug.LogWarning("  ⚠️ NO HAY STREAMS - Añade VideoStreamSender a 'Streams'");
        }
    }
    
    void CheckVideoStreamSender()
    {
        Debug.Log("\n🎥 VIDEO STREAM SENDER:");
        
        var videoSender = FindFirstObjectByType<VideoStreamSender>();
        
        if (videoSender == null)
        {
            Debug.LogError("  ❌ NO ENCONTRADO - Añade VideoStreamSender al GameObject");
            return;
        }
        
        Debug.Log($"  ✅ Encontrado en: {videoSender.gameObject.name}");
        Debug.Log($"  • Enabled: {videoSender.enabled}");
        
        // Verificar cámara source
        var cameraField = videoSender.GetType().GetField("m_source",
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (cameraField != null)
        {
            var camera = cameraField.GetValue(videoSender) as Camera;
            Debug.Log($"  • Source Camera: {camera?.name ?? "NULL"}");
            
            if (camera == null)
            {
                Debug.LogWarning("  ⚠️ NO HAY CÁMARA - Asigna una Camera en 'Source'");
            }
        }
    }
    
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= checkInterval)
        {
            timer = 0f;
            
            if (broadcast != null)
            {
                var streams = broadcast.Streams.ToList();
                Debug.Log($"[RenderStreamingDebug] 📊 Broadcast Streams: {streams.Count}");
            }
            
            if (signalingManager != null)
            {
                // Verificar si el signaling está conectado
                var runningField = signalingManager.GetType().GetField("m_running",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (runningField != null)
                {
                    var isRunning = (bool)runningField.GetValue(signalingManager);
                    Debug.Log($"[RenderStreamingDebug] 🔌 Signaling Running: {isRunning}");
                }
            }
        }
    }
    
    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 14;
        
        GUILayout.BeginArea(new Rect(10, 10, 400, 200));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("=== Render Streaming Debug ===", style);
        
        style.normal.textColor = signalingManager != null ? Color.green : Color.red;
        GUILayout.Label($"SignalingManager: {(signalingManager != null ? "OK" : "MISSING")}", style);
        
        style.normal.textColor = broadcast != null ? Color.green : Color.red;
        GUILayout.Label($"Broadcast: {(broadcast != null ? "OK" : "MISSING")}", style);
        
        if (broadcast != null)
        {
            style.normal.textColor = Color.cyan;
            GUILayout.Label($"Active Streams: {broadcast.Streams.Count()}", style);
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
