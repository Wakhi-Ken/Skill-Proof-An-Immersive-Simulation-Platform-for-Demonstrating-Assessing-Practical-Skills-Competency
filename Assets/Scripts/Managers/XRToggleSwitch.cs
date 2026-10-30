
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// switch types: breaker (main power), light, appliance (stove, geyser, inverter)
public enum SwitchKind { Breaker, Light, Appliance }

[RequireComponent(typeof(XRSimpleInteractable))]
public class XRToggleSwitch : MonoBehaviour
{
    public SwitchKind kind = SwitchKind.Light;

    [SerializeField] private bool startOn = false;

    [Header("Visual")]
    public Transform lever;
    public Vector3 offEuler = new Vector3(-25f, 0f, 0f);
    public Vector3 onEuler = new Vector3(25f, 0f, 0f);
    public float moveTime = 0.12f;
    public GameObject onIndicator;
    public GameObject offIndicator;

    [Header("Individual Light")]
    public Light controlledLight;

    [Header("Event")]
    public UnityEvent<bool> onToggled;

    private XRSimpleInteractable interactable;
    private bool isOn;
    private Coroutine anim;

    public bool IsOn => isOn;

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();

        isOn = startOn;

        ApplyVisual(true);
        ApplyLight();
    }

    void OnEnable()
    {
        if (interactable == null)
            interactable = GetComponent<XRSimpleInteractable>();

        interactable.selectEntered.AddListener(OnSelected);
    }

    void OnDisable()
    {
        if (interactable != null)
            interactable.selectEntered.RemoveListener(OnSelected);
    }

    private void OnSelected(SelectEnterEventArgs args)
    {
        Toggle();
    }

    public void Toggle()
    {
        SetOn(!isOn);
    }

    /// <param name="notify">false = change silently without firing onToggled</param>
    /// <param name="sound">false = no click sound</param>
    public void SetOn(bool on, bool notify = true, bool sound = true)
    {
        if (isOn == on)
            return;

        isOn = on;

        if (sound && SoundManager.Instance != null)
        {
            if (kind == SwitchKind.Breaker)
                SoundManager.Instance.PlayBreakerSwitch(transform.position);
            else
                SoundManager.Instance.PlayLightSwitch(transform.position, isOn);
        }

        ApplyVisual(false);
        ApplyLight();

        if (notify)
            onToggled?.Invoke(isOn);
    }

    private void ApplyLight()
    {
        if (controlledLight != null)
        {
            controlledLight.enabled = isOn;
        }
    }

    private void ApplyVisual(bool instant)
    {
        if (onIndicator != null)
            onIndicator.SetActive(isOn);

        if (offIndicator != null)
            offIndicator.SetActive(!isOn);

        if (lever == null)
            return;

        var target = Quaternion.Euler(isOn ? onEuler : offEuler);

        if (instant || moveTime <= 0f || !isActiveAndEnabled)
        {
            lever.localRotation = target;
            return;
        }

        if (anim != null)
            StopCoroutine(anim);

        anim = StartCoroutine(Move(target));
    }

    private IEnumerator Move(Quaternion target)
    {
        var start = lever.localRotation;

        for (float t = 0f; t < moveTime; t += Time.deltaTime)
        {
            lever.localRotation = Quaternion.Slerp(
                start,
                target,
                t / moveTime
            );

            yield return null;
        }

        lever.localRotation = target;
        anim = null;
    }
}
