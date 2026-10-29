using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactors;


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
    public Light flashLight;                 // optional
    public float flashDuration = 0.4f;
    public float sparkCooldown = 1.5f;
    public bool playWrongSound = true;
    public XRBaseInputInteractor[] hapticControllers;   // left + right hand interactors
    [SerializeField] private float armDelay = 1f;       // ignore tasks that are completed as the scene starts

    [Header("Session tracker")]
    [Tooltip("ON: wrong tests and working with the power ON are sent to the SessionTracker automatically")]
    public bool registerWithTracker = true;

    [Header("Extra events (optional)")]
    public UnityEvent onSafetyViolation;     // worked with the breaker ON
    public UnityEvent onError;               // tested before the activity was complete
    public UnityEvent onTestPassed;          // machine ran successfully (first time)
    public UnityEvent<string> onMessage;     // hint text for your hints UI

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
        if (testSwitch != null) testSwitch.onToggled.AddListener(OnTestSwitchToggled);
        if (breakerSwitch != null) breakerSwitch.onToggled.AddListener(OnBreakerToggled);
    }

    void OnDisable()
    {
        ActivityTrigger.TaskCompleted -= OnTaskCompleted;
        if (testSwitch != null) testSwitch.onToggled.RemoveListener(OnTestSwitchToggled);
        if (breakerSwitch != null) breakerSwitch.onToggled.RemoveListener(OnBreakerToggled);
    }

    // ------------------------------------------------ working with the power ON
    private void OnTaskCompleted(ActivityTrigger trigger)
    {
        if (trigger.activityID != activityID) return;
        if (Time.timeSinceLevelLoad < armDelay) return;
        if (breakerSwitch == null || !breakerSwitch.IsOn) return;     // power is off: safe

        Spark(trigger.transform.position);
        Say("Danger! Switch the breaker OFF before you work on the circuit.");

        if (!violationCounted)               // one penalty each time the power was left on
        {
            violationCounted = true;
            Register(true);
            onSafetyViolation?.Invoke();
        }
    }

    //  switches
    private void OnBreakerToggled(bool on)
    {
        if (!on)
        {
            violationCounted = false;                                 // they isolated the power
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
        if (testSwitch != null) testSwitch.SetOn(false, false, false);   // flips back so they can try again
    }

    //  effects
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