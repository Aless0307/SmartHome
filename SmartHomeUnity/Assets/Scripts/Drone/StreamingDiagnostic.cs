using UnityEngine;
using Unity.RenderStreaming;
using System.Collections;
using System.Reflection;

/// <summary>
/// Diagnóstico avanzado para Unity Render Streaming
/// Verifica la configuración y el estado del streaming
/// </summary>
public class StreamingDiagnostic : MonoBehaviour
{
    private SignalingManager signalingManager;
    private Broadcast broadcast;
    private VideoStreamSender videoStreamSender;
    
    private float checkInterval = 2f;
    private float timer = 0f;
    
    void Start()
    {
        signalingManager = FindFirstObjectByType<SignalingManager>();
        broadcast = FindFirstObjectByType<Broadcast>();
        videoStreamSender = FindFirstObjectByType<VideoStreamSender>();
        
        Debug.Log("╔══════════════════════════════════════════════════════════╗");
        Debug.Log("║     DIAGNÓSTICO DE UNITY RENDER STREAMING                ║");
        Debug.Log("╚══════════════════════════════════════════════════════════╝");
        
        CheckSignalingManager();
        CheckBroadcast();
        CheckVideoStreamSender();
        CheckHandlerList();
        
        Debug.Log("═══════════════════════════════════════════════════════════");
    }
    
    void CheckSignalingManager()
    {
        Debug.Log("\n📡 SIGNALING MANAGER:");
        if (signalingManager == null)
        {
            Debug.LogError("  ❌ NO ENCONTRADO");
            return;
        }
        
        Debug.Log($"  ✅ Encontrado en: {signalingManager.gameObject.name}");
        Debug.Log($"  • runOnAwake: {signalingManager.runOnAwake}");
        
        // Verificar URL del signaling
        var signalingField = typeof(SignalingManager).GetField("m_signaling", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (signalingField != null)
        {
            var signalingSettings = signalingField.GetValue(signalingManager);
            if (signalingSettings != null)
            {
                var urlField = signalingSettings.GetType().GetProperty("url");
                if (urlField != null)
                {
                    Debug.Log($"  • URL: {urlField.GetValue(signalingSettings)}");
                }
            }
        }
    }
    
    void CheckBroadcast()
    {
        Debug.Log("\n📺 BROADCAST:");
        if (broadcast == null)
        {
            Debug.LogError("  ❌ NO ENCONTRADO");
            return;
        }
        
        Debug.Log($"  ✅ Encontrado en: {broadcast.gameObject.name}");
        
        // Contar streams
        int streamCount = 0;
        foreach (var _ in broadcast.Streams) streamCount++;
        Debug.Log($"  • Streams activos: {streamCount}");
        
        // Verificar componentes del Broadcast
        var componentsField = typeof(Broadcast).GetField("m_streams", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (componentsField != null)
        {
            var components = componentsField.GetValue(broadcast) as IEnumerable;
            if (components != null)
            {
                int count = 0;
                foreach (var comp in components)
                {
                    Debug.Log($"    - Stream[{count}]: {comp?.GetType().Name ?? "null"}");
                    count++;
                }
                if (count == 0)
                {
                    Debug.LogWarning("  ⚠️ No hay streams configurados en el Broadcast");
                }
            }
        }
    }
    
    void CheckVideoStreamSender()
    {
        Debug.Log("\n🎥 VIDEO STREAM SENDER:");
        if (videoStreamSender == null)
        {
            Debug.LogError("  ❌ NO ENCONTRADO");
            return;
        }
        
        Debug.Log($"  ✅ Encontrado en: {videoStreamSender.gameObject.name}");
        Debug.Log($"  • enabled: {videoStreamSender.enabled}");
        
        // Verificar cámara
        var cameraField = typeof(VideoStreamSender).GetField("m_Camera", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (cameraField != null)
        {
            var camera = cameraField.GetValue(videoStreamSender) as Camera;
            Debug.Log($"  • Camera: {(camera != null ? camera.name : "NULL - ¡CONFIGURAR!")}");
        }
        
        // Verificar resolución
        var widthField = typeof(VideoStreamSender).GetField("m_Width", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        var heightField = typeof(VideoStreamSender).GetField("m_Height", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (widthField != null && heightField != null)
        {
            Debug.Log($"  • Resolución: {widthField.GetValue(videoStreamSender)}x{heightField.GetValue(videoStreamSender)}");
        }
    }
    
    void CheckHandlerList()
    {
        Debug.Log("\n📋 HANDLER LIST (SignalingManager):");
        if (signalingManager == null) return;
        
        var handlersField = typeof(SignalingManager).GetField("m_handlers", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (handlersField != null)
        {
            var handlers = handlersField.GetValue(signalingManager) as IList;
            if (handlers != null && handlers.Count > 0)
            {
                Debug.Log($"  • Total handlers: {handlers.Count}");
                for (int i = 0; i < handlers.Count; i++)
                {
                    var handler = handlers[i];
                    Debug.Log($"    [{i}] {handler?.GetType().Name ?? "null"} - {(handler != null ? ((MonoBehaviour)handler).gameObject.name : "")}");
                }
                
                // Verificar que Broadcast está en la lista
                bool hasBroadcast = false;
                foreach (var h in handlers)
                {
                    if (h is Broadcast)
                    {
                        hasBroadcast = true;
                        break;
                    }
                }
                
                if (!hasBroadcast)
                {
                    Debug.LogError("  ❌ BROADCAST NO ESTÁ EN LA LISTA DE HANDLERS!");
                    Debug.LogError("     -> Añade el Broadcast a 'Signaling Handler List' en el Inspector");
                }
                else
                {
                    Debug.Log("  ✅ Broadcast está en la lista de handlers");
                }
            }
            else
            {
                Debug.LogError("  ❌ NO HAY HANDLERS CONFIGURADOS");
                Debug.LogError("     -> Añade el Broadcast a 'Signaling Handler List' en el Inspector");
            }
        }
    }
    
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= checkInterval)
        {
            timer = 0f;
            LogRuntimeStatus();
        }
    }
    
    void LogRuntimeStatus()
    {
        if (broadcast == null) return;
        
        int streamCount = 0;
        foreach (var _ in broadcast.Streams) streamCount++;
        
        // Verificar si el SignalingManager está corriendo
        bool isRunning = false;
        var runningField = typeof(SignalingManager).GetField("m_running", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (runningField != null && signalingManager != null)
        {
            isRunning = (bool)runningField.GetValue(signalingManager);
        }
        
        Debug.Log($"[StreamingDiag] 📊 Running: {isRunning}, Streams: {streamCount}");
    }
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 200, 400, 200));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("=== Streaming Diagnostic ===");
        
        bool isRunning = false;
        if (signalingManager != null)
        {
            var runningField = typeof(SignalingManager).GetField("m_running", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (runningField != null)
            {
                isRunning = (bool)runningField.GetValue(signalingManager);
            }
        }
        
        GUILayout.Label($"SignalingManager Running: {(isRunning ? "YES" : "NO")}");
        
        int streamCount = 0;
        if (broadcast != null)
        {
            foreach (var _ in broadcast.Streams) streamCount++;
        }
        GUILayout.Label($"Active Streams: {streamCount}");
        
        GUILayout.Label($"VideoStreamSender: {(videoStreamSender != null && videoStreamSender.enabled ? "OK" : "NOT OK")}");
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
