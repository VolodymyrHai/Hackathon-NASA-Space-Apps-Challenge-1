using UnityEngine;
using UnityEngine.InputSystem;

public class DoorOpen : MonoBehaviour
{
    [Header("Doors")]
    [SerializeField] private Transform leftDoor;
    [SerializeField] private Transform rightDoor;
    [SerializeField] private float openDistance = 1.4f;
    [SerializeField] private float openTime = 0.6f;
    [SerializeField] private bool closeOnExit = true;

    [Header("Anti spam")]
    [SerializeField] private float cooldown = 1.2f;

    [Header("Hint")]
    [SerializeField] private GameObject hintCanvas;

    private Vector3 leftClosed;
    private Vector3 rightClosed;
    private float leftDirection = -1f;
    private float rightDirection = 1f;
    private float amount;
    private float nextToggleTime;
    private bool open;
    private bool playerInside;
    private const string PlayerTag = "Player";

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Start()
    {
        leftClosed = leftDoor.localPosition;
        rightClosed = rightDoor.localPosition;

        float difference = leftClosed.x - rightClosed.x;
        if (Mathf.Abs(difference) > 0.001f)
        {
            leftDirection = Mathf.Sign(difference);
            rightDirection = -leftDirection;
        }

        if (hintCanvas != null) hintCanvas.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(PlayerTag)) return;
        playerInside = true;
        if (hintCanvas != null) hintCanvas.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(PlayerTag)) return;
        playerInside = false;
        if (closeOnExit) open = false;
        if (hintCanvas != null) hintCanvas.SetActive(false);
    }

    private void OnDisable()
    {
        playerInside = false;
        if (hintCanvas != null) hintCanvas.SetActive(false);
    }

    private void Update()
    {
        if (playerInside
            && Time.time >= nextToggleTime
            && Keyboard.current != null
            && Keyboard.current.eKey.wasPressedThisFrame)
        {
            open = !open;
            nextToggleTime = Time.time + cooldown;
        }

        float target = open ? 1f : 0f;
        amount = Mathf.MoveTowards(amount, target, Time.deltaTime / Mathf.Max(0.01f, openTime));

        float offset = openDistance * Mathf.SmoothStep(0f, 1f, amount);

        leftDoor.localPosition = leftClosed + Vector3.right * (leftDirection * offset);
        rightDoor.localPosition = rightClosed + Vector3.right * (rightDirection * offset);
    }
}