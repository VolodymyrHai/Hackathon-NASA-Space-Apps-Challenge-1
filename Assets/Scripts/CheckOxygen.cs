using UnityEngine;

public class CheckOxygen : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            StaticBarsLogic.Oxygen(0);
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            StaticBarsLogic.Oxygen(1);
        }
    }
}
