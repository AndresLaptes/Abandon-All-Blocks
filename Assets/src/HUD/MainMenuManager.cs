using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para cambiar de escena

public class MainMenuManager : MonoBehaviour
{
    [Header("Paneles de la UI")]
    [Tooltip("Arrastra aquí el objeto que contiene el Título y los botones de Play, Créditos, Salir.")]
    public GameObject panelPrincipal;
    
    [Tooltip("Arrastra aquí el panel negro/oscuro de los créditos que empieza oculto.")]
    public GameObject panelCreditos;

    void Start()
    {
        // Al empezar, nos aseguramos de que el menú principal se vea y los créditos estén ocultos
        if (panelPrincipal != null) panelPrincipal.SetActive(true);
        if (panelCreditos != null) panelCreditos.SetActive(false);
    }

    public void Jugar()
    {
        SceneManager.LoadScene(1);
    }

    public void MostrarCreditos()
    {
        if (panelPrincipal != null) panelPrincipal.SetActive(false);
        if (panelCreditos != null) panelCreditos.SetActive(true);
    }

    public void VolverAlMenu()
    {
        if (panelPrincipal != null) panelPrincipal.SetActive(true);
        if (panelCreditos != null) panelCreditos.SetActive(false);
    }

    public void Salir()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}