using UnityEngine;
using System.Collections; // Necesario para las Corrutinas (el fade)

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Reproductores (Audio Sources)")]
    public AudioSource reproductorMusica;
    public AudioSource reproductorEfectos;

    [Header("Configuración de Música")]
    [Tooltip("Volumen máximo de la música de fondo (0.0 a 1.0)")]
    [Range(0f, 1f)] public float volumenMaximoMusica = 0.5f; 
    [Tooltip("Tiempo en segundos que tarda en desvanecerse la música")]
    public float tiempoFade = 1.5f;

    [Header("Música Inicial (Menú)")]
    public AudioClip musicaFondo;

    [Header("Efectos UI")]
    public AudioClip sfxBotonHover;
    public AudioClip sfxBotonClick;

    [Header("Efectos Jugador")]
    public AudioClip sfxAtaque;
    public AudioClip sfxDefensa;
    public AudioClip sfxRecibirDano;
    public AudioClip sfxMuerte;

    [Header("Efectos Enemigos")]
    public AudioClip sfxEnemigoAtaque;
    public AudioClip sfxEnemigoMuerte;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (musicaFondo != null)
        {
            reproductorMusica.volume = volumenMaximoMusica; // Aplicamos el volumen elegido
            reproductorMusica.clip = musicaFondo;
            reproductorMusica.loop = true;
            reproductorMusica.Play();
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null)
        {
            // Los efectos mantienen su propio volumen, ajenos a la música
            reproductorEfectos.PlayOneShot(clip);
        }
    }
    
    public void CambiarMusica(AudioClip nuevaCancion)
    {
        if (nuevaCancion == null) return;

        if (reproductorMusica.clip == nuevaCancion && reproductorMusica.isPlaying)
        {
            return;
        }

        StopAllCoroutines(); 
        
        StartCoroutine(RutinaFadeMusica(nuevaCancion));
    }

    private IEnumerator RutinaFadeMusica(AudioClip nuevaCancion)
    {
        if (reproductorMusica.isPlaying)
        {
            float volumenActual = reproductorMusica.volume;
            float t = 0f;

            while (t < tiempoFade)
            {
                t += Time.deltaTime;
                reproductorMusica.volume = Mathf.Lerp(volumenActual, 0f, t / tiempoFade);
                yield return null;
            }
        }

        reproductorMusica.Stop();
        reproductorMusica.clip = nuevaCancion;
        reproductorMusica.Play();

        float tiempoIn = 0f;
        while (tiempoIn < tiempoFade)
        {
            tiempoIn += Time.deltaTime;
            reproductorMusica.volume = Mathf.Lerp(0f, volumenMaximoMusica, tiempoIn / tiempoFade);
            yield return null;
        }

        reproductorMusica.volume = volumenMaximoMusica;
    }

   
    public void CambiarVolumenMusica(float nuevoVolumen)
    {
        volumenMaximoMusica = Mathf.Clamp01(nuevoVolumen);
        reproductorMusica.volume = volumenMaximoMusica;
    }
}