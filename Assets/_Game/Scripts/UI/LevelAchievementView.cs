using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 关卡成就面板 View：读取解锁状态与关卡名，选关时通知 Controller。
/// 星数显示：关闭 StarDark 下未获得星数对应的亮星星物体，获得的几颗保持显示。
/// </summary>
public class LevelAchievementView : MonoBehaviour
{
    private const int LevelCount = 4;
    private const int StarsPerLevel = 3;

    [Header("面板")]
    [SerializeField] private GameObject panelRoot;

    [Header("列表")]
    [SerializeField] private LevelAchievementItemView[] levelItems;

    [Header("按钮")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button resetProgressButton;

    [Header("星级显示（扩展 Phase 前保持关闭）")]
    [SerializeField] private bool showStars;

    [Header("亮星物体（可选，留空则自动绑定 StarDark 下亮星星子节点）")]
    [SerializeField] private GameObject[] starBrightObjects = new GameObject[LevelCount * StarsPerLevel];

    [Header("关卡插画（images 下 Image_1~4，留空则自动绑定）")]
    [SerializeField] private Image[] levelImages = new Image[LevelCount];

    [SerializeField] private Color lockedLevelImageColor = new Color(0.45f, 0.45f, 0.45f, 1f);

    [Header("显示层级（嵌套 Canvas 时确保盖在主菜单之上）")]
    [SerializeField] private int panelSortingOrder = 100;

    /// <summary>玩家选择进入某关时触发。</summary>
    public event Action<int> OnLevelEnterRequested;

    private bool isInitialized;
    private bool nestedCanvasNormalized;

    /// <summary>打开面板并刷新关卡列表。</summary>
    public void Show(LevelDatabaseSO levelDatabase, SaveManager saveManager)
    {
        EnsureInitialized();
        Refresh(levelDatabase, saveManager);

        if (panelRoot != null)
        {
            EnsurePanelCoversScreen();
            EnsurePanelCanvasOnTop();
            panelRoot.transform.SetAsLastSibling();
            panelRoot.SetActive(true);
        }
    }

    public void Hide()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void EnsureInitialized()
    {
        if (isInitialized)
        {
            return;
        }

        isInitialized = true;
        ResolveReferences();
        EnsureGraphicRaycaster();
        EnsureNestedCanvasCompatibility();
        EnsureStarBrightBindings();
        EnsureLevelImageBindings();
        EnsureStarFrameLayerVisible();
        BindLevelItems();

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnCloseClicked);
            closeButton.onClick.AddListener(OnCloseClicked);
        }

