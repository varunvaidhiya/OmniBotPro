using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using OmniBot.VR.Core;
using OmniBot.VR.Core.Platform;

namespace OmniBot.VR.UI.Garage
{
    /// <summary>
    /// A card for one robot in the user's garage — name + resolved model/type,
    /// tinted by its category. Mirrors the website's GarageView rows.
    /// </summary>
    public class RobotCardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text modelText;
        [SerializeField] private Image categoryDot;
        [SerializeField] private Button button;

        public void Bind(GarageRobot robot, Action<GarageRobot> onClick)
        {
            if (nameText != null) nameText.text = robot.DisplayName;

            if (modelText != null)
            {
                var model = robot.HardwareModel != null ? robot.HardwareModel.Name : robot.UserRobot.HardwareModelId;
                var type = robot.RobotType != null ? robot.RobotType.Name : robot.UserRobot.RobotTypeId;
                modelText.text = $"{model} · {type}";
            }

            if (categoryDot != null)
                categoryDot.color = robot.Category != null ? OhhoTheme.Hex(robot.Category.Color) : OhhoTheme.Muted;

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClick?.Invoke(robot));
            }
        }
    }
}
