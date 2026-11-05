
using UnityEngine;
using UnityEngine.InputSystem;

public class OrbitCamera : MonoBehaviour
{
    public Transform pivot;

    public float distance = 8f;
    public float sensitivity = 0.2f;

    public float zoomSpeed = 0.01f;
    public float minDistance = 2f;
    public float maxDistance = 20f;

    public float yaw = 0f;
    public float pitch = 60f;

    void LateUpdate()
    {
        if (pivot == null) return;

        if (Mouse.current != null)
        {
            if (Mouse.current.rightButton.isPressed)
            {
                Vector2 mouse = Mouse.current.delta.ReadValue();

                yaw += mouse.x * sensitivity;
                pitch -= mouse.y * sensitivity;
                pitch = Mathf.Clamp(pitch, 15f, 85f);
            }

            float scroll = Mouse.current.scroll.ReadValue().y;

            distance -= scroll * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        transform.position =
            pivot.position - rotation * Vector3.forward * distance;

        transform.rotation = rotation;
    }
}
