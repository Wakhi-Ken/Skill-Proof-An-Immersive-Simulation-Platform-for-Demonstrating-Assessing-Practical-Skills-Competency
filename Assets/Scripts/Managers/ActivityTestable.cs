using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactors;


// activityTestable: a circuit that can be tested, with a breaker switch and a test switch. If the breaker is left ON while the test switch is toggled, sparks and a warning are triggered. If the test switch is toggled while the circuit is not ready (steps missing or wrong object in place), sparks and an error are triggered. If the test switch is toggled while the circuit is ready, the machine runs and a success event is triggered.
public class ActivityTestable : MonoBehaviour
{
    [Header("Which activity (same ID as the ActivityTriggers)")]
    public string activityID = "activity_2";

    [Header("Switches (XRToggleSwitch)")]
    public XRToggleSwitch breakerSwitch;     // this circuit's breaker, or the power supply switch (starts ON)
    public XRToggleSwitch testSwitch;        // bulb / stove / geyser / inverter switch (starts OFF)

    [Header("Machine")]
    public MachineSFX machine;

    [Header("Spark effects")]
    public ParticleSystem sparkParticles;    // moved to where the spark happens
    public Light flashLight;                
    public float flashDuration = 0.4f;
    public float sparkCooldown = 1.5f;
    public bool playWrongSound = true;
    public XRBaseInputInteractor[] hapticControllers;   // left + right hand interactors
    [SerializeField] private float armDelay = 1f;       

    [Header("Session tracker")]

    public bool registerWithTracker = true;
    // if true, the SessionTracker will be notified of safety violations and errors
    [Header("Extra events")]
    public UnityEvent onSafetyViolation;     
    public UnityEvent onError;               
    public UnityEvent onTestPassed;          
    public UnityEvent<string> onMessage;     
    private float lastSpark = -99f;
    private float lastError = -99f;
    private bool violationCounted;
    private bool passed;

    void Awake()
    {
        if (flashLight != null) flashLight.enabled = false;
    }

    void OnEnable()
    {
        ActivityTrigger.TaskCompleted += OnTaskCompleted;
        ActivityTrigger.TaskUndone += OnTaskUndone;
        if (testSwitch != null) testSwitch.onToggled.AddListener(OnTestSwitchToggled);
        if (breakerSwitch != null) breakerSwitch.onToggled.AddListener(OnBreakerToggled);
    }

    void OnDisable()
    {
        ActivityTrigger.TaskCompleted -= OnTaskCompleted;
        ActivityTrigger.TaskUndone -= OnTaskUndone;
        if (testSwitch != null) testSwitch.onToggled.RemoveListener(OnTestSwitchToggled);
        if (breakerSwitch != null) breakerSwitch.onToggled.RemoveListener(OnBreakerToggled);
    }

    // working with the power ON
    private void OnTaskCompleted(ActivityTrigger trigger)
    {
        // power is off: safe
        if (trigger.activityID != activityID) return;
        if (Time.timeSinceLevelLoad < armDelay) return;
        if (breakerSwitch == null || !breakerSwitch.IsOn) return;     

        Spark(trigger.transform.position);
        Say("Danger! Switch the breaker OFF before you work on the circuit.");

        // one penalty each time the power was left on
        if (!violationCounted)               
        {
            violationCounted = true;
            Register(true);
            onSafetyViolation?.Invoke();
        }
    }

    // A component was taken out: the circuit is open, so the machine loses power
    private void OnTaskUndone(ActivityTrigger trigger)
    {
        if (trigger.activityID != activityID) return;

        if (machine != null && machine.IsRunning)
        {
            machine.StopTest();
            if (testSwitch != null) testSwitch.SetOn(false, false, false);
            Say("A component was removed. The circuit is open.");
        }
    }

    //switches
    private void OnBreakerToggled(bool on)
    {
        //isolated the power
        if (!on)
        {
            violationCounted = false;                                 
            if (machine != null) machine.StopTest();                  // power cut
        }
        else if (testSwitch != null && testSwitch.IsOn)
        {
            AttemptTest();                                            // power back ON with test switch left ON
        }
    }

    private void OnTestSwitchToggled(bool on)
    {
        if (on) AttemptTest();
        else if (machine != null) machine.StopTest();
    }

    private void AttemptTest()
    {
        if (breakerSwitch != null && !breakerSwitch.IsOn)
        {
            Say("No power. Switch the breaker back ON to test.");
            StartCoroutine(ResetTestSwitch());
            return;
        }

        // Tested the wrong way: steps missing, or a wrong object is still in a wrong place
        if (!ActivityTrigger.IsActivityComplete(activityID) || ErrorTrigger.HasPending(activityID))
        {
            Spark(machine != null ? machine.transform.position : transform.position);
            Say("The circuit is not ready. Finish all the steps and check your wiring.");
            Register(false);
            onError?.Invoke();
            StartCoroutine(ResetTestSwitch());
            return;
        }

        if (machine != null)
        {
            machine.SetWiredCorrectly(true);
            machine.RunTest();
        }

        if (!passed)
        {
            passed = true;
            Say("Test passed!");
            onTestPassed?.Invoke();
        }
    }

    private IEnumerator ResetTestSwitch()
    {
        yield return new WaitForSeconds(0.5f);
        if (testSwitch != null) testSwitch.SetOn(false, false, false);  
    }

    // effects
    private void Spark(Vector3 position)
    {
        if (Time.time - lastSpark < sparkCooldown) return;
        lastSpark = Time.time;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySpark(position);
            if (playWrongSound) SoundManager.Instance.PlayWrong();
        }

        if (sparkParticles != null)
        {
            sparkParticles.transform.position = position;
            sparkParticles.Play();
        }

        MachineSFX.Buzz(hapticControllers, 1f, 0.35f);
        if (flashLight != null) StartCoroutine(Flash(position));
    }

    private IEnumerator Flash(Vector3 position)
    {
        flashLight.transform.position = position;
        for (float t = 0f; t < flashDuration; t += Time.deltaTime)
        {
            flashLight.enabled = Random.value > 0.4f;
            flashLight.intensity = Random.Range(1.5f, 5f);
            yield return null;
        }
        flashLight.enabled = false;
    }

    // Sends the mistake to the SessionTracker (at most once per cooldown, so flipping the switch fast can't spam errors)
    private void Register(bool safetyViolation)
    {
        if (!registerWithTracker || SessionTracker.Instance == null) return;
        if (!safetyViolation)
        {
            if (Time.time - lastError < sparkCooldown) return;
            lastError = Time.time;
        }
        SessionTracker.Instance.RegisterError(activityID, safetyViolation);
    }

    private void Say(string message) => onMessage?.Invoke(message);
}