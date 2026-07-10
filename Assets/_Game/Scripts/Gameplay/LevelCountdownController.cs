using System;
using UnityEngine;

/// <summary>
/// 关卡倒计时：驱动 TopBar 进度条，归零时扣星并播放音效（不失败）。
/// 低血量闪烁/警告特效预留接口，确认后再接 prefab。
/// </summary>
public class LevelCountdownController : BaseMonoManager<LevelCountdownController>
{
    /// <summary>剩余时间比例低于此值时进入「警告阶段」（供后续 VFX 使用）。</summary>
    public const float WarningThresholdNormalized = 0.2f;

    private TopBarController topBar;
    private float totalSeconds;
    private float remainingSeconds;
    private bool isRunning;
    private bool timeoutHandled;
    private bool warningPhaseActive;

    public float TotalSeconds => totalSeconds;

    public float RemainingSeconds => remainingSeconds;

    public float RemainingNormalized =>
        totalSeconds > 0f ? Mathf.Clamp01(remainingSeconds / totalSeconds) : 1f;

    public bool IsRunning => isRunning;

    public bool IsWarningPhase => warningPhaseActive;

    /// <summary>进入警告阶段（剩余 &lt; 20%）时触发，供 vfx_jingao 等接入。</summary>
    public event Action WarningPhaseStarted;

    /// <summary>离开警告阶段或倒计时停止时触发。</summary>
    public event Action WarningPhaseEnded;

    /// <summary>倒计时归零且已扣星时触发，供 vfx_camera 停止等接入。</summary>
    public event Action CountdownExpired;

    protected override bool PersistAcrossScenes => true;

    public static void EnsureInstance()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject controllerObject = new GameObject(nameof(LevelCountdownController));
        controllerObject.AddComponent<LevelCountdownController>();
    }

    public void StartForLevel(LevelEntry levelEntry, TopBarController bar)
    {
        Stop();

        topBar = bar;
        if (levelEntry == null || levelEntry.timeLimitSeconds <= 0f)
        {
            topBar?.ConfigureCountdown(false);
            return;
        }

        totalSeconds = levelEntry.timeLimitSeconds;
        remainingSeconds = totalSeconds;
        isRunning = true;
        timeoutHandled = false;
        warningPhaseActive = false;

        topBar?.ConfigureCountdown(true, totalSeconds);
        topBar?.SetCountdownRemaining(totalSeconds);
        UpdateWarningPhase(false);
    }

    public void Stop()
    {
        if (warningPhaseActive)
        {
            UpdateWarningPhase(false);
        }

        isRunning = false;
        totalSeconds = 0f;
        remainingSeconds = 0f;
        timeoutHandled = false;
        topBar = null;
    }

    private void Update()
    {
        if (!isRunning)
        {
            return;
        }

        if (!CanTick())
        {
            return;
        }

        remainingSeconds -= Time.deltaTime;
        if (remainingSeconds <= 0f)
        {
            remainingSeconds = 0f;
            topBar?.SetCountdownRemaining(0f);
            HandleTimeoutOnce();
            return;
        }

        topBar?.SetCountdownRemaining(remainingSeconds);
        UpdateWarningPhase(RemainingNormalized <= WarningThresholdNormalized);
    }

    private bool CanTick()
    {
        if (SceneFlowManager.Instance != null && SceneFlowManager.Instance.IsLoading)
        {
            return false;
        }

        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying)
        {
            return false;
        }

        return true;
    }

    private void HandleTimeoutOnce()
    {
        if (timeoutHandled)
        {
            return;
        }

        timeoutHandled = true;
        UpdateWarningPhase(false);

        AudioManager.Instance?.PlayCountdown();

        LevelStarTracker.EnsureInstance();
        LevelStarTracker.Instance?.ReportTimeout();

        CountdownExpired?.Invoke();
    }

    private void UpdateWarningPhase(bool shouldWarn)
    {
        if (warningPhaseActive == shouldWarn)
        {
            return;
        }

        warningPhaseActive = shouldWarn;
        if (warningPhaseActive)
        {
            WarningPhaseStarted?.Invoke();
        }
        else
        {
            WarningPhaseEnded?.Invoke();
        }
    }
}
