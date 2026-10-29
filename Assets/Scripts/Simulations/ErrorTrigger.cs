using System.Collections.Generic;
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

    [Tooltip("Seconds before the alert sound can play again for this trigger")]
    public float cooldown = 2f;

    private static readonly List<ErrorTrigger> all = new List<ErrorTrigger>();

    // object - how many of its colliders are inside (an object can have several colliders)
    private readonly Dictionary<GameObject, int> inside = new Dictionary<GameObject, int>();
    private float lastAlert = -999f;
    private bool committed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => all.Clear();

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnEnable()
    {
        if (!all.Contains(this)) all.Add(this);
    }

    private void OnDisable()
    {
        all.Remove(this);
        inside.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag)) return;

        var key = KeyOf(other);
        inside.TryGetValue(key, out int count);
        inside[key] = count + 1;

        // Alert only: the error is NOT recorded here
        if (Time.time - lastAlert < cooldown) return;
        lastAlert = Time.time;
        if (SoundManager.Instance != null) SoundManager.Instance.PlayWrong();
    }

    private void OnTriggerExit(Collider other)
    {
        var key = KeyOf(other);
        if (!inside.TryGetValue(key, out int count)) return;

        if (count <= 1) inside.Remove(key);
        else inside[key] = count - 1;
    }

    private static GameObject KeyOf(Collider c)
    {
        return c.attachedRigidbody != null ? c.attachedRigidbody.gameObject : c.gameObject;
    }

  
    private bool HasWrongObject()
    {
        // Objects that were disabled/destroyed while inside never send OnTriggerExit, so clean them out
        var dead = new List<GameObject>();
        foreach (var kv in inside)
            if (kv.Key == null || !kv.Key.activeInHierarchy) dead.Add(kv.Key);
        foreach (var d in dead) inside.Remove(d);

        return inside.Count > 0;
    }

    //True if any ErrorTrigger of this activity currently has a wrong object in it.</summary>
    public static bool HasPending(string activityID)
    {
        foreach (var t in all)
            if (t.relatedActivityID == activityID && t.HasWrongObject()) return true;
        return false;
    }
    // Registers all pending errors (wrong objects still in the zones) as actual mistakes.</summary>
    public static void CommitPendingErrors()
    {
        foreach (var t in all)
        {
            if (t.committed || !t.HasWrongObject()) continue;
            t.committed = true;

            if (SessionTracker.Instance != null)
                SessionTracker.Instance.RegisterError(t.relatedActivityID, t.isSafetyViolation);
        }
    }

    //Immediate error. Only for actions that are clearly wrong the moment they happen (button / XR event).</summary>
    public void Report()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayWrong();
        if (SessionTracker.Instance != null)
            SessionTracker.Instance.RegisterError(relatedActivityID, isSafetyViolation);
    }
}