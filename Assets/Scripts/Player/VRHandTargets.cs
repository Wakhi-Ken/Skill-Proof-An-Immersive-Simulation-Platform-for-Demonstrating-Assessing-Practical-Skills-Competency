using UnityEngine;

public class VRHeadLockToBody : MonoBehaviour
{
    [Header("Drag these in")]
    public Transform head;        // Main Camera (the headset)
    public Transform playerBody;  // Your player body object

    [Header("Options")]
    public bool lockHeight = false;      // also lock the head's height above the body
    public float headHeight = 1.6f;      // height above the body pivot (if lockHeight is on)
    public bool useSmoothing = false;
    public float smoothSpeed = 15f;

    void LateUpdate()
    {
        if (head == null || playerBody == null)
            return;

        // How far the head is from being directly above the body (horizontal only)
        Vector3 offset = new Vector3(
            playerBody.position.x - head.position.x,
            0f,
            playerBody.position.z - head.position.z
        );

        if (lockHeight)
        {
            float targetHeadY = playerBody.position.y + headHeight;
            offset.y = targetHeadY - head.position.y;
        }

        // Move the rig (this object) so the head ends up above the body
        Vector3 targetPos = transform.position + offset;

        transform.position = useSmoothing
            ? Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime)
            : targetPos;
    }
}