using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    public float tiempoDeViajePorCasilla = 0.4f;
    public float tiempoEsperaMinimo = 0.5f;
    public float tiempoEsperaMaximo = 1.5f;

    [Header("Configuración de Ataque")]
    [Tooltip("Distancia que retrocede para tomar impulso")]
    public float distanciaRetroceso = 0.5f; 
    public float tiempoAnticipacion = 0.4f; // Cuánto tarda en cargar hacia atrás
    public float tiempoDash = 0.1f;         // Velocidad del golpe
    public float tiempoRecuperacion = 0.2f; // Cuánto tarda en volver a su casilla original

    [Header("Herramientas")]
    public bool modoDebug = false;

    private float blocSize;
    private LevelManager levelManager;
    private Transform player;
    private EnemySpawner spawner;

    private bool isMoving = false;
    private bool isAttacking = false;

    public void Inicializar(float tamanoBloque, LevelManager manager, Transform jugador, EnemySpawner spawnerRef)
    {
        blocSize = tamanoBloque;
        levelManager = manager;
        player = jugador;
        spawner = spawnerRef;

        StartCoroutine(RutinaDeIA());
    }

    private IEnumerator RutinaDeIA()
    {
        yield return new WaitForSeconds(Random.Range(0.1f, 1f));

        while (true)
        {
            float tiempoEspera = Random.Range(tiempoEsperaMinimo, tiempoEsperaMaximo);
            yield return new WaitForSeconds(tiempoEspera);

            if (!isMoving && !isAttacking && player != null)
            {
                // Aplanamos las posiciones (ignorando la Y) para calcular la distancia en cuadrícula
                Vector3 miPosPlana = new Vector3(transform.position.x, 0, transform.position.z);
                Vector3 playerPosPlana = new Vector3(player.position.x, 0, player.position.z);
                
                // Si el jugador está justo en la casilla de al lado (aprox 1 bloque de distancia)
                if (Vector3.Distance(miPosPlana, playerPosPlana) < (blocSize * 1.2f))
                {
                    // ¡INICIAR SECUENCIA DE ATAQUE!
                    yield return StartCoroutine(RealizarAtaque(player.position));
                }
                else
                {
                    // Si el jugador está lejos, pensar cómo acercarse
                    Vector3 mejorCasilla = CalcularMejorCasilla();
                    
                    if (Vector3.Distance(transform.position, mejorCasilla) > 0.1f)
                    {
                        yield return StartCoroutine(MoverA(mejorCasilla));
                    }
                }
            }
        }
    }

    private IEnumerator RealizarAtaque(Vector3 posJugadorDestino)
    {
        isAttacking = true;

        Vector3 posInicial = transform.position;
        Vector3 direccionAtaque = (posJugadorDestino - posInicial).normalized;
        direccionAtaque.y = 0; // Evitamos que mire hacia el cielo o el suelo

        // Mirar fijamente al jugador antes de atacar
        if (direccionAtaque != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direccionAtaque);
        }

        // --- 1. WIND-UP (Anticipación visual) ---
        // Se echa un poco hacia atrás para "tomar impulso"
        Vector3 posRetroceso = posInicial - (direccionAtaque * distanciaRetroceso);
        float t = 0;
        while (t < tiempoAnticipacion)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(posInicial, posRetroceso, t / tiempoAnticipacion);
            yield return null;
        }

        // --- 2. DASH (El golpe) ---
        // Avanza rapidísimo hacia adelante
        Vector3 posImpacto = posInicial + (direccionAtaque * (blocSize * 0.8f)); // 0.8f para no fusionarse en el centro del jugador
        t = 0;
        while (t < tiempoDash)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(posRetroceso, posImpacto, t / tiempoDash);
            yield return null;
        }

        // --- 3. TRIGGER DEL DAÑO ---
        // Al terminar el Dash, comprobamos si el jugador sigue cerca.
        // Si el jugador se ha movido rápido y la esquivó, ¡el ataque fallará! (Gran jugabilidad)
        Vector3 posPlanaImpacto = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 posPlanaJugador = new Vector3(player.position.x, 0, player.position.z);

        if (Vector3.Distance(posPlanaImpacto, posPlanaJugador) < (blocSize * 1.1f))
        {
            PlayerHealth salud = player.GetComponent<PlayerHealth>();
            if (salud != null) salud.RecibirDano();
        }

        // --- 4. RECUPERACIÓN ---
        // El enemigo rebota/vuelve a su casilla original
        t = 0;
        while (t < tiempoRecuperacion)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(posImpacto, posInicial, t / tiempoRecuperacion);
            yield return null;
        }

        transform.position = posInicial;
        isAttacking = false;
    }

    private Vector3 CalcularMejorCasilla()
    {
        Vector3[] direcciones = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        
        Vector3 posicionIdeal = transform.position;
        float mejorPuntuacion = float.MaxValue; 

        for (int i = 0; i < direcciones.Length; i++)
        {
            Vector3 destinoPrueba = new Vector3(
                transform.position.x + (direcciones[i].x * blocSize),
                transform.position.y,
                transform.position.z + (direcciones[i].z * blocSize)
            );

            int costoFloodField = levelManager.ObtenerCostoFloodField(destinoPrueba);
            
            if (costoFloodField == 255) continue;
            if (spawner.HayEnemigoEn(destinoPrueba, this.gameObject)) continue;
            
            if (Mathf.Abs(player.position.x - destinoPrueba.x) < 0.1f && Mathf.Abs(player.position.z - destinoPrueba.z) < 0.1f)
            {
                continue;
            }
            
            float penalizacionPorApelotonamiento = 0f;
            foreach (GameObject aliado in spawner.enemigosActivos)
            {
                if (aliado == this.gameObject || aliado == null) continue;
                
                float distAliado = Vector3.Distance(destinoPrueba, aliado.transform.position);
                if (distAliado < blocSize * 1.5f) 
                {
                    penalizacionPorApelotonamiento += (blocSize * 2f - distAliado);
                }
            }

            float puntuacionFinal = costoFloodField + penalizacionPorApelotonamiento;

            if (puntuacionFinal < mejorPuntuacion)
            {
                mejorPuntuacion = puntuacionFinal;
                posicionIdeal = destinoPrueba;
            }
        }

        return posicionIdeal;
    }

    private IEnumerator MoverA(Vector3 destino)
    {
        isMoving = true;

        Vector3 direccionMirada = (destino - transform.position).normalized;
        if (direccionMirada != Vector3.zero)
        {
            direccionMirada.y = 0; 
            transform.rotation = Quaternion.LookRotation(direccionMirada);
        }

        Vector3 posicionInicial = transform.position;
        float tiempoPasado = 0f;

        while (tiempoPasado < tiempoDeViajePorCasilla)
        {
            tiempoPasado += Time.deltaTime;
            float progreso = tiempoPasado / tiempoDeViajePorCasilla;
            transform.position = Vector3.Lerp(posicionInicial, destino, progreso);
            yield return null;
        }

        transform.position = destino;
        isMoving = false;
    }
}