using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{   
    private Vector2 currentPosition;
    //Vector2 newPosition = new Vector2(2,0);

    private float inputX;
    [SerializeField] float moveSpeed = 5f;

    Rigidbody2D myRigidBody;
    // Start is called before the first frame update
    void Start()
    {   
        myRigidBody = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        inputX = Input.GetAxis("Horizontal");
        Debug.Log(inputX);
        currentPosition = transform.position;
        //transform.position = currentPosition + new Vector2(inputX * moveSpeed * Time.deltaTime, 0);
        
        myRigidBody.velocity = new Vector2(inputX * moveSpeed, myRigidBody.velocity.y);

    }
}
