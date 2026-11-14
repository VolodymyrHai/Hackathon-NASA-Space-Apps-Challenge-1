using UnityEngine;
using UnityEngine.UI;

public class HealPartBody : MonoBehaviour
{
    [SerializeField] private ButtonPress bp;
    public void HPB(int bpt)
    {
        bp.bpt = (BodyPartType)bpt;
        gameObject.SetActive(false);
        bp.StartSequence();
    }
}
