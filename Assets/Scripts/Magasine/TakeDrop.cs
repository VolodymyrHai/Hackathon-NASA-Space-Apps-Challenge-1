using UnityEngine;
using UnityEngine.InputSystem;

public class TakeDrop : MonoBehaviour
{
    bool Player;
    void Update()
    {
        if(Player && Keyboard.current.eKey.wasPressedThisFrame)
        {
            AirdropSpawner s = gameObject.GetComponent<AirdropSpawner>();
            CommandCentreInteraction.food += s.food;
            CommandCentreInteraction.water += s.water;
            CommandCentreInteraction.medicine += s.medicine;
            Destroy(gameObject);
        }
    }
    void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player") 
        {
            Player=true;
        }
    }
    void OnTriggerExit(Collider other)
    {
        if(other.tag == "Player") 
        {
            Player=false;
        }
    }
}
