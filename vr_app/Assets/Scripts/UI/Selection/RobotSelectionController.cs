using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using OmniBot.VR.Core.Platform;

namespace OmniBot.VR.UI.Selection
{
    /// <summary>
    /// The spatial "add robot" flow — a four-step wizard mirroring the
    /// website's <c>RobotSelector.tsx</c>: <b>categories → types → models →
    /// name → confirm</b>. The user picks a category (drones, wheeled, legged…),
    /// then a robot type within it (OmniBot Pro, TurtleBot 4…), then a hardware
    /// model, then names their robot and confirms. On confirm, the selection is
    /// written to Supabase <c>user_robots</c> via <see cref="GarageClient.AddUserRobot"/>
    /// — the same table the website and Android app write to — so the new robot
    /// immediately appears in the garage, ready to teleoperate.
    ///
    /// Each step is a panel + a card prefab + a list container, all assigned in
    /// the inspector. The controller hides/shows the step panels and spawns the
    /// cards from the catalog. Back and Cancel buttons step backward or exit.
    /// </summary>
    public class RobotSelectionController : MonoBehaviour
    {
        [Header("Step panels (assign in the scene)")]
        [SerializeField] private GameObject categoryStep;
        [SerializeField] private GameObject typeStep;
        [SerializeField] private GameObject modelStep;
        [SerializeField] private GameObject nameStep;
        [SerializeField] private GameObject confirmStep;

        [Header("List containers")]
        [SerializeField] private Transform categoryListParent;
        [SerializeField] private Transform typeListParent;
        [SerializeField] private Transform modelListParent;

        [Header("Card prefabs")]
        [SerializeField] private CategoryCardView categoryCardPrefab;
        [SerializeField] private TypeCardView typeCardPrefab;
        [SerializeField] private ModelCardView modelCardPrefab;

