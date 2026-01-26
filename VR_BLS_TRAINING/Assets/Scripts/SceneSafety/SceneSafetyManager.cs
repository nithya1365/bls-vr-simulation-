using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System.Collections;

public class SceneSafetyManager : MonoBehaviour
{
    [Header("=== SCENE SAFETY STATUS ===")]
    public bool isSceneSafe = false;

    [Header("=== SAFETY CHECKS ===")]
    public bool crowdCleared = false;
    public bool obstaclesRemoved = false;
    public bool emergencyCalled = false;

    [Header("=== SCENE OBJECTS ===")]
    public GameObject crowdGroup;
    public GameObject[] obstacles;
    public GameObject safeZoneIndicator;
    public Transform victimTransform;

    [Header("=== UI ELEMENTS ===")]
    public GameObject safetyWarningPanel;
    public TextMeshProUGUI instructionText;
    public Button clearCrowdButton;
    public Button removeObstaclesButton;
    public Button call911Button;
    public Button confirmSafeButton;
    public Slider progressBar;

    [Header("=== PHONE UI ===")]
    public GameObject phoneUI;
    public AudioSource dialingAudio;
    public AudioSource operatorAudio;

    [Header("=== LOCKED FEATURES ===")]
    public GameObject cprControlsUI;
    public GameObject aedSystemUI;

    [Header("=== EVENTS ===")]
    public UnityEvent OnSceneSafeConfirmed;

    [Header("=== TIMING & SCORING ===")]
    private float startTime;
    public float completionTime;
    public int safetyScore = 0;

    // ================= START =================
    void Start()
    {
        if (safetyWarningPanel != null)
            safetyWarningPanel.SetActive(false);

        if (safeZoneIndicator != null)
            safeZoneIndicator.SetActive(false);

        if (phoneUI != null)
            phoneUI.SetActive(false);

        LockEmergencyFeatures();
    }

    // ================= ENTRY =================
    public void TriggerSceneSafetyModule()
    {
        InitializeSceneSafety();
    }

    void InitializeSceneSafety()
    {
        LockEmergencyFeatures();

        if (safetyWarningPanel != null)
            safetyWarningPanel.SetActive(true);

        UpdateInstruction("SCENE IS UNSAFE! Clear the area before helping the victim.");

        clearCrowdButton.onClick.RemoveAllListeners();
        clearCrowdButton.onClick.AddListener(ClearCrowd);
        clearCrowdButton.interactable = true;

        removeObstaclesButton.onClick.RemoveAllListeners();
        removeObstaclesButton.onClick.AddListener(RemoveObstacles);
        removeObstaclesButton.interactable = false;

        call911Button.onClick.RemoveAllListeners();
        call911Button.onClick.AddListener(Call911);
        call911Button.interactable = false;

        confirmSafeButton.onClick.RemoveAllListeners();
        confirmSafeButton.onClick.AddListener(ConfirmSceneSafety);
        confirmSafeButton.interactable = false;

        if (progressBar != null)
            progressBar.value = 0f;

        startTime = Time.time;
    }

    // ================= STEP 1 =================
    public void ClearCrowd()
    {
        if (crowdCleared) return;
        crowdCleared = true;

        if (crowdGroup != null)
            StartCoroutine(MoveAwayAndDisableCrowd());

        clearCrowdButton.interactable = false;
        removeObstaclesButton.interactable = true;

        UpdateInstruction("Crowd cleared. Remove obstacles.");
        progressBar.value = 0.25f;
    }

    // ================= STEP 2 =================
    public void RemoveObstacles()
    {
        if (obstaclesRemoved) return;
        obstaclesRemoved = true;

        if (obstacles != null)
            StartCoroutine(MoveObstaclesAside());

        removeObstaclesButton.interactable = false;
        call911Button.interactable = true;

        if (safeZoneIndicator != null)
            safeZoneIndicator.SetActive(true);

        UpdateInstruction("Obstacles removed. Call emergency services.");
        progressBar.value = 0.5f;
    }

