using UnityEngine;
using Unity.RenderStreaming;

/// <summary>
/// Controlador de dron individual - maneja el movimiento basado en input
/// Cada cliente tiene su propio dron con este script
/// </summary>
public class DroneController : MonoBehaviour
{
    [Header("Movement Settings")]
    private float moveSpeed = 250f;        // Velocidad reducida a la mitad
    private float rotationSpeed = 100f;    // Velocidad reducida a la mitad
    private float verticalSpeed = 150f;    // Velocidad reducida a la mitad
    private float smoothTime = 0.01f;      // Forzar valor del código
    
    [Header("Drone Physics")]
    [SerializeField] private float hoverForce = 9.81f;
    [SerializeField] private float tiltAmount = 15f;
    
    [Header("Camera Settings")]
    [SerializeField] private Camera droneCamera;
    
    [Header("Boundaries")]
    [SerializeField] private Vector3 minBounds = new Vector3(-500, -100, -500);
    [SerializeField] private Vector3 maxBounds = new Vector3(500, 300, 500);
    [SerializeField] private bool useBoundaries = false; // Desactivar por defecto
    
    [Header("Visual")]
    [SerializeField] private GameObject[] propellers;
    [SerializeField] private float propellerSpeed = 1000f;
    [SerializeField] private Light droneLight;
    [SerializeField] private Color droneColor = Color.white;
    
    // Input values (set by InputReceiver or directly)
    private float horizontalInput;
    private float verticalInput;
    private float elevationInput;
    private float rotationInput;
    
    // Movement smoothing
    private Vector3 currentVelocity;
    private Vector3 targetVelocity;
    private Vector3 smoothVelocity;
    
    // Drone state
    private bool isActive = true;
    private string ownerId; // ID del cliente que controla este dron
    
    // Components
    private Rigidbody rb;
    private AudioSource audioSource;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        
        // Si no hay Rigidbody, usar movimiento simple
        if (rb != null)
        {
            rb.useGravity = false;
            rb.drag = 2f;
            rb.angularDrag = 4f;
        }
        
        // Configurar cámara - buscar en múltiples lugares
        FindAndSetupCamera();
        
        // Configurar luz del dron con color único
        if (droneLight != null)
        {
            droneLight.color = droneColor;
        }
        
