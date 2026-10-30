using UnityEngine;
using UnityEngine.Video;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Put on the screen (Quad/Plane) that shows the tutorial video.
/// Point at the screen and press select (trigger/grip) to play or pause.
/// Needs a VideoPlayer (added automatically) and an XRSimpleInteractable (added automatically).
/// </summary>
[RequireComponent(typeof(VideoPlayer), typeof(XRSimpleInteractable))]
public class TutorialVideoPlayer : MonoBehaviour
{
    [Tooltip("Start playing as soon as the menu opens")]
    public bool autoPlay = false;

    [Tooltip("Optional: a play icon / 'Press to play' object that is shown while the video is not playing")]
    public GameObject playIcon;

    private VideoPlayer video;
    private XRSimpleInteractable interactable;

    void Awake()
    {
        video = GetComponent<VideoPlayer>();
        interactable = GetComponent<XRSimpleInteractable>();
        video.playOnAwake = false;
    }

    void OnEnable()
    {
        interactable.selectEntered.AddListener(OnSelected);
        video.loopPointReached += OnVideoEnded;
    }

    void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnSelected);
        video.loopPointReached -= OnVideoEnded;
    }

    void Start()
    {
        video.Prepare();                    // load it now so the first press plays without a delay
        if (autoPlay) video.Play();
    }

    void Update()
    {
        if (playIcon != null) playIcon.SetActive(!video.isPlaying);
    }

    private void OnSelected(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
    {
        if (video.isPlaying) video.Pause();
        else video.Play();
    }

    // When the video finishes, go back to the start so the next press plays it again
    private void OnVideoEnded(VideoPlayer vp)
    {
        if (!vp.isLooping) vp.Stop();
    }

    // Public so a button (for example "Start Simulation") can stop the video before the scene changes
    public void PauseVideo() => video.Pause();
    public void RestartVideo() { video.Stop(); video.Play(); }
}
