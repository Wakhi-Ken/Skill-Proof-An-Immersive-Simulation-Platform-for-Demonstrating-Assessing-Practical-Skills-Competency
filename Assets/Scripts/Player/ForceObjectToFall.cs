
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ForceObjectToFall : MonoBehaviour
{
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Make sure gravity is enabled
        rb.useGravity = true;

        // Allow the object to move
        rb.isKinematic = false;
    }

    private void FixedUpdate()
    {
        // Continuously apply downward force
        if (!IsGrounded())
        {
            rb.AddForce(Vector3.down * 9.81f, ForceMode.Acceleration);
        }
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(
            transform.position,
            Vector3.down,
            0.2f
        );
    }
}
