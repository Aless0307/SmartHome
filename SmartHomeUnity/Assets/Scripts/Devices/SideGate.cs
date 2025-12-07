using UnityEngine;

/// <summary>
/// SideGate modificado para funcionar con Smart Home Server
/// Funciona con tecla Y con comandos del servidor
/// </summary>
public class SideGate : MonoBehaviour
{
    public float distance = 300f;       // cuánto se mueve
    public float speed = 3f;            // velocidad
    public KeyCode key = KeyCode.P;     // tecla para abrir/cerrar
    
    // Eje en el que se mueve la puerta
    public enum Axis { X, Y, Z }
    public Axis axis = Axis.X;

    private Vector3 closedPos;
    private Vector3 openPos;
    private bool moving = false;
    private bool isOpen = false;

    void Start()
    {
        closedPos = transform.position;
        openPos = closedPos + AxisDirection() * distance;
    }

    private Vector3 AxisDirection()
    {
        switch (axis)
        {
            case Axis.X: return Vector3.left;
            case Axis.Y: return Vector3.down;
            case Axis.Z: return Vector3.back;
            default: return Vector3.left;
        }
    }

    void Update()
    {
        // Control por tecla (original)
        if (Input.GetKeyDown(key) && !moving)
        {
            Toggle();
        }

        // Movimiento
        if (moving)
        {
            Vector3 target = isOpen ? closedPos : openPos;
            transform.position = Vector3.MoveTowards(
                transform.position,
                target,
                speed * Time.deltaTime
            );

            if (Vector3.Distance(transform.position, target) < 0.01f)
            {
                transform.position = target;
                moving = false;
                isOpen = !isOpen;
            }
        }
    }
    
    // ═══════════════════════════════════════════════════════════
    // MÉTODOS PARA SMART HOME SERVER
    // ═══════════════════════════════════════════════════════════
    
    /// <summary>
    /// Toggle - llamado desde el servidor o tecla
    /// </summary>
    public void Toggle()
    {
        if (!moving)
        {
            moving = true;
            Debug.Log($"🚪 {gameObject.name}: {(isOpen ? "Cerrando" : "Abriendo")}");
        }
    }
    
    /// <summary>
    /// Llamado por DeviceBridge cuando status=true (GUI dice "Abierta")
    /// Pero en realidad necesitamos CERRAR para que coincida
    /// </summary>
    public void TurnOn()
    {
        Debug.Log($"🚪 {gameObject.name}: TurnOn (status=true) -> Cerrar puerta");
        // Cuando status=true, la puerta debe estar CERRADA visualmente
        if (isOpen && !moving)
        {
            moving = true; // Esto la cerrará porque isOpen=true va a closedPos
        }
    }
    
    /// <summary>
    /// Llamado por DeviceBridge cuando status=false (GUI dice "Cerrada")
    /// Pero en realidad necesitamos ABRIR para que coincida
    /// </summary>
    public void TurnOff()
    {
        Debug.Log($"🚪 {gameObject.name}: TurnOff (status=false) -> Abrir puerta");
        // Cuando status=false, la puerta debe estar ABIERTA visualmente
        if (!isOpen && !moving)
        {
            moving = true; // Esto la abrirá porque isOpen=false va a openPos
        }
    }
    
    /// <summary>
    /// Abrir puerta
    /// </summary>
    public void Open()
    {
        if (!moving && !isOpen)
        {
            moving = true;
            Debug.Log($"🚪 {gameObject.name}: Abriendo");
        }
    }
    
    /// <summary>
    /// Cerrar puerta
    /// </summary>
    public void Close()
    {
        if (!moving && isOpen)
        {
            moving = true;
            Debug.Log($"🚪 {gameObject.name}: Cerrando");
        }
    }
    
    /// <summary>
    /// Método llamado por DeviceBridge cuando el servidor envía comando
    /// </summary>
    public void OnSmartHomeToggle()
    {
        Toggle();
    }
    
    /// <summary>
    /// Para establecer estado específico desde el servidor
    /// </summary>
    public void SetState(bool open)
    {
        if (open && !isOpen)
            Open();
        else if (!open && isOpen)
            Close();
    }
    
    /// <summary>
    /// Obtener estado actual
    /// </summary>
    public bool IsOpen => isOpen;
}
