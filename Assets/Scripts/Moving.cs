using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Moving : MonoBehaviour
{
    public float speed = 4f;
    public float runSpeed = 7f;
    [Tooltip("Turn rate multiplier. Existing scene values are preserved.")]
    public float rotationSpeed = 10f;
    public Transform cameraTransform;

    [Header("Movement inertia")]
    [Min(0.02f)] public float turnSmoothTime = 0.22f;
    [Min(0.02f)] public float accelerationTime = 0.28f;
    [Min(0.02f)] public float brakingTime = 0.14f;
    [Range(0f, 1f)] public float sharpTurnSpeed = 0.15f;

    [Header("Moon jump")]
    [Min(0.01f)] public float moonGravity = 1.62f;
    [Min(0.1f)] public float jumpHeight = 1.5f;
    [Min(0f)] public float airAcceleration = 1.5f;

    [Header("Jump forgiveness and recovery")]
    [Range(0f, 0.2f)] public float coyoteTime = 0.10f;
    [Range(0f, 0.2f)] public float jumpBufferTime = 0.12f;
    public bool recoverBelowTerrain = true;

    private readonly RaycastHit[] groundHits = new RaycastHit[32];
    private MoonWorldGenerator world;
    private Vector3 startPosition;
    private float originalStepOffset;
    private float clock;
    private float lastGrounded = float.NegativeInfinity;
    private float lastJumpPress = float.NegativeInfinity;
    private float nextRecoveryCheck;
    private float verticalVelocity;
    private Vector3 horizontalVelocity;
    private CameraController orbitCamera;
    private Vector2 previousInput;
    private Vector3 inputForward = Vector3.forward;
    private Vector3 inputRight = Vector3.right;
    private Animator animator;
    private CharacterController controller;
    private float turnVelocity;
    private float currentSpeed;
    private float speedVelocity;
    private bool runningAnimation;
    public bool IsSprinting => runningAnimation;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        originalStepOffset = controller.stepOffset;
        startPosition = transform.position;
        world = FindAnyObjectByType<MoonWorldGenerator>();
        animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.applyRootMotion = false;
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        Keyboard keyboard = Application.isFocused ? Keyboard.current : null;
        Vector2 input = Vector2.zero;
        if (keyboard != null)
        {
            input.x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            input.y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
        }
        bool sprint = keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
        SimulateMovement(input, sprint, keyboard != null && keyboard.spaceKey.wasPressedThisFrame,
            keyboard != null && keyboard.eKey.wasPressedThisFrame, dt);
    }

    // Input and the motor are separate so grounded, airborne and collision cases
    // can be exercised deterministically without driving an editor keyboard.
    void SimulateMovement(Vector2 input, bool sprintHeld, bool jumpPressed, bool dancePressed, float dt)
    {
        if (dt <= 0f || controller == null || !controller.enabled) return;
        clock += dt;
        if (orbitCamera == null && cameraTransform != null) orbitCamera = cameraTransform.GetComponent<CameraController>();
        bool inputChanged = input != previousInput;
        if (inputChanged && orbitCamera != null) orbitCamera.NotifyMovementInputChanged();
        previousInput = input;
        bool hasInput = input.sqrMagnitude > 0f && cameraTransform != null;
        bool sprint = hasInput && sprintHeld;
        float walkSpeed = Mathf.Max(0f, speed);
        float sprintSpeed = Mathf.Max(walkSpeed, runSpeed);
        float targetSpeed = 0f;

        if (hasInput)
        {
            // Automatic camera return must not steer a held W/A/S/D direction.
            // Keep the previous input basis until keys change or manual look resumes.
            if (orbitCamera == null || !orbitCamera.IsAutomaticallyReturning || inputChanged)
            {
                inputForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);
                if (inputForward.sqrMagnitude < .0001f)
                    inputForward = Vector3.ProjectOnPlane(cameraTransform.up, Vector3.up);
                inputForward.Normalize();
                inputRight = Vector3.Cross(Vector3.up, inputForward);
            }
            Vector3 forward = inputForward;
            Vector3 right = inputRight;
            Vector3 desired = (forward * input.y + right * input.x).normalized;
            float targetYaw = Mathf.Atan2(desired.x, desired.z) * Mathf.Rad2Deg;
            float yaw = transform.eulerAngles.y;
            float angle = Mathf.Abs(Mathf.DeltaAngle(yaw, targetYaw));

            // Ease into angular movement and cap its speed. Translate along the
            // body's heading, so direction changes form an arc instead of sliding.
            float maxTurnRate = Mathf.Max(0.1f, rotationSpeed) * 36f;
            float nextYaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref turnVelocity,
                Mathf.Max(0.02f, turnSmoothTime), maxTurnRate, dt);
            transform.rotation = Quaternion.Euler(0f, nextYaw, 0f);

            float turnAmount = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(25f, 150f, angle));
            float turnFactor = Mathf.Lerp(1f, Mathf.Clamp01(sharpTurnSpeed), turnAmount);
            targetSpeed = (sprint ? sprintSpeed : walkSpeed) * turnFactor;
        }
        else
        {
            // Release angular inertia without rotating after input is released.
            turnVelocity = Mathf.MoveTowards(turnVelocity, 0f, 1440f * dt);
        }

        float easeTime = targetSpeed < currentSpeed ? brakingTime : accelerationTime;
        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedVelocity,
            Mathf.Max(0.02f, easeTime), Mathf.Infinity, dt);
        if (!hasInput && currentSpeed < 0.025f)
        {
            currentSpeed = 0f;
            speedVelocity = 0f;
        }
        bool wasGrounded = verticalVelocity <= 0f && (controller.isGrounded || ProbeGround());
        if (wasGrounded) lastGrounded = clock;
        if (jumpPressed) lastJumpPress = clock;
        bool jumped = clock - lastGrounded <= Mathf.Clamp(coyoteTime, 0f, .2f) &&
            clock - lastJumpPress <= Mathf.Clamp(jumpBufferTime, 0f, .2f);
        if (jumped)
        {
            lastGrounded = lastJumpPress = float.NegativeInfinity;
        }
        controller.stepOffset = wasGrounded && !jumped ? originalStepOffset : 0f;
        Vector3 desiredVelocity = transform.forward * currentSpeed;
        if (wasGrounded)
        {
            horizontalVelocity = desiredVelocity;
            verticalVelocity = -Mathf.Max(2f, currentSpeed * 1.5f);
        }
        if (jumped)
        {
            verticalVelocity = Mathf.Sqrt(2f * Mathf.Max(0.01f, moonGravity) * Mathf.Max(0.1f, jumpHeight));
        }
        else if (!wasGrounded && hasInput)
        {
            // Preserve takeoff momentum; only gently steer while airborne.
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, desiredVelocity,
                Mathf.Max(0f, airAcceleration) * dt);
        }

        // Move does not apply Unity's global gravity. Integrate lunar gravity here
        // and use mean vertical speed to reduce frame-rate-dependent jump height.
        float nextVertical = Mathf.Max(-30f, verticalVelocity - Mathf.Max(0.01f, moonGravity) * dt);
        float verticalDisplacement = (verticalVelocity + nextVertical) * 0.5f * dt;
        verticalVelocity = nextVertical;
        if (controller.enabled)
        {
            CollisionFlags hits = controller.Move(horizontalVelocity * dt + Vector3.up * verticalDisplacement);
            if ((hits & CollisionFlags.Above) != 0 && verticalVelocity > 0f) verticalVelocity = 0f;
            if ((hits & CollisionFlags.Below) != 0 && verticalVelocity < 0f) verticalVelocity = -1f;
        }
        else
        {
            verticalVelocity = 0f;
            horizontalVelocity = Vector3.zero;
        }
        bool onGround = !jumped && verticalVelocity <= 0f && (controller.isGrounded || ProbeGround());

        // Use actual motion so animation follows braking and stops at walls.
        float actualSpeed = controller.enabled ? Vector3.ProjectOnPlane(controller.velocity, Vector3.up).magnitude : 0f;
        bool moving = onGround && actualSpeed > 0.08f;
        float runGap = sprintSpeed - walkSpeed;
        if (runGap <= 0.01f || !moving) runningAnimation = false;
        else if (runningAnimation) runningAnimation = actualSpeed > walkSpeed + runGap * 0.10f;
        else runningAnimation = actualSpeed > walkSpeed + runGap * 0.30f;

        if (animator != null)
        {
            animator.SetBool("Grounded", onGround);
            if (jumped)
            {
                animator.ResetTrigger("Jump");
                animator.SetTrigger("Jump");
            }
            animator.SetBool("IsWalking", moving && !runningAnimation);
            animator.SetBool("IsRunning", moving && runningAnimation);
            if (hasInput || moving || !onGround) animator.SetBool("Dance", false);
            else if (dancePressed)
                animator.SetBool("Dance", !animator.GetBool("Dance"));
        }
        RecoverIfNeeded();
    }

    bool ProbeGround()
    {
        Bounds bounds = controller.bounds;
        float radius = Mathf.Max(.02f, controller.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z)) * .9f);
        Vector3 origin = new Vector3(bounds.center.x, bounds.min.y + radius + .04f, bounds.center.z);
        int count = Physics.SphereCastNonAlloc(origin, radius, Vector3.down, groundHits, .16f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float minimumNormal = Mathf.Cos(controller.slopeLimit * Mathf.Deg2Rad);
        for (int i = 0; i < count; i++)
        {
            var t = groundHits[i].transform;
            if (t == null || t == transform || t.IsChildOf(transform)) continue;
            if (groundHits[i].normal.y >= minimumNormal) return true;
        }
        return false;
    }

    void RecoverIfNeeded()
    {
        if (!recoverBelowTerrain || clock < nextRecoveryCheck) return;
        nextRecoveryCheck = clock + .5f;
        Vector3 destination;
        if (world != null && world.isActiveAndEnabled)
        {
            Vector3 local = world.transform.InverseTransformPoint(transform.position);
            float ground = world.SampleHeightLocal(local.x, local.z);
            if (local.y >= ground - 8f) return;
            Vector2 safe = world.flattenBaseSite ? world.baseCenter + new Vector2(0f, -30f) : new Vector2(0,0);
            destination = world.transform.TransformPoint(new Vector3(safe.x, world.SampleHeightLocal(safe.x, safe.y) + .6f, safe.y));
        }
        else
        {
            if (transform.position.y >= startPosition.y - 500f) return;
            destination = startPosition + Vector3.up;
        }
        controller.enabled = false;
        transform.position = destination;
        verticalVelocity = currentSpeed = speedVelocity = turnVelocity = 0f;
        horizontalVelocity = Vector3.zero;
        lastGrounded = lastJumpPress = float.NegativeInfinity;
        if (world != null) world.PreparePlayerCollision();
        controller.enabled = true;
        Physics.SyncTransforms();
    }

    void OnDisable()
    {
        currentSpeed = 0f;
        speedVelocity = 0f;
        turnVelocity = 0f;
        runningAnimation = false;
        verticalVelocity = 0f;
        horizontalVelocity = Vector3.zero;
        lastGrounded = lastJumpPress = float.NegativeInfinity;
        if (controller != null) controller.stepOffset = originalStepOffset;
        if (animator != null)
        {
            animator.ResetTrigger("Jump");
            animator.SetBool("Grounded", controller != null && controller.enabled && controller.isGrounded);
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsRunning", false);
        }
    }
}
