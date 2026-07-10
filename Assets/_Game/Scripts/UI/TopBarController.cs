using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 关卡顶部栏：退出回主界面、当前关静音切换、局内星级显示；由 SceneFlowManager 懒加载，仅关卡显示。
/// 不修改预制体布局，加载后仅绑定按钮与显隐。每关静音状态写入存档。
/// </summary>
public class TopBarController : MonoBehaviour
{
    private const string FilledStarSpritePath = "Assets/_Game/Prefabs/UI/Sprites/亮星星.png";
    private const string EmptyStarSpritePath = "Assets/_Game/Prefabs/UI/Sprites/空星星槽.png";
    [Header("退出")]
    [SerializeField] private Button backButton;

    [Header("声音")]
    [SerializeField] private Button soundOpenButton;
    [SerializeField] private Button soundCloseButton;

    [Header("星级")]
    [SerializeField] private Image[] starImages;
    [SerializeField] private Sprite filledStarSprite;
    [SerializeField] private Sprite emptyStarSprite;

    public bool IsVisible => gameObject.activeSelf;

    private void Awake()
    {
        ResolveReferences();
        BindButtons();
        RefreshSoundButtons();
    }

    private void OnEnable()
    {
        LevelStarTracker.EnsureInstance();
        if (LevelStarTracker.Instance != null)
        {
            LevelStarTracker.Instance.StarsChanged += RefreshStars;
            RefreshStars(LevelStarTracker.Instance.CurrentStars);
        }
    }

    private void OnDisable()
    {
        if (LevelStarTracker.Instance != null)
        {
            LevelStarTracker.Instance.StarsChanged -= RefreshStars;
        }
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }

    /// <summary>显示或隐藏顶部栏（主界面隐藏，关卡显示）。</summary>
    public void SetVisible(bool visible)
    {
        if (gameObject.activeSelf == visible)
        {
            if (visible)
            {
                RefreshSoundButtons();
            }

            return;
        }

        gameObject.SetActive(visible);

        if (visible)
        {
            RefreshSoundButtons();
        }
    }

    private void BindButtons()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }
        else
        {
            Debug.LogError("[TopBarController] 未找到 btn_Back，请在 Inspector 拖入或确保子节点名为 btn_Back。", this);
        }

        if (soundOpenButton != null)
        {
            soundOpenButton.onClick.AddListener(OnSoundOpenClicked);
        }

        if (soundCloseButton != null)
        {
            soundCloseButton.onClick.AddListener(OnSoundCloseClicked);
        }
    }

    private void UnbindButtons()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnBackClicked);
        }

        if (soundOpenButton != null)
        {
            soundOpenButton.onClick.RemoveListener(OnSoundOpenClicked);
        }

        if (soundCloseButton != null)
        {
            soundCloseButton.onClick.RemoveListener(OnSoundCloseClicked);
        }
    }

    private void ResolveReferences()
    {
        if (backButton == null)
        {
            Transform backTransform = transform.Find("btn_Back");
            if (backTransform != null)
            {
                backButton = backTransform.GetComponent<Button>();
            }
        }

        if (soundOpenButton == null)
        {
            Transform openTransform = transform.Find("btn_SoundOpen");
            if (openTransform != null)
            {
                soundOpenButton = openTransform.GetComponent<Button>();
            }
        }

        if (soundCloseButton == null)
        {
            Transform closeTransform = transform.Find("btn_SoundClose");
            if (closeTransform != null)
            {
                soundCloseButton = closeTransform.GetComponent<Button>();
            }
        }

        ResolveStarReferences();
    }

    private void ResolveStarReferences()
    {
        if (starImages == null || starImages.Length == 0)
        {
            string[] starNames = { "img_Star1", "img_Star2", "img_Star3" };
            List<Image> resolvedStars = new List<Image>(starNames.Length);

            for (int i = 0; i < starNames.Length; i++)
            {
                Transform starTransform = transform.Find(starNames[i]);
                if (starTransform == null)
                {
                    continue;
                }

                Image starImage = starTransform.GetComponent<Image>();
                if (starImage != null)
                {
                    resolvedStars.Add(starImage);
                }
            }

            starImages = resolvedStars.ToArray();
        }

        if (filledStarSprite == null && starImages != null && starImages.Length > 0 && starImages[0] != null)
        {
            filledStarSprite = starImages[0].sprite;
        }

        EnsureStarSpritesAssigned();
    }

    private void EnsureStarSpritesAssigned()
    {
#if UNITY_EDITOR
        if (filledStarSprite == null)
        {
            filledStarSprite = AssetDatabase.LoadAssetAtPath<Sprite>(FilledStarSpritePath);
        }

        if (emptyStarSprite == null)
        {
            emptyStarSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EmptyStarSpritePath);
        }
