using System.Collections.Generic;
using UnityEngine;

public class WindArea2D : MonoBehaviour
{
    [Header("Area")]
    [SerializeField] private Vector2 areaSize = new Vector2(4f, 2f);
    [SerializeField] private Vector2 areaOffset;

    [Header("Wind")]
    [SerializeField] private Vector2 windDirection = Vector2.right;
    [SerializeField] private float windForce = 8f;
    [SerializeField] private ForceMode2D forceMode = ForceMode2D.Force;

    [Header("Filter")]
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private bool affectOnlyPlayerBody = true;
    [SerializeField] private LayerMask targetLayers = Physics2D.AllLayers;

    [Header("Debug")]
    [SerializeField] private bool drawGizmo = true;
    [SerializeField] private Color gizmoColor = new Color(0.4f, 0.8f, 1f, 0.8f);
    [SerializeField] private Color arrowColor = new Color(0.9f, 0.95f, 1f, 0.95f);

    private readonly HashSet<Rigidbody2D> affectedBodies = new HashSet<Rigidbody2D>();

    private void FixedUpdate()
    {
        Vector2 direction = GetWorldWindDirection();
        if (direction.sqrMagnitude < 0.0001f || windForce <= 0f)
        {
            return;
        }

        Collider2D[] hits = Physics2D.OverlapBoxAll(GetWorldCenter(), areaSize, transform.eulerAngles.z, targetLayers);
        if (hits == null || hits.Length == 0)
        {
            return;
        }

        affectedBodies.Clear();
        Vector2 force = direction * windForce;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            Rigidbody2D targetBody = hit.attachedRigidbody;
            if (targetBody == null || !affectedBodies.Add(targetBody))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(targetTag) && !targetBody.CompareTag(targetTag))
            {
                continue;
            }

            PlayerBody playerBody = targetBody.GetComponent<PlayerBody>();
            if (affectOnlyPlayerBody && playerBody == null)
            {
                continue;
            }

            if (playerBody != null)
            {
                playerBody.AddForce(force, forceMode);
                continue;
            }

            targetBody.AddForce(force, forceMode);
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmo)
        {
            return;
        }

        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(areaOffset, areaSize);

        Vector2 direction = GetLocalWindDirection();
        if (direction.sqrMagnitude >= 0.0001f)
        {
            DrawArrow(areaOffset, direction.normalized);
        }

        Gizmos.matrix = previousMatrix;
    }

    private Vector2 GetWorldCenter()
    {
        return (Vector2)transform.position + (Vector2)transform.TransformVector(areaOffset);
    }

    private Vector2 GetLocalWindDirection()
    {
        return windDirection.normalized;
    }

    private Vector2 GetWorldWindDirection()
    {
        Vector2 localDirection = GetLocalWindDirection();
        Vector3 worldDirection = transform.TransformDirection(new Vector3(localDirection.x, localDirection.y, 0f));
        return new Vector2(worldDirection.x, worldDirection.y).normalized;
    }

    private void DrawArrow(Vector2 localCenter, Vector2 localDirection)
    {
        Gizmos.color = arrowColor;

        float bodyLength = Mathf.Max(Mathf.Min(areaSize.x, areaSize.y) * 0.4f, 0.4f);
        Vector3 start = localCenter - localDirection * (bodyLength * 0.5f);
        Vector3 end = localCenter + localDirection * (bodyLength * 0.5f);
        Gizmos.DrawLine(start, end);

        Vector2 headBase = localCenter + localDirection * (bodyLength * 0.5f - 0.15f);
        Vector2 perpendicular = new Vector2(-localDirection.y, localDirection.x);
        float headLength = Mathf.Max(bodyLength * 0.25f, 0.2f);
        float headWidth = headLength * 0.5f;

        Gizmos.DrawLine(end, headBase - localDirection * headLength + perpendicular * headWidth);
        Gizmos.DrawLine(end, headBase - localDirection * headLength - perpendicular * headWidth);
    }

    private void OnValidate()
    {
        areaSize.x = Mathf.Max(0.01f, areaSize.x);
        areaSize.y = Mathf.Max(0.01f, areaSize.y);
    }
}
