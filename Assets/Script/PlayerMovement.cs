using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
public class PlayerMovement : MonoBehaviour
{
    [Header ("Movement")]

    public float groundDrag;
    public MovementState state;
    public float dashSpeedChangeFactor;

    public float maxYSpeed;

    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump;
    public Transform groundCheck;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;


    [Header ("Ground Check")]
    
    public LayerMask whatIsGround;
    bool grounded;
    public float moveSpeed;

    public float dashSpeed;
    public float walkSpeed;


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

    private float desiredMoveSpeed;
    private float lastDesiredMoveSpeed;
    private MovementState lastState;
    private bool keepMomentum;

    private void StateHandler()
    {
        if(!grounded)
        {
            state = MovementState.air;
            desiredMoveSpeed = walkSpeed;
        }
        //Mode - Dashing
        else if (dashing)
        {
            state = MovementState.dashing;
            desiredMoveSpeed = dashSpeed;
            speedChangeFactor = dashSpeedChangeFactor;
        }


        //Mode - walking
        else if (grounded)
        {
            state = MovementState.walking;
            desiredMoveSpeed = walkSpeed;
        }

       
        //Rest Modes will be added if needed

        bool desiredMoveSpeedHasChanged = desiredMoveSpeed != lastDesiredMoveSpeed;
        if (lastState == MovementState.dashing) keepMomentum = true;

        if (desiredMoveSpeedHasChanged)
        {
            if(keepMomentum)
            {
                StopAllCoroutines();
                StartCoroutine(SmoothlyLerpMoveSpeed());
            }
            else
            {
                StopAllCoroutines();
                moveSpeed = desiredMoveSpeed;
            }
        }
        lastDesiredMoveSpeed = desiredMoveSpeed;
        lastState = state;
    }

    private float speedChangeFactor;
    private IEnumerator SmoothlyLerpMoveSpeed()
    {
        // Smoothly lerp movementSpeed to desired value
        float time = 0;
        float difference = Mathf.Abs(desiredMoveSpeed - moveSpeed);
        float startValue = moveSpeed;

        float boostFactor = speedChangeFactor;

        while (time < difference)
        {
            moveSpeed = Mathf.Lerp(startValue, desiredMoveSpeed, time / difference);
            time += Time.deltaTime * boostFactor;

            yield return null;
        }
        moveSpeed = desiredMoveSpeed;
        speedChangeFactor = 1f;
        keepMomentum = false;
    }


    private void FixedUpdate()
    {
        // SpeedControl();
        MovePlayer();


        //limit y vel

        if(maxYSpeed != 0 && rb.linearVelocity.y > maxYSpeed)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, maxYSpeed, rb.linearVelocity.z);
    }

    private void Update()
    {
        // ground check

        // grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);
        
        grounded = Physics.CheckSphere(groundCheck.position,0.3f, whatIsGround);
        
        Debug.Log("Grounded: " + grounded);

        //forInput
        MyInput();

        StateHandler(); //for player state

       

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
        if(Input.GetKeyDown(jumpKey) && readyToJump && grounded)
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

    public enum MovementState
    {
        walking,
        sprinting,
        crouching,
        dashing,
        air
    }

    public bool dashing;
}
