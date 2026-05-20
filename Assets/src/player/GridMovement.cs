using System.Collections;
using UnityEngine;

public class GridMovement : MonoBehaviour
{
    [SerializeField] private float step_size = 1f;
    [SerializeField] private float tiempoMovimiento = 0.5f; 
    
    [Header("Configuración de Caída")]
    [SerializeField] private float limiteCaidaY = -5f; 
    
    private bool isMoving = false;
    private bool isDead = false;
    private Animator anim;
    
    private LevelManager levelManager;

    void Start()
    {
        anim = GetComponentInChildren<Animator>(); 
        levelManager = FindObjectOfType<LevelManager>();
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
        if (anim == null) anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
            anim.SetBool("Caminando", false);
            anim.ResetTrigger("Caer");
        }
    }
    
    public void move(Vector3 direction)
    {
        if (anim != null && anim.GetCurrentAnimatorStateInfo(0).IsName("pray")) return;
        if (isDead) return;

        if (!isMoving && direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
            
            Vector3 destino = transform.position + direction * step_size;
            destino.x = Mathf.Round(destino.x / step_size) * step_size;
            destino.z = Mathf.Round(destino.z / step_size) * step_size;
            
            if (levelManager != null && levelManager.EsPared(destino))
            {
                return; 
            }

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
    
    public bool IsMoving() => isMoving;
    public float TiempoMovimiento => tiempoMovimiento;
}