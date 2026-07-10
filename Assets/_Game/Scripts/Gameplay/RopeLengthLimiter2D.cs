using UnityEngine;

public class RopeLengthLimiter2D : MonoBehaviour
{
    [SerializeField] private Rigidbody2D anchorBody;
    [SerializeField] private Rigidbody2D targetBody;
    [Min(0.01f)]
    [SerializeField] private float maxLength = 5f;
    [SerializeField] private bool removeOutwardVelocity = true;
    [SerializeField] private bool drawGizmo = true;
    [SerializeField] private Color gizmoColor = new Color(1f, 0.6f, 0.1f, 0.9f);

    private PlayerBody anchorPlayerBody;
    private PlayerBody targetPlayerBody;

    public void Configure(Rigidbody2D anchor, Rigidbody2D target, float length)
    {
        anchorBody = anchor;
        targetBody = target;
        maxLength = Mathf.Max(0.01f, length);
        RefreshCachedPlayerBodies();
    }

    private void Awake()
    {
        RefreshCachedPlayerBodies();
    }

    private void FixedUpdate()
    {
        if (anchorBody == null || targetBody == null)
        {
            return;
        }

        ResolveBodies(out Rigidbody2D referenceBody, out Rigidbody2D constrainedBody);
        if (referenceBody == null || constrainedBody == null || referenceBody == constrainedBody)
        {
            return;
        }

        Vector2 referencePosition = referenceBody.position;
        Vector2 constrainedPosition = constrainedBody.position;
        Vector2 delta = constrainedPosition - referencePosition;
        float distance = delta.magnitude;

        if (distance <= maxLength || distance <= 0.0001f)
        {
            return;
        }

        Vector2 direction = delta / distance;
        Vector2 clampedPosition = referencePosition + direction * maxLength;

        constrainedBody.position = clampedPosition;

        if (!removeOutwardVelocity)
        {
            return;
        }

        Vector2 relativeVelocity = constrainedBody.velocity - referenceBody.velocity;
        float outwardSpeed = Vector2.Dot(relativeVelocity, direction);
        if (outwardSpeed > 0f)
        {
            constrainedBody.velocity -= direction * outwardSpeed;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmo || anchorBody == null)
        {
            return;
        }

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(anchorBody.position, maxLength);
    }

    private void ResolveBodies(out Rigidbody2D referenceBody, out Rigidbody2D constrainedBody)
    {
        RefreshCachedPlayerBodies();

        bool anchorFixed = anchorPlayerBody != null && anchorPlayerBody.IsFixed;
        bool targetFixed = targetPlayerBody != null && targetPlayerBody.IsFixed;

        if (anchorFixed && !targetFixed)
        {
            referenceBody = anchorBody;
            constrainedBody = targetBody;
            return;
        }

        if (!anchorFixed && targetFixed)
        {
            referenceBody = targetBody;
            constrainedBody = anchorBody;
            return;
        }

        referenceBody = anchorBody;
        constrainedBody = targetBody;
    }

    private void RefreshCachedPlayerBodies()
    {
        anchorPlayerBody = anchorBody != null ? anchorBody.GetComponent<PlayerBody>() : null;
        targetPlayerBody = targetBody != null ? targetBody.GetComponent<PlayerBody>() : null;
    }
}
