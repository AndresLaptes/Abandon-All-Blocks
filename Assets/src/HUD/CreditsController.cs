using UnityEngine;
using UnityEngine.SceneManagement;

public class CreditsController : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    [Tooltip("Arrastra aquí tu panel ContenedorTexto")]
    public RectTransform contenedorTexto; 
    
    [Tooltip("Velocidad a la que sube el texto")]
    public float velocidadScroll = 40f;
    
    [Tooltip("Posición Y en la que el texto se detiene")]
    public float puntoFinalY = 3200f; 

    [Header("Interfaz")]
    [Tooltip("Arrastra aquí tu botón Btn_Volver")]
    public GameObject botonVolver;

    [Header("Navegación")]
    [Tooltip("Nombre exacto de la escena de tu menú principal")]
    public string escenaMenu = "MenuPrincipal";

    private bool haTerminado = false;

    void Start()
    {
        if (botonVolver != null)
        {
            botonVolver.SetActive(false);
        }
    }

    void Update()
    {
        if (haTerminado) return;

        if (contenedorTexto != null)
        {
            contenedorTexto.anchoredPosition += Vector2.up * velocidadScroll * Time.deltaTime;

            if (contenedorTexto.anchoredPosition.y >= puntoFinalY)
            {
                haTerminado = true; 
                
                if (botonVolver != null)
                {
                    botonVolver.SetActive(true);
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape))
        {
            haTerminado = true;
            CargarMenu();
        }
    }

    public void CargarMenu()
    {
        if (AudioManager.instance != null && AudioManager.instance.musicaFondo != null)
            AudioManager.instance.CambiarMusica(AudioManager.instance.musicaFondo);
        SceneManager.LoadScene(escenaMenu);
    }
}