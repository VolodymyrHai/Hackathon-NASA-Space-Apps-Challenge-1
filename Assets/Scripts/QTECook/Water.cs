using UnityEngine;
using UnityEngine.InputSystem;

public class Water : MonoBehaviour
{
    bool f;
    void Update()
    {
        if(f && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if(CommandCentreInteraction.water>=5){
            CommandCentreInteraction.water-=5;
            StaticBarsLogic.ChangeThirst(20);}
        }
    }
    void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player")
        {
            f=true;
        }
    }
    void OnTriggerExit(Collider other)
    {
        if(other.tag == "Player")
        {
            f=false;
        }
    }
}
