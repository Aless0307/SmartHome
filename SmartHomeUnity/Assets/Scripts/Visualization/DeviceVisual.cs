using UnityEngine;

/// <summary>
/// Componente visual de un dispositivo en 3D
/// Se adjunta a objetos que representan dispositivos
/// </summary>
public class DeviceVisual : MonoBehaviour
{
    [Header("Estado")]
    public DeviceData deviceData;
    
    [Header("Componentes")]
    public MeshRenderer meshRenderer;
    public Light deviceLight;
    
    [Header("Colores")]
    public Color onColor = new Color(0.4f, 0.8f, 0.4f);
    public Color offColor = Color.gray;
    
    // Material original
    private Material material;
    private Color originalEmission;
    
    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        deviceLight = GetComponent<Light>();
        
        if (meshRenderer != null)
        {
            material = meshRenderer.material;
        }
    }
    
    /// <summary>
    /// Establecer datos del dispositivo
    /// </summary>
    public void SetDevice(DeviceData device)
    {
        deviceData = device;
        gameObject.name = $"{device.type}_{device.name}";
        UpdateVisual();
    }
    
    /// <summary>
    /// Actualizar datos del dispositivo
    /// </summary>
    public void UpdateDevice(DeviceData device)
    {
        deviceData = device;
        UpdateVisual();
    }
    
    /// <summary>
    /// Actualizar visualización según estado
    /// </summary>
    private void UpdateVisual()
    {
        if (deviceData == null) return;
        
        switch (deviceData.type?.ToLower())
        {
            case "light":
                UpdateLightVisual();
                break;
            case "thermostat":
                UpdateThermostatVisual();
                break;
            case "door":
                UpdateDoorVisual();
                break;
            case "camera":
                UpdateCameraVisual();
                break;
            case "sensor":
                UpdateSensorVisual();
                break;
            default:
                UpdateDefaultVisual();
                break;
        }
    }
    
    private void UpdateLightVisual()
    {
        Color color = Color.yellow;
        
        // Usar color del dispositivo si existe
        if (!string.IsNullOrEmpty(deviceData.color))
        {
            ColorUtility.TryParseHtmlString(deviceData.color, out color);
        }
        
        if (material != null)
        {
            material.color = deviceData.status ? color : offColor;
            
            if (material.HasProperty("_EmissionColor"))
            {
                Color emission = deviceData.status ? color * (deviceData.value / 100f + 0.5f) : Color.black;
                material.SetColor("_EmissionColor", emission);
            }
        }
        
        if (deviceLight != null)
        {
            deviceLight.intensity = deviceData.status ? (deviceData.value / 100f) * 2f : 0;
            deviceLight.color = color;
        }
        
        // Animar escala si está encendido
        float targetScale = deviceData.status ? 0.35f : 0.3f;
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * targetScale, Time.deltaTime * 5);
    }
    
    private void UpdateThermostatVisual()
    {
        // Color basado en temperatura
        float temp = deviceData.value;
        Color color = Color.Lerp(Color.blue, Color.red, (temp - 16) / 14f);
        
        if (!deviceData.status)
        {
            color = offColor;
        }
        
        if (material != null)
        {
            material.color = color;
        }
    }
    
    private void UpdateDoorVisual()
    {
        // Rotar puerta si está abierta/cerrada
        float targetRotation = deviceData.status ? 90 : 0;
        Quaternion targetRot = Quaternion.Euler(0, targetRotation, 0);
        transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRot, Time.deltaTime * 3);
        
        if (material != null)
        {
            material.color = deviceData.status ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.6f, 0.3f, 0.2f);
        }
    }
    
    private void UpdateCameraVisual()
    {
        if (material != null)
        {
            material.color = deviceData.status ? Color.green : offColor;
            
            if (material.HasProperty("_EmissionColor"))
            {
                Color emission = deviceData.status ? Color.green * 0.5f : Color.black;
                material.SetColor("_EmissionColor", emission);
            }
        }
    }
    
    private void UpdateSensorVisual()
    {
        if (material != null)
        {
            material.color = deviceData.status ? Color.cyan : offColor;
            
            if (material.HasProperty("_EmissionColor"))
            {
                Color emission = deviceData.status ? Color.cyan * 0.3f : Color.black;
                material.SetColor("_EmissionColor", emission);
            }
        }
        
        // Pulsar si está activo
        if (deviceData.status)
        {
            float pulse = 0.2f + Mathf.Sin(Time.time * 3) * 0.02f;
            transform.localScale = Vector3.one * pulse;
        }
    }
    
    private void UpdateDefaultVisual()
    {
        if (material != null)
        {
            material.color = deviceData.status ? onColor : offColor;
        }
    }
    
    /// <summary>
    /// Al hacer clic en el dispositivo
    /// </summary>
    void OnMouseDown()
    {
        if (deviceData == null) return;
        
        // Toggle del dispositivo
        DeviceManager.Instance?.Toggle(deviceData.id);
        
        Debug.Log($"🖱️ Click en {deviceData.name}");
    }
    
    /// <summary>
    /// Al pasar el mouse por encima
    /// </summary>
    void OnMouseEnter()
    {
        // Resaltar
        if (material != null)
        {
            material.SetColor("_EmissionColor", material.color * 0.3f);
        }
    }
    
    void OnMouseExit()
    {
        // Quitar resaltado
        UpdateVisual();
    }
}
