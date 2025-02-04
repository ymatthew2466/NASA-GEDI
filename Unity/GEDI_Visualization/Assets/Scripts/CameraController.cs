using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float moveSpeed = 8f;        // Speed for moving the camera
    public float rotationSpeed = 15.0f;   // Speed for rotating the camera
    public float verticalSpeed = 1f;     // Speed for rising and descending (Y-axis)

    private Vector3 cameraOffset;        // Camera offset from the target point
    private Vector3 lastMousePosition;   // Last mouse position for detecting movement
    private bool isRotating = false;     // To check if the right mouse button is held down

    void Start()
    {
        // Initialize camera offset
        cameraOffset = transform.position;
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
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))  // Ctrl key to move down
        {
            riseInput = -1f;
        }

        // Create a movement vector using horizontal, vertical, and rise inputs
        Vector3 movement = new Vector3(horizontalInput, riseInput * verticalSpeed, verticalInput) * moveSpeed * Time.deltaTime;

        // Translate the camera using this movement vector
        transform.Translate(movement, Space.Self);
    }

    // Handle mouse rotation (Right Mouse Button to rotate)
    void HandleRotation()
    {
        float rotationInput = 0f;
        if (Input.GetKey(KeyCode.K))  // Space key to move up
        {
            rotationInput = 1f;
        }
        if (Input.GetKey(KeyCode.J))  // Ctrl key to move down
        {
            rotationInput = -1f;
        }

            // Rotate the camera around the Y-axis (horizontal rotation) and X-axis (vertical rotation)
        float yaw = rotationInput * rotationSpeed * Time.deltaTime;

        transform.RotateAround(transform.position, Vector3.up, yaw);   // Horizontal rotation

      
    }

}
