using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;


// Tracks the current simulation session, including the timer, errors, and scoring.
public class SessionTracker : MonoBehaviour
{
    public static SessionTracker Instance { get; private set; }

    [Header("Simulation (must match a document ID in Firestore)")]
    public string simulationId = "electrical_wiring";

    [Header("Timer")]
    public float timeLimitMinutes = 0f;

    [Header("Scoring")]
    public float safetyPenalty = 10f;      // lost from safety score per safety violation
    public float accuracyPenalty = 5f;     // lost from accuracy per error
    public float passMark = 70f;           // minimum score to count as competent
    public int maxErrorsToPass = 5;        // more errors than this = not competent
    public bool countOutOfOrderAsError = true;

    public bool autoFinishWhenAllDone = true;

    [Header("Testing")]
    public int useFirstActivities = 0;

    // Events the UI listens to
    public event Action OnSessionStarted;
    public event Action OnProgressChanged;
    public event Action OnTimeUp;
    public event Action<PerformanceData> OnSessionFinished;   // fires straight away
    public event Action<bool> OnPerformanceSaved;             // fires when the save finishes

    // Live state
    public bool IsRunning { get; private set; }
    public SimulationData Simulation { get; private set; }
    public List<ActivityResult> Results { get; private set; } = new List<ActivityResult>();
    public int TotalErrors { get; private set; }
    public int SafetyViolations { get; private set; }
    public PerformanceData LastPerformance { get; private set; }

    public float SafetyScore => Mathf.Max(0f, 100f - SafetyViolations * safetyPenalty);
    public float ElapsedTime => IsRunning ? Time.time - startTime : lastElapsed;

    public bool HasTimeLimit => timeLimitMinutes > 0f;
    public float TimeRemaining => HasTimeLimit
        ? Mathf.Max(0f, timeLimitMinutes * 60f - ElapsedTime)
        : 0f;

    public int CompletedCount
    {
        get
        {
            int count = 0;
            foreach (ActivityResult r in Results) if (r.completed) count++;
            return count;
        }
    }

    private float startTime;
    private float lastElapsed;
    private DateTime startedAtUtc;

