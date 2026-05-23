using UnityEngine;
using UnityEngine.EventSystems; // Obligatorio para detectar eventos del ratón en Canvas
using TMPro; // Para controlar el color del texto si usas TextMeshPro

public class BotonEfectos : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Configuración de Tamaño")]
    public float escalaHover = 1.1f;       // Tamaño al pasar el ratón por encima
    public float escalaClick = 0.9f;       // Tamaño al pulsar (efecto hundido)
    public float velocidadTransicion = 12f; // Rapidez del efecto visual

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

        // Buscamos si el botón tiene un texto de TextMeshPro dentro
        textoBoton = GetComponentInChildren<TextMeshProUGUI>();
        if (textoBoton != null)
        {
            textoBoton.color = colorNormal;
            colorObjetivo = colorNormal;
        }
    }

    void Update()
    {
        // Interpolación suave (Lerp) para que el movimiento y el color sean fluidos
        transform.localScale = Vector3.Lerp(transform.localScale, escalaObjetivo, velocidadTransicion * Time.deltaTime);
        
        if (textoBoton != null)
        {
            textoBoton.color = Color.Lerp(textoBoton.color, colorObjetivo, velocidadTransicion * Time.deltaTime);
        }
    }

    // El ratón entra al botón (Hover)
    public void OnPointerEnter(PointerEventData eventData)
    {
        escalaObjetivo = escalaOriginal * escalaHover;
        colorObjetivo = colorHover;
    }

    // El ratón sale del botón
    public void OnPointerExit(PointerEventData eventData)
    {
        escalaObjetivo = escalaOriginal;
        colorObjetivo = colorNormal;
    }

    // Haces clic (Mantienes pulsado)
    public void OnPointerDown(PointerEventData eventData)
    {
        escalaObjetivo = escalaOriginal * escalaClick;
    }

    // Sueltas el clic
    public void OnPointerUp(PointerEventData eventData)
    {
        // Si el ratón sigue dentro del botón al soltar, vuelve al tamaño de Hover
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