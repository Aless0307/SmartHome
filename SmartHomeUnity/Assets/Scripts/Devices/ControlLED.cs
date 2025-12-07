using UnityEngine;

/// <summary>
/// ControlLED modificado para funcionar con Smart Home Server
/// Funciona con tecla Y con comandos del servidor
/// </summary>
public class ControlLED : MonoBehaviour
{
    public Material materialRojo;
    public Material materialVerde;
    public Light luzLED;
    public Transform rejillas;
    public float anguloAbierto = 90f;
    public float velocidadApertura = 2f;
    public KeyCode key = KeyCode.E;
    
    private Renderer ledRenderer;
    private bool encendido = false;
    private float anguloActual = 0f;

    void Start()
    {
        ledRenderer = GetComponent<Renderer>();
        if (ledRenderer != null && materialRojo != null)
        {
            ledRenderer.material = materialRojo;
        }
        
        if (luzLED != null)
        {
            luzLED.color = Color.red;
            luzLED.intensity = 5f;
        }
    }

    void Update()
    {
        // Control por tecla (original)
        if (Input.GetKeyDown(key))
        {
            Toggle();
        }

        // Animar rejillas
        if (rejillas != null)
        {
            float anguloObjetivo = encendido ? anguloAbierto : 0f;
            anguloActual = Mathf.Lerp(anguloActual, anguloObjetivo, Time.deltaTime * velocidadApertura);
            rejillas.localRotation = Quaternion.Euler(anguloActual, 0f, 0f);
        }
    }
    
    // ═══════════════════════════════════════════════════════════
    // MÉTODOS PARA SMART HOME SERVER
    // ═══════════════════════════════════════════════════════════
    
    public void Toggle()
    {
        if (encendido)
            TurnOff();
        else
            TurnOn();
    }
    
    public void TurnOn()
    {
        encendido = true;
        
        if (ledRenderer != null && materialVerde != null)
        {
            ledRenderer.material = materialVerde;
        }
        
        if (luzLED != null)
        {
            luzLED.color = Color.green;
            luzLED.intensity = 5f;
        }
        
        Debug.Log($"💡 {gameObject.name}: ENCENDIDO");
    }
    
    public void TurnOff()
    {
        encendido = false;
        
        if (ledRenderer != null && materialRojo != null)
        {
            ledRenderer.material = materialRojo;
        }
        
        if (luzLED != null)
        {
            luzLED.color = Color.red;
            luzLED.intensity = 5f;
        }
        
        Debug.Log($"💡 {gameObject.name}: APAGADO");
    }
    
    public void OnSmartHomeToggle()
    {
        Toggle();
    }
    
    public void SetState(bool on)
    {
        if (on)
            TurnOn();
        else
            TurnOff();
    }
    
    public bool IsOn => encendido;
}
