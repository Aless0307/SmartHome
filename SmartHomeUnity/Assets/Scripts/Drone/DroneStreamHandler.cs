using UnityEngine;
using Unity.RenderStreaming;
using UnityEngine.InputSystem;

/// <summary>
/// Componente que se adjunta a cada dron para manejar su stream individual
/// Usa los componentes de Unity Render Streaming directamente
/// </summary>
[RequireComponent(typeof(DroneController))]
public class DroneStreamHandler : MonoBehaviour
{
    [Header("Stream Components")]
    [SerializeField] private VideoStreamSender videoStreamSender;
    [SerializeField] private AudioStreamSender audioStreamSender;
    
    [Header("Camera Settings")]
    [SerializeField] private Camera streamCamera;
    [SerializeField] private int textureWidth = 1920;
    [SerializeField] private int textureHeight = 1080;
    [SerializeField] private int textureDepth = 24;
    
    [Header("Input")]
    [SerializeField] private InputAction moveAction;
    [SerializeField] private InputAction elevationAction;
    [SerializeField] private InputAction rotationAction;
    
    private DroneController droneController;
    private RenderTexture streamTexture;
    private string connectionId;
    private bool isInitialized = false;
    
    void Awake()
    {
        droneController = GetComponent<DroneController>();
        
        // Configurar cámara si no está asignada
        if (streamCamera == null)
        {
            streamCamera = GetComponentInChildren<Camera>();
        }
        
        // Configurar acciones de input
        SetupInputActions();
    }
    
    /// <summary>
    /// Configurar las acciones de input para el nuevo Input System
    /// </summary>
    private void SetupInputActions()
    {
        // Movimiento WASD/Arrows
        if (moveAction == null)
        {
            moveAction = new InputAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
        }
        
        // Elevación Space/Shift
        if (elevationAction == null)
        {
            elevationAction = new InputAction("Elevation", InputActionType.Value);
            elevationAction.AddCompositeBinding("1DAxis")
                .With("Positive", "<Keyboard>/space")
                .With("Negative", "<Keyboard>/leftShift");
            elevationAction.AddCompositeBinding("1DAxis")
                .With("Positive", "<Keyboard>/e")
                .With("Negative", "<Keyboard>/q");
        }
        
        // Rotación Q/E o Mouse
        if (rotationAction == null)
        {
            rotationAction = new InputAction("Rotation", InputActionType.Value);
            rotationAction.AddCompositeBinding("1DAxis")
                .With("Positive", "<Keyboard>/e")
                .With("Negative", "<Keyboard>/q");
            rotationAction.AddBinding("<Mouse>/delta/x");
        }
    }
    
    void OnEnable()
    {
        moveAction?.Enable();
        elevationAction?.Enable();
        rotationAction?.Enable();
    }
    
    void OnDisable()
    {
        moveAction?.Disable();
        elevationAction?.Disable();
        rotationAction?.Disable();
    }
    
    /// <summary>
    /// Inicializar el handler para una conexión específica
    /// </summary>
    public void Initialize(string connId)
    {
        connectionId = connId;
        
        // Crear RenderTexture para el stream
        CreateStreamTexture();
        
        // Configurar VideoStreamSender si existe
        SetupVideoSender();
        
        isInitialized = true;
        
        Debug.Log($"[DroneStreamHandler] Inicializado para conexión: {connectionId}");
    }
    
    /// <summary>
    /// Crear la textura de renderizado para el stream
    /// </summary>
    private void CreateStreamTexture()
    {
        // Liberar textura anterior si existe
        if (streamTexture != null)
        {
            streamTexture.Release();
            Destroy(streamTexture);
        }
        
        // Crear nueva RenderTexture
        streamTexture = new RenderTexture(textureWidth, textureHeight, textureDepth, RenderTextureFormat.BGRA32);
        streamTexture.name = $"DroneStream_{connectionId}";
        streamTexture.Create();
        
        // Asignar a la cámara
        if (streamCamera != null)
        {
            streamCamera.targetTexture = streamTexture;
            Debug.Log($"[DroneStreamHandler] RenderTexture creada: {textureWidth}x{textureHeight}");
        }
    }
    
    /// <summary>
    /// Configurar el VideoStreamSender
    /// </summary>
    private void SetupVideoSender()
    {
        if (videoStreamSender != null && streamTexture != null)
        {
            // El VideoStreamSender usará la textura de la cámara
            // La configuración específica depende de la versión del paquete
            Debug.Log("[DroneStreamHandler] VideoStreamSender configurado");
        }
    }
    
    void Update()
    {
        if (!isInitialized) return;
        
        // Procesar input y enviarlo al controlador del dron
        ProcessInput();
    }
    
    /// <summary>
    /// Procesar input del cliente remoto
    /// </summary>
    private void ProcessInput()
    {
        if (droneController == null) return;
        
        // Leer valores de las acciones
        Vector2 moveValue = moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
        float elevationValue = elevationAction?.ReadValue<float>() ?? 0f;
        float rotationValue = rotationAction?.ReadValue<float>() ?? 0f;
        
        // Aplicar al controlador
        droneController.OnMove(moveValue);
        droneController.OnElevation(elevationValue);
        droneController.OnRotation(rotationValue * 0.1f); // Reducir sensibilidad del mouse
    }
    
    /// <summary>
    /// Obtener la RenderTexture del stream
    /// </summary>
    public RenderTexture GetStreamTexture()
    {
        return streamTexture;
    }
    
    /// <summary>
    /// Obtener la cámara del dron
    /// </summary>
    public Camera GetCamera()
    {
        return streamCamera;
    }
    
    /// <summary>
    /// Cambiar resolución del stream en tiempo real
    /// </summary>
    public void SetResolution(int width, int height)
    {
        textureWidth = width;
        textureHeight = height;
        
        if (isInitialized)
        {
            CreateStreamTexture();
        }
    }
    
    void OnDestroy()
    {
        // Limpiar recursos
        if (streamTexture != null)
        {
            streamTexture.Release();
            Destroy(streamTexture);
        }
        
        moveAction?.Dispose();
        elevationAction?.Dispose();
        rotationAction?.Dispose();
    }
}
