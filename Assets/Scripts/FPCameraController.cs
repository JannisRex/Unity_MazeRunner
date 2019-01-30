using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPCameraController : MonoBehaviour {

    //Genral
    public Camera FPCamera;                         // ref to CameraGameObject
    private Vector3 CameraOffset;                   // Distance between Player and Camera

    
    // Sensitivity
    public float sensitivityX = 3f; // multiplier for mouse camera control
    public float sensitivityY = 3f; // multiplier for mouse camera control

    // X - CameraConstraints
    public float minimunX = -60f;   // Only lower head 60f  
    public float maxinumX = 60f;    // Only raise head 60f
    
    // Y - CameraConstraints
    public float minimunY = -360f;  // able to do full 360 sideways
    public float maximunY = 360f;   // able to do full 360 sideways

    //Roation
    private float rotationX;
    private float rotationY;


    void Update()
    {
        Rotation();                         // Apply Camera and Player Rotations                 
        UnlockMouse();                      // Make Windows Mouse Controllable again
    }

    void LateUpdate()
    {
        Position();                         // Apply Camera Position
    }

    void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;                           // Lock Mouse (to prevent getting out of the window)
        //CameraOffset = FPCamera.transform.position - transform.position;    //Distance between Camera and PlayerObject
        CameraOffset = new Vector3(0f, .5f, 0.1f);                          // Just choose an offset 
    }

    void Rotation()                 // Apply Camera and Player Rotations
    {
        rotationY += Input.GetAxis("Mouse X") * sensitivityX;                           // Gets X - Mouse Axis and multiplies with sens
        rotationX += Input.GetAxis("Mouse Y") * sensitivityY;                           // Gets Y - Mouse Axis and multiplies with sens

        rotationX = Mathf.Clamp(rotationX, minimunX, maxinumX);                         // Keep Rotation inside Min and Max

        transform.localEulerAngles = new Vector3(0, rotationY, 0);                      // Player should just rotates around Y-Axis

        FPCamera.transform.localEulerAngles = new Vector3(-rotationX, rotationY, 0);  // Cam needs to rotate both X and Y     (-rotationX to remove Y - Inversion)
    }

    void Position()                 // Keep Camera pos behind FPcollider
    {
        FPCamera.transform.position = transform.position + CameraOffset;          // Always keep offset on cam (following the Player)
    }

    void UnlockMouse()              // Stop locking Mouse in Unity (For exiting and such)
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
