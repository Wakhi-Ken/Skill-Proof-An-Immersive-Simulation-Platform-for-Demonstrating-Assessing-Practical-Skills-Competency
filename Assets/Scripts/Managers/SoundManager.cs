using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public enum MachineType { LightBulb, SolarInverter, Stove, Generic, Geyser }

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }
    

    public enum Channel { SFX, Ambience }

    [Header("Mixer (expose: SFXVol, AmbienceVol)")]
    [SerializeField] AudioMixer mixer;
    [SerializeField] AudioMixerGroup sfxGroup, ambienceGroup;

    [Header("Sounds used in every activity")]
    [SerializeField] AudioClip taskCorrect;
    [SerializeField] AudioClip activityComplete;
    [SerializeField] AudioClip wrong;

    [Header("Grab sounds (applied automatically to EVERY XR Grab Interactable)")]
    [SerializeField] AudioClip pickup;
    [SerializeField] AudioClip drop;
    [SerializeField] AudioClip snap;                 // object clicks into an XR Socket Interactor
    [SerializeField, Range(0f, 1f)] float pickupVolume = 1f;
    [SerializeField, Range(0f, 1f)] float dropVolume = 1f;
    [SerializeField, Range(0f, 1f)] float pickupHaptic = 0.25f;   // 0 = no vibration
    [SerializeField] float hapticDuration = 0.08f;

    [Header("Electrical sounds")]
    [SerializeField] AudioClip spark;                // wrong wiring / connecting with power ON
    [SerializeField] AudioClip breakerSwitch;
    [SerializeField] AudioClip lightSwitchOn, lightSwitchOff;

    // breaker lever clunk

    [Serializable]
    public class MachineSounds
    {
        public MachineType type;
        public AudioClip start;   // bulb: switch click | inverter: relay/beep | stove: ignition clicks
        public AudioClip loop;    // bulb: soft hum | inverter: hum/fan | stove: flame loop
    }

    [Header("Machine sounds (played when the player tests the machine)")]
    [SerializeField] MachineSounds[] machineSounds;

    [Header("3D sound pool")]
    [SerializeField] int spatialPoolSize = 10;
    [SerializeField] float spatialMaxDistance = 15f;

    [Header("Slider names (the name of each Slider object in your UI)")]
    [SerializeField] string sfxSliderName = "SFXSlider";
    [SerializeField] string ambienceSliderName = "AmbienceSlider";

    [Serializable]
    public class SceneAudio
    {
        public string sceneName;   // must match the scene name exactly
        public AudioClip ambience;
    }

    [Header("Ambience per scene (leave the clip empty for silence)")]
    [SerializeField] SceneAudio[] sceneAudio;

    AudioSource ambience, sfx;
    readonly List<Slider> boundSliders = new List<Slider>();
    readonly List<AudioSource> spatialPool = new List<AudioSource>();
    int spatialNext;

    XRInteractionManager hookedManager;
    readonly HashSet<XRGrabInteractable> hookedGrabs = new HashSet<XRGrabInteractable>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        ambience = CreateSource(ambienceGroup, true);
        sfx = CreateSource(sfxGroup, false);
        CreateSpatialPool();
    }

    void Start()
    {
        if (Instance != this) return;

        // Mixer values must be set in Start, not Awake
        foreach (Channel c in Enum.GetValues(typeof(Channel)))
            ApplyVolume(c, GetVolume(c));

        SceneManager.sceneLoaded += OnSceneLoaded;
        PlaySceneAudio(SceneManager.GetActiveScene().name);
        BindSliders();
        HookGrabInteractables();
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (hookedManager != null) hookedManager.interactableRegistered -= OnInteractableRegistered;
    }

    AudioSource CreateSource(AudioMixerGroup group, bool loop)
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.outputAudioMixerGroup = group;
        s.loop = loop;
        s.playOnAwake = false;
        s.spatialBlend = 0f; // 2D sound
        return s;
    }

    //  Scene changes
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlaySceneAudio(scene.name);
        BindSliders();
        HookGrabInteractables();
    }

    void PlaySceneAudio(string sceneName)
    {
        foreach (var sa in sceneAudio)
        {
            if (sa.sceneName != sceneName) continue;
            SwapLoop(ambience, sa.ambience);
            return;
        }
    }

    void SwapLoop(AudioSource source, AudioClip clip)
    {
        if (clip == null) { source.Stop(); source.clip = null; return; }
        if (source.clip == clip && source.isPlaying) return; // don't restart
        source.clip = clip;
        source.Play();
    }

    // Sliders
    // Call it yourself if you create the settings panel later while the scene is running.
    public void BindSliders()
    {
        boundSliders.RemoveAll(s => s == null);

        // true = also find sliders on hidden panels
        foreach (var slider in FindObjectsOfType<Slider>(true))
        {
            if (boundSliders.Contains(slider)) continue;

            Channel channel;
            if (slider.name == sfxSliderName) channel = Channel.SFX;
            else if (slider.name == ambienceSliderName) channel = Channel.Ambience;
            else continue;

            slider.minValue = 0.0001f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(GetVolume(channel));
            slider.onValueChanged.AddListener(v => SetVolume(channel, v));
            boundSliders.Add(slider);
        }
    }

    // Sounds (2D)
    public void PlayTaskCorrect() => PlaySfx(taskCorrect);
    public void PlayActivityComplete() => PlaySfx(activityComplete);
    public void PlayWrong() => PlaySfx(wrong);

    public void PlaySfx(AudioClip clip)
    {
        if (clip == null) { Debug.LogWarning("SoundManager: that clip is not assigned"); return; }
        sfx.PlayOneShot(clip);
    }

    // Sounds (3D, at a position in the world). All go through the SFX mixer group,
    // so the SFX slider controls them too.
    void CreateSpatialPool()
    {
        for (int i = 0; i < spatialPoolSize; i++)
        {
            var go = new GameObject("SFX_3D_" + i);
            go.transform.SetParent(transform);
            var s = go.AddComponent<AudioSource>();
            ConfigureSpatial(s, false, spatialMaxDistance);
            spatialPool.Add(s);
        }
    }

    void ConfigureSpatial(AudioSource s, bool loop, float maxDistance)
    {
        s.outputAudioMixerGroup = sfxGroup;
        s.loop = loop;
        s.playOnAwake = false;
        s.spatialBlend = 1f;
        s.rolloffMode = AudioRolloffMode.Linear;
        s.minDistance = 0.5f;
        s.maxDistance = maxDistance;
    }

    public void PlaySfxAt(AudioClip clip, Vector3 position, float volume = 1f,
                          float pitchMin = 0.95f, float pitchMax = 1.05f)
    {
        if (clip == null) return;

        AudioSource src = null;
        foreach (var s in spatialPool)
            if (!s.isPlaying) { src = s; break; }
        if (src == null)
        {
            src = spatialPool[spatialNext];
            spatialNext = (spatialNext + 1) % spatialPool.Count;
        }

        src.Stop();
        src.transform.position = position;
        src.clip = clip;
        src.volume = Mathf.Clamp01(volume);
        src.pitch = UnityEngine.Random.Range(pitchMin, pitchMax);
        src.Play();
    }

    /// <summary>For looping machine sounds: adds a 3D AudioSource to 'host' that is routed to the SFX group.</summary>
    public AudioSource CreateSfxSource(GameObject host, bool loop, float maxDistance = 10f)
    {
        var s = host.AddComponent<AudioSource>();
        ConfigureSpatial(s, loop, maxDistance);
        return s;
    }

    public void PlaySpark(Vector3 position) => PlaySfxAt(spark, position, 1f, 0.9f, 1.1f);
    public void PlayBreakerSwitch(Vector3 position) => PlaySfxAt(breakerSwitch, position);
    public void PlayLightSwitch(Vector3 position, bool turnedOn)
    {
        var clip = turnedOn ? lightSwitchOn : lightSwitchOff;
        if (clip == null) clip = turnedOn ? lightSwitchOff : lightSwitchOn;   // use whichever is assigned
        PlaySfxAt(clip, position);
    }

    public MachineSounds GetMachineSounds(MachineType type)
    {
        if (machineSounds != null)
            foreach (var m in machineSounds)
                if (m.type == type) return m;
        return null;
    }

      void HookGrabInteractables()
    {
        hookedGrabs.RemoveWhere(g => g == null);

        if (hookedManager != null) hookedManager.interactableRegistered -= OnInteractableRegistered;
        hookedManager = FindFirstObjectByType<XRInteractionManager>();
        if (hookedManager == null) return;

        hookedManager.interactableRegistered += OnInteractableRegistered;

        var existing = new List<IXRInteractable>();
        hookedManager.GetRegisteredInteractables(existing);
        foreach (var i in existing) HookGrab(i as XRGrabInteractable);
    }

    void OnInteractableRegistered(InteractableRegisteredEventArgs args)
    {
        HookGrab(args.interactableObject as XRGrabInteractable);
    }

    void HookGrab(XRGrabInteractable grab)
    {
        if (grab == null || !hookedGrabs.Add(grab)) return;
        grab.selectEntered.AddListener(OnGrabEntered);
        grab.selectExited.AddListener(OnGrabExited);
    }

    void OnGrabEntered(SelectEnterEventArgs args)
    {
        Vector3 pos = args.interactableObject.transform.position;

        // Put into a socket: click instead of pickup
        if (args.interactorObject is XRSocketInteractor)
        {
            PlaySfxAt(snap, pos, pickupVolume);
            return;
        }

        PlaySfxAt(pickup, pos, pickupVolume);

        if (pickupHaptic > 0f && args.interactorObject is XRBaseInputInteractor controller)
            controller.SendHapticImpulse(pickupHaptic, hapticDuration);
    }

    void OnGrabExited(SelectExitEventArgs args)
    {
        if (args.isCanceled) return;                                // object disabled / destroyed / scene change
        if (args.interactorObject is XRSocketInteractor) return;    // taken out of a socket: the hand's pickup plays next

        PlaySfxAt(drop, args.interactableObject.transform.position, dropVolume);
    }

    // Volume (0 to 1),
    static string Param(Channel c) => c == Channel.SFX ? "SFXVol" : "AmbienceVol";

    public float GetVolume(Channel c) => PlayerPrefs.GetFloat(Param(c), 0.8f);

    public void SetVolume(Channel c, float value)
    {
        value = Mathf.Clamp(value, 0.0001f, 1f);
        ApplyVolume(c, value);
        PlayerPrefs.SetFloat(Param(c), value);
    }

    void ApplyVolume(Channel c, float value)
    {
        mixer.SetFloat(Param(c), Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f);
    }
}