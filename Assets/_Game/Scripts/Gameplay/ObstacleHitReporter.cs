using UnityEngine;

/// <summary>
/// 非致死障碍：挂于带 Collider2D 的障碍物体，标记 Tag 并确保碰撞体；由 PlayerBody 触发扣星与音效。
/// </summary>
[DisallowMultipleComponent]
public class ObstacleHitReporter : MonoBehaviour
{
    private static float lastSoundTime;
    private const float SoundCooldownSeconds = 0.25f;

    private void Awake()
    {
        gameObject.tag = GameConstants.Tags.Obstacle;
        EnsureCollider();
    }

    /// <summary>玩家碰到障碍时由 PlayerBody 调用。</summary>
    public static void HandlePlayerContact(Collider2D collider)
    {
        if (collider == null)
        {
            return;
        }

        if (!IsObstacleCollider(collider))
        {
            return;
        }

        if (!CanApplyGameplayPenalty())
        {
            return;
        }

        if (Time.time - lastSoundTime >= SoundCooldownSeconds)
        {
            lastSoundTime = Time.time;
            AudioManager.Instance?.PlayCollision();
        }

        LevelStarTracker.EnsureInstance();
        LevelStarTracker.Instance?.ReportObstacleHit();
    }

    private static bool IsObstacleCollider(Collider2D collider)
    {
        if (collider.CompareTag(GameConstants.Tags.Obstacle))
        {
            return true;
        }

        return collider.GetComponent<ObstacleHitReporter>() != null
            || collider.GetComponentInParent<ObstacleHitReporter>() != null;
    }

    private static bool CanApplyGameplayPenalty()
    {
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying)
        {
            return false;
        }

        if (SceneFlowManager.Instance != null && SceneFlowManager.Instance.IsLoading)
        {
            return false;
        }

        return true;
    }

    private void EnsureCollider()
    {
        if (GetComponent<Collider2D>() != null)
        {
            return;
        }

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            gameObject.AddComponent<PolygonCollider2D>();
        }
    }
}
