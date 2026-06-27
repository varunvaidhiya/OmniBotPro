using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using OmniBot.VR.Core.Platform;

namespace OmniBot.VR.UI.Garage
{
    /// <summary>
    /// The teleop garage — the user's saved robots, pulled from Supabase
    /// (`user_robots`) the moment they open Pilot. This is the connected
    /// experience in action: a robot added on the website or Android app is
    /// already here, no re-entry. Selecting one will start teleoperation with the
    /// robot's profile (Phase 2).
    /// </summary>
    public class GaragePanelController : MonoBehaviour
    {
        [SerializeField] private Transform listParent;     // a layout group
        [SerializeField] private RobotCardView cardPrefab;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button refreshButton;

        private readonly List<GameObject> _spawned = new List<GameObject>();

        private void OnEnable()
        {
            if (refreshButton != null) refreshButton.onClick.AddListener(Reload);
            Reload();
        }

        private void OnDisable()
        {
            if (refreshButton != null) refreshButton.onClick.RemoveListener(Reload);
        }

        public void Reload()
        {
            SetStatus("Loading your garage…");
            Clear();

            var catalog = OhhoCatalog.Instance;
            if (catalog == null) { SetStatus("Catalog unavailable."); return; }

            // Make sure the catalog is present so saved ids resolve to real models,
            // then pull the user's robots from Supabase.
            if (catalog.IsLoaded) FetchRobots();
            else
            {
                catalog.OnLoaded += OnCatalogReady;
                catalog.EnsureLoaded();
            }
        }

        private void OnCatalogReady(VrCatalog _)
        {
            if (OhhoCatalog.Instance != null) OhhoCatalog.Instance.OnLoaded -= OnCatalogReady;
            FetchRobots();
        }

        private void FetchRobots()
        {
            var garage = GarageClient.Instance;
            if (garage == null) { SetStatus("Garage client unavailable."); return; }

            garage.GetUserRobots((robots, err) =>
            {
                if (err != null) { SetStatus(err); return; }
                if (robots == null || robots.Count == 0)
                {
                    SetStatus("No robots yet. Add one on ohho-robotics.com or the app — it'll appear here.");
                    return;
                }
                Populate(robots);
            });
        }

        private void Populate(List<UserRobot> robots)
        {
            Clear();
            var catalog = OhhoCatalog.Instance;
            foreach (var robot in robots)
            {
                var resolved = catalog != null ? catalog.Resolve(robot) : null;
                if (resolved == null) continue;
                var card = Instantiate(cardPrefab, listParent);
                card.Bind(resolved, OnRobotSelected);
                _spawned.Add(card.gameObject);
            }
            SetStatus($"{robots.Count} robot{(robots.Count == 1 ? "" : "s")} in your garage");
        }

        private void OnRobotSelected(GarageRobot robot)
        {
            // Phase 2: hand this robot's profile (drive kind + arm DOF) to the
            // control layer and start teleoperation. For now, confirm the pick.
            SetStatus($"Selected {robot.DisplayName} — teleop wiring lands in Phase 2.");
        }

        private void Clear()
        {
            foreach (var go in _spawned) if (go != null) Destroy(go);
            _spawned.Clear();
        }

        private void SetStatus(string s)
        {
            if (statusText != null) statusText.text = s;
        }
    }
}
