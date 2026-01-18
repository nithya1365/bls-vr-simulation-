using UnityEngine;

public class VictimWalkAndCollapse : MonoBehaviour
{
    public float walkSpeed = 1.2f;
    public float walkDuration = 4f;
    private float timer = 0f;
    private bool collapsed = false;
    private Rigidbody rb;
    private Animator anim;

    [Header("Scene Safety Integration")]
    public SceneSafetyManager sceneSafetyManager;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        if (sceneSafetyManager == null)
        {
            sceneSafetyManager = FindObjectOfType<SceneSafetyManager>();
        }
    }

    void Update()
    {
        if (collapsed) return;
        timer += Time.deltaTime;

        transform.Translate(Vector3.forward * walkSpeed * Time.deltaTime, Space.Self);

        if (timer >= walkDuration)
        {
            Collapse();
        }
    }

    void Collapse()
    {
        collapsed = true;
        Debug.Log("Victim collapsed!");

        if (anim != null)
        {
            anim.SetTrigger("Collapse");
        }

        if (rb != null)
        {
            rb.useGravity = true;
            rb.isKinematic = false;
        }

        StartCoroutine(TriggerSceneSafetyAfterDelay());
    }

    System.Collections.IEnumerator TriggerSceneSafetyAfterDelay()
    {
        yield return new WaitForSeconds(2f); // Wait 2 seconds

        if (sceneSafetyManager != null)
        {
            sceneSafetyManager.TriggerSceneSafetyModule();
        }
        else
        {
            Debug.LogError("Scene Safety Manager not found!");
        }
    }
}