using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR; 

public class HMDController : MonoBehaviour
{
    public Transform vrCamera; // The transform of the VR camera (typically the "Camera (eye)" object)

    // SteamVR actions for the joystick on the left and right controllers
    public SteamVR_Action_Vector2 leftJoystick;   // Left joystick for movement
    public SteamVR_Action_Vector2 rightJoystick;  // Right joystick for rise, descent, and rotation

    // Reference to the VR input sources (left and right hands)
    public SteamVR_Input_Sources leftHand = SteamVR_Input_Sources.LeftHand;
    public SteamVR_Input_Sources rightHand = SteamVR_Input_Sources.RightHand;

    // Movement speed variables
    public float moveSpeed = 1.5f;  // Speed for movement on the left joystick
    public float riseSpeed = 1.0f;  // Speed for rise/descent on the right joystick
    public float rotateSpeed = 1f; // Rotation speed for turning on the right joystick
    void Start()
    {
        // Force SteamVR to initialize manually if CameraRig isn't being used
        SteamVR.Initialize();
    }
    void Update()
    {
        HandleLeftJoystickMovement();
        HandleRightJoystickMovement();
    }

    // Handle the movement from the left joystick (forward, backward, left, right)
    void HandleLeftJoystickMovement()
    {
        // Get the input from the left joystick (X and Y axis)
        Vector2 moveInput = leftJoystick.GetAxis(leftHand);


        // Forward/Backward movement
        Vector3 forwardMovement = vrCamera.forward * moveInput.y * moveSpeed * Time.deltaTime;

        // Left/Right movement
        Vector3 rightMovement = vrCamera.right * moveInput.x * moveSpeed * Time.deltaTime;


        // Apply movement (ignoring vertical movement)
        transform.position += forwardMovement + rightMovement;
    }

    // Handle the rise, descent, and rotation from the right joystick
    void HandleRightJoystickMovement()
    {
        // Get the input from the right joystick (X for rotation, Y for rise/descent)
        Vector2 rotationInput = rightJoystick.GetAxis(rightHand);

        // Rise/Descent (Y axis controls up/down)
        Vector3 verticalMovement = Vector3.up * rotationInput.y * riseSpeed * Time.deltaTime;
        transform.position += verticalMovement;

        // Rotation (X axis controls yaw rotation)
        float yawRotation = rotationInput.x * rotateSpeed * Time.deltaTime;
        transform.Rotate(0, yawRotation, 0);
    }
}