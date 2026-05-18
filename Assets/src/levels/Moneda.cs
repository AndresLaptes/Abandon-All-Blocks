using UnityEngine;

public class Moneda : MonoBehaviour
{
    [Header("Animación")]
    public float velocidadRotacion = 90f;
    public float alturaFlote = 0.2f;
    public float velocidadFlote = 3f;

    private float yInicial;

    void Start()
    {
        // Guardamos la altura original (techo del bloque) para flotar respecto a ella
        yInicial = transform.position.y;
    }

    void Update()
    {
        // Girar sobre su propio eje Y
        transform.Rotate(Vector3.up * velocidadRotacion * Time.deltaTime, Space.World);

        // Flotar arriba y abajo suavemente usando un Seno matemático
        float nuevoY = yInicial + (Mathf.Sin(Time.time * velocidadFlote) * alturaFlote);
        transform.position = new Vector3(transform.position.x, nuevoY, transform.position.z);
    }

    // Esta función salta cuando el jugador "choca" físicamente con el área de la moneda
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // TODO: Sumarlo a tu inventario o marcador
            Debug.Log("¡Has recogido una moneda!");
            Destroy(gameObject);
        }
    }
}