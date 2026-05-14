using System.Collections;
using System.Collections.Generic;
using System.Linq; 
using UnityEngine;

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

    private LevelData[] nivelesJuego;
    private Queue<GameObject> tilePool = new Queue<GameObject>();
    private List<GameObject> activeTilesInRoom = new List<GameObject>();
    private int actualLevelIndex = 0;

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
    }

    void Start()
    {
        cargarSigueinteNivel();
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

        int numCubos = (datoSala.sizeLevel * 5) + 1; 
        EnsurePoolCapacity(numCubos);
        
        GenerateRoom(datoSala); 
        PosicionarJugador();

        camara.IniciarSeguimiento(player.transform, blocSize, datoSala.sizeLevel);

        if (wallGenerator != null)
        {
            wallGenerator.blocSize = blocSize;
            wallGenerator.GenerarParedes(datoSala.sizeLevel);
        }

        actualLevelIndex++;
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
        GameObject losaSpawn = activeTilesInRoom[0];
        Renderer rendererLosa = losaSpawn.GetComponentInChildren<Renderer>();
        float techoDelSuelo = rendererLosa.bounds.max.y;
        float mitadAlturaJugador = 0.5f; 
        Renderer rendererJugador = player.GetComponentInChildren<Renderer>();
        if (rendererJugador != null) mitadAlturaJugador = rendererJugador.bounds.size.y / 2f;
        player.transform.position = new Vector3(0f, techoDelSuelo + mitadAlturaJugador, -1f * blocSize);
        GridMovement mov = player.GetComponent<GridMovement>();
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
                    if (datoSala.filas[z].columnas[x] == 1)
                        SpawnTileFromPool(new Vector3((x - 2) * blocSize, 0, z * blocSize));
                }
            }
        }
    }

    private void SpawnTileFromPool(Vector3 position)
    {
        if (tilePool.Count == 0) return;
        GameObject tileObj = tilePool.Dequeue();
        tileObj.transform.position = position;
        tileObj.SetActive(true);
        activeTilesInRoom.Add(tileObj);
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