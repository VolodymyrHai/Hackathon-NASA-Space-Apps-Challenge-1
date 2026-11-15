using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Magas : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private Image image;
    [SerializeField] private Button[] btn;
    [SerializeField] private GameObject[] can;

    private bool buy = true;
    public void SwitchCanvases(int i)
    {
        if (i == 0)
        {
            can[0].gameObject.SetActive(true);
            can[1].gameObject.SetActive(false);
        }

        if (i == 1)
        {
            can[1].gameObject.SetActive(true);
            can[0].gameObject.SetActive(false);
        }
    }
    void Awake()
    {
        buy = true;
    }

    public void BuyMed()
    {
        if (!buy) return;
        Deliver(2, 0, 0);
        StartCoroutine(Stop());
    }

    public void BuyWat()
    {
        if (!buy) return;
        Deliver(0, 15, 0);
        StartCoroutine(Stop());
    }

    public void BuyFood()
    {
        if (!buy) return;
        Deliver(0, 0, 10);
        StartCoroutine(Stop());
    }

    private void Deliver(int medicine, int water, int food)
    {
        Instantiate(prefab);
        prefab.GetComponent<AirdropSpawner>().Init(new Vector3(13+Random.Range(-100,100),20,13+Random.Range(-100,100)),medicine,water,food);
    }

    private IEnumerator Stop()
    {
        for (int i = 0; i < btn.Length; i++)
        {
            btn[i].interactable = false;
        }
        buy = false;

        yield return new WaitForSeconds(1200);

        for (int i = 0; i < btn.Length; i++)
        {
            btn[i].interactable = true;
        }
        buy = true;
    }
}
