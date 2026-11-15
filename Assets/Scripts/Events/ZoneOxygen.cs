using UnityEngine;

public class ZoneOxygen : MonoBehaviour
{
    [SerializeField] private bool exit;
    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            if (exit)
            {
                StaticBarsLogic.Oxygen(0);
                if(StaticBarsLogic.rada<=1){
                StaticBarsLogic.Radiation(1);
                           }
                                      }
            else
            {
                StaticBarsLogic.Oxygen(1);
                StaticBarsLogic.Radiation(-1);
            }
        }
    }

}
