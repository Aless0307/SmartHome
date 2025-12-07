using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Control de bocina inteligente (Echo Dot) para Smart Home
/// Reproduce audio 3D espacial que se atenúa con la distancia
/// </summary>
public class SmartSpeaker : MonoBehaviour
{
    [Header("Configuración de Audio")]
    [Tooltip("Lista de canciones/audios disponibles")]
    public AudioClip[] playlist;
    
    [Tooltip("Nombres para mostrar en la GUI (debe coincidir con playlist)")]
    public string[] trackNames;
    
    [Header("Audio 3D")]
    [Range(0f, 1f)]
    public float volume = 0.8f;
    
    [Tooltip("Distancia mínima donde el sonido es 100%")]
    public float minDistance = 1f;
    
    [Tooltip("Distancia máxima donde el sonido deja de escucharse")]
    public float maxDistance = 15f;
    
    [Header("Estado")]
    public bool isPlaying = false;
    public int currentTrackIndex = 0;
    
    [Header("Visualización (Opcional)")]
    [Tooltip("Material emisivo que brilla cuando reproduce")]
    public Renderer ledRenderer;
    public Color playingColor = new Color(0.2f, 0.8f, 1f); // Cyan Alexa
    public Color stoppedColor = Color.black;
    
    // Componentes
    private AudioSource audioSource;
    
    void Awake()
    {
        // Crear o obtener AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Configurar audio 3D espacial
        ConfigureAudioSource();
    }
    
    void Start()
    {
        // Verificar playlist
        if (playlist == null || playlist.Length == 0)
        {
            Debug.LogWarning($"🔊 {gameObject.name}: No hay canciones en la playlist");
        }
        else
        {
            Debug.Log($"🔊 {gameObject.name}: {playlist.Length} canciones cargadas");
            
            // Cargar primera canción
            if (playlist[0] != null)
            {
                audioSource.clip = playlist[0];
            }
        }
        
        UpdateVisuals();
    }
    
    /// <summary>
    /// Configurar AudioSource para audio 3D
    /// </summary>
    private void ConfigureAudioSource()
    {
        audioSource.playOnAwake = false;
        audioSource.loop = true; // Repetir canción
        audioSource.volume = volume;
        
        // Audio 3D espacial
        // 0 = 2D (se escucha igual en todos lados)
        // 1 = 3D completo (depende de distancia)
        audioSource.spatialBlend = 0f; // TEMPORAL: 2D para probar que funciona
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.dopplerLevel = 0f;
        
        Debug.Log($"🔊 {gameObject.name}: Audio configurado (spatialBlend=0 para prueba)");
    }
    
    // ═══════════════════════════════════════════════════════════
    // MÉTODOS PARA SMART HOME SERVER
    // ═══════════════════════════════════════════════════════════
    
    /// <summary>
    /// Reproducir
    /// </summary>
    public void Play()
    {
        if (audioSource.clip != null)
        {
            audioSource.Play();
            isPlaying = true;
            UpdateVisuals();
            Debug.Log($"🔊 {gameObject.name}: ▶️ Reproduciendo - {GetCurrentTrackName()}");
        }
        else
        {
            Debug.LogWarning($"🔊 {gameObject.name}: No hay canción seleccionada");
        }
    }
    
    /// <summary>
    /// Pausar
    /// </summary>
    public void Pause()
    {
        audioSource.Pause();
        isPlaying = false;
        UpdateVisuals();
        Debug.Log($"🔊 {gameObject.name}: ⏸️ Pausado");
    }
    
    /// <summary>
    /// Detener
    /// </summary>
    public void Stop()
    {
        audioSource.Stop();
        isPlaying = false;
        UpdateVisuals();
        Debug.Log($"🔊 {gameObject.name}: ⏹️ Detenido");
    }
    
    /// <summary>
    /// Toggle Play/Pause
    /// </summary>
    public void Toggle()
    {
        if (isPlaying)
            Pause();
        else
            Play();
    }
    
    /// <summary>
    /// Siguiente canción
    /// </summary>
    public void NextTrack()
    {
        if (playlist == null || playlist.Length == 0) return;
        
        currentTrackIndex = (currentTrackIndex + 1) % playlist.Length;
        LoadTrack(currentTrackIndex);
        
        if (isPlaying)
            Play();
    }
    
