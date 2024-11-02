using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementControl : MonoBehaviour
{
    CharacterController controller;

    public Transform groundCheck;

    public LayerMask groundMask;

    Vector3 verticalVelocity;

    int jumpCharges;

    bool isOnGround;

    bool isSprinting;
    bool isCrouching;

    Vector3 move;
    Vector3 input;

    float speed;
    public float runSpeed;
    public float sprintSpeed;
    public float crouchSpeed;

    public float airSpeed;

    float gravity;
    public float normalGravity;
    public float jumpHeight;

    float crouchHeight;
    float standHeight;

    Vector3 crouchingCenter = new Vector3(0, 0.5f, 0);
    Vector3 standingCenter = new Vector3(0, 0, 0);

    void Start()
    {
        controller = GetComponent<CharacterController>();
        standHeight = transform.localScale.y;
    }

    void HandleInput()
    {
        input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));

        // Player Direction Movement
        input = transform.TransformDirection(input);
        input = Vector3.ClampMagnitude(input, 1f);

        if (Input.GetKeyDown(KeyCode.Space) && jumpCharges > 0)
        {
            Jump();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            if (isCrouching)
            {
                ExitCrouch();
            }
            else
            {
                Crouch();
            }
            isCrouching = !isCrouching;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            isSprinting = !isSprinting;
        }
    }

    void Update()
    {
        HandleInput();

        if (isOnGround)
        {
            GroundedMovement();
        }
        else
        {
            AirMovement();
        }

        GroundedMovement();

        controller.Move(move * Time.deltaTime);

        checkGround();
        ApplyGravity();
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

        move = Vector3.ClampMagnitude(move, speed);
    }

    void checkGround()
    {
        isOnGround = Physics.CheckSphere(groundCheck.position, 0.2f, groundMask);

        if (isOnGround)
        {
            jumpCharges = 1;  // Reset jump charges only when grounded
        }
    }

    void ApplyGravity()
    {
        gravity = normalGravity;
        verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }

    void Jump()
    {
        verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2 * gravity);
        jumpCharges--;  // Decrease jump charges each time a jump occurs
    }

    void Crouch()
    {
        controller.height = crouchHeight;
        controller.center = crouchingCenter;
        transform.localScale = new Vector3(transform.localScale.x, crouchHeight, transform.localScale.z);
        isCrouching = true;
    }

    void ExitCrouch()
    {
        controller.height = (standHeight * 2);
        controller.center = standingCenter;
        transform.localScale = new Vector3(transform.localScale.x, standHeight, transform.localScale.z);
        isCrouching = false;
    }
}