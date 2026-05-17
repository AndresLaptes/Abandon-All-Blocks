using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    public float tiempoDeViajePorCasilla = 0.4f;
    public float tiempoEsperaMinimo = 0.5f;
    public float tiempoEsperaMaximo = 1.5f;

    [Header("Herramientas")]
    public bool modoDebug = false;

    private float blocSize;
    private LevelManager levelManager;
    private Transform player;
    private EnemySpawner spawner;

    private bool isMoving = false;

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

            if (!isMoving && player != null)
            {
                Vector3 mejorCasilla = CalcularMejorCasilla();
                
                if (Vector3.Distance(transform.position, mejorCasilla) > 0.1f)
                {
                    yield return StartCoroutine(MoverA(mejorCasilla));
                }
            }
        }
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
            
            if (costoFloodField == 255)
            {
                continue;
            }

            if (spawner.HayEnemigoEn(destinoPrueba, this.gameObject))
            {
                continue;
            }

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