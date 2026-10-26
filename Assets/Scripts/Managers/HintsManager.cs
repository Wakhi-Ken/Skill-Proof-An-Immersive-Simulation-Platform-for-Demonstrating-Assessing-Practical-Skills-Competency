using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Shows a hint you type in the Inspector for the first activity the player
/// has not finished yet, in order. Put one HintManager in each simulation scene.
/// </summary>
public class HintManager : MonoBehaviour
{
    [Serializable]
    public class HintStep
    {
        [Tooltip("Must match the activityID used by your triggers, e.g. activity_1")]
        public string activityId;

        [Tooltip("Short title shown after the step number")]
        public string title;

        [Tooltip("The specific directions for the player")]
        [TextArea(3, 6)] public string hint;
    }

    [Header("UI")]
    [SerializeField] private TMP_Text hintText;

    [Header("Hints in order (first = activity 1)")]
    [SerializeField] private List<HintStep> steps = new List<HintStep>();

    [SerializeField]
    [TextArea(2, 4)]
    private string allDoneMessage = "All activities complete. Press Complete to see your results.";

    private readonly HashSet<string> _completed = new HashSet<string>();

    // Fills empty activity ids with activity_1, activity_2, ... when you add elements.
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
        Refresh();
    }

    /// <summary>Call from your session tracker, e.g. NotifyActivityCompleted("activity_1").</summary>
    public void NotifyActivityCompleted(string activityId)
    {
        _completed.Add(activityId);
        Refresh();
    }

    /// <summary>Call if an activity is undone and should be required again.</summary>
    public void NotifyActivityReset(string activityId)
    {
        _completed.Remove(activityId);
        Refresh();
    }

    /// <summary>Clears progress, e.g. when a simulation restarts.</summary>
    public void ResetProgress()
    {
        _completed.Clear();
        Refresh();
    }

    private void Refresh()
    {
        if (hintText == null) return;

        // Always point at the first activity that is not done yet, so the player
        // is sent back in order even if they finished one out of sequence.
        for (int i = 0; i < steps.Count; i++)
        {
            if (!_completed.Contains(steps[i].activityId))
            {
                string title = string.IsNullOrEmpty(steps[i].title) ? "" : " " + steps[i].title;
                hintText.text = $"Step {i + 1} of {steps.Count}:{title}\n{steps[i].hint}";
                return;
            }
        }

        hintText.text = allDoneMessage;
    }
}