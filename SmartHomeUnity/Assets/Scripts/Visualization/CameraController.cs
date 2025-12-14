using UnityEngine;

/// <summary>
/// Controlador de cámara para navegar la casa 3D
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Configuración")]
    public float moveSpeed = 10f;
    public float rotateSpeed = 3f;
    public float zoomSpeed = 5f;
    public float smoothTime = 0.1f;
    
    [Header("Límites")]
    public float minZoom = 5f;
    public float maxZoom = 50f;
    public float minAngle = 10f;
    public float maxAngle = 80f;
    
    [Header("Objetivo")]
    public Transform target;
    public Vector3 offset = new Vector3(0, 10, -10);
    
    // Estado
    private float currentZoom;
    private float currentAngle = 45f;
    private float currentRotation = 0f;
    private Vector3 velocity;
    
    void Start()
    {
        currentZoom = offset.magnitude;
        
        if (target == null)
        {
            // Crear punto de objetivo en el centro
            GameObject targetObj = new GameObject("CameraTarget");
            target = targetObj.transform;
            target.position = Vector3.zero;
        }
        
        UpdateCameraPosition();
    }
    
    void Update()
    {
        HandleInput();
        UpdateCameraPosition();
    }
    
    void HandleInput()
    {
        // Rotación con clic derecho
        if (Input.GetMouseButton(1))
        {
            currentRotation += Input.GetAxis("Mouse X") * rotateSpeed;
            currentAngle -= Input.GetAxis("Mouse Y") * rotateSpeed;
            currentAngle = Mathf.Clamp(currentAngle, minAngle, maxAngle);
        }
        
        // Zoom con rueda del mouse
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            currentZoom -= scroll * zoomSpeed;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
        }
        
        // Movimiento con WASD o flechas
        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            move.z += 1;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            move.z -= 1;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            move.x -= 1;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            move.x += 1;
        
        if (move != Vector3.zero)
        {
            move = Quaternion.Euler(0, currentRotation, 0) * move;
            target.position += move.normalized * moveSpeed * Time.deltaTime;
        }
        
        // Reset con R
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetCamera();
        }
    }
    
    void UpdateCameraPosition()
    {
        if (target == null) return;
        
        // Calcular posición basada en ángulo y zoom
        float x = currentZoom * Mathf.Sin(currentRotation * Mathf.Deg2Rad) * Mathf.Cos(currentAngle * Mathf.Deg2Rad);
        float y = currentZoom * Mathf.Sin(currentAngle * Mathf.Deg2Rad);
        float z = currentZoom * Mathf.Cos(currentRotation * Mathf.Deg2Rad) * Mathf.Cos(currentAngle * Mathf.Deg2Rad);
        
        Vector3 targetPosition = target.position + new Vector3(x, y, z);
        
        // Suavizar movimiento
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        transform.LookAt(target.position);
    }
    
    /// <summary>
    /// Enfocar una habitación específica
    /// </summary>
    public void FocusOnRoom(RoomController room)
    {
        if (room == null) return;
        target.position = room.transform.position + Vector3.up * (room.roomSize.y / 2);
    }
    
    /// <summary>
    /// Enfocar un dispositivo específico
    /// </summary>
    public void FocusOnDevice(DeviceVisual device)
    {
        if (device == null) return;
        target.position = device.transform.position;
        currentZoom = 5f;
    }
    
    /// <summary>
    /// Resetear cámara a posición inicial
    /// </summary>
    public void ResetCamera()
    {
        target.position = Vector3.zero;
        currentZoom = 15f;
        currentAngle = 45f;
        currentRotation = 0f;
    }
    
    /// <summary>
    /// Vista superior
    /// </summary>
    public void TopView()
    {
        currentAngle = 80f;
        currentZoom = 30f;
    }
    
    /// <summary>
    /// Vista isométrica
    /// </summary>
    public void IsometricView()
    {
        currentAngle = 45f;
        currentRotation = 45f;
        currentZoom = 20f;
    }
}
