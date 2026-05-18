using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int vidasMaximas = 3;
    private int vidasActuales;

    private HUDCorazones3D hudCorazones;
    private GridMovement movimiento;
    private LevelManager manager;

    void Start()
    {
        vidasActuales = vidasMaximas;
        movimiento = GetComponent<GridMovement>();
        manager = FindObjectOfType<LevelManager>();
        hudCorazones = FindObjectOfType<HUDCorazones3D>();
        
        if (hudCorazones != null)
        {
            hudCorazones.ConstruirHUD(vidasMaximas);
            hudCorazones.ActualizarVidasHUD(vidasActuales);
        }
        else
        {
            Debug.LogWarning("HUDCorazones3D no encontrado en la escena.");
        }
    }

    public void RecibirDano()
    {
        if (vidasActuales <= 0 || !movimiento.enabled) return; 

        vidasActuales--;
        
        if (hudCorazones != null)
        {
            hudCorazones.ActualizarVidasHUD(vidasActuales);
        }

        if (vidasActuales > 0)
        {
            StartCoroutine(RutinaMuerteYRespawn());
        }
        else
        {
            Debug.LogError("GAME OVER");
        }
    }

    private IEnumerator RutinaMuerteYRespawn()
    {
        movimiento.enabled = false;

        yield return new WaitForSeconds(2.5f);

        manager.PosicionarJugador();

        movimiento.enabled = true;
    }
}