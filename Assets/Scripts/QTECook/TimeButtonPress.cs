using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TimeButtonPress : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private Image[] showStadia;
    [SerializeField] private TMP_Text fals;
    [SerializeField] private int amount;
    [SerializeField] private float timer;
    [SerializeField] private bool falsstart;
    [SerializeField] private int failed;
    [SerializeField] private bool first;
    private bool play;
    void Awake()
    {
        first =true;
        play = false;
    }
    void OEnable()
    {
        StartQTE();
    }
    public void StartQTE()
    {
        amount = 0;
        failed = 0;
        CommandCentreInteraction.food-=10;
        image.color = Color.red;
        StartCoroutine(Wait());
    }
    private IEnumerator Wait()
    {
        play = false;
        falsstart = true;
        image.color = Color.red;
        yield return new WaitForSeconds(Random.Range(5,10));    
        image.color = Color.green;
        timer = 1f;
        falsstart = false;
        play = true;
    }
    private void Update()
    {
        timer-=Time.deltaTime;
        if(amount == 3)
        {
            StaticBarsLogic.ChangeHunger(24 - 8*failed);
            gameObject.SetActive(false);
        }
        else if (falsstart && !first&& Keyboard.current.anyKey.wasPressedThisFrame)
        {
            amount+=1;
            Failed();
        }
        else if (falsstart && first&& Keyboard.current.anyKey.wasPressedThisFrame)
        {
            amount+=1;
            fals.gameObject.SetActive(true);
            showStadia[amount-1].color = Color.red;
            StartCoroutine(text());
        }
        else if (timer <= 0 && play)
        {
            amount+=1;
            Failed();
        }
        else if(play && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            amount+=1;
            showStadia[amount-1].color = Color.green;
            StartCoroutine(Wait());
        }
    }
    private void Failed()
    {
        showStadia[amount-1].color = Color.red;
        failed+=1;
        StaticBarsLogic.Damage(BodyPartType.body, 5);
        StaticBarsLogic.ChangeHunger(1);
        StaticBarsLogic.ChangeThirst(-10);
        StaticBarsLogic.ChangeMind(-5);
        StartCoroutine(Wait());
    }
    private IEnumerator text()
    {
        yield return new WaitForSeconds(5);
        fals.gameObject.SetActive(false);
    }

}
