using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementControl : MonoBehaviour
{
    //---------------------------------------
    // Gravity-related variables
    //---------------------------------------

    float gravity;
    public float normalGravity;
    public float wallRunGravity;

    //---------------------------------------
    // Jump-related variables
    //---------------------------------------

    int jumpCharges;
    bool doubleJumpUsed;
    public float jumpHeight;
    bool isWallJumping;
    float wallJumpTimer;
    public float maxWallJumpTimer;

    //---------------------------------------
    // Ground check variables
    //---------------------------------------

    public Transform groundCheck;
    public LayerMask groundMask;
    bool isOnGround;

    //---------------------------------------
    // Wall run variables
    //---------------------------------------

    public LayerMask wallMask;
    bool isWallRunning;
    bool hasWallRun;
    bool onLeftWall;
    bool onRightWall;

    RaycastHit leftWallHit;
    RaycastHit rightWallHit;
    Vector3 wallNormal;
    Vector3 lastWallNormal;

    public float wallRunSpeedIncrease;
    public float wallRunSpeedDecrease;
    public float wallRunTilt;
    public float tilt;

    //---------------------------------------
    // Climbing variables
    //---------------------------------------

    bool isClimbing;
    bool canClimb;
    bool hasClimbed;
    RaycastHit wallHit;

    float climbTimer;
    public float maxClimbTimer;
    public float climbSpeed;

    //---------------------------------------
    // Sliding variables
    //---------------------------------------

    bool isSliding;
    float slideTimer;
    public float maxSlideTimer;
    public float slideSpeedIncrease;
    public float slideSpeedDecrease;

    //---------------------------------------
    // Movement speed and state variables
    //---------------------------------------

    CharacterController controller;
    float speed;
    public float runSpeed;
    public float sprintSpeed;
    public float crouchSpeed;
    public float airSpeed;

    bool isSprinting;
    bool isCrouching;

    //---------------------------------------
    // Height and crouch variables
    //---------------------------------------

    float crouchHeight;
    float standHeight;

    Vector3 crouchingCenter = new Vector3(0, 0.5f, 0);
    Vector3 standingCenter = new Vector3(0, 0, 0);

    //---------------------------------------
    // Movement input and direction
    //---------------------------------------

    Vector3 move;
    Vector3 input;
    Vector3 forwardDirection;
    Vector3 verticalVelocity;

    //---------------------------------------
    // Camera variables
    //---------------------------------------

    public Camera playerCamera;
    float normalFov;
    public float specialFov;
    public float cameraChangeTime;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        standHeight = transform.localScale.y;
        normalFov = playerCamera.fieldOfView;
    }

    void IncreaseSpeed(float speedIncrease)
    {
        speed += speedIncrease;
    }

    void DecreaseSpeed(float speedDecrease)
    {
        speed -= speedDecrease * Time.deltaTime;
    }

    void HandleInput()
    {
        input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));

        // Player Direction Movement
        input = transform.TransformDirection(input);
        input = Vector3.ClampMagnitude(input, 1f);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isSliding)
            {
                // Exit slide and stand up
                ExitCrouch();
                isSliding = false;

                // Allow a double jump
                doubleJumpUsed = false;

                // Perform the first jump
                Jump();
            }
            else if (jumpCharges > 0 || (!doubleJumpUsed && !isOnGround))
            {
                // Perform a jump or double jump
                Jump();

                // If already airborne, mark the double jump as used
                if (!isOnGround)
                {
                    doubleJumpUsed = true;
                }
            }
        }


        if (Input.GetKeyDown(KeyCode.C))
        {
            if (isCrouching)
            {
                ExitCrouch();  // Toggle crouch off if already crouching
            }
            else
            {
                Crouch();      // Toggle crouch on if not crouching
            }
        }

        // Sprint toggle with LeftShift, restricted while sliding or crouching
        if (Input.GetKeyDown(KeyCode.LeftShift) && !isSliding && !isCrouching)
        {
            isSprinting = !isSprinting; // Toggle sprint on/off
        }
    }

    void CameraEffects()
    {
        float targetFov;

        // Set target FOV based on the player's current state
        if (isWallRunning || isSliding || isSprinting)
        {
            targetFov = specialFov;  // Use special FOV during wall running, sliding, or sprinting
        }
        else
        {
            targetFov = normalFov;   // Return to normal FOV otherwise
        }

        // Smoothly interpolate the camera's FOV
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, cameraChangeTime * Time.deltaTime);

        // Wall running tilt effect
        if (isWallRunning)
        {
            if (onRightWall)
            {
                tilt = Mathf.Lerp(tilt, wallRunTilt, cameraChangeTime * Time.deltaTime);
            }
            if (onLeftWall)
            {
                tilt = Mathf.Lerp(tilt, -wallRunTilt, cameraChangeTime * Time.deltaTime);
            }
        }
        else
        {
            tilt = Mathf.Lerp(tilt, 0f, cameraChangeTime * Time.deltaTime);
        }
    }


    void Update()
    {
        HandleInput();
        CheckWallRun();
        CheckClimbing();
        if (isOnGround)
            GroundedMovement();
        else if (!isOnGround && !isWallRunning && !isClimbing)
            AirMovement();

        if (isSliding)
        {
            SlideMovement();
            DecreaseSpeed(slideSpeedDecrease);
            slideTimer -= Time.deltaTime;

            if (slideTimer < 0)
            {
                isSliding = false;
            }
        }
        else if (isWallRunning)
        {
            WallRunMovement();
            DecreaseSpeed(wallRunSpeedDecrease);
        }
        else if (isClimbing)
        {
            ClimbMovement();
            climbTimer -= 1f * Time.deltaTime;
            if (climbTimer < 0)
            {
                isClimbing = false;
                hasClimbed = true;
            }
        }

        controller.Move(move * Time.deltaTime);
        checkGround();
        ApplyGravity();
        CameraEffects();
    }

    void GroundedMovement()
    {
        speed = isSprinting ? sprintSpeed : isCrouching ? crouchSpeed : runSpeed;

        // move and stop horizontally
        if (input.x != 0)
        {
            move.x += input.x * speed;
        }
        else
        {
            move.x = 0;
        }

        // move and stop vertically
        if (input.z != 0)
        {
            move.z += input.z * speed;
        }
        else
        {
            move.z = 0;
        }

        move = Vector3.ClampMagnitude(move, speed);
    }

    void AirMovement()
    {
        move.x += input.x * airSpeed;
        move.z += input.z * airSpeed;

        if (isWallJumping)
        {
            move += forwardDirection;
            wallJumpTimer -= 1f * Time.deltaTime;
            if (wallJumpTimer <= 0)
            {
                isWallJumping = false;
            }
        }

        move = Vector3.ClampMagnitude(move, speed);
    }

    void SlideMovement()
    {
        move += forwardDirection;
        move = Vector3.ClampMagnitude(move, speed);
    }

    void WallRunMovement()
    {
        if (input.z > (forwardDirection.z - 10f) && input.z < (forwardDirection.z + 10f))
        {
            move += forwardDirection;
        }
        else if (input.z < (forwardDirection.z - 10f) && input.z > (forwardDirection.z + 10f))
        {
            move.x = 0f;
            move.z = 0f;
            ExitWallRun();
        }
        move.x += input.x * airSpeed;
        move = Vector3.ClampMagnitude(move, speed);
    }

    void ClimbMovement()
    {
        forwardDirection = Vector3.up;
        move.x += input.x * airSpeed;
        move.z += input.z * airSpeed;

        verticalVelocity += forwardDirection;
        speed = climbSpeed;

        move = Vector3.ClampMagnitude(move, speed);
        verticalVelocity = Vector3.ClampMagnitude(verticalVelocity, speed);
    }

    void checkGround()
    {
        isOnGround = Physics.CheckSphere(groundCheck.position, 0.2f, groundMask);

        if (isOnGround)
        {
            jumpCharges = 1;  // Reset jump charges only when grounded
            doubleJumpUsed = false;  // Reset double jump
            hasWallRun = false;
            hasClimbed = false;
            climbTimer = maxClimbTimer;
        }
    }

    void CheckWallRun()
    {
        onLeftWall = Physics.Raycast(transform.position, -transform.right, out leftWallHit, 0.7f, wallMask);
        onRightWall = Physics.Raycast(transform.position, transform.right, out rightWallHit, 0.7f, wallMask);

        if ((onRightWall || onLeftWall) && !isWallRunning)
        {
            TestWallRun();
        }
        if ((!onRightWall && !onLeftWall) && isWallRunning)
        {
            ExitWallRun();
        }
    }

    void CheckClimbing()
    {
        canClimb = Physics.Raycast(transform.position, transform.forward, out wallHit, 0.7f, wallMask);
        float wallAngle = Vector3.Angle(-wallHit.normal, transform.forward);
        if (wallAngle < 15 && !hasClimbed && canClimb)
        {
            isClimbing = true;
        }
        else
        {
            isClimbing = false;
        }
    }

    void TestWallRun()
    {
        wallNormal = onLeftWall ? leftWallHit.normal : rightWallHit.normal;

        if (hasWallRun)
        {
            float wallAngle = Vector3.Angle(wallNormal, lastWallNormal);
            if (wallAngle > 15)
            {
                WallRun();
            }
        }
        else
        {
            WallRun();
            hasWallRun = true;
        }
    }

    void ApplyGravity()
    {
        gravity = isWallRunning ? wallRunGravity : isClimbing ? 0f : normalGravity;
        verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }

    void Jump()
    {
        if (!isOnGround && !isWallRunning)
        {
            jumpCharges -= 1;
        }
        else if (isWallRunning)
        {
            ExitWallRun();
            IncreaseSpeed(wallRunSpeedIncrease);
        }
        hasClimbed = false;
        climbTimer = maxClimbTimer;
        verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2 * gravity);
    }

    void Crouch()
    {
        controller.height = crouchHeight;
        controller.center = crouchingCenter;
        transform.localScale = new Vector3(transform.localScale.x, crouchHeight, transform.localScale.z);
        isCrouching = true;

        if (speed > runSpeed)
        {
            isSliding = true;
            forwardDirection = transform.forward;

            if (isOnGround)
            {
                IncreaseSpeed(slideSpeedIncrease);
            }

            slideTimer = maxSlideTimer;
        }
    }

    void ExitCrouch()
    {
        controller.height = (standHeight * 2);
        controller.center = standingCenter;
        transform.localScale = new Vector3(transform.localScale.x, standHeight, transform.localScale.z);
        isCrouching = false;
        isSliding = false;
    }

    void WallRun()
    {
        isWallRunning = true;
        jumpCharges = 1;
        IncreaseSpeed(wallRunSpeedIncrease);
        verticalVelocity = new Vector3(0f, 0f, 0f);

        forwardDirection = Vector3.Cross(wallNormal, Vector3.up);

        if (Vector3.Dot(forwardDirection, transform.forward) < 0)
        {
            forwardDirection = -forwardDirection;
        }
    }

    void ExitWallRun()
    {
        isWallRunning = false;
        lastWallNormal = wallNormal;
        forwardDirection = wallNormal;
        isWallJumping = true;
        wallJumpTimer = maxWallJumpTimer;
    }
}