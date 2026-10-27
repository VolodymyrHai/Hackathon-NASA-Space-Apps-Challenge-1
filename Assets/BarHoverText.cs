using UnityEngine;
using UnityEngine.EventSystems;

public class BarHoverText : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject textObject;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (textObject != null)
        {
            textObject.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (textObject != null)
        {
            textObject.SetActive(false);
        }
    }
    
    private void Start()
    {
        if (textObject != null)
        {
            textObject.SetActive(false);
        }
    }

}

