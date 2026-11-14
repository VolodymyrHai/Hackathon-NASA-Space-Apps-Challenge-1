using UnityEngine;

public class CheckBards : MonoBehaviour
{
    public int change;
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            StaticBarsLogic.Radiation(change);
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            StaticBarsLogic.Radiation(0);
        }
    }
}
