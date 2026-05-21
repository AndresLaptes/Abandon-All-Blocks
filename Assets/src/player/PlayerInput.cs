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
        if (gridMovement.IsDead() || gridMovement.IsHurt()) return;

        bool defendiendo = Input.GetMouseButton(1);
        gridMovement.SetDefending(defendiendo);

        if (defendiendo) return;

        if (Input.GetMouseButtonDown(0) && !gridMovement.IsMoving() && !gridMovement.IsAttacking())
        {
            gridMovement.Atacar();
            return;
        }

        if (gridMovement.IsMoving() || gridMovement.IsAttacking()) return;
        
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