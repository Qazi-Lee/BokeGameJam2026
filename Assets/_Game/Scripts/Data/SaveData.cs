using System;

/// <summary>
/// 本地单存档数据：关卡进度 + 每关星级 + 每关独立 BGM/SFX 音量。
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

    /// <summary>各关 BGM 音量（索引 = 关卡索引），缺省 1。</summary>
    public float[] levelBgmVolumes;

    /// <summary>各关 SFX 音量（索引 = 关卡索引），缺省 1。</summary>
    public float[] levelSfxVolumes;

    /// <summary>各关是否静音（索引 = 关卡索引），缺省 false（有声音）。</summary>
    public bool[] levelMuted;

    /// <summary>各关历史最高星数（索引 = 关卡索引），未通关过为 0。</summary>
    public int[] levelStars;

    /// <summary>创建新游戏存档。</summary>
    public static SaveData CreateNewGame()
    {
        SaveData data = new SaveData
        {
            currentLevelIndex = 0,
            completedLevelIndices = Array.Empty<int>(),
            hasSave = true
        };
        data.EnsureLevelSaveCapacity(GameConstants.LevelCount);
        return data;
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

    /// <summary>确保每关存档数组长度与关卡数一致，旧存档缺项补默认值。</summary>
    public void EnsureLevelSaveCapacity(int levelCount)
    {
        if (levelCount <= 0)
        {
            levelBgmVolumes = Array.Empty<float>();
            levelSfxVolumes = Array.Empty<float>();
            levelMuted = Array.Empty<bool>();
            levelStars = Array.Empty<int>();
            return;
        }

        levelBgmVolumes = EnsureVolumeArray(levelBgmVolumes, levelCount);
        levelSfxVolumes = EnsureVolumeArray(levelSfxVolumes, levelCount);
        levelMuted = EnsureMuteArray(levelMuted, levelCount);
        levelStars = EnsureStarArray(levelStars, levelCount);
    }

    /// <summary>兼容旧调用。</summary>
    public void EnsureLevelAudioCapacity(int levelCount)
    {
        EnsureLevelSaveCapacity(levelCount);
    }

    private static int[] EnsureStarArray(int[] existing, int levelCount)
    {
        int[] result = new int[levelCount];
        for (int i = 0; i < levelCount; i++)
        {
            if (existing != null && i < existing.Length)
            {
                result[i] = ClampStarCount(existing[i]);
            }
            else
            {
                result[i] = 0;
            }
        }

        return result;
    }

    private static int ClampStarCount(int value)
    {
        if (value < 0)
        {
            return 0;
        }

        if (value > LevelStarTracker.MaxStars)
        {
            return LevelStarTracker.MaxStars;
        }

        return value;
    }

    private static bool[] EnsureMuteArray(bool[] existing, int levelCount)
    {
        bool[] result = new bool[levelCount];
        for (int i = 0; i < levelCount; i++)
        {
            if (existing != null && i < existing.Length)
            {
                result[i] = existing[i];
            }
            else
            {
                result[i] = false;
            }
        }

        return result;
    }

    private static float[] EnsureVolumeArray(float[] existing, int levelCount)
    {
        float[] result = new float[levelCount];
        for (int i = 0; i < levelCount; i++)
        {
            if (existing != null && i < existing.Length)
            {
                result[i] = Clamp01(existing[i]);
            }
            else
            {
                result[i] = GameConstants.DefaultAudioVolume;
            }
        }

        return result;
    }

    private static float Clamp01(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        if (value > 1f)
        {
            return 1f;
        }

        return value;
    }
}
