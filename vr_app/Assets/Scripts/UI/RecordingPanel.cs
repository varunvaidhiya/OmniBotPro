using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using OmniBot.VR.Core;
using OmniBot.VR.Recording;

namespace OmniBot.VR.UI
{
    /// <summary>
    /// Recording control panel for VR.
    ///
    /// Features:
    ///   - Start / Stop &amp; Save / Discard buttons
    ///   - Episode name input (auto-filled with timestamp)
    ///   - Live stats: elapsed time, frame count
    ///   - Pulsing red recording indicator
    ///   - List of saved episode JSONL files with sizes
    ///   - "Export to Robot" button — POSTs each file to the VR bridge HTTP endpoint
    /// </summary>
    public class RecordingPanel : MonoBehaviour
    {
        // ── Inspector — Controls ──────────────────────────────────────────────
        [Header("Recording Controls")]
        [SerializeField] private TMP_InputField episodeNameInput;
        [SerializeField] private Button         btnStartRecording;
        [SerializeField] private Button         btnStopSave;
        [SerializeField] private Button         btnDiscard;

        // ── Inspector — Stats ─────────────────────────────────────────────────
        [Header("Live Stats")]
        [SerializeField] private TMP_Text   elapsedTimeLabel;
        [SerializeField] private TMP_Text   frameCountLabel;
        [SerializeField] private GameObject recordingIndicator;   // pulsing red dot

        // ── Inspector — Episode List ──────────────────────────────────────────
        [Header("Episode List")]
        [SerializeField] private Transform     episodeListContainer;
        [SerializeField] private GameObject    episodeListItemPrefab; // has TMP_Text + Button
        [SerializeField] private Button        btnExportAll;
        [SerializeField] private TMP_Text      exportStatusLabel;

        // ── Inspector — Export ────────────────────────────────────────────────
        [Header("Export Settings")]
        [SerializeField] private int   bridgeHttpPort = 8765;

        // ── State ─────────────────────────────────────────────────────────────
        private float        _indicatorPulseTimer = 0f;
        private const float  PulsePeriod          = 0.75f; // seconds per on/off cycle
        private DatasetRecorder _recorder;

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Start()
        {
            _recorder = FindObjectOfType<DatasetRecorder>();
            if (_recorder == null)
            {
                _recorder = gameObject.AddComponent<DatasetRecorder>();
                Debug.LogWarning("[RecordingPanel] DatasetRecorder not found — created one.");
            }

            // Auto-fill episode name
            RefreshEpisodeName();

            // Wire buttons
            if (btnStartRecording) btnStartRecording.onClick.AddListener(OnStartClicked);
            if (btnStopSave)       btnStopSave.onClick.AddListener(OnStopSaveClicked);
            if (btnDiscard)        btnDiscard.onClick.AddListener(OnDiscardClicked);
            if (btnExportAll)      btnExportAll.onClick.AddListener(OnExportAllClicked);

            // Wire episode manager events
            EpisodeManager.Instance.OnEpisodeStarted  += _ => OnStateChanged();
            EpisodeManager.Instance.OnEpisodeStopped  += _ => { OnStateChanged(); RefreshEpisodeList(); };
            EpisodeManager.Instance.OnEpisodeDiscarded += _ => OnStateChanged();

            // Initial UI state
            OnStateChanged();
            RefreshEpisodeList();

            if (exportStatusLabel != null) exportStatusLabel.gameObject.SetActive(false);
        }

        private void Update()
        {
            // Update stats at display rate
            UpdateLiveStats();

            // Pulse the recording indicator
            if (recordingIndicator != null)
            {
                bool isRecording = EpisodeManager.Instance.State == EpisodeManager.EpisodeState.Recording;
                if (isRecording)
                {
                    _indicatorPulseTimer += Time.deltaTime;
                    bool on = ((int)(_indicatorPulseTimer / (PulsePeriod * 0.5f)) % 2) == 0;
                    recordingIndicator.SetActive(on);
                }
                else
                {
                    recordingIndicator.SetActive(false);
                    _indicatorPulseTimer = 0f;
                }
            }
        }

        // ── Button handlers ──────────────────────────────────────────────────

        private void OnStartClicked()
        {
            string epName = episodeNameInput != null ? episodeNameInput.text.Trim() : "";
            EpisodeManager.Instance.StartEpisode(epName);
        }

        private void OnStopSaveClicked()
        {
            EpisodeManager.Instance.StopEpisode();
        }

        private void OnDiscardClicked()
        {
            EpisodeManager.Instance.DiscardEpisode();
        }

        private void OnExportAllClicked()
        {
            StartCoroutine(ExportAllEpisodes());
        }

        // ── UI helpers ────────────────────────────────────────────────────────