    // ================= STEP 3 =================
    public void Call911()
    {
        if (emergencyCalled) return;
        emergencyCalled = true;

        // 🔴 Hide Scene Safety UI while calling
        if (safetyWarningPanel != null)
            safetyWarningPanel.SetActive(false);

        if (phoneUI != null)
            phoneUI.SetActive(true);

        if (dialingAudio != null)
            dialingAudio.Play();

        call911Button.interactable = false;

        StartCoroutine(HandleEmergencyCall());
    }

    IEnumerator HandleEmergencyCall()
    {
        yield return new WaitForSeconds(3f);

        if (dialingAudio != null)
            dialingAudio.Stop();

        if (operatorAudio != null)
            operatorAudio.Play();

        // 🔴 Hide phone UI
        if (phoneUI != null)
            phoneUI.SetActive(false);

        // 🔴 Show scene safety UI again
        if (safetyWarningPanel != null)
            safetyWarningPanel.SetActive(true);

        UpdateInstruction("Emergency services contacted. Confirm scene safety.");

        confirmSafeButton.interactable = true;
        progressBar.value = 0.75f;
    }

    // ================= STEP 4 =================
    public void ConfirmSceneSafety()
    {
        if (isSceneSafe) return;
        isSceneSafe = true;

        completionTime = Time.time - startTime;
        CalculateSafetyScore();

        if (safetyWarningPanel != null)
            safetyWarningPanel.SetActive(false);

        UnlockEmergencyFeatures();
        progressBar.value = 1f;

        OnSceneSafeConfirmed?.Invoke();
    }

    // ================= HELPERS =================
    void UpdateInstruction(string message)
    {
        if (instructionText != null)
            instructionText.text = message;
    }

    void LockEmergencyFeatures()
    {
        if (cprControlsUI != null)
            cprControlsUI.SetActive(false);

        if (aedSystemUI != null)
            aedSystemUI.SetActive(false);
    }

    void UnlockEmergencyFeatures()
    {
        if (cprControlsUI != null)
            cprControlsUI.SetActive(true);
    }

    void CalculateSafetyScore()
    {
        if (completionTime < 20f) safetyScore = 100;
        else if (completionTime < 40f) safetyScore = 85;
        else if (completionTime < 60f) safetyScore = 70;
        else safetyScore = 50;
    }

    // ================= ANIMATIONS =================
    IEnumerator MoveAwayAndDisableCrowd()
    {
        float duration = 2f;
        float elapsed = 0f;

        Transform[] crowdMembers = crowdGroup.GetComponentsInChildren<Transform>();
        Vector3[] startPos = new Vector3[crowdMembers.Length];

        for (int i = 0; i < crowdMembers.Length; i++)
            startPos[i] = crowdMembers[i].position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            for (int i = 1; i < crowdMembers.Length; i++)
            {
                Vector3 dir = (crowdMembers[i].position - victimTransform.position).normalized;
                dir.y = 0;
                crowdMembers[i].position = Vector3.Lerp(startPos[i], startPos[i] + dir * 4f, t);
            }
            yield return null;
        }

        crowdGroup.SetActive(false);
    }

    IEnumerator MoveObstaclesAside()
    {
        float duration = 1.5f;
        float elapsed = 0f;

        Vector3[] startPos = new Vector3[obstacles.Length];
        Vector3[] targetPos = new Vector3[obstacles.Length];

        for (int i = 0; i < obstacles.Length; i++)
        {
            startPos[i] = obstacles[i].transform.position;
            targetPos[i] = startPos[i] + Vector3.right * 3f;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            for (int i = 0; i < obstacles.Length; i++)
                obstacles[i].transform.position = Vector3.Lerp(startPos[i], targetPos[i], t);

            yield return null;
        }
    }
}