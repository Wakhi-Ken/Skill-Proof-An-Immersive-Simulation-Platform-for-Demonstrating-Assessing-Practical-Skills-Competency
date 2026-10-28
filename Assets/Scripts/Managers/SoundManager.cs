using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        ambience = CreateSource(ambienceGroup, true);
        sfx = CreateSource(sfxGroup, false);
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
    }

    void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
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

    // Sounds
    public void PlayTaskCorrect() => PlaySfx(taskCorrect);
    public void PlayActivityComplete() => PlaySfx(activityComplete);
    public void PlayWrong() => PlaySfx(wrong);

    public void PlaySfx(AudioClip clip)
    {
        if (clip == null) { Debug.LogWarning("SoundManager: that clip is not assigned"); return; }
        sfx.PlayOneShot(clip);
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