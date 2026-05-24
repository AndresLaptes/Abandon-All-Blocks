using UnityEngine;

public class CaidaSimple : MonoBehaviour
{
    public float fallSpeed = 5f;
    public float yDestruccion = -10f;

    void Update()
    {
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime, Space.World);
        if (transform.position.y < yDestruccion) Destroy(gameObject);
    }
}
