using UnityEngine;
using Unity.RenderStreaming;
using Unity.RenderStreaming.Signaling;
using System.Reflection;

/// <summary>
/// Script para forzar la conexión del SignalingManager
/// Añadir a cualquier GameObject en la escena
/// </summary>
public class ForceSignalingConnection : MonoBehaviour
{
    private SignalingManager signalingManager;
    private bool hasStarted = false;
    private string signalingStatus = "Checking...";
    
    [Header("Configuración Manual")]
    public string signalingUrl = "ws://127.0.0.1:8888";
    
    void Start()
    {
        signalingManager = FindFirstObjectByType<SignalingManager>();
        
        if (signalingManager == null)
        {
            Debug.LogError("[ForceSignaling] ❌ SignalingManager no encontrado!");
            signalingStatus = "ERROR: No SignalingManager";
            return;
        }
        
        Debug.Log("[ForceSignaling] ✅ SignalingManager encontrado");
        
        // Verificar si está corriendo
        StartCoroutine(CheckAndStartSignaling());
    }
    
    System.Collections.IEnumerator CheckAndStartSignaling()
    {
        yield return new WaitForSeconds(0.5f);
        
        // Verificar estado
        var runningField = typeof(SignalingManager).GetField("m_running", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        bool isRunning = false;
        if (runningField != null)
        {
            isRunning = (bool)runningField.GetValue(signalingManager);
        }
        
        Debug.Log($"[ForceSignaling] Estado actual: Running = {isRunning}");
        
        // Verificar la URL de signaling
        CheckSignalingURL();
        
        // Verificar si realmente hay una conexión de signaling activa
        var signalingField = typeof(SignalingManager).GetField("m_signaling", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (signalingField != null)
        {
            var signaling = signalingField.GetValue(signalingManager);
            if (signaling != null)
            {
                Debug.Log($"[ForceSignaling] 📡 Signaling object: {signaling.GetType().Name}");
                signalingStatus = $"Signaling: {signaling.GetType().Name}";
                
                // Verificar si es WebSocketSignaling
                if (signaling is ISignaling wsSignaling)
                {
                    Debug.Log($"[ForceSignaling] ✅ ISignaling implementado");
                }
            }
            else
            {
                Debug.LogWarning("[ForceSignaling] ⚠️ Signaling object es NULL!");
                signalingStatus = "ERROR: Signaling is NULL";
            }
        }
        
        hasStarted = true;
        
        // Si Running es true pero no hay conexión real, intentar reiniciar
        yield return new WaitForSeconds(2f);
        
        // Verificar si el webserver recibió la conexión de Unity
        Debug.Log("[ForceSignaling] 💡 Si el webserver NO muestra conexión de Unity:");
        Debug.Log("[ForceSignaling]    1. Verifica que el webserver esté corriendo");
        Debug.Log("[ForceSignaling]    2. La URL debe ser ws://127.0.0.1:8888");
        Debug.Log("[ForceSignaling]    3. Prueba desmarcando 'Use Default Settings' en SignalingManager");
    }
    
    void CheckSignalingURL()
    {
        // Intentar obtener la URL configurada
        var signalingField = typeof(SignalingManager).GetField("m_signaling", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (signalingField != null)
        {
            var signalingSettings = signalingField.GetValue(signalingManager);
            if (signalingSettings != null)
            {
                Debug.Log($"[ForceSignaling] Signaling Type: {signalingSettings.GetType().Name}");
                
                // Intentar obtener la URL de diferentes maneras
                var urlProp = signalingSettings.GetType().GetProperty("Url");
                if (urlProp != null)
                {
                    var url = urlProp.GetValue(signalingSettings);
                    Debug.Log($"[ForceSignaling] 📡 URL (Url): {url}");
                }
                
                var urlField = signalingSettings.GetType().GetField("m_url", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (urlField != null)
                {
                    var url = urlField.GetValue(signalingSettings);
                    Debug.Log($"[ForceSignaling] 📡 URL (m_url): {url}");
                }
            }
        }
        
        // También verificar el settings asset
        var settingsField = typeof(SignalingManager).GetField("m_settings", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (settingsField != null)
        {
            var settings = settingsField.GetValue(signalingManager);
            if (settings != null)
            {
                Debug.Log($"[ForceSignaling] Settings Asset: {settings}");
            }
        }
    }
    
    void Update()
    {
        if (!hasStarted) return;
        
        // Monitorear el estado cada 5 segundos
        if (Time.frameCount % 300 == 0)
        {
            var runningField = typeof(SignalingManager).GetField("m_running", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (runningField != null && signalingManager != null)
            {
                bool isRunning = (bool)runningField.GetValue(signalingManager);
                Debug.Log($"[ForceSignaling] 📊 SignalingManager Running: {isRunning}");
            }
        }
    }
    
    void OnGUI()
    {
           }
    
    void RestartSignaling()
    {
        if (signalingManager == null) return;
        
        Debug.Log("[ForceSignaling] 🔄 Reiniciando Signaling...");
        
        // Stop
        var stopMethod = typeof(SignalingManager).GetMethod("Stop", 
            BindingFlags.Public | BindingFlags.Instance);
        if (stopMethod != null) 
        {
            stopMethod.Invoke(signalingManager, null);
            Debug.Log("[ForceSignaling] Stop() llamado");
        }
        
        // Esperar un poco
        StartCoroutine(RestartAfterDelay());
    }
    
    System.Collections.IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        
        // Run
        var runMethod = typeof(SignalingManager).GetMethod("Run", 
            BindingFlags.Public | BindingFlags.Instance);
        if (runMethod != null) 
        {
            runMethod.Invoke(signalingManager, null);
            Debug.Log("[ForceSignaling] Run() llamado");
        }
        
        Debug.Log("[ForceSignaling] ✅ Signaling reiniciado - Verifica el webserver");
    }
}
