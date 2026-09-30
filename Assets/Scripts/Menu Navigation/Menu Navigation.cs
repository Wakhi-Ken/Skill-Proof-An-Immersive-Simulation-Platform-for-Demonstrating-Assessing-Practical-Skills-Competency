using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuNavigation : MonoBehaviour
{

   
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

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

}
