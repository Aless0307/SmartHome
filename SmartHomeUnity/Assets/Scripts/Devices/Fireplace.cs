using UnityEngine;

/// <summary>
/// Controlador para la Chimenea con animación de fuego
/// Tipo: fireplace
/// Controles: ON/OFF (encender/apagar animación)
/// </summary>
public class Fireplace : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("ID del dispositivo en la base de datos")]
    public string deviceId;
    
    [Header("Componentes")]
    [Tooltip("Objeto con la animación del fuego (el padre 'fuego')")]
    public GameObject fireObject;
    
    [Tooltip("Componente Animation del fuego")]
    public Animation fireAnimation;
    
    [Tooltip("Nombre del clip de animación")]
    public string animationClipName = "Take 001";
    
    [Header("Efectos Opcionales")]
    [Tooltip("Luz del fuego (Point Light)")]
    public Light fireLight;
    
    [Tooltip("Intensidad de la luz cuando está encendida")]
    public float lightIntensity = 2f;
    
    [Tooltip("Sistema de partículas de chispas (opcional)")]
    public ParticleSystem sparksParticles;
    
    [Header("Audio (Opcional)")]
    [Tooltip("Sonido del fuego crepitando")]
    public AudioSource fireSound;
    
    // Estado actual
    private bool isOn = false;
    
    void Start()
    {
        // Si fireObject no está asignado, usar este mismo objeto
        if (fireObject == null)
        {
            fireObject = gameObject;
        }
        
        // Auto-obtener componentes si no están asignados
        if (fireAnimation == null)
        {
            fireAnimation = fireObject.GetComponent<Animation>();
        }
        
        if (fireLight == null)
        {
            fireLight = GetComponentInChildren<Light>();
        }
        
        // Debug para verificar referencias
        Debug.Log($"[Fireplace] Start - fireObject: {fireObject.name}, fireAnimation: {(fireAnimation != null ? "OK" : "NULL")}");
        
        // Estado inicial: apagado
        isOn = false;
        
        // NO desactivar el objeto si el script está en él
        // En su lugar, solo ocultar los renderers hijos
        SetRenderersVisible(false);
        
        if (fireAnimation != null)
        {
            fireAnimation.Stop();
        }
        
        Debug.Log("[Fireplace] Inicializado - Fuego apagado");
    }
    
    /// <summary>
    /// Muestra u oculta los renderers del fuego sin desactivar el objeto
    /// </summary>
    private void SetRenderersVisible(bool visible)
    {
        // Obtener todos los renderers en este objeto y sus hijos
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            r.enabled = visible;
        }
    }
    
    /// <summary>
    /// Procesa comandos del servidor
    /// </summary>
    public void ProcessCommand(string command, string value)
    {
        Debug.Log($"[Fireplace] Comando: {command}, Valor: {value}");
        
        switch (command.ToUpper())
        {
            case "ON":
                TurnOn();
                break;
                
            case "OFF":
                TurnOff();
                break;
                
            case "TOGGLE":
                Toggle();
                break;
                
            case "SET_STATUS":
                bool status = value == "true" || value == "1";
                SetFireState(status);
                break;
        }
    }
    
    /// <summary>
    /// Enciende la chimenea
    /// </summary>
    public void TurnOn()
    {
        Debug.Log("[Fireplace] TurnOn() llamado!");
        SetFireState(true);
    }
    
    /// <summary>
    /// Apaga la chimenea
    /// </summary>
    public void TurnOff()
    {
        Debug.Log("[Fireplace] TurnOff() llamado!");
        SetFireState(false);
    }
    
    /// <summary>
    /// Alterna el estado
    /// </summary>
    public void Toggle()
    {
        SetFireState(!isOn);
    }
    
    /// <summary>
    /// Establece el estado del fuego
    /// </summary>
    private void SetFireState(bool on)
    {
        isOn = on;
        Debug.Log($"[Fireplace] SetFireState({on})");
        
        // Mostrar/ocultar renderers
        SetRenderersVisible(on);
        
        // Controlar animación
        if (fireAnimation != null)
        {
            if (on)
            {
                // Reproducir animación en loop
                fireAnimation.wrapMode = WrapMode.Loop;
                fireAnimation.Play(animationClipName);
                Debug.Log($"[Fireplace] Reproduciendo animación: {animationClipName}");
            }
            else
            {
                fireAnimation.Stop();
                Debug.Log("[Fireplace] Animación detenida");
            }
        }
        else
        {
            Debug.LogWarning("[Fireplace] fireAnimation es NULL!");
        }
        
        // Controlar luz
        if (fireLight != null)
        {
            fireLight.enabled = on;
            if (on)
            {
                fireLight.intensity = lightIntensity;
                StartCoroutine(FlickerLight());
            }
            else
            {
                StopAllCoroutines();
            }
        }
        
        // Controlar partículas
        if (sparksParticles != null)
        {
            if (on)
                sparksParticles.Play();
            else
                sparksParticles.Stop();
        }
        
        // Controlar sonido
        if (fireSound != null)
        {
            if (on)
                fireSound.Play();
            else
                fireSound.Stop();
        }
        
        Debug.Log($"[Fireplace] Estado: {(on ? "ENCENDIDA 🔥" : "APAGADA")}");
    }
    
    /// <summary>
    /// Efecto de parpadeo de luz para simular fuego real
    /// </summary>
    private System.Collections.IEnumerator FlickerLight()
    {
        while (isOn && fireLight != null)
        {
            // Variar intensidad aleatoriamente
            fireLight.intensity = lightIntensity + Random.Range(-0.5f, 0.5f);
            yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
        }
    }
    
    /// <summary>
    /// Obtiene el estado actual
    /// </summary>
    public bool IsOn()
    {
        return isOn;
    }
    
    // Para testing en el Inspector
    [ContextMenu("Encender Chimenea")]
    private void TestTurnOn() => TurnOn();
    
    [ContextMenu("Apagar Chimenea")]
    private void TestTurnOff() => TurnOff();
}
