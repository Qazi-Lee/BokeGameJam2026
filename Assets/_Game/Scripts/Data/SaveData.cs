using System;

/// <summary>
/// 本地单存档数据，仅保存关卡进度信息。
/// 扩展 Phase 可在此增加 levelStars 等字段。
/// </summary>
[Serializable]
public class SaveData
{
    /// <summary>当前进度关卡索引（继续游戏进入此关的场景初始位置）。</summary>
    public int currentLevelIndex;

    /// <summary>已通关关卡索引列表，例如 [0, 1] 表示第 1、2 关已通关。</summary>
    public int[] completedLevelIndices = Array.Empty<int>();

    /// <summary>是否存在有效存档。有存档时「继续游戏」可点击。</summary>
    public bool hasSave;

    /// <summary>创建新游戏存档。</summary>
    public static SaveData CreateNewGame()
    {
        return new SaveData
        {
            currentLevelIndex = 0,
            completedLevelIndices = Array.Empty<int>(),
            hasSave = true
        };
    }

    /// <summary>创建无存档的空数据。</summary>
    public static SaveData CreateEmpty()
    {
        return new SaveData
        {
            currentLevelIndex = 0,
            completedLevelIndices = Array.Empty<int>(),
            hasSave = false
        };
    }
}
