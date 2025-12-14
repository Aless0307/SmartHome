using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gestor de la interfaz de usuario para el Smart Home
/// Maneja login y visualización de dispositivos
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject loginPanel;
    public GameObject mainPanel;
    public GameObject loadingPanel;
    
    [Header("Login")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public Button loginButton;
    public TMP_Text loginStatusText;
    
    [Header("Main")]
    public TMP_Text connectionStatusText;
    public TMP_Text userNameText;
    public Transform deviceListContent;
    public GameObject deviceCardPrefab;
    
    [Header("Colores")]
    public Color connectedColor = Color.green;
    public Color disconnectedColor = Color.red;
    public Color deviceOnColor = new Color(0.2f, 0.8f, 0.2f);
    public Color deviceOffColor = new Color(0.5f, 0.5f, 0.5f);
    
    // Singleton
    public static UIManager Instance { get; private set; }
    
    // Cache de cards de dispositivos
    private Dictionary<string, DeviceCardUI> deviceCards = new Dictionary<string, DeviceCardUI>();
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // Configurar eventos de botones
        if (loginButton != null)
        {
            loginButton.onClick.AddListener(OnLoginClicked);
        }
        
        // Suscribirse a eventos del cliente
        if (SmartHomeClient.Instance != null)
        {
            SmartHomeClient.Instance.OnConnectionChanged += OnConnectionChanged;
            SmartHomeClient.Instance.OnLoginResult += OnLoginResult;
        }
        
        // Suscribirse a eventos del DeviceManager
        if (DeviceManager.Instance != null)
        {
            DeviceManager.Instance.OnDevicesLoaded.AddListener(OnDevicesLoaded);
            DeviceManager.Instance.OnDeviceUpdated.AddListener(OnDeviceUpdated);
        }
        
        // Mostrar panel de login
        ShowLoginPanel();
    }
    
    void OnDestroy()
    {
        if (SmartHomeClient.Instance != null)
        {
            SmartHomeClient.Instance.OnConnectionChanged -= OnConnectionChanged;
            SmartHomeClient.Instance.OnLoginResult -= OnLoginResult;
        }
        
        if (DeviceManager.Instance != null)
        {
            DeviceManager.Instance.OnDevicesLoaded.RemoveListener(OnDevicesLoaded);
            DeviceManager.Instance.OnDeviceUpdated.RemoveListener(OnDeviceUpdated);
        }
    }
    
    // ═══════════════════════════════════════════════════════════
    // PANELES
    // ═══════════════════════════════════════════════════════════
    
    public void ShowLoginPanel()
    {
        if (loginPanel != null) loginPanel.SetActive(true);
        if (mainPanel != null) mainPanel.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }
    
    public void ShowMainPanel()
    {
        if (loginPanel != null) loginPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }
    
    public void ShowLoadingPanel(string message = "Cargando...")
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            var text = loadingPanel.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = message;
        }
    }
    
    public void HideLoadingPanel()
    {
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }
    
    // ═══════════════════════════════════════════════════════════
    // LOGIN
    // ═══════════════════════════════════════════════════════════
    
    private void OnLoginClicked()
    {
        string username = usernameInput != null ? usernameInput.text : "admin";
        string password = passwordInput != null ? passwordInput.text : "admin123";
        
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            SetLoginStatus("Por favor ingrese usuario y contraseña", Color.yellow);
            return;
        }
        
        ShowLoadingPanel("Conectando...");
        
        if (SmartHomeClient.Instance != null)
        {
            SmartHomeClient.Instance.username = username;
            SmartHomeClient.Instance.password = password;
            SmartHomeClient.Instance.Connect();
        }
    }
    
    private void OnConnectionChanged(bool connected)
    {
        if (connectionStatusText != null)
        {
            connectionStatusText.text = connected ? "● Conectado" : "○ Desconectado";
            connectionStatusText.color = connected ? connectedColor : disconnectedColor;
        }
        
        if (!connected)
        {
            HideLoadingPanel();
            ShowLoginPanel();
            SetLoginStatus("Desconectado del servidor", disconnectedColor);
        }
    }
    
    private void OnLoginResult(bool success)
    {
        HideLoadingPanel();
        
        if (success)
        {
            SetLoginStatus("Login exitoso!", connectedColor);
            
            if (userNameText != null && SmartHomeClient.Instance != null)
            {
                userNameText.text = $"Usuario: {SmartHomeClient.Instance.username}";
            }
            
            ShowMainPanel();
        }
        else
        {
            SetLoginStatus("Usuario o contraseña incorrectos", disconnectedColor);
        }
    }
    
    private void SetLoginStatus(string message, Color color)
    {
        if (loginStatusText != null)
        {
            loginStatusText.text = message;
            loginStatusText.color = color;
        }
    }
    
    // ═══════════════════════════════════════════════════════════
    // DISPOSITIVOS
    // ═══════════════════════════════════════════════════════════
    
    private void OnDevicesLoaded(List<DeviceData> devices)
    {
        ClearDeviceCards();
        
        foreach (var device in devices)
        {
            CreateDeviceCard(device);
        }
    }
    
    private void OnDeviceUpdated(DeviceData device)
    {
        if (deviceCards.ContainsKey(device.id))
        {
            deviceCards[device.id].UpdateData(device);
        }
        else
        {
            CreateDeviceCard(device);
        }
    }
    
    private void CreateDeviceCard(DeviceData device)
    {
        if (deviceCardPrefab == null || deviceListContent == null) return;
        
        GameObject cardObj = Instantiate(deviceCardPrefab, deviceListContent);
        DeviceCardUI card = cardObj.GetComponent<DeviceCardUI>();
        
        if (card != null)
        {
            card.SetDevice(device);
            deviceCards[device.id] = card;
        }
    }
    
    private void ClearDeviceCards()
    {
        foreach (var card in deviceCards.Values)
        {
            if (card != null)
            {
                Destroy(card.gameObject);
            }
        }
        deviceCards.Clear();
    }
    
    // ═══════════════════════════════════════════════════════════
    // ACCIONES
    // ═══════════════════════════════════════════════════════════
    
    public void OnRefreshClicked()
    {
        SmartHomeClient.Instance?.GetDevices();
    }
    
    public void OnDisconnectClicked()
    {
        SmartHomeClient.Instance?.Disconnect();
    }
    
    public void OnAllOffClicked()
    {
        DeviceManager.Instance?.TurnOffAll();
    }
}
