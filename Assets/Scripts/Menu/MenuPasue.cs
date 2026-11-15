using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuPasue : MonoBehaviour
{
    [SerializeField] private Image pauseMenu;
    [SerializeField] private Slider sensivity;
    private bool pause;
    void Awake()
    {
        sensivity.value = PlayerPrefs.GetFloat("Sens");
    }
    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            pause = !pause;
            pauseMenu.gameObject.SetActive(pause);
            Time.timeScale= pause ? 0f:1f;
        }
    }
    public void Resume()
    {
        pause = false;
        pauseMenu.gameObject.SetActive(false);
        Time.timeScale= 1f;
    }
    public void Slider()
    {
        PlayerPrefs.SetFloat("Sens",sensivity.value);
    }
    public void SaveExit()
    {
        Time.timeScale= 1f;
        SceneManager.LoadScene(0);
    }
}
