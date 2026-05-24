using System.Collections;
using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("Vida")]
    public int vidasMaximas = 5;
    [Tooltip("Segundos en los que tras recibir un golpe el boss queda quieto, parpadea y no puede atacar ni recibir más daño.")]
    public float duracionInvulnerabilidad = 1.8f;

    [Header("Movimiento")]
    public float tiempoDeViajePorCasilla = 0.5f;
    public float tiempoEsperaMinimo = 0.4f;
    public float tiempoEsperaMaximo = 1.0f;

    [Header("Ataque")]
    public float distanciaRetroceso = 0.6f;
    public float tiempoAnticipacion = 1.0f;
    public float tiempoDash = 0.12f;
    public float tiempoRecuperacion = 0.8f;
    [Tooltip("Multiplicador de daño que aplica al player. 1 = un corazón.")]
    public int danoAlPlayer = 1;

    [Header("Muerte")]
    [Tooltip("Tiempo desde que se dispara Morir hasta que el GameObject se destruye.")]
    public float retrasoDestruccion = 2.5f;
    [Tooltip("Profundidad a la que se hunde el cuerpo al morir.")]
    public float profundidadHundimiento = 1.5f;

    [Header("Spawn")]
    [Tooltip("Si > 0, fuerza la altura Y inicial del boss ignorando el cálculo automático del spawner.")]
    public float alturaSpawnOverride = 1.25f;

    [Header("Orientación")]
    [Tooltip("Compensa el forward del mesh. 0 si el modelo mira a +Z. 180 si mira al revés. 90 o -90 si va de lado.")]
    public float eulerOffsetY = 0f;

    private float desiredYRotation = 0f;
    private bool aplicarRotacionEnLateUpdate = false;

    private Coroutine ataqueCoroutine;

    [Header("Parpadeo al recibir daño")]
    [Tooltip("Veces por segundo que parpadea el boss durante la invulnerabilidad.")]
    public float frecuenciaParpadeo = 12f;

    private Renderer[] renderersParpadeo;

    private float blocSize;
    private LevelManager levelManager;
    private Transform player;

    private int vidasActuales;
    private bool isMoving = false;
    private bool isAttacking = false;
    private bool isInvulnerable = false;
    private bool isDead = false;

    private Animator anim;

    public bool IsDead() => isDead;
    public int VidasActuales => vidasActuales;

    public void Inicializar(float tamanoBloque, LevelManager manager, Transform jugador)
    {
        blocSize = tamanoBloque;
        levelManager = manager;
        player = jugador;

        vidasActuales = vidasMaximas;

        if (alturaSpawnOverride > 0f)
        {
            Vector3 p = transform.position;
            p.y = alturaSpawnOverride;
            transform.position = p;
        }

        anim = GetComponentInChildren<Animator>();
        if (anim != null) anim.applyRootMotion = false;

        renderersParpadeo = GetComponentsInChildren<Renderer>();

        desiredYRotation = transform.eulerAngles.y;
        aplicarRotacionEnLateUpdate = true;

        StartCoroutine(RutinaDeIA());
    }

    void LateUpdate()
    {
        if (!aplicarRotacionEnLateUpdate) return;
        Vector3 e = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(e.x, desiredYRotation, e.z);
    }

    private void OrientarHacia(Vector3 direccion)
    {
        direccion.y = 0f;
        if (direccion.sqrMagnitude < 0.0001f) return;
        float baseY = Quaternion.LookRotation(direccion).eulerAngles.y;
        desiredYRotation = baseY + eulerOffsetY;
    }

    public void RecibirDano()
    {
        if (isDead || isInvulnerable) return;

        if (isAttacking && ataqueCoroutine != null)
        {
            StopCoroutine(ataqueCoroutine);
            ataqueCoroutine = null;
            isAttacking = false;
            Vector3 p = transform.position;
            p.x = Mathf.Round(p.x / blocSize) * blocSize;
            p.z = Mathf.Round(p.z / blocSize) * blocSize;
            transform.position = p;
        }

        vidasActuales--;

        if (vidasActuales <= 0)
        {
            Morir();
        }
        else
        {
            if (anim != null) anim.SetTrigger("Damaged");
            StartCoroutine(RutinaInvulnerabilidad());
        }
    }

    private IEnumerator RutinaInvulnerabilidad()
    {
        isInvulnerable = true;
        StartCoroutine(RutinaParpadeo());
        yield return new WaitForSeconds(duracionInvulnerabilidad);
        isInvulnerable = false;
    }

    private IEnumerator RutinaParpadeo()
    {
        if (renderersParpadeo == null || renderersParpadeo.Length == 0) yield break;

        float intervalo = frecuenciaParpadeo > 0f ? (1f / (2f * frecuenciaParpadeo)) : 0.05f;
        bool visible = false;

        while (isInvulnerable && !isDead)
        {
            visible = !visible;
            foreach (Renderer r in renderersParpadeo) if (r != null) r.enabled = visible;
            yield return new WaitForSeconds(intervalo);
        }

        foreach (Renderer r in renderersParpadeo) if (r != null) r.enabled = true;
    }

    private void Morir()
    {
        isDead = true;
        StopAllCoroutines();
        RestaurarVisibilidad();
        isInvulnerable = false;
        isMoving = false;
        isAttacking = false;

        if (anim != null) anim.SetTrigger("Morir");

        StartCoroutine(RutinaMuerte());
    }

    private void RestaurarVisibilidad()
    {
        if (renderersParpadeo == null) return;
        foreach (Renderer r in renderersParpadeo) if (r != null) r.enabled = true;
    }

    private IEnumerator RutinaMuerte()
    {
        yield return new WaitForSeconds(retrasoDestruccion);

        float t = 0;
        float duracionHundimiento = 1.5f;
        Vector3 posOriginal = transform.position;
        Vector3 posFinal = posOriginal + Vector3.down * (blocSize * profundidadHundimiento);

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
        yield return new WaitForSeconds(Random.Range(0.3f, 0.8f));

        while (true)
        {
            if (isDead) yield break;

            float tiempoEspera = Random.Range(tiempoEsperaMinimo, tiempoEsperaMaximo);
            yield return new WaitForSeconds(tiempoEspera);

            if (isMoving || isAttacking || isInvulnerable || player == null || isDead) continue;

            Vector3 miPosPlana = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 playerPosPlana = new Vector3(player.position.x, 0, player.position.z);

            if (Vector3.Distance(miPosPlana, playerPosPlana) < (blocSize * 1.2f))
            {
                ataqueCoroutine = StartCoroutine(RealizarAtaque(player.position));
                yield return new WaitUntil(() => !isAttacking || isDead);
                ataqueCoroutine = null;
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

    private IEnumerator RealizarAtaque(Vector3 posJugadorDestino)
    {
        isAttacking = true;
        if (anim != null) anim.SetTrigger("Atacar");

        Vector3 posInicial = new Vector3(
            Mathf.Round(transform.position.x / blocSize) * blocSize,
            transform.position.y,
            Mathf.Round(transform.position.z / blocSize) * blocSize
        );

        Vector3 direccionAtaque = (posJugadorDestino - posInicial).normalized;
        direccionAtaque.y = 0;
        OrientarHacia(direccionAtaque);

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
            if (player != null) OrientarHacia(player.position - transform.position);
            yield return null;
        }

        Vector3 posPlanaImpacto = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 posPlanaJugador = new Vector3(player.position.x, 0, player.position.z);

        if (Vector3.Distance(posPlanaImpacto, posPlanaJugador) < (blocSize * 1.1f))
        {
            PlayerHealth salud = player.GetComponent<PlayerHealth>();
            if (salud != null)
            {
                for (int i = 0; i < danoAlPlayer; i++) salud.RecibirDano();
            }
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

            int costoFloodField = levelManager != null ? levelManager.ObtenerCostoFloodField(destinoPrueba) : 0;
            if (costoFloodField == 255) continue;

            if (Mathf.Abs(player.position.x - destinoPrueba.x) < 0.1f && Mathf.Abs(player.position.z - destinoPrueba.z) < 0.1f) continue;

            if (costoFloodField < mejorPuntuacion)
            {
                mejorPuntuacion = costoFloodField;
                posicionIdeal = destinoPrueba;
            }
        }
        return posicionIdeal;
    }

    private IEnumerator MoverA(Vector3 destino)
    {
        isMoving = true;
        if (anim != null) anim.SetBool("Caminando", true);

        Vector3 direccionMirada = (destino - transform.position).normalized;
        OrientarHacia(direccionMirada);

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
