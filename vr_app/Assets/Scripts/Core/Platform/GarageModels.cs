using System.Collections.Generic;
using Newtonsoft.Json;

namespace OmniBot.VR.Core.Platform
{
    /// <summary>
    /// One robot in the signed-in user's garage — a row of the Supabase
    /// `user_robots` table (see website/supabase/migrations/0002_user_robots.sql).
    /// JSON comes straight from PostgREST in snake_case; the property names map it.
    ///
    /// This is the shared identity that makes the experience connected: a robot
    /// the user added on the website or Android app appears here automatically,
    /// with no re-entry. It stores only the catalog ids — resolve them to
    /// displayable models via <see cref="OhhoCatalog"/>.
    /// </summary>
    public class UserRobot
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("user_id")] public string UserId;
        [JsonProperty("name")] public string Name;
        [JsonProperty("robot_type_id")] public string RobotTypeId;
        [JsonProperty("hardware_model_id")] public string HardwareModelId;
        [JsonProperty("status")] public string Status; // active | draft | simulated | offline
        [JsonProperty("config")] public Dictionary<string, object> Config = new Dictionary<string, object>();
        [JsonProperty("created_at")] public string CreatedAt;
        [JsonProperty("updated_at")] public string UpdatedAt;
    }

    /// <summary>A user's robot joined with its catalog entries, ready to display
    /// (mirrors the website's GarageRobot).</summary>
    public class GarageRobot
    {
        public UserRobot UserRobot;
        public VrRobotType RobotType;
        public VrHardwareModel HardwareModel;
        public VrCategory Category;

        public string DisplayName => UserRobot?.Name;
        public bool HasArm => HardwareModel != null && HardwareModel.HasArm;
        public string Locomotion => HardwareModel?.Locomotion;
    }
}
