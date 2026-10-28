using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ErrorTrigger : MonoBehaviour
{
    [Tooltip("Optional: the activity this mistake belongs to, e.g. activity_2")]
    public string relatedActivityID = "";

    [Tooltip("Tick if this is a safety mistake (lowers the safety score)")]
    public bool isSafetyViolation = true;

    [Tooltip("Leave empty to accept any object. Otherwise only this tag counts.")]
    public string requiredTag = "";

    [Tooltip("Seconds before the same trigger can count again")]
    public float cooldown = 2f;

    private float lastTime = -999f;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag)) return;
        Report();
    }

    // Public so XR events or buttons can call it too
    public void Report()
    {
        if (Time.time - lastTime < cooldown) return;
        lastTime = Time.time;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayWrong();

        if (SessionTracker.Instance != null)
            SessionTracker.Instance.RegisterError(relatedActivityID, isSafetyViolation);
    }
}