using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Cook : MonoBehaviour
{
    [SerializeField] private Image canvas;
    [SerializeField] private GameObject pressE;
    bool Player;
    void Update()
    {
        if(Player && Keyboard.current.eKey.wasPressedThisFrame)
        {
            canvas.gameObject.SetActive(true);
            pressE.gameObject.SetActive(false);
        }
    }
    void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player") 
        {
            Player=true;
            pressE.gameObject.SetActive(true);
        }
    }
    void OnTriggerExit(Collider other)
    {
        if(other.tag == "Player") 
        {
            Player=false;
            pressE.gameObject.SetActive(false);
            canvas.gameObject.SetActive(false);
        }
    }
}
