using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ActivityTrigger : MonoBehaviour
{
    [Tooltip("Must match the activityID in Firestore, e.g. activity_5")]
    public string activityID = "activity_1";

    [Tooltip("Leave empty to accept any object. Otherwise only this tag counts (the tag must exist).")]
    public string requiredTag = "";

    [Tooltip("If true, removing the object from the socket makes this task incomplete again " +
             "(only possible while the activity hasn't been fully completed yet).")]
    public bool undoOnRemove = true;

    [Tooltip("Seconds after an object is socketed before this collider is switched off.")]
    public float disableDelay = 5f;

    private static readonly Dictionary<string, List<ActivityTrigger>> groups = new Dictionary<string, List<ActivityTrigger>>();
    private static readonly HashSet<string> activityDone = new HashSet<string>();

    //Fired every time a task of any activity is completed. ActivityTestable listens to this.
    public static event Action<ActivityTrigger> TaskCompleted;

    //True once ALL tasks of this activity are complete.
    public static bool IsActivityComplete(string id) => activityDone.Contains(id);

    private Collider col;
    private bool taskDone;
    private Coroutine disableRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        groups.Clear();
        activityDone.Clear();
        TaskCompleted = null;
    }

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Awake()
    {
        col = GetComponent<Collider>();

        if (groups.TryGetValue(activityID, out var list) && !list.Contains(this))
            list.Add(this);
    }

    private void OnDestroy()
    {
        if (groups.TryGetValue(activityID, out var list))
            list.Remove(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (taskDone) return;
        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag)) return;
        Complete();
    }

    // Hook to the socket's "Select Entered" event
    public void OnSocketEntered()
    {
        Complete();

        // Restart the timer; the collider is only switched off after the delay
        CancelDisable();
        disableRoutine = StartCoroutine(DisableAfterDelay());
    }

    // Hook to the socket's "Select Exited" event
    public void OnSocketExited()
    {
        // Cancel any pending disable and make sure the collider is on
        CancelDisable();
        if (col != null) col.enabled = true;

        // Undo the task only if the activity hasn't already been reported
        if (undoOnRemove && taskDone && !activityDone.Contains(activityID))
            taskDone = false;
    }

    private IEnumerator DisableAfterDelay()
    {
        yield return new WaitForSeconds(disableDelay);
        if (col != null) col.enabled = false;
        disableRoutine = null;
    }

    private void CancelDisable()
    {
        if (disableRoutine != null)
        {
            StopCoroutine(disableRoutine);
            disableRoutine = null;
        }
    }

    public void Complete()
    {
        if (taskDone || activityDone.Contains(activityID)) return;
        taskDone = true;

        var list = GetGroup(activityID);
        int done = 0;
        foreach (var t in list)
            if (t.taskDone) done++;

        Debug.Log($"{activityID}: {done}/{list.Count} tasks complete");

        if (done >= list.Count)
        {
            activityDone.Add(activityID);
            if (SoundManager.Instance != null) SoundManager.Instance.PlayActivityComplete();
            if (SessionTracker.Instance != null)
                SessionTracker.Instance.CompleteActivity(activityID);
        }
        else
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayTaskCorrect();
        }

        TaskCompleted?.Invoke(this);
    }

    private static List<ActivityTrigger> GetGroup(string id)
    {
        if (groups.TryGetValue(id, out var list)) return list;

        list = new List<ActivityTrigger>();
        var all = FindObjectsByType<ActivityTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in all)
            if (t.activityID == id) list.Add(t);

        groups[id] = list;
        return list;
    }
}