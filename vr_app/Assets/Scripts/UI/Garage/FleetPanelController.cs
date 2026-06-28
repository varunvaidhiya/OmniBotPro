using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using OmniBot.VR.Core.Platform;
using OmniBot.VR.Control;
using OmniBot.VR.UI.Selection;

namespace OmniBot.VR.UI.Garage
{
    /// <summary>
    /// Enhanced garage — the "saved-robot fleet" view. Extends the
    /// <see cref="GaragePanelController"/> with a quick-resume banner for the
    /// last-driven robot and per-robot status badges (active / offline /
    /// simulated). The quick-resume button skips the garage browse and jumps
    /// straight into teleop with the last robot, applying its saved calibration.
    ///
    /// Designed to sit alongside <see cref="GaragePanelController"/> on the same
    /// panel (or replace it) — both fire <c>RobotPicked</c>, so
    /// <see cref="OmniBot.VR.App.OhhoVrApp"/> routes either to teleop the same way.
    /// </summary>
    public class FleetPanelController : MonoBehaviour
    {
        [SerializeField] private Transform listParent;
        [SerializeField] private RobotCardView cardPrefab;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button addRobotButton;

        [Header("Quick-resume")]
        [SerializeField] private GameObject quickResumeBanner;
        [SerializeField] private TMP_Text quickResumeLabel;
        [SerializeField] private Button quickResumeButton;

        [Header("Add-robot flow (Phase 1)")]
        [Tooltip("The robot selection controller. Opened when Add Robot is pressed.")]
        [SerializeField] private RobotSelectionController selectionController;
        [Tooltip("The selection flow panel (hidden until Add Robot is pressed).")]
        [SerializeField] private GameObject selectionPanel;

        private readonly List<GameObject> _spawned = new List<GameObject>();

        /// <summary>Fired when the user picks a robot to teleoperate.</summary>
        public event Action<GarageRobot> RobotPicked;

        /// <summary>Fired when the user picks the quick-resume (last) robot.</summary>
        public event Action QuickResume;

        private void OnEnable()
        {
            if (refreshButton != null) refreshButton.onClick.AddListener(Reload);
            if (quickResumeButton != null) quickResumeButton.onClick.AddListener(() => QuickResume?.Invoke());
            if (addRobotButton != null) addRobotButton.onClick.AddListener(OpenAddRobot);
            if (selectionController != null)
            {
                selectionController.OnRobotAdded += OnRobotAdded;
                selectionController.OnCancelled += CloseAddRobot;
            }
            if (selectionPanel != null) selectionPanel.SetActive(false);
            UpdateQuickResumeBanner();
            Reload();
        }

        private void OnDisable()
        {
            if (refreshButton != null) refreshButton.onClick.RemoveListener(Reload);
            if (addRobotButton != null) addRobotButton.onClick.RemoveListener(OpenAddRobot);
            if (selectionController != null)
            {
                selectionController.OnRobotAdded -= OnRobotAdded;
                selectionController.OnCancelled -= CloseAddRobot;
            }
        }

        public void Reload()
        {
            SetStatus("Loading your fleet…");
            Clear();

            var catalog = OhhoCatalog.Instance;
            if (catalog == null) { SetStatus("Catalog unavailable."); return; }

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
            int activeCount = 0;
            foreach (var robot in robots)
            {
                var resolved = catalog != null ? catalog.Resolve(robot) : null;
                if (resolved == null) continue;
                var card = Instantiate(cardPrefab, listParent);
                card.Bind(resolved, OnRobotSelected);
                _spawned.Add(card.gameObject);
                if (robot.Status == "active") activeCount++;
            }
            SetStatus($"{robots.Count} robot{(robots.Count == 1 ? "" : "s")} · {activeCount} active");
        }

        private void OnRobotSelected(GarageRobot robot)
        {
            SetStatus($"Selected {robot.DisplayName} — starting teleop…");
            RobotPicked?.Invoke(robot);
        }

        // ── Quick-resume banner ───────────────────────────────────────────────────

        /// <summary>
        /// Show or hide the quick-resume banner based on whether a last robot is
        /// saved. Called on enable and after a robot is picked.
        /// </summary>
        private void UpdateQuickResumeBanner()
        {
            if (quickResumeBanner == null) return;
            bool has = LastRobotStore.HasLastRobot;
            quickResumeBanner.SetActive(has);
            if (has && quickResumeLabel != null)
                quickResumeLabel.text = $"Resume {LastRobotStore.LastRobotName} ({LastRobotStore.LastRobotIp})";
        }

        /// <summary>Refresh the banner after a teleop session ends.</summary>
        public void RefreshQuickResume() => UpdateQuickResumeBanner();

        private void Clear()
        {
            foreach (var go in _spawned) if (go != null) Destroy(go);
            _spawned.Clear();
        }

        private void SetStatus(string s)
        {
            if (statusText != null) statusText.text = s;
        }

        // ── Phase 1: add-robot flow ──────────────────────────────────────────────

        private void OpenAddRobot()
        {
            if (selectionPanel != null) selectionPanel.SetActive(true);
            selectionController?.Open();
        }

        private void CloseAddRobot()
        {
            if (selectionPanel != null) selectionPanel.SetActive(false);
        }

        private void OnRobotAdded(UserRobot robot)
        {
            CloseAddRobot();
            Reload();
        }
    }
}
