using UnityEngine;

/// <summary>
/// Sistema de Cámara de Seguridad para Smart Home
/// Controla una cámara de vigilancia con luz infrarroja
/// </summary>
public class SecurityCamera : MonoBehaviour
{
    [Header("Identificación")]
    [Tooltip("ID único de la cámara (debe coincidir con MongoDB)")]
    public string cameraId;
    
    [Tooltip("Nombre para mostrar")]
    public string cameraName = "Cámara 1";
    
    [Tooltip("Ubicación de la cámara")]
    public string location = "entrada";

    [Header("Componentes")]
    [Tooltip("La cámara de Unity que renderiza la vista")]
    public Camera securityCam;
    
    [Tooltip("Luz infrarroja/LED de la cámara")]
    public Light irLight;
    
    [Tooltip("Renderer del LED indicador (opcional)")]
    public Renderer ledIndicator;

    [Header("Configuración de Cámara")]
    [Tooltip("Resolución del RenderTexture")]
    public int renderWidth = 1280;
    public int renderHeight = 720;
    
    [Tooltip("Profundidad de la cámara (menor = renderiza primero)")]
    public int cameraDepth = -1;

    [Header("Configuración de Luz")]
    [Tooltip("Intensidad de la luz IR cuando está encendida")]
    public float lightIntensity = 2f;
    
    [Tooltip("Color de la luz (rojo para IR típico)")]
    public Color lightColor = new Color(0.8f, 0.2f, 0.2f);
    
    [Tooltip("Rango de la luz")]
    public float lightRange = 10f;

    [Header("Estado")]
    public bool isCameraOn = false;  // Iniciar APAGADA, el servidor mandará el estado real
    public bool isLightOn = false;
    public bool isRecording = false;

    // RenderTexture para capturar la vista de la cámara
    private RenderTexture renderTexture;
    
    // Colores del LED indicador
    private Color ledOnColor = Color.green;
    private Color ledOffColor = Color.red;
    private Color ledRecordingColor = new Color(1f, 0.5f, 0f); // Naranja
    
    // Intensidad original de la luz (guardada del Inspector)
    private float originalLightIntensity;

    void Awake()
    {
        // Generar ID si no tiene
        if (string.IsNullOrEmpty(cameraId))
        {
            cameraId = System.Guid.NewGuid().ToString().Substring(0, 8);
        }
    }

    void Start()
    {
        // FORZAR apagada al inicio - el servidor mandará el estado real
        isCameraOn = false;
        
        SetupCamera();
        SetupLight();
        UpdateVisuals();
        
        Debug.Log($"📹 {cameraName}: Inicializada en {location} (esperando estado del servidor)");
    }

    /// <summary>
    /// Configurar la cámara de seguridad
    /// </summary>
    private void SetupCamera()
    {
        if (securityCam == null)
        {
            securityCam = GetComponentInChildren<Camera>();
        }

        if (securityCam != null)
        {
            // Crear RenderTexture para esta cámara
            renderTexture = new RenderTexture(renderWidth, renderHeight, 24);
            renderTexture.name = $"SecurityCam_{cameraName}";
            securityCam.targetTexture = renderTexture;
            securityCam.depth = cameraDepth;
            securityCam.enabled = isCameraOn;
            
            Debug.Log($"📹 {cameraName}: RenderTexture creado ({renderWidth}x{renderHeight})");
        }
        else
        {
            Debug.LogWarning($"📹 {cameraName}: No se encontró componente Camera");
        }
    }

    /// <summary>
    /// Configurar la luz IR
    /// </summary>
    private void SetupLight()
    {
        if (irLight == null)
        {
            irLight = GetComponentInChildren<Light>();
        }

        if (irLight != null)
        {
            // Guardar la intensidad original configurada en el Inspector
            originalLightIntensity = irLight.intensity;
            if (originalLightIntensity <= 0) originalLightIntensity = lightIntensity;
            
            irLight.color = lightColor;
            // Si la luz empieza apagada, ponerla a 0
            if (!isLightOn) irLight.intensity = 0f;
            irLight.range = lightRange;
            irLight.enabled = true; // Siempre habilitada, controlamos con intensity
        }
    }

    // ═══════════════════════════════════════════════════════════
    // MÉTODOS DE CONTROL
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Encender cámara
    /// </summary>
    public void TurnOn()
    {
        isCameraOn = true;
        if (securityCam != null)
        {
            securityCam.enabled = true;
        }
        UpdateVisuals();
        Debug.Log($"📹 {cameraName}: Cámara ENCENDIDA");
    }

    /// <summary>
    /// Apagar cámara
    /// </summary>
    public void TurnOff()
    {
        isCameraOn = false;
        if (securityCam != null)
        {
            securityCam.enabled = false;
        }
        UpdateVisuals();
        Debug.Log($"📹 {cameraName}: Cámara APAGADA");
    }

    /// <summary>
    /// Toggle cámara
    /// </summary>
    public void ToggleCamera()
    {
        if (isCameraOn)
            TurnOff();
        else
            TurnOn();
    }

