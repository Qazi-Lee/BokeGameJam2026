using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TextTipTrigger2D : MonoBehaviour
{
    [SerializeField] private List<int> textIndexes = new List<int>();
    [SerializeField] private float showTime = 2f;

    private bool hasTriggered;

    private void Awake()
    {
        Collider2D triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered)
        {
            return;
        }

        if (!other.CompareTag(GameConstants.Tags.Player))
        {
            return;
        }

        PlayerBody playerBody = other.GetComponent<PlayerBody>();
        if (playerBody == null)
        {
            return;
        }

        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying)
        {
            return;
        }

        if (TextTipUI.Instance == null || textIndexes == null || textIndexes.Count == 0)
        {
            return;
        }

        hasTriggered = true;
        TextTipUI.Instance.ShowText(textIndexes, showTime);
    }
}
