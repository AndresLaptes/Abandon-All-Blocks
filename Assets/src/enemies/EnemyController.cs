using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    public float tiempoDeViajePorCasilla = 0.4f;
    public float tiempoEsperaMinimo = 0.5f;
    public float tiempoEsperaMaximo = 1.5f;

    [Header("Configuración de Ataque")]
    public float distanciaRetroceso = 0.5f; 
    public float tiempoAnticipacion = 1.10f; 
    public float tiempoDash = 0.12f;         
    public float tiempoRecuperacion = 0.97f; 

    [Header("Habilidades Especiales")]
    [Tooltip("Pon aquí el prefab del charco. Solo el demonio de brea debería tenerlo.")]
    public GameObject prefabRastroBrea; 

    private float blocSize;
    private LevelManager levelManager;
    private Transform player;
    private EnemySpawner spawner;

    private bool isMoving = false;
    private bool isAttacking = false;
    private bool isDead = false;
    
    private Animator anim;

    public bool IsDead() => isDead;

    public void Inicializar(float tamanoBloque, LevelManager manager, Transform jugador, EnemySpawner spawnerRef)
    {
        blocSize = tamanoBloque;
        levelManager = manager;
        player = jugador;
        spawner = spawnerRef;

        anim = GetComponentInChildren<Animator>();
        if (anim != null) anim.applyRootMotion = false;

        StartCoroutine(RutinaDeIA());
    }

    public void RecibirDano()
    {
        if (isDead) return;
        isDead = true;
        
        StopAllCoroutines(); 
        
        if (anim != null) anim.SetTrigger("Morir");
        
        if (AudioManager.instance != null) 
            AudioManager.instance.PlaySFX(AudioManager.instance.sfxEnemigoMuerte);
        
        if (spawner != null && spawner.enemigosActivos.Contains(this.gameObject))
        {
            spawner.enemigosActivos.Remove(this.gameObject);
        }

        StartCoroutine(RutinaMuerte());
    }

    private IEnumerator RutinaMuerte()
    {
        yield return new WaitForSeconds(0.8f);

        float t = 0;
        float duracionHundimiento = 0.6f;
        Vector3 posOriginal = transform.position;
        Vector3 posFinal = posOriginal + Vector3.down * (blocSize * 1.5f);

        while (t < duracionHundimiento)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(posOriginal, posFinal, t / duracionHundimiento);
            yield return null;
        }

        Destroy(gameObject);
    }

    private IEnumerator RutinaDeIA()
    {
        yield return new WaitForSeconds(Random.Range(0.1f, 1f));

        while (true)
        {
            if (isDead) yield break; 

            float tiempoEspera = Random.Range(tiempoEsperaMinimo, tiempoEsperaMaximo);
            yield return new WaitForSeconds(tiempoEspera);

            if (!isMoving && !isAttacking && player != null && !isDead)
            {
                Vector3 miPosPlana = new Vector3(transform.position.x, 0, transform.position.z);
                Vector3 playerPosPlana = new Vector3(player.position.x, 0, player.position.z);
                
                if (Vector3.Distance(miPosPlana, playerPosPlana) < (blocSize * 1.2f))
                {
                    yield return StartCoroutine(RealizarAtaque(player.position));
                }
                else
                {
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
        if (anim != null) anim.SetTrigger("Atacar");
        
        if (AudioManager.instance != null) 
            AudioManager.instance.PlaySFX(AudioManager.instance.sfxEnemigoAtaque);
        
        Vector3 posInicial = new Vector3(
            Mathf.Round(transform.position.x / blocSize) * blocSize,
            transform.position.y,
            Mathf.Round(transform.position.z / blocSize) * blocSize
        );
        
        Vector3 direccionAtaque = (posJugadorDestino - posInicial).normalized;
        direccionAtaque.y = 0; 
        if (direccionAtaque != Vector3.zero) transform.rotation = Quaternion.LookRotation(direccionAtaque);

        Vector3 posRetroceso = posInicial - (direccionAtaque * distanciaRetroceso);
        float t = 0;
        while (t < tiempoAnticipacion)
        {
            if (isDead) yield break; 
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(posInicial, posRetroceso, t / tiempoAnticipacion);
            yield return null;
        }

        Vector3 posImpacto = posInicial + (direccionAtaque * (blocSize * 0.8f)); 
        t = 0;
        while (t < tiempoDash)
        {
            if (isDead) yield break; 
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(posRetroceso, posImpacto, t / tiempoDash);
            yield return null;
        }

        Vector3 posPlanaImpacto = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 posPlanaJugador = new Vector3(player.position.x, 0, player.position.z);

        if (Vector3.Distance(posPlanaImpacto, posPlanaJugador) < (blocSize * 1.1f))
        {
            PlayerHealth salud = player.GetComponent<PlayerHealth>();
            if (salud != null) salud.RecibirDano();
        }

        t = 0;
        while (t < tiempoRecuperacion)
        {
            if (isDead) yield break; 
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
        Vector3 posActualAlineada = new Vector3(
            Mathf.Round(transform.position.x / blocSize) * blocSize,
            transform.position.y,
            Mathf.Round(transform.position.z / blocSize) * blocSize
        );

        Vector3 posicionIdeal = posActualAlineada; 
        float mejorPuntuacion = float.MaxValue; 

        for (int i = 0; i < direcciones.Length; i++)
        {
            Vector3 destinoPrueba = new Vector3(
                posActualAlineada.x + (direcciones[i].x * blocSize),
                posActualAlineada.y,
                posActualAlineada.z + (direcciones[i].z * blocSize)
            );

            int costoFloodField = levelManager.ObtenerCostoFloodField(destinoPrueba);
            if (costoFloodField == 255) continue;
            if (spawner.HayEnemigoEn(destinoPrueba, this.gameObject)) continue;
            
            if (Mathf.Abs(player.position.x - destinoPrueba.x) < 0.1f && Mathf.Abs(player.position.z - destinoPrueba.z) < 0.1f) continue;
            
            float penalizacionPorApelotonamiento = 0f;
            foreach (GameObject aliado in spawner.enemigosActivos)
            {
                if (aliado == this.gameObject || aliado == null) continue;
                float distAliado = Vector3.Distance(destinoPrueba, aliado.transform.position);
                if (distAliado < blocSize * 1.5f) penalizacionPorApelotonamiento += (blocSize * 2f - distAliado);
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
        if (anim != null) anim.SetBool("Caminando", true);

        if (prefabRastroBrea != null)
        {
            Vector3 posRastro = new Vector3(
                Mathf.Round(transform.position.x / blocSize) * blocSize,
                transform.position.y + 0.05f, 
                Mathf.Round(transform.position.z / blocSize) * blocSize
            );
            Instantiate(prefabRastroBrea, posRastro, Quaternion.identity);
        }

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
            if (isDead) yield break; 
            tiempoPasado += Time.deltaTime;
            transform.position = Vector3.Lerp(posicionInicial, destino, tiempoPasado / tiempoDeViajePorCasilla);
            yield return null;
        }

        transform.position = destino;
        isMoving = false;
        if (anim != null) anim.SetBool("Caminando", false);
    }
}