using System.Collections.Generic;
using UnityEngine;

public class WallGenerator : MonoBehaviour
{
    [Header("Prefab de pared")]
    public GameObject wallPrefab;

    [Header("Configuración")]
    public float blocSize = 2f;

    private List<GameObject> activeWalls = new List<GameObject>();

    // Tamaño real del prefab medido en el primer uso.
    private Vector3 nativeSize = Vector3.one;
    private bool nativeSizeCached = false;

    public void GenerarParedes(int sizeLevel)
    {
        LimpiarParedes();

        if (wallPrefab == null)
        {
            Debug.LogWarning("WallGenerator: falta asignar el wallPrefab.");
            return;
        }

        CacheNativeSize();

        // El prefab puede tener Z o X como eje de "ancho de cara".
        // Consideramos que el eje de mayor tamaño horizontal es el ancho.
        bool widthIsZ = nativeSize.z >= nativeSize.x;

        // Cada instancia de pared debería ocupar exactamente 1 tile de ancho (blocSize).
        // La altura no se escala (caída infinita).
        // El grosor tampoco se escala (queda igual que en MagicaVoxel).
        Vector3 scale = Vector3.one;
        if (widthIsZ)
            scale.z = blocSize / nativeSize.z;   // ancho = 1 tile
        else
            scale.x = blocSize / nativeSize.x;

        // Filas verticales: 1 arriba (row=1), la del suelo (row=0), 2 abajo (row=-1..-2)
        Quaternion rotFrontal = Quaternion.Euler(0f, 90f, 0f);

        for (int row = 1; row >= -2; row--)
        {
            float wallCenterY = blocSize / 2f + (row - 0.5f) * nativeSize.y;

            // Pared lateral izquierda
            for (int z = 0; z < sizeLevel; z++)
            {
                float posZ = z * blocSize;
                SpawnWall(new Vector3(-2.5f * blocSize, wallCenterY, posZ), Quaternion.identity, scale);
            }

            // Pared del fondo
            for (int x = -2; x <= 2; x++)
                SpawnWall(new Vector3(x * blocSize, wallCenterY, (sizeLevel - 0.5f) * blocSize), rotFrontal, scale);
        }
    }

    public void LimpiarParedes()
    {
        foreach (GameObject wall in activeWalls)
            if (wall != null) Destroy(wall);
        activeWalls.Clear();
    }

    // Instancia el prefab una sola vez para medir sus bounds reales, luego lo destruye.
    private void CacheNativeSize()
    {
        if (nativeSizeCached) return;

        GameObject temp = Instantiate(wallPrefab);
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
            Debug.LogWarning("WallGenerator: el wallPrefab no tiene Renderer, usando tamaño 1×1×1.");
        }

        Destroy(temp);
        nativeSizeCached = true;
        Debug.Log($"WallGenerator: tamaño nativo detectado = {nativeSize}");
    }

    private void SpawnWall(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        GameObject wall = Instantiate(wallPrefab, position, rotation, transform);
        wall.transform.localScale = scale;
        activeWalls.Add(wall);
    }
}
