 using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.UI;

public class WallRunning : NetworkBehaviour
{
    [Header("Wallrunnig")]
    [Tooltip("Layer that defines what Walls are")]
    public LayerMask whatIsWall;
    [Tooltip("Layer that defines what the Ground is")]
    public LayerMask whatIsGround;
    [Tooltip("How much force is applied to the player when wallrunning")]
    public float wallRunForce;
    [Tooltip("How long the player can wall run for")]
    public float maxWallRunTime;
    private float wallRunTimer;
    

    [Header("Wall Jumping")]
    [Tooltip("How much upwards force is applied to the player after jumping out of a wall run")]
    public float wallJumpUpForce;
    [Tooltip("How much lateral force is applied to the player after jumping out of the wall")]
    public float wallJumpSideForce;

    [Header("Inputs")]
    [Tooltip("The key that triggers the wall jump action")]
    public KeyCode jumpKey = KeyCode.Space;
    private float horizontalInput;
    private float verticalInput;

    [Header("Dection")]
    [Tooltip("The how close to the wall does the player need to be to be able to wall run")]
    public float wallCheckDistance;
    [Tooltip("How high off the ground must the player be to begin a wall run")]
    public float minJumpHeight;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    public bool wallLeft;
    public bool wallRight;

    [Header("Exiting")]
    [Tooltip("Bool that indicates if the player is currently exiting a wall")]
    public bool exitingWall;
    [Tooltip("How long the player must wait to begin a wall run after exiting a wall run")]
    public float exitWallTime;
    private float exitWallTimer;

    [Header("Gravity")]
    [Tooltip("Is the player affected by gravity when wall running?")]
    public bool useGravity;
    [Tooltip("If the player is affected by gravity, this determines the amount of force applied to counter gravity")]
    public float gravityCounterForce;

    [Header("Cotoye Time")]
    public float coyoteTime;
    private float coyoteTimer;
    private bool coyoteJumpAvailable;

    [Header("QuickTurn Wall Jump")]
    [Tooltip("How is the player's speed affected when quick turning")]
    [Range(0f, 1f)]
    public float quickTurnSpeedModifier;

    [Header("Camera Effects")]
    [Tooltip("How far the camera tilts while wall running")]
    public float tiltValue;

    [Header("References")]
    [Tooltip("A reference to a GameObject that stores the player�s orientation")]
    public Transform orientation;
    [Tooltip("A reference to the player camera")]
    public PlayerCam cam;
    private PlayerMovement pm;
    private Rigidbody rb;
    private LedgeGrabbing lg;
    private PlayerControls inputs;
    private WallGrab wallgrabScript;


    // Start is called before the first frame update
    void Start()
    {
        if (!IsOwner) return;
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovement>();
        lg = GetComponent<LedgeGrabbing>();
        wallgrabScript = GetComponent<WallGrab>();
        inputs = new PlayerControls();
        inputs.PlayerMovement.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;
        //Debug.Log("Wall Run Timer = " + wallRunTimer);
        CheckForWall();
        StateMachine();
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;
        if (pm.wallrunning)
        {
            WallRunningMovement();
        }
    }

