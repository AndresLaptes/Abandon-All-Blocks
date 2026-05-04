using System.Collections;
using UnityEngine;

public class VoxelTiles : MonoBehaviour
{
    private LevelManager levelManager;
    private bool isFalling = false;
    private float fallSpeed = 5f;
    
    void Awake() 
    {
        levelManager = FindObjectOfType<LevelManager>();
    }

    public void StartFalling(float delay)
    {
        StartCoroutine(FallAfterDelay(delay));
    }
    
    private IEnumerator FallAfterDelay(float delay)
    {
        // La celda espera su tiempo individual (así caen independientemente)
        yield return new WaitForSeconds(delay);
        isFalling = true;
    }
    
    void Update()
    {
        if (isFalling)
        {
            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

            if (transform.position.y < -10f)
            {
                isFalling = false; 
                transform.position = Vector3.zero; 
                levelManager.ReturnTileToPool(this.gameObject);
            }
        }
    }
}