    /// <summary>
    /// Canción anterior
    /// </summary>
    public void PreviousTrack()
    {
        if (playlist == null || playlist.Length == 0) return;
        
        currentTrackIndex--;
        if (currentTrackIndex < 0) currentTrackIndex = playlist.Length - 1;
        LoadTrack(currentTrackIndex);
        
        if (isPlaying)
            Play();
    }
    
    /// <summary>
    /// Cargar canción por índice
    /// </summary>
    public void LoadTrack(int index)
    {
        if (playlist == null || index < 0 || index >= playlist.Length) return;
        
        bool wasPlaying = isPlaying;
        if (wasPlaying) audioSource.Stop();
        
        currentTrackIndex = index;
        audioSource.clip = playlist[index];
        
        Debug.Log($"🔊 {gameObject.name}: 🎵 Cargada: {GetCurrentTrackName()}");
        
        if (wasPlaying) Play();
    }
    
    /// <summary>
    /// Establecer volumen (0-100)
    /// </summary>
    public void SetVolume(int volumePercent)
    {
        volume = Mathf.Clamp01(volumePercent / 100f);
        audioSource.volume = volume;
        Debug.Log($"🔊 {gameObject.name}: 🔉 Volumen = {volumePercent}%");
    }
    
    /// <summary>
    /// Obtener nombre de canción actual
    /// </summary>
    public string GetCurrentTrackName()
    {
        if (trackNames != null && currentTrackIndex < trackNames.Length && !string.IsNullOrEmpty(trackNames[currentTrackIndex]))
        {
            return trackNames[currentTrackIndex];
        }
        
        if (playlist != null && currentTrackIndex < playlist.Length && playlist[currentTrackIndex] != null)
        {
            return playlist[currentTrackIndex].name;
        }
        
        return "Sin canción";
    }
    
    /// <summary>
    /// Obtener lista de canciones
    /// </summary>
    public string[] GetTrackList()
    {
        if (trackNames != null && trackNames.Length > 0)
            return trackNames;
        
        if (playlist != null)
        {
            string[] names = new string[playlist.Length];
            for (int i = 0; i < playlist.Length; i++)
            {
                names[i] = playlist[i] != null ? playlist[i].name : "???";
            }
            return names;
        }
        
        return new string[0];
    }
    
    // ═══════════════════════════════════════════════════════════
    // MÉTODOS PARA DEVICE BRIDGE (SendMessage)
    // ═══════════════════════════════════════════════════════════
    
    public void TurnOn()
    {
        Play();
    }
    
    public void TurnOff()
    {
        Stop();
    }
    
    public void OnSmartHomeToggle()
    {
        Toggle();
    }
    
    /// <summary>
    /// Recibir valor del servidor (volumen 0-100)
    /// </summary>
    public void OnSmartHomeValue(string value)
    {
        if (int.TryParse(value, out int vol))
        {
            SetVolume(vol);
        }
    }
    
    /// <summary>
    /// Recibir comando especial del servidor
    /// </summary>
    public void OnSmartHomeCommand(string command)
    {
        Debug.Log($"🔊 {gameObject.name}: Comando recibido: {command}");
        
        switch (command.ToUpper())
        {
            case "PLAY":
                Play();
                break;
            case "PAUSE":
                Pause();
                break;
            case "STOP":
                Stop();
                break;
            case "NEXT":
                NextTrack();
                break;
            case "PREV":
            case "PREVIOUS":
                PreviousTrack();
                break;
            default:
                // Verificar si es un número (índice de canción)
                if (int.TryParse(command, out int trackIndex))
                {
                    LoadTrack(trackIndex);
                }
                break;
        }
    }
    
    /// <summary>
    /// Actualizar visuales (LED del Echo Dot)
    /// </summary>
    private void UpdateVisuals()
    {
        if (ledRenderer != null)
        {
            foreach (var mat in ledRenderer.materials)
            {
                if (mat.HasProperty("_EmissionColor"))
                {
                    Color emission = isPlaying ? playingColor : stoppedColor;
                    mat.SetColor("_EmissionColor", emission);
                    mat.EnableKeyword("_EMISSION");
                }
            }
        }
    }
    
    // ═══════════════════════════════════════════════════════════
    // DEBUG
    // ═══════════════════════════════════════════════════════════
    
    void OnDrawGizmosSelected()
    {
        // Dibujar rango de audio en el editor
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, minDistance);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxDistance);
    }
}
