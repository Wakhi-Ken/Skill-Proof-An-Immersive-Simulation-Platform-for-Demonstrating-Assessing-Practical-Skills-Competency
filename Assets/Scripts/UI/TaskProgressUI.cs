using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TaskProgressUI : MonoBehaviour
{
    [Header("One toggle and one label per activity, in order")]
    [SerializeField] private Toggle[] activityToggles;
    [SerializeField] private TMP_Text[] activityLabels;

    [Header("Live numbers")]
    [SerializeField] private TMP_Text errorsText;
    [SerializeField] private TMP_Text safetyScoreText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text timerText;

    private bool subscribed;

    private void Start()
    {
        Subscribe();
        Refresh();
    }

    private void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    private void OnDestroy()
    {
        if (subscribed && SessionTracker.Instance != null)
        {
            SessionTracker.Instance.OnProgressChanged -= Refresh;
            SessionTracker.Instance.OnSessionStarted -= Refresh;
        }
    }

    private void Subscribe()
    {
        if (subscribed || SessionTracker.Instance == null) return;

        SessionTracker.Instance.OnProgressChanged += Refresh;
        SessionTracker.Instance.OnSessionStarted += Refresh;
        subscribed = true;
    }

    private void Update()
    {
        SessionTracker t = SessionTracker.Instance;
        if (t != null && t.IsRunning && timerText != null)
        {
            int seconds = Mathf.FloorToInt(t.ElapsedTime);
            timerText.text = $"Time: {seconds / 60:00}:{seconds % 60:00}";
        }
    }

    private void Refresh()
    {
        SessionTracker t = SessionTracker.Instance;
        if (t == null) return;

        for (int i = 0; i < activityToggles.Length; i++)
        {
            bool hasActivity = i < t.Results.Count;

            activityToggles[i].gameObject.SetActive(hasActivity);
            if (!hasActivity) continue;

            activityToggles[i].interactable = false;
            activityToggles[i].SetIsOnWithoutNotify(t.Results[i].completed);

            if (i < activityLabels.Length && activityLabels[i] != null)
                activityLabels[i].text = t.Results[i].name;
        }

        if (errorsText != null) errorsText.text = "Errors: " + t.TotalErrors;
        if (safetyScoreText != null) safetyScoreText.text = "Safety: " + Mathf.RoundToInt(t.SafetyScore) + "%";
        if (progressText != null) progressText.text = t.CompletedCount + " / " + t.Results.Count + " tasks";
    }
}