using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using Unity.Mathematics;

public class RadBarsLogic : MonoBehaviour
{
    [Header("Oxygen visual")]
    [SerializeField] private Image oxygenBar;
    [SerializeField] private TMP_Text oxygenText;
    [Header("Radiation visual")]    
    [SerializeField] private Image radiationBar;
    [SerializeField] private TMP_Text radiationText;

    [Header("Oxygen")]
    [SerializeField] private float oxygenInterval = 5f;
    [SerializeField, Range(0f, 100f)] private float oxygenDrain = 2f;
    [SerializeField, Range(0f, 100f)] private float oxygenRecovery = 4f;
    [Header("Radiation")]
    [SerializeField] private float radiationInterval = 5f;
    [SerializeField, Range(0f, 100f)] private float radiationDrain = 1f;
    [SerializeField, Range(0f, 100f)] private float radiationRecovery = 1f;


    private float oxygen = 100f;
    private float radiation = 0f;

    private float oxygenTimer;
    private float radiationTimer;

    [HideInInspector] public int oxygenChange = 1;
    [HideInInspector] public int radiationChange;
    void Awake()
    {
        oxygenChange = 1;
        StaticBarsLogic.rad = this;
    }
    void Update()
    {
        radiationTimer += Time.deltaTime;
        if (radiationTimer >= radiationInterval)
        {
            radiationTimer-=radiationInterval;
            ChangeRadiation();
        }
        oxygenTimer += Time.deltaTime;
        if (oxygenTimer >= oxygenInterval)
        {
            oxygenTimer-=oxygenInterval;
            ChangeOxygen();
        }
    }

    private void ChangeOxygen()
    {
        oxygen = Mathf.Clamp(oxygen + oxygenChange*oxygenRecovery-oxygenDrain,0,100);
        UpdateUI();
    }
    private void ChangeRadiation()
    {
        radiation = Mathf.Clamp(radiation + radiationChange*radiationDrain,0,100);
        UpdateUI();
    }
    private void UpdateUI()
    {
        oxygenBar.fillAmount = oxygen/100;
        oxygenText.text=(int)oxygen+"%";
        radiationBar.fillAmount = radiation/100;
        radiationText.text=(int)radiation+"%";
    }
    
}
