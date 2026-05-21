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
        // Si cambiamos el sizeLevel en el inspector, redimensionamos la matriz
        if (filas == null || filas.Length != sizeLevel)
        {
            filas = new LevelRow[sizeLevel];

            for (int i = 0; i < sizeLevel; i++)
            {
                filas[i] = new LevelRow();
                for (int j = 0; j < 5; j++)
                {
                    filas[i].columnas[j] = 1; // Rellenamos con 1 por defecto
                }
            }
        }

        if (gridPinchos == null || gridPinchos.Length != sizeLevel)
        {
            gridPinchos = new LevelRow[sizeLevel];
            for (int i = 0; i < sizeLevel; i++)
            {
                gridPinchos[i] = new LevelRow();
                for (int j = 0; j < 5; j++) gridPinchos[i].columnas[j] = 0;
            }
        }

        if (gridHachas == null || gridHachas.Length != sizeLevel)
        {
            gridHachas = new LevelRow[sizeLevel];
            for (int i = 0; i < sizeLevel; i++)
            {
                gridHachas[i] = new LevelRow();
                for (int j = 0; j < 5; j++) gridHachas[i].columnas[j] = 0;
            }
        }
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