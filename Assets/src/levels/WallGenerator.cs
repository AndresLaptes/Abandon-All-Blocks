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
    public GameObject puerta;
    [Range(-2, 2)] public int puertaCellX = 0;
    public Vector3 puertaPanelOffset = Vector3.zero;

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

    [Header("Fade de paredes")]
    [Tooltip("World Y por encima de cual la pared se ve con textura completa.")]
    public float fadeTopY = 1f;
    [Tooltip("World Y por debajo de cual la pared está 100% disuelta en colorFondo.")]
    public float fadeBottomY = -5f;

    [HideInInspector] public Color fadeColorFondo = Color.black;
    [HideInInspector] public Color fadeTint = new Color(0.6f, 0.6f, 0.6f, 1f);
    [HideInInspector] public bool paredesApiladas = false;

    private List<GameObject> activeWalls = new List<GameObject>();
    private Vector3 nativeSize = Vector3.one;
    private Vector3 nativeCenter = Vector3.zero;
    private bool nativeSizeCached = false;
    private Material fadeMatActual;
    private Material antorchaMatActual;
    private MaterialPropertyBlock fadeMPB;
    private GameObject panelActual;
    private Vector3 panelBasePos;
    [HideInInspector] public Door doorActual;

    void Update()
    {
        if (panelActual != null)
            panelActual.transform.position = panelBasePos + puertaPanelOffset;
    }

    public void CargarAssetsDesdeCarpeta(string nombreCarpeta)
    {
        paredSuelo = Resources.Load<GameObject>($"{nombreCarpeta}/paredBase") ?? Resources.Load<GameObject>($"{nombreCarpeta}/pared1_1");
        paredArribaA = Resources.Load<GameObject>($"{nombreCarpeta}/paredDecoA") ?? Resources.Load<GameObject>($"{nombreCarpeta}/pared1_2");
        paredArribaB = Resources.Load<GameObject>($"{nombreCarpeta}/paredDecoB") ?? Resources.Load<GameObject>($"{nombreCarpeta}/pared1_3");
        paredFade = Resources.Load<GameObject>($"{nombreCarpeta}/paredFade") ?? Resources.Load<GameObject>($"{nombreCarpeta}/pared1_4");
        
        puertaArriba = Resources.Load<GameObject>($"{nombreCarpeta}/puertaArriba") ?? Resources.Load<GameObject>($"{nombreCarpeta}/puerta1_1");
        puertaAbajo = Resources.Load<GameObject>($"{nombreCarpeta}/puertaAbajo") ?? Resources.Load<GameObject>($"{nombreCarpeta}/puerta1_2");
        puerta = Resources.Load<GameObject>($"{nombreCarpeta}/puerta");
        
        antorchaPrefab = Resources.Load<GameObject>($"{nombreCarpeta}/antorcha");

        fadeMatActual = Resources.Load<Material>($"{nombreCarpeta}/Wall1_FadeDown");
        antorchaMatActual = Resources.Load<Material>($"{nombreCarpeta}/mat_antorcha")
                          ?? Resources.Load<Material>($"{nombreCarpeta}/antorcha")
                          ?? Resources.Load<Material>($"{nombreCarpeta}/defaultMat");

        nativeSizeCached = false;
        Debug.Log($"WallGenerator: Muros cargados desde {nombreCarpeta} (fadeMat={(fadeMatActual != null ? fadeMatActual.name : "NO ENCONTRADO")}, antorchaMat={(antorchaMatActual != null ? antorchaMatActual.name : "NO ENCONTRADO")})");
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
        float floorTopY = paredesApiladas ? blocSize : blocSize / 2f;
        float pivotYAdjust = paredesApiladas ? 0f : 0.5f;

        bool generarPuerta = (puertaArriba != null && puertaAbajo != null);

        int totalArriba = paredesApiladas ? 3 : (1 + filasArriba);
        for (int i = 0; i < totalArriba; i++)
        {
            float wallCenterY = floorTopY + (i + pivotYAdjust) * nativeSize.y;
            bool esBase = (i == 0);
            int skipBackX = (generarPuerta && (i == 0 || i == 1)) ? puertaCellX : int.MinValue;
            GenerarFila(sizeLevel, wallCenterY, scale, rotFrontal, esBase, false, skipBackX, i);
        }

        float fadeTopRowCenterY = floorTopY - (1f - pivotYAdjust) * nativeSize.y;
        GenerarFila(sizeLevel, fadeTopRowCenterY, scale, rotFrontal, false, true);

        for (int i = 0; i < filasAbajo; i++)
        {
            float wallCenterY = fadeTopRowCenterY - (i + 1) * nativeSize.y;
            GenerarFila(sizeLevel, wallCenterY, scale, rotFrontal, false, true);
        }

        if (generarPuerta) SpawnPuerta(sizeLevel, scale);
        SpawnAntorchas(sizeLevel);
    }

    private void GenerarFila(int sizeLevel, float wallCenterY, Vector3 scale, Quaternion rotFrontal, bool esBase, bool esFade, int skipBackX = int.MinValue, int filaIndex = -1)
    {
        float offset = nativeSize.x / 2f;
        for (int z = 0; z < sizeLevel; z++)
            SpawnWall(ElegirPrefab(esBase, esFade, filaIndex), new Vector3(-2.5f * blocSize - offset, wallCenterY, z * blocSize), Quaternion.identity, scale);
        for (int x = -2; x <= 2; x++)
        {
            if (x == skipBackX) continue;
            SpawnWall(ElegirPrefab(esBase, esFade, filaIndex), new Vector3(x * blocSize, wallCenterY, (sizeLevel - 0.5f) * blocSize + offset), rotFrontal, scale);
        }
        Vector3 cornerScale = new Vector3(1f, 1f, (offset * 2f) / nativeSize.z);
        Vector3 cornerPos = new Vector3(-2.5f * blocSize - offset, wallCenterY, (sizeLevel - 0.5f) * blocSize + offset);
        GameObject prefabEsquina = esFade ? (paredFade != null ? paredFade : paredSuelo) : ElegirPrefab(esBase, esFade, filaIndex);
        SpawnWall(prefabEsquina, cornerPos, Quaternion.identity, cornerScale);
    }

    private void SpawnPuerta(int sizeLevel, Vector3 scale)
    {
        float offset = nativeSize.x / 2f;
        float floorTopY = paredesApiladas ? blocSize : blocSize / 2f;
        float pivotYAdjust = paredesApiladas ? 0f : 0.5f;
        float xPos = puertaCellX * blocSize;
        float zPos = (sizeLevel - 0.5f) * blocSize + offset;
        GameObject doorRoot = new GameObject("Door");
        doorRoot.transform.SetParent(transform);
        doorRoot.transform.position = new Vector3(xPos, 0f, zPos);
        Quaternion rotFrontal = Quaternion.Euler(0f, 90f, 0f);
        float yAbajo = floorTopY + pivotYAdjust * nativeSize.y;
        GameObject abajo = Instantiate(puertaAbajo, new Vector3(xPos, yAbajo, zPos), rotFrontal, doorRoot.transform);
        abajo.transform.localScale = scale;
        float yArriba = floorTopY + (1f + pivotYAdjust) * nativeSize.y;
        GameObject arriba = Instantiate(puertaArriba, new Vector3(xPos, yArriba, zPos), rotFrontal, doorRoot.transform);
        arriba.transform.localScale = scale;
        GameObject panel = null;
        GameObject pivote = null;
        if (puerta != null)
        {
            pivote = new GameObject("PanelHinge");
            pivote.transform.SetParent(doorRoot.transform);
            pivote.transform.localRotation = Quaternion.identity;
            pivote.transform.position = Vector3.zero;

            panel = Instantiate(puerta, pivote.transform);
            panel.name = "PanelPuerta";
            panel.transform.localPosition = Vector3.zero;
            panel.transform.localRotation = rotFrontal;
            panel.transform.localScale = scale;

            Renderer rPanel = panel.GetComponentInChildren<Renderer>();
            float halfWidth = (rPanel != null) ? rPanel.bounds.size.x / 2f : blocSize / 2f;

            panelBasePos = new Vector3(xPos + halfWidth, yAbajo, zPos);
            pivote.transform.position = panelBasePos + puertaPanelOffset;
            panel.transform.localPosition = new Vector3(-halfWidth, 0f, 0f);

            panelActual = pivote;
        }
        Door doorComp = doorRoot.AddComponent<Door>();
        doorComp.Configurar(blocSize, puertaCellX, sizeLevel);
        doorComp.panel = pivote;
        doorActual = doorComp;
        activeWalls.Add(doorRoot);
    }

    private GameObject ElegirPrefab(bool esBase, bool esFade, int filaIndex = -1)
    {
        if (esFade) return paredFade != null ? paredFade : paredSuelo;
        if (esBase) return paredSuelo;
        if (paredesApiladas)
        {
            if (filaIndex == 1 && paredArribaA != null) return paredArribaA;
            if (filaIndex == 2 && paredArribaB != null) return paredArribaB;
            return paredSuelo;
        }
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

        Debug.Log($"WallGenerator: spawneando antorchas con prefab='{antorchaPrefab.name}' (instanceId={antorchaPrefab.GetInstanceID()}), material override='{(antorchaMatActual != null ? antorchaMatActual.name : "ninguno")}' (instanceId={(antorchaMatActual != null ? antorchaMatActual.GetInstanceID() : 0)})");
        for (int z = separacionAntorchas / 2; z < sizeLevel; z += separacionAntorchas)
        {
            Vector3 pos = new Vector3(xMontaje, alturaAntorcha, z * blocSize);
            GameObject ant = Instantiate(antorchaPrefab, pos, rot, transform);
            ant.transform.localScale = antorchaEscala;
            if (antorchaMatActual != null)
            {
                foreach (Renderer r in ant.GetComponentsInChildren<Renderer>())
                {
                    int n = r.sharedMaterials.Length;
                    Material[] mats = new Material[n];
                    for (int i = 0; i < n; i++) mats[i] = antorchaMatActual;
                    r.sharedMaterials = mats;
                }
            }
            activeWalls.Add(ant);
        }
    }

    public void LimpiarParedes()
    {
        foreach (GameObject wall in activeWalls) if (wall != null) Destroy(wall);
        activeWalls.Clear();
        panelActual = null;
        doorActual = null;
    }

    private void CacheNativeSize()
    {
        if (nativeSizeCached) return;
        GameObject reference = paredSuelo != null ? paredSuelo : paredFade;
        if (reference == null) { nativeSize = Vector3.one; nativeCenter = Vector3.zero; nativeSizeCached = true; return; }
        GameObject temp = Instantiate(reference, Vector3.zero, Quaternion.identity);
        Renderer[] renderers = temp.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
            nativeSize = b.size;
            nativeCenter = b.center;
        }
        else { nativeSize = Vector3.one; nativeCenter = Vector3.zero; }
        Destroy(temp);
        nativeSizeCached = true;
    }

    private void SpawnWall(GameObject prefab, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        if (prefab == null) return;
        Vector3 spawnPos = position;
        if (paredesApiladas)
        {
            Vector3 nativeCenterXZ = new Vector3(nativeCenter.x, 0f, nativeCenter.z);
            Vector3 pivotComp = rotation * Vector3.Scale(nativeCenterXZ, scale);
            spawnPos = position - pivotComp;
        }
        GameObject wall = Instantiate(prefab, spawnPos, rotation, transform);
        wall.transform.localScale = scale;
        if (prefab == paredFade)
        {
            if (fadeMPB == null) fadeMPB = new MaterialPropertyBlock();
            fadeMPB.SetColor("_ColorFondo", fadeColorFondo);
            fadeMPB.SetColor("_BaseColor", fadeTint);
            fadeMPB.SetFloat("_TopY", fadeTopY);
            fadeMPB.SetFloat("_BottomY", fadeBottomY);

            foreach (Renderer r in wall.GetComponentsInChildren<Renderer>())
            {
                if (fadeMatActual != null) r.sharedMaterial = fadeMatActual;
                r.SetPropertyBlock(fadeMPB);
            }
        }
        activeWalls.Add(wall);
    }
}