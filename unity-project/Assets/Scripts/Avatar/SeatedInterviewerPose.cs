using System;
using UnityEngine;

/// <summary>
/// Converts the source avatar's standing idle into a stable seated lower-body
/// pose after animation evaluation. Upper-body motion remains available to the
/// gesture and lip-sync systems.
/// </summary>
[DefaultExecutionOrder(100)]
public class SeatedInterviewerPose : MonoBehaviour
{
    [Header("Audience Facing")]
    [Tooltip("Usually the active camera. Only horizontal rotation is applied so the seated pose stays level.")]
    public Transform facingTarget;
    [Range(15f, 180f)] public float bodyTurnSpeed = 90f;

    [Header("Seat Alignment")]
    [Range(55f, 95f)] public float upperLegPitch = 80f;
    [Range(-100f, -45f)] public float lowerLegPitch = -80f;
    [Range(0f, 8f)] public float legSpread = 2.5f;

    [Header("Rig Bones")]
    public Transform hips;
    public Transform leftUpperLeg;
    public Transform leftLowerLeg;
    public Transform rightUpperLeg;
    public Transform rightLowerLeg;

    [Header("Diagnostics")]
    public bool rigBound;

    private void Awake()
    {
        BindRig();
    }

    private void LateUpdate()
    {
        FaceTargetSmoothly();
        ApplySeatedPose();
    }

    public void FaceTargetImmediately()
    {
        if (!TryGetFacingRotation(out Quaternion targetRotation)) return;
        transform.rotation = targetRotation;
    }

    public void FaceTargetSmoothly()
    {
        if (!TryGetFacingRotation(out Quaternion targetRotation)) return;
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            Mathf.Max(15f, bodyTurnSpeed) * Time.deltaTime);
    }

    public bool BindRig()
    {
        // Unity serializes unassigned Object fields as "fake null" references,
        // so use Unity's overloaded null check rather than C# null-coalescing.
        if (hips == null) hips = FindDescendant("Hips");
        if (leftUpperLeg == null) leftUpperLeg = FindDescendant("LeftUpLeg", "LeftUpperLeg");
        if (leftLowerLeg == null) leftLowerLeg = FindDescendant("LeftLeg", "LeftLowerLeg");
        if (rightUpperLeg == null) rightUpperLeg = FindDescendant("RightUpLeg", "RightUpperLeg");
        if (rightLowerLeg == null) rightLowerLeg = FindDescendant("RightLeg", "RightLowerLeg");

        rigBound = hips != null && leftUpperLeg != null && leftLowerLeg != null &&
            rightUpperLeg != null && rightLowerLeg != null;
        if (!rigBound)
        {
            Transform[] hierarchy = GetComponentsInChildren<Transform>(true);
            string availableNames = string.Join(", ", Array.ConvertAll(hierarchy, item => item.name));
            Debug.LogError($"[SeatedInterviewerPose] {name}: required hip/leg bones were not found. " +
                $"Available hierarchy: {availableNames}");
        }
        return rigBound;
    }

    public void ApplySeatedPose()
    {
        if (!rigBound && !BindRig()) return;

        // The Avaturn/Mixamo-style rig points each leg bone down its local Y axis.
        // Pitching the thighs forward and applying the inverse pitch at the knees
        // produces a seated right angle while retaining the animated source pose.
        leftUpperLeg.localRotation *= Quaternion.Euler(upperLegPitch, 0f, legSpread);
        rightUpperLeg.localRotation *= Quaternion.Euler(upperLegPitch, 0f, -legSpread);
        leftLowerLeg.localRotation *= Quaternion.Euler(lowerLegPitch, 0f, 0f);
        rightLowerLeg.localRotation *= Quaternion.Euler(lowerLegPitch, 0f, 0f);
    }

    private bool TryGetFacingRotation(out Quaternion targetRotation)
    {
        targetRotation = transform.rotation;
        if (facingTarget == null) return false;

        Vector3 horizontalDirection = facingTarget.position - transform.position;
        horizontalDirection.y = 0f;
        if (horizontalDirection.sqrMagnitude < 0.0001f) return false;

        targetRotation = Quaternion.LookRotation(horizontalDirection.normalized, Vector3.up);
        return true;
    }

    private Transform FindDescendant(params string[] candidateNames)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            foreach (string candidate in candidateNames)
            {
                if (string.Equals(child.name, candidate, StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }
        }
        return null;
    }
}
