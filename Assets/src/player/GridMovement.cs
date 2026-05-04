using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridMovement : MonoBehaviour
{
    [SerializeField] private float step_size = 1f;
    // Start is called before the first frame update

    public void ConfigurarPaso(float nuevoPaso)
    {
        step_size = nuevoPaso;
    }
    
    public void move(Vector3 direction)
    {
        transform.position += direction * step_size;
    }
}
