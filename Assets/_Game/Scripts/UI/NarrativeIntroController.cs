using System.Collections;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 叙事介绍控制器：按页索引从独立预制体懒加载面板，延迟显示继续提示并等待全屏点击。
/// 显示时额外叠一层全屏背景（+半透明遮罩），sortingOrder 低于面板，不改动预制体内部。
/// 挂于 SceneFlowManager 节点。
/// </summary>
public class NarrativeIntroController : MonoBehaviour
{
    public const int NewGamePageIndex = 0;
    public const int FirstLevelPageIndex = 1;
    public const int PageCount = 5;

    private static readonly string[] DefaultPageBackgroundPaths =
    {
        null, // Panel0 = 新游戏，保持纯黑
        "Assets/_Game/Art/Sprites/Level1BGD.png",
        "Assets/_Game/Art/Sprites/Level2BGD.png",
        "Assets/_Game/Art/Sprites/Level3BGD.png",
        "Assets/_Game/Art/Sprites/level4BGD.jpg",
    };

    private static readonly string[] DefaultPanelPrefabPaths =
    {
        "Assets/_Game/Prefabs/UI/PanelsSum/Panel0.prefab",
        "Assets/_Game/Prefabs/UI/PanelsSum/Panel1.prefab",
        "Assets/_Game/Prefabs/UI/PanelsSum/Panel2.prefab",
        "Assets/_Game/Prefabs/UI/PanelsSum/Panel3.prefab",
        "Assets/_Game/Prefabs/UI/PanelsSum/Panel4.prefab"
    };

    [Header("独立面板预制体（Panel0=新游戏，Panel1~4=各关）")]
    [SerializeField] private GameObject[] panelPrefabs = new GameObject[PageCount];

    [Header("底层背景（对应 Panel0~4；留空则 Panel1~4 自动加载默认 BGD，Panel0/缺图用纯黑；已手动指定的槽位不会被覆盖）")]
    [SerializeField] private Sprite[] pageBackgrounds = new Sprite[PageCount];
    [SerializeField] [Range(0f, 1f)] private float backdropMaskAlpha = 0.35f;

    [Header("交互")]
    [SerializeField] private float closeTipDelay = 1.5f;
    [SerializeField] private int canvasSortOrder = 150;

    private readonly LevelIntroductionView[] panelInstances = new LevelIntroductionView[PageCount];
    private int activePageIndex = -1;

    private GameObject backdropRoot;
    private Image backdropBackgroundImage;
    private Image backdropMaskImage;
    private SceneTransitionUI transitionUI;
    private bool pageBackgroundsEnsured;

    public bool IsShowing { get; private set; }

    /// <summary>由 SceneFlowManager 注入预制体与背景，Build 必须走此路径。</summary>
    public void Configure(GameObject[] prefabs, Sprite[] backgrounds)
    {
        if (prefabs != null)
        {
            EnsurePanelPrefabArraySize();
            int copyCount = Mathf.Min(prefabs.Length, panelPrefabs.Length);
            for (int i = 0; i < copyCount; i++)
            {
                if (prefabs[i] != null)
                {
                    panelPrefabs[i] = prefabs[i];
                }
            }
        }

        if (backgrounds != null)
        {
            EnsurePageBackgroundArraySize();
            int copyCount = Mathf.Min(backgrounds.Length, pageBackgrounds.Length);
            for (int i = 0; i < copyCount; i++)
            {
                if (backgrounds[i] != null)
                {
                    pageBackgrounds[i] = backgrounds[i];
                }
            }
        }
    }

    private void Awake()
    {
        EnsurePrefabReferences();
        transitionUI = GetComponent<SceneTransitionUI>();
    }

    public IEnumerator ShowNewGameIntroAndWait()
    {
        yield return ShowPageAndWait(NewGamePageIndex);
    }

    public IEnumerator ShowLevelIntroAndWait(int levelIndex)
    {
        int pageIndex = FirstLevelPageIndex + levelIndex;
        if (!HasPanelPrefab(pageIndex))
        {
            Debug.LogWarning(
                $"[NarrativeIntroController] 关卡 {levelIndex} 无对应介绍预制体（page {pageIndex}），已跳过。");
            yield break;
        }

        yield return ShowPageAndWait(pageIndex);
    }

