using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlaySingleplayer()
    {
        SceneManager.LoadScene("GamemodeSelect"); // rename later if needed
    }

    public void OpenOptions()
    {
        Debug.Log("Options not implemented yet");
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game Quit");
    }
}
