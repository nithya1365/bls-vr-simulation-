using UnityEngine;

public class VictimWalkAndCollapse : MonoBehaviour
{
    public float walkSpeed = 1.2f;
    public float walkDuration = 4f;

    private float timer = 0f;
    private bool collapsed = false;

    private Rigidbody rb;
    private Animator anim;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (collapsed) return;

        timer += Time.deltaTime;

        // Move victim forward
        transform.Translate(Vector3.forward * walkSpeed * Time.deltaTime, Space.Self);

        if (timer >= walkDuration)
        {
            Collapse();
        }
    }

    void Collapse()
    {
        collapsed = true;

        // Stop walking animation
        if (anim != null)
            anim.enabled = false;

        // Enable physics
        rb.isKinematic = false;
        rb.useGravity = true;

        // Add slight forward fall
        rb.AddTorque(-transform.right * 25f, ForceMode.Impulse);
    }
}
