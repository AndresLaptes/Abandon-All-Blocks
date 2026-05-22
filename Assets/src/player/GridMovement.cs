using System.Collections;
using UnityEngine;

public class GridMovement : MonoBehaviour
{
    [SerializeField] private float step_size = 1f;
    [SerializeField] private float tiempoMovimiento = 0.5f; 
    
    [Header("Configuración de Caída")]
    [SerializeField] private float limiteCaidaY = -5f; 

    [Header("Equipamiento Visual")]
    [Tooltip("Arrastra aquí tu espada desde la Jerarquía")]
    public GameObject espadaObj;
    [Tooltip("Arrastra aquí tu escudo desde la Jerarquía")]
    public GameObject escudoObj;
    public float tiempoAparicionArmas = 0.5f; 
    
    [Tooltip("Tamaño máximo que alcanzarán las armas al terminar de rezar")]
    public Vector3 tamanoFinalArmas = new Vector3(0.5f, 0.5f, 0.5f);
    
    private bool isMoving = false;
    private bool isDead = false;
    private bool isAttacking = false;
    private bool isDefending = false;
    private bool isHurt = false;
    private bool estabaRezando = false;
    
    private Animator anim;
    private LevelManager levelManager;

    void Start()
    {
        anim = GetComponentInChildren<Animator>(); 
        levelManager = FindObjectOfType<LevelManager>();
        
        if (anim != null)
        {
            anim.applyRootMotion = false;
        }
    }

    void Update()
    {
        if (anim == null) return;

        bool estaRezando = anim.GetCurrentAnimatorStateInfo(0).IsName("pray");

        if (estaRezando && !estabaRezando)
        {
            OcultarArmas();
            estabaRezando = true;
        }
        else if (!estaRezando && estabaRezando)
        {
            StartCoroutine(AparecerArmasProgresivamente());
            estabaRezando = false;
        }
    }

    private void OcultarArmas()
    {
        if (espadaObj != null) espadaObj.SetActive(false);
        if (escudoObj != null) escudoObj.SetActive(false);
    }

    private IEnumerator AparecerArmasProgresivamente()
    {
        if (espadaObj != null)
        {
            espadaObj.SetActive(true);
            espadaObj.transform.localScale = Vector3.zero; 
        }
        if (escudoObj != null)
        {
            escudoObj.SetActive(true);
            escudoObj.transform.localScale = Vector3.zero;
        }

        float t = 0f;
        while (t < tiempoAparicionArmas)
        {
            t += Time.deltaTime;
            float progreso = t / tiempoAparicionArmas;

            if (espadaObj != null) espadaObj.transform.localScale = Vector3.Lerp(Vector3.zero, tamanoFinalArmas, progreso);
            if (escudoObj != null) escudoObj.transform.localScale = Vector3.Lerp(Vector3.zero, tamanoFinalArmas, progreso);

            yield return null;
        }

        if (espadaObj != null) espadaObj.transform.localScale = tamanoFinalArmas;
        if (escudoObj != null) escudoObj.transform.localScale = tamanoFinalArmas;
    }

    public void ConfigurarPaso(float nuevoPaso)
    {
        step_size = nuevoPaso;
    }

