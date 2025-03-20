using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required for UI components

public class CameraControllerMouse : MonoBehaviour
{
    public float moveSpeed = 8f;        // Speed for moving the camera
    public float rotationSpeed = 15.0f;   // Speed for rotating the camera
    public float verticalSpeed = 1f;     // Speed for rising and descending (Y-axis)
    public Slider speedSlider; // Assign in the inspector


    private float speedScale = 1f;
    private Vector3 cameraOffset;        // Camera offset from the target point
    private Vector3 lastMousePosition;   // Last mouse position for detecting movement
    private bool isRotating = false;     // To check if the right mouse button is held down

    void Start()
    {
        // Initialize camera offset
        cameraOffset = transform.position;
        speedSlider.onValueChanged.AddListener(SetScale);
    }

    private void SetScale(float scale)
    {
        speedScale = (0.1f + 0.9f * scale) * 10f;
    }

    void Update()
    {
        HandleMovement();
        HandleRotation();
    }

    // Handle W, A, S, D movement
    void HandleMovement()
    {
        float horizontalInput = Input.GetAxis("Horizontal"); // A, D keys for left-right movement
        float verticalInput = Input.GetAxis("Vertical");     // W, S keys for forward-backward movement

        // Vertical control: Space for rise, Ctrl for descend
        float riseInput = 0f;
        if (Input.GetKey(KeyCode.Space))  // Space key to move up
        {
            riseInput = 1f;
        }
        if (Input.GetKey(KeyCode.C))  // Ctrl key to move down
        {
            riseInput = -1f;
        }

        // Create a movement vector using horizontal, vertical, and rise inputs
        Vector3 movement = new Vector3(horizontalInput, riseInput * verticalSpeed, verticalInput) * moveSpeed * speedScale * Time.deltaTime;

        // Translate the camera using this movement vector
        transform.Translate(movement, Space.Self);
    }

    // Handle mouse rotation (Right Mouse Button to rotate)
    void HandleRotation()
    {
        if (Input.GetMouseButtonDown(1))  // Right mouse button pressed
        {
            isRotating = true;
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(1))    // Right mouse button released
        {
            isRotating = false;
        }

        if (isRotating)
        {
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;

            // Rotate the camera around the Y-axis (horizontal rotation) and X-axis (vertical rotation)
            float yaw = mouseDelta.x * speedScale * rotationSpeed * Time.deltaTime;
            float pitch = -mouseDelta.y * speedScale * rotationSpeed * Time.deltaTime;

            transform.RotateAround(transform.position, Vector3.up, yaw);   // Horizontal rotation
            transform.RotateAround(transform.position, transform.right, pitch); // Vertical rotation

            lastMousePosition = Input.mousePosition;
        }

        else 
        {
            float horizontalRotation = 0f;
            if (Input.GetKey(KeyCode.L))  // spin left
            {
                horizontalRotation = 1f;
            }
            if (Input.GetKey(KeyCode.J))  // spin right
            {
                horizontalRotation = -1f;
            }
            float verticalRotation = 0f;
            if (Input.GetKey(KeyCode.K))  // roll up
            {
                verticalRotation = 1f;
            }
            if (Input.GetKey(KeyCode.I))  // roll down
            {
                verticalRotation = -1f;
            }

            // Rotate the camera around the Y-axis (horizontal rotation) and X-axis (vertical rotation)
            float yaw = horizontalRotation * rotationSpeed * speedScale * Time.deltaTime;
            float pitch = verticalRotation * rotationSpeed * speedScale * Time.deltaTime;

            transform.RotateAround(transform.position, Vector3.up, yaw);   // Horizontal rotation
            transform.RotateAround(transform.position, transform.right, pitch);   // Vertical rotation
        }
    }

}
