using System.Collections;

using UnityEngine;

public class VictimWalkAndCollapse : MonoBehaviour
{
    [Header("WALK SETTINGS")]
    public float walkSpeed = 1.2f;
    public float walkDuration = 4f;

    private float timer = 0f;
    private bool collapsed = false;

    private Rigidbody rb;
    private Animator anim;

    [Header("SCENE SAFETY INTEGRATION")]
    public SceneSafetyManager sceneSafetyManager;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        if (sceneSafetyManager == null)
        {
            sceneSafetyManager = FindObjectOfType<SceneSafetyManager>();
        }

        // Ensure correct initial physics state
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }

    void Update()
    {
        if (collapsed) return;

        timer += Time.deltaTime;

        // Move victim forward while walking
        transform.Translate(Vector3.forward * walkSpeed * Time.deltaTime, Space.Self);

        if (timer >= walkDuration)
        {
            Collapse();
        }
    }

    void Collapse()
    {
        if (collapsed) return;
        collapsed = true;

        Debug.Log("Victim collapsed!");

        // Trigger collapse animation
        if (anim != null)
        {
            anim.SetTrigger("Collapse");

            // Disable animator AFTER animation finishes
            StartCoroutine(DisableAnimatorAfterCollapse());
        }

        // Enable physics so body falls naturally
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = true;
            rb.isKinematic = false;
        }

        // Trigger scene safety after delay
        StartCoroutine(TriggerSceneSafetyAfterDelay());
    }

    IEnumerator DisableAnimatorAfterCollapse()
    {
        // Wait for collapse animation to finish
        yield return new WaitForSeconds(1.2f);

        if (anim != null)
        {
            anim.enabled = false;
        }
    }

    IEnumerator TriggerSceneSafetyAfterDelay()
    {
        // Short pause before showing scene safety UI
        yield return new WaitForSeconds(2f);

        if (sceneSafetyManager != null)
        {
            sceneSafetyManager.TriggerSceneSafetyModule();
        }
        else
        {
            Debug.LogError("SceneSafetyManager not found!");
        }
    }
}