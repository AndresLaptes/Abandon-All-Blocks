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
    public string carpetaNiveles = "Nivel1";
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
        LevelData[] nivelesDesordenados = Resources.LoadAll<LevelData>(carpetaNiveles);

        nivelesJuego = nivelesDesordenados.OrderBy(nivel => nivel.roomNumber).ToArray();

        if (nivelesJuego.Length == 0)
        {
            Debug.LogError($"No he encontrado niveles en la carpeta Resources/{carpetaNiveles}!");
        }

        GameObject sueloDinamico = Resources.Load<GameObject>($"{carpetaNiveles}/floor");
        
        if (sueloDinamico != null)
        {
            voxelPrefab = sueloDinamico; 
            Debug.Log($"Suelo '{carpetaNiveles}/floor' cargado automáticamente.");
        }
        else
        {
            Debug.LogWarning($"No hay un prefab llamado 'floor' en Resources/{carpetaNiveles}. Usaré el cubo por defecto.");
        }

        actualLevelIndex = nivelInicialParaProbar - 1;
        if (actualLevelIndex < 0) actualLevelIndex = 0;
    }

    void Start()
    {
        cargarSigueinteNivel();
    }

    public void cargarSigueinteNivel()
    {
        if (actualLevelIndex >= nivelesJuego.Length)
        {
            Debug.Log("¡Has completado el último nivel de esta carpeta!");
            return;
        }

        foreach (GameObject tileViejo in activeTilesInRoom)
        {
            tileViejo.SetActive(false);
            tilePool.Enqueue(tileViejo);
        }
        activeTilesInRoom.Clear();

        LevelData datoSala = nivelesJuego[actualLevelIndex];

        int numCubos = (datoSala.sizeLevel * 5) + 1; 
        EnsurePoolCapacity(numCubos);
        
        GenerateRoom(datoSala.sizeLevel); 
        
        PosicionarJugador();

        camara.IniciarSeguimiento(player.transform, blocSize, datoSala.sizeLevel);

        if (wallGenerator != null)
        {
            wallGenerator.blocSize = blocSize;
            wallGenerator.GenerarParedes(datoSala.sizeLevel);
        }

        actualLevelIndex++;
    }

    public void PosicionarJugador()
    {
        if (activeTilesInRoom.Count == 0) return;

        GameObject losaSpawn = activeTilesInRoom[0];
        Renderer rendererLosa = losaSpawn.GetComponentInChildren<Renderer>();
        float techoDelSuelo = rendererLosa.bounds.max.y;
        float mitadAlturaJugador = 0.5f; 

        Renderer rendererJugador = player.GetComponentInChildren<Renderer>();
        if (rendererJugador != null)
        {
            mitadAlturaJugador = rendererJugador.bounds.size.y / 2f;
        }

        float alturaPerfecta = techoDelSuelo + mitadAlturaJugador;
        player.transform.position = new Vector3(0f, alturaPerfecta, -1f * blocSize);
        
        GridMovement movimientoJugador = player.GetComponent<GridMovement>();
        if (movimientoJugador != null)
        {
            movimientoJugador.ConfigurarPaso(blocSize);
        }
    }

    private void EnsurePoolCapacity(int requiredCapacity)
    {
        int currentTotalCubes = tilePool.Count + activeTilesInRoom.Count;
        if (currentTotalCubes < requiredCapacity)
        {
            int amountToCreate = requiredCapacity - currentTotalCubes;
            
            for (int i = 0; i < amountToCreate; i++)
            {
                GameObject tile = Instantiate(voxelPrefab);
                tile.transform.localScale = new Vector3(blocSize, blocSize, blocSize);
                tile.SetActive(false);
                tile.transform.SetParent(this.transform);
                tilePool.Enqueue(tile);
            }
        }
    }

    private void GenerateRoom(int length)
    {
        activeTilesInRoom.Clear();
        Vector3 posicionSpawn = new Vector3(0f, 0f, -1f * blocSize);
        SpawnTileFromPool(posicionSpawn);
        
        for (int z = 0; z < length; z++)
        {
            for (int x = -2; x <= 2; x++)
            {
                Vector3 spawnPos = new Vector3(x * blocSize, 0, z * blocSize);
                SpawnTileFromPool(spawnPos);
            }
        }
    }

    private void SpawnTileFromPool(Vector3 position)
    {
        GameObject tileObj = tilePool.Dequeue();
        tileObj.transform.position = position;
        tileObj.SetActive(true);
        activeTilesInRoom.Add(tileObj);
    }

    public void ReturnTileToPool(GameObject tile)
    {
        tile.SetActive(false);
        tilePool.Enqueue(tile);
    }
    
    public bool ExisteSueloEn(Vector3 destino)
    {
        int gridX = Mathf.RoundToInt(destino.x / blocSize);
        int gridZ = Mathf.RoundToInt(destino.z / blocSize);
        int indiceNivel = actualLevelIndex - 1;

       
        if (indiceNivel >= 0 && indiceNivel < nivelesJuego.Length)
        {
            int sizeLevel = nivelesJuego[indiceNivel].sizeLevel;
            if (gridZ == sizeLevel && wallGenerator != null && gridX == wallGenerator.puertaCellX)
            {
                return true; 
            }
        }

        foreach (GameObject tile in activeTilesInRoom)
        {
            if (Mathf.Abs(tile.transform.position.x - destino.x) < 0.1f &&
                Mathf.Abs(tile.transform.position.z - destino.z) < 0.1f)
            {
                return true;
            }
        }
        return false; 
    }

    public bool EsPared(Vector3 destino)
    {
        int gridX = Mathf.RoundToInt(destino.x / blocSize);
        int gridZ = Mathf.RoundToInt(destino.z / blocSize);

        int indiceNivel = actualLevelIndex - 1;
        if (indiceNivel < 0 || indiceNivel >= nivelesJuego.Length) return false;

        int sizeLevel = nivelesJuego[indiceNivel].sizeLevel;

       
        if (gridX < -2) return true;

       
        if (gridZ >= sizeLevel)
        {
            if (gridZ == sizeLevel && wallGenerator != null && gridX == wallGenerator.puertaCellX)
            {
                return false;
            }
            return true; 
        }

        
        return false;
    }
}