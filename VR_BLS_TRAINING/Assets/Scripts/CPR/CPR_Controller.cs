using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(AudioSource))]
public class CPR_Controller : MonoBehaviour
{
    // ===================== DEPTH =====================
    public float minDepth = 0.05f;   // 5 cm
    public float maxDepth = 0.06f;   // 6 cm
    public float recoilThreshold = 0.005f; // 5 mm

    float startY;
    float deepestY;
    float releaseY;
    bool pressing;

    // ===================== COUNT =====================
    public int compressionCount = 0;

    // ===================== RATE =====================
    public int compressionsPerMinute;
    List<float> compressionTimes = new List<float>();

    public float rateWindowSeconds = 30f;

    [Range(0.05f, 0.5f)]
    public float cpmSmoothing = 0.2f;
    float smoothedCPM = 0f;

    // ===================== METRONOME =====================
    public float metronomeBPM = 90f;
    AudioSource metronome;
    float metronomeInterval;

    // ===================== UI =====================
    [Header("UI")]
    public Slider depthGauge;
    public TMP_Text cpmText;
    public Image recoilIndicator;

    // ===================== START =====================
    void Start()
    {
        metronome = GetComponent<AudioSource>();
        metronome.playOnAwake = false;
        metronome.loop = false;

        metronomeInterval = 60f / metronomeBPM;
        InvokeRepeating(nameof(PlayClick), 0f, metronomeInterval);
    }

    void PlayClick()
    {
        if (metronome && metronome.clip)
            metronome.PlayOneShot(metronome.clip);
    }

    // ===================== CPR LOGIC =====================
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Hand")) return;

        startY = other.transform.position.y;
        deepestY = startY;
        pressing = true;
    }

    void OnTriggerStay(Collider other)
    {
        if (!pressing || !other.CompareTag("Hand")) return;

        float y = other.transform.position.y;
        if (y < deepestY)
            deepestY = y;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Hand")) return;

        releaseY = other.transform.position.y;

        float depth = startY - deepestY;

        // Ignore accidental tiny touches
        if (depth < 0.01f)
        {
            pressing = false;
            return;
        }

        compressionCount++;

        // ================= RATE =================
        float now = Time.time;
        compressionTimes.Add(now);
        compressionTimes.RemoveAll(t => now - t > rateWindowSeconds);

        if (compressionTimes.Count >= 2)
        {
            float elapsed = compressionTimes[^1] - compressionTimes[0];
            if (elapsed > 0f)
            {
                float rawCPM = (compressionTimes.Count / elapsed) * 60f;
                smoothedCPM = smoothedCPM == 0f
                    ? rawCPM
                    : Mathf.Lerp(smoothedCPM, rawCPM, cpmSmoothing);

                compressionsPerMinute = Mathf.RoundToInt(smoothedCPM);
            }
        }

        // ================= DEPTH GAUGE =================
        if (depthGauge != null)
        {
            depthGauge.value = depth;

            if (depth < minDepth)
                depthGauge.fillRect.GetComponent<Image>().color = Color.red;
            else if (depth > maxDepth)
                depthGauge.fillRect.GetComponent<Image>().color = new Color(1f, 0.6f, 0f);
            else
                depthGauge.fillRect.GetComponent<Image>().color = Color.green;
        }

        // ================= CPM TEXT =================
        if (cpmText != null)
        {
            cpmText.text = $"CPM: {compressionsPerMinute}";

            if (compressionsPerMinute < 100)
                cpmText.color = Color.red;
            else if (compressionsPerMinute > 120)
                cpmText.color = new Color(1f, 0.6f, 0f);
            else
                cpmText.color = Color.green;
        }

        // ================= RECOIL =================
        bool fullRecoil = Mathf.Abs(releaseY - startY) <= recoilThreshold;

        if (recoilIndicator != null)
        {
            recoilIndicator.color = fullRecoil ? Color.green : Color.red;
        }

        pressing = false;
    }
}
