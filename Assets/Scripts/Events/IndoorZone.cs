using UnityEngine;

/// <summary>
/// Глобальний стан "гравець у приміщенні".
/// Читається скриптами подій (DustStorm, SolarRadiation).
/// </summary>
public static class IndoorState
{
    private static int zonesInside;

    /// <summary>Можна виставити вручну, наприклад якщо приміщення це окрема сцена.</summary>
    public static bool ForceIndoors;

    public static bool IsIndoors => ForceIndoors || zonesInside > 0;

    internal static void Add(int amount) => zonesInside = Mathf.Max(0, zonesInside + amount);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        zonesInside = 0;
        ForceIndoors = false;
    }
}

/// <summary>
/// Повісьте на об'єкт з Collider (Is Trigger) який покриває приміщення.
/// Поки гравець (тег Player) усередині, IndoorState.IsIndoors = true.
/// Працює і в 3D, і в 2D.
/// </summary>
public class IndoorZone : MonoBehaviour
{
    public string playerTag = "Player";

    private int inside;

    private void Reset()
    {
        var c3 = GetComponent<Collider>();
        if (c3 != null) c3.isTrigger = true;

        var c2 = GetComponent<Collider2D>();
        if (c2 != null) c2.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other) => Enter(other);
    private void OnTriggerExit(Collider other) => Exit(other);

    private void Enter(Component other)
    {
        if (!IsPlayer(other)) return;
        inside++;
        IndoorState.Add(1);
    }

    private void Exit(Component other)
    {
        if (!IsPlayer(other) || inside <= 0) return;
        inside--;
        IndoorState.Add(-1);
    }

    private bool IsPlayer(Component other)
    {
        if (other.CompareTag(playerTag)) return true;

        // колайдер може бути на дочірньому об'єкті гравця
        var rb = other.GetComponentInParent<Rigidbody>();
        if (rb != null && rb.CompareTag(playerTag)) return true;

        var rb2 = other.GetComponentInParent<Rigidbody2D>();
        return rb2 != null && rb2.CompareTag(playerTag);
    }

    private void OnDisable()
    {
        // якщо зону вимкнули, поки гравець всередині
        if (inside > 0)
        {
            IndoorState.Add(-inside);
            inside = 0;
        }
    }
}