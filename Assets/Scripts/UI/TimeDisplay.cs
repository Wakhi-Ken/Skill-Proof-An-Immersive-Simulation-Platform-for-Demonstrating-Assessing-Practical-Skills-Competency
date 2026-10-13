using TMPro;
using UnityEngine;

public class TimeDisplay : MonoBehaviour
{
    [Header("Drag the two TextMeshPro texts here")]
    [SerializeField] private TMP_Text canvasTimerText;   // task progress canvas
    [SerializeField] private TMP_Text watchTimerText;    // watch canvas

    [Header("Wording")]
    [SerializeField] private string prefix = "Time: ";
    [SerializeField] private string countdownPrefix = "Remaining: ";
    [SerializeField] private bool showCountdownIfLimitSet = true;

    private void Update()
    {
        string text = BuildText();

        if (canvasTimerText != null) canvasTimerText.text = text;
        if (watchTimerText != null) watchTimerText.text = text;
    }

    private string BuildText()
    {
        SessionTracker t = SessionTracker.Instance;
        if (t == null) return prefix + "00:00";

        bool countdown = t.IsRunning && t.HasTimeLimit && showCountdownIfLimitSet;
        float shown = countdown ? t.TimeRemaining : t.ElapsedTime;
        string head = countdown ? countdownPrefix : prefix;

        int seconds = Mathf.FloorToInt(shown);
        return $"{head}{seconds / 60:00}:{seconds % 60:00}";
    }
}