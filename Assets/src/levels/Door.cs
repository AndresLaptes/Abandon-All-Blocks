using UnityEngine;

public class Door : MonoBehaviour
{
    [Tooltip("Si está cerrada, no permite el paso aunque el jugador llegue.")]
    public bool estaAbierta = true;

    [Tooltip("Distancia (en plano XZ) al centro de la puerta para activar paso de sala.")]
    public float radioActivacion = 1.5f;

    private LevelManager levelManager;
    private Transform jugador;
    private bool yaUsada = false;

    void Awake()
    {
        levelManager = FindObjectOfType<LevelManager>();
        if (levelManager == null) { Debug.LogWarning("Door: no se ha encontrado LevelManager en la escena."); return; }
        if (levelManager.player == null) { Debug.LogWarning("Door: LevelManager.player no asignado en el inspector."); return; }
        jugador = levelManager.player.transform;
    }

    void Update()
    {
        if (yaUsada || !estaAbierta || jugador == null) return;

        Vector3 a = transform.position;
        Vector3 b = jugador.position;
        float distXZ = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        if (distXZ <= radioActivacion)
        {
            Debug.Log($"Door: jugador entró en rango ({distXZ:F2}). Llamando cargarSigueinteNivel()...");
            yaUsada = true;
            levelManager.cargarSigueinteNivel();
        }
    }

    public void Abrir() { estaAbierta = true; }
    public void Cerrar() { estaAbierta = false; }
}
