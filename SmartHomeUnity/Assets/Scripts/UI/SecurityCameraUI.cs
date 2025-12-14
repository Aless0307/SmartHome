using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// UI para visualizar múltiples cámaras de seguridad
/// Muestra un panel con feeds en vivo de todas las cámaras
/// </summary>
public class SecurityCameraUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [Tooltip("Panel principal que contiene todo")]
    public GameObject securityPanel;
    
    [Tooltip("Contenedor para las tarjetas de cámaras (GridLayout)")]
    public Transform camerasContainer;
    
    [Tooltip("Prefab de tarjeta de cámara")]
    public GameObject cameraCardPrefab;
    
    [Tooltip("RawImage para vista ampliada de cámara seleccionada")]
    public RawImage fullscreenView;
    
    [Tooltip("Panel de vista ampliada")]
    public GameObject fullscreenPanel;
    
    [Tooltip("Texto con nombre de cámara en fullscreen")]
    public Text fullscreenCameraName;

    [Header("Botones")]
    public Button openPanelButton;
    public Button closePanelButton;
    public Button closeFullscreenButton;

    [Header("Auto-detectar")]
    [Tooltip("Buscar cámaras automáticamente en la escena")]
    public bool autoDetectCameras = true;

    // Lista de cámaras detectadas
    private List<SecurityCamera> securityCameras = new List<SecurityCamera>();
    
    // Diccionario de tarjetas UI por cámara
    private Dictionary<SecurityCamera, CameraCardUI> cameraCards = new Dictionary<SecurityCamera, CameraCardUI>();
    
    // Cámara actualmente seleccionada
    private SecurityCamera selectedCamera;

    void Start()
    {
        // Configurar botones
        if (openPanelButton != null)
            openPanelButton.onClick.AddListener(OpenSecurityPanel);
        
        if (closePanelButton != null)
            closePanelButton.onClick.AddListener(CloseSecurityPanel);
        
        if (closeFullscreenButton != null)
            closeFullscreenButton.onClick.AddListener(CloseFullscreen);

        // Iniciar oculto
        if (securityPanel != null)
            securityPanel.SetActive(false);
        
        if (fullscreenPanel != null)
            fullscreenPanel.SetActive(false);

        // Detectar cámaras
        if (autoDetectCameras)
        {
            Invoke("DetectAndCreateUI", 0.5f);
        }
    }

    /// <summary>
    /// Detectar cámaras en la escena y crear UI
    /// </summary>
    public void DetectAndCreateUI()
    {
        // Encontrar todas las SecurityCamera en la escena
        SecurityCamera[] cameras = FindObjectsOfType<SecurityCamera>();
        securityCameras.Clear();
        securityCameras.AddRange(cameras);

        Debug.Log($"📹 SecurityCameraUI: Detectadas {securityCameras.Count} cámaras");

        // Crear tarjetas UI
        CreateCameraCards();
    }

    /// <summary>
    /// Crear tarjetas UI para cada cámara
    /// </summary>
    private void CreateCameraCards()
    {
        // Limpiar tarjetas existentes
        foreach (Transform child in camerasContainer)
        {
            Destroy(child.gameObject);
        }
        cameraCards.Clear();

        // Crear tarjeta para cada cámara
        foreach (var cam in securityCameras)
        {
            if (cameraCardPrefab != null)
            {
                GameObject cardObj = Instantiate(cameraCardPrefab, camerasContainer);
                CameraCardUI card = cardObj.GetComponent<CameraCardUI>();
                
                if (card != null)
                {
                    card.Setup(cam, this);
                    cameraCards[cam] = card;
                }
            }
            else
            {
                // Crear tarjeta básica sin prefab
                CreateBasicCameraCard(cam);
            }
        }
    }

    /// <summary>
    /// Crear tarjeta básica si no hay prefab
    /// </summary>
    private void CreateBasicCameraCard(SecurityCamera cam)
    {
        // Crear contenedor
        GameObject cardObj = new GameObject($"CameraCard_{cam.cameraName}");
        cardObj.transform.SetParent(camerasContainer);
        
        RectTransform rect = cardObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 180);
        
        // Agregar componente básico
        CameraCardUI card = cardObj.AddComponent<CameraCardUI>();
        
        // Crear fondo
        Image bg = cardObj.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        
        // Crear RawImage para el feed
        GameObject feedObj = new GameObject("Feed");
        feedObj.transform.SetParent(cardObj.transform);
        RectTransform feedRect = feedObj.AddComponent<RectTransform>();
        feedRect.anchorMin = new Vector2(0.05f, 0.25f);
        feedRect.anchorMax = new Vector2(0.95f, 0.95f);
        feedRect.offsetMin = Vector2.zero;
        feedRect.offsetMax = Vector2.zero;
        RawImage feedImage = feedObj.AddComponent<RawImage>();
        
        // Crear texto nombre
        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(cardObj.transform);
        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0);
        nameRect.anchorMax = new Vector2(1, 0.2f);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;
        Text nameText = nameObj.AddComponent<Text>();
        nameText.text = cam.cameraName;
        nameText.alignment = TextAnchor.MiddleCenter;
        nameText.fontSize = 14;
        nameText.color = Color.white;
        nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Asignar referencias y configurar
        card.cameraFeed = feedImage;
        card.cameraNameText = nameText;
        card.Setup(cam, this);
        
        cameraCards[cam] = card;
    }

    /// <summary>
    /// Abrir panel de cámaras de seguridad
    /// </summary>
    public void OpenSecurityPanel()
    {
        if (securityPanel != null)
        {
            securityPanel.SetActive(true);
            RefreshAllFeeds();
        }
    }

    /// <summary>
    /// Cerrar panel de cámaras
    /// </summary>
    public void CloseSecurityPanel()
    {
        if (securityPanel != null)
        {
            securityPanel.SetActive(false);
        }
        CloseFullscreen();
    }

    /// <summary>
    /// Mostrar cámara en vista ampliada
    /// </summary>
    public void ShowFullscreen(SecurityCamera cam)
    {
        selectedCamera = cam;
        
        if (fullscreenPanel != null && fullscreenView != null && cam != null)
        {
            fullscreenPanel.SetActive(true);
            fullscreenView.texture = cam.GetRenderTexture();
            
            if (fullscreenCameraName != null)
            {
                fullscreenCameraName.text = $"{cam.cameraName} - {cam.location}";
            }
        }
    }

    /// <summary>
    /// Cerrar vista ampliada
    /// </summary>
    public void CloseFullscreen()
    {
        if (fullscreenPanel != null)
        {
            fullscreenPanel.SetActive(false);
        }
        selectedCamera = null;
    }

    /// <summary>
    /// Actualizar todos los feeds
    /// </summary>
    public void RefreshAllFeeds()
    {
        foreach (var kvp in cameraCards)
        {
            kvp.Value.UpdateFeed();
        }
    }

    /// <summary>
    /// Obtener cámara por ID
    /// </summary>
    public SecurityCamera GetCameraById(string id)
    {
        return securityCameras.Find(c => c.cameraId == id);
    }

    /// <summary>
    /// Obtener todas las cámaras
    /// </summary>
    public List<SecurityCamera> GetAllCameras()
    {
        return securityCameras;
    }

    /// <summary>
    /// Agregar cámara dinámicamente
    /// </summary>
    public void AddCamera(SecurityCamera cam)
    {
        if (!securityCameras.Contains(cam))
        {
            securityCameras.Add(cam);
            CreateCameraCards();
        }
    }

    void Update()
    {
        // Atajo de teclado para abrir/cerrar panel (C de Cameras)
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (securityPanel != null)
            {
                if (securityPanel.activeSelf)
                    CloseSecurityPanel();
                else
                    OpenSecurityPanel();
            }
        }

        // Escape para cerrar fullscreen
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (fullscreenPanel != null && fullscreenPanel.activeSelf)
                CloseFullscreen();
            else if (securityPanel != null && securityPanel.activeSelf)
                CloseSecurityPanel();
        }
    }
}