    // Order is judged only the FIRST time an activity is completed, so removing and
    // re-adding a component doesn't change the procedural score or add extra errors.
    private readonly HashSet<string> orderChecked = new HashSet<string>();
    private readonly HashSet<string> inOrderIds = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        // Ends the session when the time limit is reached
        if (IsRunning && HasTimeLimit && ElapsedTime >= timeLimitMinutes * 60f)
        {
            Debug.Log("Time is up.");
            OnTimeUp?.Invoke();
            FinishSession();
        }
    }

    // starts the session and loads the simulation data from Firestore. Returns false if the session could not be started.
    public async void StartSession()
    {
        await StartSessionAsync();
    }

    public async Task<bool> StartSessionAsync()
    {
        if (IsRunning) return false;

        if (DatabaseManager.Instance == null)
        {
            Debug.LogError("SessionTracker: DatabaseManager is missing.");
            return false;
        }

        Simulation = await DatabaseManager.Instance.GetSimulationAsync(simulationId);
        if (Simulation == null || Simulation.activities == null)
        {
            Debug.LogError("SessionTracker: simulation '" + simulationId + "' not found in Firestore.");
            return false;
        }

        Results.Clear();
        foreach (ActivityInfo info in Simulation.activities)
        {
            if (useFirstActivities > 0 && Results.Count >= useFirstActivities) break;

            Results.Add(new ActivityResult
            {
                activityID = info.activityID,
                name = info.name,
                completed = false,
                errors = 0
            });
        }

        TotalErrors = 0;
        SafetyViolations = 0;
        orderChecked.Clear();
        inOrderIds.Clear();
        lastElapsed = 0f;
        startTime = Time.time;
        startedAtUtc = DateTime.UtcNow;
        IsRunning = true;

        Debug.Log("Session started: " + Simulation.name + " (" + Results.Count + " activities)");
        OnSessionStarted?.Invoke();
        OnProgressChanged?.Invoke();
        return true;
    }

    // Called by ActivityTrigger
    public void CompleteActivity(string activityID)
    {
        if (!IsRunning)
        {
            Debug.LogWarning("SessionTracker: '" + activityID + "' ignored because no session is running. Press Start Simulation first.");
            return;
        }

        int index = Results.FindIndex(r => r.activityID == activityID);
        if (index < 0)
        {
            Debug.LogWarning("SessionTracker: no activity with the ID '" + activityID + "' in this session.");
            return;
        }

        if (Results[index].completed) return;

        bool previousDone = true;
        for (int i = 0; i < index; i++)
            if (!Results[i].completed) previousDone = false;

        Results[index].completed = true;

        // Only judge the order the first time this activity is completed
        if (orderChecked.Add(activityID))
        {
            if (previousDone)
            {
                inOrderIds.Add(activityID);
            }
            else if (countOutOfOrderAsError)
            {
                RegisterError(activityID, false);   // also fires the progress event
            }
        }

        Debug.Log("Activity completed: " + activityID + " (" + CompletedCount + "/" + Results.Count + ")");
        OnProgressChanged?.Invoke();

        if (autoFinishWhenAllDone && CompletedCount == Results.Count)
            FinishSession();
    }

    // Called by ActivityTrigger when a component of a completed activity is taken out again
    public void UncompleteActivity(string activityID)
    {
        if (!IsRunning) return;

        int index = Results.FindIndex(r => r.activityID == activityID);
        if (index < 0 || !Results[index].completed) return;

        Results[index].completed = false;

        Debug.Log("Activity no longer complete: " + activityID + " (" + CompletedCount + "/" + Results.Count + ")");
        OnProgressChanged?.Invoke();
    }

    // Called by ErrorTrigger (activityID can be empty if the error isn't tied to one step)
    public void RegisterError(string activityID, bool isSafetyViolation)
    {
        if (!IsRunning) return;

        TotalErrors++;
        if (isSafetyViolation) SafetyViolations++;

        if (!string.IsNullOrEmpty(activityID))
        {
            ActivityResult r = Results.Find(x => x.activityID == activityID);
            if (r != null) r.errors++;
        }

        Debug.Log("Error registered. Total: " + TotalErrors + ", safety violations: " + SafetyViolations);
        OnProgressChanged?.Invoke();
    }

    // Finish / Complete button
    public async void FinishSession()
    {
        if (!IsRunning)
        {
            Debug.LogWarning("SessionTracker: FinishSession ignored because no session is running. Press Start Simulation first.");
            return;
        }

        // Wrong objects still in a wrong place now count as errors
        ErrorTrigger.CommitPendingErrors();

        IsRunning = false;
        lastElapsed = Time.time - startTime;

        PerformanceData p = BuildPerformance();
        LastPerformance = p;

        // Show the results straight away, without waiting for the network
        OnSessionFinished?.Invoke(p);

        bool saved = false;
        if (DatabaseManager.Instance != null)
            saved = await DatabaseManager.Instance.SavePerformanceAsync(p);

        Debug.Log(saved ? "Performance saved." : "Performance could not be saved.");
        OnPerformanceSaved?.Invoke(saved);
    }

    private PerformanceData BuildPerformance()
    {
        int total = Results.Count;
        int completed = CompletedCount;
        bool allDone = completed == total;

        float safety = SafetyScore;
        float procedural = total == 0 ? 0f : (float)inOrderIds.Count / total * 100f;
        float accuracy = Mathf.Max(0f, 100f - TotalErrors * accuracyPenalty);
        float compliance = (safety + procedural) / 2f;
        float completion = total == 0 ? 0f : (float)completed / total * 100f;
        float competency = (completion + safety + procedural + accuracy) / 4f;

        bool competent = allDone
                         && safety >= passMark
                         && competency >= passMark
                         && TotalErrors <= maxErrorsToPass;

        string level = competency >= 90f ? "Advanced"
                     : competent ? "Competent"
                     : "Not Yet Competent";

        string feedback = competent
            ? "Well done. You completed every task safely and demonstrated competence."
            : allDone
                ? "Not yet competent. Reduce your errors and safety violations, then try again."
                : "Not yet competent. Some tasks were not completed. Review the steps you missed, then try again.";

        return new PerformanceData
        {
            simulationID = simulationId,
            simulationName = Simulation.name,
            startedAt = Timestamp.FromDateTime(startedAtUtc),
            completedAt = Timestamp.FromDateTime(DateTime.UtcNow),
            completionTime = lastElapsed,
            errors = TotalErrors,
            allTasksCompleted = allDone,
            safetyScore = safety,
            competencyScore = competency,
            proceduralScore = procedural,
            accuracy = accuracy,
            compliance = compliance,
            competent = competent,
            competencyLevel = level,
            feedbackText = feedback,
            activities = new List<ActivityResult>(Results)
        };
    }

    // Testing helpers
    [ContextMenu("Debug: Complete Next Activity")]
    private void DebugCompleteNext()
    {
        foreach (ActivityResult r in Results)
        {
            if (!r.completed)
            {
                CompleteActivity(r.activityID);
                return;
            }
        }
    }

    [ContextMenu("Debug: Add Safety Violation")]
    private void DebugAddViolation() { RegisterError("", true); }

    [ContextMenu("Debug: Finish Now")]
    private void DebugFinishNow() { FinishSession(); }
}