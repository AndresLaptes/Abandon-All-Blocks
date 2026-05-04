using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFramer : MonoBehaviour
{
    [Header("Seguimiento de camara")]
    public float smoothSpeed = 5f;
    public float anticipacionZ = 5f;
    
    [Header("Ajustes Visuales (Modificar en Play)")]
    [Range(10f, 80f)] public float inclinacionX = 40f;
    [Range(-90f, 90f)] public float giroY = -25f;      
    
    public float bajarCamaraY = -2f;
    
    private Camera cam;
    private Transform objetivo; 
    private Vector3 offset;
    private float actualBlocSize;
    private float maxLimitZ;
    
    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true; 
    }

    public void IniciarSeguimiento(Transform jugador, float tamañoBloque, int longitudNivel)
    {
        objetivo = jugador;
        actualBlocSize = tamañoBloque; 
        maxLimitZ = (longitudNivel - 1) * tamañoBloque;
        
        float bloquesVisiblesVerticales = 8f; 
        cam.orthographicSize = (bloquesVisiblesVerticales * tamañoBloque) / 2f;
        transform.rotation = Quaternion.Euler(inclinacionX, giroY, 0f);        
        offset = -(transform.forward * 25f);
        Vector3 puntoDeEnfoque = objetivo.position + (Vector3.forward * anticipacionZ * actualBlocSize);
        puntoDeEnfoque.z = Mathf.Clamp(puntoDeEnfoque.z, 0f, maxLimitZ); 
        
        Vector3 posicionInicial = puntoDeEnfoque + offset;
        posicionInicial.y += bajarCamaraY; 

        transform.position = posicionInicial;
    }

    
    void LateUpdate()
    {
        if (objetivo == null) return;

        transform.rotation = Quaternion.Euler(inclinacionX, giroY, 0f);
        offset = -(transform.forward * 25f);
        Vector3 puntoDeEnfoque = objetivo.position + (Vector3.forward * anticipacionZ * actualBlocSize);
        puntoDeEnfoque.x = 0f;
        puntoDeEnfoque.z = Mathf.Clamp(puntoDeEnfoque.z, 0f, maxLimitZ);
        Vector3 posicionDeseada = puntoDeEnfoque + offset;
        posicionDeseada.y += bajarCamaraY; 
        transform.position = Vector3.Lerp(transform.position, posicionDeseada, smoothSpeed * Time.deltaTime);
    }
}