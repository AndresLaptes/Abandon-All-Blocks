using UnityEngine;
using TMPro;

public class HUDContadorMonedas : MonoBehaviour
{
    public TextMeshPro textoContador;
    public string prefijo = "";
    private int totalMonedas = 0;

    void Start()
    {
        ActualizarTexto();
    }

    public void SumarMoneda(int cantidad = 1)
    {
        totalMonedas += cantidad;
        ActualizarTexto();
    }

    private void ActualizarTexto()
    {
        if (textoContador != null)
        {
            textoContador.text = prefijo + totalMonedas.ToString();
        }
    }
}