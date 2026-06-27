using System.Collections.Generic;
using Newtonsoft.Json;

namespace OmniBot.VR.Core.Platform
{
    /// <summary>
    /// C# mirror of the website's VR manifest (website/lib/vr/manifest.ts →
    /// public/vr/manifest.json). The headset fetches this one static file to
    /// learn the OhhO branding, the Supabase auth config, and which products
    /// require a VR headset. Keep these fields in sync with that module.
    /// </summary>
    public class VrManifest
    {
        [JsonProperty("version")] public int Version;
        [JsonProperty("theme")] public VrTheme Theme;
        [JsonProperty("auth")] public VrAuthConfig Auth;
        [JsonProperty("products")] public List<VrProduct> Products = new List<VrProduct>();
    }

    public class VrTheme
    {
        [JsonProperty("bg")] public string Bg;
        [JsonProperty("surface")] public string Surface;
        [JsonProperty("cyan")] public string Cyan;
        [JsonProperty("violet")] public string Violet;
        [JsonProperty("violetLite")] public string VioletLite;
        [JsonProperty("text")] public string Text;
        [JsonProperty("muted")] public string Muted;
        [JsonProperty("border")] public string Border;
        [JsonProperty("fonts")] public VrFonts Fonts;
    }

    public class VrFonts
    {
        [JsonProperty("display")] public string Display;
        [JsonProperty("body")] public string Body;
        [JsonProperty("mono")] public string Mono;
    }

    /// <summary>
    /// Supabase auth config. Public values (same anon key the website inlines).
    /// GoTrue lives at <c>{Url}/auth/v1</c>; the garage table is queried via
    /// PostgREST at <c>{Url}/rest/v1/{GarageTable}</c>.
    /// </summary>
    public class VrAuthConfig
    {
        [JsonProperty("provider")] public string Provider;
        [JsonProperty("url")] public string Url;
        [JsonProperty("anonKey")] public string AnonKey;
        [JsonProperty("signIn")] public string SignIn;       // "email_otp"
        [JsonProperty("garageTable")] public string GarageTable;
    }

    /// <summary>A product surfaced in the VR console (serializable subset of the
    /// website Product — no JSX icon).</summary>
    public class VrProduct
    {
        [JsonProperty("slug")] public string Slug;
        [JsonProperty("name")] public string Name;
        [JsonProperty("tag")] public string Tag;
        [JsonProperty("desc")] public string Desc;
        [JsonProperty("accent")] public string Accent;       // "cyan" | "violet"
        [JsonProperty("category")] public string Category;
        [JsonProperty("hero")] public string Hero;
        [JsonProperty("highlights")] public List<string> Highlights = new List<string>();
        [JsonProperty("specs")] public List<VrProductSpec> Specs = new List<VrProductSpec>();
        [JsonProperty("app")] public VrAppLink App;
    }

    public class VrProductSpec
    {
        [JsonProperty("label")] public string Label;
        [JsonProperty("value")] public string Value;
    }

    public class VrAppLink
    {
        [JsonProperty("href")] public string Href;
        [JsonProperty("label")] public string Label;
    }
}