    private void CheckForWall()
    {
        wallRight = Physics.Raycast(transform.position, orientation.right, out rightWallHit, wallCheckDistance, whatIsWall);
        wallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallHit, wallCheckDistance, whatIsWall);
    }

    private bool AboveGround()
    {
        return !Physics.Raycast(transform.position, Vector3.down, minJumpHeight, whatIsGround);
    }

    private void StateMachine()
    {
        //getting inputs
        horizontalInput = inputs.PlayerMovement.Movement.ReadValue<Vector2>().x; 
            //Input.GetAxisRaw("Horizontal");
        verticalInput = inputs.PlayerMovement.Movement.ReadValue<Vector2>().y;

        //State 1 - Wall Running
        if ((wallLeft || wallRight) && verticalInput > 0 && AboveGround() && !exitingWall)
        {
             //Debug.Log("Wall jump input = " + inputs.PlayerMovement.Jump.ReadValue<float>());
             //Start Wallrun here
             if(!pm.wallrunning)
            {
                if(!cam.isQuickTurning() && !wallgrabScript.exitingWall)
                {
                    StartWallRun();
                }
            }

             if(wallRunTimer > 0)
            {
                wallRunTimer -= Time.deltaTime;
            }

             if(wallRunTimer<= 0 && pm.wallrunning)
            {
                exitingWall= true;
                exitWallTimer = exitWallTime;
            }

            if (inputs.PlayerMovement.Jump.triggered)
            {
                Debug.Log("Normal Wall Jump");
                WallJump(false);
            }
        }
        //State 2 - Exiting
        else if (exitingWall)
        {
            if (pm.wallrunning)
            {
                //Debug.Log("Stopping wall run from state 2 - exiting");
                StopWallRun();
            }

     
            if (exitWallTimer > 0)
            {
                
                exitWallTimer -= Time.deltaTime;
            }

            if(exitWallTimer <= 0)
            {
                exitingWall = false;
            }
        }
        //State 3 None
        else
        {
            if (pm.wallrunning)
            {
                //Debug.Log("Stopping wall run from state 3 - none");
                StopWallRun();
            }

            if(coyoteTimer > 0)
            {
                coyoteTimer -= Time.deltaTime;
            }

            if(coyoteTimer > 0 && inputs.PlayerMovement.Jump.triggered && coyoteJumpAvailable)
            {
                Debug.Log("Coyote Time Wall Jump");
                
                WallJump(true);
            }
        }
    }

    private void StartWallRun()
    {
        if (pm.wallGrabbing) return;
        //Debug.Log("Start Wall Run");
        pm.wallrunning = true;
        wallRunTimer = maxWallRunTime;
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        coyoteJumpAvailable = true;
        //apply camera effects
        cam.AddToFov(10f);
        if (wallLeft) cam.DoTilt(-tiltValue);
        if(wallRight) cam.DoTilt(tiltValue);
    }

    private void WallRunningMovement()
    {
        rb.useGravity = useGravity;
       
        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;

        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        if((orientation.forward - wallForward).magnitude > (orientation.forward - -wallForward).magnitude)
        {
            wallForward = -wallForward;
        }
        //forward force
        rb.AddForce(wallForward*wallRunForce, ForceMode.Force);

        //weaken Gravity
        if(useGravity)
        {
            rb.AddForce(transform.up*gravityCounterForce, ForceMode.Force);
        }

    }

    private void StopWallRun()
    {
        //Debug.Log("Stop Wall Run");
        //Debug.Break();
        pm.wallrunning = false;
        //reset camera effects
        cam.ResetFov();
        cam.DoTilt(0f);
        coyoteTimer = coyoteTime;
    }

    private void WallJump(bool isCoyote)
    {
        //to prevent during wallGrab jump
        if (pm.wallGrabbing) return;
        //Debug.Log("Wall Jump");
        if (lg.holding || lg.exitingLedge) return;
        exitingWall = true;
        if (!isCoyote) exitWallTimer = exitWallTime;
        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;

        Vector3 forceToApply = transform.up * wallJumpUpForce + wallNormal * wallJumpSideForce;
        //add force


        if (pm.quickTurned) {
            rb.velocity *= quickTurnSpeedModifier;
            Debug.Log("Quick Turn Wall Jump");
        }
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z); 
        
        rb.AddForce(forceToApply, ForceMode.Impulse);

        coyoteJumpAvailable = false;


    }

    public Vector3 GetWallNormal()
    {
        if (wallLeft)
        {
            return leftWallHit.normal;
        } else if (wallRight)
        {
            return rightWallHit.normal;
        } else
        {
            return Vector3.zero;
        }
    }
}
