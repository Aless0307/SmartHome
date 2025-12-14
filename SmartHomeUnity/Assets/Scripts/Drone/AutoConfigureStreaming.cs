using UnityEngine;
using Unity.RenderStreaming;
using System.Reflection;
using System.Collections;

/// <summary>
/// Script que configura automáticamente Unity Render Streaming
/// Añade el Broadcast a la lista de handlers del SignalingManager
/// </summary>
[DefaultExecutionOrder(-100)] // Ejecutar antes que otros scripts
public class AutoConfigureStreaming : MonoBehaviour
{
    void Awake()
    {
        Debug.Log("[AutoConfig] 🔧 Configurando Unity Render Streaming automáticamente...");
        
        var signalingManager = GetComponent<SignalingManager>();
        var broadcast = GetComponent<Broadcast>();
        
        if (signalingManager == null)
        {
            Debug.LogError("[AutoConfig] ❌ SignalingManager no encontrado en este GameObject");
            return;
        }
        
        if (broadcast == null)
        {
            Debug.LogError("[AutoConfig] ❌ Broadcast no encontrado en este GameObject");
            return;
        }
        
        // Obtener la lista de handlers usando reflexión
        var handlersField = typeof(SignalingManager).GetField("m_handlers", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (handlersField == null)
        {
            Debug.LogError("[AutoConfig] ❌ No se pudo acceder al campo m_handlers");
            return;
        }
        
        var handlers = handlersField.GetValue(signalingManager) as IList;
        
        if (handlers == null)
        {
            // Crear nueva lista si es null
            var listType = typeof(System.Collections.Generic.List<>).MakeGenericType(typeof(SignalingHandlerBase));
            handlers = System.Activator.CreateInstance(listType) as IList;
            handlersField.SetValue(signalingManager, handlers);
            Debug.Log("[AutoConfig] 📝 Lista de handlers creada");
        }
        
        // Verificar si el Broadcast ya está en la lista
        bool broadcastFound = false;
        foreach (var handler in handlers)
        {
            if (handler == broadcast)
            {
                broadcastFound = true;
                break;
            }
        }
        
        if (!broadcastFound)
        {
            handlers.Add(broadcast);
            Debug.Log("[AutoConfig] ✅ Broadcast añadido a la lista de handlers del SignalingManager");
        }
        else
        {
            Debug.Log("[AutoConfig] ℹ️ Broadcast ya estaba en la lista de handlers");
        }
        
        // Verificar VideoStreamSender
        var videoStreamSender = GetComponent<VideoStreamSender>();
        if (videoStreamSender != null)
        {
            // Intentar añadir el VideoStreamSender al Broadcast
            AddStreamToBroadcast(broadcast, videoStreamSender);
        }
        
        Debug.Log("[AutoConfig] ✅ Configuración completada");
        LogCurrentConfig(signalingManager, broadcast);
    }
    
    void AddStreamToBroadcast(Broadcast broadcast, VideoStreamSender videoStreamSender)
    {
        // Verificar si ya está añadido
        foreach (var stream in broadcast.Streams)
        {
            if (stream == videoStreamSender)
            {
                Debug.Log("[AutoConfig] ℹ️ VideoStreamSender ya está en el Broadcast");
                return;
            }
        }
        
        // Intentar usar el método AddComponent si existe
        var addMethod = typeof(Broadcast).GetMethod("AddComponent", 
            BindingFlags.Public | BindingFlags.Instance);
        
        if (addMethod != null)
        {
            try
            {
                addMethod.Invoke(broadcast, new object[] { videoStreamSender });
                Debug.Log("[AutoConfig] ✅ VideoStreamSender añadido al Broadcast");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[AutoConfig] ⚠️ No se pudo añadir VideoStreamSender: {e.Message}");
            }
        }
    }
    
    void LogCurrentConfig(SignalingManager signalingManager, Broadcast broadcast)
    {
        Debug.Log("╔══════════════════════════════════════════╗");
        Debug.Log("║   CONFIGURACIÓN ACTUAL                   ║");
        Debug.Log("╚══════════════════════════════════════════╝");
        
        // Contar handlers
        var handlersField = typeof(SignalingManager).GetField("m_handlers", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (handlersField != null)
        {
            var handlers = handlersField.GetValue(signalingManager) as IList;
            if (handlers != null)
            {
                Debug.Log($"  Handlers en SignalingManager: {handlers.Count}");
                for (int i = 0; i < handlers.Count; i++)
                {
                    var h = handlers[i];
                    Debug.Log($"    [{i}] {h?.GetType().Name ?? "null"}");
                }
            }
        }
        
        // Contar streams en Broadcast
        int streamCount = 0;
        foreach (var _ in broadcast.Streams) streamCount++;
        Debug.Log($"  Streams en Broadcast: {streamCount}");
    }
}
