using UnityEngine;

/// <summary>
/// TVLift modificado para funcionar con Smart Home Server
/// Funciona con tecla Y con comandos del servidor
/// </summary>
public class TVLift : MonoBehaviour
{
    public Vector3 targetPosition;
    public float speed = 2f;
    
    private Vector3 initialPosition;
    private Vector3 currentTarget;
    private bool moving = false;
    private bool isAtTarget = false;

    void Start()
    {
        initialPosition = transform.position;
        currentTarget = targetPosition;
    }

    void Update()
    {
        // Control por tecla (original)
        if (Input.GetKeyDown(KeyCode.R))
        {
            Toggle();
        }

        // Movimiento
        if (moving)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                currentTarget,
                speed * Time.deltaTime
            );

            if (Vector3.Distance(transform.position, currentTarget) < 0.01f)
            {
                transform.position = currentTarget;
                moving = false;
                isAtTarget = !isAtTarget;
            }
        }
    }
    
    // ═══════════════════════════════════════════════════════════
    // MÉTODOS PARA SMART HOME SERVER
    // ═══════════════════════════════════════════════════════════
    
    public void Toggle()
    {
        if (!moving)
        {
            currentTarget = isAtTarget ? initialPosition : targetPosition;
            moving = true;
            Debug.Log($"📺 {gameObject.name}: Moviendo");
        }
    }
    
    public void Raise()
    {
        if (!moving && !isAtTarget)
        {
            currentTarget = targetPosition;
            moving = true;
        }
    }
    
    public void Lower()
    {
        if (!moving && isAtTarget)
        {
            currentTarget = initialPosition;
            moving = true;
        }
    }
    
    public void OnSmartHomeToggle()
    {
        Toggle();
    }
    
    /// <summary>
    /// Llamado por DeviceBridge cuando status=true (Mostrar TV)
    /// </summary>
    public void TurnOn()
    {
        Debug.Log($"📺 {gameObject.name}: TurnOn -> Mostrar TV (Bajar)");
        Lower(); // Bajar = mostrar la TV
    }
    
    /// <summary>
    /// Llamado por DeviceBridge cuando status=false (Esconder TV)
    /// </summary>
    public void TurnOff()
    {
        Debug.Log($"📺 {gameObject.name}: TurnOff -> Esconder TV (Subir)");
        Raise(); // Subir = esconder la TV
    }
    
    public void SetState(bool raised)
    {
        if (raised && !isAtTarget)
            Raise();
        else if (!raised && isAtTarget)
            Lower();
    }
    
    public bool IsRaised => isAtTarget;
}
