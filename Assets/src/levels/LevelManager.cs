using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class DecoracionEntry
{
    public GameObject prefab;
    public Vector3 escala = new Vector3(3f, 3f, 3f);
    public Vector3 rotacion = new Vector3(0f, 180f, 0f);
    [Tooltip("Ajuste fino sobre la posición auto-calculada (base del modelo apoyada en el suelo).")]
    public Vector3 offset = Vector3.zero;
}

public class LevelManager : MonoBehaviour
{
    [Header("Configuración")]
    public GameObject voxelPrefab;
    public GameObject player;
    public float blocSize = 2f;

    [Header("Configuración de Niveles")]
    public int nivelInicialParaProbar = 1; 

    [Header("Referencias Externas")]
    public CameraFramer camara;
    public WallGenerator wallGenerator;
    public EnemySpawner enemySpawner;

    [Header("Decoraciones / Obstáculos")]
    [Tooltip("Índice 0 -> valor 2 en la matriz, índice 1 -> valor 3, etc. Cada entrada lleva su propia escala y rotación.")]
    public DecoracionEntry[] decoraciones;

    private LevelData[] nivelesJuego;
    private Queue<GameObject> tilePool = new Queue<GameObject>();
    private List<GameObject> activeTilesInRoom = new List<GameObject>();
    private List<GameObject> activeDecorationsInRoom = new List<GameObject>();
    private int actualLevelIndex = 0;

    private int[,] flowField;
    private float flowFieldTimer = 0f;

    void Awake()
    {
        List<LevelData> listaNiveles = new List<LevelData>();
        int i = 1;
        while (true)
        {
            LevelData[] nivelesEnCarpeta = Resources.LoadAll<LevelData>("Nivel" + i);
            if (nivelesEnCarpeta.Length == 0) break;
            listaNiveles.AddRange(nivelesEnCarpeta);
            i++;
        }
        nivelesJuego = listaNiveles.OrderBy(nivel => nivel.roomNumber).ToArray();

        actualLevelIndex = nivelInicialParaProbar - 1;
        if (actualLevelIndex < 0) actualLevelIndex = 0;

        if (decoraciones == null || decoraciones.Length == 0)
        {
            decoraciones = Resources.LoadAll<GameObject>("Objects")
                .OrderBy(o => o.name)
                .Select(o => new DecoracionEntry { prefab = o })
                .ToArray();
            Debug.Log($"LevelManager: {decoraciones.Length} decoraciones auto-cargadas desde Resources/Objects/ -> [{string.Join(", ", decoraciones.Select(e => e.prefab.name))}]");
        }
    }

    void Start()
    {
        cargarSigueinteNivel();
    }

    void Update()
    {
        if (nivelesJuego == null || nivelesJuego.Length == 0 || player == null) return;
        
        flowFieldTimer += Time.deltaTime;
        if (flowFieldTimer >= 0.15f)
        {
            GenerarFloodField();
            flowFieldTimer = 0f;
        }
    }

    public void cargarSigueinteNivel()
    {
        if (actualLevelIndex >= nivelesJuego.Length) return;

        LevelData datoSala = nivelesJuego[actualLevelIndex];
        string carpetaActual = "Nivel" + (actualLevelIndex + 1);

        GameObject nuevoSuelo = Resources.Load<GameObject>($"{carpetaActual}/floor");
        if (nuevoSuelo != null && voxelPrefab != nuevoSuelo)
        {
            DestruirPoolActual();
            voxelPrefab = nuevoSuelo;
        }

        if (wallGenerator != null)
        {
            wallGenerator.CargarAssetsDesdeCarpeta(carpetaActual);
        }

        foreach (GameObject tileViejo in activeTilesInRoom)
        {
            tileViejo.SetActive(false);
            tilePool.Enqueue(tileViejo);
        }
        activeTilesInRoom.Clear();

        foreach (GameObject deco in activeDecorationsInRoom) if (deco != null) Destroy(deco);
        activeDecorationsInRoom.Clear();

        int numCubos = (datoSala.sizeLevel * 5) + 1; 
        EnsurePoolCapacity(numCubos);
        
        GenerateRoom(datoSala); 
        PosicionarJugador();

        camara.IniciarSeguimiento(player.transform, blocSize, datoSala.sizeLevel);
        camara.SetFondo(datoSala.colorFondo);

        if (wallGenerator != null)
        {
            wallGenerator.blocSize = blocSize;
            wallGenerator.fadeColorFondo = datoSala.colorFondo;
            wallGenerator.fadeTint = datoSala.fadeTint;
            wallGenerator.paredesApiladas = datoSala.paredesApiladas;
            wallGenerator.GenerarParedes(datoSala.sizeLevel);
        }

        if (enemySpawner != null)
        {
            enemySpawner.GenerarEnemigos(datoSala, blocSize, this);
        }

        actualLevelIndex++;
    }

