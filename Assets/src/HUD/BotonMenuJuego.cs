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
    public int tamanioFuente = 28;
    public Color colorTexto = new Color(0.6f, 0f, 0f, 1f);

    [Header("Posición / tamaño")]
    [Tooltip("(1,1) = esquina superior derecha. (0,1) = superior izquierda. (1,0) = inferior derecha.")]
    public Vector2 anchor = new Vector2(1f, 1f);
    public Vector2 offsetDesdeEsquina = new Vector2(-100f, -40f);
    public Vector2 tamano = new Vector2(160f, 60f);

    [Header("Fondo")]
    public Color colorFondo = new Color(0f, 0f, 0f, 0.55f);

    void Start()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) { Debug.LogWarning("BotonMenuJuego: no hay Canvas en la escena."); return; }

        GameObject go = new GameObject("BotonMenuJuego");
        go.transform.SetParent(canvas.transform, false);
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer >= 0) go.layer = uiLayer;

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = offsetDesdeEsquina;
        rt.sizeDelta = tamano;

        Image img = go.AddComponent<Image>();
        img.color = colorFondo;
        img.raycastTarget = true;

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        string escena = escenaMenu;
        btn.onClick.AddListener(() => SceneManager.LoadScene(escena));

        GameObject txtGO = new GameObject("Texto");
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
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = colorTexto;
        tmp.raycastTarget = false;
    }
}
