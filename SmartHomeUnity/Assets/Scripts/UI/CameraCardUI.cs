using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Tarjeta UI individual para una cámara de seguridad
/// Muestra el feed en vivo y controles
/// </summary>
public class CameraCardUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI Elements")]
    public RawImage cameraFeed;
    public Text cameraNameText;
    public Text locationText;
    public Image statusIndicator;
    public Button lightToggleButton;
    public Button cameraToggleButton;
    public Button fullscreenButton;
    public Text lightButtonText;
    public Text cameraButtonText;

    [Header("Colores de Estado")]
    public Color onlineColor = Color.green;
    public Color offlineColor = Color.red;
    public Color recordingColor = new Color(1f, 0.5f, 0f);

    // Referencias
    private SecurityCamera securityCamera;
    private SecurityCameraUI parentUI;
    
    // Actualización periódica
    private float updateInterval = 0.1f; // 10 FPS para el feed
    private float lastUpdate;

    /// <summary>
    /// Configurar la tarjeta con una cámara
    /// </summary>
    public void Setup(SecurityCamera cam, SecurityCameraUI parent)
    {
        securityCamera = cam;
        parentUI = parent;

        if (cam == null) return;

        // Configurar textos
        if (cameraNameText != null)
            cameraNameText.text = cam.cameraName;
        
        if (locationText != null)
            locationText.text = cam.location;

        // Configurar feed
        UpdateFeed();

        // Configurar botones
        SetupButtons();

        // Actualizar estado visual
        UpdateStatus();
    }

    /// <summary>
    /// Configurar listeners de botones
    /// </summary>
    private void SetupButtons()
    {
        if (lightToggleButton != null)
        {
            lightToggleButton.onClick.RemoveAllListeners();
            lightToggleButton.onClick.AddListener(OnLightToggleClick);
        }

        if (cameraToggleButton != null)
        {
            cameraToggleButton.onClick.RemoveAllListeners();
            cameraToggleButton.onClick.AddListener(OnCameraToggleClick);
        }

        if (fullscreenButton != null)
        {
            fullscreenButton.onClick.RemoveAllListeners();
            fullscreenButton.onClick.AddListener(OnFullscreenClick);
        }
    }

    /// <summary>
    /// Actualizar el feed de la cámara
    /// </summary>
    public void UpdateFeed()
    {
        if (cameraFeed != null && securityCamera != null)
        {
            RenderTexture rt = securityCamera.GetRenderTexture();
            if (rt != null)
            {
                cameraFeed.texture = rt;
                
                // Mostrar pantalla negra si la cámara está apagada
                cameraFeed.color = securityCamera.isCameraOn ? Color.white : Color.black;
            }
        }
    }

    /// <summary>
    /// Actualizar indicadores de estado
    /// </summary>
    public void UpdateStatus()
    {
        if (securityCamera == null) return;

        // Indicador de estado
        if (statusIndicator != null)
        {
            if (securityCamera.isRecording)
                statusIndicator.color = recordingColor;
            else if (securityCamera.isCameraOn)
                statusIndicator.color = onlineColor;
            else
                statusIndicator.color = offlineColor;
        }

        // Texto botón de luz
        if (lightButtonText != null)
        {
            lightButtonText.text = securityCamera.isLightOn ? "💡 Apagar" : "💡 Encender";
        }

        // Texto botón de cámara
        if (cameraButtonText != null)
        {
            cameraButtonText.text = securityCamera.isCameraOn ? "📹 Apagar" : "📹 Encender";
        }

        // Color del feed si está apagada
        if (cameraFeed != null)
        {
            cameraFeed.color = securityCamera.isCameraOn ? Color.white : new Color(0.2f, 0.2f, 0.2f);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // EVENTOS DE BOTONES
    // ═══════════════════════════════════════════════════════════

    private void OnLightToggleClick()
    {
        if (securityCamera != null)
        {
            securityCamera.ToggleLight();
            UpdateStatus();
            Debug.Log($"📹 UI: Toggle luz de {securityCamera.cameraName}");
        }
    }

    private void OnCameraToggleClick()
    {
        if (securityCamera != null)
        {
            securityCamera.ToggleCamera();
            UpdateStatus();
            UpdateFeed();
            Debug.Log($"📹 UI: Toggle cámara {securityCamera.cameraName}");
        }
    }

    private void OnFullscreenClick()
    {
        if (parentUI != null && securityCamera != null)
        {
            parentUI.ShowFullscreen(securityCamera);
        }
    }

    /// <summary>
    /// Click en la tarjeta para fullscreen
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // Doble click para fullscreen
        if (eventData.clickCount == 2)
        {
            OnFullscreenClick();
        }
    }

    void Update()
    {
        // Actualización periódica del estado
        if (Time.time - lastUpdate > updateInterval)
        {
            lastUpdate = Time.time;
            UpdateStatus();
        }
    }

    /// <summary>
    /// Obtener la cámara asociada
    /// </summary>
    public SecurityCamera GetCamera()
    {
        return securityCamera;
    }
}