    public IEnumerator ShowPageAndWait(int pageIndex)
    {
        LevelIntroductionView panel = EnsurePanelInstance(pageIndex);
        if (panel == null)
        {
            Debug.LogWarning($"[NarrativeIntroController] 页面 {pageIndex} 无可用介绍面板，已跳过。");
            yield break;
        }

        HideActivePanel();
        IsShowing = true;
        activePageIndex = pageIndex;

        // 转场黑幕 sortingOrder=200，须先收起，否则介绍被挡住。
        transitionUI?.SuppressOverlayForContent();
        ShowBackdrop(pageIndex);
        panel.Show();

        if (closeTipDelay > 0f)
        {
            yield return WaitUnscaled(closeTipDelay);
        }

        panel.SetCloseTipVisible(true);
        yield return WaitForScreenClick();

        // 关闭介绍前先黑幕接住，避免闪回主菜单/关卡。
        transitionUI?.HoldBlack();
        panel.Hide();
        HideBackdrop();
        activePageIndex = -1;
        IsShowing = false;
    }

    private void ShowBackdrop(int pageIndex)
    {
        EnsureBackdropBuilt();

        Sprite background = ResolvePageBackground(pageIndex);
        if (background != null)
        {
            backdropBackgroundImage.sprite = background;
            backdropBackgroundImage.color = Color.white;
            backdropBackgroundImage.enabled = true;
        }
        else
        {
            backdropBackgroundImage.sprite = null;
            backdropBackgroundImage.color = Color.black;
            backdropBackgroundImage.enabled = true;
        }

        Color maskColor = Color.black;
        maskColor.a = backdropMaskAlpha;
        backdropMaskImage.color = maskColor;
        backdropMaskImage.gameObject.SetActive(backdropMaskAlpha > 0f);

        backdropRoot.SetActive(true);
    }

    private void HideBackdrop()
    {
        if (backdropRoot != null)
        {
            backdropRoot.SetActive(false);
        }
    }

    private Sprite ResolvePageBackground(int pageIndex)
    {
        if (pageIndex < 0 || pageIndex >= PageCount)
        {
            return null;
        }

#if UNITY_EDITOR
        EnsurePageBackgrounds();
#endif

        if (pageBackgrounds != null &&
            pageIndex < pageBackgrounds.Length &&
            pageBackgrounds[pageIndex] != null)
        {
            return pageBackgrounds[pageIndex];
        }

        return null;
    }

    private void EnsurePrefabReferences()
    {
#if UNITY_EDITOR
        EnsureEditorPrefabReferences();
        EnsurePageBackgrounds();
#endif

        for (int i = 0; i < PageCount; i++)
        {
            if (panelPrefabs[i] != null)
            {
                continue;
            }

            Debug.LogWarning(
                $"[NarrativeIntroController] 页面 {i} 未配置介绍预制体，Build 中请在 SceneFlowManager 指定 introPanelPrefabs。",
                this);
        }
    }

    private void EnsureBackdropBuilt()
    {
        if (backdropRoot != null)
        {
            Canvas existingCanvas = backdropRoot.GetComponent<Canvas>();
            if (existingCanvas != null)
            {
                existingCanvas.sortingOrder = canvasSortOrder - 2;
            }

            return;
        }

        backdropRoot = new GameObject("IntroBackdropCanvas");
        backdropRoot.transform.SetParent(transform, false);

        Canvas canvas = backdropRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = canvasSortOrder - 2;

        CanvasScaler scaler = backdropRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        backdropRoot.AddComponent<GraphicRaycaster>();

        backdropBackgroundImage = CreateStretchImage(backdropRoot.transform, "BackdropBackground", Color.black);
        backdropMaskImage = CreateStretchImage(backdropRoot.transform, "BackdropMask", new Color(0f, 0f, 0f, backdropMaskAlpha));

        backdropRoot.SetActive(false);
    }

    private static Image CreateStretchImage(Transform parent, string name, Color color)
    {
        GameObject imageObject = new GameObject(name);
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        image.preserveAspect = false;

        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return image;
    }

    private void HideActivePanel()
    {
        if (activePageIndex < 0)
        {
            return;
        }

        LevelIntroductionView panel = panelInstances[activePageIndex];
        if (panel != null)
        {
            panel.Hide();
        }

        activePageIndex = -1;
    }

    private bool HasPanelPrefab(int pageIndex)
    {
        return pageIndex >= 0 && pageIndex < PageCount && panelPrefabs[pageIndex] != null;
    }

