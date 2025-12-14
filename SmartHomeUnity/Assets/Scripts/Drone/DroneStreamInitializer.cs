using UnityEngine;
using Unity.RenderStreaming;
using System.Collections;

/// <summary>
/// Este script se encarga de inicializar correctamente el VideoStreamSender
/// con una cámara del dron ANTES de que comience la negociación WebRTC.
/// 
/// IMPORTANTE: Este script debe ejecutarse ANTES que SignalingManager.
/// Configurar Script Execution Order: DroneStreamInitializer (-100) antes que SignalingManager (0)
/// </summary>
[DefaultExecutionOrder(-100)] // Ejecutar antes que otros scripts
public class DroneStreamInitializer : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private VideoStreamSender videoStreamSender;
    [SerializeField] private Camera droneCamera;
    [SerializeField] private GameObject dronePrefab;
    
    [Header("Configuración de Cámara")]
    [SerializeField] private int cameraWidth = 1920;
    [SerializeField] private int cameraHeight = 1080;
    [SerializeField] private int cameraDepth = 24;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    // Cámara activa para el streaming
    private static Camera activeStreamCamera;
    private static RenderTexture streamRenderTexture;
    private static DroneStreamInitializer instance;
    
    public static Camera ActiveStreamCamera => activeStreamCamera;
    public static RenderTexture StreamRenderTexture => streamRenderTexture;
    
    void Awake()
    {
        instance = this;
        Debug.Log("[DroneStreamInitializer] 🚀 Awake - Inicializando streaming del dron...");
        
        // Buscar VideoStreamSender si no está asignado
        if (videoStreamSender == null)
        {
            videoStreamSender = FindFirstObjectByType<VideoStreamSender>();
        }
        
        if (videoStreamSender == null)
        {
            Debug.LogError("[DroneStreamInitializer] ❌ No se encontró VideoStreamSender!");
            return;
        }
        
        // Configurar la cámara del streaming
        SetupStreamingCamera();
    }
    
    void Start()
    {
        Debug.Log("[DroneStreamInitializer] ✅ Start - Streaming configurado");
        StartCoroutine(VerifyStreamingSetup());
    }
    
    /// <summary>
    /// Configura la cámara que será usada para el streaming
    /// </summary>
    private void SetupStreamingCamera()
    {
        Debug.Log("[DroneStreamInitializer] 📷 Configurando cámara de streaming...");
        
        // 1. Buscar cámara del dron si no está asignada
        if (droneCamera == null)
        {
            // Buscar en la escena una cámara con "Drone" en el nombre
            var cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var cam in cameras)
            {
                if (cam.name.Contains("Drone") || cam.name.Contains("Stream"))
                {
                    droneCamera = cam;
                    Debug.Log($"[DroneStreamInitializer] 📷 Cámara encontrada: {cam.name}");
                    break;
                }
            }
        }
        
        // 2. Si no hay cámara del dron, crear una temporal que siga a Camera.main
        if (droneCamera == null)
        {
            Debug.Log("[DroneStreamInitializer] 📷 Creando cámara de streaming temporal...");
            
            GameObject camObj = new GameObject("DroneStreamCamera");
            camObj.transform.SetParent(transform);
            droneCamera = camObj.AddComponent<Camera>();
            droneCamera.fieldOfView = 90;
            droneCamera.nearClipPlane = 0.1f;
            droneCamera.farClipPlane = 1000f;
            
            // Copiar posición de la cámara principal
            if (Camera.main != null)
            {
                camObj.transform.position = Camera.main.transform.position;
                camObj.transform.rotation = Camera.main.transform.rotation;
            }
        }
        
        // 3. Crear RenderTexture para el streaming
        if (streamRenderTexture == null)
        {
            streamRenderTexture = new RenderTexture(cameraWidth, cameraHeight, cameraDepth, RenderTextureFormat.BGRA32);
            streamRenderTexture.name = "DroneStreamRT";
            streamRenderTexture.antiAliasing = 2;
            streamRenderTexture.Create();
            Debug.Log($"[DroneStreamInitializer] 📹 RenderTexture creado: {cameraWidth}x{cameraHeight}");
        }
        
        // 4. Asignar RenderTexture a la cámara
        droneCamera.targetTexture = streamRenderTexture;
        activeStreamCamera = droneCamera;
        
        // 5. Configurar el VideoStreamSender con esta cámara
        ConfigureVideoStreamSender();
        
        Debug.Log($"[DroneStreamInitializer] ✅ Cámara configurada: {droneCamera.name}");
    }
    
    /// <summary>
    /// Configura el VideoStreamSender para usar nuestra cámara
    /// </summary>
    private void ConfigureVideoStreamSender()
    {
        if (videoStreamSender == null)
        {
            Debug.LogError("[DroneStreamInitializer] ❌ VideoStreamSender es null!");
            return;
        }
        
        var senderType = videoStreamSender.GetType();
        bool configured = false;
        
        // IMPORTANTE: El VideoStreamSender de Unity Render Streaming usa:
        // - m_sourceType: enum que define si usa Camera, Texture, WebCam, etc.
        // - m_source: UnityEngine.Object que es la fuente (Camera o Texture)
        
        // 1. Primero intentar cambiar el sourceType a Camera (valor 0)
        foreach (var field in senderType.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance))
        {
            if (field.Name == "m_sourceType" || field.Name.Contains("SourceType"))
            {
                try
                {
                    // 0 = Camera en el enum VideoStreamSource
                    field.SetValue(videoStreamSender, 0);
                    Debug.Log($"[DroneStreamInitializer] ✅ sourceType establecido a Camera (0) via: {field.Name}");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[DroneStreamInitializer] ⚠️ Error en sourceType: {e.Message}");
                }
            }
        }
        
        // 2. Asignar la cámara al campo m_source
        foreach (var field in senderType.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance))
        {
            if (field.Name == "m_source" || field.Name == "m_Source")
            {
                try
                {
                    // m_source es de tipo UnityEngine.Object, puede ser Camera o Texture
                    field.SetValue(videoStreamSender, droneCamera);
                    Debug.Log($"[DroneStreamInitializer] ✅ Cámara asignada a m_source: {droneCamera.name}");
                    configured = true;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[DroneStreamInitializer] ⚠️ Error asignando cámara: {e.Message}");
                }
            }
        }
        
        // 3. También intentar asignar campos de Camera directamente
        foreach (var field in senderType.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
        {
            if (field.FieldType == typeof(Camera) && field.Name.Contains("amera"))
            {
                try
                {
                    field.SetValue(videoStreamSender, droneCamera);
                    Debug.Log($"[DroneStreamInitializer] ✅ Cámara asignada via campo: {field.Name}");
                    configured = true;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[DroneStreamInitializer] ⚠️ Error en campo {field.Name}: {e.Message}");
                }
            }
        }
        
        // Listar todos los campos para debug
        if (showDebugInfo)
        {
            Debug.Log("[DroneStreamInitializer] 🔍 Campos del VideoStreamSender:");
            foreach (var field in senderType.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
            {
                try
                {
                    var value = field.GetValue(videoStreamSender);
                    Debug.Log($"  - {field.Name} ({field.FieldType.Name}) = {value}");
                }
                catch { }
            }
        }
        
        if (!configured)
        {
            Debug.LogWarning("[DroneStreamInitializer] ⚠️ No se pudo configurar automáticamente.");
            Debug.LogWarning("  Por favor, configura manualmente en el Inspector:");
            Debug.LogWarning("  1. VideoStreamSender > Source Type = Camera");
            Debug.LogWarning("  2. VideoStreamSender > Source = [tu cámara]");
        }
    }
    
    /// <summary>
    /// Verifica que todo esté configurado correctamente
    /// </summary>
    private IEnumerator VerifyStreamingSetup()
    {
        yield return new WaitForSeconds(1f);
        
        Debug.Log("========== VERIFICACIÓN DE STREAMING ==========");
        Debug.Log($"📷 Cámara activa: {(activeStreamCamera != null ? activeStreamCamera.name : "NULL")}");
        Debug.Log($"📹 RenderTexture: {(streamRenderTexture != null ? $"{streamRenderTexture.width}x{streamRenderTexture.height}" : "NULL")}");
        Debug.Log($"📺 VideoStreamSender: {(videoStreamSender != null ? videoStreamSender.gameObject.name : "NULL")}");
        Debug.Log($"📺 VideoStreamSender.enabled: {(videoStreamSender != null ? videoStreamSender.enabled.ToString() : "N/A")}");
        
        // Verificar que la cámara está renderizando
        if (activeStreamCamera != null && streamRenderTexture != null)
        {
            Debug.Log($"📷 Cámara.targetTexture: {(activeStreamCamera.targetTexture != null ? activeStreamCamera.targetTexture.name : "NULL")}");
            Debug.Log($"📷 Cámara.enabled: {activeStreamCamera.enabled}");
        }
        
        Debug.Log("================================================");
    }
    
    /// <summary>
    /// Cambiar la cámara de streaming a la de un dron específico
    /// </summary>
    public static void SetDroneCamera(Camera newCamera)
    {
        if (newCamera == null)
        {
            Debug.LogWarning("[DroneStreamInitializer] ⚠️ SetDroneCamera: cámara es null");
            return;
        }
        
        Debug.Log($"[DroneStreamInitializer] 🔄 Cambiando cámara a: {newCamera.name}");
        
        // Mantener el RenderTexture existente
        if (streamRenderTexture != null)
        {
            // Quitar el RenderTexture de la cámara anterior
            if (activeStreamCamera != null && activeStreamCamera != newCamera)
            {
                activeStreamCamera.targetTexture = null;
            }
            
            // Asignar a la nueva cámara
            newCamera.targetTexture = streamRenderTexture;
        }
        
        activeStreamCamera = newCamera;
        
        // Actualizar el VideoStreamSender si es necesario
        if (instance != null && instance.videoStreamSender != null)
        {
            instance.ConfigureVideoStreamSender();
        }
        
        Debug.Log($"[DroneStreamInitializer] ✅ Cámara de streaming actualizada a: {newCamera.name}");
    }
    
    void OnDestroy()
    {
        if (streamRenderTexture != null)
        {
            streamRenderTexture.Release();
            Destroy(streamRenderTexture);
        }
    }
    
    void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUIStyle style = new GUIStyle();
        style.fontSize = 16;
        style.normal.textColor = Color.yellow;
        
        int y = 200;
        GUI.Label(new Rect(10, y, 500, 25), "=== Drone Stream Initializer ===", style);
        y += 25;
        
        style.normal.textColor = activeStreamCamera != null ? Color.green : Color.red;
        GUI.Label(new Rect(10, y, 500, 25), $"Stream Camera: {(activeStreamCamera != null ? activeStreamCamera.name : "NULL")}", style);
        y += 25;
        
        style.normal.textColor = streamRenderTexture != null ? Color.green : Color.red;
        GUI.Label(new Rect(10, y, 500, 25), $"RenderTexture: {(streamRenderTexture != null ? "OK" : "NULL")}", style);
        y += 25;
        
        style.normal.textColor = Color.cyan;
        if (activeStreamCamera != null)
        {
            GUI.Label(new Rect(10, y, 500, 25), $"Camera Pos: {activeStreamCamera.transform.position}", style);
        }
    }
}
