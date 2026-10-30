
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class GloveEquip : MonoBehaviour
{
    [Header("Bare Hand Models: Disabled When Gloves Are Equipped")]
    public GameObject[] bareHandModels;

    [Header("Gloves: Attached to Player's Hands")]
    public GameObject[] handGloves;

    [Header("Pickup")]

    public GameObject pickupToHide;

    public bool hidePickupOnEquip = true;

    public float hideDelay = 0.1f;

    [Header("Activity Tracking")]
    public ActivityTrigger gloveActivity;

    [Header("Events")]
    public UnityEvent onEquipped;

    public bool IsEquipped { get; private set; }

    private void Awake()
    {
        if (pickupToHide == null)
            pickupToHide = gameObject;

        if (gloveActivity == null)
            gloveActivity = GetComponent<ActivityTrigger>();
    }

    // Connect to XR Grab Interactable - Select Entered
    public void Equip()
    {
        if (IsEquipped)
            return;

        IsEquipped = true;

        // Activate gloves on the player's hands
        foreach (GameObject glove in handGloves)
        {
            if (glove != null)
                glove.SetActive(true);
        }

        // Hide bare hand models
        foreach (GameObject hand in bareHandModels)
        {
            if (hand != null)
                hand.SetActive(false);
        }

        // Complete the glove sub-activity
        if (gloveActivity != null)
        {
            gloveActivity.Complete();

            Debug.Log("Glove sub-activity completed!");
        }
        else
        {
            Debug.LogWarning(
                "GloveEquip: ActivityTrigger is not assigned!"
            );
        }

        // Invoke optional events
        onEquipped?.Invoke();

        // Hide the pickup after grabbing
        if (hidePickupOnEquip)
            StartCoroutine(HidePickup());
    }

    private IEnumerator HidePickup()
    {
        yield return new WaitForSeconds(hideDelay);

        if (pickupToHide != null)
            pickupToHide.SetActive(false);
    }
}
