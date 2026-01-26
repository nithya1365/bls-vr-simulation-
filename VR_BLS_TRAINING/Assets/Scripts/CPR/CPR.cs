using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

[RequireComponent(typeof(AudioSource))]
public class CPR_HandTracking_Controller : MonoBehaviour
{
    /* ================= HAND TRACKING ================= */
    private XRHandSubsystem handSubsystem;
    public XRHandJointID trackedJoint = XRHandJointID.Palm;

    [Header("CHEST REFERENCE")]
    public Transform chestReference; // Place on sternum

    /* ================= CPR DEPTH ================= */
    [Header("DEPTH (meters)")]
    public float minDepth = 0.05f;   // 5 cm
    public float maxDepth = 0.06f;   // 6 cm
    public float recoilThreshold = 0.005f;

    float startY;
    float deepestY;
    float releaseY;
    bool pressing = false;

    /* ================= RATE ================= */
    [Header("RATE")]
    public int compressionCount;
    public int compressionsPerMinute;
    List<float> compressionTimes = new List<float>();
    public float rateWindowSeconds = 30f;
    public float smoothing = 0.2f;
    float smoothedCPM;

    /* ================= UI ================= */
    [Header("UI")]
    public Slider depthGauge;
    public Image depthFillImage;
    public TMP_Text cpmText;
    public Image recoilIndicator;

    /* ================= METRONOME ================= */
    [Header("METRONOME")]
    public float metronomeBPM = 100f;
    AudioSource audioSource;
    float metronomeInterval;

    /* ================= START ================= */
    void Start()
    {
        // Get XR Hand Subsystem
        var xrManager = XRGeneralSettings.Instance.Manager;
        handSubsystem = xrManager.activeLoader?.GetLoadedSubsystem<XRHandSubsystem>();

        if (handSubsystem == null)
        {
            Debug.LogError("XR Hand Subsystem NOT found!");
            enabled = false;
            return;
        }

        audioSource = GetComponent<AudioSource>();
        metronomeInterval = 60f / metronomeBPM;
        InvokeRepeating(nameof(PlayMetronome), 0f, metronomeInterval);
    }

    /* ================= UPDATE ================= */
    void Update()
    {
        Debug.Log("UPDATE  cpr is RUNNING");

        XRHand rightHand = handSubsystem.rightHand;
        if (!rightHand.isTracked) return;

        XRHandJoint palm = rightHand.GetJoint(trackedJoint);
        if (!palm.TryGetPose(out Pose pose)) return;

        float handY = pose.position.y;
        float chestY = chestReference.position.y;

        // START COMPRESSION
        if (!pressing && handY < chestY)
        {
            pressing = true;
            startY = handY;
            deepestY = handY;
        }

        // TRACK DEPTH
        if (pressing && handY < deepestY)
        {
            deepestY = handY;
        }

        // RELEASE
        if (pressing && handY > chestY)
        {
            releaseY = handY;
            ProcessCompression();
            pressing = false;
        }
        //this is temp 
        if (Input.GetKeyDown(KeyCode.Alpha1))
            UpdateUI(0.03f);   // shallow red

        if (Input.GetKeyDown(KeyCode.Alpha2))
            UpdateUI(0.055f);  // correct green

        if (Input.GetKeyDown(KeyCode.Alpha3))
            UpdateUI(0.07f);   // deep orange
    }

    /* ================= PROCESS CPR ================= */
    void ProcessCompression()
    {
        float depth = startY - deepestY;

        // Ignore noise
        if (depth < 0.01f) return;

        compressionCount++;

        float now = Time.time;
        compressionTimes.Add(now);
        compressionTimes.RemoveAll(t => now - t > rateWindowSeconds);

        if (compressionTimes.Count >= 2)
        {
            float elapsed = compressionTimes[^1] - compressionTimes[0];
            float rawCPM = (compressionTimes.Count / elapsed) * 60f;
            smoothedCPM = smoothedCPM == 0 ? rawCPM : Mathf.Lerp(smoothedCPM, rawCPM, smoothing);
            compressionsPerMinute = Mathf.RoundToInt(smoothedCPM);
        }

        UpdateUI(depth);
    }

    /* ================= UI FEEDBACK ================= */
    void UpdateUI(float depth)
    {
        if (depthGauge == null || depthFillImage == null) return;

        depthGauge.value = depth;

        if (depth < minDepth)
        {
            depthFillImage.color = Color.red;        // too shallow
        }
        else if (depth > maxDepth)
        {
            depthFillImage.color = new Color(1f, 0.6f, 0f); // too deep (orange)
        }
        else
        {
            depthFillImage.color = Color.green;      // correct
        }

        if (cpmText != null)
            cpmText.text = $"CPM: {compressionsPerMinute}";

        if (recoilIndicator != null)
            recoilIndicator.color =
                Mathf.Abs(releaseY - startY) <= recoilThreshold
                ? Color.green
                : Color.red;
    }


    /* ================= METRONOME ================= */
    void PlayMetronome()
    {
        if (audioSource && audioSource.clip)
            audioSource.PlayOneShot(audioSource.clip);
    }
}
