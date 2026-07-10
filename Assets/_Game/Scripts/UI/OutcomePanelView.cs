using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 单页胜负面板视图：管理显隐与继续/重试按钮引用。
/// 挂于 VictoryPanel_N / DefeatPanel_N 预制体根节点。
/// </summary>
public class OutcomePanelView : MonoBehaviour
{
    private const string VictoryButtonName = "btn_Goon";
    private const string DefeatButtonName = "btn_Restart";

    [Header("按钮（留空则按子节点名自动查找）")]
    [SerializeField] private Button actionButton;

    public Button ActionButton => actionButton;

    public bool IsVictoryPanel { get; private set; }

    private void Awake()
    {
        ResolveReferences();
        Hide();
    }

    public void Show()
    {
        ResolveReferences();
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void ResolveReferences()
    {
        if (actionButton != null)
        {
            return;
        }

        Transform victoryButton = FindChildRecursive(transform, VictoryButtonName);
        if (victoryButton != null)
        {
            actionButton = victoryButton.GetComponent<Button>();
            IsVictoryPanel = true;
            return;
        }

        Transform defeatButton = FindChildRecursive(transform, DefeatButtonName);
        if (defeatButton != null)
        {
            actionButton = defeatButton.GetComponent<Button>();
            IsVictoryPanel = false;
        }
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent.name == childName)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChildRecursive(parent.GetChild(i), childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
