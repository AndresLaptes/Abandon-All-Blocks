using UnityEngine;

public class TrampaHacha : MonoBehaviour
{
    [Header("Oscilación")]
    [Tooltip("Amplitud máxima del balanceo en grados respecto a la vertical.")]
    public float amplitudGrados = 45f;
    [Tooltip("Segundos por ciclo completo del péndulo.")]
    public float periodo = 1f;
    [Tooltip("Desfase inicial en segundos. Útil para evitar que hachas adyacentes oscilen sincronizadas.")]
    public float faseInicial = 0f;
    [Tooltip("Eje de rotación local del balanceo. (0,0,1) = oscila en plano XY (de izquierda a derecha en mundo).")]
    public Vector3 ejeRotacion = new Vector3(0f, 0f, 1f);

    [Header("Daño")]
    [Tooltip("Distancia (desde el eje del hacha hasta el filo) usada para detectar contacto con el player.")]
    public float longitudHacha = 1.5f;
    [Tooltip("Radio de contacto alrededor del segmento del hacha.")]
    public float radioDano = 0.7f;

    private Quaternion rotacionBase;
    private Transform player;
    private PlayerHealth playerHealth;
    private bool inicializada = false;
    private EnemySpawner enemySpawner;

    public void Inicializar(Transform jugador)
    {
        player = jugador;
        if (player != null) playerHealth = player.GetComponent<PlayerHealth>();
        enemySpawner = FindObjectOfType<EnemySpawner>();
        rotacionBase = transform.localRotation;
        inicializada = true;
    }

    void Update()
    {
        if (!inicializada) return;

        float t = Time.time + faseInicial;
        float angulo = amplitudGrados * Mathf.Sin(2f * Mathf.PI * t / periodo);
        transform.localRotation = rotacionBase * Quaternion.AngleAxis(angulo, ejeRotacion);

        if (playerHealth != null && player != null && PlayerEnContacto())
        {
            playerHealth.RecibirDano();
        }

        if (enemySpawner != null)
        {
            Vector3 puntaFilo = transform.position + transform.rotation * (Vector3.down * longitudHacha);
            Vector3 centroXZ = new Vector3((transform.position.x + puntaFilo.x) * 0.5f, 0f, (transform.position.z + puntaFilo.z) * 0.5f);
            enemySpawner.DaniarEnemigosEnArea(centroXZ, radioDano);
        }
    }

    private bool PlayerEnContacto()
    {
        Vector3 pivot = transform.position;
        Vector3 puntaFilo = pivot + transform.rotation * (Vector3.down * longitudHacha);

        Vector3 segDir = puntaFilo - pivot;
        float segLen = segDir.magnitude;
        if (segLen < 0.001f) return Vector3.Distance(player.position, pivot) < radioDano;

        Vector3 segNorm = segDir / segLen;
        Vector3 toPlayer = player.position - pivot;
        float proj = Mathf.Clamp(Vector3.Dot(toPlayer, segNorm), 0f, segLen);
        Vector3 closest = pivot + segNorm * proj;
        return Vector3.Distance(player.position, closest) < radioDano;
    }
}
