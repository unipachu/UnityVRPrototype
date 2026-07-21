using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class PerfInfoToTmpText : MonoBehaviour {
    [Header("Settings")]
    [SerializeField] bool showCurrentFps = false;
    [SerializeField] bool showCurrentFrameTime = true;
    [SerializeField] bool showLowestFps = false;
    [SerializeField] bool showLowestFrameTime = true;
    [SerializeField] bool showAvgFps = false;
    [SerializeField] bool showAvgFrameTime = true;
    [Tooltip("Duration, in seconds, over which the lowest FPS (highest frame time) is measured.")]
    [SerializeField] float lowestFpsMeasurementWindow = 5;
    [Tooltip("Duration, in seconds, over which the average FPS and frame time are calculated.")]
    [SerializeField] float avgFpsMeasurementWindow = 10;

    [Header("Refs")]
    [Tooltip("Text where we want to display performance info.")]
    [SerializeField] TMP_Text perfInfoText;

    class FrameSample {
        public float frameTime;
        public float age;
    }

    readonly List<FrameSample> frameSamples = new();

    void Update() {
        float deltaTime = Time.unscaledDeltaTime;

        // Age existing samples.
        for (int i = 0; i < frameSamples.Count; i++)
            frameSamples[i].age += deltaTime;

        // Remove samples no longer needed by either time window.
        float maxWindow = Mathf.Max(lowestFpsMeasurementWindow, avgFpsMeasurementWindow);
        for (int i = frameSamples.Count - 1; i >= 0; i--) {
            if (frameSamples[i].age > maxWindow)
                frameSamples.RemoveAt(i);
        }

        // Add current frame.
        frameSamples.Add(new FrameSample{frameTime = deltaTime, age = 0f});

        perfInfoText.text = CreatePerfInfoText();
    }

    string CreatePerfInfoText() {
        StringBuilder stringBuilder = new();

        float currentFrameTime = Time.unscaledDeltaTime;
        float currentFps = currentFrameTime > 0f ? 1f / currentFrameTime : 0f;

        if (showCurrentFps)
            stringBuilder.AppendLine($"FPS: {currentFps:F1}");

        if (showCurrentFrameTime)
            stringBuilder.AppendLine($"Frame: {currentFrameTime * 1000f:F2} ms");

        float worstFrameTime = 0f;
        bool hasWorst = false;

        float avgFrameTimeSum = 0f;
        int avgFrameCount = 0;

        for (int i = frameSamples.Count - 1; i >= 0; i--) {
            FrameSample sample = frameSamples[i];
            if (sample.age <= lowestFpsMeasurementWindow) {
                if (!hasWorst || sample.frameTime > worstFrameTime) {
                    worstFrameTime = sample.frameTime;
                    hasWorst = true;
                }
            }
            if (sample.age <= avgFpsMeasurementWindow) {
                avgFrameTimeSum += sample.frameTime;
                avgFrameCount++;
            }
        }

        if (hasWorst) {
            float lowestFps = 1f / worstFrameTime;
            if (showLowestFps)
                stringBuilder.AppendLine($"Lowest FPS ({lowestFpsMeasurementWindow:F0}s): {lowestFps:F1}");
            if (showLowestFrameTime)
                stringBuilder.AppendLine($"Worst Frame ({lowestFpsMeasurementWindow:F0}s): {worstFrameTime * 1000f:F2} ms");
        }

        if (avgFrameCount > 0) {
            float avgFrameTime = avgFrameTimeSum / avgFrameCount;
            float avgFps = 1f / avgFrameTime;
            if (showAvgFps)
                stringBuilder.AppendLine($"Avg FPS ({avgFpsMeasurementWindow:F0}s): {avgFps:F1}");
            if (showAvgFrameTime)
                stringBuilder.AppendLine($"Avg Frame ({avgFpsMeasurementWindow:F0}s): {avgFrameTime * 1000f:F2} ms");
        }

        return stringBuilder.ToString().TrimEnd();
    }
}
