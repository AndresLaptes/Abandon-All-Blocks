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
}