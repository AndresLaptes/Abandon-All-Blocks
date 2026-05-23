using UnityEngine;

[System.Serializable]
public class LevelRow 
{
    // Representa las 5 columnas de ancho de tu carril
    public int[] columnas = new int[5];
}

[CreateAssetMenu(fileName = "NewRoom", menuName = "Dante's Inferno/Room Data")]
public class LevelData : ScriptableObject
{
    [Header("Configuración nivel")] 
    public string levelName;
    public int roomNumber;
    
    [Tooltip("Al cambiar este número, la matriz de abajo se reseteará llena de 1s")]
    public int sizeLevel; 

    [Header("Diseño del Mapa (0 = Vacío, 1 = Suelo, 2+ = Suelo + decoración)")]
    public LevelRow[] filas;

    private void OnValidate()
    {
        if (sizeLevel < 1) sizeLevel = 1;
        filas = RedimensionarPreservando(filas, sizeLevel, 1);
        gridPinchos = RedimensionarPreservando(gridPinchos, sizeLevel, 0);
        gridHachas = RedimensionarPreservando(gridHachas, sizeLevel, 0);
    }

    private static LevelRow[] RedimensionarPreservando(LevelRow[] viejas, int nuevoTamanio, int valorDefault)
    {
        if (viejas != null && viejas.Length == nuevoTamanio)
        {
            // Tamaño correcto: solo asegurar que cada fila tiene 5 columnas
            for (int i = 0; i < viejas.Length; i++)
            {
                if (viejas[i] == null) viejas[i] = NuevaFila(valorDefault);
                else if (viejas[i].columnas == null || viejas[i].columnas.Length != 5)
                {
                    viejas[i].columnas = new int[5];
                    for (int j = 0; j < 5; j++) viejas[i].columnas[j] = valorDefault;
                }
            }
            return viejas;
        }

        LevelRow[] nuevas = new LevelRow[nuevoTamanio];
        for (int i = 0; i < nuevoTamanio; i++)
        {
            if (viejas != null && i < viejas.Length && viejas[i] != null && viejas[i].columnas != null && viejas[i].columnas.Length == 5)
                nuevas[i] = viejas[i];
            else
                nuevas[i] = NuevaFila(valorDefault);
        }
        return nuevas;
    }

    private static LevelRow NuevaFila(int valorDefault)
    {
        LevelRow r = new LevelRow();
        for (int j = 0; j < 5; j++) r.columnas[j] = valorDefault;
        return r;
    }

    [Header("Aspecto")]
    public Color colorFondo = Color.black;
    [Tooltip("Multiplica BaseColor de la pared fade Unlit. Bájalo para oscurecerla y que iguale el brillo de las paredes Lit de este nivel.")]
    [ColorUsage(false, false)] public Color fadeTint = new Color(0.6f, 0.6f, 0.6f, 1f);

    [Header("Mecánicas del suelo")]
    public bool floorFall;
    public float fallFloorVelocity;

    [Header("Paredes")]
    [Tooltip("Si está activo: fila 1 sobre paredBase = toda paredDecoA, fila 2 = toda paredDecoB. Ignora filasArriba/probabilidadDecoracion del WallGenerator.")]
    public bool paredesApiladas;

    [Header("Dificultad")]
    public int requieredEnemiesToDoor;

    [Header("Spawns")]
    public int numHerejes;
    public int numGargolas;
    public int numBrea;
    public int numCoins;
    public int numTramps;

    public GameObject[] allowedEnemyTypes;

    [Header("Trampas")]
    [Tooltip("Índices Z (filas) donde aparecen troncos rodantes. Ej: [3,5] = filas Z=3 y Z=5 tendrán tronco.")]
    public int[] filasTrampaTronco;

    [Header("Trampas de pinchos")]
    [Tooltip("Grid paralelo al suelo. 1 = trampa de pinchos en esa celda. Solo activa donde hay suelo.")]
    public LevelRow[] gridPinchos;

    [Header("Trampas de hachas")]
    [Tooltip("Grid paralelo al suelo. 1 = hacha colgante oscilando sobre esa celda.")]
    public LevelRow[] gridHachas;
}