    public void GenerarFloodField()
    {
        if (actualLevelIndex < 1) return;
        
        LevelData datoSala = nivelesJuego[actualLevelIndex - 1];
        int columnas = 5;
        int filas = datoSala.sizeLevel;
        
        flowField = new int[columnas, filas];
        
        for (int x = 0; x < columnas; x++)
        {
            for (int z = 0; z < filas; z++)
            {
                flowField[x, z] = 255; 
            }
        }
        
        int playerGridX = Mathf.RoundToInt(player.transform.position.x / blocSize) + 2;
        int playerGridZ = Mathf.RoundToInt(player.transform.position.z / blocSize);
        
        
        playerGridX = Mathf.Clamp(playerGridX, 0, columnas - 1);
        playerGridZ = Mathf.Clamp(playerGridZ, 0, filas - 1);
        
        Queue<Vector2Int> cola = new Queue<Vector2Int>();
        cola.Enqueue(new Vector2Int(playerGridX, playerGridZ));
        flowField[playerGridX, playerGridZ] = 0; 
        
        Vector2Int[] direcciones = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        
        while (cola.Count > 0)
        {
            Vector2Int actual = cola.Dequeue();
            int costoActual = flowField[actual.x, actual.y];
            
            foreach (Vector2Int dir in direcciones)
            {
                Vector2Int vecino = actual + dir;
                
                if (vecino.x >= 0 && vecino.x < columnas && vecino.y >= 0 && vecino.y < filas)
                {
                    Vector3 posMundo = new Vector3((vecino.x - 2) * blocSize, 0f, vecino.y * blocSize);
                    
                    if (ExisteSueloEn(posMundo) && !EsPared(posMundo))
                    {
                        if (flowField[vecino.x, vecino.y] > costoActual + 1)
                        {
                            flowField[vecino.x, vecino.y] = costoActual + 1;
                            cola.Enqueue(vecino);
                        }
                    }
                }
            }
        }
    }

    public int ObtenerCostoFloodField(Vector3 posicion)
    {
        if (flowField == null) return 255;
        
        int gridX = Mathf.RoundToInt(posicion.x / blocSize) + 2;
        int gridZ = Mathf.RoundToInt(posicion.z / blocSize);
        
        LevelData datoSala = nivelesJuego[actualLevelIndex - 1];
        
        if (gridX >= 0 && gridX < 5 && gridZ >= 0 && gridZ < datoSala.sizeLevel)
        {
            return flowField[gridX, gridZ];
        }
        
        return 255;
    }

    private void DestruirPoolActual()
    {
        foreach (GameObject tile in activeTilesInRoom) if (tile != null) Destroy(tile);
        foreach (GameObject tile in tilePool) if (tile != null) Destroy(tile);
        activeTilesInRoom.Clear();
        tilePool.Clear();
    }

    public void PosicionarJugador()
    {
        if (activeTilesInRoom.Count == 0) return;
        GridMovement mov = player.GetComponent<GridMovement>();
        if (mov != null) mov.ResetearEstado();
        GameObject losaSpawn = activeTilesInRoom[0];
        Renderer rendererLosa = losaSpawn.GetComponentInChildren<Renderer>();
        float techoDelSuelo = rendererLosa.bounds.max.y;
        float mitadAlturaJugador = 0.5f;
        Renderer rendererJugador = player.GetComponentInChildren<Renderer>();
        if (rendererJugador != null) mitadAlturaJugador = rendererJugador.bounds.size.y / 2f;
        player.transform.position = new Vector3(0f, techoDelSuelo + mitadAlturaJugador, -1f * blocSize);
        if (mov != null) mov.ConfigurarPaso(blocSize);
    }

    private void EnsurePoolCapacity(int requiredCapacity)
    {
        int total = tilePool.Count + activeTilesInRoom.Count;
        if (total < requiredCapacity)
        {
            int amount = requiredCapacity - total;
            for (int i = 0; i < amount; i++)
            {
                GameObject tile = Instantiate(voxelPrefab);
                tile.transform.localScale = new Vector3(blocSize, blocSize, blocSize);
                tile.SetActive(false);
                tile.transform.SetParent(transform);
                tilePool.Enqueue(tile);
            }
        }
    }