    private LevelIntroductionView EnsurePanelInstance(int pageIndex)
    {
        if (!HasPanelPrefab(pageIndex))
        {
            EnsurePrefabReferences();
        }

        if (!HasPanelPrefab(pageIndex))
        {
            return null;
        }

        if (panelInstances[pageIndex] != null)
        {
            return panelInstances[pageIndex];
        }

        GameObject panelObject = Instantiate(panelPrefabs[pageIndex], transform);
        panelObject.name = panelPrefabs[pageIndex].name;

        LevelIntroductionView panel = panelObject.GetComponent<LevelIntroductionView>();
        if (panel == null)
        {
            panel = panelObject.AddComponent<LevelIntroductionView>();
        }

        ConfigureCanvas(panelObject);
        NormalizeIntroPanelHierarchy(panelObject);
        panel.Hide();
        panelInstances[pageIndex] = panel;
        return panel;
    }

    /// <summary>
    /// mask 上的 World Space Canvas 嵌在 Screen Space Overlay 下，Editor 有时能显示但 Build 会整页不可见。
    /// </summary>
    private static void NormalizeIntroPanelHierarchy(GameObject panelObject)
    {
        Transform mask = panelObject.transform.Find("mask");
        if (mask == null)
        {
            return;
        }

        Canvas maskCanvas = mask.GetComponent<Canvas>();
        if (maskCanvas != null)
        {
            Object.Destroy(maskCanvas);
        }

        GraphicRaycaster maskRaycaster = mask.GetComponent<GraphicRaycaster>();
        if (maskRaycaster != null)
        {
            Object.Destroy(maskRaycaster);
        }

        Image maskImage = mask.GetComponent<Image>();
        if (maskImage != null)
        {
            maskImage.raycastTarget = false;
        }
    }

    private void ConfigureCanvas(GameObject panelObject)
    {
        Canvas canvas = panelObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = canvasSortOrder;

        if (panelObject.GetComponent<GraphicRaycaster>() == null)
        {
            panelObject.AddComponent<GraphicRaycaster>();
        }
    }

    private static IEnumerator WaitUnscaled(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private static IEnumerator WaitForScreenClick()
    {
        while (!WasScreenClickedThisFrame())
        {
            yield return null;
        }
    }

    private static bool WasScreenClickedThisFrame()
    {
        if (Input.GetMouseButtonDown(0))
        {
            return true;
        }

        return Input.touchCount > 0 &&
               Input.GetTouch(0).phase == TouchPhase.Began;
    }

    private void EnsurePanelPrefabArraySize()
    {
        if (panelPrefabs != null && panelPrefabs.Length >= PageCount)
        {
            return;
        }

        GameObject[] resized = new GameObject[PageCount];
        if (panelPrefabs != null)
        {
            int copyCount = Mathf.Min(panelPrefabs.Length, PageCount);
            for (int i = 0; i < copyCount; i++)
            {
                resized[i] = panelPrefabs[i];
            }
        }

        panelPrefabs = resized;
    }

    private void EnsurePageBackgroundArraySize()
    {
        if (pageBackgrounds != null && pageBackgrounds.Length >= PageCount)
        {
            return;
        }

        Sprite[] resized = new Sprite[PageCount];
        if (pageBackgrounds != null)
        {
            int copyCount = Mathf.Min(pageBackgrounds.Length, PageCount);
            for (int i = 0; i < copyCount; i++)
            {
                resized[i] = pageBackgrounds[i];
            }
        }

        pageBackgrounds = resized;
    }

#if UNITY_EDITOR
    private void EnsureEditorPrefabReferences()
    {
        EnsurePanelPrefabArraySize();

        for (int i = 0; i < PageCount; i++)
        {
            if (panelPrefabs[i] != null)
            {
                continue;
            }

            panelPrefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultPanelPrefabPaths[i]);
        }
    }

    private void EnsurePageBackgrounds()
    {
        if (pageBackgroundsEnsured)
        {
            return;
        }

        pageBackgroundsEnsured = true;
        EnsurePageBackgroundArraySize();

        for (int i = FirstLevelPageIndex; i < PageCount; i++)
        {
            if (pageBackgrounds[i] != null)
            {
                continue;
            }

            if (i >= DefaultPageBackgroundPaths.Length ||
                string.IsNullOrEmpty(DefaultPageBackgroundPaths[i]))
            {
                continue;
            }

            pageBackgrounds[i] = AssetDatabase.LoadAssetAtPath<Sprite>(DefaultPageBackgroundPaths[i]);
        }
    }
#endif
}
