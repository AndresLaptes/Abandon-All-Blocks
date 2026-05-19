using System.Collections;
using UnityEngine;

public class Moneda : MonoBehaviour
{
    public float velocidadRotacion = 90f;
    public float alturaFlote = 0.2f;
    public float velocidadFlote = 3f;

    public float alturaRecogida = 1.5f;
    public float tiempoAnimacionRecogida = 0.3f;

    private float yInicial;
    private bool recogida = false;
    private Collider miCollider;

    void Start()
    {
        yInicial = transform.position.y;
        miCollider = GetComponent<Collider>();
    }

    void Update()
    {
        if (recogida) return;

        transform.Rotate(Vector3.up * velocidadRotacion * Time.deltaTime, Space.World);

        float nuevoY = yInicial + (Mathf.Sin(Time.time * velocidadFlote) * alturaFlote);
        transform.position = new Vector3(transform.position.x, nuevoY, transform.position.z);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!recogida && other.CompareTag("Player"))
        {
            recogida = true;
            if (miCollider != null) miCollider.enabled = false;
            
            HUDContadorMonedas hud = FindObjectOfType<HUDContadorMonedas>();
            if (hud != null) hud.SumarMoneda(1);

            StartCoroutine(AnimacionRecogida());
        }
    }

    private IEnumerator AnimacionRecogida()
    {
        Vector3 posInicial = transform.position;
        Vector3 posAlta = posInicial + new Vector3(0, alturaRecogida, 0);
        Vector3 escalaInicial = transform.localScale;

        float mitadTiempo = tiempoAnimacionRecogida / 2f;
        float t = 0;

        while (t < mitadTiempo)
        {
            t += Time.deltaTime;
            float progreso = t / mitadTiempo;
            transform.position = Vector3.Lerp(posInicial, posAlta, progreso);
            transform.Rotate(Vector3.up * velocidadRotacion * 5f * Time.deltaTime, Space.World);
            yield return null;
        }

        t = 0;
        while (t < mitadTiempo)
        {
            t += Time.deltaTime;
            float progreso = t / mitadTiempo;
            transform.position = Vector3.Lerp(posAlta, posInicial, progreso);
            transform.localScale = Vector3.Lerp(escalaInicial, Vector3.zero, progreso);
            transform.Rotate(Vector3.up * velocidadRotacion * 5f * Time.deltaTime, Space.World);
            yield return null;
        }

        Destroy(gameObject);
    }
}