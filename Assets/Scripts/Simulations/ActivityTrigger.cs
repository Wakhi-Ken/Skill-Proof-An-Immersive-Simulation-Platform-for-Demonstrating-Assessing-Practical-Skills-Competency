using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;


// A trigger collider that represents a task of an activity. The task is complete when the correct object is socketed or placed in the zone.
[RequireComponent(typeof(Collider))]
public class ActivityTrigger : MonoBehaviour
{
    public string activityID = "activity_1";

    [Header("Which object counts as correct")]

    public GameObject requiredObject;

    public string requiredTag = "";

    public XRSocketInteractor socket;

    [Header("Behaviour")]

    public bool undoOnRemove = true;

    public float disableDelay = 5f;

    private static readonly Dictionary<string, List<ActivityTrigger>> groups = new Dictionary<string, List<ActivityTrigger>>();
    private static readonly HashSet<string> activityDone = new HashSet<string>();

    // Fired every time a task of any activity is completed. ActivityTestable listens to this.
    public static event Action<ActivityTrigger> TaskCompleted;

    // True once ALL tasks of this activity are complete.
    public static bool IsActivityComplete(string id) => activityDone.Contains(id);

    // Fired when a component of an activity is taken out again. ActivityTestable listens to this.
    public static event Action<ActivityTrigger> TaskUndone;

    private Collider col;
    private readonly HashSet<Collider> insideCorrect = new HashSet<Collider>();   // zone-only tasks
    private bool taskDone;
    private Coroutine disableRoutine;
    private int lastEnteredFrame = -1;
    private int lastExitedFrame = -1;
    private float lastWrongAlert = -99f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        groups.Clear();
        activityDone.Clear();
        TaskCompleted = null;
        TaskUndone = null;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // Static data survives scene loads, so wipe it whenever a new scene replaces the old one.
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;   // an additive scene must not wipe progress
        groups.Clear();
        activityDone.Clear();
    }

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Awake()
    {
        col = GetComponent<Collider>();

        if (socket == null) socket = GetComponentInParent<XRSocketInteractor>();

        if (groups.TryGetValue(activityID, out var list) && !list.Contains(this))
            list.Add(this);
    }

    private void OnEnable()
    {
        // Works without the Inspector hooks; the Inspector hooks still work too (duplicates are ignored)
        if (socket != null)
        {
            socket.selectEntered.AddListener(OnSocketSelectEntered);
            socket.selectExited.AddListener(OnSocketSelectExited);
        }
    }

    private void OnDisable()
    {
        if (socket != null)
        {
            socket.selectEntered.RemoveListener(OnSocketSelectEntered);
            socket.selectExited.RemoveListener(OnSocketSelectExited);
        }
    }

    private void OnDestroy()
    {
        if (groups.TryGetValue(activityID, out var list))
            list.Remove(this);
    }

    //is this the right object?
    private bool IsCorrect(GameObject item)
    {
        if (item == null) return false;

        if (requiredObject != null)
            return item.transform == requiredObject.transform || item.transform.IsChildOf(requiredObject.transform);

        if (!string.IsNullOrEmpty(requiredTag))
            return item.CompareTag(requiredTag);

        return true;   
    }

    private bool HasRequirement => requiredObject != null || !string.IsNullOrEmpty(requiredTag);

    // True while the player is still holding the object in their hand
    private static bool IsHeldByPlayer(GameObject item)
    {
        var grab = item.GetComponentInParent<XRGrabInteractable>();
        if (grab == null) return false;

        foreach (var interactor in grab.interactorsSelecting)
            if (!(interactor is XRSocketInteractor)) return true;
        return false;
    }

    // Trigger events for zone-only tasks
    private void OnTriggerEnter(Collider other) => TryCompleteFrom(other);
    private void OnTriggerStay(Collider other) => TryCompleteFrom(other);



    private void TryCompleteFrom(Collider other)
    {
        if (socket != null) return;     

        var rb = other.attachedRigidbody;
        var root = rb != null ? rb.gameObject : other.gameObject;

        if (!IsCorrect(root) && !IsCorrect(other.gameObject)) return;
        insideCorrect.Add(other);           

        if (taskDone) return;
        if (IsHeldByPlayer(root)) return;   
        Complete();
    }

    private void OnTriggerExit(Collider other)
    {
        if (socket != null) return;
        if (!insideCorrect.Remove(other)) return;

        insideCorrect.RemoveWhere(c => c == null);        // objects destroyed while inside
        if (insideCorrect.Count == 0 && undoOnRemove)
            UndoTask();                                   // the required object was taken out of the zone
    }

  
    private void OnSocketSelectEntered(SelectEnterEventArgs args)
        => HandleSocketEntered(args.interactableObject.transform.gameObject);

    private void OnSocketSelectExited(SelectExitEventArgs args)
        => HandleSocketExited();

    // socket "Select Entered"
    public void OnSocketEntered()
    {
        GameObject item = null;
        if (socket != null && socket.firstInteractableSelected != null)
            item = socket.firstInteractableSelected.transform.gameObject;
        HandleSocketEntered(item);
    }

    // socket "Select Exited"
    public void OnSocketExited() => HandleSocketExited();

    private void HandleSocketEntered(GameObject item)
    {
        if (Time.frameCount == lastEnteredFrame) return;   
        lastEnteredFrame = Time.frameCount;

        if (socket != null)
        {
            if (item == null) return;

            if (!IsCorrect(item))
            {
                // Wrong object in this socket, only an alert sound, the task stays incomplete
                if (Time.time - lastWrongAlert > 1f)
                {
                    lastWrongAlert = Time.time;
                    if (SoundManager.Instance != null) SoundManager.Instance.PlayWrong();
                }
                return;
            }
        }
        else if (HasRequirement)
        {
            Debug.LogWarning($"ActivityTrigger '{name}': no socket found, so it cannot check WHICH object was socketed. " +
                             "Assign the Socket field.", this);
        }

        Complete();

        // Restart the timer, the collider is only switched off after the delay
        CancelDisable();
        disableRoutine = StartCoroutine(DisableAfterDelay());
    }

    private void HandleSocketExited()
    {
        if (Time.frameCount == lastExitedFrame) return;
        lastExitedFrame = Time.frameCount;

        // Cancel any pending disable and make sure the collider is on
        CancelDisable();
        if (col != null) col.enabled = true;

        // Taking a component out undoes the task at ANY time, even after the activity was completed
        if (undoOnRemove) UndoTask();
    }

    private void UndoTask()
    {
        if (!taskDone) return;
        taskDone = false;

        // The whole activity is no longer complete
        if (activityDone.Remove(activityID))
        {
            Debug.Log($"{activityID}: a component was removed, the activity is no longer complete");
            if (SessionTracker.Instance != null)
                SessionTracker.Instance.UncompleteActivity(activityID);
        }
        else
        {
            Debug.Log($"{activityID}: a task was undone");
        }

        TaskUndone?.Invoke(this);
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

    // completion
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