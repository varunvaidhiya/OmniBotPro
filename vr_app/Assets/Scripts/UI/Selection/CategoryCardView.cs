using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using OmniBot.VR.Core;
using OmniBot.VR.Core.Platform;

namespace OmniBot.VR.UI.Selection
{
    /// <summary>
    /// A card for one robot category in the selection flow — colored chip +
    /// label + blurb. Mirrors the website's category chips in
    /// <c>RobotSelector.tsx</c>. The category color tints the dot so the user
    /// can scan the grid visually.
    /// </summary>
    public class CategoryCardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text labeltext;
        [SerializeField] private TMP_Text blurbText;
        [SerializeField] private Image categoryDot;
        [SerializeField] private Button button;

        public void Bind(VrCategory category, Action<VrCategory> onClick)
        {
            if (labeltext != null) labeltext.text = category?.Label ?? "?";
            if (blurbText != null) blurbText.text = category?.Blurb ?? "";
            if (categoryDot != null)
                categoryDot.color = category != null ? OhhoTheme.Hex(category.Color) : OhhoTheme.Muted;
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClick?.Invoke(category));
            }
        }
    }
}
