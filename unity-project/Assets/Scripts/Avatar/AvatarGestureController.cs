using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class AvatarGestureController : MonoBehaviour
{
    [Header("Avatar Transforms")]
    public Transform headBone;
    public Transform spineBone;
    public Transform leftArmBone;
    public Transform rightArmBone;

    [Header("Base Animation")]
    [Tooltip("Optional animation authored for this exact avatar. It is played as the base pose beneath procedural gestures.")]
    public AnimationClip idleClip;

    [Header("State")]
    public string currentGesture = "idle";

    private Quaternion origHeadRot;
    private Quaternion origSpineRot;
    private Quaternion origLeftArmRot;
    private Quaternion origRightArmRot;

    private Coroutine activeGestureCoroutine;
    private PlayableGraph idleGraph;
    private AnimationClipPlayable idlePlayable;

    private void Start()
    {
        AutoBindBones();
        CacheOriginalRotations();
        StartIdleAnimation();
    }

    private void OnDestroy()
    {
        if (idleGraph.IsValid())
        {
            idleGraph.Destroy();
        }
    }

    private void Update()
    {
        if (idleGraph.IsValid() && idleClip != null && idleClip.length > 0f && idlePlayable.GetTime() >= idleClip.length)
        {
            idlePlayable.SetTime(idlePlayable.GetTime() % idleClip.length);
        }
    }

    public void AutoBindBones()
    {
        Animator animator = GetComponentInChildren<Animator>(true);
        if (animator != null && animator.isHuman)
        {
            headBone ??= animator.GetBoneTransform(HumanBodyBones.Head);
            spineBone ??= animator.GetBoneTransform(HumanBodyBones.Spine);
            leftArmBone ??= animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            rightArmBone ??= animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        }

        headBone ??= FindDescendant("Head");
        spineBone ??= FindDescendant("Spine", "Spine1", "Spine2", "Chest");
        leftArmBone ??= FindDescendant("LeftArm", "LeftUpperArm");
        rightArmBone ??= FindDescendant("RightArm", "RightUpperArm");
    }

    private Transform FindDescendant(params string[] candidateNames)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            foreach (string candidate in candidateNames)
            {
                if (string.Equals(child.name, candidate, System.StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }
        }

        return null;
    }

    private void StartIdleAnimation()
    {
        Animator animator = GetComponentInChildren<Animator>(true);
        if (idleClip == null || animator == null)
        {
            return;
        }

        idleGraph = PlayableGraph.Create($"{name}_IdleGraph");
        idleGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        AnimationPlayableOutput output = AnimationPlayableOutput.Create(idleGraph, "Idle", animator);
        idlePlayable = AnimationClipPlayable.Create(idleGraph, idleClip);
        idlePlayable.SetApplyFootIK(false);
        output.SetSourcePlayable(idlePlayable);
        idleGraph.Play();
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
