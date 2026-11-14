using UnityEngine;

public class BaseStation : MonoBehaviour
{
    public enum UseKind { Water, Food, Rest, Medical, Command, Power, Solar, Supply }
    public UseKind kind;
    public string stationName;
    public float holdSeconds = 1.5f;
    public int supplyIndex = -1;
    public GameObject usedMarker;
    public MoonBaseServices Owner { get; private set; }
    public void Configure(MoonBaseServices owner, UseKind useKind, string label, float seconds, int index = -1)
    {
        Owner = owner; kind = useKind; stationName = label; holdSeconds = Mathf.Max(.2f, seconds); supplyIndex = index;
        owner.Register(this);
    }
}
