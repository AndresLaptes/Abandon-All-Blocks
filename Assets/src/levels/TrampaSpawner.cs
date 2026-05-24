using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrampaSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject prefabTronco;
    public GameObject prefabPinchos;
    public GameObject prefabHacha;

    [Header("Pinchos")]
    [Tooltip("Altura Y donde se coloca el prefab de pinchos (sobre el suelo del nivel).")]
    public float alturaSpawnPinchos = 1f;

    [Header("Hachas")]
    [Tooltip("Altura Y donde se coloca el pivote del hacha (la cabeza del techo/mango).")]
    public float alturaSpawnHachas = 4f;
    [Tooltip("Si true, da a cada hacha un faseInicial aleatorio para desincronizarlas.")]
    public bool desfasarHachas = true;

    [Header("Ciclo")]
    [Tooltip("Segundos entre que un tronco cae al vacío y aparece el siguiente en la misma fila.")]
    public float retrasoRespawn = 2f;

    [Tooltip("Segundos de delay inicial antes de spawnear el primer tronco de cada fila.")]
    public float retrasoInicial = 0.5f;

    [Header("Spawn")]
    [Tooltip("Celdas fuera del tablero donde aparece el tronco antes de entrar a la sala. 0 = primera celda dentro de la sala, pegado a la pared.")]
    public float offsetSpawnEnCeldas = 0f;

    [Tooltip("Altura Y del tronco al spawnear (sobre el suelo).")]
    public float alturaSpawn = 1.75f;

    [Header("Tronco")]
    public float velocidadTronco = 4f;
    public float velocidadAngular = 360f;

    private List<Coroutine> coroutinesActivas = new List<Coroutine>();
    private List<GameObject> trampasActivas = new List<GameObject>();
    private List<GameObject> pinchosActivos = new List<GameObject>();
    private List<GameObject> hachasActivas = new List<GameObject>();
    private HashSet<int> filasMuertas = new HashSet<int>();
    private float blocSizeCacheado = 2.5f;

    public void LimpiarTrampas()
    {
        foreach (var c in coroutinesActivas) if (c != null) StopCoroutine(c);
        coroutinesActivas.Clear();

        foreach (var t in trampasActivas) if (t != null) Destroy(t);
        trampasActivas.Clear();

        foreach (var p in pinchosActivos) if (p != null) Destroy(p);
        pinchosActivos.Clear();

        foreach (var h in hachasActivas) if (h != null) Destroy(h);
        hachasActivas.Clear();

        filasMuertas.Clear();
    }

    public void GenerarTrampas(LevelData datos, float blocSize, LevelManager manager)
    {
        LimpiarTrampas();
        blocSizeCacheado = blocSize;

        GenerarTroncos(datos, blocSize, manager);
        GenerarPinchos(datos, blocSize, manager);
        GenerarHachas(datos, blocSize, manager);
    }

    public void HacerCaerEnFila(int filaZ)
    {
        filasMuertas.Add(filaZ);
        float posZ = filaZ * blocSizeCacheado;

        foreach (GameObject p in pinchosActivos)
        {
            if (p == null) continue;
            if (Mathf.Abs(p.transform.position.z - posZ) < blocSizeCacheado * 0.5f) HacerCaer(p);
        }

        foreach (GameObject t in trampasActivas)
        {
            if (t == null) continue;
            if (Mathf.Abs(t.transform.position.z - posZ) < blocSizeCacheado * 0.5f) HacerCaer(t);
        }
    }

    private void HacerCaer(GameObject obj)
    {
        if (obj.GetComponent<CaidaSimple>() != null) return;
        foreach (MonoBehaviour mb in obj.GetComponents<MonoBehaviour>())
        {
            if (mb != null) mb.enabled = false;
        }
        obj.AddComponent<CaidaSimple>();
    }

    private void GenerarTroncos(LevelData datos, float blocSize, LevelManager manager)
    {
        if (prefabTronco == null) return;
        if (datos.filasTrampaTronco == null || datos.filasTrampaTronco.Length == 0) return;

        foreach (int fila in datos.filasTrampaTronco)
        {
            if (fila < 0 || fila >= datos.sizeLevel) continue;
            Coroutine c = StartCoroutine(BucleSpawn(fila, blocSize, manager));
            coroutinesActivas.Add(c);
        }
    }

    private void GenerarPinchos(LevelData datos, float blocSize, LevelManager manager)
    {
        if (prefabPinchos == null) return;
        if (datos.gridPinchos == null) return;

        for (int z = 0; z < datos.sizeLevel; z++)
        {
            if (datos.gridPinchos[z] == null) continue;
            for (int x = 0; x < 5; x++)
            {
                if (datos.gridPinchos[z].columnas[x] != 1) continue;
                if (datos.filas == null || datos.filas[z] == null) continue;
                if (datos.filas[z].columnas[x] == 0) continue; // no spawn si no hay suelo

                Vector3 pos = new Vector3((x - 2) * blocSize, alturaSpawnPinchos, z * blocSize);
                GameObject inst = Instantiate(prefabPinchos, pos, Quaternion.identity, transform);
                pinchosActivos.Add(inst);

                TrampaPinchos tp = inst.GetComponent<TrampaPinchos>();
                if (tp != null) tp.Inicializar(manager.player != null ? manager.player.transform : null);
            }
        }
    }

    private void GenerarHachas(LevelData datos, float blocSize, LevelManager manager)
    {
        if (prefabHacha == null) return;
        if (datos.gridHachas == null) return;

        for (int z = 0; z < datos.sizeLevel; z++)
        {
            if (datos.gridHachas[z] == null) continue;
            for (int x = 0; x < 5; x++)
            {
                if (datos.gridHachas[z].columnas[x] != 1) continue;
                if (datos.filas == null || datos.filas[z] == null) continue;
                if (datos.filas[z].columnas[x] == 0) continue;

                Vector3 pos = new Vector3((x - 2) * blocSize, alturaSpawnHachas, z * blocSize);
                GameObject inst = Instantiate(prefabHacha, pos, Quaternion.identity, transform);
                hachasActivas.Add(inst);

                TrampaHacha th = inst.GetComponent<TrampaHacha>();
                if (th != null)
                {
                    if (desfasarHachas) th.faseInicial = Random.Range(0f, th.periodo);
                    th.Inicializar(manager.player != null ? manager.player.transform : null);
                }
            }
        }
    }

    private IEnumerator BucleSpawn(int fila, float blocSize, LevelManager manager)
    {
        yield return new WaitForSeconds(retrasoInicial);
        if (filasMuertas.Contains(fila)) yield break;

        GameObject activo = SpawnTroncoIdle(fila, blocSize, manager);
        Activar(activo);
        GameObject siguiente = SpawnTroncoIdle(fila, blocSize, manager);

        while (true)
        {
            while (activo != null && !filasMuertas.Contains(fila)) yield return null;
            if (filasMuertas.Contains(fila)) yield break;
            trampasActivas.Remove(activo);

            if (retrasoRespawn > 0f) yield return new WaitForSeconds(retrasoRespawn);
            if (filasMuertas.Contains(fila)) yield break;

            Activar(siguiente);
            activo = siguiente;

            siguiente = SpawnTroncoIdle(fila, blocSize, manager);
        }
    }

    private GameObject SpawnTroncoIdle(int fila, float blocSize, LevelManager manager)
    {
        float xSpawn = -(2f + offsetSpawnEnCeldas) * blocSize;
        float zSpawn = fila * blocSize;
        Vector3 pos = new Vector3(xSpawn, alturaSpawn, zSpawn);
        Quaternion rot = Quaternion.Euler(0f, 90f, 0f);

        GameObject inst = Instantiate(prefabTronco, pos, rot, transform);
        trampasActivas.Add(inst);

        TrampaTronco t = inst.GetComponent<TrampaTronco>();
        if (t != null)
        {
            t.velocidadLineal = velocidadTronco;
            t.velocidadAngular = velocidadAngular;
            t.direccionMovimiento = new Vector3(1f, 0f, 0f);
            t.ejeRotacion = new Vector3(0f, 0f, 1f);
            t.activo = false;
            t.Inicializar(blocSize, manager.player != null ? manager.player.transform : null);
        }
        return inst;
    }

    private void Activar(GameObject inst)
    {
        if (inst == null) return;
        TrampaTronco t = inst.GetComponent<TrampaTronco>();
        if (t != null) t.activo = true;
    }
}
