using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
    public Transform pivot;
    public float distance = 8f, sensitivity = .2f, zoomSpeed = .01f, minDistance = 2f, maxDistance = 20f;
    public float yaw, pitch = 18f;

    [Header("Camera safety")]
    [Min(.05f)] public float collisionRadius = .20f;
    [Min(.01f)] public float rotationSmoothTime = .08f;
    public LayerMask obstructionMask = ~0;
    [Min(1f)] public float safeViewDistance = 3.5f;
    public Shader occlusionShader;
    [Min(.03f)] public float dissolveTime = .18f;

    [Header("Indoor camera")]
    public bool lowerPitchIndoors = true;
    [Range(5, 45)] public float indoorPitch = 10f;
    [Min(1f)] public float ceilingCheckHeight = 4.5f;

    [Header("Return behind player")]
    public bool autoReturn = true;
    public bool followEveryMove = true;
    [Min(.2f)] public float straightRunDelay = 1.8f;
    [Min(.2f)] public float manualReturnDelay = 2.5f;
    [Range(5, 85)] public float basePitch = 16f;
    [Min(1)] public float returnDegreesPerSecond = 65f;

    public bool IsAutomaticallyReturning { get; private set; }
    public void NotifyMovementInputChanged() { stableRunTime = 0; IsAutomaticallyReturning = false; }

    readonly RaycastHit[] hits = new RaycastHit[96];
    readonly HashSet<Renderer> blocked = new HashSet<Renderer>();
    readonly Dictionary<Renderer, FadeState> faded = new Dictionary<Renderer, FadeState>();
    readonly List<Renderer> remove = new List<Renderer>();
    Transform playerRoot;
    CharacterController controller;
    Moving motor;
    Camera view;
    float shownYaw, shownPitch, yawVelocity, pitchVelocity, zoomVelocity, shownDistance;
    float stableRunTime, sinceManual;
    Vector3 runHeading;
    bool initialized, ownsCursor, waitForRelease;
    CursorLockMode previousLock;
    bool previousVisible;

    sealed class FadeState
    {
        public Material[] originals, temporary;
        public MaterialPropertyBlock saved = new MaterialPropertyBlock(), working = new MaterialPropertyBlock();
        public float visibility = 1;
    }

    static bool Valid(float f) { return !float.IsNaN(f) && !float.IsInfinity(f); }
    void OnEnable() { initialized = false; stableRunTime = sinceManual = 0; }
    void OnDisable() { ReleaseCursor(); RestoreOccluders(); }
    void OnDestroy() { RestoreOccluders(); }
    void OnApplicationFocus(bool focused) { if (!focused) { ReleaseCursor(); waitForRelease = true; stableRunTime = sinceManual = 0; } }

    void CaptureCursor()
    {
        if (ownsCursor) return;
        previousLock = Cursor.lockState;
        previousVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        ownsCursor = true;
    }

    void ReleaseCursor()
    {
        if (!ownsCursor) return;
        Cursor.lockState = previousLock;
        Cursor.visible = previousVisible;
        ownsCursor = false;
    }

    void LateUpdate()
    {
        if (pivot == null) { ReleaseCursor(); RestoreOccluders(); return; }
        Vector3 center = pivot.position;
        if (!Valid(center.x) || !Valid(center.y) || !Valid(center.z)) return;
        if (!Valid(yaw)) yaw = 0;
        if (!Valid(pitch)) pitch = 18f;
        if (!Valid(distance)) distance = 8;

        float minimum = Mathf.Max(Valid(minDistance) ? minDistance : 2, Mathf.Max(1, safeViewDistance));
        float maximum = Mathf.Max(minimum, Valid(maxDistance) ? maxDistance : 20);
        distance = Mathf.Clamp(distance, minimum, maximum);

        if (!initialized)
        {
            controller = pivot.GetComponentInParent<CharacterController>();
            playerRoot = controller != null ? controller.transform : pivot.parent;
            motor = playerRoot != null ? playerRoot.GetComponent<Moving>() : null;
            view = GetComponent<Camera>();
            shownYaw = yaw;
            shownPitch = Mathf.Clamp(pitch, 5, 85);
            shownDistance = distance;
            yawVelocity = pitchVelocity = zoomVelocity = 0;
            initialized = true;
        }

        Mouse mouse = Mouse.current;
        bool manual = false;
        bool escape = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        if (escape) { ReleaseCursor(); waitForRelease = true; manual = true; }
        if (mouse == null || !mouse.rightButton.isPressed) { ReleaseCursor(); waitForRelease = false; }

        if (Application.isFocused && mouse != null)
        {
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (mouse.rightButton.isPressed && !waitForRelease && (ownsCursor || !overUI))
            {
                bool justCaptured = !ownsCursor;
                CaptureCursor();
                manual = true;
                Vector2 delta = mouse.delta.ReadValue();
                if (!justCaptured && Valid(delta.x) && Valid(delta.y) && Valid(sensitivity))
                {
                    yaw = Mathf.Repeat(yaw + delta.x * sensitivity, 360);
                    pitch -= delta.y * sensitivity;
                }
            }
            float scroll = mouse.scroll.ReadValue().y;
            if (!overUI && Valid(scroll) && Valid(zoomSpeed) && Mathf.Abs(scroll) > .001f)
            {
                distance = Mathf.Clamp(distance - scroll * zoomSpeed, minimum, maximum);
                manual = true;
            }
        }

        float dt = Time.deltaTime;
        Vector3 velocity = controller != null ? Vector3.ProjectOnPlane(controller.velocity, Vector3.up) : Vector3.zero;
        bool running = motor != null ? motor.IsSprinting : velocity.magnitude > 2.4f;
        SimulateReturn(dt, manual || !Application.isFocused, velocity, running);
        pitch = Mathf.Clamp(pitch, 5, 85);

        float targetPitch = pitch;
        if (lowerPitchIndoors && IsUnderRoofOrTightSpace(center, shownYaw, pitch, distance))
        {
            targetPitch = Mathf.Min(pitch, indoorPitch);
        }

        if (dt > 0)
        {
            shownYaw = Mathf.SmoothDampAngle(shownYaw, yaw, ref yawVelocity, Mathf.Max(.01f, rotationSmoothTime), Mathf.Infinity, dt);
            shownPitch = Mathf.SmoothDampAngle(shownPitch, targetPitch, ref pitchVelocity, Mathf.Max(.01f, rotationSmoothTime * 2f), Mathf.Infinity, dt);
        }

        Quaternion rotation = Quaternion.Euler(shownPitch, shownYaw, 0);
        Vector3 direction = -(rotation * Vector3.forward);
        float hardLimit;
        float target = ResolveDistance(center, direction, distance, out hardLimit);

        if (dt > 0) shownDistance = Mathf.SmoothDamp(shownDistance, target, ref zoomVelocity, .24f, Mathf.Infinity, dt);
        else shownDistance = target;

        shownDistance = Mathf.Min(shownDistance, hardLimit);
        transform.SetPositionAndRotation(center + direction * shownDistance, rotation);
        UpdateOccluders(dt);
    }

    void SimulateReturn(float dt, bool manual, Vector3 velocity, bool running)
    {
        IsAutomaticallyReturning = false;
        if (dt <= 0) return;
        if (manual) { sinceManual = stableRunTime = 0; return; }
        sinceManual += dt;

        if (followEveryMove && autoReturn && velocity.sqrMagnitude > .05f && sinceManual >= manualReturnDelay)
        {
            IsAutomaticallyReturning = true;
            Vector3 moveDir = velocity.normalized;
            float moveYaw = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
            yaw = Mathf.MoveTowardsAngle(yaw, moveYaw, Mathf.Max(1, returnDegreesPerSecond) * dt);
            pitch = Mathf.MoveTowards(pitch, basePitch, Mathf.Max(1, returnDegreesPerSecond) * .65f * dt);
            return;
        }

        if (!autoReturn || !running || velocity.sqrMagnitude < .25f) { stableRunTime = 0; return; }
        Vector3 direction = velocity.normalized;
        if (stableRunTime <= 0 || Vector3.Angle(runHeading, direction) > 12) { runHeading = direction; stableRunTime = 0; }
        stableRunTime += dt;
        if (stableRunTime < straightRunDelay || sinceManual < manualReturnDelay) return;
        IsAutomaticallyReturning = true;
        float targetYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        yaw = Mathf.MoveTowardsAngle(yaw, targetYaw, Mathf.Max(1, returnDegreesPerSecond) * dt);
        pitch = Mathf.MoveTowards(pitch, basePitch, Mathf.Max(1, returnDegreesPerSecond) * .65f * dt);
    }

    bool IsUnderRoofOrTightSpace(Vector3 center, float currentYaw, float currentPitch, float currentDist)
    {
        int count = Physics.RaycastNonAlloc(center, Vector3.up, hits, ceilingCheckHeight, obstructionMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            if (!Own(hits[i].transform)) return true;
        }

        Quaternion highRot = Quaternion.Euler(currentPitch, currentYaw, 0);
        Vector3 highDir = -(highRot * Vector3.forward);
        count = Physics.SphereCastNonAlloc(center, Mathf.Max(.05f, collisionRadius), highDir, hits, currentDist, obstructionMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            if (!Own(hits[i].transform) && Dissolvable(hits[i].collider) == null)
            {
                return true;
            }
        }
        return false;
    }

    bool Own(Transform t) { return t == null || t == transform || (playerRoot != null && (t == playerRoot || t.IsChildOf(playerRoot))); }

    Renderer Dissolvable(Collider collider)
    {
        if (occlusionShader == null || collider == null) return null;
        var mesh = collider as MeshCollider;
        if (mesh != null && mesh.sharedMesh != null && mesh.sharedMesh.name == "Moon mesh") return null;
        var renderer = collider.GetComponent<Renderer>();
        if (renderer == null || renderer is SkinnedMeshRenderer || !renderer.enabled) return null;
        foreach (var material in renderer.sharedMaterials)
            if (material == null) return null;
        return renderer;
    }

    float ResolveDistance(Vector3 origin, Vector3 direction, float requested, out float hard)
    {
        int count = Physics.SphereCastNonAlloc(origin, Mathf.Max(.05f, collisionRadius), direction, hits, requested, obstructionMask, QueryTriggerInteraction.Ignore);
        hard = requested;
        float soft = requested;
        for (int i = 0; i < count; i++)
        {
            if (Own(hits[i].transform)) continue;
            float limit = Mathf.Max(.08f, hits[i].distance - .08f);
            if (Dissolvable(hits[i].collider) != null) soft = Mathf.Min(soft, limit);
            else hard = Mathf.Min(hard, limit);
        }
        return Mathf.Min(hard, Mathf.Max(Mathf.Min(requested, safeViewDistance), soft));
    }

    void MarkLine(Vector3 target)
    {
        Vector3 delta = transform.position - target;
        float length = delta.magnitude;
        if (length < .05f) return;
        int count = Physics.SphereCastNonAlloc(target, .16f, delta / length, hits, length, obstructionMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            if (!Own(hits[i].transform))
            {
                var r = Dissolvable(hits[i].collider);
                if (r != null) blocked.Add(r);
            }
        }
    }

    void UpdateOccluders(float dt)
    {
        blocked.Clear();
        Vector3 body = controller != null ? controller.bounds.center : pivot.position;
        MarkLine(body);
        MarkLine(pivot.position);

        foreach (var renderer in blocked)
        {
            if (faded.ContainsKey(renderer)) continue;
            var state = new FadeState();
            state.originals = renderer.sharedMaterials;
            state.temporary = new Material[state.originals.Length];
            renderer.GetPropertyBlock(state.saved);

            for (int i = 0; i < state.originals.Length; i++)
            {
                Material source = state.originals[i];
                var copy = new Material(source) { shader = occlusionShader, hideFlags = HideFlags.DontSave };
                if (source.HasProperty("_BaseColor"))
                    copy.SetColor("_BaseColor", source.GetColor("_BaseColor"));
                else if (source.HasProperty("_Color"))
                    copy.SetColor("_BaseColor", source.GetColor("_Color"));

                if (!source.HasProperty("_BaseMap") && source.HasProperty("_MainTex"))
                    copy.SetTexture("_BaseMap", source.GetTexture("_MainTex"));

                state.temporary[i] = copy;
            }
            renderer.sharedMaterials = state.temporary;
            faded.Add(renderer, state);
        }

        Vector4 screenCenter = new Vector4(.5f, .5f, 0, 0);
        Vector4 radius = new Vector4(.18f, .25f, 0, 0);

        if (view != null)
        {
            Vector3 p = view.WorldToViewportPoint(body);
            screenCenter = new Vector4(p.x, p.y, 0, 0);
            float height = controller != null ? Mathf.Max(.8f, controller.bounds.extents.y) : .8f;
            Vector3 top = view.WorldToViewportPoint(body + transform.up * height);
            Vector3 side = view.WorldToViewportPoint(body + transform.right * .6f);
            radius = new Vector4(
                Mathf.Clamp(Mathf.Abs(side.x - p.x) + .045f, .08f, .48f),
                Mathf.Clamp(Mathf.Abs(top.y - p.y) + .05f, .13f, .65f),
                0,
                0
            );
        }

        remove.Clear();
        foreach (var pair in faded)
        {
            Renderer renderer = pair.Key;
            FadeState state = pair.Value;

            if (renderer == null)
            {
                DisposeMaterials(state);
                remove.Add(renderer);
                continue;
            }

            float target = blocked.Contains(renderer) ? 0f : 1f;
            state.visibility = Mathf.MoveTowards(state.visibility, target, Mathf.Max(0, dt) / Mathf.Max(.03f, dissolveTime));

            renderer.GetPropertyBlock(state.working);
            state.working.SetFloat("_CameraFade", state.visibility);
            state.working.SetVector("_FadeCenter", screenCenter);
            state.working.SetVector("_FadeRadius", radius);
            renderer.SetPropertyBlock(state.working);

            if (target == 1 && state.visibility >= .999f)
            {
                Restore(renderer, state);
                remove.Add(renderer);
            }
        }

        foreach (var renderer in remove)
            faded.Remove(renderer);
    }

    static void DisposeMaterials(FadeState state)
    {
        foreach (var mat in state.temporary)
        {
            if (mat != null)
            {
                if (Application.isPlaying)
                    Destroy(mat);
                else
                    DestroyImmediate(mat);
            }
        }
    }

    static void Restore(Renderer renderer, FadeState state)
    {
        if (renderer != null)
        {
            renderer.sharedMaterials = state.originals;
            renderer.SetPropertyBlock(state.saved);
        }
        DisposeMaterials(state);
    }

    void RestoreOccluders()
    {
        foreach (var pair in faded)
            Restore(pair.Key, pair.Value);

        faded.Clear();
        blocked.Clear();
    }
}
