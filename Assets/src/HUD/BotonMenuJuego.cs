using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BotonMenuJuego : MonoBehaviour
{
    [Header("Destino")]
    public string escenaMenu = "MenuPrincipal";

    [Header("Texto")]
    public string textoBoton = "MENU";
    public int tamanioFuente = 24;

    [Header("Posición / tamaño")]
    [Tooltip("(1,1) = esquina superior derecha. (0,1) = superior izquierda.")]
    public Vector2 anchor = new Vector2(1f, 1f);
    public Vector2 offsetDesdeEsquina = new Vector2(-90f, -60f);
    public Vector2 tamano = new Vector2(140f, 40f);
    public Vector2 escala = new Vector2(1.2f, 1.2f);

    [Header("Colores (mismo estilo que MenuPrincipal)")]
    public Color colorNormal = new Color(0f, 0f, 0f, 1f);
    public Color colorHover = new Color(0.6f, 0f, 0f, 1f);
    public float escalaHover = 1.1f;
    public float escalaClick = 0.9f;
    public float velocidadTransicion = 12f;

    void Start()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) { Debug.LogWarning("BotonMenuJuego: no hay Canvas en la escena."); return; }

        int uiLayer = LayerMask.NameToLayer("UI");

        GameObject go = new GameObject("BotonMenuJuego");
        go.transform.SetParent(canvas.transform, false);
        if (uiLayer >= 0) go.layer = uiLayer;

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = offsetDesdeEsquina;
        rt.sizeDelta = tamano;
        rt.localScale = new Vector3(escala.x, escala.y, 1f);

        Image img = go.AddComponent<Image>();
        img.enabled = false;

        GameObject txtGO = new GameObject("Text (TMP)");
        txtGO.transform.SetParent(go.transform, false);
        if (uiLayer >= 0) txtGO.layer = uiLayer;

        RectTransform txtRT = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero;
        txtRT.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = textoBoton;
        tmp.fontSize = tamanioFuente;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = colorNormal;
        tmp.raycastTarget = true;

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = tmp;
        string escena = escenaMenu;
        btn.onClick.AddListener(() =>
        {
            if (AudioManager.instance != null && AudioManager.instance.musicaFondo != null)
                AudioManager.instance.CambiarMusica(AudioManager.instance.musicaFondo);
            SceneManager.LoadScene(escena);
        });

        BotonEfectos efectos = go.AddComponent<BotonEfectos>();
        efectos.escalaHover = escalaHover;
        efectos.escalaClick = escalaClick;
        efectos.velocidadTransicion = velocidadTransicion;
        efectos.colorNormal = colorNormal;
        efectos.colorHover = colorHover;
    }
}
