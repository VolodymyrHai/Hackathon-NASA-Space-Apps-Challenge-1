using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public class ButtonPress : MonoBehaviour
{
    [SerializeField] private RectTransform text;
    [SerializeField] private TMP_Text textComponent;
    [SerializeField] private Image[] images;

    [Header("Час на відповідь (секунди)")]
    [SerializeField] private float timePerKey = 2f;

    [Header("Максимум помилок")]
    [SerializeField] private int maxErrors = 3;

    private static readonly Key[] allowedKeys =
    {
        Key.Q, Key.W, Key.E, Key.R, Key.T,
        Key.A, Key.S, Key.D, Key.F,
        Key.Z, Key.X, Key.C, Key.V
    };

    private int attempts = 0;
    private int errors = 0;
    private Key currentKey = Key.None;
    private float timeLeft;
    private bool isRunning = false;

    private void Start()
    {
        StartSequence();
    }

    public void StartSequence()
    {
        attempts = 0;
        errors = 0;
        isRunning = true;
        ShowNextText();
    }

    private void ShowNextText()
    {
        if (attempts >= 15)
        {
            EndSequence();
            return;
        }

        attempts++;

        float width = text.rect.width;
        float height = text.rect.height;

        float x = Random.Range(width / 2, Screen.width - width / 2);
        float y = Random.Range(height / 2, Screen.height - height / 2);

        text.position = new Vector2(x, y);

        currentKey = allowedKeys[Random.Range(0, allowedKeys.Length)];

        textComponent.text = currentKey.ToString();
        text.gameObject.SetActive(true);

        timeLeft = timePerKey;
    }

    private void Update()
    {
        if (!isRunning) return;
        if (Keyboard.current == null) return;
        if (currentKey == Key.None) return;

        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f)
        {
            RegisterError();
            return;
        }

        if (Keyboard.current[currentKey].wasPressedThisFrame)
        {
            ShowNextText();
            return;
        }

        foreach (Key key in allowedKeys)
        {
            if (key != currentKey && Keyboard.current[key].wasPressedThisFrame)
            {
                RegisterError();
                break;
            }
        }
    }

    private void RegisterError()
    {
        errors++;
        ShowError(); 

        if (errors >= maxErrors)
        {
            EndSequence();
            return;
        }

        timeLeft = timePerKey;
    }

    private void ShowError()
    {
        images[errors-1].color = Color.red;
        StaticBarsLogic.Damage(BodyPartType.body, 5);
        StaticBarsLogic.ChangeHunger(2);
        StaticBarsLogic.ChangeThirst(-5);
        StaticBarsLogic.ChangeMind(-2);
    }

    private void EndSequence()
    {
        StaticBarsLogic.ChangeHunger(27 - 9*errors);
        isRunning = false;
        currentKey = Key.None;
        text.gameObject.SetActive(false);
    }
}