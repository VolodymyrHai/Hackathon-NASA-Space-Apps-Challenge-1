using UnityEngine;

public class ZoneOxygen : MonoBehaviour
{
    [SerializeField] private bool exit;
    void Awake()
    {
        StaticBarsLogic.rada = 1;
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            if (exit)
            {
                StaticBarsLogic.Oxygen(0);
                StaticBarsLogic.Radiation(StaticBarsLogic.rada);
            }
            else
            {
                StaticBarsLogic.Oxygen(1);
                StaticBarsLogic.Radiation(-1);
            }
        }
    }

}
