using UnityEngine;

public class AEDPad : MonoBehaviour
{
    // This is the tag of the correct pad zone
    public string correctZoneTag;

    // Has this pad been placed correctly?
    public bool placedCorrectly = false;

    private void OnTriggerEnter(Collider other)
    {
        // Check if this pad touched the correct zone
        if (other.CompareTag(correctZoneTag) && !placedCorrectly)
        {
            // Snap pad to the zone position
            transform.position = other.transform.position;
            transform.rotation = other.transform.rotation;

            // Lock the pad in place
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }

            placedCorrectly = true;

            Debug.Log(gameObject.name + " placed correctly");
        }
    }
}
