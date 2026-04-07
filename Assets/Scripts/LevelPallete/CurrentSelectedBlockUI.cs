using UnityEngine;
using UnityEngine.UI;

public class CurrentSelectedBlockUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RawImage currentBlockIcon;

    public void SetCurrentIcon(Texture iconTexture)
    {
        if (currentBlockIcon == null)
            return;

        currentBlockIcon.texture = iconTexture;
        currentBlockIcon.enabled = iconTexture != null;
    }

    public void ClearIcon()
    {
        if (currentBlockIcon == null)
            return;

        currentBlockIcon.texture = null;
        currentBlockIcon.enabled = false;
    }
}