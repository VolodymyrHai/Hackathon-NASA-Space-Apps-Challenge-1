using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class CommandCentreInteraction : MonoBehaviour
{
    [Header ("UI")]
    [SerializeField] private GameObject canvas;
    [SerializeField] private GameObject hintCanvas;

    [Header("TextMeshPro")]
    [SerializeField] private TextMeshProUGUI waterTMP;
    [SerializeField] private TextMeshProUGUI foodTMP;
    [SerializeField] private TextMeshProUGUI energyTMP;
    [SerializeField] private TextMeshProUGUI medicineTMP;
    [SerializeField] private TextMeshProUGUI solarPanelTMP;

    public static int water;
    public static int food;
    public float energy;
    public static int medicine;
    public bool isSolarPanelActive;


    private string playerTag = "Player";
    private bool isActiveCanvas;

    /*private void Start()
    {
        water = 15;
        food = 10;
        energy = 100;
        medicine = 5;
        isSolarPanelActive = true;

        waterTMP.text = "Water: "+water;
        foodTMP.text = "Food: "+food;
        energyTMP.text = "Energy: "+energy+"%";
        medicineTMP.text = "Medicine: "+medicine;
        solarPanelTMP.text = "Solar Panel: Active";

    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == playerTag)
        {
            hintCanvas.SetActive(true);
        }

    }

    private void OnTriggerStay(Collider other)
    {
        if (other.tag == playerTag)
        {
            if (isActiveCanvas) hintCanvas.SetActive(false);

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                canvas.SetActive(true);
                isActiveCanvas = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        canvas.SetActive(false);
        hintCanvas.SetActive(false);
    }

    public void CloseCanvas()
    {
        canvas.SetActive(false);
        isActiveCanvas = false;
    }*/
}