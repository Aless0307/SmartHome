using UnityEngine;
using Unity.RenderStreaming;
using Unity.RenderStreaming.Signaling;
using System;
using System.Reflection;

/// <summary>
/// Script que fuerza la creación de un WebSocketSignaling manualmente
/// Esto bypasea el problema del SignalingManager que no crea la conexión
/// </summary>
public class ManualWebSocketSignaling : MonoBehaviour
{
    [Header("Configuración")]
    public string signalingUrl = "ws://127.0.0.1:8888";
    public bool autoStart = true;
    
    private SignalingManager signalingManager;
    private bool isConnected = false;
    private string statusMessage = "Iniciando...";
    
    void Start()
    {
        if (autoStart)
        {
            StartCoroutine(InitializeSignaling());
        }
    }
    
    System.Collections.IEnumerator InitializeSignaling()
    {
        yield return new WaitForSeconds(1f);
        
        signalingManager = FindFirstObjectByType<SignalingManager>();
        
        if (signalingManager == null)
        {
            statusMessage = "ERROR: No SignalingManager encontrado";
            Debug.LogError("[ManualWS] " + statusMessage);
            yield break;
        }
        
        Debug.Log("[ManualWS] SignalingManager encontrado, verificando configuración...");
        
        // Verificar el estado actual
        var runningField = typeof(SignalingManager).GetField("m_running", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        bool isRunning = runningField != null && (bool)runningField.GetValue(signalingManager);
        
        Debug.Log($"[ManualWS] Running: {isRunning}");
        
        // Verificar si hay un signaling creado
        var signalingField = typeof(SignalingManager).GetField("m_signaling", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        var currentSignaling = signalingField?.GetValue(signalingManager);
        
        if (currentSignaling != null)
        {
            Debug.Log($"[ManualWS] ✅ Signaling existente: {currentSignaling.GetType().Name}");
            statusMessage = "Signaling OK: " + currentSignaling.GetType().Name;
            isConnected = true;
        }
        else
        {
            Debug.LogWarning("[ManualWS] ⚠️ NO hay signaling creado!");
            statusMessage = "No signaling - intentando crear...";
            
            // Intentar forzar la creación
            yield return StartCoroutine(TryForceSignaling());
        }
    }
    
    System.Collections.IEnumerator TryForceSignaling()
    {
        Debug.Log("[ManualWS] Intentando crear WebSocketSignaling manualmente...");
        
        // Primero, detener el SignalingManager si está "corriendo"
        var stopMethod = typeof(SignalingManager).GetMethod("Stop", 
            BindingFlags.Public | BindingFlags.Instance);
        if (stopMethod != null)
        {
            stopMethod.Invoke(signalingManager, null);
            Debug.Log("[ManualWS] SignalingManager detenido");
        }
        
        yield return new WaitForSeconds(0.5f);
        
        // Crear WebSocketSignaling manualmente
        try
        {
            // El tipo WebSocketSignaling debería estar en el assembly
            var wsType = Type.GetType("Unity.RenderStreaming.Signaling.WebSocketSignaling, Unity.RenderStreaming");
            
            if (wsType == null)
            {
                // Buscar en todos los assemblies
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    wsType = assembly.GetType("Unity.RenderStreaming.Signaling.WebSocketSignaling");
                    if (wsType != null) break;
                }
            }
            
            if (wsType != null)
            {
                Debug.Log($"[ManualWS] ✅ WebSocketSignaling type encontrado: {wsType.FullName}");
                
                // Buscar constructor
                var constructors = wsType.GetConstructors();
                Debug.Log($"[ManualWS] Constructores encontrados: {constructors.Length}");
                
                foreach (var ctor in constructors)
                {
                    var parameters = ctor.GetParameters();
                    Debug.Log($"[ManualWS] Constructor: {parameters.Length} parámetros");
                    foreach (var p in parameters)
                    {
                        Debug.Log($"[ManualWS]   - {p.Name}: {p.ParameterType.Name}");
                    }
                }
                
                // Intentar crear instancia
                object wsInstance = null;
                
                // Probar diferentes constructores
                try
                {
                    // Constructor con URL string
                    var ctorWithUrl = wsType.GetConstructor(new Type[] { typeof(string) });
                    if (ctorWithUrl != null)
                    {
                        wsInstance = ctorWithUrl.Invoke(new object[] { signalingUrl });
                        Debug.Log("[ManualWS] ✅ WebSocketSignaling creado con URL");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ManualWS] Constructor con URL falló: {e.Message}");
                }
                
                if (wsInstance == null)
                {
                    try
                    {
                        // Constructor con URL y timeout
                        var ctorWithTimeout = wsType.GetConstructor(new Type[] { typeof(string), typeof(float) });
                        if (ctorWithTimeout != null)
                        {
                            wsInstance = ctorWithTimeout.Invoke(new object[] { signalingUrl, 5f });
                            Debug.Log("[ManualWS] ✅ WebSocketSignaling creado con URL y timeout");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[ManualWS] Constructor con timeout falló: {e.Message}");
                    }
                }
                
                if (wsInstance != null)
                {
                    // Asignar al SignalingManager
                    var signalingField = typeof(SignalingManager).GetField("m_signaling", 
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    
                    if (signalingField != null)
                    {
                        signalingField.SetValue(signalingManager, wsInstance);
                        Debug.Log("[ManualWS] ✅ WebSocketSignaling asignado al SignalingManager");
                        
                        // Ahora iniciar
                        var runMethod = typeof(SignalingManager).GetMethod("Run", 
                            BindingFlags.Public | BindingFlags.Instance);
                        if (runMethod != null)
                        {
                            runMethod.Invoke(signalingManager, null);
                            Debug.Log("[ManualWS] ✅ SignalingManager.Run() llamado");
                            
                            statusMessage = "Signaling manual iniciado";
                            isConnected = true;
                        }
                    }
                }
                else
                {
                    statusMessage = "No se pudo crear WebSocketSignaling";
                    Debug.LogError("[ManualWS] No se pudo crear instancia de WebSocketSignaling");
                }
            }
            else
            {
                statusMessage = "WebSocketSignaling type no encontrado";
                Debug.LogError("[ManualWS] ❌ WebSocketSignaling type no encontrado en ningún assembly");
                
                // Listar tipos disponibles en Unity.RenderStreaming
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.FullName.Contains("RenderStreaming"))
                    {
                        Debug.Log($"[ManualWS] Assembly: {assembly.FullName}");
                        foreach (var type in assembly.GetTypes())
                        {
                            if (type.Name.Contains("Signaling"))
                            {
                                Debug.Log($"[ManualWS]   Type: {type.FullName}");
                            }
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            statusMessage = $"Error: {e.Message}";
            Debug.LogError($"[ManualWS] ❌ Error: {e}");
        }
    }
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 200, 500, 150));
        GUILayout.BeginVertical("box");
        
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
        GUILayout.Label("=== MANUAL WEBSOCKET DEBUG ===", titleStyle);
        
        GUIStyle greenStyle = new GUIStyle(GUI.skin.label) { normal = { textColor = Color.green } };
        GUIStyle redStyle = new GUIStyle(GUI.skin.label) { normal = { textColor = Color.red } };
        GUIStyle yellowStyle = new GUIStyle(GUI.skin.label) { normal = { textColor = Color.yellow } };
        
        GUILayout.Label($"URL: {signalingUrl}");
        GUILayout.Label($"Status: {statusMessage}", isConnected ? greenStyle : yellowStyle);
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("🔧 Forzar Conexión Manual"))
        {
            StartCoroutine(TryForceSignaling());
        }
        
        if (GUILayout.Button("📋 Listar Tipos de Signaling"))
        {
            ListSignalingTypes();
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
    
    void ListSignalingTypes()
    {
        Debug.Log("[ManualWS] === LISTANDO TIPOS DE SIGNALING ===");
        
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.Name.Contains("Signaling") && !type.IsInterface)
                    {
                        Debug.Log($"[ManualWS] {type.FullName} en {assembly.GetName().Name}");
                    }
                }
            }
            catch { }
        }
    }
}
