using System;
using UnityEngine;
using OmniBot.VR.Core;

namespace OmniBot.VR.Recording
{
    /// <summary>
    /// Manages recording episode state and publishes the corresponding
    /// /vr/record_start and /vr/record_stop signals over ROSBridge.
    ///
    /// State machine: Idle → Recording → Saving → Idle
    ///                     ↘ Paused  ↗
    /// </summary>
    public class EpisodeManager : MonoBehaviour
    {
        // ── Episode state ────────────────────────────────────────────────────
        public enum EpisodeState { Idle, Recording, Paused, Saving }

        // ── Singleton ────────────────────────────────────────────────────────
        private static EpisodeManager _instance;
        public static EpisodeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("EpisodeManager");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<EpisodeManager>();
                }
                return _instance;
            }
        }

        // ── Public state ─────────────────────────────────────────────────────
        public EpisodeState State { get; private set; } = EpisodeState.Idle;
        public string CurrentEpisodeName { get; private set; } = "";
        public float  EpisodeDuration    { get; private set; } = 0f;

        // ── Events ────────────────────────────────────────────────────────────
        public event Action<string> OnEpisodeStarted;
        public event Action<string> OnEpisodeStopped;
        public event Action<string> OnEpisodeDiscarded;

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (State == EpisodeState.Recording || State == EpisodeState.Paused)
                EpisodeDuration += (State == EpisodeState.Recording) ? Time.deltaTime : 0f;
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Starts a new recording episode with the given name.
        /// If name is null or empty, auto-generates one as "ep_{UnixTimestamp}".
        /// Ignored if already recording.
        /// </summary>
        public void StartEpisode(string episodeName = null)
        {
            if (State == EpisodeState.Recording || State == EpisodeState.Paused)
            {
                Debug.LogWarning("[EpisodeManager] Already recording — StartEpisode ignored.");
                return;
            }

            // Auto-generate episode name if not provided
            if (string.IsNullOrEmpty(episodeName))
                episodeName = GenerateEpisodeName();

            CurrentEpisodeName = episodeName;
            EpisodeDuration    = 0f;
            State              = EpisodeState.Recording;

            // Publish /vr/record_start with episode name
            if (ROSBridgeClient.Instance.IsConnected)
                ROSBridgeClient.Instance.Publish(RobotConfig.TOPIC_VR_RECORD_START, new StringMsg(episodeName));

            Debug.Log($"[EpisodeManager] Episode started: {episodeName}");
            OnEpisodeStarted?.Invoke(episodeName);
        }

        /// <summary>
        /// Stops and saves the current episode.
        /// Publishes /vr/record_stop with Bool=true (save).
        /// </summary>
        public void StopEpisode()
        {
            if (State != EpisodeState.Recording && State != EpisodeState.Paused)
            {
                Debug.LogWarning("[EpisodeManager] Not recording — StopEpisode ignored.");
                return;
            }

            State = EpisodeState.Saving;
            string name = CurrentEpisodeName;

            if (ROSBridgeClient.Instance.IsConnected)
                ROSBridgeClient.Instance.Publish(RobotConfig.TOPIC_VR_RECORD_STOP, new BoolMsg(true));

            Debug.Log($"[EpisodeManager] Episode stopped (saving): {name} — duration {EpisodeDuration:F1}s");
            OnEpisodeStopped?.Invoke(name);

            // Return to idle immediately (Saving is a transient state)
            State = EpisodeState.Idle;
        }

        /// <summary>
        /// Discards the current episode without saving.
        /// Publishes /vr/record_stop with Bool=false (discard).
        /// </summary>
        public void DiscardEpisode()
        {
            if (State != EpisodeState.Recording && State != EpisodeState.Paused)
            {
                Debug.LogWarning("[EpisodeManager] Not recording — DiscardEpisode ignored.");
                return;
            }

            string name = CurrentEpisodeName;
            State = EpisodeState.Idle;
            EpisodeDuration = 0f;

            if (ROSBridgeClient.Instance.IsConnected)
                ROSBridgeClient.Instance.Publish(RobotConfig.TOPIC_VR_RECORD_STOP, new BoolMsg(false));

            Debug.Log($"[EpisodeManager] Episode discarded: {name}");
            OnEpisodeDiscarded?.Invoke(name);
        }

        /// <summary>Pauses recording (timer stops but episode is not ended).</summary>
        public void PauseEpisode()
        {
            if (State != EpisodeState.Recording) return;
            State = EpisodeState.Paused;
            Debug.Log($"[EpisodeManager] Episode paused: {CurrentEpisodeName}");
        }

        /// <summary>Resumes a paused episode.</summary>
        public void ResumeEpisode()
        {
            if (State != EpisodeState.Paused) return;
            State = EpisodeState.Recording;
            Debug.Log($"[EpisodeManager] Episode resumed: {CurrentEpisodeName}");
        }

        // ── Private helpers ──────────────────────────────────────────────────

        /// <summary>Generates a unique episode name based on the current UTC Unix timestamp.</summary>
        private static string GenerateEpisodeName()
        {
            long unixTs = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return $"ep_{unixTs}";
        }
    }
}
