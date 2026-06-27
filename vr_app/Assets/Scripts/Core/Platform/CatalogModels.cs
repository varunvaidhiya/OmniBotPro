using System.Collections.Generic;
using Newtonsoft.Json;

namespace OmniBot.VR.Core.Platform
{
    /// <summary>
    /// C# mirror of the website's VR catalog (website/lib/vr/catalog.ts →
    /// public/vr/catalog.json): every robot category + robot type + hardware
    /// model. The headset uses it to render a user's garage and to add new
    /// robots — the same catalog the website's RobotSelector uses. Keep these
    /// fields aligned with website/lib/garage/types.ts.
    /// </summary>
    public class VrCatalog
    {
        [JsonProperty("version")] public int Version;
        [JsonProperty("categories")] public List<VrCategory> Categories = new List<VrCategory>();
        [JsonProperty("robotTypes")] public List<VrRobotType> RobotTypes = new List<VrRobotType>();
    }

    public class VrCategory
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("label")] public string Label;
        [JsonProperty("icon")] public string Icon;   // lucide icon name
        [JsonProperty("blurb")] public string Blurb;
        [JsonProperty("color")] public string Color;  // accent hex
    }

    public class VrRobotType
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("name")] public string Name;
        [JsonProperty("category")] public string Category; // → VrCategory.Id
        [JsonProperty("tagline")] public string Tagline;
        [JsonProperty("description")] public string Description;
        [JsonProperty("hardwareModels")] public List<VrHardwareModel> HardwareModels = new List<VrHardwareModel>();
    }

    public class VrHardwareModel
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("name")] public string Name;
        [JsonProperty("manufacturer")] public string Manufacturer;
        [JsonProperty("desc")] public string Desc;
        [JsonProperty("specs")] public Dictionary<string, string> Specs = new Dictionary<string, string>();
        [JsonProperty("avatarPath")] public string AvatarPath; // SVG path, viewBox 0 0 32 32
        [JsonProperty("price")] public string Price;
        [JsonProperty("ros")] public string Ros;               // ros1 | ros2 | both | none | custom
        [JsonProperty("locomotion")] public string Locomotion; // → DriveKind on the website
        [JsonProperty("hasArm")] public bool HasArm;
        [JsonProperty("payloadKg")] public float PayloadKg;
        [JsonProperty("weightKg")] public float WeightKg;
        [JsonProperty("supportedProducts")] public List<string> SupportedProducts = new List<string>();
    }
}