    public void ResetearEstado()
    {
        StopAllCoroutines();
        isMoving = false;
        isDead = false;
        isAttacking = false;
        isDefending = false;
        isHurt = false;
        estabaRezando = false;

        if (espadaObj != null) { espadaObj.SetActive(true); espadaObj.transform.localScale = tamanoFinalArmas; }
        if (escudoObj != null) { escudoObj.SetActive(true); escudoObj.transform.localScale = tamanoFinalArmas; }

        if (anim == null) anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.applyRootMotion = false;
            anim.Rebind();
            anim.Update(0f);
            anim.SetBool("Caminando", false);
            anim.SetBool("Defendiendo", false);
            anim.ResetTrigger("Caer");
        }
    }

    public bool IsDead() => isDead;
    public bool IsHurt() => isHurt;
    public bool IsAttacking() => isAttacking;
    public bool IsDefending() => isDefending;
    public bool IsMoving() => isMoving;
    public float TiempoMovimiento => tiempoMovimiento;

    public void SetHurt(bool state) 
    { 
        isHurt = state; 
    }

    public void SetDefending(bool def) 
    { 
        if (anim != null && anim.GetCurrentAnimatorStateInfo(0).IsName("pray")) return;

        if (isDefending == def) return; 
        
        isDefending = def; 
        if (anim != null) anim.SetBool("Defendiendo", def); 
    }

    public void Atacar() 
    { 
        if (anim != null && anim.GetCurrentAnimatorStateInfo(0).IsName("pray")) return;

        if (isAttacking) return;
        StartCoroutine(RutinaAtaque()); 
    }

    private IEnumerator RutinaAtaque()
    {
        isAttacking = true;
        
        Vector3 posicionOriginal = transform.position;
        Quaternion rotacionOriginal = transform.rotation;
        
        if (anim != null) 
        {
            anim.applyRootMotion = true; 
            anim.SetTrigger("Atacar");
        }
        
        yield return new WaitForSeconds(1.7f); 
        
        if (anim != null) 
        {
            anim.applyRootMotion = false; 
            
            transform.position = posicionOriginal;
            transform.rotation = rotacionOriginal;
        }
        
        isAttacking = false;
    }
    
    public void move(Vector3 direction)
    {
        if (anim != null && anim.GetCurrentAnimatorStateInfo(0).IsName("pray")) return;
        if (isDead || isHurt || isAttacking || isDefending) return;

        if (!isMoving && direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
            
            Vector3 destino = transform.position + direction * step_size;
            destino.x = Mathf.Round(destino.x / step_size) * step_size;
            destino.z = Mathf.Round(destino.z / step_size) * step_size;
            
            if (levelManager != null && levelManager.EsPared(destino)) return; 

            if (levelManager != null && levelManager.EsCeldaPuerta(destino))
            {
                levelManager.IniciarAperturaPuerta();
                StartCoroutine(MoverConRetraso(destino, tiempoMovimiento));
            }
            else if (levelManager != null && levelManager.ExisteSueloEn(destino))
            {
                StartCoroutine(MoverSuavemente(destino));
            }
            else
            {
                StartCoroutine(CaerAlVacioParabola(direction));
            }
        }
    }

    private IEnumerator CaerAlVacioParabola(Vector3 direction)
    {
        isMoving = true;
        isDead = true; 

        if (anim != null) anim.SetBool("Caminando", false);
        if (anim != null) anim.SetTrigger("Caer");

        Vector3 posicionInicial = transform.position;
        float velocidadHorizontal = step_size / tiempoMovimiento; 
        float tiempoPasado = 0f;
        float tiempoTropiezo = 0.09f;

        while (true)
        {
            Vector3 posHorizontal = posicionInicial + direction * (velocidadHorizontal * tiempoPasado);
            float caidaY = posicionInicial.y;

            if (tiempoPasado > tiempoTropiezo)
            {
                float tiempoGravedad = tiempoPasado - tiempoTropiezo;
                float fuerzaGravedad = 20f * Mathf.Pow(tiempoGravedad, 2); 
                caidaY = posicionInicial.y - fuerzaGravedad;
            }

            if (caidaY <= limiteCaidaY)
            {
                transform.position = new Vector3(posHorizontal.x, limiteCaidaY, posHorizontal.z);
                break;
            }

            transform.position = new Vector3(posHorizontal.x, caidaY, posHorizontal.z);
            tiempoPasado += Time.deltaTime;
            yield return null;
        }
    }
    
    private IEnumerator MoverConRetraso(Vector3 destino, float retraso)
    {
        isMoving = true;
        float esperado = 0f;
        while (esperado < retraso)
        {
            esperado += Time.deltaTime;
            yield return null;
        }
        if (anim != null) anim.SetBool("Caminando", true);
        Vector3 posicionInicial = transform.position;
        float tiempoPasado = 0f;
        while (tiempoPasado < tiempoMovimiento)
        {
            transform.position = Vector3.Lerp(posicionInicial, destino, tiempoPasado / tiempoMovimiento);
            tiempoPasado += Time.deltaTime;
            yield return null;
        }
        transform.position = destino;
        if (anim != null) anim.SetBool("Caminando", false);
        isMoving = false;
    }

    private IEnumerator MoverSuavemente(Vector3 destino)
    {
        isMoving = true;
        if (anim != null) anim.SetBool("Caminando", true);

        Vector3 posicionInicial = transform.position;
        float tiempoPasado = 0f;

        while (tiempoPasado < tiempoMovimiento)
        {
            transform.position = Vector3.Lerp(posicionInicial, destino, tiempoPasado / tiempoMovimiento);
            tiempoPasado += Time.deltaTime;
            yield return null;
        }

        transform.position = destino;
        if (anim != null) anim.SetBool("Caminando", false);
        isMoving = false;
    }
}