        [Header("Navigation")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TMP_Text stepHeader;

        [Header("Name step")]
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private Button nameNextButton;

        [Header("Confirm step")]
        [SerializeField] private TMP_Text confirmSummary;
        [SerializeField] private Button confirmButton;

        [Header("Status")]
        [SerializeField] private TMP_Text statusText;

        // ── State ────────────────────────────────────────────────────────────────
        private readonly SelectionState _state = new SelectionState();
        private readonly List<GameObject> _spawned = new List<GameObject>();

        /// <summary>Fired when a robot is successfully added (the garage should reload).</summary>
        public event Action<UserRobot> OnRobotAdded;

        /// <summary>Fired when the user cancels / exits the flow.</summary>
        public event Action OnCancelled;

        private void OnEnable()
        {
            if (backButton != null) backButton.onClick.AddListener(Back);
            if (cancelButton != null) cancelButton.onClick.AddListener(Cancel);
            if (nameNextButton != null) nameNextButton.onClick.AddListener(OnNameNext);
            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
        }

        private void OnDisable()
        {
            if (backButton != null) backButton.onClick.RemoveListener(Back);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(Cancel);
            if (nameNextButton != null) nameNextButton.onClick.RemoveListener(OnNameNext);
            if (confirmButton != null) confirmButton.onClick.RemoveListener(OnConfirm);
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>Open the selection flow at the first step (categories).</summary>
        public void Open()
        {
            _state.Step = SelectionStep.Category;
            _state.CategoryId = null;
            _state.RobotTypeId = null;
            _state.HardwareModelId = null;
            _state.RobotName = null;
            ShowCurrentStep();
        }

        // ── Step transitions ──────────────────────────────────────────────────────

        private void ShowCurrentStep()
        {
            SetPanelActive(categoryStep, _state.Step == SelectionStep.Category);
            SetPanelActive(typeStep, _state.Step == SelectionStep.Type);
            SetPanelActive(modelStep, _state.Step == SelectionStep.Model);
            SetPanelActive(nameStep, _state.Step == SelectionStep.Name);
            SetPanelActive(confirmStep, _state.Step == SelectionStep.Confirm);

            if (stepHeader != null)
                stepHeader.text = _state.Step switch
                {
                    SelectionStep.Category => "1. Pick a category",
                    SelectionStep.Type => "2. Pick a robot type",
                    SelectionStep.Model => "3. Pick a hardware model",
                    SelectionStep.Name => "4. Name your robot",
                    SelectionStep.Confirm => "5. Confirm",
                    _ => "Add robot",
                };

            if (backButton != null)
                backButton.gameObject.SetActive(_state.Step != SelectionStep.Category);

            switch (_state.Step)
            {
                case SelectionStep.Category: PopulateCategories(); break;
                case SelectionStep.Type: PopulateTypes(); break;
                case SelectionStep.Model: PopulateModels(); break;
                case SelectionStep.Name: PrepareNameStep(); break;
                case SelectionStep.Confirm: PrepareConfirmStep(); break;
            }
        }

        private void OnCategoryPicked(VrCategory cat)
        {
            _state.CategoryId = cat?.Id;
            _state.Step = SelectionStep.Type;
            ShowCurrentStep();
        }

        private void OnTypePicked(VrRobotType type)
        {
            _state.RobotTypeId = type?.Id;
            _state.Step = SelectionStep.Model;
            ShowCurrentStep();
        }

        private void OnModelPicked(VrHardwareModel model)
        {
            _state.HardwareModelId = model?.Id;
            // Pre-fill the name with the model name
            if (string.IsNullOrEmpty(_state.RobotName))
                _state.RobotName = model?.Name;
            _state.Step = SelectionStep.Name;
            ShowCurrentStep();
        }

        private void OnNameNext()
        {
            _state.RobotName = nameInput != null ? nameInput.text.Trim() : "";
            if (string.IsNullOrEmpty(_state.RobotName))
            {
                SetStatus("Please enter a name for your robot.");
                return;
            }
            _state.Step = SelectionStep.Confirm;
            ShowCurrentStep();
        }

        private void OnConfirm()
        {
            SetStatus("Saving to your garage…");
            if (confirmButton != null) confirmButton.interactable = false;

            var garage = GarageClient.Instance;
            if (garage == null)
            {
                SetStatus("Garage client unavailable.");
                if (confirmButton != null) confirmButton.interactable = true;
                return;
            }

            garage.AddUserRobot(_state.RobotName, _state.RobotTypeId, _state.HardwareModelId,
                (robot, err) =>
                {
                    if (confirmButton != null) confirmButton.interactable = true;
                    if (err != null)
                    {
                        SetStatus($"Failed: {err}");
                        return;
                    }
                    SetStatus($"Added {robot?.Name ?? _state.RobotName} to your garage.");
                    Debug.Log($"[RobotSelectionController] Robot added: {robot?.Id}");
                    OnRobotAdded?.Invoke(robot);
                    // Stay on confirm so the user sees the success message;
                    // the garage panel (subscriber) will close us + reload.
                });
        }

        private void Back()
        {
            switch (_state.Step)
            {
                case SelectionStep.Type:
                    _state.Step = SelectionStep.Category; break;
                case SelectionStep.Model:
                    _state.Step = SelectionStep.Type; break;
                case SelectionStep.Name:
                    _state.Step = SelectionStep.Model; break;
                case SelectionStep.Confirm:
                    _state.Step = SelectionStep.Name; break;
                default: return; // can't go back from Category
            }
            ShowCurrentStep();
        }

        private void Cancel()
        {
            SetPanelActive(categoryStep, false);
            SetPanelActive(typeStep, false);
            SetPanelActive(modelStep, false);
            SetPanelActive(nameStep, false);
            SetPanelActive(confirmStep, false);
            OnCancelled?.Invoke();
        }

        // ── Population ────────────────────────────────────────────────────────────

        private void PopulateCategories()
        {
            ClearSpawned();
            var catalog = OhhoCatalog.Instance;
            if (catalog == null || !catalog.IsLoaded) { SetStatus("Catalog not loaded."); return; }

            foreach (var cat in catalog.Categories)
            {
                if (categoryCardPrefab == null || categoryListParent == null) continue;
                var card = Instantiate(categoryCardPrefab, categoryListParent);
                card.Bind(cat, OnCategoryPicked);
                _spawned.Add(card.gameObject);
            }
            SetStatus("");
        }

        private void PopulateTypes()
        {
            ClearSpawned();
            var catalog = OhhoCatalog.Instance;
            if (catalog == null || catalog.Catalog == null) { SetStatus("Catalog unavailable."); return; }

            foreach (var type in catalog.Catalog.RobotTypes)
            {
                if (type.Category != _state.CategoryId) continue;
                if (typeCardPrefab == null || typeListParent == null) continue;
                var card = Instantiate(typeCardPrefab, typeListParent);
                card.Bind(type, OnTypePicked);
                _spawned.Add(card.gameObject);
            }
            SetStatus("");
        }

        private void PopulateModels()
        {
            ClearSpawned();
            var catalog = OhhoCatalog.Instance;
            var type = catalog?.FindRobotType(_state.RobotTypeId);
            if (type == null || type.HardwareModels == null) { SetStatus("No models for this type."); return; }

            foreach (var model in type.HardwareModels)
            {
                if (modelCardPrefab == null || modelListParent == null) continue;
                var card = Instantiate(modelCardPrefab, modelListParent);
                card.Bind(model, OnModelPicked);
                _spawned.Add(card.gameObject);
            }
            SetStatus("");
        }

        private void PrepareNameStep()
        {
            if (nameInput != null && !string.IsNullOrEmpty(_state.RobotName))
                nameInput.text = _state.RobotName;
            SetStatus("");
        }

        private void PrepareConfirmStep()
        {
            if (confirmSummary != null)
            {
                var catalog = OhhoCatalog.Instance;
                var type = catalog?.FindRobotType(_state.RobotTypeId);
                var model = catalog?.FindHardwareModel(_state.RobotTypeId, _state.HardwareModelId);
                var cat = catalog?.FindCategory(_state.CategoryId);
                confirmSummary.text =
                    $"Name: {_state.RobotName}\n" +
                    $"Category: {cat?.Label ?? _state.CategoryId}\n" +
                    $"Type: {type?.Name ?? _state.RobotTypeId}\n" +
                    $"Model: {model?.Name ?? _state.HardwareModelId}";
            }
            SetStatus("");
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void ClearSpawned()
        {
            foreach (var go in _spawned) if (go != null) Destroy(go);
            _spawned.Clear();
        }

        private static void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null) panel.SetActive(active);
        }

        private void SetStatus(string s)
        {
            if (statusText != null) statusText.text = s ?? "";
        }
    }
}
