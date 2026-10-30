using Firebase.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CompleteSimulationUI : MonoBehaviour
{
    [Header("Results canvas")]
    [SerializeField] private GameObject completeCanvas;

    [Header("Hide these when the results appear ")]
    [SerializeField] private GameObject[] hideOnComplete;

    [Header("Texts ")]
    [SerializeField] private TMP_Text userNameText;
    [SerializeField] private TMP_Text simulationNameText;
    [SerializeField] private TMP_Text competencyText;      // demonstrated or not (green / red)
    [SerializeField] private TMP_Text feedbackText;        // performance feedback
    [SerializeField] private TMP_Text tasksText;           // Completed, or how many are not
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text errorsText;
    [SerializeField] private TMP_Text safetyText;
    [SerializeField] private TMP_Text overallScoreText;    // overall score of all measurements
    [SerializeField] private TMP_Text saveStatusText;

    [Header("Sliders (0 to 100). ")]
    [SerializeField] private Slider proceduralSlider;
    [SerializeField] private Slider complianceSlider;
    [SerializeField] private Slider competenceSlider;

    [Header("value text")]
    [SerializeField] private TMP_Text proceduralValueText;
    [SerializeField] private TMP_Text complianceValueText;
    [SerializeField] private TMP_Text competenceValueText;

    [Header("Colors")]
    [SerializeField] private Color goodColor = new Color(0.20f, 0.80f, 0.30f);   // green
    [SerializeField] private Color badColor = new Color(0.90f, 0.20f, 0.20f);    // red

    private bool subscribed;

    private void Start()
    {
        if (completeCanvas == null)
            Debug.LogWarning("CompleteSimulationUI: the Complete Canvas slot is empty.");
        else
            completeCanvas.SetActive(false);

        PrepareSlider(proceduralSlider);
        PrepareSlider(complianceSlider);
        PrepareSlider(competenceSlider);

        Subscribe();
    }

    private void OnDestroy()
    {
        if (subscribed && SessionTracker.Instance != null)
        {
            SessionTracker.Instance.OnSessionFinished -= ShowResults;
            SessionTracker.Instance.OnPerformanceSaved -= ShowSaveStatus;
            SessionTracker.Instance.OnSessionStarted -= HideResults;
        }
    }

    private void Subscribe()
    {
        if (subscribed) return;

        if (SessionTracker.Instance == null)
        {
            Debug.LogWarning("CompleteSimulationUI: no SessionTracker found in the scene.");
            return;
        }

        SessionTracker.Instance.OnSessionFinished += ShowResults;
        SessionTracker.Instance.OnPerformanceSaved += ShowSaveStatus;
        SessionTracker.Instance.OnSessionStarted += HideResults;
        subscribed = true;
        Debug.Log("CompleteSimulationUI: listening for the end of the session.");
    }

    // This is called by a "Finish" button in the simulation, or by the SessionTracker when the last task is completed
    public void CompleteSimulation()
    {
        if (SessionTracker.Instance == null) return;
        SessionTracker.Instance.FinishSession();   // does nothing if no session is running
    }

    private void HideResults()
    {
        if (completeCanvas != null)
            completeCanvas.SetActive(false);
    }

    private void ShowResults(PerformanceData p)
    {
        Debug.Log("CompleteSimulationUI: showing results.");

        foreach (GameObject go in hideOnComplete)
        {
            if (go == null) continue;

            // Hiding a parent of the results canvas would hide the results too
            if (completeCanvas != null && completeCanvas.transform.IsChildOf(go.transform))
            {
                Debug.LogWarning("CompleteSimulationUI: '" + go.name
                    + "' contains the results canvas, so it was not hidden. Remove it from Hide On Complete.");
                continue;
            }

            go.SetActive(false);
        }

        // Count finished and unfinished tasks
        int done = 0;
        int total = 0;
        if (p.activities != null)
        {
            total = p.activities.Count;
            foreach (ActivityResult a in p.activities)
                if (a.completed) done++;
        }
        int notDone = total - done;

        float safety = (float)p.safetyScore;
        float competence = (float)p.competencyScore;
        float procedural = (float)p.proceduralScore;
        float compliance = (float)p.compliance;

        // Overall score of the whole simulation = average of the four measurements shown
        float overall = (safety + competence + procedural + compliance) / 4f;

        int seconds = Mathf.RoundToInt((float)p.completionTime);

        // User name
        SetText(userNameText, GetUserName());
        SetText(simulationNameText, p.simulationName);

        // Competency: demonstrated or not
        if (competencyText != null)
        {
            competencyText.text = p.competent
                ? "Competency demonstrated"
                : "Competency not demonstrated";
            competencyText.color = p.competent ? goodColor : badColor;
        }

        SetText(feedbackText, p.feedbackText);

        // Tasks: Completed, or how many were not completed
        if (tasksText != null)
        {
            tasksText.text = p.allTasksCompleted
                ? $"Tasks: Completed ({done}/{total})"
                : $"Tasks: {notDone} not completed ({done}/{total} done)";
            tasksText.color = p.allTasksCompleted ? goodColor : badColor;
        }

        SetText(timeText, $"{seconds / 60:00}:{seconds % 60:00}");
        SetText(errorsText, p.errors.ToString());
        SetText(safetyText, Mathf.RoundToInt(safety) + "%");
        SetText(overallScoreText, Mathf.RoundToInt(overall) + "%");
        SetText(saveStatusText, "Saving your results...");

        SetSlider(proceduralSlider, proceduralValueText, procedural, "Procedural accuracy: ");
        SetSlider(complianceSlider, complianceValueText, compliance, "Compliance: ");
        SetSlider(competenceSlider, competenceValueText, competence, "Competence: ");

        if (completeCanvas != null)
        {
            completeCanvas.SetActive(true);

            if (!completeCanvas.activeInHierarchy)
                Debug.LogWarning("CompleteSimulationUI: the results canvas is switched on but still hidden "
                    + "because one of its parents is inactive.");
        }
    }

    private void ShowSaveStatus(bool saved)
    {
        SetText(saveStatusText, saved
            ? "Results saved."
            : "Could not save your results. Check your connection.");
    }

    // Helpers
    private string GetUserName()
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null) return "";

        return string.IsNullOrEmpty(user.DisplayName) ? user.Email : user.DisplayName;
    }

    private void SetText(TMP_Text target, string value)
    {
        if (target != null) target.text = value;
    }

    private void PrepareSlider(Slider slider)
    {
        if (slider == null) return;
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.interactable = false;
    }

    private void SetSlider(Slider slider, TMP_Text valueText, float value, string label)
    {
        if (slider != null) slider.value = value;
        if (valueText != null) valueText.text = label + Mathf.RoundToInt(value) + "%";
    }
}