        private void OnStateChanged()
        {
            var state = EpisodeManager.Instance.State;
            bool idle      = state == EpisodeManager.EpisodeState.Idle;
            bool recording = state == EpisodeManager.EpisodeState.Recording ||
                             state == EpisodeManager.EpisodeState.Paused;

            if (btnStartRecording) btnStartRecording.interactable = idle;
            if (btnStopSave)       btnStopSave.interactable       = recording;
            if (btnDiscard)        btnDiscard.interactable         = recording;

            if (idle) RefreshEpisodeName();
        }

        private void RefreshEpisodeName()
        {
            if (episodeNameInput != null)
            {
                long ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                episodeNameInput.text = $"ep_{ts}";
            }
        }

        private void UpdateLiveStats()
        {
            if (_recorder == null) return;
            var (frames, duration, _) = _recorder.GetRecordingStats();

            if (elapsedTimeLabel != null)
                elapsedTimeLabel.text = $"Time: {TimeSpan.FromSeconds(duration):mm\\:ss\\.f}";

            if (frameCountLabel != null)
                frameCountLabel.text = $"Frames: {frames}";
        }

        private void RefreshEpisodeList()
        {
            if (episodeListContainer == null) return;

            // Clear existing items
            foreach (Transform child in episodeListContainer)
                Destroy(child.gameObject);

            string dir = Path.Combine(Application.persistentDataPath, "recordings");
            if (!Directory.Exists(dir)) return;

            string[] files = Directory.GetFiles(dir, "*.jsonl");
            Array.Sort(files); // alphabetical / by name

            foreach (string file in files)
            {
                var info     = new FileInfo(file);
                string label = $"{info.Name}  ({FormatFileSize(info.Length)})";

                if (episodeListItemPrefab != null)
                {
                    GameObject item = Instantiate(episodeListItemPrefab, episodeListContainer);
                    var textComp = item.GetComponentInChildren<TMP_Text>();
                    if (textComp != null) textComp.text = label;

                    // Wire per-item export button if present
                    var btn = item.GetComponentInChildren<Button>();
                    if (btn != null)
                    {
                        string capturedPath = file;
                        btn.onClick.AddListener(() => StartCoroutine(ExportSingleEpisode(capturedPath)));
                    }
                }
            }
        }

        // ── Export coroutines ────────────────────────────────────────────────

        private IEnumerator ExportAllEpisodes()
        {
            string dir = Path.Combine(Application.persistentDataPath, "recordings");
            if (!Directory.Exists(dir)) yield break;

            string[] files = Directory.GetFiles(dir, "*.jsonl");
            ShowExportStatus($"Exporting {files.Length} file(s)...");

            int succeeded = 0;
            foreach (string file in files)
            {
                yield return ExportSingleEpisode(file, status => { });
                succeeded++;
            }

            ShowExportStatus($"Exported {succeeded}/{files.Length} episodes.", hideAfter: 4f);
            RefreshEpisodeList();
        }

        private IEnumerator ExportSingleEpisode(string filePath, Action<string> statusCallback = null)
        {
            string robotIp  = PlayerPrefs.GetString("robot_ip", "192.168.1.101");
            string endpoint = $"http://{robotIp}:{bridgeHttpPort}/upload_episode";

            byte[] fileData;
            try
            {
                fileData = File.ReadAllBytes(filePath);
            }
            catch (Exception ex)
            {
                string errMsg = $"Failed to read {Path.GetFileName(filePath)}: {ex.Message}";
                Debug.LogError($"[RecordingPanel] {errMsg}");
                statusCallback?.Invoke(errMsg);
                ShowExportStatus(errMsg);
                yield break;
            }

            string filename = Path.GetFileName(filePath);

            WWWForm form = new WWWForm();
            form.AddBinaryData("file", fileData, filename, "application/jsonl");

            using (UnityWebRequest req = UnityWebRequest.Post(endpoint, form))
            {
                req.timeout = 30;
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    string msg = $"Exported: {filename}";
                    Debug.Log($"[RecordingPanel] {msg}");
                    statusCallback?.Invoke(msg);
                    ShowExportStatus(msg);
                }
                else
                {
                    string errMsg = $"Export failed ({filename}): {req.error}";
                    Debug.LogWarning($"[RecordingPanel] {errMsg}");
                    statusCallback?.Invoke(errMsg);
                    ShowExportStatus(errMsg);
                }
            }
        }

        private Coroutine _exportStatusCoroutine;

        private void ShowExportStatus(string message, float hideAfter = 3f)
        {
            if (exportStatusLabel == null) return;
            exportStatusLabel.text = message;
            exportStatusLabel.gameObject.SetActive(true);

            if (_exportStatusCoroutine != null) StopCoroutine(_exportStatusCoroutine);
            _exportStatusCoroutine = StartCoroutine(HideExportStatus(hideAfter));
        }

        private IEnumerator HideExportStatus(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (exportStatusLabel != null) exportStatusLabel.gameObject.SetActive(false);
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024)      return $"{bytes} B";
            if (bytes < 1048576)   return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / 1048576.0:F1} MB";
        }
    }
}
