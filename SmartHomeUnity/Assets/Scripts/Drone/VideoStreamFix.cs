using UnityEngine;
using Unity.RenderStreaming;
using System.Reflection;

/// <summary>
/// FIX para el problema de "a=inactive" en el streaming de video.
/// 
/// El problema: Cuando Unity responde al SDP offer del navegador,
/// el video track está marcado como "inactive" porque el VideoStreamSender
/// no tiene una cámara configurada o no está activo.
/// 
/// La solución: Este script fuerza la cámara del VideoStreamSender
/// ANTES de que comience cualquier negociación WebRTC.
/// 
/// USO:
/// 1. Añade este script a un GameObject en la escena (puede ser el mismo que tiene SignalingManager)
/// 2. Asigna la cámara que quieres transmitir en el campo "Stream Camera"
/// 3. El script configurará automáticamente el VideoStreamSender
/// 
/// IMPORTANTE: El Script Execution Order de este script debe ser menor que SignalingManager
/// </summary>
[DefaultExecutionOrder(-200)] // Ejecutar MUY antes que otros scripts
public class VideoStreamFix : MonoBehaviour
{
    [Header("Cámara a transmitir")]
    [Tooltip("La cámara cuya vista se transmitirá. Si está vacía, buscará una cámara con 'Drone' o 'Stream' en el nombre.")]
    public Camera streamCamera;
    
    [Header("Referencias (auto-detectadas si vacías)")]
    public VideoStreamSender videoStreamSender;
    public SignalingManager signalingManager;
    
    [Header("Configuración")]
    [Tooltip("Crear una cámara de streaming si no se encuentra ninguna")]
    public bool createCameraIfMissing = true;
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    
    // Singleton para acceso desde otros scripts
    public static VideoStreamFix Instance { get; private set; }
    
    void Awake()
    {
        Instance = this;
        
        Log("🔧 VideoStreamFix - Iniciando corrección de streaming...");
        
        // 1. Buscar componentes
        FindComponents();
        
        // 2. Buscar o crear cámara
        SetupCamera();
        
        // 3. Configurar VideoStreamSender ANTES de que SignalingManager inicie
        ConfigureVideoStreamSender();
        
        Log("✅ VideoStreamFix - Configuración completada");
    }
    
    void FindComponents()
    {
        if (videoStreamSender == null)
        {
            videoStreamSender = FindFirstObjectByType<VideoStreamSender>();
            if (videoStreamSender != null)
                Log($"📹 VideoStreamSender encontrado: {videoStreamSender.gameObject.name}");
            else
                LogError("❌ VideoStreamSender NO encontrado!");
        }
        
        if (signalingManager == null)
        {
            signalingManager = FindFirstObjectByType<SignalingManager>();
            if (signalingManager != null)
                Log($"📡 SignalingManager encontrado: {signalingManager.gameObject.name}");
        }
    }
    
    void SetupCamera()
    {
        // Si ya tenemos cámara, usarla
        if (streamCamera != null)
        {
            Log($"📷 Usando cámara asignada: {streamCamera.name}");
            return;
        }
        
        // Buscar cámara con nombres específicos
        var cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var cam in cameras)
        {
            if (cam.name.Contains("Drone") || cam.name.Contains("Stream"))
            {
                streamCamera = cam;
                Log($"📷 Cámara encontrada por nombre: {cam.name}");
                return;
            }
        }
        
        // Usar Camera.main
        if (Camera.main != null)
        {
            streamCamera = Camera.main;
            Log($"📷 Usando Camera.main: {streamCamera.name}");
            return;
        }
        
