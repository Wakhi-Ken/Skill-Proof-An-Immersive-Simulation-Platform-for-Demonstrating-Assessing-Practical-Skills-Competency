using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuNavigation : MonoBehaviour
{
    public GameObject pauseCanvas;

    [Header("Loading screen (shown during every scene change)")]
    public GameObject loadingCanvas;
    [Tooltip("Optional progress bar")]
    public Slider progressBar;
    [Tooltip("Optional text such as 45%")]
    public TMP_Text progressText;
    [Tooltip("Shortest time the loading screen stays up, so it never just flashes")]
    public float minimumLoadSeconds = 1f;

    private bool isLoading;

    private void Start()
    {
        // The loading canvas stays hidden until a scene change starts
        if (loadingCanvas != null)
            loadingCanvas.SetActive(false);
    }

    // ---------- Scene buttons (same method names as before) ----------

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

    public void RestartGame()
    {
        LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnApplicationQuit()
    {
        Application.Quit();
    }

    // ---------- Pause ----------

    public void PauseGame()
    {
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

    // ---------- Loading ----------

    private void LoadScene(string sceneName)
    {
        if (isLoading) return;   // ignores a second tap while loading
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        isLoading = true;

        // A paused game has timeScale 0, which would freeze the next scene too
        Time.timeScale = 1f;

        if (pauseCanvas != null)
            pauseCanvas.SetActive(false);

        if (loadingCanvas != null)
            loadingCanvas.SetActive(true);

        UpdateProgress(0f);

        // Wait one frame so the loading canvas is drawn before loading starts
        yield return null;

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        if (operation == null)
        {
            Debug.LogError("MenuNavigation: could not load scene '" + sceneName
                           + "'. Check the name and that it is in Build Settings.");

            if (loadingCanvas != null)
                loadingCanvas.SetActive(false);

            isLoading = false;
            yield break;
        }

        // Hold the new scene back until loading is done and the minimum time has passed
        operation.allowSceneActivation = false;
        float startTime = Time.realtimeSinceStartup;

        while (operation.progress < 0.9f
               || Time.realtimeSinceStartup - startTime < minimumLoadSeconds)
        {
            // Unity reports 0 to 0.9 while loading, so scale it to 0 to 1
            UpdateProgress(Mathf.Clamp01(operation.progress / 0.9f));
            yield return null;
        }

        UpdateProgress(1f);
        operation.allowSceneActivation = true;

        // The new scene replaces this one, so the loading canvas disappears with it
    }

    private void UpdateProgress(float value)
    {
        if (progressBar != null)
            progressBar.value = value;

        if (progressText != null)
            progressText.text = Mathf.RoundToInt(value * 100f) + "%";
    }
}