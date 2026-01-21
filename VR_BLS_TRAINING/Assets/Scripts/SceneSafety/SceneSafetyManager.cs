using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

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
    public Transform xrRig; // Assign XR Origin/Rig 

    void Start()
    {
        SetupXRCanvas();

        if (safetyWarningPanel != null)
            safetyWarningPanel.SetActive(false);

        if (safeZoneIndicator != null)
            safeZoneIndicator.SetActive(false);

        // Lock features immediately
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

        // Set canvas to World Space for XR
        uiCanvas.renderMode = RenderMode.WorldSpace;

        // Scale down the canvas for VR
        uiCanvas.transform.localScale = Vector3.one * 0.001f;

        // Add GraphicRaycaster if not present
        if (uiCanvas.GetComponent<GraphicRaycaster>() == null)
        {
            uiCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        // Add TrackedDeviceGraphicRaycaster for XR
        if (uiCanvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
        {
            var trackedRaycaster = uiCanvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            // Remove standard GraphicRaycaster if TrackedDevice version is added
            var standardRaycaster = uiCanvas.GetComponent<GraphicRaycaster>();
            if (standardRaycaster != null && !(standardRaycaster is TrackedDeviceGraphicRaycaster))
            {
                Destroy(standardRaycaster);
            }
        }

        PositionCanvasInFrontOfPlayer();
    }

    void PositionCanvasInFrontOfPlayer()
    {
        if (uiCanvas == null) return;

        // Find XR Rig if not assigned
        if (xrRig == null)
        {
            GameObject xrOrigin = GameObject.Find("XR Origin") ?? GameObject.Find("XR Rig");
            if (xrOrigin != null)
            {
                xrRig = xrOrigin.transform;
            }
            else
            {
                // Try to find camera
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    xrRig = mainCam.transform.parent;
                }
            }
        }

        if (xrRig != null)
        {
            // Position canvas in front of player
            Vector3 forward = xrRig.forward;
            forward.y = 0; // Keep it level
            forward.Normalize();

            uiCanvas.transform.position = xrRig.position + forward * canvasDistance + Vector3.up * 1.5f;
            uiCanvas.transform.rotation = Quaternion.LookRotation(forward);
        }
    }

    public void TriggerSceneSafetyModule()
    {
        Debug.Log("SCENE SAFETY MODULE STARTED");
        PositionCanvasInFrontOfPlayer(); // Reposition UI when triggered
        InitializeSceneSafety();
    }

    void InitializeSceneSafety()
    {
        // Lock CPR and AED features
        LockEmergencyFeatures();

        // now show warning (after collapse)
        if (safetyWarningPanel != null)
            safetyWarningPanel.SetActive(true);

        // Hide safe zone
        if (safeZoneIndicator != null)
            safeZoneIndicator.SetActive(false);

        // Set initial instruction
        UpdateInstruction("SCENE IS UNSAFE! Clear the area before helping the victim.");

        // Setup buttons
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

        // Set progress to 0
        if (progressBar != null)
            progressBar.value = 0;
    }

    public void ClearCrowd()
    {
        if (crowdCleared) return;

        Debug.Log("[STEP 1] Clearing crowd...");
        crowdCleared = true;

        if (crowdGroup != null)
        {
            StartCoroutine(MoveAwayAndDisableCrowd());
        }

        clearCrowdButton.interactable = false;
        UpdateInstruction("Crowd cleared. Now remove obstacles from the area.");

        if (removeObstaclesButton != null)
            removeObstaclesButton.interactable = true;

        // Update progress
        if (progressBar != null)
            progressBar.value = 0.33f;
    }

    public void RemoveObstacles()
    {
        if (obstaclesRemoved) return;

        Debug.Log("[STEP 2] Removing obstacles...");
        obstaclesRemoved = true;

        if (obstacles != null)
        {
            StartCoroutine(MoveObstaclesAside());
        }

        removeObstaclesButton.interactable = false;

        if (safeZoneIndicator != null)
            safeZoneIndicator.SetActive(true);

        UpdateInstruction("Obstacles removed. Safe zone identified. Confirm scene is safe.");

        if (confirmSafeButton != null)
            confirmSafeButton.interactable = true;

        // Update progress
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

        // Complete progress
        if (progressBar != null)
            progressBar.value = 1f;

        Debug.Log($"Scene Safety Complete! Time: {completionTime:F1}s | Score: {safetyScore}/100");

        OnSceneSafeConfirmed?.Invoke();
    }

    void UpdateInstruction(string message)
    {
        if (instructionText != null)
            instructionText.text = message;
        Debug.Log($"[Instruction] {message}");
    }

    void LockEmergencyFeatures()
    {
        if (cprControlsUI != null)
            cprControlsUI.SetActive(false);
        if (aedSystemUI != null)
            aedSystemUI.SetActive(false);
        Debug.Log("CPR and AED features LOCKED until scene is safe.");
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

        Debug.Log($"Safety Score: {safetyScore}/100 (Time: {completionTime:F1}s)");
    }

    System.Collections.IEnumerator MoveAwayAndDisableCrowd()
    {
        float duration = 2f;
        float elapsed = 0f;
        float moveDistance = 5f;

        Transform[] crowdMembers = crowdGroup.GetComponentsInChildren<Transform>();
        Vector3[] startPositions = new Vector3[crowdMembers.Length];

        for (int i = 0; i < crowdMembers.Length; i++)
        {
            startPositions[i] = crowdMembers[i].position;
        }

        // Move crowd backward
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            for (int i = 1; i < crowdMembers.Length; i++)
            {
                Vector3 directionAway = (crowdMembers[i].position - victimTransform.position).normalized;
                directionAway.y = 0;

                Vector3 targetPos = startPositions[i] + (directionAway * moveDistance);
                crowdMembers[i].position = Vector3.Lerp(startPositions[i], targetPos, progress);
            }

            yield return null;
        }

        // Now fade and disable
        yield return StartCoroutine(FadeAndDisableCrowd());
        Debug.Log("Crowd moved back and faded out.");
    }

    System.Collections.IEnumerator FadeAndDisableCrowd()
    {
        float duration = 1f;
        float elapsed = 0f;

        Renderer[] crowdRenderers = crowdGroup.GetComponentsInChildren<Renderer>();

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1 - (elapsed / duration);

            foreach (Renderer rend in crowdRenderers)
            {
                if (rend.material.HasProperty("_Color"))
                {
                    Color color = rend.material.color;
                    color.a = alpha;
                    rend.material.color = color;
                }
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

        Vector3[] startPositions = new Vector3[obstacles.Length];
        Vector3[] targetPositions = new Vector3[obstacles.Length];

        for (int i = 0; i < obstacles.Length; i++)
        {
            if (obstacles[i] != null)
            {
                startPositions[i] = obstacles[i].transform.position;
                targetPositions[i] = startPositions[i] + (Vector3.right * moveDistance);
            }
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            for (int i = 0; i < obstacles.Length; i++)
            {
                if (obstacles[i] != null)
                {
                    obstacles[i].transform.position = Vector3.Lerp(startPositions[i], targetPositions[i], progress);
                }
            }

            yield return null;
        }

        Debug.Log("Obstacles moved aside.");
    }

    public bool IsSceneSafe()
    {
        return isSceneSafe;
    }

    public int GetSafetyScore()
    {
        return safetyScore;
    }
}