    private void GenerateRoom(LevelData datoSala)
    {
        Vector3 posSpawn = new Vector3(0f, 0f, -1f * blocSize);
        SpawnTileFromPool(posSpawn);
        if (datoSala.filas != null)
        {
            for (int z = 0; z < datoSala.filas.Length; z++)
            {
                for (int x = 0; x < 5; x++)
                {
                    int valor = datoSala.filas[z].columnas[x];
                    if (valor < 1) continue;
                    Vector3 pos = new Vector3((x - 2) * blocSize, 0, z * blocSize);
                    GameObject tile = SpawnTileFromPool(pos);
                    if (valor >= 2 && tile != null) SpawnDecoracion(valor - 2, tile);
                }
            }
        }
    }

    private GameObject SpawnTileFromPool(Vector3 position)
    {
        if (tilePool.Count == 0) return null;
        GameObject tileObj = tilePool.Dequeue();
        tileObj.transform.position = position;
        tileObj.SetActive(true);
        activeTilesInRoom.Add(tileObj);
        return tileObj;
    }

    private void SpawnDecoracion(int prefabIndex, GameObject tileObj)
    {
        if (decoraciones == null || prefabIndex < 0 || prefabIndex >= decoraciones.Length)
        {
            Debug.LogWarning($"LevelManager: valor de decoración {prefabIndex + 2} sin entrada asignada (array tiene {(decoraciones == null ? 0 : decoraciones.Length)} elementos).");
            return;
        }
        DecoracionEntry entry = decoraciones[prefabIndex];
        if (entry == null || entry.prefab == null)
        {
            Debug.LogWarning($"LevelManager: el slot {prefabIndex} de decoraciones está vacío.");
            return;
        }

        Renderer rTile = tileObj.GetComponentInChildren<Renderer>();
        float topY = rTile != null ? rTile.bounds.max.y : tileObj.transform.position.y + blocSize / 2f;

        Vector3 spawnXZ = new Vector3(tileObj.transform.position.x + entry.offset.x, topY, tileObj.transform.position.z + entry.offset.z);
        GameObject deco = Instantiate(entry.prefab, spawnXZ, Quaternion.Euler(entry.rotacion), transform);
        deco.transform.localScale = Vector3.Scale(deco.transform.localScale, entry.escala);

        Renderer[] decoRenderers = deco.GetComponentsInChildren<Renderer>();
        if (decoRenderers.Length > 0)
        {
            Bounds b = decoRenderers[0].bounds;
            foreach (Renderer r in decoRenderers) b.Encapsulate(r.bounds);
            float deltaY = topY - b.min.y + entry.offset.y;
            deco.transform.position += new Vector3(0f, deltaY, 0f);
        }

        activeDecorationsInRoom.Add(deco);
    }

    public void ReturnTileToPool(GameObject tile) { tile.SetActive(false); tilePool.Enqueue(tile); }

    public bool ExisteSueloEn(Vector3 destino)
    {
        int gridX = Mathf.RoundToInt(destino.x / blocSize);
        int gridZ = Mathf.RoundToInt(destino.z / blocSize);
        int index = actualLevelIndex - 1;
        if (index >= 0 && index < nivelesJuego.Length)
        {
            if (gridZ == nivelesJuego[index].sizeLevel && wallGenerator != null && gridX == wallGenerator.puertaCellX) return true;
        }
        foreach (GameObject tile in activeTilesInRoom)
            if (Mathf.Abs(tile.transform.position.x - destino.x) < 0.1f && Mathf.Abs(tile.transform.position.z - destino.z) < 0.1f) return true;
        return false;
    }

    public bool EsCeldaPuerta(Vector3 destino)
    {
        if (wallGenerator == null) return false;
        int index = actualLevelIndex - 1;
        if (index < 0 || index >= nivelesJuego.Length) return false;
        int gridX = Mathf.RoundToInt(destino.x / blocSize);
        int gridZ = Mathf.RoundToInt(destino.z / blocSize);
        return gridZ == nivelesJuego[index].sizeLevel && gridX == wallGenerator.puertaCellX;
    }

    public void IniciarAperturaPuerta()
    {
        if (wallGenerator != null && wallGenerator.doorActual != null)
            wallGenerator.doorActual.IniciarApertura();
    }

    public bool EsPared(Vector3 destino)
    {
        int gridX = Mathf.RoundToInt(destino.x / blocSize);
        int gridZ = Mathf.RoundToInt(destino.z / blocSize);
        int index = actualLevelIndex - 1;
        if (index < 0 || index >= nivelesJuego.Length) return false;
        int size = nivelesJuego[index].sizeLevel;
        if (gridX < -2 || gridX > 2) return true;
        if (gridZ >= size) return !(gridZ == size && wallGenerator != null && gridX == wallGenerator.puertaCellX);
        return false;
    }
}