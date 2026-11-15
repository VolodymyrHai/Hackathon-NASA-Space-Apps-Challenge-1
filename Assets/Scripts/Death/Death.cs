using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Death : MonoBehaviour
{
    [SerializeField]private GameObject canvas;
    void Awake()
    {
        StaticBarsLogic.d=this;
    }
    public void SmertjVNishchitje()
    {
        StartCoroutine(corot());
    }
    public IEnumerator corot()
    {
        canvas.SetActive(true);
        yield return new WaitForSeconds(5);
        canvas.SetActive(false);
        SceneManager.LoadScene(0);
    }
}
