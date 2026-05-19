using UnityEngine;
using TMPro;

public class HUDContadorSala : MonoBehaviour
{
    public TextMeshPro textoContador;
    public string prefijo = "";

    public void ActualizarSala(int numeroSala)
    {
        if (textoContador != null)
        {
            textoContador.text = prefijo + numeroSala.ToString();
        }
    }
}