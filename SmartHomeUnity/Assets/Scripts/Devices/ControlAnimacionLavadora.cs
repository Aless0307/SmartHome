using UnityEngine;

/// <summary>
/// Control de Animación de Lavadora - Compatible con Smart Home Server
/// Funciona con tecla R y con comandos del servidor
/// </summary>
public class ControlAnimacionLavadora : MonoBehaviour
{
    // Componente Animation (el antiguo sistema)
    private Animation _animation;
    
    // Nombre del clip de animación
    public string nombreClip = "Animation";
    
    private bool estaReproduciendo = false;

    void Start()
    {
        // Obtener el componente Animation
        _animation = GetComponent<Animation>();
        
        if (_animation == null)
        {
            Debug.LogError("Error: No se encontró el componente Animation en " + gameObject.name);
            enabled = false;
            return;
        }

        // Desactivar reproducción automática
        _animation.playAutomatically = false;
        
        // Detener cualquier animación
        _animation.Stop();
        
        Debug.Log("🧺 Lavadora lista. Presiona 'R' para iniciar/detener.");
    }

    void Update()
    {
        // Control por tecla (original)
        if (Input.GetKeyDown(KeyCode.L))
        {
            Toggle();
        }
    }

    // ═══════════════════════════════════════════════════════════
    // MÉTODOS PARA SMART HOME SERVER
    // ═══════════════════════════════════════════════════════════
    
    public void Toggle()
    {
        estaReproduciendo = !estaReproduciendo;
        
        if (estaReproduciendo)
        {
            _animation.Play(nombreClip);
            Debug.Log("🧺 Lavadora INICIADA");
        }
        else
        {
            _animation.Stop(nombreClip);
            Debug.Log("🧺 Lavadora DETENIDA");
        }
    }
    
    public void TurnOn()
    {
        if (!estaReproduciendo)
        {
            estaReproduciendo = true;
            _animation.Play(nombreClip);
            Debug.Log("🧺 Lavadora INICIADA");
        }
    }
    
    public void TurnOff()
    {
        if (estaReproduciendo)
        {
            estaReproduciendo = false;
            _animation.Stop(nombreClip);
            Debug.Log("🧺 Lavadora DETENIDA");
        }
    }
    
    public void OnSmartHomeToggle()
    {
        Debug.Log("🧺 OnSmartHomeToggle RECIBIDO!");
        Toggle();
    }
    
    public void SetState(bool on)
    {
        if (on)
            TurnOn();
        else
            TurnOff();
    }
    
    public bool IsRunning => estaReproduciendo;

    void OnDisable()
    {
        if (_animation != null)
        {
            _animation.Stop();
        }
    }
}
