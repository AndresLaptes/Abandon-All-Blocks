using System.Collections;
using UnityEngine;

public class BreaTrail : MonoBehaviour
{
    [Header("Configuración del charco")]
    public float tiempoVida = 6f;     
    public float tiempoEncoger = 2f;  
    public float multiplicadorLentitud = 2f;

    private Vector3 escalaOriginal;

    void Start()
    {
        escalaOriginal = transform.localScale;
        StartCoroutine(CicloVida());
    }

    private IEnumerator CicloVida()
    {
        // 1. Se queda en el suelo molestando
        yield return new WaitForSeconds(tiempoVida);

        // 2. Se encoge progresivamente en todas las direcciones
        float t = 0;
        while (t < tiempoEncoger)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(escalaOriginal, Vector3.zero, t / tiempoEncoger);
            yield return null;
        }

        // 3. Se destruye
        Destroy(gameObject);
    }
}