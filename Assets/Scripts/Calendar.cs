using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class Calendar : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject CalendarCanvas;

    public void Open()
    {
        CalendarCanvas.SetActive(true);
    }
    public void Close()
    {
        CalendarCanvas.SetActive(false);
    }
}