        // Aplicar color a materiales del dron
        SetDroneColor(droneColor);
    }
    
    void OnDestroy()
    {
        // Limpiar el RenderTexture
        if (droneStreamTexture != null)
        {
            droneStreamTexture.Release();
            Destroy(droneStreamTexture);
            droneStreamTexture = null;
        }
        
        // Desactivar cámara
        if (droneCamera != null)
        {
            droneCamera.enabled = false;
        }
        
        Debug.Log("[DroneController] 📷 Dron destruido - recursos limpiados");
    }
    
    /// <summary>
    /// Restaura el VideoStreamSender para que NO apunte a ninguna cámara (o a MainCamera)
    /// Esto evita que Unity muestre la cámara del dron después de destruirlo
    /// </summary>
    private void RestoreMainCameraToVideoStreamSender()
    {
        var videoSenders = FindObjectsByType<VideoStreamSender>(FindObjectsSortMode.None);
        Camera mainCam = Camera.main;
        
        foreach (var sender in videoSenders)
        {
            var senderType = sender.GetType();
            var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            
            // Restaurar la cámara a MainCamera (o null)
            var sourceField = senderType.GetField("m_source", bindingFlags);
            if (sourceField != null)
            {
                try 
                { 
                    // Poner MainCamera o null para que no apunte a la cámara del dron destruido
                    sourceField.SetValue(sender, mainCam); 
                    Debug.Log("[DroneController] 📷 VideoStreamSender restaurado a MainCamera");
                }
                catch { }
            }
        }
    }
    
    /// <summary>
    /// Buscar y configurar la cámara del dron
    /// NUNCA usa Camera.main - solo cámaras hijas o crea una nueva
    /// La cámara del dron SOLO renderiza a RenderTexture (no a pantalla)
    /// Así la MainCamera siempre se ve en Unity
    /// </summary>
    private void FindAndSetupCamera()
    {
        // Si ya está asignada, configurarla para solo renderizar a RenderTexture
        if (droneCamera != null)
        {
            ConfigureDroneCameraForStreamingOnly();
            return;
        }
        
        // Buscar en hijos SOLAMENTE
        droneCamera = GetComponentInChildren<Camera>();
        if (droneCamera != null)
        {
            ConfigureDroneCameraForStreamingOnly();
            return;
        }
        
        // Si no hay cámara hija, crear una nueva como hijo del dron
        Debug.Log("[DroneController] Creando cámara para el dron");
        GameObject camObj = new GameObject("DroneCamera");
        camObj.transform.SetParent(transform);
        camObj.transform.localPosition = new Vector3(0, 3, -8);
        camObj.transform.localRotation = Quaternion.Euler(20, 0, 0);
        droneCamera = camObj.AddComponent<Camera>();
        droneCamera.tag = "Untagged"; // NO es MainCamera
        
        ConfigureDroneCameraForStreamingOnly();
    }
    
    /// <summary>
    /// Configura la cámara del dron para que SOLO renderice al RenderTexture del streaming
    /// NO renderiza a la pantalla, así la MainCamera siempre se ve en Unity
    /// </summary>
    private void ConfigureDroneCameraForStreamingOnly()
    {
        if (droneCamera == null) return;
        
        // Crear RenderTexture si no existe
        if (droneStreamTexture == null)
        {
            droneStreamTexture = new RenderTexture(1280, 720, 24, RenderTextureFormat.BGRA32);
            droneStreamTexture.name = "DroneStreamRT";
            droneStreamTexture.Create();
        }
        
        // La cámara del dron SOLO renderiza al RenderTexture, NO a la pantalla
        droneCamera.targetTexture = droneStreamTexture;
        droneCamera.depth = -10; // Menor prioridad que MainCamera
        droneCamera.enabled = true; // Puede estar activa porque no renderiza a pantalla
        
        Debug.Log("[DroneController] 📷 Cámara del dron configurada para streaming (no afecta vista Unity)");
    }
    
    // RenderTexture exclusivo para esta cámara de dron
    private RenderTexture droneStreamTexture;
    
    void Update()
    {
        if (!isActive) return;
        
        // Rotar propelas
        RotatePropellers();
        
        // Forzar cámara
        ForceCameraPosition();
    }
    
    void LateUpdate()
    {
        // ÚLTIMO paso: asegurar que la cámara esté con el dron
        ForceCameraPosition();
    }
    
    void FixedUpdate()
    {
        if (!isActive) return;
        
        if (rb != null)
        {
            MoveWithPhysics();
        }
        else
        {
            MoveSimple();
        }
    }
    
    /// <summary>
    /// Movimiento usando física (más realista)
    /// </summary>
    private void MoveWithPhysics()
    {
        // Calcular dirección de movimiento relativa al dron
        Vector3 forward = transform.forward * verticalInput;
        Vector3 right = transform.right * horizontalInput;
        Vector3 up = Vector3.up * elevationInput;
        
        // Aplicar fuerza de movimiento
        Vector3 moveForce = (forward + right) * moveSpeed;
        moveForce += up * verticalSpeed;
        
        // Fuerza de hover para contrarrestar gravedad
        moveForce += Vector3.up * hoverForce;
        
        rb.AddForce(moveForce, ForceMode.Force);
        
        // Rotación
        float rotation = rotationInput * rotationSpeed * Time.fixedDeltaTime;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0, rotation, 0));
        
        // Aplicar límites
        EnforceBoundaries();
    }
    
    /// <summary>
    /// Movimiento simple sin física (más responsivo)
    /// </summary>
    private void MoveSimple()
    {
        // El modelo del dron está rotado, así que usamos ejes del mundo
        // pero rotados según la orientación horizontal del dron (yaw)
        
        // Obtener la rotación horizontal del dron (solo en Y)
        float yaw = transform.eulerAngles.y;
        Quaternion horizontalRotation = Quaternion.Euler(0, yaw, 0);
        
        // W/S = adelante/atrás en el plano horizontal
        Vector3 forward = horizontalRotation * Vector3.forward * verticalInput;
        
        // A/D = izquierda/derecha en el plano horizontal  
        Vector3 right = horizontalRotation * Vector3.right * horizontalInput;
        
        // Space/Shift = arriba/abajo (siempre vertical)
        Vector3 up = Vector3.up * elevationInput;
        
        targetVelocity = (forward + right) * moveSpeed + up * verticalSpeed;
        
        // Suavizar movimiento
        currentVelocity = Vector3.SmoothDamp(currentVelocity, targetVelocity, ref smoothVelocity, smoothTime);
        
        // Aplicar movimiento
        transform.position += currentVelocity * Time.fixedDeltaTime;
        
        // Rotación (Q/E) - solo en Y
        float rotation = rotationInput * rotationSpeed * Time.fixedDeltaTime;
        transform.Rotate(0, rotation, 0, Space.World);
        
        // Aplicar límites
        EnforceBoundaries();
        
        // FORZAR CÁMARA A SEGUIR AL DRON
        ForceCameraPosition();
    }
    
    /// <summary>
    /// Forzar la cámara a estar en la posición del dron
    /// </summary>
    private void ForceCameraPosition()
    {
        // Solo procesar si ESTE dron es el que recibe input
        if (!hasReceivedInput) return;
        
        if (droneCamera == null) return;
        
        // SIEMPRE mover la cámara, sea hija o no
        // Usar solo el yaw para la rotación de la cámara (ignorar pitch/roll del modelo)
        float yaw = transform.eulerAngles.y;
        Quaternion yawRotation = Quaternion.Euler(0, yaw, 0);
        
        // Calcular posición con offset rotado
        Vector3 rotatedOffset = yawRotation * cameraOffset;
        Vector3 targetPos = transform.position + rotatedOffset;
        
        // Rotación: yaw del dron + pitch de la cámara (mirando ligeramente hacia abajo)
        Quaternion cameraRotation = Quaternion.Euler(cameraPitch, yaw, 0);
        
        // Si es hija, primero desparentarla para poder moverla libremente
        if (droneCamera.transform.IsChildOf(transform))
        {
            droneCamera.transform.SetParent(null);
        }
        
        // FORZAR posición y rotación
        droneCamera.transform.position = targetPos;
        droneCamera.transform.rotation = cameraRotation;
    }
    
    // Flag para saber si este dron ha recibido input
    private bool hasReceivedInput = false;
    
    /// <summary>
    /// Mantener el dron dentro de los límites de la escena
    /// </summary>
    private void EnforceBoundaries()
    {
        if (!useBoundaries) return; // Solo aplicar si está activado
        
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minBounds.x, maxBounds.x);
        pos.y = Mathf.Clamp(pos.y, minBounds.y, maxBounds.y);
        pos.z = Mathf.Clamp(pos.z, minBounds.z, maxBounds.z);
        transform.position = pos;
    }
    
    /// <summary>
    /// Inclinar visualmente el dron basado en movimiento
    /// </summary>
    private void ApplyVisualTilt()
    {
        // DESACTIVADO temporalmente para debug
        // El tilt puede interferir con el movimiento
        return;
        
        // Calcular inclinación basada en input
        float targetPitch = -verticalInput * tiltAmount;
        float targetRoll = horizontalInput * tiltAmount;
        
        // Obtener rotación actual
        Vector3 currentRotation = transform.localEulerAngles;
        
        // Convertir a rango -180 a 180
        if (currentRotation.x > 180) currentRotation.x -= 360;
        if (currentRotation.z > 180) currentRotation.z -= 360;
        
        // Suavizar hacia la inclinación objetivo
        float newPitch = Mathf.Lerp(currentRotation.x, targetPitch, Time.deltaTime * 5f);
        float newRoll = Mathf.Lerp(currentRotation.z, targetRoll, Time.deltaTime * 5f);
        
        // Aplicar solo inclinación, mantener yaw actual
        transform.localEulerAngles = new Vector3(newPitch, transform.localEulerAngles.y, newRoll);
    }
    
    /// <summary>
    /// Rotar las hélices del dron
    /// </summary>
    private void RotatePropellers()
    {
        if (propellers == null) return;
        
        foreach (var propeller in propellers)
        {
            if (propeller != null)
            {
                propeller.transform.Rotate(0, propellerSpeed * Time.deltaTime, 0);
            }
        }
    }
    
    /// <summary>
    /// Actualizar la cámara para seguir al dron
    /// </summary>
    private void UpdateCamera()
    {
        if (droneCamera == null) return;
        
        // FORZAR posición y rotación de la cámara para que siempre esté con el dron
        droneCamera.transform.position = transform.position + transform.TransformDirection(cameraOffset);
        droneCamera.transform.rotation = transform.rotation;
    }
    
    [Header("Camera Offset - Ajustar en Inspector")]
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 2f, 5f); // ADELANTE del dron (vista primera persona)
    [SerializeField] private float cameraPitch = 10f; // Inclinación hacia abajo
    
    #region Input Methods - Llamados por Unity Render Streaming InputReceiver
    
    /// <summary>
    /// Recibir input horizontal (A/D o flechas izq/der)
    /// </summary>
    public void OnHorizontal(float value)
    {
        horizontalInput = value;
    }
    
    /// <summary>
    /// Recibir input vertical (W/S o flechas arriba/abajo)
    /// </summary>
    public void OnVertical(float value)
    {
        verticalInput = value;
    }
    
    /// <summary>
    /// Recibir input de elevación (Space/Shift o Q/E)
    /// </summary>
    public void OnElevation(float value)
    {
        elevationInput = value;
    }
    
    /// <summary>
    /// Recibir input de rotación (Q/E o mouse)
    /// </summary>
    public void OnRotation(float value)
    {
        rotationInput = value;
    }
    
    /// <summary>
    /// Método alternativo para recibir input como Vector2
    /// Compatible con el nuevo Input System de Unity
    /// </summary>
    public void OnMove(Vector2 value)
    {
        horizontalInput = value.x;
        verticalInput = value.y;
    }
    
    /// <summary>
    /// Establecer todos los inputs de una vez (útil para WebRTC)
    /// </summary>
    public void SetInput(float horizontal, float vertical, float elevation, float rotation)
    {
        horizontalInput = horizontal;
        verticalInput = vertical;
        elevationInput = elevation;
        rotationInput = rotation;
        
        // Marcar que este dron ha recibido input - solo este controlará la cámara
        if (horizontal != 0 || vertical != 0 || elevation != 0 || rotation != 0)
        {
            if (!hasReceivedInput)
            {
                hasReceivedInput = true;
                SwitchToThisDroneCamera();
            }
        }
    }
    
    // RenderTexture compartido para streaming
    private static RenderTexture sharedStreamTexture;
    
    // Cámara que el VideoStreamSender ya está usando - la moveremos para seguir al dron
    private static Camera streamingCamera;
    private static bool streamingCameraFound = false;
    
    /// <summary>
    /// Cambia la cámara del streaming a la cámara de este dron
    /// Usa RenderTexture para no afectar la vista de Unity
    /// </summary>
    private void SwitchToThisDroneCamera()
    {
        if (droneCamera == null) FindAndSetupCamera();
        if (droneCamera == null) return;
        
        // La cámara ya está configurada con RenderTexture en FindAndSetupCamera
        // Solo necesitamos configurar el VideoStreamSender para usar nuestra cámara/RenderTexture
        Debug.Log("[DroneController] 📷 Configurando streaming con cámara del dron");
        
        ConfigureVideoStreamSenderWithDroneCamera();
    }
    
    /// <summary>
    /// Configura el VideoStreamSender para usar la cámara del dron (que renderiza a RenderTexture)
    /// </summary>
    private void ConfigureVideoStreamSenderWithDroneCamera()
    {
        var videoSenders = FindObjectsByType<VideoStreamSender>(FindObjectsSortMode.None);
        
        foreach (var sender in videoSenders)
        {
            var senderType = sender.GetType();
            var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            
            // Opción 1: Establecer la cámara directamente
            var sourceTypeField = senderType.GetField("m_sourceType", bindingFlags);
            if (sourceTypeField != null)
            {
                try { sourceTypeField.SetValue(sender, 0); } // 0 = Camera
                catch { }
            }
            
            var sourceField = senderType.GetField("m_source", bindingFlags);
            if (sourceField != null)
            {
                try { sourceField.SetValue(sender, droneCamera); }
                catch { }
            }
            
            // Opción 2: Si usa RenderTexture, asignar el nuestro
            if (droneStreamTexture != null)
            {
                foreach (var field in senderType.GetFields(bindingFlags))
                {
                    if (field.FieldType == typeof(RenderTexture))
                    {
                        try { field.SetValue(sender, droneStreamTexture); }
                        catch { }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Encontrar la cámara que usa el VideoStreamSender y hacer que siga a este dron
    /// (Método legacy - ahora usa DroneStreamInitializer)
    /// </summary>
    private void FindStreamingCamera()
    {
        Debug.Log("[DroneController] 🔍 Buscando cámara de streaming...");
        
        // ESTRATEGIA SIMPLE: Encontrar el VideoStreamSender existente y cambiar su cámara a la del dron
        
        // 1. Primero buscar la cámara dentro del prefab del dron
        var droneCameras = GetComponentsInChildren<Camera>(true);
        foreach (var cam in droneCameras)
        {
            if (cam.name.Contains("Drone") || cam.name.Contains("Camera"))
            {
                droneCamera = cam;
                Debug.Log($"[DroneController] ✅ Usando DroneCamera del prefab: {cam.name}");
                break;
            }
        }
        
        // Si no encontramos cámara en el dron, crear una
        if (droneCamera == null)
        {
            GameObject camObj = new GameObject("DroneCamera");
            camObj.transform.SetParent(transform);
            camObj.transform.localPosition = new Vector3(0, 0.5f, 0);
            camObj.transform.localRotation = Quaternion.identity;
            droneCamera = camObj.AddComponent<Camera>();
            droneCamera.fieldOfView = 90;
            droneCamera.nearClipPlane = 0.1f;
            droneCamera.farClipPlane = 1000f;
        }
        
        // 2. Encontrar el VideoStreamSender existente en la escena y cambiar su cámara
        var videoSenders = FindObjectsByType<VideoStreamSender>(FindObjectsSortMode.None);
        
        foreach (var sender in videoSenders)
        {
            bool cameraSet = SetVideoStreamSenderCamera(sender, droneCamera);
            if (cameraSet)
            {
                streamingCameraFound = true;
                break;
            }
        }
    }
    
    /// <summary>
    /// Cambiar la cámara de un VideoStreamSender usando reflexión para máxima compatibilidad
    /// </summary>
    private bool SetVideoStreamSenderCamera(VideoStreamSender sender, Camera newCamera)
    {
        var senderType = sender.GetType();
        bool success = false;
        
        // 1. Intentar propiedad 'sourceCamera' o 'camera'
        foreach (var prop in senderType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
            if ((prop.Name.ToLower().Contains("camera") || prop.Name.ToLower().Contains("source")) 
                && prop.PropertyType == typeof(Camera) && prop.CanWrite)
            {
                try { prop.SetValue(sender, newCamera); success = true; }
                catch { }
            }
        }
        
        // 2. Intentar campo 'm_Camera' o similar
        foreach (var field in senderType.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
        {
            if ((field.Name.ToLower().Contains("camera") || field.Name == "m_Camera" || field.Name == "m_camera")
                && field.FieldType == typeof(Camera))
            {
                try { field.SetValue(sender, newCamera); success = true; }
                catch { }
            }
        }
        
        // 3. Si el VideoStreamSender usa RenderTexture en lugar de Camera directa
        if (!success)
        {
            // Crear RenderTexture compartido
            if (sharedStreamTexture == null)
            {
                sharedStreamTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.BGRA32);
                sharedStreamTexture.name = "DroneStreamRT";
                sharedStreamTexture.Create();
            }
            
            // Asignar a la cámara del dron
            newCamera.targetTexture = sharedStreamTexture;
            
            // Buscar campos de Texture/source en el sender
            foreach (var field in senderType.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
            {
                if (field.FieldType == typeof(Texture) || field.FieldType == typeof(RenderTexture) 
                    || field.Name.ToLower().Contains("source") || field.Name.ToLower().Contains("texture"))
                {
                    try { field.SetValue(sender, sharedStreamTexture); success = true; }
                    catch { }
                }
            }
            
            // Intentar propiedades también
            foreach (var prop in senderType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if ((prop.PropertyType == typeof(Texture) || prop.PropertyType == typeof(RenderTexture))
                    && prop.CanWrite)
                {
                    try { prop.SetValue(sender, sharedStreamTexture); success = true; }
                    catch { }
                }
            }
        }
        
        return success;
    }
    
    #endregion
    
    #region Configuration Methods
    
    /// <summary>
    /// Establecer el ID del cliente dueño de este dron
    /// </summary>
    public void SetOwner(string id)
    {
        ownerId = id;
        gameObject.name = $"Drone_{id}";
    }
    
    /// <summary>
    /// Obtener el ID del dueño
    /// </summary>
    public string GetOwner()
    {
        return ownerId;
    }
    
    /// <summary>
    /// Establecer color único del dron
    /// </summary>
    public void SetDroneColor(Color color)
    {
        droneColor = color;
        
        // Aplicar a luz
        if (droneLight != null)
        {
            droneLight.color = color;
        }
        
        // Aplicar a materiales (buscar renderers)
        var renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (var renderer in renderers)
        {
            // Crear instancia del material para no afectar otros drones
            foreach (var material in renderer.materials)
            {
                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", color * 2f);
                }
            }
        }
    }
    
    /// <summary>
    /// Establecer límites de vuelo
    /// </summary>
    public void SetBoundaries(Vector3 min, Vector3 max)
    {
        minBounds = min;
        maxBounds = max;
    }
    
    /// <summary>
    /// Activar/desactivar el dron
    /// </summary>
    public void SetActive(bool active)
    {
        isActive = active;
        
        if (!active && rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
    
    /// <summary>
    /// Obtener la cámara del dron
    /// </summary>
    public Camera GetCamera()
    {
        return droneCamera;
    }
    
    /// <summary>
    /// Teletransportar el dron a una posición
    /// </summary>
    public void Teleport(Vector3 position)
    {
        transform.position = position;
        
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        currentVelocity = Vector3.zero;
    }
    
    #endregion
    
    /// <summary>
    /// Dibujar límites en el editor
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = (minBounds + maxBounds) / 2f;
        Vector3 size = maxBounds - minBounds;
        Gizmos.DrawWireCube(center, size);
    }
}
