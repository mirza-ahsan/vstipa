using System;
using UnityEngine;

public class PersonaManager : MonoBehaviour
{
    [Serializable]
    public class PersonaSlot
    {
        public string persona;
        public string displayName;
        public GameObject avatarRoot;
        public AvatarGestureController gestureController;
        public AvatarLipSync lipSync;
        public Color accentColor = Color.white;
        [Range(0f, 100f)] public float baselineSmile;
    }

    public QuestionPlaybackController playbackController;
    public PersonaSlot[] personas;
    public string activePersona;

    public event Action<PersonaSlot> OnPersonaChanged;

    private void Awake()
    {
        HideAllAvatars();
    }

    public bool SelectPersona(string personaId)
    {
        return SelectPersona(personaId, string.Empty);
    }

    public bool SelectPersona(string personaId, string targetRole)
    {
        PersonaSlot selected = null;
        foreach (PersonaSlot slot in personas)
        {
            bool isSelected = slot != null && string.Equals(slot.persona, personaId, StringComparison.OrdinalIgnoreCase);
            if (slot?.avatarRoot != null) slot.avatarRoot.SetActive(isSelected);
            if (isSelected) selected = slot;
        }

        if (selected == null)
        {
            Debug.LogError($"[PersonaManager] Unknown persona '{personaId}'.");
            return false;
        }

        activePersona = selected.persona;
        selected.lipSync?.SetPersonaBaselineSmile(selected.baselineSmile);

        if (playbackController != null)
        {
            playbackController.activeAvatarGestureController = selected.gestureController;
            playbackController.activeAvatarLipSync = selected.lipSync;
            if (selected.lipSync != null) selected.lipSync.audioSource = playbackController.audioSource;
            if (string.IsNullOrWhiteSpace(targetRole))
                playbackController.LoadManifest(selected.persona);
            else
                playbackController.LoadRoleBasedManifest(selected.persona, targetRole);
        }

        OnPersonaChanged?.Invoke(selected);
        Debug.Log($"[PersonaManager] Selected {selected.displayName} ({selected.persona}).");
        return true;
    }

    public void ReturnToSelection()
    {
        activePersona = string.Empty;
        playbackController?.StopPlayback();
        HideAllAvatars();
    }

    public PersonaSlot GetActiveSlot()
    {
        foreach (PersonaSlot slot in personas)
        {
            if (slot != null && string.Equals(slot.persona, activePersona, StringComparison.OrdinalIgnoreCase))
                return slot;
        }
        return null;
    }

    private void HideAllAvatars()
    {
        if (personas == null) return;
        foreach (PersonaSlot slot in personas)
        {
            if (slot?.avatarRoot != null) slot.avatarRoot.SetActive(false);
        }
    }
}
