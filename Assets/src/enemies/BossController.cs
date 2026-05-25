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

    [Header("Animación procedural - referencias")]
    [Tooltip("Torso central (Transform que se mueve arriba/abajo con el bob al andar).")]
    public Transform body;
    public Transform head;
    public Transform alaIzqPivot;
    public Transform alaDerPivot;
    public Transform piernaIzqPivot;
    public Transform piernaDerPivot;
    public Transform brazoIzqPivot;
    public Transform brazoDerPivot;
    public Transform colaPivot;

    [Header("Aleteo continuo de alas")]
    [Tooltip("Frecuencia del aleteo (ciclos por segundo).")]
    public float aleteoFrecuencia = 1.5f;
    [Tooltip("Amplitud del aleteo en grados.")]
    public float aleteoAmplitud = 25f;
    [Tooltip("Eje local del ala izq sobre el que rota al aletear.")]
    public Vector3 ejeAleteoIzq = new Vector3(0f, 0f, 1f);
    [Tooltip("Eje local del ala der (típicamente espejado al izq).")]
    public Vector3 ejeAleteoDer = new Vector3(0f, 0f, -1f);

    [Header("Bob del cuerpo al moverse")]
    public float bobFrecuencia = 4f;
    public float bobAmplitud = 0.15f;

    [Header("Caminar (piernas + brazos + cabeza)")]
    public float walkFrecuencia = 6f;
    public float piernaAmplitud = 30f;
    public float brazoAmplitud = 25f;
    public float cabezaAmplitud = 8f;

    [Header("Ataque - animación brazos")]
    [Tooltip("Grados sumados al Euler de la pose A durante la anticipación (brazos arriba). Positivo = mismo sentido que la apose.")]
    public float ataqueAnguloAnticipacion = 90f;
    [Tooltip("Grados sumados al Euler de la pose A en el impacto (brazos golpeando, sentido opuesto al de levantar).")]
    public float ataqueAnguloImpacto = -100f;
    [Tooltip("Máscara Vector3 que indica sobre qué componente Euler (X/Y/Z) del brazo izq se aplica el ángulo de ataque. (0,0,1) = mismo eje que la apose Z.")]
    public Vector3 ataqueEjeBrazoIzq = new Vector3(0f, 0f, 1f);
    [Tooltip("Máscara del brazo der. Suele ser simétrica (signo opuesto al izq) si la apose también lo es.")]
    public Vector3 ataqueEjeBrazoDer = new Vector3(0f, 0f, -1f);

    [Header("Pose A de brazos (offset base)")]
    [Tooltip("Rotación Euler local base del brazo izquierdo en reposo. El swing se aplica encima.")]
    public Vector3 brazoIzqAposeEuler = new Vector3(0f, 0f, 35f);
    [Tooltip("Rotación Euler local base del brazo derecho en reposo. El swing se aplica encima.")]
    public Vector3 brazoDerAposeEuler = new Vector3(0f, 0f, -35f);
    [Tooltip("Desplazamiento local del pivote del brazo izq respecto a su posición original (para despegarlo del cuerpo).")]
    public Vector3 brazoIzqOffsetPos = Vector3.zero;
    [Tooltip("Desplazamiento local del pivote del brazo der respecto a su posición original.")]
    public Vector3 brazoDerOffsetPos = Vector3.zero;

    [Header("Cola")]
    [Tooltip("Frecuencia del balanceo lateral de la cola (ciclos por segundo).")]
    public float colaFrecuencia = 1.2f;
    [Tooltip("Amplitud del balanceo de la cola en grados.")]
    public float colaAmplitud = 15f;
    [Tooltip("Eje sobre el que rota la cola (típicamente Y para balanceo lateral).")]
    public Vector3 colaEje = new Vector3(0f, 1f, 0f);

    private float bodyBaseY = 0f;
    private bool bodyBaseCacheada = false;

    private Vector3 brazoIzqBasePos;
    private Vector3 brazoDerBasePos;
    private bool brazosBaseCacheados = false;

    private float attackArmAngle = 0f;

    [Header("Caída al vacío")]
    [Tooltip("Velocidad de caída cuando se queda sin suelo debajo.")]
    public float velocidadCaida = 5f;
    [Tooltip("Y bajo el cual al caer se dispara la muerte normal del boss.")]
    public float yMuerteCaida = -5f;
    private bool isFalling = false;

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

    void Update()
    {
        if (isDead) return;

        if (!isFalling && !isMoving && !isAttacking && levelManager != null && blocSize > 0f)
        {
            Vector3 celdaAlineada = new Vector3(
                Mathf.Round(transform.position.x / blocSize) * blocSize,
                transform.position.y,
                Mathf.Round(transform.position.z / blocSize) * blocSize
            );
            if (!levelManager.ExisteSueloEn(celdaAlineada))
            {
                isFalling = true;
                StopAllCoroutines();
            }
        }

        if (isFalling)
        {
            transform.Translate(Vector3.down * velocidadCaida * Time.deltaTime, Space.World);
            if (transform.position.y < yMuerteCaida)
            {
                isFalling = false;
                ForzarMuerte();
            }
        }
    }

    private void ForzarMuerte()
    {
        if (isDead) return;
        isInvulnerable = false;
        vidasActuales = 0;
        Morir();
    }

    void LateUpdate()
    {
        AnimarPiezas();

        if (!aplicarRotacionEnLateUpdate) return;
        Vector3 e = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(e.x, desiredYRotation, e.z);
    }

    private void AnimarPiezas()
    {
        if (isDead) return;

        // Aleteo continuo de alas, esté quieto o moviéndose.
        float aleteo = Mathf.Sin(Time.time * Mathf.PI * 2f * aleteoFrecuencia) * aleteoAmplitud;
        if (alaIzqPivot != null) alaIzqPivot.localRotation = Quaternion.AngleAxis(aleteo, ejeAleteoIzq);
        if (alaDerPivot != null) alaDerPivot.localRotation = Quaternion.AngleAxis(aleteo, ejeAleteoDer);

        // Cola: balanceo lateral continuo.
        float colaSway = Mathf.Sin(Time.time * Mathf.PI * 2f * colaFrecuencia) * colaAmplitud;
        if (colaPivot != null) colaPivot.localRotation = Quaternion.AngleAxis(colaSway, colaEje);

        // Bob + paso + cabeza solo si se está moviendo.
        if (body != null && !bodyBaseCacheada)
        {
            bodyBaseY = body.localPosition.y;
            bodyBaseCacheada = true;
        }

        if (!brazosBaseCacheados)
        {
            if (brazoIzqPivot != null) brazoIzqBasePos = brazoIzqPivot.localPosition;
            if (brazoDerPivot != null) brazoDerBasePos = brazoDerPivot.localPosition;
            brazosBaseCacheados = true;
        }

        if (brazoIzqPivot != null)
            brazoIzqPivot.localPosition = brazoIzqBasePos + OffsetEnLocal(brazoIzqPivot, brazoIzqOffsetPos);
        if (brazoDerPivot != null)
            brazoDerPivot.localPosition = brazoDerBasePos + OffsetEnLocal(brazoDerPivot, brazoDerOffsetPos);

        if (isMoving)
        {
            float walkT = Time.time * Mathf.PI * 2f * walkFrecuencia;

            if (body != null)
            {
                float bobOffset = Mathf.Sin(Time.time * Mathf.PI * 2f * bobFrecuencia) * bobAmplitud;
                Vector3 bp = body.localPosition;
                bp.y = bodyBaseY + bobOffset;
                body.localPosition = bp;
            }

            float legSwing = Mathf.Sin(walkT) * piernaAmplitud;
            if (piernaIzqPivot != null) piernaIzqPivot.localRotation = Quaternion.Euler(legSwing, 0f, 0f);
            if (piernaDerPivot != null) piernaDerPivot.localRotation = Quaternion.Euler(-legSwing, 0f, 0f);

            // Brazos: opuestos a la pierna del mismo lado (caminar natural) + pose A base.
            float armSwing = Mathf.Sin(walkT) * brazoAmplitud;
            if (brazoIzqPivot != null) brazoIzqPivot.localRotation = Quaternion.Euler(brazoIzqAposeEuler) * Quaternion.Euler(-armSwing, 0f, 0f);
            if (brazoDerPivot != null) brazoDerPivot.localRotation = Quaternion.Euler(brazoDerAposeEuler) * Quaternion.Euler(armSwing, 0f, 0f);

            if (head != null)
            {
                float headSway = Mathf.Sin(walkT * 0.5f) * cabezaAmplitud;
                head.localRotation = Quaternion.Euler(0f, headSway, 0f);
            }
        }
        else
        {
            // Reposo: extremidades y cabeza vuelven suavemente a neutro.
            float k = Time.deltaTime * 5f;
            if (piernaIzqPivot != null) piernaIzqPivot.localRotation = Quaternion.Slerp(piernaIzqPivot.localRotation, Quaternion.identity, k);
            if (piernaDerPivot != null) piernaDerPivot.localRotation = Quaternion.Slerp(piernaDerPivot.localRotation, Quaternion.identity, k);
            if (brazoIzqPivot != null) brazoIzqPivot.localRotation = Quaternion.Slerp(brazoIzqPivot.localRotation, Quaternion.Euler(brazoIzqAposeEuler), k);
            if (brazoDerPivot != null) brazoDerPivot.localRotation = Quaternion.Slerp(brazoDerPivot.localRotation, Quaternion.Euler(brazoDerAposeEuler), k);
            if (head != null) head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.identity, k);
            if (body != null && bodyBaseCacheada)
            {
                Vector3 bp = body.localPosition;
                bp.y = Mathf.Lerp(bp.y, bodyBaseY, k);
                body.localPosition = bp;
            }
        }

        // Atacando: el ángulo de ataque se suma directamente al Euler de la apose en la componente indicada por el eje-máscara.
        if (isAttacking)
        {
            Vector3 izqEuler = brazoIzqAposeEuler + ataqueEjeBrazoIzq * attackArmAngle;
            Vector3 derEuler = brazoDerAposeEuler + ataqueEjeBrazoDer * attackArmAngle;
            if (brazoIzqPivot != null) brazoIzqPivot.localRotation = Quaternion.Euler(izqEuler);
            if (brazoDerPivot != null) brazoDerPivot.localRotation = Quaternion.Euler(derEuler);
        }
    }

    // Convierte un offset "en mundo" a unidades de localPosition del pivot indicado, dividiendo por la escala del padre.
    private Vector3 OffsetEnLocal(Transform t, Vector3 offsetMundo)
    {
        Transform p = t.parent;
        if (p == null) return offsetMundo;
        Vector3 s = p.lossyScale;
        return new Vector3(
            s.x != 0f ? offsetMundo.x / s.x : 0f,
            s.y != 0f ? offsetMundo.y / s.y : 0f,
            s.z != 0f ? offsetMundo.z / s.z : 0f
        );
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
            if (AudioManager.instance != null)
            {
                AudioClip clip = AudioManager.instance.sfxDanoBoss != null
                    ? AudioManager.instance.sfxDanoBoss
                    : AudioManager.instance.sfxRecibirDano;
                AudioManager.instance.PlaySFX(clip);
            }
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

        if (AudioManager.instance != null)
        {
            AudioClip clip = AudioManager.instance.sfxMuerteBoss != null
                ? AudioManager.instance.sfxMuerteBoss
                : AudioManager.instance.sfxEnemigoMuerte;
            AudioManager.instance.PlaySFX(clip);
        }

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

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySFX(AudioManager.instance.sfxEnemigoAtaque);

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
        float anguloBrazoInicio = attackArmAngle;
        while (t < tiempoAnticipacion)
        {
            if (isDead) yield break;
            t += Time.deltaTime;
            float k = t / tiempoAnticipacion;
            transform.position = Vector3.Lerp(posInicial, posRetroceso, k);
            attackArmAngle = Mathf.Lerp(anguloBrazoInicio, ataqueAnguloAnticipacion, k);
            yield return null;
        }

        Vector3 posImpacto = posInicial + (direccionAtaque * (blocSize * 0.8f));
        t = 0;
        while (t < tiempoDash)
        {
            if (isDead) yield break;
            t += Time.deltaTime;
            float k = t / tiempoDash;
            transform.position = Vector3.Lerp(posRetroceso, posImpacto, k);
            attackArmAngle = Mathf.Lerp(ataqueAnguloAnticipacion, ataqueAnguloImpacto, k);
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
            float k = t / tiempoRecuperacion;
            transform.position = Vector3.Lerp(posImpacto, posInicial, k);
            attackArmAngle = Mathf.Lerp(ataqueAnguloImpacto, 0f, k);
            yield return null;
        }

        transform.position = posInicial;
        attackArmAngle = 0f;
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

            GridMovement playerMov = player.GetComponent<GridMovement>();
            if (playerMov != null && playerMov.EstaApuntandoA(destinoPrueba)) continue;

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
