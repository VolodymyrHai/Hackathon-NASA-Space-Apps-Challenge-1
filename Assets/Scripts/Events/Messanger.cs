using System.Collections;
using TMPro;
using UnityEngine;

public class Messanger : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    public void Show()
    {
        StartCoroutine(Close());
    }
    private IEnumerator Close()
    {
        text.gameObject.SetActive(true);
        yield return new WaitForSeconds(5);
        text.gameObject.SetActive(false);
    }
}
