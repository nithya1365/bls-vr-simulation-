using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class cpr_depth_test : MonoBehaviour
{
    [Header("Assign These")]
    public Transform rightHand;        // RightHandAnchor
    public Transform chestReference;   // ChestReference (sternum)

    [Header("CPR Depth Limits (meters)")]
    public float minCorrectDepth = 0.05f;
    public float maxCorrectDepth = 0.06f;

    [Header("CPR Rate Settings")]
    public float calibrationTime = 10f;     // first 10 seconds
    public float rateWindowSeconds = 30f;    // sliding window
    public float smoothing = 0.2f;

    [Header("Metronome")]
    public float metronomeBPM = 110f;
    public float stopOffset = 0.08f; // hand clearly above chest = CPR stopped

    AudioSource audioSource;
    float metronomeInterval;
    bool metronomeRunning = false;

    bool pressing = false;
    bool cprActive = false;

    float startY;
    float deepestY;
    float cprStartTime;

    List<float> compressionTimes = new List<float>();
    float smoothedCPM = 0f;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        metronomeInterval = 60f / metronomeBPM;
    }

    void Update()
    {
        if (rightHand == null || chestReference == null) return;

        float handY = rightHand.position.y;
        float chestY = chestReference.position.y;

        /* ================= START CPR ================= */
        if (!pressing && handY < chestY)
        {
            pressing = true;
            startY = chestY;
            deepestY = handY;

            if (!cprActive)
            {
                cprActive = true;
                cprStartTime = Time.time;
                compressionTimes.Clear();
                smoothedCPM = 0f;

                StartMetronome();
                Debug.Log("🫀 CPR STARTED");
            }
        }

        /* ================= TRACK DEPTH ================= */
        if (pressing && handY < deepestY)
        {
            deepestY = handY;
        }

        /* ================= RELEASE ================= */
        if (pressing && handY > chestY)
        {
            float depth = startY - deepestY;

            // Depth feedback
            if (depth < minCorrectDepth)
                Debug.Log($"⚠️ TOO SHALLOW: {depth:F3} m");
            else if (depth > maxCorrectDepth)
                Debug.Log($"❌ TOO DEEP: {depth:F3} m");
            else
                Debug.Log($"✅ DEPTH OK: {depth:F3} m");

            float now = Time.time;
            compressionTimes.Add(now);
            compressionTimes.RemoveAll(t => now - t > rateWindowSeconds);

            float elapsedSinceStart = now - cprStartTime;

            /* ===== CPM CALIBRATION PHASE ===== */
            if (elapsedSinceStart < calibrationTime)
            {
                Debug.Log("⏳ CPR RATE: calibrating...");
            }
            else if (compressionTimes.Count >= 3)
            {
                float elapsed = compressionTimes[^1] - compressionTimes[0];

                if (elapsed > 1f)
                {
                    float rawCPM = (compressionTimes.Count / elapsed) * 60f;
                    smoothedCPM = smoothedCPM == 0
                        ? rawCPM
                        : Mathf.Lerp(smoothedCPM, rawCPM, smoothing);

                    int cpm = Mathf.RoundToInt(smoothedCPM);

                    if (cpm < 100)
                        Debug.Log($"❌ CPR RATE: {cpm} CPM (Too Slow)");
                    else if (cpm > 120)
                        Debug.Log($"⚠️ CPR RATE: {cpm} CPM (Too Fast)");
                    else
                        Debug.Log($"✅ CPR RATE: {cpm} CPM (Good)");
                }
            }

            pressing = false;
        }

        /* ================= STOP CPR ================= */
        if (cprActive && handY > chestY + stopOffset)
        {
            StopMetronome();
            cprActive = false;
            pressing = false;

            Debug.Log("🛑 CPR STOPPED");
        }
    }

    /* ================= METRONOME ================= */
    void StartMetronome()
    {
        if (audioSource == null || audioSource.clip == null || metronomeRunning) return;
        metronomeRunning = true;
        InvokeRepeating(nameof(PlayMetronome), 0f, metronomeInterval);
    }

    void PlayMetronome()
    {
        audioSource.PlayOneShot(audioSource.clip);
    }

    void StopMetronome()
    {
        if (!metronomeRunning) return;
        CancelInvoke(nameof(PlayMetronome));
        metronomeRunning = false;
    }
}
