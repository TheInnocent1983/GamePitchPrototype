using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    float rotation_y = 0f;
    float rotation_x = 0f;

    public float minimum_x = -90f;
    public float maximum_x = 90f;

    public float sensitivity;
    public Camera cam;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        rotation_y += Input.GetAxis("Mouse Y") * sensitivity;
        rotation_x += Input.GetAxis("Mouse X") * sensitivity;

        rotation_y = Mathf.Clamp(rotation_y, minimum_x, maximum_x);

        // Apply rotations
        cam.transform.localEulerAngles = new Vector3(-rotation_y, 0, 0); // Rotate camera up and down
        transform.localEulerAngles = new Vector3(0, rotation_x, 0); // Rotate player left and right
    }
}
