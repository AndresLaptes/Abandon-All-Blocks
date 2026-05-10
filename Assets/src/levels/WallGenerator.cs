using System.Collections.Generic;
using UnityEngine;

public class WallGenerator : MonoBehaviour
{
    [Header("Prefabs de pared (Nivel 1)")]
    public GameObject paredSuelo;     // pared1_1 — primera fila visible encima del suelo
    public GameObject paredArribaA;   // pared1_2 — variante decorada
    public GameObject paredArribaB;   // pared1_3 — variante decorada
    public GameObject paredFade;      // pared1_4 — debajo del suelo, con shader de fade

    [Header("Puerta")]
    public GameObject puertaArriba;   // puerta1_1 — parte superior
    public GameObject puertaAbajo;    // puerta1_2 — parte inferior
    [Tooltip("Celda X (de -2 a +2) donde aparece la puerta en el muro del fondo. 0 = centro.")]
    [Range(-2, 2)] public int puertaCellX = 0;

    [Header("Configuración")]
    public float blocSize = 2f;
    [Tooltip("Filas extra de paredes encima de pared1_1 (con variantes decoradas posibles).")]
    [Range(0, 5)] public int filasArriba = 1;
    [Tooltip("Filas de pared1_4 debajo del suelo (zona de fade).")]
    [Range(0, 8)] public int filasAbajo = 4;

    [Tooltip("Solape entre paredes adyacentes para disimular costuras. 1.0 = sin solape.")]
    [Range(1f, 1.15f)] public float solapePared = 1.01f;

    [Tooltip("Probabilidad (0..1) de que una pared de fila superior sea decorada (1_2 o 1_3) en lugar de lisa (1_1).")]
    [Range(0f, 1f)] public float probabilidadDecoracion = 0.15f;

    [Header("Decoración - Antorchas")]
    public GameObject antorchaPrefab;
    [Tooltip("Cada cuántos tiles se coloca una antorcha en la pared izquierda. 0 = no colocar.")]
    [Range(0, 10)] public int separacionAntorchas = 4;
    [Tooltip("Altura (Y mundial) a la que se montan las antorchas en la pared.")]
    public float alturaAntorcha = 2.5f;
    [Tooltip("Rotación en Euler para orientar la antorcha respecto a la pared izquierda.")]
    public Vector3 antorchaRotacionEuler = new Vector3(0f, 90f, 0f);
    [Tooltip("Desplazamiento extra hacia el interior de la sala (eje X) desde la cara interior de la pared.")]
    public float antorchaOffsetInterior = 0f;
    [Tooltip("Escala aplicada a la antorcha. Útil si el modelo se exportó pequeño.")]
    public Vector3 antorchaEscala = new Vector3(3f, 3f, 3f);

    private List<GameObject> activeWalls = new List<GameObject>();

    private Vector3 nativeSize = Vector3.one;
    private bool nativeSizeCached = false;

    public void GenerarParedes(int sizeLevel)
    {
        LimpiarParedes();

        if (paredSuelo == null)
        {
            Debug.LogWarning("WallGenerator: falta asignar 'paredSuelo' (pared1_1).");
            return;
        }

        CacheNativeSize();

        bool widthIsZ = nativeSize.z >= nativeSize.x;
        Vector3 scale = Vector3.one;
        if (widthIsZ)
            scale.z = (blocSize / nativeSize.z) * solapePared;
        else
            scale.x = (blocSize / nativeSize.x) * solapePared;

        Quaternion rotFrontal = Quaternion.Euler(0f, 90f, 0f);

        // Suelo: el tile está centrado en y=0, escalado a blocSize → ocupa [-blocSize/2, +blocSize/2].
        float floorTopY = blocSize / 2f;
        float floorBottomY = -blocSize / 2f;

        bool generarPuerta = (puertaArriba != null && puertaAbajo != null);

        // ----- Encima del suelo -----
        // i=0 → pared1_1 (asentada justo sobre el suelo)
        // i>=1 → variantes decoradas (con probabilidad)
        int totalArriba = 1 + filasArriba;
        for (int i = 0; i < totalArriba; i++)
        {
            float wallCenterY = floorTopY + (i + 0.5f) * nativeSize.y;
            bool esBase = (i == 0);
            // En las dos primeras filas dejamos hueco para la puerta
            int skipBackX = (generarPuerta && (i == 0 || i == 1)) ? puertaCellX : int.MinValue;
            GenerarFila(sizeLevel, wallCenterY, scale, rotFrontal, esBase, false, skipBackX);
        }

        // ----- Fila de fade a la altura del suelo (donde arranca el degradado) -----
        GenerarFila(sizeLevel, 0f, scale, rotFrontal, false, true);

        // ----- Debajo del suelo (zona fade) -----
        for (int i = 0; i < filasAbajo; i++)
        {
            float wallCenterY = floorBottomY - (i + 0.5f) * nativeSize.y;
            GenerarFila(sizeLevel, wallCenterY, scale, rotFrontal, false, true);
        }

        // ----- Puerta -----
        if (generarPuerta)
            SpawnPuerta(sizeLevel, scale);

        // ----- Antorchas -----
        SpawnAntorchas(sizeLevel);
    }