#endif
    }

    /// <summary>刷新顶部栏三星显示（从左到右，扣星时右侧先变暗）。</summary>
    public void RefreshStars(int starCount)
    {
        ResolveStarReferences();

        if (starImages == null || starImages.Length == 0)
        {
            return;
        }

        Array.Sort(starImages, CompareStarHorizontalOrder);
        int clampedStars = Mathf.Clamp(starCount, 0, starImages.Length);

        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] == null)
            {
                continue;
            }

            bool filled = i < clampedStars;
            if (filled && filledStarSprite != null)
            {
                starImages[i].sprite = filledStarSprite;
                starImages[i].enabled = true;
            }
            else if (!filled && emptyStarSprite != null)
            {
                starImages[i].sprite = emptyStarSprite;
                starImages[i].enabled = true;
            }
            else
            {
                starImages[i].enabled = filled;
            }
        }
    }

    private static int CompareStarHorizontalOrder(Image left, Image right)
    {
        if (left == null || right == null)
        {
            return 0;
        }

        return left.rectTransform.anchoredPosition.x.CompareTo(right.rectTransform.anchoredPosition.x);
    }

    /// <summary>非静音：显示「声音开启」按钮；静音：显示「声音关闭」按钮。</summary>
    public void RefreshSoundButtons()
    {
        bool muted = ResolveMutedState();

        if (soundOpenButton != null)
        {
            soundOpenButton.gameObject.SetActive(!muted);
        }

        if (soundCloseButton != null)
        {
            soundCloseButton.gameObject.SetActive(muted);
        }
    }

    private static bool ResolveMutedState()
    {
        if (AudioManager.Instance != null)
        {
            return AudioManager.Instance.IsMuted;
        }

        if (SceneFlowManager.Instance != null && SaveManager.Instance != null)
        {
            int levelIndex = SceneFlowManager.Instance.CurrentLevelIndex;
            if (levelIndex >= 0)
            {
                return SaveManager.Instance.GetLevelMuted(levelIndex);
            }
        }

        return false;
    }

    private void OnBackClicked()
    {
        AudioManager.Instance?.PlayButtonClick();

        if (SceneFlowManager.Instance == null)
        {
            Debug.LogWarning("[TopBarController] SceneFlowManager 不存在，无法返回主界面。");
            return;
        }

        if (SceneFlowManager.Instance.IsLoading)
        {
            return;
        }

        SceneFlowManager.Instance.LoadMainMenu();
    }

    private void OnSoundOpenClicked()
    {
        AudioManager.Instance?.PlayButtonClick();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMuted(true);
        }

        RefreshSoundButtons();
    }

    private void OnSoundCloseClicked()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMuted(false);
            AudioManager.Instance.PlayButtonClick();
        }

        RefreshSoundButtons();
    }

#if UNITY_EDITOR
    [ContextMenu("Validate References")]
    private void ValidateReferences()
    {
        ResolveReferences();
        if (backButton == null)
        {
            Debug.LogError("[TopBarController] 引用未配齐，请拖入 backButton（btn_Back）。", this);
        }

        if (soundOpenButton == null || soundCloseButton == null)
        {
            Debug.LogError("[TopBarController] 声音按钮未配齐，请拖入 btn_SoundOpen / btn_SoundClose。", this);
        }

        if (starImages == null || starImages.Length == 0)
        {
            Debug.LogError("[TopBarController] 星级 Image 未配齐，请拖入 img_Star1~3。", this);
        }
    }
#endif
}
