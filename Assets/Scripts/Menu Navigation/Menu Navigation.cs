using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuNavigation : MonoBehaviour
{
    public GameObject pauseCanvas;

    public void StartMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }
    public void SimulationMenu()
    {
        SceneManager.LoadScene("Simulation Menu");
    }
    public void LoadSimulatorSCene1()
    {
        SceneManager.LoadScene("SImulation 1");
    }

    public void LoadSimulatorSCene2()
    {
        SceneManager.LoadScene("SImulation 2");
    }

    public void OnApplicationQuit()
    {
        Application.Quit();
    }

    public void PauseGame()
    { 
        if (pauseCanvas != null) { pauseCanvas.SetActive(true);
        } Time.timeScale = 0f; 
    }
    public void ResumeGame()
    { 
        Time.timeScale = 1f;
        if (pauseCanvas != null)
        {
            pauseCanvas.SetActive(false);
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

}
