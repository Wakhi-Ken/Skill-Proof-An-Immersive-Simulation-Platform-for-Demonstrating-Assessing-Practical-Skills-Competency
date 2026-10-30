using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit.Interactors;


public class MachineSFX : MonoBehaviour
{
    [Header("Setup")]
    public MachineType machineType = MachineType.LightBulb;
    public bool IsWiredCorrectly;

    [Header("Loop")]
    [Range(0f, 1f)] public float loopVolume = 0.6f;
    public float loopFadeIn = 0.5f;

    [Header("Success visuals")]
    public Renderer[] glowRenderers;                         // bulb glass, inverter LED, stove burner, geyser element
    [ColorUsage(false, true)] public Color glowColor = Color.white;
    public float glowIntensity = 2f;
    public Light[] glowLights;                               // keep to 1-2 for Quest performance
    public float lightIntensity = 1.5f;
    public GameObject[] enableOnSuccess;                     // e.g. flame particles, geyser "heating" indicator

    [Tooltip("Stove and Geyser: seconds for the glow to ramp up to full")]
    [FormerlySerializedAs("stoveRampTime")]                  // keeps the value you already set in the Inspector
    public float rampTime = 2f;

    [Header("Fault visuals (fallback: CircuitTestController normally does the sparks)")]
    public ParticleSystem sparkParticles;
    public Light flashLight;
    public float flashDuration = 0.5f;
    public float faultCooldown = 1.5f;
    public XRBaseInputInteractor[] hapticControllers;        // drag left + right hand interactors

    [Header("Events (hook your session tracker here)")]
    public UnityEvent onTestSuccess;
    public UnityEvent onTestFault;       // e.g. +1 error, -safety score
    public UnityEvent onMachineStopped;

    private AudioSource loopSource;
    private Coroutine runRoutine;
    private Material[] glowMats;                             // cached material instances, one per glow renderer
    private float lastFaultTime = -99f;
    public bool IsRunning { get; private set; }

    [ContextMenu("Test: run")]
    void EditorTestRun() { IsWiredCorrectly = true; RunTest(); }

    [ContextMenu("Test: stop")]
    void EditorTestStop() => StopTest();

    void Awake()
    {
        // Create the material instances once, instead of on every SetGlow call
        glowMats = new Material[glowRenderers.Length];
        for (int i = 0; i < glowRenderers.Length; i++)
        {
            if (glowRenderers[i] == null) continue;
            glowMats[i] = glowRenderers[i].material;
            glowMats[i].EnableKeyword("_EMISSION");
        }

        SetGlow(0f);
        SetActive(enableOnSuccess, false);
        if (flashLight != null) flashLight.enabled = false;
    }

    void OnDisable()
    {
        // Coroutines stop automatically when disabled, so reset the state to allow testing again later
        runRoutine = null;
        IsRunning = false;
        if (loopSource != null) loopSource.Stop();
    }

    void OnDestroy()
    {
        if (glowMats == null) return;
        foreach (var m in glowMats) if (m != null) Destroy(m);
    }

    private void EnsureLoopSource()
    {
        if (loopSource != null || SoundManager.Instance == null) return;
        loopSource = SoundManager.Instance.CreateSfxSource(gameObject, true);
    }

    //  public API
    public void SetWiredCorrectly(bool value) => IsWiredCorrectly = value;

    public void RunTest()
    {
        if (IsRunning) return;

        if (IsWiredCorrectly) runRoutine = StartCoroutine(RunRoutine());
        else TriggerFault();
    }

    public void StopTest()
    {
        if (runRoutine != null) StopCoroutine(runRoutine);
        runRoutine = null;
        if (loopSource != null) loopSource.Stop();
        SetGlow(0f);
        SetActive(enableOnSuccess, false);
        if (IsRunning) onMachineStopped?.Invoke();
        IsRunning = false;
    }

    public void TriggerFault()
    {
        if (Time.time - lastFaultTime < faultCooldown) return;
        lastFaultTime = Time.time;

        StopTest();
        StartCoroutine(FaultRoutine());
        onTestFault?.Invoke();
    }

    //success
    private IEnumerator RunRoutine()
    {
        IsRunning = true;
        EnsureLoopSource();

        var sounds = SoundManager.Instance != null ? SoundManager.Instance.GetMachineSounds(machineType) : null;

        if (sounds != null && SoundManager.Instance != null)
            SoundManager.Instance.PlaySfxAt(sounds.start, transform.position, 0.9f, 1f, 1f);

        switch (machineType)
        {
            case MachineType.LightBulb:
                for (int i = 0; i < 5; i++)          // quick filament flicker, then steady
                {
                    SetGlow(Random.Range(0.2f, 1f));
                    yield return new WaitForSeconds(0.05f);
                }
                break;

            case MachineType.Stove:
            case MachineType.Geyser:                  // heating element / burner warms up gradually
                SetActive(enableOnSuccess, true);
                for (float t = 0; t < rampTime; t += Time.deltaTime)
                {
                    SetGlow(t / rampTime);
                    yield return null;
                }
                break;

            default:                                  // inverter / generic: short pause like a relay closing
                yield return new WaitForSeconds(0.3f);
                break;
        }

        SetGlow(1f);
        SetActive(enableOnSuccess, true);
        onTestSuccess?.Invoke();

        if (sounds != null && sounds.loop != null && loopSource != null)
        {
            loopSource.clip = sounds.loop;
            loopSource.volume = 0f;
            loopSource.Play();
            for (float t = 0; t < loopFadeIn; t += Time.deltaTime)
            {
                loopSource.volume = Mathf.Lerp(0f, loopVolume, t / loopFadeIn);
                yield return null;
            }
            loopSource.volume = loopVolume;
        }
    }

    // fault
    private IEnumerator FaultRoutine()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySpark(transform.position);
            SoundManager.Instance.PlayWrong();
        }

        if (sparkParticles != null) sparkParticles.Play();
        Buzz(hapticControllers, 1f, 0.35f);

        if (flashLight != null)
        {
            for (float t = 0; t < flashDuration; t += Time.deltaTime)
            {
                flashLight.enabled = Random.value > 0.4f;
                flashLight.intensity = Random.Range(1.5f, 5f);
                yield return null;
            }
            flashLight.enabled = false;
        }
    }

    //  helpers
    private void SetGlow(float t)
    {
        for (int i = 0; i < glowMats.Length; i++)
            if (glowMats[i] != null)
                glowMats[i].SetColor("_EmissionColor", glowColor * (glowIntensity * t));

        foreach (var l in glowLights)
        {
            if (l == null) continue;
            l.enabled = t > 0.01f;
            l.intensity = lightIntensity * t;
        }
    }

    private static void SetActive(GameObject[] objs, bool state)
    {
        foreach (var o in objs) if (o != null) o.SetActive(state);
    }

    public static void Buzz(XRBaseInputInteractor[] controllers, float amplitude, float duration)
    {
        if (controllers == null) return;
        foreach (var c in controllers)
            if (c != null) c.SendHapticImpulse(amplitude, duration);
    }
}