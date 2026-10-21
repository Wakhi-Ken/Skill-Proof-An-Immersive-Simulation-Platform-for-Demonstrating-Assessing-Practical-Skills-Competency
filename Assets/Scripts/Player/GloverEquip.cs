using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class GloveEquip : MonoBehaviour
{
    [Header("Bare hand models: turned OFF when gloves are equipped")]
    public GameObject[] bareHandModels;

    [Header("Gloves parented to the hands: turned ON when equipped")]
    public GameObject[] handGloves;

    [Header("Pickup")]
    [Tooltip("The glove object the player grabs. Defaults to this object.")]
    public GameObject pickupToHide;
    public bool hidePickupOnEquip = true;

    [Tooltip("Small delay so the grab finishes before the pickup is switched off.")]
    public float hideDelay = 0.1f;

    [Header("Events")]
    [Tooltip("Optional: e.g. ActivityTrigger.Complete to count this as a task.")]
    public UnityEvent onEquipped;

    public bool IsEquipped { get; private set; }

    private void Awake()
    {
        if (pickupToHide == null) pickupToHide = gameObject;
    }

    // Hook to the XR Grab Interactable's "Select Entered" event
    public void Equip()
    {
        if (IsEquipped) return;
        IsEquipped = true;

        foreach (var g in handGloves)
            if (g != null) g.SetActive(true);

        foreach (var h in bareHandModels)
            if (h != null) h.SetActive(false);

        onEquipped?.Invoke();

        if (hidePickupOnEquip)
            StartCoroutine(HidePickup());
    }

    private IEnumerator HidePickup()
    {
        // Wait so the interactor isn't holding the object while it's disabled
        yield return new WaitForSeconds(hideDelay);
        if (pickupToHide != null) pickupToHide.SetActive(false);
    }
}