using System.Collections;
using UnityEngine;

public class VoxelTiles : MonoBehaviour
{
    public float fallSpeed = 5f;
    [Tooltip("Segundos de temblor antes de caer.")]
    public float duracionTemblor = 1f;
    [Tooltip("Amplitud del temblor en unidades de mundo.")]
    public float magnitudTemblor = 0.05f;

    private LevelManager levelManager;
    private bool isFalling = false;

    void Awake()
    {
        levelManager = FindObjectOfType<LevelManager>();
    }

    void OnEnable()
    {
        isFalling = false;
    }

    public void StartFalling(float delay)
    {
        StartCoroutine(SecuenciaCaida(delay));
    }

    private IEnumerator SecuenciaCaida(float delay)
    {
        float esperaSinTemblor = Mathf.Max(0f, delay - duracionTemblor);
        if (esperaSinTemblor > 0f) yield return new WaitForSeconds(esperaSinTemblor);

        Vector3 posOriginal = transform.position;
        float duracion = Mathf.Min(delay, duracionTemblor);
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            float offX = (Random.value - 0.5f) * magnitudTemblor;
            float offZ = (Random.value - 0.5f) * magnitudTemblor;
            transform.position = posOriginal + new Vector3(offX, 0f, offZ);
            yield return null;
        }
        transform.position = posOriginal;

        isFalling = true;
    }

    void Update()
    {
        if (!isFalling) return;

        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime, Space.World);

        if (transform.position.y < -10f)
        {
            isFalling = false;
            if (levelManager != null) levelManager.ReturnTileToPool(this.gameObject);
            else gameObject.SetActive(false);
        }
    }
}
