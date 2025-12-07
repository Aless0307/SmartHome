using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Componente UI para una tarjeta de dispositivo
/// </summary>
public class DeviceCardUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text nameText;
    public TMP_Text typeText;
    public TMP_Text roomText;
    public TMP_Text statusText;
    public TMP_Text valueText;
    public Image iconImage;
    public Image backgroundImage;
    public Button toggleButton;
    public Slider valueSlider;
    
    [Header("Icons")]
    public Sprite lightIcon;
    public Sprite thermostatIcon;
    public Sprite doorIcon;
    public Sprite cameraIcon;
    public Sprite sensorIcon;
    public Sprite defaultIcon;
    
    [Header("Colors")]
    public Color onColor = new Color(0.3f, 0.8f, 0.3f);
    public Color offColor = new Color(0.5f, 0.5f, 0.5f);
    public Color lightOnColor = new Color(1f, 0.9f, 0.3f);
    
    // Datos del dispositivo
    private DeviceData deviceData;
    
    void Start()
    {
        // Configurar listeners
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(OnToggleClicked);
        }
        
        if (valueSlider != null)
        {
            valueSlider.onValueChanged.AddListener(OnValueChanged);
        }
    }
    
    /// <summary>
    /// Establecer datos del dispositivo
    /// </summary>
    public void SetDevice(DeviceData device)
    {
        deviceData = device;
        UpdateUI();
    }
    
    /// <summary>
    /// Actualizar datos del dispositivo
    /// </summary>
    public void UpdateData(DeviceData device)
    {
        deviceData = device;
        UpdateUI();
    }
    
    /// <summary>
    /// Actualizar la interfaz
    /// </summary>
    private void UpdateUI()
    {
        if (deviceData == null) return;
        
        // Nombre
        if (nameText != null)
        {
            nameText.text = deviceData.name;
        }
        
        // Tipo
        if (typeText != null)
        {
            typeText.text = GetTypeDisplay(deviceData.type);
        }
        
        // Habitación
        if (roomText != null)
        {
            roomText.text = deviceData.room;
        }
        
        // Estado
        if (statusText != null)
        {
            statusText.text = deviceData.status ? "ON" : "OFF";
            statusText.color = deviceData.status ? onColor : offColor;
        }
        
        // Valor
        if (valueText != null)
        {
            if (deviceData.type == "thermostat")
            {
                valueText.text = $"{deviceData.value}°C";
            }
            else if (deviceData.type == "light")
            {
                valueText.text = $"{deviceData.value}%";
            }
            else
            {
                valueText.text = deviceData.value.ToString();
            }
        }
        
        // Slider
        if (valueSlider != null)
        {
            valueSlider.SetValueWithoutNotify(deviceData.value);
            
            // Configurar rango según tipo
            if (deviceData.type == "thermostat")
            {
                valueSlider.minValue = 16;
                valueSlider.maxValue = 30;
            }
            else
            {
                valueSlider.minValue = 0;
                valueSlider.maxValue = 100;
            }
            
            valueSlider.gameObject.SetActive(
                deviceData.type == "light" || 
                deviceData.type == "thermostat"
            );
        }
        
        // Ícono
        if (iconImage != null)
        {
            iconImage.sprite = GetIcon(deviceData.type);
            iconImage.color = deviceData.status ? GetIconColor() : offColor;
        }
        
        // Fondo
        if (backgroundImage != null)
        {
            Color bgColor = deviceData.status ? onColor : offColor;
            bgColor.a = 0.2f;
            backgroundImage.color = bgColor;
        }
        
        // Botón
        if (toggleButton != null)
        {
            var btnText = toggleButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
            {
                btnText.text = deviceData.status ? "Apagar" : "Encender";
            }
            
            var btnImage = toggleButton.GetComponent<Image>();
            if (btnImage != null)
            {
                btnImage.color = deviceData.status ? new Color(0.8f, 0.3f, 0.3f) : onColor;
            }
        }
    }
    
    /// <summary>
    /// Obtener ícono según tipo
    /// </summary>
    private Sprite GetIcon(string type)
    {
        switch (type?.ToLower())
        {
            case "light": return lightIcon ?? defaultIcon;
            case "thermostat": return thermostatIcon ?? defaultIcon;
            case "door": return doorIcon ?? defaultIcon;
            case "camera": return cameraIcon ?? defaultIcon;
            case "sensor": return sensorIcon ?? defaultIcon;
            default: return defaultIcon;
        }
    }
    
    /// <summary>
    /// Obtener color del ícono cuando está encendido
    /// </summary>
    private Color GetIconColor()
    {
        if (deviceData == null) return onColor;
        
        switch (deviceData.type?.ToLower())
        {
            case "light":
                // Usar color del dispositivo si existe
                if (!string.IsNullOrEmpty(deviceData.color))
                {
                    Color c;
                    if (ColorUtility.TryParseHtmlString(deviceData.color, out c))
                    {
                        return c;
                    }
                }
                return lightOnColor;
            default:
                return onColor;
        }
    }
    
    /// <summary>
    /// Obtener texto de tipo
    /// </summary>
    private string GetTypeDisplay(string type)
    {
        switch (type?.ToLower())
        {
            case "light": return "💡 Luz";
            case "thermostat": return "🌡️ Termostato";
            case "door": return "🚪 Puerta";
            case "camera": return "📷 Cámara";
            case "sensor": return "📡 Sensor";
            default: return "📱 Dispositivo";
        }
    }
    
    // ═══════════════════════════════════════════════════════════
    // EVENTOS
    // ═══════════════════════════════════════════════════════════
    
    private void OnToggleClicked()
    {
        if (deviceData == null) return;
        DeviceManager.Instance?.Toggle(deviceData.id);
    }
    
    private void OnValueChanged(float value)
    {
        if (deviceData == null) return;
        DeviceManager.Instance?.SetValue(deviceData.id, (int)value);
    }
}
