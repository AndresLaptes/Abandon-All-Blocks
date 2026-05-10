using UnityEngine;

public class RandomIdle : StateMachineBehaviour
{
    [Header("Tiempo de espera (Segundos)")]
    public float tiempoMinimo = 4f;
    public float tiempoMaximo = 10f;
    
    private float temporizador;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        temporizador = Random.Range(tiempoMinimo, tiempoMaximo);
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        temporizador -= Time.deltaTime;

        if (temporizador <= 0)
        {
            animator.SetTrigger("JugarIdle2");
            temporizador = Random.Range(tiempoMinimo, tiempoMaximo); 
        }
    }
}