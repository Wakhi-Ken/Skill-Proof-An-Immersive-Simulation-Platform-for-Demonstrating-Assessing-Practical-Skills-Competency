using UnityEngine;

public class VRHandTargets : MonoBehaviour
{

    public Transform controller;
    public Transform HandTarget;

    // Update is called once per frame
    void LateUpdate()
    {
        if (controller != null || HandTarget == null)
            return;
        HandTarget.position = controller.position;
        HandTarget.rotation = controller.rotation;
    }
}
