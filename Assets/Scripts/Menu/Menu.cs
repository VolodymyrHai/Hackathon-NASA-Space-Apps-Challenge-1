using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{

    public void Awake()
    {
        if (!PlayerPrefs.HasKey("Sens"))
        {
            PlayerPrefs.SetFloat("Sens",50f);
        }
    }
    public void Exit()
    {
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif
    }
    public void Play()
    {
        SceneManager.LoadScene(1);
    }
}
