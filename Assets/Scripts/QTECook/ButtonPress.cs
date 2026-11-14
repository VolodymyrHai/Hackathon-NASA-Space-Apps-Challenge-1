using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using System.Collections;

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

    public BodyPartType bpt;
    public void StartSequence()
    {
        attempts = 0;
        errors = 0;
        isRunning = true;
        gameObject.SetActive(true);
        text.gameObject.SetActive(false);
        StartCoroutine(waitPlay());
    }
    public IEnumerator waitPlay()
    {

        yield return new WaitForSeconds(2);
        text.gameObject.SetActive(true);
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
        images[errors].color = Color.red;
        errors++;
        StaticBarsLogic.ChangeMind(-5);
        if (StaticBarsLogic.model.Body[bpt].HP <= StaticBarsLogic.model.Body[bpt].maxHP / 2)
        {
            StaticBarsLogic.ChangeMind(-5);
        }
        if (StaticBarsLogic.model.Body[bpt].HP <= StaticBarsLogic.model.Body[bpt].maxHP *0.3)
        {
            StaticBarsLogic.ChangeMind(-10);
        }
        if (errors >= maxErrors)
        {
            EndSequence();
            return;
        }

        timeLeft = timePerKey;
    }


    private void EndSequence()
    {
        images[0].color = Color.white;
        images[1].color = Color.white;
        images[2].color = Color.white;
        StaticBarsLogic.Heal(bpt,30 - 10*errors);
        isRunning = false;
        currentKey = Key.None;
        gameObject.SetActive(false);
    }
}