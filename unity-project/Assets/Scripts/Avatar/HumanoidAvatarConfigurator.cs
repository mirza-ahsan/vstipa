using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds a Unity Humanoid Avatar at runtime for GLB imports whose scripted importer
/// exposes a valid Mixamo-style skeleton but no serialized Avatar asset.
/// </summary>
public class HumanoidAvatarConfigurator : MonoBehaviour
{
    public Animator animator;
    public bool configureOnAwake = true;
    public bool configurationSucceeded;

    private static readonly (HumanBodyBones human, string model)[] BoneMap =
    {
        (HumanBodyBones.Hips, "Hips"),
        (HumanBodyBones.Spine, "Spine"),
        (HumanBodyBones.Chest, "Spine1"),
        (HumanBodyBones.UpperChest, "Spine2"),
        (HumanBodyBones.Neck, "Neck"),
        (HumanBodyBones.Head, "Head"),
        (HumanBodyBones.LeftShoulder, "LeftShoulder"),
        (HumanBodyBones.LeftUpperArm, "LeftArm"),
        (HumanBodyBones.LeftLowerArm, "LeftForeArm"),
        (HumanBodyBones.LeftHand, "LeftHand"),
        (HumanBodyBones.RightShoulder, "RightShoulder"),
        (HumanBodyBones.RightUpperArm, "RightArm"),
        (HumanBodyBones.RightLowerArm, "RightForeArm"),
        (HumanBodyBones.RightHand, "RightHand"),
        (HumanBodyBones.LeftUpperLeg, "LeftUpLeg"),
        (HumanBodyBones.LeftLowerLeg, "LeftLeg"),
        (HumanBodyBones.LeftFoot, "LeftFoot"),
        (HumanBodyBones.LeftToes, "LeftToeBase"),
        (HumanBodyBones.RightUpperLeg, "RightUpLeg"),
        (HumanBodyBones.RightLowerLeg, "RightLeg"),
        (HumanBodyBones.RightFoot, "RightFoot"),
        (HumanBodyBones.RightToes, "RightToeBase")
    };

    private void Awake()
    {
        if (configureOnAwake) Configure();
    }

    public bool Configure()
    {
        animator ??= GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            Debug.LogError($"[HumanoidAvatarConfigurator] {name}: Animator not found.");
            return false;
        }

        if (animator.avatar != null && animator.avatar.isValid && animator.avatar.isHuman)
        {
            configurationSucceeded = true;
            return true;
        }

        var transforms = GetComponentsInChildren<Transform>(true);
        var byName = new Dictionary<string, Transform>();
        foreach (Transform transformInRig in transforms)
        {
            if (!byName.ContainsKey(transformInRig.name)) byName.Add(transformInRig.name, transformInRig);
        }

        var humanBones = new List<HumanBone>();
        foreach ((HumanBodyBones human, string model) in BoneMap)
        {
            if (!byName.ContainsKey(model)) continue;
            humanBones.Add(new HumanBone
            {
                boneName = model,
                humanName = HumanTrait.BoneName[(int)human],
                limit = new HumanLimit { useDefaultValues = true }
            });
        }

        var skeleton = new SkeletonBone[transforms.Length];
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform bone = transforms[i];
            skeleton[i] = new SkeletonBone
            {
                name = bone.name,
                position = bone.localPosition,
                rotation = bone.localRotation,
                scale = bone.localScale
            };
        }

        HumanDescription description = new HumanDescription
        {
            human = humanBones.ToArray(),
            skeleton = skeleton,
            upperArmTwist = 0.5f,
            lowerArmTwist = 0.5f,
            upperLegTwist = 0.5f,
            lowerLegTwist = 0.5f,
            armStretch = 0.05f,
            legStretch = 0.05f,
            feetSpacing = 0f,
            hasTranslationDoF = false
        };

        Avatar runtimeAvatar = AvatarBuilder.BuildHumanAvatar(gameObject, description);
        if (runtimeAvatar == null || !runtimeAvatar.isValid || !runtimeAvatar.isHuman)
        {
            Debug.LogError($"[HumanoidAvatarConfigurator] {name}: runtime Humanoid mapping failed.");
            if (runtimeAvatar != null) Destroy(runtimeAvatar);
            return false;
        }

        runtimeAvatar.name = $"{name}_RuntimeHumanoid";
        animator.avatar = runtimeAvatar;
        animator.applyRootMotion = false;
        configurationSucceeded = true;
        Debug.Log($"[HumanoidAvatarConfigurator] {name}: Humanoid mapping verified with {humanBones.Count} mapped bones.");
        return true;
    }
}
