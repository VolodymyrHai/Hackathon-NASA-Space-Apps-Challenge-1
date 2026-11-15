using UnityEngine;
using UnityEngine.InputSystem;

public class FixSolarPanel : MonoBehaviour
{
    [SerializeField]private WiresMinigame wm;
    bool tr = false;
    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            tr = true;
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            tr = false;
        }
    }
    void Update()
    {
        if(tr && EnergyCost.broken && Keyboard.current.eKey.wasPressedThisFrame)
        {
            wm.Open();
        }
    }
}
