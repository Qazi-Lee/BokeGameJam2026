using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class AttachPoint : MonoBehaviour
{
    [Header("Attach")]
    [SerializeField] private float attachRadius = 1.25f;

    [Header("Movement")]
    [SerializeField] private bool canMoveVerticallyWhileAttached;
    [ShowIf(nameof(canMoveVerticallyWhileAttached))]
    [SerializeField] private float verticalMoveSpeed = 3f;
    [ShowIf(nameof(canMoveVerticallyWhileAttached))]
    [SerializeField] private bool clampVerticalMovement;
    [ShowIf("@this.canMoveVerticallyWhileAttached && this.clampVerticalMovement")]
    [SerializeField] private float minYOffset = -2f;
    [ShowIf("@this.canMoveVerticallyWhileAttached && this.clampVerticalMovement")]
    [SerializeField] private float maxYOffset = 2f;

    [Header("Hint")]
    [SerializeField] private Transform hintRoot;
    [SerializeField] private Sprite hintSprite;
    [SerializeField] private Vector3 hintOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private GameObject attachEffectPrefab;

    [Header("Hint Animation")]
    [SerializeField] private bool enableHintBreath = true;
    [SerializeField] private float breathScaleMultiplier = 1.12f;
    [SerializeField] private float breathDuration = 0.6f;

    [Header("Gizmo")]
    [SerializeField] private bool drawGizmo = true;
    [SerializeField] private Color gizmoColor = new Color(0.2f, 0.9f, 0.3f, 0.9f);

    private PlayerBody attachedPlayer;
    private float initialLocalY;
    private bool initialLocalYCaptured;
    private float verticalInput;
    private bool hintVisible;
    private Vector3 cachedHintScale = Vector3.one;
    private Tween hintBreathTween;

    public Vector2 Position => transform.position;

    private void Awake()
    {
        CaptureInitialLocalY();
        EnsureHintRoot();
        ApplyHintSprite();
        HideHint(force: true);
    }

    private void Update()
    {
        UpdateVerticalInput();
    }

    private void FixedUpdate()
    {
        if (!canMoveVerticallyWhileAttached || attachedPlayer == null || !CanAcceptInput())
        {
            return;
        }

        if (Mathf.Approximately(verticalInput, 0f))
        {
            return;
        }

        MoveVertically(verticalInput);
    }

    private void LateUpdate()
    {
        SetHintVisible(ShouldShowHint());
    }

    private void OnValidate()
    {
        attachRadius = Mathf.Max(0.01f, attachRadius);
        verticalMoveSpeed = Mathf.Max(0f, verticalMoveSpeed);

        if (minYOffset > maxYOffset)
        {
            float temp = minYOffset;
            minYOffset = maxYOffset;
            maxYOffset = temp;
        }

        EnsureHintRoot();
        ApplyHintSprite();

        if (hintRoot != null)
        {
            hintRoot.localPosition = hintOffset;
            cachedHintScale = hintRoot.localScale;
        }
    }

    private void OnDestroy()
    {
        StopHintBreath();
    }

    public bool CanAttach(PlayerBody playerBody)
    {
        if (playerBody == null)
        {
            return false;
        }

        Vector2 offset = playerBody.transform.position - transform.position;
        return offset.sqrMagnitude <= attachRadius * attachRadius;
    }

    public float GetDistanceSqr(PlayerBody playerBody)
    {
        if (playerBody == null)
        {
            return float.MaxValue;
        }

        Vector2 offset = playerBody.transform.position - transform.position;
        return offset.sqrMagnitude;
    }

    public void AttachPlayer(PlayerBody playerBody)
    {
        attachedPlayer = playerBody;
        PlayAttachEffect();
    }

    public void DetachPlayer(PlayerBody playerBody)
    {
        if (attachedPlayer != playerBody)
        {
            return;
        }

        attachedPlayer = null;
        verticalInput = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmo)
        {
            return;
        }

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, attachRadius);
    }

    private void UpdateVerticalInput()
    {
        if (!canMoveVerticallyWhileAttached || attachedPlayer == null || !CanAcceptInput())
        {
            verticalInput = 0f;
            return;
        }

        verticalInput = 0f;
        if (Input.GetKey(KeyCode.W))
        {
            verticalInput += 1f;
        }

        if (Input.GetKey(KeyCode.S))
        {
            verticalInput -= 1f;
        }
    }

    private void MoveVertically(float input)
    {
        CaptureInitialLocalY();

        Vector3 localPosition = transform.localPosition;
        localPosition.y += input * verticalMoveSpeed * Time.fixedDeltaTime;

        if (clampVerticalMovement)
        {
            float minY = initialLocalY + minYOffset;
            float maxY = initialLocalY + maxYOffset;
            localPosition.y = Mathf.Clamp(localPosition.y, minY, maxY);
        }

        transform.localPosition = localPosition;
    }

    private bool ShouldShowHint()
    {
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying)
        {
            return false;
        }

        GameObject[] players = GameObject.FindGameObjectsWithTag(GameConstants.Tags.Player);
        for (int i = 0; i < players.Length; i++)
        {
            PlayerBody playerBody = players[i].GetComponent<PlayerBody>();
            if (playerBody == null || playerBody.IsFixed)
            {
                continue;
            }

            if (CanAttach(playerBody))
            {
                return true;
            }
        }

        return false;
    }

    private void EnsureHintRoot()
    {
        if (hintRoot != null)
        {
            return;
        }

        Transform child = transform.Find("Hint");
        if (child != null)
        {
            hintRoot = child;
        }
    }

    private void ApplyHintSprite()
    {
        if (hintSprite == null || hintRoot == null)
        {
            return;
        }

        SpriteRenderer renderer = hintRoot.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sprite = hintSprite;
        }
    }

    private void PlayAttachEffect()
    {
        if (attachEffectPrefab == null)
        {
            return;
        }

        GameObject effectInstance = Instantiate(attachEffectPrefab, transform.position, attachEffectPrefab.transform.rotation);
        Destroy(effectInstance, 1f);
    }

    private void SetHintVisible(bool visible)
    {
        if (visible)
        {
            ShowHint();
            return;
        }

        HideHint(force: false);
    }

    private void ShowHint()
    {
        if (hintRoot == null || hintVisible)
        {
            return;
        }

        hintVisible = true;
        hintRoot.gameObject.SetActive(true);
        cachedHintScale = hintRoot.localScale;
        StartHintBreath();
    }

    private void HideHint(bool force)
    {
        if (hintRoot == null)
        {
            return;
        }

        if (!hintVisible && !force)
        {
            return;
        }

        hintVisible = false;
        StopHintBreath();
        hintRoot.gameObject.SetActive(false);
    }

    private void StartHintBreath()
    {
        if (!enableHintBreath || hintRoot == null)
        {
            return;
        }

        StopHintBreath();
        hintRoot.localScale = cachedHintScale;
        hintBreathTween = hintRoot
            .DOScale(cachedHintScale * breathScaleMultiplier, breathDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    private void StopHintBreath()
    {
        if (hintBreathTween != null)
        {
            hintBreathTween.Kill();
            hintBreathTween = null;
        }

        if (hintRoot != null)
        {
            hintRoot.localScale = cachedHintScale;
        }
    }

    private bool CanAcceptInput()
    {
        return GameStateManager.Instance == null || GameStateManager.Instance.IsPlaying;
    }

    private void CaptureInitialLocalY()
    {
        if (initialLocalYCaptured)
        {
            return;
        }

        initialLocalY = transform.localPosition.y;
        initialLocalYCaptured = true;
    }
}
