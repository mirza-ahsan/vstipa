using System.Collections;
using UnityEngine;

public class AvatarGestureController : MonoBehaviour
{
    [Header("Avatar Transforms")]
    public Transform headBone;
    public Transform spineBone;
    public Transform leftArmBone;
    public Transform rightArmBone;

    [Header("State")]
    public string currentGesture = "idle";

    private Quaternion origHeadRot;
    private Quaternion origSpineRot;
    private Quaternion origLeftArmRot;
    private Quaternion origRightArmRot;

    private Coroutine activeGestureCoroutine;

    private void Start()
    {
        CacheOriginalRotations();
    }

    public void CacheOriginalRotations()
    {
        if (headBone != null) origHeadRot = headBone.localRotation;
        if (spineBone != null) origSpineRot = spineBone.localRotation;
        if (leftArmBone != null) origLeftArmRot = leftArmBone.localRotation;
        if (rightArmBone != null) origRightArmRot = rightArmBone.localRotation;
    }

    public void TriggerGesture(string gestureName)
    {
        currentGesture = gestureName.ToLower();

        if (activeGestureCoroutine != null)
        {
            StopCoroutine(activeGestureCoroutine);
        }

        switch (currentGesture)
        {
            case "nod":
            case "nod_firm":
                activeGestureCoroutine = StartCoroutine(NodCoroutine());
                break;
            case "lean_forward":
                activeGestureCoroutine = StartCoroutine(LeanForwardCoroutine());
                break;
            case "lean_back":
                activeGestureCoroutine = StartCoroutine(LeanBackCoroutine());
                break;
            case "arms_crossed":
                activeGestureCoroutine = StartCoroutine(ArmsCrossedCoroutine());
                break;
            case "thinking":
                activeGestureCoroutine = StartCoroutine(ThinkingCoroutine());
                break;
            case "smile":
                activeGestureCoroutine = StartCoroutine(SmileGestureCoroutine());
                break;
            default:
                activeGestureCoroutine = StartCoroutine(IdleCoroutine());
                break;
        }
    }

    private IEnumerator NodCoroutine()
    {
        float duration = 1.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float pitchOffset = Mathf.Sin(t * Mathf.PI * 3.0f) * 12.0f; // Nod up and down 1.5 cycles

            if (headBone != null)
            {
                headBone.localRotation = origHeadRot * Quaternion.Euler(pitchOffset, 0, 0);
            }
            yield return null;
        }

        ResetPose();
    }

    private IEnumerator LeanForwardCoroutine()
    {
        float duration = 2.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.PingPong(elapsed * 0.8f, 1.0f);
            float leanAngle = Mathf.SmoothStep(0f, 10.0f, t);

            if (spineBone != null)
            {
                spineBone.localRotation = origSpineRot * Quaternion.Euler(leanAngle, 0, 0);
            }
            yield return null;
        }

        ResetPose();
    }

    private IEnumerator LeanBackCoroutine()
    {
        float duration = 2.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.PingPong(elapsed * 0.8f, 1.0f);
            float leanAngle = Mathf.SmoothStep(0f, -8.0f, t);

            if (spineBone != null)
            {
                spineBone.localRotation = origSpineRot * Quaternion.Euler(leanAngle, 0, 0);
            }
            yield return null;
        }

        ResetPose();
    }

    private IEnumerator ArmsCrossedCoroutine()
    {
        float duration = 3.0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.PingPong(elapsed * 0.6f, 1.0f);

            if (leftArmBone != null) leftArmBone.localRotation = origLeftArmRot * Quaternion.Euler(0, 45.0f * t, 20.0f * t);
            if (rightArmBone != null) rightArmBone.localRotation = origRightArmRot * Quaternion.Euler(0, -45.0f * t, -20.0f * t);

            yield return null;
        }

        ResetPose();
    }

    private IEnumerator ThinkingCoroutine()
    {
        float duration = 2.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Sin(elapsed * 2.0f) * 6.0f;

            if (headBone != null) headBone.localRotation = origHeadRot * Quaternion.Euler(-4.0f, t, 6.0f);
            yield return null;
        }

        ResetPose();
    }

    private IEnumerator SmileGestureCoroutine()
    {
        float duration = 2.0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float tilt = Mathf.Sin(elapsed * Mathf.PI) * 4.0f;

            if (headBone != null) headBone.localRotation = origHeadRot * Quaternion.Euler(-2.0f, 0, tilt);
            yield return null;
        }

        ResetPose();
    }

    private IEnumerator IdleCoroutine()
    {
        while (true)
        {
            float breath = Mathf.Sin(Time.time * 1.5f) * 1.5f;
            if (spineBone != null) spineBone.localRotation = origSpineRot * Quaternion.Euler(breath, 0, 0);
            yield return null;
        }
    }

    public void ResetPose()
    {
        if (headBone != null) headBone.localRotation = origHeadRot;
        if (spineBone != null) spineBone.localRotation = origSpineRot;
        if (leftArmBone != null) leftArmBone.localRotation = origLeftArmRot;
        if (rightArmBone != null) rightArmBone.localRotation = origRightArmRot;
    }
}
