using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 按 Unity 组件依赖顺序安全移除 Canvas 栈：GraphicRaycaster → CanvasScaler → Canvas。
/// </summary>
public static class UiCanvasComponentUtility
{
    public static void RemoveCanvasStack(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        GraphicRaycaster raycaster = target.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
        {
            Object.Destroy(raycaster);
        }

        CanvasScaler scaler = target.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            Object.Destroy(scaler);
        }

        Canvas canvas = target.GetComponent<Canvas>();
        if (canvas != null)
        {
            Object.Destroy(canvas);
        }
    }

    /// <summary>移除 root 及其子层级上所有带 Canvas 的物体上的 Canvas 栈。</summary>
    public static void RemoveCanvasStacksUnder(Transform root)
    {
        if (root == null)
        {
            return;
        }

        Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas != null)
            {
                RemoveCanvasStack(canvas.gameObject);
            }
        }
    }

    /// <summary>禁用 mask 半透明层对射线的拦截（常见于 World Space Canvas 嵌套问题）。</summary>
    public static void DisableMaskRaycastBlocking(Transform mask)
    {
        if (mask == null)
        {
            return;
        }

        RemoveCanvasStack(mask.gameObject);

        Image image = mask.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = false;
        }
    }
}
