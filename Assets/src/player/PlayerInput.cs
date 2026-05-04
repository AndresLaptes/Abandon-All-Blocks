using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GridMovement))]
public class PlayerInput : MonoBehaviour
{
    private GridMovement gridMovement;
    
    void Start()
    {
        gridMovement = GetComponent<GridMovement>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) 
            gridMovement.move(Vector3.forward);
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) 
            gridMovement.move(Vector3.left);
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) 
            gridMovement.move(Vector3.back);
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) 
            gridMovement.move(Vector3.right);
    }
}