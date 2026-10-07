using UnityEngine;

public class VRHandTargets : MonoBehaviour
{

    public Transform head;

    void LateUpdate()
    {
        if (head == null)
            return;

        // Follow the VR head's X and Z position
        transform.position = new Vector3(
            head.position.x,
            transform.position.y,
            head.position.z
        );

    }
}
