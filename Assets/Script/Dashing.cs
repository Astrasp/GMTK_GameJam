using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dashing : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Header("References")]
    public Transform orientation;
    public Transform playerCam;
    private Rigidbody rb;
    private PlayerMovement pm; //PlayerMovementDashing
    [Header("Dashing")]
    public float dashForce;
    public float dashUpwardForce;
    public float dashDuration;

    [Header("Cooldown")]
    public float dashCd;
    private float dashCdTimer;

    [Header("Input")]
    public KeyCode dashKey = KeyCode.LeftShift;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovement>();//PlayerMovementDashing
    }

    private void Dash()
    {
      Vector3 forceToApply = orientation.forward * dashForce + orientation.up * dashUpwardForce;  

      rb.AddForce(forceToApply, ForceMode.Impulse);

      Invoke(nameof(ResetDash), dashDuration);
    
    }

    private void ResetDash()
    {
        
    }

}
