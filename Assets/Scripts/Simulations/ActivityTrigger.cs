using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ActivityTrigger : MonoBehaviour
{
    [Tooltip("Must match the activityID in Firestore, e.g. activity_1")]
    public string activityID = "activity_1";

    [Tooltip("Leave empty to accept any object. Otherwise only this tag counts (the tag must exist).")]
    public string requiredTag = "";

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag)) return;
        Complete();
    }

    // Public so you can also call it from XR events (socket snapped, button pressed)
    public void Complete()
    {
        if (SessionTracker.Instance != null)
            SessionTracker.Instance.CompleteActivity(activityID);
    }
}