using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class DeathZone : MonoBehaviour
{

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
        if (!other.CompareTag(GameConstants.Tags.Player))
        {
            return;
        }

        PlayerBody body = other.GetComponent<PlayerBody>();
        if (body == null)
        {
            return;
        }
        
        if (GameStateManager.Instance == null)
        {
            Debug.LogWarning("[DeathZone] GameStateManager not found in scene.");
            return;
        }
    //暂时用同步加载场景
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        return;
        GameStateManager.Instance.EnterGameOver();
    }
}
