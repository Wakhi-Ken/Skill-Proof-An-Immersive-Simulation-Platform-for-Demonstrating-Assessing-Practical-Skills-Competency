using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// hints manager: shows the current hint for the current activity, or a message if all done or not started yet.
public class HintsManager : MonoBehaviour
{
    [Serializable]
    public class HintStep
    {
    
        public string activityId;

   
        public string title;

        [TextArea(3, 6)] public string hint;
    }

    [Header("UI")]
    [SerializeField] private TMP_Text hintText;

    [Header("Hints in order (first = activity 1)")]
    [SerializeField] private List<HintStep> steps = new List<HintStep>();

    [Header("Messages")]
    [SerializeField][TextArea(2, 4)] private string beforeStartMessage = "Press Start Simulation to begin.";
    [SerializeField][TextArea(2, 4)] private string allDoneMessage = "All activities complete. Press Complete to see your results.";
    [SerializeField][TextArea(2, 4)] private string sessionEndedMessage = "The session has ended.";

    private SessionTracker tracker;

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

    private void OnFinished(PerformanceData p) { Refresh(); }

    private void Refresh()
    {
        if (hintText == null) return;

        // Session not started yet
        if (tracker == null || tracker.Results == null || tracker.Results.Count == 0)
        {
            hintText.text = beforeStartMessage;
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
                return;
            }
        }

        // Nothing left to do
        hintText.text = tracker.CompletedCount == tracker.Results.Count
            ? allDoneMessage
            : sessionEndedMessage;   // e.g. time ran out or Finish pressed early
    }
}