    /// <summary>
    /// Encender luz IR
    /// </summary>
    public void LightOn()
    {
        isLightOn = true;
        if (irLight != null)
        {
            irLight.intensity = originalLightIntensity;
            Debug.Log($"📹 {cameraName}: Luz IR ENCENDIDA (intensidad: {originalLightIntensity})");
        }
        UpdateVisuals();
    }

    /// <summary>
    /// Apagar luz IR
    /// </summary>
    public void LightOff()
    {
        isLightOn = false;
        if (irLight != null)
        {
            irLight.intensity = 0f;
            Debug.Log($"📹 {cameraName}: Luz IR APAGADA");
        }
        UpdateVisuals();
    }

    /// <summary>
    /// Toggle luz IR
    /// </summary>
    public void ToggleLight()
    {
        if (isLightOn)
            LightOff();
        else
            LightOn();
    }

    /// <summary>
    /// Iniciar grabación (visual)
    /// </summary>
    public void StartRecording()
    {
        isRecording = true;
        UpdateVisuals();
        Debug.Log($"📹 {cameraName}: GRABANDO");
    }

    /// <summary>
    /// Detener grabación
    /// </summary>
    public void StopRecording()
    {
        isRecording = false;
        UpdateVisuals();
        Debug.Log($"📹 {cameraName}: Grabación DETENIDA");
    }

    // ═══════════════════════════════════════════════════════════
    // MÉTODOS PARA SMART HOME SERVER (DeviceBridge)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Llamado por DeviceBridge cuando status=true
    /// </summary>
    public void OnSmartHomeOn()
    {
        TurnOn();
    }

    /// <summary>
    /// Llamado por DeviceBridge cuando status=false
    /// </summary>
    public void OnSmartHomeOff()
    {
        TurnOff();
    }

    public void OnSmartHomeToggle()
    {
        ToggleCamera();
    }

    /// <summary>
    /// Recibir comandos especiales del servidor
    /// Formato: "CMD:comando" en el campo color
    /// </summary>
    public void OnSmartHomeCommand(string command)
    {
        Debug.Log($"📹 {cameraName}: Comando recibido: {command}");
        
        switch (command.ToUpper())
        {
            case "LIGHT_ON":
                LightOn();
                break;
            case "LIGHT_OFF":
                LightOff();
                break;
            case "LIGHT_TOGGLE":
                ToggleLight();
                break;
            case "RECORD_START":
                StartRecording();
                break;
            case "RECORD_STOP":
                StopRecording();
                break;
        }
    }

    /// <summary>
    /// Recibir valor del servidor (0 = luz apagada, 100 = luz encendida)
    /// </summary>
    public void OnSmartHomeValue(string value)
    {
        if (int.TryParse(value, out int brightness))
        {
            if (irLight != null)
            {
                if (brightness > 0)
                {
                    isLightOn = true;
                    irLight.intensity = originalLightIntensity;
                    Debug.Log($"📹 {cameraName}: Luz IR ENCENDIDA (valor={brightness}, intensidad={originalLightIntensity})");
                }
                else
                {
                    isLightOn = false;
                    irLight.intensity = 0f;
                    Debug.Log($"📹 {cameraName}: Luz IR APAGADA (valor={brightness})");
                }
                UpdateVisuals();
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // VISUALES
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Actualizar indicadores visuales
    /// </summary>
    private void UpdateVisuals()
    {
        if (ledIndicator != null)
        {
            Color ledColor;
            
            if (!isCameraOn)
                ledColor = ledOffColor;
            else if (isRecording)
                ledColor = ledRecordingColor;
            else
                ledColor = ledOnColor;

            // Aplicar color emisivo
            foreach (var mat in ledIndicator.materials)
            {
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", ledColor * 2f);
                    mat.EnableKeyword("_EMISSION");
                }
                if (mat.HasProperty("_Color"))
                {
                    mat.SetColor("_Color", ledColor);
                }
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // ACCESO A RENDER TEXTURE
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Obtener la textura renderizada de esta cámara
    /// </summary>
    public RenderTexture GetRenderTexture()
    {
        return renderTexture;
    }

    /// <summary>
    /// Obtener la cámara
    /// </summary>
    public Camera GetCamera()
    {
        return securityCam;
    }

    /// <summary>
    /// Obtener estado como JSON para el servidor
    /// </summary>
    public string GetStatusJson()
    {
        return $"{{\"id\":\"{cameraId}\",\"name\":\"{cameraName}\",\"location\":\"{location}\"," +
               $"\"cameraOn\":{isCameraOn.ToString().ToLower()}," +
               $"\"lightOn\":{isLightOn.ToString().ToLower()}," +
               $"\"recording\":{isRecording.ToString().ToLower()}}}";
    }

    void OnDestroy()
    {
        // Liberar RenderTexture
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // DEBUG
    // ═══════════════════════════════════════════════════════════

    void OnDrawGizmosSelected()
    {
        // Dibujar el cono de visión de la cámara
        if (securityCam != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.matrix = securityCam.transform.localToWorldMatrix;
            Gizmos.DrawFrustum(Vector3.zero, securityCam.fieldOfView, 
                              securityCam.farClipPlane, securityCam.nearClipPlane, 
                              securityCam.aspect);
        }

        // Dibujar rango de luz
        if (irLight != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(irLight.transform.position, lightRange);
        }
    }
}
