using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityVolumeRendering;

/// <summary>
/// Lightweight IProgressHandler that fires an Action&lt;float&gt; callback (0–1) whenever progress
/// is reported. Replicates the weighted stage-stack math of EasyVolumeRendering's ProgressHandler.
///
/// Thread safety: ReportProgress is called from Task.Run background threads inside
/// CreateTextureInternalAsync. The callback is always dispatched to the Unity main thread
/// via UniTask.Post so callers can safely update UI.
/// </summary>
public class LoadingProgressHandler : IProgressHandler
{
    private struct Stage { public float Start, End; }

    private readonly Action<float> _onProgress;
    private readonly float _rangeStart;
    private readonly float _rangeEnd;

    // _totalProgress may be written from background threads; captured into the UniTask.Post
    // closure as a local 'scaled' value before posting, so no shared-state race on read.
    private float _totalProgress;
    private readonly Stack<Stage> _stageStack = new Stack<Stage>(4);

    /// <param name="onProgress">
    ///   Invoked on the Unity main thread with overall progress mapped to [rangeStart, rangeEnd].
    /// </param>
    /// <param name="rangeStart">Output value when internal progress is 0.</param>
    /// <param name="rangeEnd">Output value when internal progress is 1.</param>
    public LoadingProgressHandler(Action<float> onProgress, float rangeStart = 0f, float rangeEnd = 1f)
    {
        _onProgress = onProgress;
        _rangeStart = rangeStart;
        _rangeEnd = rangeEnd;
        _stageStack.Push(new Stage { Start = 0f, End = 1f });
    }

    // --- IProgressHandler -----------------------------------------------------------------------

    public void StartStage(float weight, string description = "")
    {
        Stage parent = _stageStack.Peek();
        _stageStack.Push(new Stage
        {
            Start = _totalProgress,
            End   = _totalProgress + (parent.End - parent.Start) * weight
        });
    }

    public void EndStage()
    {
        // Set progress to the top of the current stage, then pop it.
        ReportProgress(1f);
        _stageStack.Pop();
        // _totalProgress is now == the popped stage's End, which is correct for the parent.
    }

    public void ReportProgress(float progress, string description = "")
    {
        _totalProgress = AbsoluteProgress(progress);
        Notify();
    }

    public void ReportProgress(int currentStep, int totalSteps, string description = "")
        => ReportProgress(currentStep / (float)totalSteps);

    public void Fail() { }

    // --- Internals ------------------------------------------------------------------------------

    private float AbsoluteProgress(float progress)
    {
        Stage stage = _stageStack.Peek();
        return Mathf.Lerp(stage.Start, stage.End, progress);
    }

    private void Notify()
    {
        // Capture scaled value now (may be on background thread) and post to main thread.
        float scaled = Mathf.Lerp(_rangeStart, _rangeEnd, Mathf.Clamp01(_totalProgress));
        UniTask.Post(() => _onProgress?.Invoke(scaled), PlayerLoopTiming.Update);
    }
}
