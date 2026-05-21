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

    [Header("Configuración de Monedas")]
    public GameObject prefabMoneda;
    public float offsetAlturaMonedas = 1.5f;

    [Header("Referencias Externas")]
    public CameraFramer camara;
    public WallGenerator wallGenerator;
    public EnemySpawner enemySpawner;
    public TrampaSpawner trampaSpawner;

    private LevelData[] nivelesJuego;
    private Queue<GameObject> tilePool = new Queue<GameObject>();
    private List<GameObject> activeTilesInRoom = new List<GameObject>();
    private List<GameObject> monedasActivas = new List<GameObject>();
    private int actualLevelIndex = 0;

    private int[,] flowField;
    private float flowFieldTimer = 0f;

    private HUDContadorSala hudSala;

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
        hudSala = FindObjectOfType<HUDContadorSala>();
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

        for (int i = 0; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
            {
                int targetIndex = (i == 0) ? 9 : i - 1; 
                
                if (targetIndex >= 0 && targetIndex < nivelesJuego.Length)
                {
                    actualLevelIndex = targetIndex;
                    cargarSigueinteNivel();
                }
            }
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

        LimpiarMonedas();

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
        camara.SetFondo(datoSala.colorFondo);

        if (wallGenerator != null)
        {
            wallGenerator.blocSize = blocSize;
            wallGenerator.fadeColorFondo = datoSala.colorFondo;
            wallGenerator.fadeTint = Color.white; // UnlitFadeShade ya aplica fake-shading; el brillo lo determina la normal de cada cara
            wallGenerator.paredesApiladas = datoSala.paredesApiladas;
            wallGenerator.GenerarParedes(datoSala.sizeLevel);
        }

        if (enemySpawner != null)
        {
            enemySpawner.GenerarEnemigos(datoSala, blocSize, this);
        }

        if (trampaSpawner != null)
        {
            trampaSpawner.GenerarTrampas(datoSala, blocSize, this);
        }

        GenerarMonedas(datoSala);

        if (hudSala != null)
        {
            hudSala.ActualizarSala(datoSala.roomNumber);
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

    private void LimpiarMonedas()
    {
        foreach (GameObject moneda in monedasActivas) if (moneda != null) Destroy(moneda);
        monedasActivas.Clear();
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

    private void GenerarMonedas(LevelData datoSala)
    {
        if (prefabMoneda == null || activeTilesInRoom.Count == 0) return;

        List<GameObject> casillasVacias = new List<GameObject>();

        foreach (GameObject tile in activeTilesInRoom)
        {
            Vector3 pos = tile.transform.position;
            
            if (pos.z < 0) continue; 
            if (EsPared(pos)) continue;
            if (enemySpawner != null && enemySpawner.HayEnemigoEn(pos, null)) continue;
            
            casillasVacias.Add(tile);
        }

        for (int i = 0; i < datoSala.numCoins; i++)
        {
            if (casillasVacias.Count == 0) break;

            int rnd = Random.Range(0, casillasVacias.Count);
            GameObject tileElegido = casillasVacias[rnd];
            casillasVacias.RemoveAt(rnd);

            float alturaSuelo = tileElegido.transform.position.y + offsetAlturaMonedas; 
            Vector3 posSpawn = new Vector3(tileElegido.transform.position.x, alturaSuelo, tileElegido.transform.position.z);
            
            GameObject nuevaMoneda = Instantiate(prefabMoneda, posSpawn, Quaternion.identity);
            monedasActivas.Add(nuevaMoneda);
        }
    }
}