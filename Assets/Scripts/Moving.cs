using UnityEngine;
using UnityEngine.InputSystem;

public class Moving : MonoBehaviour
{
    public float speed = 4f;
    public float rotationSpeed = 10f;
    public Transform cameraTransform;

    private Animator animator;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        if (Keyboard.current == null || cameraTransform == null)
            return;

        float x = 0f;
        float z = 0f;

        if (Keyboard.current.wKey.isPressed) z += 1f;
        if (Keyboard.current.sKey.isPressed) z -= 1f;
        if (Keyboard.current.aKey.isPressed) x -= 1f;
        if (Keyboard.current.dKey.isPressed) x += 1f;

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 movement = (forward * z + right * x).normalized;

        bool isMoving = movement.sqrMagnitude > 0f;

        if (isMoving)
        {
            transform.position += movement * speed * Time.deltaTime;
            
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        if (animator != null)
        {
            animator.SetBool("IsWalking", isMoving);

            if (isMoving)
            {
                animator.SetBool("Dance", false);
            }

            if (Keyboard.current.eKey.wasPressedThisFrame && !isMoving) 
            { 
                bool isDancing = animator.GetBool("Dance");
                animator.SetBool("Dance", !isDancing); 
            }
        }
    }
}