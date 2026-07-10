using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 关卡顶部栏：退出回主界面、当前关静音切换；由 SceneFlowManager 懒加载，仅关卡显示。
/// 不修改预制体布局，加载后仅绑定按钮与显隐。每关静音状态写入存档。
/// </summary>
public class TopBarController : MonoBehaviour
{
    [Header("退出")]
    [SerializeField] private Button backButton;

    [Header("声音")]
    [SerializeField] private Button soundOpenButton;
    [SerializeField] private Button soundCloseButton;

    public bool IsVisible => gameObject.activeSelf;

    private void Awake()
    {
        ResolveReferences();
        BindButtons();
        RefreshSoundButtons();
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
    }
#endif
}
