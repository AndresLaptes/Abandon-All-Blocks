using System.Collections;
using UnityEngine;

public class TrampaPinchos : MonoBehaviour
{
    [Header("Animación")]
    [Tooltip("Y local de los pinchos en reposo (escondidos bajo el suelo).")]
    public float yReposo = -0.5f;
    [Tooltip("Y local de los pinchos al activarse (sobresalen del suelo).")]
    public float yActivo = 0.5f;

    public float tiempoSubida = 0.15f;
    public float tiempoArriba = 1f;
    public float tiempoBajada = 0.3f;
    public float tiempoRearmado = 0.5f;

    [Header("Detección")]
    [Tooltip("Distancia al player (XZ) para activar la trampa.")]
    public float radioActivacion = 1.0f;

    [Header("Replicación")]
    [Tooltip("Si true, clona el primer hijo del prefab hasta tener numPinchos copias usando patronOffsets.")]
    public bool autoReplicar = true;
    public int numPinchos = 5;
    [Tooltip("Posiciones XZ locales para cada pincho relativas al centro de la celda. Se reparten cíclicamente si hay menos que numPinchos.")]
    public Vector2[] patronOffsets = new Vector2[]
    {
        new Vector2(0f, 0f),
        new Vector2(-0.5f, -0.5f),
        new Vector2(0.5f, -0.5f),
        new Vector2(-0.5f, 0.5f),
        new Vector2(0.5f, 0.5f)
    };

    private Transform[] pinchos;
    private bool armada = true;
    private Transform player;
    private PlayerHealth playerHealth;
    private bool inicializada = false;

    public void Inicializar(Transform jugador)
    {
        player = jugador;
        if (player != null) playerHealth = player.GetComponent<PlayerHealth>();

        if (autoReplicar && transform.childCount > 0 && numPinchos > 0 && patronOffsets != null && patronOffsets.Length > 0)
        {
            Transform plantilla = transform.GetChild(0);

            Vector2 off0 = patronOffsets[0 % patronOffsets.Length];
            plantilla.localPosition = new Vector3(off0.x, plantilla.localPosition.y, off0.y);

            int existentes = transform.childCount;
            for (int i = existentes; i < numPinchos; i++)
            {
                GameObject copia = Instantiate(plantilla.gameObject, transform);
                Vector2 off = patronOffsets[i % patronOffsets.Length];
                copia.transform.localPosition = new Vector3(off.x, plantilla.localPosition.y, off.y);
                copia.transform.localRotation = plantilla.localRotation;
                copia.transform.localScale = plantilla.localScale;
            }

            for (int i = 1; i < Mathf.Min(transform.childCount, numPinchos); i++)
            {
                Vector2 off = patronOffsets[i % patronOffsets.Length];
                Transform c = transform.GetChild(i);
                c.localPosition = new Vector3(off.x, c.localPosition.y, off.y);
            }
        }

        int count = transform.childCount;
        pinchos = new Transform[count];
        for (int i = 0; i < count; i++)
        {
            pinchos[i] = transform.GetChild(i);
            Vector3 p = pinchos[i].localPosition;
            p.y = yReposo;
            pinchos[i].localPosition = p;
        }
        inicializada = true;
    }

    void Update()
    {
        if (!inicializada || !armada || player == null) return;

        Vector3 a = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 b = new Vector3(player.position.x, 0f, player.position.z);
        if (Vector3.Distance(a, b) < radioActivacion)
        {
            armada = false;
            StartCoroutine(CicloActivacion());
        }
    }

    private IEnumerator CicloActivacion()
    {
        yield return MoverPinchos(yReposo, yActivo, tiempoSubida);

        if (playerHealth != null && player != null)
        {
            Vector3 a = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 b = new Vector3(player.position.x, 0f, player.position.z);
            if (Vector3.Distance(a, b) < radioActivacion)
                playerHealth.RecibirDano();
        }

        yield return new WaitForSeconds(tiempoArriba);
        yield return MoverPinchos(yActivo, yReposo, tiempoBajada);
        yield return new WaitForSeconds(tiempoRearmado);
        armada = true;
    }

    private IEnumerator MoverPinchos(float yDesde, float yHasta, float duracion)
    {
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duracion);
            float y = Mathf.Lerp(yDesde, yHasta, k);
            for (int i = 0; i < pinchos.Length; i++)
            {
                if (pinchos[i] == null) continue;
                Vector3 p = pinchos[i].localPosition;
                p.y = y;
                pinchos[i].localPosition = p;
            }
            yield return null;
        }
    }
}
