using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class CommandCentreInteraction : MonoBehaviour
{
    [Header ("UI")]
    [SerializeField] private GameObject canvas;
    [SerializeField] private GameObject hintCanvas;

    [Header("TextMeshPro")]
    [SerializeField] private TextMeshProUGUI waterTMP;
    [SerializeField] private TextMeshProUGUI foodTMP;
    [SerializeField] private TextMeshProUGUI energyTMP;
    [SerializeField] private TextMeshProUGUI WaterTMP;
    [SerializeField] private TextMeshProUGUI medicineTMP;
    [SerializeField] private TextMeshProUGUI solarPanelTMP;

    private int water;
    private int food;
    private int energy;
    private int medicine;
    private bool isSolarPanelActive;


    private string playerTag = "Player";
    private bool isActiveCanvas;

    private void Start()
    {
        waterTMP.text = "Water: 15";
        foodTMP.text = "Food: 10";
        energyTMP.text = "Energy: 100%";
        medicineTMP.text = "Medicine: 5";
        solarPanelTMP.text = "Solar Panel: Active";

        water = 15;
        food = 10;
        energy = 100;
        medicine = 5;
        isSolarPanelActive = true;

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
    }
}