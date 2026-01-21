using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class SceneSafetyManager : MonoBehaviour
{
    [Header("SCENE SAFETY STATUS")]
    public bool isSceneSafe = false;

    [Header("SAFETY CHECKS")]
    public bool crowdCleared = false;
    public bool obstaclesRemoved = false;

    [Header("SCENE OBJECTS")]
    public GameObject crowdGroup;
    public GameObject[] obstacles;
    public GameObject safeZoneIndicator;
    public Transform victimTransform;

    [Header("UI ELEMENTS")]
    public GameObject safetyWarningPanel;
    public TextMeshProUGUI instructionText;
    public Button clearCrowdButton;
    public Button removeObstaclesButton;
    public Button confirmSafeButton;
    public Slider progressBar;

    [Header("LOCKED FEATURES")]
    public GameObject cprControlsUI;
    public GameObject aedSystemUI;

    [Header("EVENTS")]
    public UnityEvent OnSceneSafeConfirmed;

    [Header("TIMING & SCORING")]
    private float startTime;
    public float completionTime;
    public int safetyScore = 0;

    [Header("XR SETUP")]
    public Canvas uiCanvas;
    public float canvasDistance = 2f;
    public Transform xrRig;

    void Start()
    {
        SetupXRCanvas();

        if (safetyWarningPanel != null)
            safetyWarningPanel.SetActive(false);

        if (safeZoneIndicator != null)
            safeZoneIndicator.SetActive(false);

        LockEmergencyFeatures();
    }

    void SetupXRCanvas()
    {
        if (uiCanvas == null)
        {
            uiCanvas = GetComponentInParent<Canvas>();
            if (uiCanvas == null)
            {
                Debug.LogWarning("No Canvas found! Make sure UI is on a Canvas.");
                return;
            }
        }

        uiCanvas.renderMode = RenderMode.WorldSpace;
        uiCanvas.transform.localScale = Vector3.one * 0.001f;

        GraphicRaycaster standardRaycaster = uiCanvas.GetComponent<GraphicRaycaster>();
        if (standardRaycaster != null)
        {
            DestroyImmediate(standardRaycaster);
        }

        if (uiCanvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
        {
            uiCanvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            Debug.Log("Added TrackedDeviceGraphicRaycaster for XR interaction");
        }

        PositionCanvasInFrontOfPlayer();
    }

    void PositionCanvasInFrontOfPlayer()
    {
        if (uiCanvas == null) return;

        if (xrRig == null)
        {
            GameObject xrOrigin = GameObject.Find("XR Origin") ?? GameObject.Find("XR Rig");
            if (xrOrigin != null)
            {
                xrRig = xrOrigin.transform;
            }
            else if (Camera.main != null)
            {
                xrRig = Camera.main.transform;
            }
        }

        if (xrRig != null)
        {
            Vector3 forward = xrRig.forward;
            forward.y = 0;
            forward.Normalize();

            uiCanvas.transform.position =
                xrRig.position + forward * canvasDistance + Vector3.up * 1.5f;

            uiCanvas.transform.rotation = Quaternion.LookRotation(forward);
        }
    }

    public void TriggerSceneSafetyModule()
    {
        Debug.Log("SCENE SAFETY MODULE STARTED");
        PositionCanvasInFrontOfPlayer();
        InitializeSceneSafety();
    }

    void InitializeSceneSafety()
    {
        LockEmergencyFeatures();

        if (safetyWarningPanel != null)
            safetyWarningPanel.SetActive(true);

        if (safeZoneIndicator != null)
            safeZoneIndicator.SetActive(false);

        UpdateInstruction("SCENE IS UNSAFE! Clear the area before helping the victim.");

        if (clearCrowdButton != null)
        {
            clearCrowdButton.onClick.RemoveAllListeners();
            clearCrowdButton.onClick.AddListener(ClearCrowd);
            clearCrowdButton.interactable = true;
        }

        if (removeObstaclesButton != null)
        {
            removeObstaclesButton.onClick.RemoveAllListeners();
            removeObstaclesButton.onClick.AddListener(RemoveObstacles);
            removeObstaclesButton.interactable = false;
        }

        if (confirmSafeButton != null)
        {
            confirmSafeButton.onClick.RemoveAllListeners();
            confirmSafeButton.onClick.AddListener(ConfirmSceneSafety);
            confirmSafeButton.interactable = false;
        }

        startTime = Time.time;

        if (progressBar != null)
            progressBar.value = 0f;
    }

    public void ClearCrowd()
    {
        if (crowdCleared) return;

        Debug.Log("[STEP 1] Clearing crowd...");
        crowdCleared = true;

        if (crowdGroup != null)
            StartCoroutine(MoveAwayAndDisableCrowd());

        clearCrowdButton.interactable = false;
        UpdateInstruction("Crowd cleared. Now remove obstacles from the area.");

        if (removeObstaclesButton != null)
            removeObstaclesButton.interactable = true;

        if (progressBar != null)
            progressBar.value = 0.33f;
    }

    public void RemoveObstacles()
    {
        if (obstaclesRemoved) return;

        Debug.Log("[STEP 2] Removing obstacles...");
        obstaclesRemoved = true;

        if (obstacles != null)
            StartCoroutine(MoveObstaclesAside());

        removeObstaclesButton.interactable = false;

        if (safeZoneIndicator != null)
            safeZoneIndicator.SetActive(true);

        UpdateInstruction("Obstacles removed. Confirm scene safety.");

        if (confirmSafeButton != null)
            confirmSafeButton.interactable = true;

        if (progressBar != null)
            progressBar.value = 0.66f;
    }

    public void ConfirmSceneSafety()
    {
        if (isSceneSafe) return;

        Debug.Log("[STEP 3] SCENE CONFIRMED SAFE!");
        isSceneSafe = true;

        completionTime = Time.time - startTime;
        CalculateSafetyScore();

        if (safetyWarningPanel != null)
            safetyWarningPanel.SetActive(false);

        UnlockEmergencyFeatures();

        if (progressBar != null)
            progressBar.value = 1f;

        Debug.Log($"Scene Safety Complete! Time: {completionTime:F1}s | Score: {safetyScore}/100");

        OnSceneSafeConfirmed?.Invoke();
    }

    void UpdateInstruction(string message)
    {
        if (instructionText != null)
            instructionText.text = message;

        Debug.Log("[Instruction] " + message);
    }

    void LockEmergencyFeatures()
    {
        if (cprControlsUI != null)
            cprControlsUI.SetActive(false);

        if (aedSystemUI != null)
            aedSystemUI.SetActive(false);

        Debug.Log("CPR and AED features LOCKED.");
    }

    void UnlockEmergencyFeatures()
    {
        if (cprControlsUI != null)
            cprControlsUI.SetActive(true);

        Debug.Log("CPR controls UNLOCKED.");
    }

    void CalculateSafetyScore()
    {
        if (completionTime < 20f)
            safetyScore = 100;
        else if (completionTime < 40f)
            safetyScore = 85;
        else if (completionTime < 60f)
            safetyScore = 70;
        else
            safetyScore = 50;
    }

    System.Collections.IEnumerator MoveAwayAndDisableCrowd()
    {
        float duration = 2f;
        float elapsed = 0f;
        float moveDistance = 5f;

        Transform[] crowdMembers = crowdGroup.GetComponentsInChildren<Transform>();
        Vector3[] startPositions = new Vector3[crowdMembers.Length];

        for (int i = 0; i < crowdMembers.Length; i++)
            startPositions[i] = crowdMembers[i].position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            for (int i = 1; i < crowdMembers.Length; i++)
            {
                Vector3 dir = (crowdMembers[i].position - victimTransform.position).normalized;
                dir.y = 0;

                crowdMembers[i].position =
                    Vector3.Lerp(startPositions[i], startPositions[i] + dir * moveDistance, t);
            }

            yield return null;
        }

        crowdGroup.SetActive(false);
    }

    System.Collections.IEnumerator MoveObstaclesAside()
    {
        float duration = 1.5f;
        float elapsed = 0f;
        float moveDistance = 3f;

        Vector3[] startPos = new Vector3[obstacles.Length];

        for (int i = 0; i < obstacles.Length; i++)
            startPos[i] = obstacles[i].transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            for (int i = 0; i < obstacles.Length; i++)
                obstacles[i].transform.position =
                    Vector3.Lerp(startPos[i], startPos[i] + Vector3.right * moveDistance, t);

            yield return null;
        }
    }

    public bool IsSceneSafe() => isSceneSafe;
    public int GetSafetyScore() => safetyScore;
}
