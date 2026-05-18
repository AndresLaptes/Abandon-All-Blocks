using System.Collections.Generic;
using UnityEngine;

public class HUDCorazones3D : MonoBehaviour
{
    [Header("Configuración Automática")]
    public GameObject prefabCorazon;
    public Vector3 posicionInicial = new Vector3(-3.5f, 2f, 5f); 
    public float distanciaEntreCorazones = 1.2f;
    public Vector3 escalaCorazones = new Vector3(1f, 1f, 1f);

    [Header("Herramientas de Desarrollo")]
    public bool actualizarEnTiempoReal = true;

    private List<GameObject> corazonesInstanciados = new List<GameObject>();

    public void ConstruirHUD(int vidasMaximas)
    {
        if (prefabCorazon == null)
        {
            Debug.LogError("<b>[HUD ERROR]</b> ¡Falta el Prefab del corazón!");
            return;
        }

        foreach (GameObject c in corazonesInstanciados)
        {
            if (c != null) Destroy(c);
        }
        corazonesInstanciados.Clear();

        int capaHUD = LayerMask.NameToLayer("HUD_3D");

        for (int i = 0; i < vidasMaximas; i++)
        {
            GameObject nuevoCorazon = Instantiate(prefabCorazon, transform);
            
            Vector3 posicionLocal = posicionInicial + new Vector3(i * distanciaEntreCorazones, 0, 0);
            nuevoCorazon.transform.localPosition = posicionLocal;
            nuevoCorazon.transform.localRotation = Quaternion.identity;
            nuevoCorazon.transform.localScale = escalaCorazones;

            AsignarCapaRecursivamente(nuevoCorazon, capaHUD);
            corazonesInstanciados.Add(nuevoCorazon);
        }
    }

    public void ActualizarVidasHUD(int vidasActuales)
    {
        for (int i = 0; i < corazonesInstanciados.Count; i++)
        {
            if (corazonesInstanciados[i] != null)
            {
                corazonesInstanciados[i].SetActive(i < vidasActuales);
            }
        }
    }

    void Update()
    {
        if (actualizarEnTiempoReal && corazonesInstanciados.Count > 0)
        {
            for (int i = 0; i < corazonesInstanciados.Count; i++)
            {
                if (corazonesInstanciados[i] != null)
                {
                    Vector3 posicionLocal = posicionInicial + new Vector3(i * distanciaEntreCorazones, 0, 0);
                    corazonesInstanciados[i].transform.localPosition = posicionLocal;
                    corazonesInstanciados[i].transform.localScale = escalaCorazones;
                }
            }
        }
    }

    private void AsignarCapaRecursivamente(GameObject obj, int nuevaCapa)
    {
        if (nuevaCapa == -1) return;
        
        obj.layer = nuevaCapa;
        foreach (Transform hijo in obj.transform)
        {
            AsignarCapaRecursivamente(hijo.gameObject, nuevaCapa);
        }
    }
}