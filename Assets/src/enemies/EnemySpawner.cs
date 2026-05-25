using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefabs de Enemigos")]
    public GameObject prefabHereje;
    public GameObject prefabGargola;
    public GameObject prefabBrea;
    public GameObject prefabBoss;

    [Header("Ajuste de Altura (Pies)")]
    public float offsetAltura = 0f;

    public List<GameObject> enemigosActivos = new List<GameObject>();

    public void LimpiarEnemigos()
    {
        foreach (GameObject e in enemigosActivos) if (e != null) Destroy(e);
        enemigosActivos.Clear();
    }

    // True si queda al menos un enemigo (cualquier tipo, incluido boss) vivo y no destruido.
    public bool HayEnemigosVivos()
    {
        foreach (GameObject e in enemigosActivos)
        {
            if (e == null) continue;

            BossController boss = e.GetComponent<BossController>();
            if (boss != null)
            {
                if (!boss.IsDead()) return true;
                continue;
            }

            EnemyController ec = e.GetComponent<EnemyController>();
            if (ec != null && !ec.IsDead()) return true;
        }
        return false;
    }
    
    // Función nueva: Devuelve el enemigo exacto en una casilla
    public EnemyController ObtenerEnemigoEn(Vector3 posDestino)
    {
        foreach (GameObject e in enemigosActivos)
        {
            if (e == null) continue;
            EnemyController cerebro = e.GetComponent<EnemyController>();
            if (cerebro != null && cerebro.IsDead()) continue; // Ignoramos muertos

            if (Mathf.Abs(e.transform.position.x - posDestino.x) < 0.1f &&
                Mathf.Abs(e.transform.position.z - posDestino.z) < 0.1f)
            {
                return cerebro;
            }
        }
        return null;
    }

    // Daña a cualquier enemigo (no boss) cuyo XZ esté dentro del radio dado.
    public int DaniarEnemigosEnArea(Vector3 centro, float radio)
    {
        int golpeados = 0;
        // Snapshot porque RecibirDano modifica enemigosActivos.
        List<GameObject> snapshot = new List<GameObject>(enemigosActivos);
        foreach (GameObject e in snapshot)
        {
            if (e == null) continue;
            if (e.GetComponent<BossController>() != null) continue;
            EnemyController ec = e.GetComponent<EnemyController>();
            if (ec == null || ec.IsDead()) continue;

            float dx = e.transform.position.x - centro.x;
            float dz = e.transform.position.z - centro.z;
            if (dx * dx + dz * dz <= radio * radio)
            {
                ec.RecibirDano();
                golpeados++;
            }
        }
        return golpeados;
    }

    // Daña al enemigo (cualquier tipo: EnemyController o BossController) que esté en la celda indicada.
    // Devuelve true si golpeó algo.
    public bool AtacarCelda(Vector3 posDestino, float tolerancia = 1.0f)
    {
        foreach (GameObject e in enemigosActivos)
        {
            if (e == null) continue;
            if (Mathf.Abs(e.transform.position.x - posDestino.x) > tolerancia) continue;
            if (Mathf.Abs(e.transform.position.z - posDestino.z) > tolerancia) continue;

            BossController boss = e.GetComponent<BossController>();
            if (boss != null)
            {
                if (boss.IsDead()) continue;
                boss.RecibirDano();
                return true;
            }

            EnemyController enemy = e.GetComponent<EnemyController>();
            if (enemy != null && !enemy.IsDead())
            {
                enemy.RecibirDano();
                return true;
            }
        }
        return false;
    }

    public bool HayEnemigoEn(Vector3 posDestino, GameObject enemigoQuePregunta)
    {
        foreach (GameObject e in enemigosActivos)
        {
            if (e == null || e == enemigoQuePregunta) continue;
            
            EnemyController cerebro = e.GetComponent<EnemyController>();
            if (cerebro != null && cerebro.IsDead()) continue; // Los muertos no bloquean casillas
            
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

        int totalEnemigos = datos.numHerejes + datos.numGargolas + datos.numBrea + datos.numBoss;
        if (totalEnemigos == 0) return;
        if (datos.filas == null || datos.filas.Length <= 1) return;

        List<Vector3> todasLasCeldas = new List<Vector3>();
        for (int z = 2; z < datos.sizeLevel; z++)
        {
            for (int x = 0; x < 5; x++)
            {
                if (datos.filas[z].columnas[x] != 1) continue;
                if (datos.gridPinchos != null && z < datos.gridPinchos.Length
                    && datos.gridPinchos[z] != null
                    && datos.gridPinchos[z].columnas[x] == 1) continue;
                if (datos.gridHachas != null && z < datos.gridHachas.Length
                    && datos.gridHachas[z] != null
                    && datos.gridHachas[z].columnas[x] == 1) continue;

                if (datos.filasTrampaTronco != null && System.Array.IndexOf(datos.filasTrampaTronco, z) >= 0) continue;

                Vector3 celdaPos = new Vector3((x - 2) * blocSize, 0f, z * blocSize);
                if (manager != null && manager.HayEstatuaEn(celdaPos)) continue;

                todasLasCeldas.Add(celdaPos);
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
        for (int i = 0; i < datos.numBoss; i++) if (prefabBoss != null) bolsaEnemigos.Add(prefabBoss);

        DesordenarLista(bolsaEnemigos); 

        List<Vector3> posicionesOcupadas = new List<Vector3>();

        foreach (GameObject prefab in bolsaEnemigos)
        {
            Vector3 posicionElegida = Vector3.zero;
            bool posicionValida = false;

            posicionValida = IntentarBuscarPosicion(celdasLejanas, posicionesOcupadas, blocSize, out posicionElegida);
            if (!posicionValida) posicionValida = IntentarBuscarPosicion(celdasMedias, posicionesOcupadas, blocSize, out posicionElegida);
            if (!posicionValida) posicionValida = IntentarBuscarPosicion(celdasCercanas, posicionesOcupadas, blocSize, out posicionElegida);
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

                Renderer[] rs = instanciaEnemigo.GetComponentsInChildren<Renderer>();
                if (rs.Length > 0)
                {
                    Bounds b = rs[0].bounds;
                    foreach (Renderer r in rs) b.Encapsulate(r.bounds);
                    float topeSuelo = blocSize / 2f;
                    float deltaY = topeSuelo - b.min.y + offsetAltura;
                    instanciaEnemigo.transform.position += new Vector3(0f, deltaY, 0f);
                }

                enemigosActivos.Add(instanciaEnemigo);

                BossController boss = instanciaEnemigo.GetComponent<BossController>();
                if (boss != null)
                {
                    boss.Inicializar(blocSize, manager, manager.player.transform);
                }
                else
                {
                    EnemyController cerebro = instanciaEnemigo.GetComponent<EnemyController>();
                    if (cerebro != null) cerebro.Inicializar(blocSize, manager, manager.player.transform, this);
                }
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