using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefabs de Enemigos")]
    public GameObject prefabHereje;
    public GameObject prefabGargola;
    public GameObject prefabBrea;

    [Header("Ajuste de Altura (Pies)")]
    [Tooltip("Si los enemigos se hunden en el suelo, sube este valor (ej: 0.5 o 1.0). Si flotan, bájalo.")]
    public float offsetAltura = 0f;

    public List<GameObject> enemigosActivos = new List<GameObject>();

    public void LimpiarEnemigos()
    {
        foreach (GameObject e in enemigosActivos) if (e != null) Destroy(e);
        enemigosActivos.Clear();
    }
    
    public bool HayEnemigoEn(Vector3 posDestino, GameObject enemigoQuePregunta)
    {
        foreach (GameObject e in enemigosActivos)
        {
            if (e == null || e == enemigoQuePregunta) continue;
            
            if (Mathf.Abs(e.transform.position.x - posDestino.x) < 0.1f &&
                Mathf.Abs(e.transform.position.z - posDestino.z) < 0.1f)
            {
                return true;
            }
        }
        return false;
    }

    public void GenerarEnemigos(LevelData datos, float blocSize, LevelManager manager)
    {
        LimpiarEnemigos();

        int totalEnemigos = datos.numHerejes + datos.numGargolas + datos.numBrea;
        if (totalEnemigos == 0) return;
        if (datos.filas == null || datos.filas.Length <= 1) return;

        List<Vector3> todasLasCeldas = new List<Vector3>();
        for (int z = 2; z < datos.sizeLevel; z++)
        {
            for (int x = 0; x < 5; x++)
            {
                if (datos.filas[z].columnas[x] == 1)
                {
                    todasLasCeldas.Add(new Vector3((x - 2) * blocSize, 0f, z * blocSize));
                }
            }
        }

        DesordenarLista(todasLasCeldas);

        float zLejano = (datos.sizeLevel * 0.66f) * blocSize; 
        float zMedio = (datos.sizeLevel * 0.33f) * blocSize;  

        List<Vector3> celdasLejanas = todasLasCeldas.Where(c => c.z >= zLejano).ToList();
        List<Vector3> celdasMedias = todasLasCeldas.Where(c => c.z >= zMedio && c.z < zLejano).ToList();
        List<Vector3> celdasCercanas = todasLasCeldas.Where(c => c.z < zMedio).ToList();

        List<GameObject> bolsaEnemigos = new List<GameObject>();
        for (int i = 0; i < datos.numHerejes; i++) if (prefabHereje != null) bolsaEnemigos.Add(prefabHereje);
        for (int i = 0; i < datos.numGargolas; i++) if (prefabGargola != null) bolsaEnemigos.Add(prefabGargola);
        for (int i = 0; i < datos.numBrea; i++) if (prefabBrea != null) bolsaEnemigos.Add(prefabBrea);

        DesordenarLista(bolsaEnemigos); 

        List<Vector3> posicionesOcupadas = new List<Vector3>();

        foreach (GameObject prefab in bolsaEnemigos)
        {
            Vector3 posicionElegida = Vector3.zero;
            bool posicionValida = false;

            posicionValida = IntentarBuscarPosicion(celdasLejanas, posicionesOcupadas, blocSize, out posicionElegida);
            
            if (!posicionValida)
                posicionValida = IntentarBuscarPosicion(celdasMedias, posicionesOcupadas, blocSize, out posicionElegida);
            
            if (!posicionValida)
                posicionValida = IntentarBuscarPosicion(celdasCercanas, posicionesOcupadas, blocSize, out posicionElegida);

            if (!posicionValida)
            {
                posicionValida = IntentarBuscarPosicion(celdasLejanas, posicionesOcupadas, 0f, out posicionElegida) ||
                                 IntentarBuscarPosicion(celdasMedias, posicionesOcupadas, 0f, out posicionElegida) ||
                                 IntentarBuscarPosicion(celdasCercanas, posicionesOcupadas, 0f, out posicionElegida);
            }

            if (posicionValida)
            {
                posicionesOcupadas.Add(posicionElegida);

                float alturaSueloTecho = (blocSize / 2f) + offsetAltura; 
                Vector3 posicionInicial = new Vector3(posicionElegida.x, alturaSueloTecho, posicionElegida.z);

                float[] angulos = { 0f, 90f, 180f, 270f };
                float anguloElegido = angulos[Random.Range(0, angulos.Length)];
                Quaternion rotacionAleatoria = Quaternion.Euler(0f, anguloElegido, 0f);

                GameObject instanciaEnemigo = Instantiate(prefab, posicionInicial, rotacionAleatoria, this.transform);

                enemigosActivos.Add(instanciaEnemigo);
                
                EnemyController cerebro = instanciaEnemigo.GetComponent<EnemyController>();
                if (cerebro != null) cerebro.Inicializar(blocSize, manager, manager.player.transform, this);
            }
        }
    }

    private bool IntentarBuscarPosicion(List<Vector3> celdasDisponibles, List<Vector3> ocupadas, float tamañoBloque, out Vector3 elegida)
    {
        elegida = Vector3.zero;
        foreach (Vector3 celda in celdasDisponibles)
        {
            bool demasiadoCerca = false;
            foreach (Vector3 ocupada in ocupadas)
            {
                if (tamañoBloque <= 0f) break;

                float distX = Mathf.Abs(celda.x - ocupada.x);
                float distZ = Mathf.Abs(celda.z - ocupada.z);

               
                if (distX < tamañoBloque * 1.1f && distZ < tamañoBloque * 1.1f)
                {
                    demasiadoCerca = true;
                    break;
                }
            }

            if (!demasiadoCerca)
            {
                elegida = celda;
                celdasDisponibles.Remove(celda); 
                return true;
            }
        }
        return false;
    }

    private void DesordenarLista<T>(List<T> lista)
    {
        for (int i = 0; i < lista.Count; i++)
        {
            T temp = lista[i];
            int rand = Random.Range(i, lista.Count);
            lista[i] = lista[rand];
            lista[rand] = temp;
        }
    }
}