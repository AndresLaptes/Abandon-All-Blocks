using System.Collections;
using UnityEngine;

public class GridMovement : MonoBehaviour
{
    [SerializeField] private float step_size = 1f;
    [SerializeField] private float tiempoMovimiento = 1.3f; 
    
    private bool isMoving = false;
    private Animator anim;

    void Start()
    {
        anim = GetComponentInChildren<Animator>(); 
    }

    public void ConfigurarPaso(float nuevoPaso)
    {
        step_size = nuevoPaso;
    }
    
    public void move(Vector3 direction)
    {
        
        if (anim != null && anim.GetCurrentAnimatorStateInfo(0).IsName("pray"))
        {
            return;
        }

        if (!isMoving && direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
            
            
            StartCoroutine(MoverSuavemente(direction));
        }
    }

    private IEnumerator MoverSuavemente(Vector3 direction)
    {
        isMoving = true;

        Vector3 destino = transform.position + direction * step_size;
        destino.x = Mathf.Round(destino.x / step_size) * step_size;
        destino.z = Mathf.Round(destino.z / step_size) * step_size;

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
    
    public bool IsMoving() 
    {
        return isMoving;
    }
}