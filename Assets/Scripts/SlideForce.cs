using UnityEngine;

public class SlideForce : MonoBehaviour
{
    [SerializeField] float slideForce = 20f;

    private void OnTriggerStay(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if (!rb) return;

        Vector3 dir = transform.forward;
        rb.AddForce(dir * slideForce, ForceMode.Acceleration);
    }
}
