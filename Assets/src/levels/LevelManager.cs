using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [Header("Configuración")]
    public GameObject voxelPrefab;
    public GameObject player;
    public float blocSize = 2f;
    
    public float offsetAlturaJugador = 0f; 

    [Header("Configuración de Niveles")]
    public int nivelInicialParaProbar = 1; 

    [Header("Configuración de Monedas")]
    public GameObject prefabMoneda;
    public float offsetAlturaMonedas = 1.5f;

    [Header("Decoración de Esquinas")]
    [Tooltip("Escala uniforme aplicada a las estatuas de esquina.")]
    public Vector3 escalaEstatuas = new Vector3(0.5f, 0.5f, 0.5f);
    [Tooltip("Desplazamiento dentro de la celda hacia la esquina exterior (0 = centro, 0.5 = borde).")]
    public float offsetEstatuaEnEsquina = 0.3f;

    [Header("Niebla")]
    [Tooltip("Material URP Transparent (Unlit) que se aplica a las nubes con tinte por nivel. Si null, usa el material del OBJ.")]
    public Material materialNiebla;
    [Tooltip("Tinta multiplicada por colorFondo del nivel. Alpha controla translucidez.")]
    public Color tintaNiebla = new Color(1.2f, 0.5f, 0.4f, 0.35f);
    [Tooltip("Cantidad de nubes a colocar por nivel.")]
    public int numNubes = 12;
    [Tooltip("Escala uniforme base de cada nube.")]
    public float escalaNubeBase = 0.4f;
    [Tooltip("Variación aleatoria de escala (±) sobre la base.")]
    public float variacionEscalaNube = 0.15f;
    [Tooltip("Fracción de la altura de la nube que queda enterrada en el suelo (0 a 1).")]
    public float fraccionEnterradaNube = 0.4f;

    [Header("Referencias Externas")]
    public CameraFramer camara;
    public WallGenerator wallGenerator;
    public EnemySpawner enemySpawner;
    public TrampaSpawner trampaSpawner;

    private LevelData[] nivelesJuego;
    private Queue<GameObject> tilePool = new Queue<GameObject>();
    private List<GameObject> activeTilesInRoom = new List<GameObject>();
    private List<GameObject> monedasActivas = new List<GameObject>();
    private List<GameObject> estatuasActivas = new List<GameObject>();
    private List<GameObject> prefabsEstatuas = new List<GameObject>();
    private List<GameObject> nubesActivas = new List<GameObject>();
    private GameObject prefabNube;
    private int actualLevelIndex = 0;

    private int[,] flowField;
    private float flowFieldTimer = 0f;

    private Coroutine rutinaCaidaSuelo;
    private List<Coroutine> caidasDiferidas = new List<Coroutine>();

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

        prefabsEstatuas.Clear();
        GameObject e1 = Resources.Load<GameObject>("Objects/estatua1");
        GameObject e2 = Resources.Load<GameObject>("Objects/estatua2");
        GameObject e3 = Resources.Load<GameObject>("Objects/estatua3");
        if (e1 != null) prefabsEstatuas.Add(e1);
        if (e2 != null) prefabsEstatuas.Add(e2);
        if (e3 != null) prefabsEstatuas.Add(e3);
        Debug.Log($"LevelManager: estatuas cargadas = {prefabsEstatuas.Count} (e1={(e1 != null)}, e2={(e2 != null)}, e3={(e3 != null)})");

        prefabNube = Resources.Load<GameObject>("Objects/nube");

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

        ChequearSueloBajoPlayer();
    }

    private void ChequearSueloBajoPlayer()
    {
        if (rutinaCaidaSuelo == null) return;
        if (player == null) return;

        GridMovement mov = player.GetComponent<GridMovement>();
        if (mov == null || mov.IsDead() || mov.IsMoving()) return;

        Vector3 p = player.transform.position;
        Vector3 celdaAlineada = new Vector3(
            Mathf.Round(p.x / blocSize) * blocSize,
            p.y,
            Mathf.Round(p.z / blocSize) * blocSize
        );

        if (!ExisteSueloEn(celdaAlineada))
        {
            mov.CaerEnSitio();
        }
    }

    public void cargarSigueinteNivel()
    {
        if (actualLevelIndex >= nivelesJuego.Length)
        {
            SceneManager.LoadScene("Credits");
            return;
        }

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
            wallGenerator.fadeTint = Color.white; 
            wallGenerator.paredesApiladas = datoSala.paredesApiladas;
            wallGenerator.GenerarParedes(datoSala.sizeLevel);
        }

        GenerarEstatuasEsquinas(datoSala);

        if (enemySpawner != null)
        {
            enemySpawner.GenerarEnemigos(datoSala, blocSize, this);
        }

        if (trampaSpawner != null)
        {
            trampaSpawner.GenerarTrampas(datoSala, blocSize, this);
        }

        if (rutinaCaidaSuelo != null) { StopCoroutine(rutinaCaidaSuelo); rutinaCaidaSuelo = null; }
        foreach (Coroutine c in caidasDiferidas) if (c != null) StopCoroutine(c);
        caidasDiferidas.Clear();

        if (datoSala.floorFall)
        {
            rutinaCaidaSuelo = StartCoroutine(RutinaCaidaDeFilas(datoSala));
        }

        GenerarMonedas(datoSala);
        GenerarNiebla(datoSala);

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
                    
                    if (ExisteSueloEn(posMundo) && !EsPared(posMundo) && !HayEstatuaEn(posMundo))
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

    private void LimpiarEstatuas()
    {
        foreach (GameObject est in estatuasActivas) if (est != null) Destroy(est);
        estatuasActivas.Clear();
    }

    private void LimpiarNubes()
    {
        foreach (GameObject n in nubesActivas) if (n != null) Destroy(n);
        nubesActivas.Clear();
    }

    private void GenerarNiebla(LevelData datoSala)
    {
        LimpiarNubes();
        if (prefabNube == null) return;
        if (datoSala.filas == null || datoSala.sizeLevel < 1) return;

        float techoSuelo = blocSize / 2f;
        if (activeTilesInRoom.Count > 0)
        {
            Renderer rTile = activeTilesInRoom[0].GetComponentInChildren<Renderer>();
            if (rTile != null) techoSuelo = rTile.bounds.max.y;
        }

        Color tinte = datoSala.colorFondo * tintaNiebla;
        tinte.a = tintaNiebla.a;

        MaterialPropertyBlock mpb = new MaterialPropertyBlock();

        List<Vector2Int> celdas = new List<Vector2Int>(5 * datoSala.sizeLevel);
        for (int x = 0; x < 5; x++)
            for (int z = 0; z < datoSala.sizeLevel; z++)
                celdas.Add(new Vector2Int(x, z));

        for (int i = 0; i < celdas.Count; i++)
        {
            int r = Random.Range(i, celdas.Count);
            Vector2Int tmp = celdas[i]; celdas[i] = celdas[r]; celdas[r] = tmp;
        }

        int total = Mathf.Min(numNubes, celdas.Count);
        for (int n = 0; n < total; n++)
        {
            int gx = celdas[n].x;
            int gz = celdas[n].y;

            float jitterX = (Random.value - 0.5f) * blocSize * 0.6f;
            float jitterZ = (Random.value - 0.5f) * blocSize * 0.6f;
            float worldX = (gx - 2) * blocSize + jitterX;
            float worldZ = gz * blocSize + jitterZ;

            float escala = escalaNubeBase + (Random.value - 0.5f) * 2f * variacionEscalaNube;
            float rotY = Random.Range(0, 4) * 90f;

            GameObject inst = Instantiate(prefabNube, new Vector3(worldX, techoSuelo, worldZ), Quaternion.Euler(0f, rotY, 0f), transform);
            inst.transform.localScale = new Vector3(escala, escala, escala);

            Renderer[] rs = inst.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                Bounds b = rs[0].bounds;
                foreach (Renderer r in rs) b.Encapsulate(r.bounds);
                float altura = b.size.y;
                float deltaY = techoSuelo - b.min.y - altura * fraccionEnterradaNube;
                inst.transform.position += new Vector3(0f, deltaY, 0f);

                foreach (Renderer r in rs)
                {
                    if (materialNiebla != null) r.sharedMaterial = materialNiebla;
                    r.GetPropertyBlock(mpb);
                    mpb.SetColor("_BaseColor", tinte);
                    mpb.SetColor("_Color", tinte);
                    r.SetPropertyBlock(mpb);
                }
            }

            nubesActivas.Add(inst);
        }
    }

    public bool HayEstatuaEn(Vector3 destino)
    {
        float umbral = blocSize * 0.5f;
        foreach (GameObject est in estatuasActivas)
        {
            if (est == null) continue;
            if (Mathf.Abs(est.transform.position.x - destino.x) < umbral
                && Mathf.Abs(est.transform.position.z - destino.z) < umbral)
                return true;
        }
        return false;
    }

    private void GenerarEstatuasEsquinas(LevelData datoSala)
    {
        LimpiarEstatuas();
        if (prefabsEstatuas.Count == 0) { Debug.LogWarning("GenerarEstatuas: no hay prefabs cargados."); return; }
        if (datoSala.filas == null || datoSala.sizeLevel < 1) { Debug.LogWarning("GenerarEstatuas: datoSala.filas null o sizeLevel<1."); return; }

        int zMax = datoSala.sizeLevel - 1;
        Vector2Int[] esquinas =
        {
            new Vector2Int(0, 0),
            new Vector2Int(4, 0),
            new Vector2Int(0, zMax),
            new Vector2Int(4, zMax)
        };

        float techoSuelo = blocSize / 2f;
        if (activeTilesInRoom.Count > 0)
        {
            Renderer rTile = activeTilesInRoom[0].GetComponentInChildren<Renderer>();
            if (rTile != null) techoSuelo = rTile.bounds.max.y;
        }

        int spawneados = 0;
        foreach (Vector2Int esq in esquinas)
        {
            int gx = esq.x;
            int gz = esq.y;
            if (datoSala.filas[gz] == null) { Debug.Log($"esquina ({gx},{gz}): fila null"); continue; }
            if (datoSala.filas[gz].columnas[gx] == 0) { Debug.Log($"esquina ({gx},{gz}): celda vacía"); continue; }

            float signoX = (gx == 4) ? 1f : -1f;
            float signoZ = (gz == zMax) ? 1f : -1f;

            float worldX = (gx - 2) * blocSize + signoX * offsetEstatuaEnEsquina * blocSize;
            float worldZ = gz * blocSize + signoZ * offsetEstatuaEnEsquina * blocSize;

            GameObject prefab = prefabsEstatuas[Random.Range(0, prefabsEstatuas.Count)];
            if (prefab == null) continue;

            float rotY = Random.Range(0, 4) * 90f;
            GameObject inst = Instantiate(prefab, new Vector3(worldX, techoSuelo, worldZ), Quaternion.Euler(0f, rotY, 0f), transform);
            inst.transform.localScale = escalaEstatuas;

            Renderer[] rs = inst.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                Bounds b = rs[0].bounds;
                foreach (Renderer r in rs) b.Encapsulate(r.bounds);
                float deltaY = techoSuelo - b.min.y;
                inst.transform.position += new Vector3(0f, deltaY, 0f);
            }

            estatuasActivas.Add(inst);
            spawneados++;
        }
        Debug.Log($"GenerarEstatuasEsquinas: spawneadas {spawneados} estatuas en nivel sizeLevel={datoSala.sizeLevel}.");
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
        
        player.transform.position = new Vector3(0f, techoDelSuelo + mitadAlturaJugador + offsetAlturaJugador, -1f * blocSize);
        
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
                if (tile.GetComponent<VoxelTiles>() == null) tile.AddComponent<VoxelTiles>();
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

    public void ReturnTileToPool(GameObject tile)
    {
        tile.SetActive(false);
        activeTilesInRoom.Remove(tile);
        tilePool.Enqueue(tile);
    }

    private IEnumerator RutinaCaidaDeFilas(LevelData datoSala)
    {
        Animator animPlayer = player != null ? player.GetComponentInChildren<Animator>() : null;
        if (animPlayer != null)
        {
            yield return null;
            float maxEspera = 15f;
            float esperado = 0f;
            while (esperado < maxEspera && animPlayer.GetCurrentAnimatorStateInfo(0).IsName("pray"))
            {
                esperado += Time.deltaTime;
                yield return null;
            }
        }

        float intervalo = Mathf.Max(0.1f, datoSala.fallFloorVelocity);
        const float tiempoTemblor = 1f;

        for (int z = -1; z < datoSala.sizeLevel; z++)
        {
            yield return new WaitForSeconds(intervalo);

            float posZ = z * blocSize;
            foreach (GameObject tile in activeTilesInRoom)
            {
                if (tile == null) continue;
                if (Mathf.Abs(tile.transform.position.z - posZ) > 0.15f) continue;
                VoxelTiles vt = tile.GetComponent<VoxelTiles>();
                if (vt != null) vt.StartFalling(tiempoTemblor);
            }

            caidasDiferidas.Add(StartCoroutine(HacerCaerTrampasYEstatuasConRetraso(z, posZ, tiempoTemblor)));
        }

        rutinaCaidaSuelo = null;
    }

    private IEnumerator HacerCaerTrampasYEstatuasConRetraso(int z, float posZ, float retraso)
    {
        yield return new WaitForSeconds(retraso);

        if (trampaSpawner != null && z >= 0) trampaSpawner.HacerCaerEnFila(z);

        foreach (GameObject est in estatuasActivas)
        {
            if (est == null) continue;
            if (Mathf.Abs(est.transform.position.z - posZ) > blocSize * 0.5f) continue;
            if (est.GetComponent<CaidaSimple>() != null) continue;
            est.AddComponent<CaidaSimple>();
        }
    }

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
        {
            if (tile == null) continue;
            if (tile.transform.position.y < -0.5f) continue;
            if (Mathf.Abs(tile.transform.position.x - destino.x) < 0.15f && Mathf.Abs(tile.transform.position.z - destino.z) < 0.15f) return true;
        }
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
        
        if (gridZ >= size) 
        {
            bool esCeldaPuerta = (gridZ == size && wallGenerator != null && gridX == wallGenerator.puertaCellX);
            
            if (esCeldaPuerta)
            {
                if (enemySpawner != null && enemySpawner.HayEnemigosVivos())
                {
                    return true;
                }

                return false;
            }
            
            return true; 
        }
        
        return false;
    }

    private bool HayTrampaEn(Vector3 pos, LevelData datoSala)
    {
        int gridX = Mathf.RoundToInt(pos.x / blocSize) + 2;
        int gridZ = Mathf.RoundToInt(pos.z / blocSize);
        if (gridX < 0 || gridX >= 5 || gridZ < 0 || gridZ >= datoSala.sizeLevel) return false;

        if (datoSala.gridPinchos != null && gridZ < datoSala.gridPinchos.Length
            && datoSala.gridPinchos[gridZ] != null
            && datoSala.gridPinchos[gridZ].columnas[gridX] == 1) return true;

        if (datoSala.gridHachas != null && gridZ < datoSala.gridHachas.Length
            && datoSala.gridHachas[gridZ] != null
            && datoSala.gridHachas[gridZ].columnas[gridX] == 1) return true;

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
            if (HayTrampaEn(pos, datoSala)) continue;
            if (HayEstatuaEn(pos)) continue;

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