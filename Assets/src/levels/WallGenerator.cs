using System.Collections.Generic;
using UnityEngine;

public class WallGenerator : MonoBehaviour
{
    [Header("Prefabs de pared")]
    public GameObject paredSuelo;     
    public GameObject paredArribaA;   
    public GameObject paredArribaB;   
    public GameObject paredFade;      

    [Header("Puerta")]
    public GameObject puertaArriba;   
    public GameObject puertaAbajo;    
    [Range(-2, 2)] public int puertaCellX = 0;

    [Header("Configuración")]
    public float blocSize = 2f;
    [Range(0, 5)] public int filasArriba = 1;
    [Range(0, 8)] public int filasAbajo = 4;
    [Range(1f, 1.15f)] public float solapePared = 1.01f;
    [Range(0f, 1f)] public float probabilidadDecoracion = 0.15f;

    [Header("Decoración - Antorchas")]
    public GameObject antorchaPrefab;
    [Range(0, 10)] public int separacionAntorchas = 4;
    public float alturaAntorcha = 2.5f;
    public Vector3 antorchaRotacionEuler = new Vector3(0f, 90f, 0f);
    public float antorchaOffsetInterior = 0f;
    public Vector3 antorchaEscala = new Vector3(3f, 3f, 3f);

    private List<GameObject> activeWalls = new List<GameObject>();
    private Vector3 nativeSize = Vector3.one;
    private bool nativeSizeCached = false;

    public void CargarAssetsDesdeCarpeta(string nombreCarpeta)
    {
        paredSuelo = Resources.Load<GameObject>($"{nombreCarpeta}/paredBase") ?? Resources.Load<GameObject>($"{nombreCarpeta}/pared1_1");
        paredArribaA = Resources.Load<GameObject>($"{nombreCarpeta}/paredDecoA") ?? Resources.Load<GameObject>($"{nombreCarpeta}/pared1_2");
        paredArribaB = Resources.Load<GameObject>($"{nombreCarpeta}/paredDecoB") ?? Resources.Load<GameObject>($"{nombreCarpeta}/pared1_3");
        paredFade = Resources.Load<GameObject>($"{nombreCarpeta}/paredFade") ?? Resources.Load<GameObject>($"{nombreCarpeta}/pared1_4");
        
        puertaArriba = Resources.Load<GameObject>($"{nombreCarpeta}/puertaArriba") ?? Resources.Load<GameObject>($"{nombreCarpeta}/puerta1_1");
        puertaAbajo = Resources.Load<GameObject>($"{nombreCarpeta}/puertaAbajo") ?? Resources.Load<GameObject>($"{nombreCarpeta}/puerta1_2");
        
        antorchaPrefab = Resources.Load<GameObject>($"{nombreCarpeta}/antorcha");

        nativeSizeCached = false; 
        Debug.Log($"WallGenerator: Muros cargados desde {nombreCarpeta}");
    }

    public void GenerarParedes(int sizeLevel)
    {
        LimpiarParedes();
        if (paredSuelo == null) return;

        CacheNativeSize();

        bool widthIsZ = nativeSize.z >= nativeSize.x;
        Vector3 scale = Vector3.one;
        if (widthIsZ) scale.z = (blocSize / nativeSize.z) * solapePared;
        else scale.x = (blocSize / nativeSize.x) * solapePared;

        Quaternion rotFrontal = Quaternion.Euler(0f, 90f, 0f);
        float floorTopY = blocSize / 2f;
        float floorBottomY = -blocSize / 2f;

        bool generarPuerta = (puertaArriba != null && puertaAbajo != null);

        int totalArriba = 1 + filasArriba;
        for (int i = 0; i < totalArriba; i++)
        {
            float wallCenterY = floorTopY + (i + 0.5f) * nativeSize.y;
            bool esBase = (i == 0);
            int skipBackX = (generarPuerta && (i == 0 || i == 1)) ? puertaCellX : int.MinValue;
            GenerarFila(sizeLevel, wallCenterY, scale, rotFrontal, esBase, false, skipBackX);
        }

        GenerarFila(sizeLevel, 0f, scale, rotFrontal, false, true);

        for (int i = 0; i < filasAbajo; i++)
        {
            float wallCenterY = floorBottomY - (i + 0.5f) * nativeSize.y;
            GenerarFila(sizeLevel, wallCenterY, scale, rotFrontal, false, true);
        }

        if (generarPuerta) SpawnPuerta(sizeLevel, scale);
        SpawnAntorchas(sizeLevel);
    }

