using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int vidasMaximas = 3;
    private int vidasActuales;

    [Header("Impacto e Invulnerabilidad")]
    public float tiempoInvulnerabilidad = 1.5f;
    public float tiempoCongeladoPorImpacto = 0.5f;

    private HUDCorazones3D hudCorazones;
    private GridMovement movimiento;
    private LevelManager manager;
    private Animator anim;
    private bool esInvulnerable = false;

    void Start()
    {
        vidasActuales = vidasMaximas;
        movimiento = GetComponent<GridMovement>();
        anim = GetComponentInChildren<Animator>();
        manager = FindObjectOfType<LevelManager>();
        hudCorazones = FindObjectOfType<HUDCorazones3D>();
        
        if (hudCorazones != null)
        {
            hudCorazones.ConstruirHUD(vidasMaximas);
            hudCorazones.ActualizarVidasHUD(vidasActuales);
        }
    }

    public void RecibirDano()
    {
        if (vidasActuales <= 0 || !movimiento.enabled || esInvulnerable || movimiento.IsDead()) return; 

        // NUEVO: Ejecuta el dash de defensa
        if (movimiento.IsDefending()) 
        {
            movimiento.GolpeBloqueado();
            return;
        }

        vidasActuales--;
        
        if (hudCorazones != null)
        {
            hudCorazones.ActualizarVidasHUD(vidasActuales);
        }

        if (vidasActuales > 0)
        {
            StartCoroutine(RutinaImpactoYParpadeo());
        }
        else
        {
            StartCoroutine(RutinaMuerteYRespawn());
        }
    }

    private IEnumerator RutinaImpactoYParpadeo()
    {
        esInvulnerable = true;
        movimiento.SetHurt(true);
        
        if (anim != null) anim.SetTrigger("Impacto");

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        float t = 0;
        bool visible = true;
        
        while (t < tiempoInvulnerabilidad)
        {
            t += 0.1f;
            visible = !visible;
            
            foreach (Renderer r in renderers) 
            {
                if (r != null) r.enabled = visible;
            }
            
            if (t >= tiempoCongeladoPorImpacto)
            {
                movimiento.SetHurt(false);
            }
            
            yield return new WaitForSeconds(0.1f);
        }

        foreach (Renderer r in renderers) 
        {
            if (r != null) r.enabled = true;
        }
        
        esInvulnerable = false;
        movimiento.SetHurt(false);
    }

    private IEnumerator RutinaMuerteYRespawn()
    {
        // 1. Bloqueamos inputs y enviamos la animación de muerte
        movimiento.SetDead(true);
        if (anim != null) anim.SetTrigger("Morir");

        // 2. Esperamos a que la animación de muerte se reproduzca entera
        yield return new WaitForSeconds(3f);
        
        // 3. Restauramos las vidas para el respawn
        vidasActuales = vidasMaximas;
        if (hudCorazones != null) hudCorazones.ActualizarVidasHUD(vidasActuales);

        // 4. Reposicionamos y forzamos el reset de las animaciones a Idle
        manager.PosicionarJugador();
        movimiento.SetDead(false);
        if (anim != null) 
        {
            anim.Rebind();
            anim.Update(0f);
        }
    }
}