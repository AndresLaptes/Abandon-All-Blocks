using UnityEngine;
using UnityEditor;
using System.IO;

public class CreadorDeNiveles : EditorWindow
{
    [Header("Rutas")]
    private string nombreCarpeta = "Nivel1";
    private string nombreArchivo = "NewRoom_01";

    private string levelName = "Sala Base";
    private int roomNumber = 1;
    private int sizeLevel = 10;
    
    private int requieredEnemiesToDoor = 0;
    private bool floorFall = false;
    private float fallFloorVelocity = 1.0f;
    
    private int numHerejes;
    private int numGargolas;
    private int numBrea;
    private int numCoins = 0;
    private int numTramps = 0;

    [MenuItem("Dante's Inferno/Creador de Niveles")]
    public static void MostrarVentana()
    {
        GetWindow<CreadorDeNiveles>("Generador de Niveles");
    }

    void OnGUI()
    {
        GUILayout.Label("Ruta de Guardado", EditorStyles.boldLabel);
        nombreCarpeta = EditorGUILayout.TextField("Carpeta (en Resources):", nombreCarpeta);
        nombreArchivo = EditorGUILayout.TextField("Nombre del Archivo:", nombreArchivo);

        GUILayout.Space(10);
        
        GUILayout.Label("Configuración nivel", EditorStyles.boldLabel);
        levelName = EditorGUILayout.TextField("Nombre del Nivel (levelName):", levelName);
        roomNumber = EditorGUILayout.IntField("Número de Sala:", roomNumber);
        sizeLevel = EditorGUILayout.IntField("Longitud (sizeLevel):", sizeLevel);

        GUILayout.Space(10);

        GUILayout.Label("Dificultad", EditorStyles.boldLabel);
        requieredEnemiesToDoor = EditorGUILayout.IntField("Enemigos para Puerta:", requieredEnemiesToDoor);
        floorFall = EditorGUILayout.Toggle("¿Suelo cae?:", floorFall);
        
        if (floorFall)
        {
            fallFloorVelocity = EditorGUILayout.FloatField("Velocidad caída:", fallFloorVelocity);
        }

        GUILayout.Space(10);

        GUILayout.Label("Spawns", EditorStyles.boldLabel);
        numHerejes = EditorGUILayout.IntField("Num Herejes:", numHerejes);
        numGargolas = EditorGUILayout.IntField("Num Gargola:", numGargolas);
        numBrea = EditorGUILayout.IntField("Num Brea:", numBrea);
        numCoins = EditorGUILayout.IntField("Num Monedas:", numCoins);
        numTramps = EditorGUILayout.IntField("Num Trampas:", numTramps);

        GUILayout.Space(15);

        if (GUILayout.Button("Crear Nivel y Guardar Datos", GUILayout.Height(40)))
        {
            CrearNivel();
        }
    }

    private void CrearNivel()
    {
        string rutaBase = "Assets/Resources";
        if (!AssetDatabase.IsValidFolder(rutaBase)) AssetDatabase.CreateFolder("Assets", "Resources");

        string rutaCarpeta = $"{rutaBase}/{nombreCarpeta}";
        if (!AssetDatabase.IsValidFolder(rutaCarpeta)) AssetDatabase.CreateFolder(rutaBase, nombreCarpeta);

        string rutaArchivoFisico = $"{rutaCarpeta}/{nombreArchivo}.asset";
        
        if (File.Exists(rutaArchivoFisico))
        {
            Debug.LogWarning($"¡Cuidado! El archivo {nombreArchivo} ya existe en {nombreCarpeta}.");
            return;
        }

        LevelData nuevoNivel = ScriptableObject.CreateInstance<LevelData>();
        
        nuevoNivel.levelName = levelName;
        nuevoNivel.roomNumber = roomNumber;
        nuevoNivel.sizeLevel = sizeLevel;
        nuevoNivel.requieredEnemiesToDoor = requieredEnemiesToDoor;
        nuevoNivel.floorFall = floorFall;
        nuevoNivel.fallFloorVelocity = fallFloorVelocity;
        nuevoNivel.numHerejes = numHerejes;
        nuevoNivel.numGargolas = numGargolas;
        nuevoNivel.numBrea = numBrea;
        nuevoNivel.numCoins = numCoins;
        nuevoNivel.numTramps = numTramps;

        AssetDatabase.CreateAsset(nuevoNivel, rutaArchivoFisico);
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = nuevoNivel;

        Debug.Log($"¡Nivel {nombreArchivo} creado con éxito y configurado!");
    }
}