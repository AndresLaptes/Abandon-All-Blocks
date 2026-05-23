using UnityEngine;
using UnityEngine.EventSystems; // Obligatorio para detectar eventos del ratón en Canvas
using TMPro; // Para controlar el color del texto si usas TextMeshPro

public class BotonEfectos : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Configuración de Tamaño")]
    public float escalaHover = 1.1f;      
    public float escalaClick = 0.9f;       
    public float velocidadTransicion = 12f;

    [Header("Configuración de Color")]
    public Color colorNormal = Color.white;
    public Color colorHover = Color.yellow;

    private Vector3 escalaOriginal;
    private Vector3 escalaObjetivo;
    private TextMeshProUGUI textoBoton;
    private Color colorObjetivo;

    void Start()
    {
        escalaOriginal = transform.localScale;
        escalaObjetivo = escalaOriginal;

        textoBoton = GetComponentInChildren<TextMeshProUGUI>();
        if (textoBoton != null)
        {
            textoBoton.color = colorNormal;
            colorObjetivo = colorNormal;
        }
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, escalaObjetivo, velocidadTransicion * Time.deltaTime);
        
        if (textoBoton != null)
        {
            textoBoton.color = Color.Lerp(textoBoton.color, colorObjetivo, velocidadTransicion * Time.deltaTime);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        escalaObjetivo = escalaOriginal * escalaHover;
        colorObjetivo = colorHover;
        AudioManager.instance.PlaySFX(AudioManager.instance.sfxBotonHover);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        escalaObjetivo = escalaOriginal;
        colorObjetivo = colorNormal;
        AudioManager.instance.PlaySFX(AudioManager.instance.sfxBotonClick);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        escalaObjetivo = escalaOriginal * escalaClick;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, Input.mousePosition, eventData.pressEventCamera))
        {
            escalaObjetivo = escalaOriginal * escalaHover;
        }
        else
        {
            escalaObjetivo = escalaOriginal;
        }
    }
}