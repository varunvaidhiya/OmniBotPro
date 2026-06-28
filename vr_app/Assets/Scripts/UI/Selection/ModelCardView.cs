using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using OmniBot.VR.Core.Platform;

namespace OmniBot.VR.UI.Selection
{
    /// <summary>
    /// A card for one hardware model in the selection flow — name, manufacturer,
    /// and a compact specs summary (DOF, payload, ROS). Mirrors the website's
    /// model cards in <c>RobotSelector.tsx</c>. Picking one advances to the
    /// Name step.
    /// </summary>
    public class ModelCardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text manufacturerText;
        [SerializeField] private TMP_Text specsText;
        [SerializeField] private Button button;

        public void Bind(VrHardwareModel model, Action<VrHardwareModel> onClick)
        {
            if (nameText != null) nameText.text = model?.Name ?? "?";
            if (manufacturerText != null) manufacturerText.text = model?.Manufacturer ?? "";

            if (specsText != null)
            {
                var sb = new StringBuilder();
                if (model != null)
                {
                    if (model.Specs != null)
                    {
                        if (model.Specs.TryGetValue("dof", out var dof)) sb.Append($"DOF {dof} · ");
                        if (model.Specs.TryGetValue("speed", out var speed)) sb.Append($"{speed} · ");
                    }
                    if (model.HasArm) sb.Append("arm · ");
                    if (!string.IsNullOrEmpty(model.Ros)) sb.Append(model.Ros);
                }
                specsText.text = sb.ToString().TrimEnd(' ', '·');
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClick?.Invoke(model));
            }
        }
    }
}
