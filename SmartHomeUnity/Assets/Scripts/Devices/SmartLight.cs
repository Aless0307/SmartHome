using UnityEngine;

/// <summary>
/// Control de luz simple para Smart Home
/// Agregar a cualquier objeto con componente Light o con luces hijas
/// </summary>
public class SmartLight : MonoBehaviour
{
    [Header("Configuración")]
    public KeyCode toggleKey = KeyCode.L;
    public bool startOn = false;
    
    [Header("Luces a controlar")]
    [Tooltip("Si está vacío, busca todas las luces en este objeto y sus hijos")]
    public Light[] lights;
    
    [Header("Colores")]
    public Color onColor = Color.white;
    public Color offColor = Color.black;
    
    [Header("Intensidad")]
    public float onIntensity = 1f;
    public float offIntensity = 0f;
    
    [Header("Estado")]
    public bool isOn = false;
    
    // Materiales emisivos (opcional)
    private Renderer[] emissiveRenderers;
    private Material[] originalMaterials;

    void Start()
    {
        // Buscar luces si no están asignadas
        if (lights == null || lights.Length == 0)
        {
            lights = GetComponentsInChildren<Light>();
        }
        
        // Buscar renderers para emisión
        emissiveRenderers = GetComponentsInChildren<Renderer>();
        
        // Estado inicial
        isOn = startOn;
        ApplyState();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            Toggle();
        }
    }
    
    // ═══════════════════════════════════════════════════════════
    // MÉTODOS PARA SMART HOME SERVER
    // ═══════════════════════════════════════════════════════════
    
    public void Toggle()
    {
        isOn = !isOn;
        ApplyState();
        Debug.Log($"💡 {gameObject.name}: {(isOn ? "ON" : "OFF")}");
    }
    
    public void TurnOn()
    {
        Debug.Log($"💡💡💡 TurnOn() llamado en {gameObject.name}, isOn antes = {isOn}");
        if (!isOn)
        {
            isOn = true;
            ApplyState();
            Debug.Log($"💡 {gameObject.name}: ON");
        }
    }
    
    public void TurnOff()
    {
        Debug.Log($"💡💡💡 TurnOff() llamado en {gameObject.name}, isOn antes = {isOn}");
        if (isOn)
        {
            isOn = false;
            ApplyState();
            Debug.Log($"💡 {gameObject.name}: OFF");
        }
    }
    
    public void OnSmartHomeToggle()
    {
        Toggle();
    }
    
    public void SetState(bool on)
    {
        isOn = on;
        ApplyState();
    }
    
    public void SetColor(Color color)
    {
        onColor = color;
        if (isOn) ApplyState();
        Debug.Log($"💡 {gameObject.name}: Color cambiado a {color}");
    }
    
    public void SetColorHex(string hexColor)
    {
        if (ColorUtility.TryParseHtmlString(hexColor, out Color color))
        {
            SetColor(color);
        }
        else if (ColorUtility.TryParseHtmlString("#" + hexColor, out color))
        {
            SetColor(color);
        }
    }
    
    public void SetIntensity(float intensity)
    {
        onIntensity = intensity;
        // Aplicar inmediatamente a todas las luces (sin importar isOn)
        // Mantener luz encendida incluso con intensidad 0 (solo muy tenue)
        foreach (var light in lights)
        {
            if (light != null)
            {
                light.intensity = intensity;
                // Si la luz está encendida, mantenerla habilitada incluso con intensidad 0
                if (isOn)
                {
                    light.enabled = true;
                }
            }
        }
        Debug.Log($"💡 {gameObject.name}: Intensidad = {intensity}");
    }
    
    public void SetValue(int value)
    {
        // El valor viene directamente como intensidad
        SetIntensity((float)value);
    }
    
    /// <summary>
    /// Recibir comando del servidor con valor
    /// </summary>
    public void OnSmartHomeValue(string value)
    {
        if (int.TryParse(value, out int intValue))
        {
            SetValue(intValue);
        }
    }
    
    /// <summary>
    /// Recibir comando del servidor con color
    /// </summary>
    public void OnSmartHomeColor(string hexColor)
    {
        SetColorHex(hexColor);
    }
    
    /// <summary>
    /// Aplicar estado actual a todas las luces
    /// </summary>
    private void ApplyState()
    {
        foreach (var light in lights)
        {
            if (light != null)
            {
                light.intensity = isOn ? onIntensity : offIntensity;
                light.color = isOn ? onColor : offColor;
                light.enabled = isOn;
            }
        }
        
        // Actualizar materiales emisivos (usar valor normalizado para emisión)
        float emissionMultiplier = Mathf.Clamp01(onIntensity / 3000f); // Normalizar a 0-1
        foreach (var renderer in emissiveRenderers)
        {
            if (renderer != null)
            {
                foreach (var mat in renderer.materials)
                {
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        Color emission = isOn ? onColor * emissionMultiplier : Color.black;
                        mat.SetColor("_EmissionColor", emission);
                    }
                }
            }
        }
    }
    
    public bool IsOn => isOn;
}