        if (resetProgressButton != null)
        {
            resetProgressButton.gameObject.SetActive(false);
            resetProgressButton.interactable = false;
        }
    }

    private void ResolveReferences()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        if (closeButton == null)
        {
            Transform backButton = FindChildRecursive(transform, "btn_back");
            if (backButton != null)
            {
                closeButton = backButton.GetComponent<Button>();
            }
        }
    }

    private void EnsureGraphicRaycaster()
    {
        if (panelRoot == null)
        {
            return;
        }

        Canvas ownCanvas = panelRoot.GetComponent<Canvas>();
        if (ownCanvas != null)
        {
            if (panelRoot.GetComponent<GraphicRaycaster>() == null)
            {
                panelRoot.AddComponent<GraphicRaycaster>();
            }

            return;
        }

        Canvas parentCanvas = panelRoot.GetComponentInParent<Canvas>();
        if (parentCanvas != null && parentCanvas.GetComponent<GraphicRaycaster>() == null)
        {
            parentCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    /// <summary>
    /// 嵌在主菜单 Canvas 下时，子 Canvas 在 Build 中常导致整页不渲染；改由父 Canvas 统一绘制。
    /// </summary>
    private void EnsureNestedCanvasCompatibility()
    {
        if (nestedCanvasNormalized || panelRoot == null || panelRoot.transform.parent == null)
        {
            return;
        }

        if (panelRoot.GetComponentInParent<Canvas>() == null)
        {
            return;
        }

        UiCanvasComponentUtility.RemoveCanvasStack(panelRoot);
        nestedCanvasNormalized = true;
    }

    private void EnsurePanelCoversScreen()
    {
        if (panelRoot == null)
        {
            return;
        }

        RectTransform rect = panelRoot.transform as RectTransform;
        if (rect != null)
        {
            rect.localScale = Vector3.one;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        Image background = panelRoot.GetComponent<Image>();
        if (background != null)
        {
            background.enabled = true;
            background.raycastTarget = true;
        }
    }

    private void EnsurePanelCanvasOnTop()
    {
        if (panelRoot == null)
        {
            return;
        }

        Canvas canvas = panelRoot.GetComponent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        canvas.overrideSorting = true;
        canvas.sortingOrder = panelSortingOrder;
    }

    private void OnCloseClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        Hide();
    }

    /// <summary>根据存档与关卡表刷新各行状态。</summary>
    public void Refresh(LevelDatabaseSO levelDatabase, SaveManager saveManager)
    {
        RefreshStarDisplay(saveManager);
        RefreshLevelImageDisplay(saveManager);

        if (levelItems == null || levelItems.Length == 0)
        {
            return;
        }

        for (int i = 0; i < levelItems.Length; i++)
        {
            LevelAchievementItemView item = levelItems[i];
            if (item == null)
            {
                continue;
            }

            item.SetLevelIndex(i);

            bool unlocked = saveManager != null && saveManager.IsLevelUnlocked(i);
            string displayName = BuildDisplayName(levelDatabase, i);
            int starCount = saveManager != null ? saveManager.GetStarCount(i) : 0;

            item.Apply(unlocked, displayName, starCount, showStars);
        }
    }

    private void RefreshStarDisplay(SaveManager saveManager)
    {
        EnsureStarFrameLayerVisible();
        EnsureStarBrightBindings();

        if (!HasConfiguredStarBrightObjects())
        {
            Debug.LogWarning("[LevelAchievementView] 未找到 StarDark 亮星引用，无法刷新星数。", this);
            return;
        }

        for (int level = 0; level < LevelCount; level++)
        {
            int starCount = saveManager != null ? saveManager.GetStarCount(level) : 0;
            starCount = Mathf.Clamp(starCount, 0, StarsPerLevel);

            for (int star = 0; star < StarsPerLevel; star++)
            {
                int index = level * StarsPerLevel + star;
                GameObject brightStar = starBrightObjects[index];
                if (brightStar == null)
                {
                    continue;
                }

                // 存档 N 星：仅前 N 个亮星星保持显示，其余关闭
                brightStar.SetActive(star < starCount);
            }
        }
    }

    private void RefreshLevelImageDisplay(SaveManager saveManager)
    {
        EnsureLevelImageBindings();

        if (levelImages == null || levelImages.Length == 0)
        {
            return;
        }

        for (int i = 0; i < levelImages.Length; i++)
        {
            Image image = levelImages[i];
            if (image == null)
            {
                continue;
            }

            bool unlocked = saveManager != null && saveManager.IsLevelUnlocked(i);
            image.color = unlocked ? Color.white : lockedLevelImageColor;
        }
    }

    private void EnsureLevelImageBindings()
    {
        if (HasConfiguredLevelImages())
        {
            return;
        }

        Transform imagesRoot = FindChildRecursive(transform, "images");
        if (imagesRoot == null)
        {
            Debug.LogWarning("[LevelAchievementView] 未找到 images 节点。", this);
            return;
        }

        levelImages = new Image[LevelCount];
        for (int i = 0; i < LevelCount; i++)
        {
            Transform child = imagesRoot.Find($"Image_{i + 1}");
            if (child == null)
            {
                Debug.LogWarning($"[LevelAchievementView] 未找到 Image_{i + 1}。", this);
                continue;
            }

            levelImages[i] = child.GetComponent<Image>();
        }
    }

    private bool HasConfiguredLevelImages()
    {
        if (levelImages == null || levelImages.Length < LevelCount)
        {
            return false;
        }

        for (int i = 0; i < LevelCount; i++)
        {
            if (levelImages[i] == null)
            {
                return false;
            }
        }

        return true;
    }

    private void EnsureStarBrightBindings()
    {
        if (HasConfiguredStarBrightObjects())
        {
            return;
        }

        Transform starDarkRoot = FindChildRecursive(transform, "StarDark");
        if (starDarkRoot == null)
        {
            Debug.LogWarning("[LevelAchievementView] 未找到 StarDark 节点。", this);
            return;
        }

        var collected = new System.Collections.Generic.List<GameObject>();
        for (int i = 0; i < starDarkRoot.childCount; i++)
        {
            Transform child = starDarkRoot.GetChild(i);
            Image image = child.GetComponent<Image>();
            if (image != null && IsBrightStarSprite(image.sprite))
            {
                collected.Add(child.gameObject);
            }
        }

        if (collected.Count < LevelCount * StarsPerLevel)
        {
            collected.Clear();
            for (int i = 0; i < starDarkRoot.childCount; i++)
            {
                collected.Add(starDarkRoot.GetChild(i).gameObject);
            }
        }

        if (collected.Count < LevelCount * StarsPerLevel)
        {
            Debug.LogWarning(
                $"[LevelAchievementView] StarDark 亮星节点不足 {LevelCount * StarsPerLevel} 个（当前 {collected.Count}）。",
                this);
            return;
        }

        starBrightObjects = new GameObject[LevelCount * StarsPerLevel];
        for (int i = 0; i < starBrightObjects.Length; i++)
        {
            starBrightObjects[i] = collected[i];
        }
    }

    private static bool IsBrightStarSprite(Sprite sprite)
    {
        if (sprite == null)
        {
            return false;
        }

        return sprite.name.Contains("亮星星") || sprite.name.Contains("亮星");
    }

    private void EnsureStarFrameLayerVisible()
    {
        Transform starFrame = FindChildRecursive(transform, "Star");
        if (starFrame != null)
        {
            starFrame.gameObject.SetActive(true);
        }
    }

    private bool HasConfiguredStarBrightObjects()
    {
        if (starBrightObjects == null || starBrightObjects.Length < LevelCount * StarsPerLevel)
        {
            return false;
        }

        for (int i = 0; i < LevelCount * StarsPerLevel; i++)
        {
            if (starBrightObjects[i] == null)
            {
                return false;
            }
        }

        return true;
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        if (parent.name == childName)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private void BindLevelItems()
    {
        if (levelItems == null)
        {
            return;
        }

        for (int i = 0; i < levelItems.Length; i++)
        {
            LevelAchievementItemView item = levelItems[i];
            if (item == null)
            {
                continue;
            }

            item.SetLevelIndex(i);
            item.OnEnterRequested -= HandleLevelEnterRequested;
            item.OnEnterRequested += HandleLevelEnterRequested;
        }
    }

    private void HandleLevelEnterRequested(int levelIndex)
    {
        OnLevelEnterRequested?.Invoke(levelIndex);
    }

    private static string BuildDisplayName(LevelDatabaseSO levelDatabase, int levelIndex)
    {
        string levelNumber = $"第{levelIndex + 1}关";
        if (levelDatabase == null)
        {
            return levelNumber;
        }

        LevelEntry entry = levelDatabase.GetLevel(levelIndex);
        if (entry == null || string.IsNullOrWhiteSpace(entry.displayName))
        {
            return levelNumber;
        }

        return $"{levelNumber}·{entry.displayName}";
    }

#if UNITY_EDITOR
    [ContextMenu("Validate References")]
    private void ValidateReferences()
    {
        ResolveReferences();
        EnsureStarBrightBindings();
        EnsureLevelImageBindings();

        if (panelRoot == null || closeButton == null)
        {
            Debug.LogError("[LevelAchievementView] 引用未配齐，请拖入 panelRoot 与 closeButton。", this);
            return;
        }

        if (!HasConfiguredStarBrightObjects() && (levelItems == null || levelItems.Length == 0))
        {
            Debug.LogWarning(
                "[LevelAchievementView] 未配置 starBrightObjects 或 levelItems，星数/列表将无法刷新。",
                this);
        }
    }
#endif
}
