using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using OmniBot.VR.Core.Platform;

namespace OmniBot.VR.UI.Selection
{
    /// <summary>
    /// A card for one robot type (e.g. "OmniBot Pro", "TurtleBot 4") in the
    /// selection flow — name + tagline. Mirrors the website's type rows in
    /// <c>RobotSelector.tsx</c>. Filtering by category already happened before
    /// these cards are spawned.
    /// </summary>
    public class TypeCardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text taglineText;
        [SerializeField] private Button button;

        public void Bind(VrRobotType type, Action<VrRobotType> onClick)
        {
            if (nameText != null) nameText.text = type?.Name ?? "?";
            if (taglineText != null) taglineText.text = type?.Tagline ?? "";
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClick?.Invoke(type));
            }
        }
    }
}
