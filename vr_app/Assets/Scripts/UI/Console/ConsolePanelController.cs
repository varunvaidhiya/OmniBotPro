using TMPro;
using UnityEngine;
using OmniBot.VR.App;
using OmniBot.VR.Core.Platform;

namespace OmniBot.VR.UI.Console
{
    /// <summary>
    /// The post-login console — the product grid the user lands on, mirroring the
    /// website console + homepage products. It shows ONLY the VR-headset products
    /// from the manifest (today: OhhO Pilot); more appear automatically when
    /// tagged `vr` on the website. Picking Pilot opens the teleop garage.
    /// </summary>
    public class ConsolePanelController : MonoBehaviour
    {
        [SerializeField] private OhhoVrApp app;
        [SerializeField] private Transform gridParent;       // a layout group
        [SerializeField] private ProductCardView cardPrefab;
        [SerializeField] private TMP_Text headerText;

        private bool _built;

        private void OnEnable()
        {
            if (app == null) app = FindObjectOfType<OhhoVrApp>();
            BuildOnceWhenReady();
        }

        private void BuildOnceWhenReady()
        {
            if (_built) return;
            var platform = OhhoPlatform.Instance;
            if (platform == null) return;

            if (platform.Manifest != null) Build();
            else platform.OnLoaded += _ => Build(); // rebuild once the manifest arrives
        }

        private void Build()
        {
            if (_built || gridParent == null || cardPrefab == null) return;
            _built = true;

            if (headerText != null) headerText.text = "Your VR consoles";

            foreach (var product in OhhoPlatform.Instance.Products)
            {
                var card = Instantiate(cardPrefab, gridParent);
                card.Bind(product, OnProductSelected);
            }
        }

        private void OnProductSelected(VrProduct product)
        {
            // Today only Pilot (teleoperation) is a headset product → open the
            // garage to pick which robot to drive. Future VR products route here.
            if (product.Slug == "pilot" && app != null) app.ShowGarage();
        }
    }
}
