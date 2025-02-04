using UnityEngine;

public class SetCameraBackground : MonoBehaviour
{
    void Start()
    {
        // Get the camera component
        Camera mainCamera = Camera.main;
        
        // Set the background color to black
        mainCamera.backgroundColor = Color.black;
    }
}