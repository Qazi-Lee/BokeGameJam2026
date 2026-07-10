using UnityEngine;

/// <summary>
/// 出口门：玩家触碰后显示胜利面板，确认后加载下一关；最后一关则回主菜单。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ExitDoor : MonoBehaviour
{
    private bool triggered;

    private void Awake()
    {
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered)
        {
            return;
        }

        if (!other.CompareTag(GameConstants.Tags.Player))
        {
            return;
        }

        if (other.GetComponent<PlayerBody>() == null)
        {
            return;
        }

        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying)
        {
            return;
        }

        if (SceneFlowManager.Instance == null)
        {
            Debug.LogWarning("[ExitDoor] SceneFlowManager not found in scene.");
            return;
        }

        if (SceneFlowManager.Instance.IsLoading)
        {
            return;
        }

        triggered = true;

        AudioManager.Instance?.PlayLevelClear();

        int levelIndex = SceneFlowManager.Instance.CurrentLevelIndex;

        if (SaveManager.Instance != null)
        {
            LevelStarTracker.EnsureInstance();
            int currentStars = LevelStarTracker.Instance != null
                ? LevelStarTracker.Instance.CurrentStars
                : LevelStarTracker.MaxStars;
            SaveManager.Instance.SettleLevelStars(levelIndex, currentStars);
            SaveManager.Instance.OnLevelCleared(levelIndex);
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.EnterTransitioning();
        }

        SceneFlowManager.Instance.StartVictoryFlow();
    }
}
