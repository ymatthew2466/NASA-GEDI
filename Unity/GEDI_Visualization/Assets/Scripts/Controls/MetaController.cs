using UnityEngine;

public class MetaController : MonoBehaviour
{
    public Transform vrCamera;

    public float moveSpeed = 1f;  // Speed for movement on the left joystick
    public float riseSpeed = 0.6f;  // Speed for rise/descent on the right joystick
    public float rotateSpeed = 0.6f; // Rotation speed for turning on the right joystick
    // Handle W, A, S, D movement
    void Update()
    {
        HandleLeftJoystickMovement();
        HandleRightJoystickMovement();
    }

    // Handle the movement from the left joystick (forward, backward, left, right)
    void HandleLeftJoystickMovement()
    {
        float speed_factor = 1.0f;
        if (OVRInput.Get(OVRInput.RawButton.LIndexTrigger)) speed_factor = 5.0f;

        if (OVRInput.Get(OVRInput.RawButton.LThumbstickUp)) vrCamera.position = vrCamera.position + vrCamera.forward * moveSpeed * speed_factor;
        if (OVRInput.Get(OVRInput.RawButton.LThumbstickDown)) vrCamera.position = vrCamera.position - vrCamera.forward * moveSpeed * speed_factor;
        if (OVRInput.Get(OVRInput.RawButton.LThumbstickLeft)) vrCamera.position = vrCamera.position - vrCamera.right * moveSpeed * speed_factor;
        if (OVRInput.Get(OVRInput.RawButton.LThumbstickRight)) vrCamera.position = vrCamera.position + vrCamera.right * moveSpeed * speed_factor;
    }

    // Handle the rise, descent, and rotation from the right joystick
    void HandleRightJoystickMovement()
    {
        float speed_factor = 1.0f;
        if (OVRInput.Get(OVRInput.RawButton.LIndexTrigger)) speed_factor = 5.0f;

        if (OVRInput.Get(OVRInput.RawButton.RThumbstickLeft))  vrCamera.Rotate(0, -speed_factor * rotateSpeed, 0);
        if (OVRInput.Get(OVRInput.RawButton.RThumbstickRight)) vrCamera.Rotate(0, speed_factor * rotateSpeed, 0);
        if (OVRInput.Get(OVRInput.Button.Two))   vrCamera.position = vrCamera.position + vrCamera.up * riseSpeed * speed_factor;
        if (OVRInput.Get(OVRInput.Button.One)) vrCamera.position = vrCamera.position - vrCamera.up * riseSpeed * speed_factor;
    }

}
