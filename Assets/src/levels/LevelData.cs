using UnityEngine;
using UnityEngine.Serialization;

//This create a new boton menu in unity
[CreateAssetMenu(fileName = "NewRoom", menuName = "Dante's Inferno/Room Data")]
public class LevelData : ScriptableObject
{
    [FormerlySerializedAs("name")] [Header("Configuración nivel")] 
    public string levelName;
    public int roomNumber;
    public int sizeLevel;

    [Header("Dificultad")]
    public int requieredEnemiesToDoor;

    public bool floorFall;
    public float fallFloorVelocity = 1.0f;
        
    [Header("Spawns")]
    public int numHerejes;
    public int numGargolas;
    public int numBrea;
    public int numCoins;
    public int numTramps;

    // Podemos pasarle los tipos de enmigos que pueden aparecer en ese nivel
    public GameObject[] allowedEnemyTypes;
}
