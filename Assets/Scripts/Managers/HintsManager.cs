using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// hints manager: shows the current hint for the current activity, or a message if all done or not started yet.
// Also flashes an image of the activity's expected outcome for the first few seconds.
public class HintsManager : MonoBehaviour
{
    [Serializable]
    public class HintStep
    {
        public string activityId;

        public string title;

        [TextArea(3, 6)] public string hint;

        //image showing the expected outcome of the activity. Shown for a few seconds at the start of each hint.
        public Sprite outcomeImage;
    }

    [Header("UI")]
    [SerializeField] private TMP_Text hintText;

    [Header("Outcome Image (shown in first seconds of each hint)")]
    [Tooltip("A UI Image in your Canvas. Position and size are set manually in the Inspector.")]
    [SerializeField] private Image outcomeImageUI;
    [SerializeField] private float imageDuration = 5f;

    [Header("Hints in order (first = activity 1)")]
    [SerializeField] private List<HintStep> steps = new List<HintStep>();

    [Header("Messages")]
    [SerializeField][TextArea(2, 4)] private string beforeStartMessage = "Press Start Simulation to begin.";
    [SerializeField][TextArea(2, 4)] private string allDoneMessage = "All activities complete. Press Complete to see your results.";
    [SerializeField][TextArea(2, 4)] private string sessionEndedMessage = "The session has ended.";

    private SessionTracker tracker;
    private Coroutine imageRoutine;
    private string lastShownActivityId;  

    private void OnValidate()
    {
        for (int i = 0; i < steps.Count; i++)
        {
            if (steps[i] != null && string.IsNullOrEmpty(steps[i].activityId))
                steps[i].activityId = "activity_" + (i + 1);
        }
    }

    private void Start()
    {
        if (outcomeImageUI != null)
            outcomeImageUI.gameObject.SetActive(false);

        tracker = SessionTracker.Instance;
        if (tracker == null)
        {
            Debug.LogWarning("HintManager: no SessionTracker found in the scene.");
        }
        else
        {
            tracker.OnSessionStarted += Refresh;
            tracker.OnProgressChanged += Refresh;
            tracker.OnSessionFinished += OnFinished;
        }
        Refresh();
    }

    private void OnDestroy()
    {
        if (tracker == null) return;
        tracker.OnSessionStarted -= Refresh;
        tracker.OnProgressChanged -= Refresh;
        tracker.OnSessionFinished -= OnFinished;
    }

    private void OnDisable()
    {
        HideImage();
    }

    private void OnFinished(PerformanceData p) { Refresh(); }

    private void Refresh()
    {
        if (hintText == null) return;

        // Session not started yet
        if (tracker == null || tracker.Results == null || tracker.Results.Count == 0)
        {
            hintText.text = beforeStartMessage;
            lastShownActivityId = null;
            HideImage();
            return;
        }

        // Only count steps that exist in this session (useFirstActivities may trim the list)
        var active = new List<HintStep>();
        foreach (HintStep s in steps)
            if (tracker.Results.Exists(r => r.activityID == s.activityId))
                active.Add(s);

        for (int i = 0; i < active.Count; i++)
        {
            ActivityResult r = tracker.Results.Find(x => x.activityID == active[i].activityId);
            if (r != null && !r.completed)
            {
                string title = string.IsNullOrEmpty(active[i].title) ? "" : " " + active[i].title;
                hintText.text = $"Step {i + 1} of {active.Count}:{title}\n{active[i].hint}";

                // Only show the image when the step actually changes
                if (lastShownActivityId != active[i].activityId)
                {
                    lastShownActivityId = active[i].activityId;
                    ShowOutcomeImage(active[i].outcomeImage);
                }
                return;
            }
        }

        // Nothing left to do
        lastShownActivityId = null;
        HideImage();
        hintText.text = tracker.CompletedCount == tracker.Results.Count
            ? allDoneMessage
            : sessionEndedMessage;   // e.g. time ran out or Finish pressed early
    }

    // ---------- Outcome image ----------

    private void ShowOutcomeImage(Sprite sprite)
    {
        if (outcomeImageUI == null) return;

        if (imageRoutine != null) StopCoroutine(imageRoutine);

        if (sprite == null)
        {
            outcomeImageUI.gameObject.SetActive(false);
            return;
        }

        outcomeImageUI.sprite = sprite;
        outcomeImageUI.gameObject.SetActive(true);

        imageRoutine = StartCoroutine(HideImageAfterDelay());
    }

    private IEnumerator HideImageAfterDelay()
    {
        yield return new WaitForSecondsRealtime(imageDuration);
        HideImage();
    }

    private void HideImage()
    {
        if (imageRoutine != null)
        {
            StopCoroutine(imageRoutine);
            imageRoutine = null;
        }
        if (outcomeImageUI != null)
            outcomeImageUI.gameObject.SetActive(false);
    }
}