    private void GenerarFila(int sizeLevel, float wallCenterY, Vector3 scale, Quaternion rotFrontal, bool esBase, bool esFade, int skipBackX = int.MinValue)
    {
        // Desplazamos las paredes hacia fuera media unidad de grosor para que no solapen con el suelo.
        float offset = nativeSize.x / 2f;

        // Pared lateral izquierda (desplazada en -X)
        for (int z = 0; z < sizeLevel; z++)
        {
            SpawnWall(ElegirPrefab(esBase, esFade), new Vector3(-2.5f * blocSize - offset, wallCenterY, z * blocSize), Quaternion.identity, scale);
        }

        // Pared del fondo (desplazada en +Z)
        for (int x = -2; x <= 2; x++)
        {
            if (x == skipBackX) continue; // hueco para la puerta
            SpawnWall(ElegirPrefab(esBase, esFade), new Vector3(x * blocSize, wallCenterY, (sizeLevel - 0.5f) * blocSize + offset), rotFrontal, scale);
        }

        // Bloque de esquina: rellena el hueco que dejan los offsets de las dos paredes.
        // El hueco es de tamaño nativeSize.x × nativeSize.y × nativeSize.x (medio grosor en X y Z).
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

        // Parent (sirve para destruirla con LimpiarParedes y para alojar el script Door)
        GameObject doorRoot = new GameObject("Door");
        doorRoot.transform.SetParent(transform);
        doorRoot.transform.position = new Vector3(xPos, 0f, zPos);

        Quaternion rotFrontal = Quaternion.Euler(0f, 90f, 0f);

        // Parte de abajo (puerta1_2) en la fila i=0
        float yAbajo = floorTopY + 0.5f * nativeSize.y;
        GameObject abajo = Instantiate(puertaAbajo, new Vector3(xPos, yAbajo, zPos), rotFrontal, doorRoot.transform);
        abajo.transform.localScale = scale;

        // Parte de arriba (puerta1_1) en la fila i=1
        float yArriba = floorTopY + 1.5f * nativeSize.y;
        GameObject arriba = Instantiate(puertaArriba, new Vector3(xPos, yArriba, zPos), rotFrontal, doorRoot.transform);
        arriba.transform.localScale = scale;

        // Lógica de paso de sala
        doorRoot.AddComponent<Door>();

        activeWalls.Add(doorRoot);
    }

    private GameObject ElegirPrefab(bool esBase, bool esFade)
    {
        if (esFade) return paredFade != null ? paredFade : paredSuelo;
        if (esBase) return paredSuelo;

        // Fila superior decorativa: mayoría lisa, ocasionalmente decorada.
        if (Random.value < probabilidadDecoracion && (paredArribaA != null || paredArribaB != null))
        {
            if (paredArribaA != null && paredArribaB != null)
                return Random.value < 0.5f ? paredArribaA : paredArribaB;
            return paredArribaA != null ? paredArribaA : paredArribaB;
        }
        return paredSuelo;
    }

    private void SpawnAntorchas(int sizeLevel)
    {
        if (antorchaPrefab == null || separacionAntorchas <= 0) return;

        float offset = nativeSize.x / 2f;
        // Cara interior de la pared izquierda (la que mira hacia el interior de la sala)
        float xMontaje = -2.5f * blocSize - offset + nativeSize.x + antorchaOffsetInterior;
        Quaternion rot = Quaternion.Euler(antorchaRotacionEuler);

        for (int z = separacionAntorchas / 2; z < sizeLevel; z += separacionAntorchas)
        {
            Vector3 pos = new Vector3(xMontaje, alturaAntorcha, z * blocSize);
            GameObject ant = Instantiate(antorchaPrefab, pos, rot, transform);
            ant.transform.localScale = antorchaEscala;
            activeWalls.Add(ant);
        }

        // Antorchas a los lados de la puerta (muro del fondo)
        if (puertaArriba != null && puertaAbajo != null)
        {
            float zMontaje = (sizeLevel - 0.5f) * blocSize + offset - nativeSize.x - antorchaOffsetInterior;
            // El muro del fondo apunta a -Z; rotamos 90° más respecto a la antorcha lateral.
            Quaternion rotFondo = Quaternion.Euler(antorchaRotacionEuler.x, antorchaRotacionEuler.y + 90f, antorchaRotacionEuler.z);

            int[] ladoCells = { puertaCellX - 1, puertaCellX + 1 };
            foreach (int cellX in ladoCells)
            {
                if (cellX < -2 || cellX > 2) continue; // solo dentro del rango del muro
                Vector3 pos = new Vector3(cellX * blocSize, alturaAntorcha, zMontaje);
                GameObject ant = Instantiate(antorchaPrefab, pos, rotFondo, transform);
                ant.transform.localScale = antorchaEscala;
                activeWalls.Add(ant);
            }
        }
    }

    public void LimpiarParedes()
    {
        foreach (GameObject wall in activeWalls)
            if (wall != null) Destroy(wall);
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
            foreach (Renderer r in renderers)
                b.Encapsulate(r.bounds);
            nativeSize = b.size;
        }
        else
        {
            nativeSize = Vector3.one;
            Debug.LogWarning("WallGenerator: el prefab de referencia no tiene Renderer, usando 1×1×1.");
        }

        Destroy(temp);
        nativeSizeCached = true;
        Debug.Log($"WallGenerator: tamaño nativo detectado = {nativeSize}");
    }

    private void SpawnWall(GameObject prefab, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        if (prefab == null) return;
        GameObject wall = Instantiate(prefab, position, rotation, transform);
        wall.transform.localScale = scale;
        activeWalls.Add(wall);
    }
}
