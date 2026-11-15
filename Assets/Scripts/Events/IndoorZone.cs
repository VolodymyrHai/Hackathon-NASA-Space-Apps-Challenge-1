using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Глобальний стан "гравець у приміщенні".
/// Читається скриптами подій (DustStorm, SolarRadiation).
/// </summary>
public class IndoorZone : MonoBehaviour
{
    private GameObject ss;
    void Awake()
    {
        ss = GameObject.FindWithTag("SunCan");
    }
    void OnTriggerStay(Collider other)
    {
        if (other.tag == "Player")
        {
            ss.SetActive(false);
            DustStorm.Instance.SetVisuals(false);
            StaticBarsLogic.entry = true;
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            ss.SetActive(true);
            DustStorm.Instance.SetVisuals(true);
            StaticBarsLogic.entry = false;
        }
    }
}