using System;
using UnityEngine;

/// <summary>
/// 当前关卡局内星级（3 星起，碰障碍/超时各最多扣 1 星）。每关加载时重置，胜利面板点击继续后写入存档。
/// </summary>
public class LevelStarTracker : BaseMonoManager<LevelStarTracker>
{
    public const int MaxStars = 3;

    public int CurrentStars { get; private set; } = MaxStars;

    public bool ObstaclePenaltyApplied { get; private set; }

    public bool TimeoutPenaltyApplied { get; private set; }

    public event Action<int> StarsChanged;

    protected override bool PersistAcrossScenes => true;

    public static void EnsureInstance()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject trackerObject = new GameObject(nameof(LevelStarTracker));
        trackerObject.AddComponent<LevelStarTracker>();
    }

    public void ResetForLevel()
    {
        CurrentStars = MaxStars;
        ObstaclePenaltyApplied = false;
        TimeoutPenaltyApplied = false;
        StarsChanged?.Invoke(CurrentStars);
    }

    /// <summary>碰到障碍物：本局最多扣 1 星。返回是否实际扣星。</summary>
    public bool ReportObstacleHit()
    {
        if (ObstaclePenaltyApplied || CurrentStars <= 0)
        {
            return false;
        }

        ObstaclePenaltyApplied = true;
        CurrentStars--;
        StarsChanged?.Invoke(CurrentStars);
        return true;
    }

    /// <summary>倒计时结束：本局最多扣 1 星（Step 4 使用）。</summary>
    public bool ReportTimeout()
    {
        if (TimeoutPenaltyApplied || CurrentStars <= 0)
        {
            return false;
        }

        TimeoutPenaltyApplied = true;
        CurrentStars--;
        StarsChanged?.Invoke(CurrentStars);
        return true;
    }
}
