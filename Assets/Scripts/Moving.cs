
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Moving : MonoBehaviour
{
    public float speed = 4f;
    public float runSpeed = 7f;
    public float rotationSpeed = 10f;
    public Transform cameraTransform;

    private Animator animator;
    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        if (animator != null)
            animator.applyRootMotion = false;

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

        bool shiftPressed =
            Keyboard.current.leftShiftKey.isPressed ||
            Keyboard.current.rightShiftKey.isPressed;

        bool isRunning = isMoving && shiftPressed;
        bool isWalking = isMoving && !isRunning;

        float currentSpeed = isRunning ? runSpeed : speed;

        // Movement with gravity and terrain collision
        controller.SimpleMove(movement * currentSpeed);

        if (isMoving)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        if (animator != null)
        {
            animator.SetBool("IsWalking", isWalking);
            animator.SetBool("IsRunning", isRunning);

            if (isMoving)
                animator.SetBool("Dance", false);

            if (Keyboard.current.eKey.wasPressedThisFrame && !isMoving)
            {
                bool isDancing = animator.GetBool("Dance");
                animator.SetBool("Dance", !isDancing);
            }
        }
    }
}

