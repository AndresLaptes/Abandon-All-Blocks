using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Paneles de la UI")]
    [Tooltip("Arrastra aquí tu contenedor de botones principales")]
    public GameObject panelPrincipal;

    void Start()
    {
        if (panelPrincipal != null) panelPrincipal.SetActive(true);
    }

    public void Jugar()
    {
        SceneManager.LoadScene("SampleScene"); 
    }

    public void MostrarCreditos()
    {

        SceneManager.LoadScene("Credits"); 
    }

    public void Salir()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}