using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using OmniBot.VR.Core;
using OmniBot.VR.Core.Platform;

namespace OmniBot.VR.UI.Console
{
    /// <summary>
    /// A single product card in the VR console — the glass card from the website's
    /// product grid (components/Products.tsx), rebuilt for world space. Bind a
    /// <see cref="VrProduct"/> and a click handler.
    /// </summary>
    public class ProductCardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text tagText;
        [SerializeField] private TMP_Text descText;
        [SerializeField] private Image accentBar;   // thin accent strip / icon tint
        [SerializeField] private Button button;

        public void Bind(VrProduct product, Action<VrProduct> onClick)
        {
            if (nameText != null) nameText.text = product.Name;
            if (tagText != null) tagText.text = product.Tag;
            if (descText != null) descText.text = product.Desc;
            if (accentBar != null) accentBar.color = OhhoTheme.Accent(product.Accent);

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClick?.Invoke(product));
            }
        }
    }
}
