using System.Collections;
using TMPro;
using UnityEngine;

public class Messanger : MonoBehaviour
{
    [SerializeField] private GameObject canvas;
    public void Show()
    {
        StartCoroutine(Close());
    }
    private IEnumerator Close()
    {
        canvas.SetActive(true);
        yield return new WaitForSeconds(5);
        canvas.SetActive(false);
    }
}
