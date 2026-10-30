
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuNavigation : MonoBehaviour
{
    [Header("Pause")]
    public GameObject pauseCanvas;

    [Header("Loading Screen")]
    public GameObject loadingCanvas;

    [Tooltip("Optional loading progress bar")]
    public Slider progressBar;

    [Tooltip("Optional loading percentage text")]
    public TMP_Text progressText;

    [Tooltip("Minimum time the loading screen stays visible")]
    public float minimumLoadSeconds = 1f;

    private bool isLoading = false;

    private void Start()
    {
        // Always restore normal game speed
        Time.timeScale = 1f;

        // Hide loading screen at startup
        if (loadingCanvas != null)
            loadingCanvas.SetActive(false);

        // Hide pause screen at startup
        if (pauseCanvas != null)
            pauseCanvas.SetActive(false);
    }

    
    // SCENE NAVIGATION
    

    public void StartMenu()
    {
        LoadScene("Main Menu");
    }

    public void SimulationMenu()
    {
        LoadScene("Simulation Menu");
    }

    public void LoadSimulatorSCene1()
    {
        LoadScene("SImulation 1");
    }

    public void LoadSimulatorSCene2()
    {
        LoadScene("SImulation 2");
    }

    
    // RESTART CURRENT SCENE
   

    public void RestartGame()
    {
        string currentSceneName =
            SceneManager.GetActiveScene().name;

        LoadScene(currentSceneName);
    }

    // QUIT APPLICATION


    public void OnApplicationQuit()
    {
        Application.Quit();
    }

  
    // PAUSE AND RESUME


    public void PauseGame()
    {
        if (isLoading)
            return;

        if (pauseCanvas != null)
            pauseCanvas.SetActive(true);

        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;

        if (pauseCanvas != null)
            pauseCanvas.SetActive(false);
    }

   
    // SCENE LOADING


    private void LoadScene(string sceneName)
    {
        if (isLoading)
            return;

        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        isLoading = true;

        // Restore normal time before loading
        Time.timeScale = 1f;

        // Hide pause screen
        if (pauseCanvas != null)
            pauseCanvas.SetActive(false);

        // Show loading screen
        if (loadingCanvas != null)
            loadingCanvas.SetActive(true);

        UpdateProgress(0f);

        // Give Unity time to display loading screen
        yield return null;

        // Load destination scene in Single mode.
        // This unloads the previous scene.
        AsyncOperation operation =
            SceneManager.LoadSceneAsync(
                sceneName,
                LoadSceneMode.Single
            );

        if (operation == null)
        {
            Debug.LogError(
                "MenuNavigation: Could not load scene: "
                + sceneName
                + ". Check Build Settings."
            );

            if (loadingCanvas != null)
                loadingCanvas.SetActive(false);

            isLoading = false;
            yield break;
        }

        // Wait before activating the new scene
        operation.allowSceneActivation = false;

        float startTime = Time.realtimeSinceStartup;

        while (
            operation.progress < 0.9f ||
            Time.realtimeSinceStartup - startTime
            < minimumLoadSeconds
        )
        {
            float progress = Mathf.Clamp01(
                operation.progress / 0.9f
            );

            UpdateProgress(progress);

            yield return null;
        }

        // Loading completed
        UpdateProgress(1f);

        // Activate the new scene
        operation.allowSceneActivation = true;
    }

  
    // LOADING PROGRESS
  

    private void UpdateProgress(float value)
    {
        if (progressBar != null)
            progressBar.value = value;

        if (progressText != null)
        {
            progressText.text =
                Mathf.RoundToInt(value * 100f) + "%";
        }
    }
}
