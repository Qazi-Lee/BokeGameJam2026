using System;
using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 主界面逻辑：存档、场景切换、弹窗调度。UI 引用由 MainMenuView 等在 Inspector 拖入。
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("配置")]
    [SerializeField] private LevelDatabaseSO levelDatabase;

    [Header("视图引用")]
    [SerializeField] private MainMenuView menuView;
    [SerializeField] private OverwriteSaveDialogView overwriteDialogView;

    public event Action OnLevelAchievementRequested;
    public event Action OnCreditsRequested;
    public event Action OnRulesRequested;
    public event Action OnSettingsRequested;

    private void Awake()
    {
        EnsureLevelDatabase();
        EnsureEventSystem();
        EnsureManagers();
        BindViews();
        RefreshContinueButton();
    }

    private void OnEnable()
    {
        RefreshContinueButton();
        SubscribeOverwriteDialog();
    }

    private void OnDisable()
    {
        UnsubscribeOverwriteDialog();
    }

    public void OnContinueClicked()
    {
        if (SaveManager.Instance == null || !SaveManager.Instance.CanContinue)
        {
            return;
        }

        if (!SaveManager.Instance.TryGetContinueLevel(out int levelIndex))
        {
            return;
        }

        LoadLevelFromMenu(levelIndex);
    }

    public void OnNewGameClicked()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.HasSave)
        {
            if (overwriteDialogView != null)
            {
                overwriteDialogView.ShowForNewGame();
            }
            else
            {
                Debug.LogWarning("[MainMenuController] 未配置覆盖存档弹窗，将直接开始新游戏。");
                StartNewGameAndLoadLevel(0);
            }

            return;
        }

        StartNewGameAndLoadLevel(0);
    }

    public void OnLevelAchievementClicked()
    {
        if (OnLevelAchievementRequested != null)
        {
            OnLevelAchievementRequested.Invoke();
            return;
        }

        Debug.Log("[MainMenuController] 关卡成就（Phase 4 接线）");
    }

    public void OnCreditsClicked()
    {
        if (OnCreditsRequested != null)
        {
            OnCreditsRequested.Invoke();
            return;
        }

        Debug.Log("[MainMenuController] 致谢名单（Phase 4 接线）");
    }

    public void OnRulesClicked()
    {
        if (OnRulesRequested != null)
        {
            OnRulesRequested.Invoke();
            return;
        }

        Debug.Log("[MainMenuController] 规则（Phase 4 接线）");
    }

    public void OnSettingsClicked()
    {
        if (OnSettingsRequested != null)
        {
            OnSettingsRequested.Invoke();
            return;
        }

        Debug.Log("[MainMenuController] 设置（Phase 4 接线）");
    }

    /// <summary>供关卡成就页选关时调用：有存档则弹覆盖确认。</summary>
    public void RequestEnterLevel(int levelIndex)
    {
        if (SaveManager.Instance != null && SaveManager.Instance.HasSave && overwriteDialogView != null)
        {
            overwriteDialogView.ShowForLevelSelect(levelIndex);
            return;
        }

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SetCurrentLevel(levelIndex);
        }

        LoadLevelFromMenu(levelIndex);
    }

    public void RefreshContinueButton()
    {
        if (menuView == null)
        {
            return;
        }

        bool canContinue = SaveManager.Instance != null && SaveManager.Instance.CanContinue;
        menuView.SetContinueEnabled(canContinue);
    }

    private void StartNewGameAndLoadLevel(int levelIndex)
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.BeginNewGame();
        }

        LoadLevelFromMenu(levelIndex);
    }

    private void LoadLevelFromMenu(int levelIndex)
    {
        if (SceneFlowManager.Instance == null)
        {
            Debug.LogError("[MainMenuController] SceneFlowManager 不存在，无法加载关卡。");
            return;
        }

        string sceneName = SaveManager.Instance != null
            ? SaveManager.Instance.GetSceneNameByLevelIndex(levelIndex)
            : null;

        if (string.IsNullOrEmpty(sceneName) && levelDatabase != null)
        {
            LevelEntry entry = levelDatabase.GetLevel(levelIndex);
            sceneName = entry != null ? entry.sceneName : null;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"[MainMenuController] 无法解析关卡场景：index={levelIndex}");
            return;
        }

        SceneFlowManager.Instance.LoadScene(sceneName, TransitionMode.SimpleFade);
    }

    private void BindViews()
    {
        if (menuView == null)
        {
            menuView = FindObjectOfType<MainMenuView>();
        }

        if (overwriteDialogView == null)
        {
            overwriteDialogView = FindObjectOfType<OverwriteSaveDialogView>();
        }

        if (menuView != null)
        {
            menuView.Bind(this);
            menuView.InitializeCarousel(levelDatabase);
            menuView.ValidateReferences();
        }
        else
        {
            Debug.LogWarning(
                "[MainMenuController] 未找到 MainMenuView，请在场景中挂载并拖入按钮引用。",
                this);
        }
    }

    private void SubscribeOverwriteDialog()
    {
        if (overwriteDialogView == null)
        {
            return;
        }

        overwriteDialogView.OnNewGameConfirmed += OnOverwriteNewGameConfirmed;
        overwriteDialogView.OnLevelSelectConfirmed += OnOverwriteLevelSelectConfirmed;
    }

    private void UnsubscribeOverwriteDialog()
    {
        if (overwriteDialogView == null)
        {
            return;
        }

        overwriteDialogView.OnNewGameConfirmed -= OnOverwriteNewGameConfirmed;
        overwriteDialogView.OnLevelSelectConfirmed -= OnOverwriteLevelSelectConfirmed;
    }

    private void OnOverwriteNewGameConfirmed()
    {
        StartNewGameAndLoadLevel(0);
    }

    private void OnOverwriteLevelSelectConfirmed(int levelIndex)
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SetCurrentLevel(levelIndex);
        }

        LoadLevelFromMenu(levelIndex);
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private void EnsureManagers()
    {
        SaveManager saveManager = SaveManager.Instance ?? FindObjectOfType<SaveManager>();
        if (saveManager == null)
        {
            GameObject saveObject = new GameObject("SaveManager");
            saveManager = saveObject.AddComponent<SaveManager>();
        }

        SceneFlowManager sceneFlowManager = SceneFlowManager.Instance ?? FindObjectOfType<SceneFlowManager>();
        if (sceneFlowManager == null)
        {
            GameObject flowObject = new GameObject("SceneFlowManager");
            sceneFlowManager = flowObject.AddComponent<SceneFlowManager>();
        }

        if (levelDatabase != null)
        {
            saveManager.SetLevelDatabase(levelDatabase);
            sceneFlowManager.SetLevelDatabase(levelDatabase);
        }

#if UNITY_EDITOR
        AssignDatabaseIfMissing(saveManager);
        AssignDatabaseIfMissing(sceneFlowManager);
#endif
    }

    private void EnsureLevelDatabase()
    {
#if UNITY_EDITOR
        if (levelDatabase == null)
        {
            levelDatabase = AssetDatabase.LoadAssetAtPath<LevelDatabaseSO>(
                "Assets/_Game/Data/ScriptableObjects/LevelDatabase.asset");
        }
#endif
    }

    private void AssignDatabaseIfMissing(MonoBehaviour manager)
    {
        if (manager == null || levelDatabase == null)
        {
            return;
        }

        if (manager is SaveManager saveManager)
        {
            saveManager.SetLevelDatabase(levelDatabase);
            return;
        }

        if (manager is SceneFlowManager sceneFlowManager)
        {
            sceneFlowManager.SetLevelDatabase(levelDatabase);
        }

        SerializedObject serializedObject = new SerializedObject(manager);
        SerializedProperty property = serializedObject.FindProperty("levelDatabase");
        if (property != null && property.objectReferenceValue == null)
        {
            property.objectReferenceValue = levelDatabase;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
