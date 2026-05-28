using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [Header("UI Game Over")]
    public GameObject panelGameOver;
    public TextMeshProUGUI textoTemporizador;

    private bool isGameOver = false;
    private float tiempoRestante = 10f;

    void Start()
    {
        if (panelGameOver != null) panelGameOver.SetActive(false);
    }

    void Update()
    {
        if (isGameOver)
        {
            tiempoRestante -= Time.deltaTime;

            if (textoTemporizador != null)
            {
                textoTemporizador.text = Mathf.CeilToInt(tiempoRestante).ToString();
            }

            if (tiempoRestante <= 0 || Input.GetKeyDown(KeyCode.Return))
            {
                VolverAlMenu();
            }
        }
    }

    public void MostrarGameOver()
    {
        isGameOver = true;
        tiempoRestante = 10f; 
        if (panelGameOver != null) panelGameOver.SetActive(true);
    }

    public void VolverAlMenu()
    {
        if (AudioManager.instance != null && AudioManager.instance.musicaFondo != null)
            AudioManager.instance.CambiarMusica(AudioManager.instance.musicaFondo);
        SceneManager.LoadScene("MenuPrincipal");
    }
}