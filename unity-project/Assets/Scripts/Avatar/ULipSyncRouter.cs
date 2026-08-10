using UnityEngine;

[RequireComponent(typeof(uLipSync.uLipSync))]
public class ULipSyncRouter : MonoBehaviour
{
    public uLipSync.uLipSync analyzer;
    public AvatarLipSync[] targets;

    private void Awake()
    {
        analyzer ??= GetComponent<uLipSync.uLipSync>();
        if (analyzer != null) analyzer.onLipSyncUpdate.AddListener(Route);
    }

    private void OnDestroy()
    {
        if (analyzer != null) analyzer.onLipSyncUpdate.RemoveListener(Route);
    }

    private void Route(uLipSync.LipSyncInfo info)
    {
        if (targets == null) return;
        foreach (AvatarLipSync target in targets)
        {
            if (target != null && target.gameObject.activeInHierarchy) target.OnULipSyncUpdate(info);
        }
    }
}
