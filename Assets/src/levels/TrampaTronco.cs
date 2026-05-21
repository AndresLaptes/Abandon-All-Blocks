using UnityEngine;

public class TrampaTronco : MonoBehaviour
{
    [Header("Rotación")]
    public Vector3 ejeRotacion = new Vector3(0f, 0f, 1f);
    public float velocidadAngular = 360f;

    [Header("Movimiento")]
    public Vector3 direccionMovimiento = new Vector3(-1f, 0f, 0f);
    public float velocidadLineal = 4f;

    [Header("Caída")]
    public float gravedad = 18f;
    public float yDestruccion = -10f;

    [Header("Daño")]
    [Tooltip("Distancia al player (XZ) a la que el tronco lo golpea.")]
    public float radioDano = 1.2f;

    [HideInInspector] public bool activo = false;
    private bool cayendo = false;
    private float velocidadVertical = 0f;
    private float xLimite;
    private Transform player;
    private PlayerHealth playerHealth;

    public void Inicializar(float blocSize, Transform jugador)
    {
        if (direccionMovimiento.x < 0f) xLimite = -(2.5f * blocSize);
        else if (direccionMovimiento.x > 0f) xLimite = (2.5f * blocSize);
        else xLimite = float.NegativeInfinity;

        player = jugador;
        if (player != null) playerHealth = player.GetComponent<PlayerHealth>();
    }

    void Update()
    {
        if (!activo) return;

        transform.Rotate(ejeRotacion * velocidadAngular * Time.deltaTime, Space.World);
        transform.position += direccionMovimiento.normalized * velocidadLineal * Time.deltaTime;

        if (!cayendo)
        {
            bool fuera = (direccionMovimiento.x < 0f && transform.position.x < xLimite) ||
                         (direccionMovimiento.x > 0f && transform.position.x > xLimite);
            if (fuera) cayendo = true;
        }
        else
        {
            velocidadVertical -= gravedad * Time.deltaTime;
            transform.position += Vector3.up * velocidadVertical * Time.deltaTime;
            if (transform.position.y < yDestruccion)
            {
                Destroy(gameObject);
                return;
            }
        }

        if (!cayendo && playerHealth != null && player != null)
        {
            Vector3 a = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 b = new Vector3(player.position.x, 0f, player.position.z);
            if (Vector3.Distance(a, b) < radioDano)
            {
                playerHealth.RecibirDano();
            }
        }
    }
}
