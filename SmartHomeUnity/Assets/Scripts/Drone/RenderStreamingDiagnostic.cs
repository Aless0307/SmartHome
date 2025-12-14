using UnityEngine;
using Unity.RenderStreaming;

/// <summary>
/// Script de diagnóstico para verificar la configuración de Unity Render Streaming
/// Añade este script a cualquier GameObject para ver el estado en la consola
/// </summary>
public class RenderStreamingDiagnostic : MonoBehaviour
{
    void Start()
    {
        Debug.Log("╔══════════════════════════════════════════════════════════════╗");
        Debug.Log("║      DIAGNÓSTICO DE UNITY RENDER STREAMING                   ║");
        Debug.Log("╚══════════════════════════════════════════════════════════════╝");
        
        // Buscar SignalingManager
        var signalingManager = FindFirstObjectByType<SignalingManager>();
        if (signalingManager != null)
        {
            Debug.Log($"✅ SignalingManager encontrado en: {signalingManager.gameObject.name}");
            Debug.Log($"   - Run On Awake: {signalingManager.runOnAwake}");
        }
        else
        {
            Debug.LogError("❌ SignalingManager NO encontrado en la escena!");
        }
        
        // Buscar Broadcast
        var broadcast = FindFirstObjectByType<Broadcast>();
        if (broadcast != null)
        {
            Debug.Log($"✅ Broadcast encontrado en: {broadcast.gameObject.name}");
            
            // Verificar streams
            int streamCount = 0;
            foreach (var stream in broadcast.Streams)
            {
                streamCount++;
                Debug.Log($"   - Stream [{streamCount}]: {stream?.GetType().Name ?? "null"}");
            }
            
            if (streamCount == 0)
            {
                Debug.LogWarning("⚠️ Broadcast no tiene streams configurados!");
            }
        }
        else
        {
            Debug.LogError("❌ Broadcast NO encontrado en la escena!");
        }
        
        // Buscar VideoStreamSender
        var videoSender = FindFirstObjectByType<VideoStreamSender>();
        if (videoSender != null)
        {
            Debug.Log($"✅ VideoStreamSender encontrado en: {videoSender.gameObject.name}");
            Debug.Log($"   - Enabled: {videoSender.enabled}");
            
            // Intentar obtener información de la fuente
            var sourceType = videoSender.GetType().GetProperty("sourceType");
            if (sourceType != null)
            {
                Debug.Log($"   - Source Type: {sourceType.GetValue(videoSender)}");
            }
        }
        else
        {
            Debug.LogError("❌ VideoStreamSender NO encontrado en la escena!");
        }
        
        // Buscar DroneStreamingBridge
        var droneBridge = FindFirstObjectByType<DroneStreamingBridge>();
        if (droneBridge != null)
        {
            Debug.Log($"✅ DroneStreamingBridge encontrado en: {droneBridge.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("⚠️ DroneStreamingBridge NO encontrado - ¿Está añadido?");
        }
        
        // Buscar MultiClientDroneStreaming
        var droneManager = FindFirstObjectByType<MultiClientDroneStreaming>();
        if (droneManager != null)
        {
            Debug.Log($"✅ MultiClientDroneStreaming encontrado en: {droneManager.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("⚠️ MultiClientDroneStreaming NO encontrado");
        }
        
        Debug.Log("══════════════════════════════════════════════════════════════");
        Debug.Log("📋 INSTRUCCIONES:");
        Debug.Log("1. Asegúrate de que el webserver esté corriendo en puerto 8888");
        Debug.Log("2. En SignalingManager, añade Broadcast a 'Signaling Handler List'");
        Debug.Log("3. En Broadcast, añade VideoStreamSender a 'Streams'");
        Debug.Log("4. Abre http://127.0.0.1:8888/receiver/ en el navegador");
        Debug.Log("══════════════════════════════════════════════════════════════");
    }
    
    void Update()
    {
        // Presiona D para re-ejecutar diagnóstico
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Start();
        }
    }
}
