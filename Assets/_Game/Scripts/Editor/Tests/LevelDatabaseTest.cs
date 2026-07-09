#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 编辑器下验证 LevelDatabaseSO 查询逻辑是否正常。
/// 菜单：Game > Tests > Validate LevelDatabase
/// </summary>
public static class LevelDatabaseTest
{
    private const string AssetPath = "Assets/_Game/Data/ScriptableObjects/LevelDatabase.asset";

    [MenuItem("Game/Tests/Validate LevelDatabase")]
    public static void ValidateLevelDatabase()
    {
        LevelDatabaseSO database = AssetDatabase.LoadAssetAtPath<LevelDatabaseSO>(AssetPath);
        if (database == null)
        {
            Debug.LogError($"[LevelDatabaseTest] 未找到资产：{AssetPath}");
            return;
        }

        int passed = 0;
        int failed = 0;

        passed += AssertEqual("LevelCount", 3, database.LevelCount, ref failed);
        passed += AssertEqual("MainMenuSceneName", GameConstants.SceneNames.MainMenu,
            database.MainMenuSceneName, ref failed);

        int sampleIndex = database.GetIndexByScene(GameConstants.SceneNames.SampleScene);
        passed += AssertEqual("SampleScene index", 0, sampleIndex, ref failed);

        passed += AssertEqual("Level01 index", 1,
            database.GetIndexByScene(GameConstants.SceneNames.Level01), ref failed);

        passed += AssertFalse("IsLastLevel(0)", database.IsLastLevel(0), ref failed);
        passed += AssertTrue("IsLastLevel(2)", database.IsLastLevel(2), ref failed);

        passed += AssertEqual("ResolveNext from index 0", GameConstants.SceneNames.Level01,
            database.ResolveNextSceneName(0), ref failed);

        passed += AssertEqual("ResolveNext from last level", GameConstants.SceneNames.MainMenu,
            database.ResolveNextSceneName(2), ref failed);

        passed += AssertEqual("ResolveNextByCurrentScene SampleScene", GameConstants.SceneNames.Level01,
            database.ResolveNextSceneNameByCurrentScene(GameConstants.SceneNames.SampleScene), ref failed);

        LevelEntry nextFromSample = database.GetNextLevel(0);
        passed += AssertNotNull("GetNextLevel(0)", nextFromSample, ref failed);
        if (nextFromSample != null)
        {
            passed += AssertEqual("GetNextLevel(0).sceneName", GameConstants.SceneNames.Level01,
                nextFromSample.sceneName, ref failed);
        }

        passed += AssertNull("GetNextLevel(last)", database.GetNextLevel(2), ref failed);

        passed += AssertEqual("Unknown scene -> MainMenu", GameConstants.SceneNames.MainMenu,
            database.ResolveNextSceneNameByCurrentScene("UnknownScene"), ref failed);

        if (failed == 0)
        {
            Debug.Log($"[LevelDatabaseTest] 全部通过（{passed} 项）。");
        }
        else
        {
            Debug.LogError($"[LevelDatabaseTest] 失败 {failed} 项，通过 {passed} 项。");
        }
    }

    private static int AssertEqual(string name, int expected, int actual, ref int failed)
    {
        if (expected == actual)
        {
            return 1;
        }

        failed++;
        Debug.LogError($"[LevelDatabaseTest] {name}: 期望 {expected}，实际 {actual}");
        return 0;
    }

    private static int AssertEqual(string name, string expected, string actual, ref int failed)
    {
        if (expected == actual)
        {
            return 1;
        }

        failed++;
        Debug.LogError($"[LevelDatabaseTest] {name}: 期望 '{expected}'，实际 '{actual}'");
        return 0;
    }

    private static int AssertTrue(string name, bool value, ref int failed)
    {
        if (value)
        {
            return 1;
        }

        failed++;
        Debug.LogError($"[LevelDatabaseTest] {name}: 期望 true，实际 false");
        return 0;
    }

    private static int AssertFalse(string name, bool value, ref int failed)
    {
        if (!value)
        {
            return 1;
        }

        failed++;
        Debug.LogError($"[LevelDatabaseTest] {name}: 期望 false，实际 true");
        return 0;
    }

    private static int AssertNotNull(string name, object value, ref int failed)
    {
        if (value != null)
        {
            return 1;
        }

        failed++;
        Debug.LogError($"[LevelDatabaseTest] {name}: 期望非 null");
        return 0;
    }

    private static int AssertNull(string name, object value, ref int failed)
    {
        if (value == null)
        {
            return 1;
        }

        failed++;
        Debug.LogError($"[LevelDatabaseTest] {name}: 期望 null");
        return 0;
    }
}
#endif
