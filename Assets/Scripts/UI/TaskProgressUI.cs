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

    [Header("Safety score colors")]
    [Tooltip("At or above this score the text uses the Good color")]
    [SerializeField] private float goodThreshold = 80f;
    [Tooltip("At or above this score (but below Good) the text uses the Warning color")]
    [SerializeField] private float warningThreshold = 50f;
    [SerializeField] private Color goodColor = new Color(0.20f, 0.80f, 0.30f);     // green
    [SerializeField] private Color warningColor = new Color(1.00f, 0.75f, 0.10f);  // amber
    [SerializeField] private Color poorColor = new Color(0.90f, 0.20f, 0.20f);     // red

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

        if (errorsText != null)
            errorsText.text = "Errors: " + t.TotalErrors;

        if (safetyScoreText != null)
        {
            float score = t.SafetyScore;
            safetyScoreText.text = "Safety: " + Mathf.RoundToInt(score) + "%";
            safetyScoreText.color = GetSafetyColor(score);
        }

        if (progressText != null)
            progressText.text = t.CompletedCount + " / " + t.Results.Count + " tasks";
    }

    private Color GetSafetyColor(float score)
    {
        if (score >= goodThreshold) return goodColor;
        if (score >= warningThreshold) return warningColor;
        return poorColor;
    }
}