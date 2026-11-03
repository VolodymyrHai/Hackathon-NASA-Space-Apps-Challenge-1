using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class BarsLogic : MonoBehaviour
{
    [SerializeField] private Image thirstBar;
    [SerializeField] private Image hungerBar;
    [SerializeField] private Image mindBar;

    [Header("Water")]
    [SerializeField] private float thirstInterval = 5f;
    [SerializeField, Range(0f, 100f)] private float thirstDrain = 2f;
    [Header("Food")]
    [SerializeField] private float hungerInterval = 5f;
    [SerializeField, Range(0f, 100f)] private float hungerDrain = 1f;

    [Header("Sanity")]
    [SerializeField, Range(0f, 100f)] private float startMind = 100f;

    [Header("Events when bar reaches zero")]
    public UnityEvent onThirstEmpty;
    public UnityEvent onHungerEmpty;
    public UnityEvent onMindEmpty;

    private float thirst = 100f;
    private float hunger = 100f;
    private float mind;

    private float thirstTimer;
    private float hungerTimer;

    private void Start()
    {
        mind = startMind;
        UpdateUI();
    }

    private void Update()
    {
        thirstTimer += Time.deltaTime;
        if (thirstTimer >= thirstInterval)
        {
            thirstTimer -= thirstInterval;
            ChangeThirst(-thirstDrain);
        }

        hungerTimer += Time.deltaTime;
        if (hungerTimer >= hungerInterval)
        {
            hungerTimer -= hungerInterval;
            ChangeHunger(-hungerDrain);
        }
    }


    public void ChangeThirst(float amount)
    {
        bool wasAlive = thirst > 0f;
        thirst = Mathf.Clamp(thirst + amount, 0f, 100f);
        if (wasAlive && thirst <= 0f) onThirstEmpty?.Invoke();
        UpdateUI();
    }

    public void ChangeHunger(float amount)
    {
        bool wasAlive = hunger > 0f;
        hunger = Mathf.Clamp(hunger + amount, 0f, 100f);
        if (wasAlive && hunger <= 0f) onHungerEmpty?.Invoke();
        UpdateUI();
    }

    public void ChangeMind(float amount)
    {
        bool wasAlive = mind > 0f;
        mind = Mathf.Clamp(mind + amount, 0f, 100f);
        if (wasAlive && mind <= 0f) onMindEmpty?.Invoke();
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (thirstBar) thirstBar.fillAmount = thirst / 100f;
        if (hungerBar) hungerBar.fillAmount = hunger / 100f;
        if (mindBar) mindBar.fillAmount = mind / 100f;
    }
}