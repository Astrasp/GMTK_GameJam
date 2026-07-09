using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
public class PlayerMovement : MonoBehaviour
{
    [Header ("Movement")]

    public float groundDrag;

    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump;
    public Transform groundCheck;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;


    [Header ("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;
    public float moveSpeed;



    public Transform orientation;

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;
    Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        readyToJump = true;
        

    }

    private void FixedUpdate()
    {
        // SpeedControl();
        MovePlayer();
         
    }

    private void Update()
    {
        // ground check

        // grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);
        
        grounded = Physics.CheckSphere(groundCheck.position,0.3f, whatIsGround);
        
        Debug.Log("Grounded: " + grounded);

        //forInput
        MyInput();

       

        // Handle Drag
        // if(grounded)
        //     rb.linearDamping = groundDrag;
        // else
        //     rb.linearDamping = 0;
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // when to jump
        if(Input.GetKey(jumpKey) && readyToJump && grounded)
        {
            readyToJump = false;
            
            Jump();

            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void MovePlayer()
    {
        //Calculate move direction
        moveDirection = orientation.forward * verticalInput + orientation.right *horizontalInput;
        moveDirection = moveDirection.normalized;

        // directly set horizontal velocity, preserve Y for gravity/jumping
        if(grounded)
        {
             Vector3 targetVelocity = moveDirection * moveSpeed;
            rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
        }
           
        
        //in air
        else if(!grounded)
        {
             Vector3 targetVelocity = moveDirection * moveSpeed * airMultiplier;
             rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
        }
       
        

    }

    // private void SpeedControl()
    // {
    //     Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f , rb.linearVelocity.z);
    //     // limt speed velocity
        
    //     if(flatVel.magnitude > moveSpeed)
    //     {
    //         Vector3 limitedVel =flatVel.normalized * moveSpeed;
    //         rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, rb.linearVelocity.z);
    //         Debug.Log("LIMITED: " + flatVel.magnitude); // only logs when actually limiting

    //     }

       
    // }

    private void Jump()
    {
        // reset y velocity
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        readyToJump = true;
    }
}