        // Crear cámara si está habilitado
        if (createCameraIfMissing)
        {
            GameObject camObj = new GameObject("VideoStreamCamera");
            camObj.transform.SetParent(transform);
            streamCamera = camObj.AddComponent<Camera>();
            streamCamera.fieldOfView = 90;
            streamCamera.nearClipPlane = 0.1f;
            streamCamera.farClipPlane = 1000f;
            Log("📷 Cámara de streaming creada: VideoStreamCamera");
        }
    }
    
    void ConfigureVideoStreamSender()
    {
        if (videoStreamSender == null)
        {
            LogError("❌ No se puede configurar - VideoStreamSender es null");
            return;
        }
        
        if (streamCamera == null)
        {
            LogError("❌ No se puede configurar - streamCamera es null");
            return;
        }
        
        var senderType = videoStreamSender.GetType();
        var bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
        
        // === PASO 1: Establecer sourceType a Camera (valor 0) ===
        var sourceTypeField = senderType.GetField("m_sourceType", bindingFlags);
        if (sourceTypeField != null)
        {
            try
            {
                // El enum VideoStreamSource tiene: Camera = 0, Screen = 1, Texture = 2, WebCam = 3
                sourceTypeField.SetValue(videoStreamSender, 0);
                Log("✅ m_sourceType = 0 (Camera)");
            }
            catch (System.Exception e)
            {
                LogWarning($"⚠️ Error en m_sourceType: {e.Message}");
            }
        }
        else
        {
            LogWarning("⚠️ Campo m_sourceType no encontrado");
        }
        
        // === PASO 2: Asignar la cámara al campo m_source ===
        var sourceField = senderType.GetField("m_source", bindingFlags);
        if (sourceField != null)
        {
            try
            {
                sourceField.SetValue(videoStreamSender, streamCamera);
                Log($"✅ m_source = {streamCamera.name}");
            }
            catch (System.Exception e)
            {
                LogWarning($"⚠️ Error en m_source: {e.Message}");
            }
        }
        else
        {
            // Intentar variantes
            foreach (var field in senderType.GetFields(bindingFlags))
            {
                if (field.Name.ToLower().Contains("source") && 
                    (field.FieldType == typeof(UnityEngine.Object) || 
                     field.FieldType == typeof(Camera) ||
                     field.FieldType.IsAssignableFrom(typeof(Camera))))
                {
                    try
                    {
                        field.SetValue(videoStreamSender, streamCamera);
                        Log($"✅ {field.Name} = {streamCamera.name}");
                        break;
                    }
                    catch { }
                }
            }
        }
        
        // === PASO 3: Verificar configuración ===
        Log("=== Verificación de VideoStreamSender ===");
        foreach (var field in senderType.GetFields(bindingFlags))
        {
            if (field.Name.StartsWith("m_"))
            {
                try
                {
                    var value = field.GetValue(videoStreamSender);
                    Log($"  {field.Name} = {value}");
                }
                catch { }
            }
        }
        
        // Asegurar que está habilitado
        videoStreamSender.enabled = true;
        Log($"📹 VideoStreamSender.enabled = {videoStreamSender.enabled}");
    }
    
    /// <summary>
    /// Cambia la cámara del streaming en tiempo de ejecución
    /// </summary>
    public void SetStreamCamera(Camera newCamera)
    {
        if (newCamera == null)
        {
            LogWarning("⚠️ SetStreamCamera: cámara es null");
            return;
        }
        
        Log($"🔄 Cambiando cámara a: {newCamera.name}");
        streamCamera = newCamera;
        ConfigureVideoStreamSender();
    }
    
    /// <summary>
    /// Método estático para cambiar la cámara desde cualquier script
    /// </summary>
    public static void SwitchCamera(Camera camera)
    {
        if (Instance != null)
        {
            Instance.SetStreamCamera(camera);
        }
        else
        {
            Debug.LogError("[VideoStreamFix] Instance es null - asegúrate de que el script está en la escena");
        }
    }
    
    void Log(string message)
    {
        if (showDebugLogs)
            Debug.Log($"[VideoStreamFix] {message}");
    }
    
    void LogWarning(string message)
    {
        Debug.LogWarning($"[VideoStreamFix] {message}");
    }
    
    void LogError(string message)
    {
        Debug.LogError($"[VideoStreamFix] {message}");
    }
    
    void OnGUI()
    {
        if (!showDebugLogs) return;
        
        GUIStyle style = new GUIStyle();
        style.fontSize = 14;
        style.normal.textColor = Color.white;
        style.normal.background = Texture2D.grayTexture;
        
        GUILayout.BeginArea(new Rect(Screen.width - 320, 10, 310, 120));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("=== VideoStreamFix ===", style);
        
        style.normal.textColor = streamCamera != null ? Color.green : Color.red;
        GUILayout.Label($"Camera: {(streamCamera != null ? streamCamera.name : "NULL")}", style);
        
        style.normal.textColor = videoStreamSender != null ? Color.green : Color.red;
        GUILayout.Label($"VideoStreamSender: {(videoStreamSender != null ? "OK" : "NULL")}", style);
        
        if (streamCamera != null)
        {
            style.normal.textColor = Color.cyan;
            GUILayout.Label($"Pos: {streamCamera.transform.position}", style);
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