    private void GenerarFila(int sizeLevel, float wallCenterY, Vector3 scale, Quaternion rotFrontal, bool esBase, bool esFade, int skipBackX = int.MinValue)
    {
        float offset = nativeSize.x / 2f;
        for (int z = 0; z < sizeLevel; z++)
            SpawnWall(ElegirPrefab(esBase, esFade), new Vector3(-2.5f * blocSize - offset, wallCenterY, z * blocSize), Quaternion.identity, scale);
        for (int x = -2; x <= 2; x++)
        {
            if (x == skipBackX) continue;
            SpawnWall(ElegirPrefab(esBase, esFade), new Vector3(x * blocSize, wallCenterY, (sizeLevel - 0.5f) * blocSize + offset), rotFrontal, scale);
        }
        Vector3 cornerScale = new Vector3(1f, 1f, (offset * 2f) / nativeSize.z);
        Vector3 cornerPos = new Vector3(-2.5f * blocSize - offset, wallCenterY, (sizeLevel - 0.5f) * blocSize + offset);
        GameObject prefabEsquina = esFade ? (paredFade != null ? paredFade : paredSuelo) : paredSuelo;
        SpawnWall(prefabEsquina, cornerPos, Quaternion.identity, cornerScale);
    }

    private void SpawnPuerta(int sizeLevel, Vector3 scale)
    {
        float offset = nativeSize.x / 2f;
        float floorTopY = blocSize / 2f;
        float xPos = puertaCellX * blocSize;
        float zPos = (sizeLevel - 0.5f) * blocSize + offset;
        GameObject doorRoot = new GameObject("Door");
        doorRoot.transform.SetParent(transform);
        doorRoot.transform.position = new Vector3(xPos, 0f, zPos);
        Quaternion rotFrontal = Quaternion.Euler(0f, 90f, 0f);
        float yAbajo = floorTopY + 0.5f * nativeSize.y;
        GameObject abajo = Instantiate(puertaAbajo, new Vector3(xPos, yAbajo, zPos), rotFrontal, doorRoot.transform);
        abajo.transform.localScale = scale;
        float yArriba = floorTopY + 1.5f * nativeSize.y;
        GameObject arriba = Instantiate(puertaArriba, new Vector3(xPos, yArriba, zPos), rotFrontal, doorRoot.transform);
        arriba.transform.localScale = scale;
        doorRoot.AddComponent<Door>();
        activeWalls.Add(doorRoot);
    }

    private GameObject ElegirPrefab(bool esBase, bool esFade)
    {
        if (esFade) return paredFade != null ? paredFade : paredSuelo;
        if (esBase) return paredSuelo;
        if (Random.value < probabilidadDecoracion && (paredArribaA != null || paredArribaB != null))
        {
            if (paredArribaA != null && paredArribaB != null) return Random.value < 0.5f ? paredArribaA : paredArribaB;
            return paredArribaA != null ? paredArribaA : paredArribaB;
        }
        return paredSuelo;
    }

    private void SpawnAntorchas(int sizeLevel)
    {
        if (antorchaPrefab == null || separacionAntorchas <= 0) return;
        float offset = nativeSize.x / 2f;
        float xMontaje = -2.5f * blocSize - offset + nativeSize.x + antorchaOffsetInterior;
        Quaternion rot = Quaternion.Euler(antorchaRotacionEuler);
        for (int z = separacionAntorchas / 2; z < sizeLevel; z += separacionAntorchas)
        {
            Vector3 pos = new Vector3(xMontaje, alturaAntorcha, z * blocSize);
            GameObject ant = Instantiate(antorchaPrefab, pos, rot, transform);
            ant.transform.localScale = antorchaEscala;
            activeWalls.Add(ant);
        }
    }

    public void LimpiarParedes()
    {
        foreach (GameObject wall in activeWalls) if (wall != null) Destroy(wall);
        activeWalls.Clear();
    }

    private void CacheNativeSize()
    {
        if (nativeSizeCached) return;
        GameObject reference = paredSuelo != null ? paredSuelo : paredFade;
        if (reference == null) { nativeSize = Vector3.one; nativeSizeCached = true; return; }
        GameObject temp = Instantiate(reference);
        Renderer[] renderers = temp.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
            nativeSize = b.size;
        }
        else nativeSize = Vector3.one;
        Destroy(temp);
        nativeSizeCached = true;
    }

    private void SpawnWall(GameObject prefab, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        if (prefab == null) return;
        GameObject wall = Instantiate(prefab, position, rotation, transform);
        wall.transform.localScale = scale;
        activeWalls.Add(wall);
    }
}