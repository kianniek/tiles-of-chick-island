using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    // variables that indicate min and max values
    [Header("Min and max angle values")]
    [SerializeField] private float minXAngle;
    [SerializeField] private float maxXAngle;

    // input sensitivity variables
    [Header("Input settings")]
    [SerializeField] private float rotateSensitivity = 1f;
    [SerializeField] private float moveSensitivity = 1f;
    [SerializeField] private float zoomSensitivity = 1f;

    // current distance and angles
    private Vector3 currentEulerAngles;
    private Vector3 currentMove;

    // current inputs
    private float mouseRotateX;
    private float mouseRotateY;
    private float mouseMoveX;
    private float mouseMoveY;
    private float mouseZoom;

    /// <summary>
    /// Called once per frame.
    /// </summary>
    private void Update()
    {
        // get and clamp all inputs
        GetRotationInput();
        GetMoveInput();
        GetZoomInput();

        // apply changes to camera rotation and position
        SetRotationAndPosition();
    }

    /// <summary>
    /// Called by update to get and clamp rotation inputs.
    /// </summary>
    private void GetRotationInput()
    {
        // get rotation input
        if (Input.GetKey(KeyCode.Mouse1))
        {
            mouseRotateX = Input.GetAxis("Mouse X") * rotateSensitivity;
            mouseRotateY = Input.GetAxis("Mouse Y") * rotateSensitivity;
        }
        else
        {
            mouseRotateX = mouseRotateY = 0;
        }

        // apply rotation inputs to current angles
        currentEulerAngles.y += mouseRotateX;
        currentEulerAngles.x -= mouseRotateY;

        // keep angles between min and max values
        currentEulerAngles.x = Mathf.Clamp(currentEulerAngles.x, minXAngle, maxXAngle);
    }

    /// <summary>
    /// Called by update to get move inputs.
    /// </summary>
    private void GetMoveInput()
    {
        // get rotation input
        if (Input.GetKey(KeyCode.Mouse0))
        {
            mouseMoveX = -Input.GetAxis("Mouse X") * moveSensitivity;
            mouseMoveY = -Input.GetAxis("Mouse Y") * moveSensitivity;
        }
        else
        {
            mouseMoveX = mouseMoveY = 0;
        }

        // apply move inputs to the focus position
        currentMove.x = mouseMoveX;
        currentMove.y = mouseMoveY;
    }

    /// <summary>
    /// Called by update to get and clamp zoom input.
    /// </summary>
    private void GetZoomInput()
    {
        // get zoom input
        mouseZoom = Input.mouseScrollDelta.y * zoomSensitivity;

        // apply zoom input to distance
        currentMove.z = mouseZoom;
    }

    /// <summary>
    /// Called when tile map is just build, to center the camera on it.
    /// </summary>
    internal void CenterOnTileMap()
    {
        // get focus position
        Vector3 focusPosition = GameManager.instance.tileMap.centerMap;

        // rotate the camera
        currentEulerAngles = new Vector3(((maxXAngle - minXAngle) * 0.5f) + minXAngle, 0, 0);
        transform.eulerAngles = currentEulerAngles;

        // position the camera
        currentMove = transform.forward * 30;
        transform.position = focusPosition - currentMove;
    }

    /// <summary>
    /// Called to apply current variables on 
    /// the rotation and position of the camera.
    /// </summary>
    private void SetRotationAndPosition()
    {
        // rotate the camera
        transform.eulerAngles = currentEulerAngles;

        // position the camera
        transform.position += transform.TransformDirection(currentMove);
    }
}
