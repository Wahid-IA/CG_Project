using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float distance = 5.0f;
    public float xSensitivity = 3.0f;
    public float ySensitivity = 3.0f;
    public float yMinLimit = -20f;
    public float yMaxLimit = 80f;
    public Vector3 targetOffset = new Vector3(0, 1.5f, 0);

    private float currentX = 0.0f;
    private float currentY = 0.0f;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        currentX = angles.y;
        currentY = angles.x;

        // Lock and hide cursor during gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        // 1. Freeze camera movement completely while paused
        if (PauseMenu.isPaused) return;

        if (target != null)
        {
            currentX += Input.GetAxis("Mouse X") * xSensitivity;
            currentY -= Input.GetAxis("Mouse Y") * ySensitivity;
            currentY = Mathf.Clamp(currentY, yMinLimit, yMaxLimit);

            Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
            Vector3 targetPosition = target.position + targetOffset;
            Vector3 position = targetPosition - (rotation * Vector3.forward * distance);

            transform.rotation = rotation;
            transform.position = position;
        }

        // 2. Removed duplicate KeyCode.Escape check here so PauseMenu handles cursor toggling
    }
}