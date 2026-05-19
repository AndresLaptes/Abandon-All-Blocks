using System.Collections;
using UnityEngine;

public class Door : MonoBehaviour
{
    [Tooltip("Si está cerrada, no permite el paso aunque el jugador llegue.")]
    public bool estaAbierta = true;

    [Tooltip("Grados que gira el panel al abrirse.")]
    public float anguloApertura = 90f;

    [HideInInspector] public float blocSize = 2f;
    [HideInInspector] public int puertaCellX = 0;
    [HideInInspector] public int sizeLevel = 0;
    [HideInInspector] public GameObject panel;

    private LevelManager levelManager;
    private Transform jugador;
    private bool yaUsada = false;
    private bool yaAbriendo = false;
    private float tiempoApertura = 0.5f;

    void Awake()
    {
        levelManager = FindObjectOfType<LevelManager>();
        if (levelManager == null) { Debug.LogWarning("Door: no se ha encontrado LevelManager en la escena."); return; }
        if (levelManager.player == null) { Debug.LogWarning("Door: LevelManager.player no asignado en el inspector."); return; }
        jugador = levelManager.player.transform;
        GridMovement mov = levelManager.player.GetComponent<GridMovement>();
        if (mov != null) tiempoApertura = mov.TiempoMovimiento;
    }

    public void Configurar(float blocSize, int puertaCellX, int sizeLevel)
    {
        this.blocSize = blocSize;
        this.puertaCellX = puertaCellX;
        this.sizeLevel = sizeLevel;
    }

    public void IniciarApertura()
    {
        if (yaAbriendo || panel == null) return;
        yaAbriendo = true;
        StartCoroutine(AbrirPanel(panel.transform.localRotation));
    }

    void Update()
    {
        if (yaUsada || !estaAbierta || jugador == null || blocSize <= 0f) return;

        int playerGridX = Mathf.RoundToInt(jugador.position.x / blocSize);
        int playerGridZ = Mathf.RoundToInt(jugador.position.z / blocSize);

        if (playerGridX == puertaCellX && playerGridZ == sizeLevel)
        {
            Debug.Log($"Door: jugador en celda puerta ({playerGridX},{playerGridZ}). Llamando cargarSigueinteNivel()...");
            yaUsada = true;
            levelManager.cargarSigueinteNivel();
        }
    }

    private IEnumerator AbrirPanel(Quaternion rotInicial)
    {
        float t = 0f;
        while (t < tiempoApertura)
        {
            t += Time.deltaTime;
            float progreso = Mathf.Clamp01(t / tiempoApertura);
            float ease = 0.5f * (1f - Mathf.Cos(Mathf.PI * progreso));
            panel.transform.localRotation = rotInicial * Quaternion.Euler(0f, anguloApertura * ease, 0f);
            yield return null;
        }
    }

    public void Abrir() { estaAbierta = true; }
    public void Cerrar() { estaAbierta = false; }
}
