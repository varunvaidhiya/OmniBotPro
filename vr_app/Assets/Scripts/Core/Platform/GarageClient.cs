using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace OmniBot.VR.Core.Platform
{
    /// <summary>
    /// Reads (and writes) the signed-in user's garage from Supabase via PostgREST
    /// — the SAME `user_robots` table the website and Android app use. This is
    /// what makes the experience connected: robots saved anywhere appear in the
    /// headset with no re-entry.
    ///
    /// RLS (auth.uid() = user_id) means the bearer token alone scopes every query
    /// to the current user — no client-side user_id filter is needed or trusted.
    /// Mirrors website/lib/garage/client.ts.
    /// </summary>
    public class GarageClient : MonoBehaviour
    {
        public static GarageClient Instance { get; private set; }

        private string _restUrl;   // {project}/rest/v1
        private string _anonKey;
        private string _table = "user_robots";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>Point the client at the Supabase project (from the manifest).</summary>
        public void Configure(VrAuthConfig auth)
        {
            if (auth == null) return;
            _restUrl = $"{auth.Url?.TrimEnd('/')}/rest/v1";
            _anonKey = auth.AnonKey;
            if (!string.IsNullOrEmpty(auth.GarageTable)) _table = auth.GarageTable;
        }

        public bool IsConfigured => !string.IsNullOrEmpty(_restUrl) && !string.IsNullOrEmpty(_anonKey);

        // ── Read the user's garage ────────────────────────────────────────────
        public void GetUserRobots(Action<List<UserRobot>, string> done)
        {
            if (!Ready(out var token, out var err)) { done?.Invoke(null, err); return; }
            var url = $"{_restUrl}/{_table}?select=*&order=created_at.desc";
            StartCoroutine(Send(UnityWebRequest.kHttpVerbGET, url, token, null, (ok, body, e) =>
            {
                if (!ok) { done?.Invoke(null, e); return; }
                try { done?.Invoke(JsonConvert.DeserializeObject<List<UserRobot>>(body) ?? new List<UserRobot>(), null); }
                catch (Exception ex) { done?.Invoke(null, ex.Message); }
            }));
        }

        // ── Add a robot to the garage (mirrors addUserRobot) ──────────────────
        public void AddUserRobot(string name, string robotTypeId, string hardwareModelId, Action<UserRobot, string> done)
        {
            if (!Ready(out var token, out var err)) { done?.Invoke(null, err); return; }
            var userId = SupabaseAuthService.Instance != null ? SupabaseAuthService.Instance.UserId : null;
            var payload = JsonConvert.SerializeObject(new InsertRow
            {
                user_id = userId,
                name = name,
                robot_type_id = robotTypeId,
                hardware_model_id = hardwareModelId,
                status = "draft",
            });
            StartCoroutine(Send(UnityWebRequest.kHttpVerbPOST, $"{_restUrl}/{_table}", token, payload, (ok, body, e) =>
            {
                if (!ok) { done?.Invoke(null, e); return; }
                try
                {
                    // PostgREST returns an array when Prefer: return=representation.
                    var rows = JsonConvert.DeserializeObject<List<UserRobot>>(body);
                    done?.Invoke(rows != null && rows.Count > 0 ? rows[0] : null, null);
                }
                catch (Exception ex) { done?.Invoke(null, ex.Message); }
            }, returnRepresentation: true));
        }

        // ── Delete a robot (mirrors deleteUserRobot) ──────────────────────────
        public void DeleteUserRobot(string id, Action<bool, string> done)
        {
            if (!Ready(out var token, out var err)) { done?.Invoke(false, err); return; }
            var url = $"{_restUrl}/{_table}?id=eq.{UnityWebRequest.EscapeURL(id)}";
            StartCoroutine(Send(UnityWebRequest.kHttpVerbDELETE, url, token, null,
                (ok, _, e) => done?.Invoke(ok, ok ? null : e)));
        }

        // ── internals ─────────────────────────────────────────────────────────

        private bool Ready(out string token, out string error)
        {
            token = null;
            if (!IsConfigured) { error = "Garage not configured."; return false; }
            var auth = SupabaseAuthService.Instance;
            if (auth == null || !auth.IsSignedIn) { error = "Sign in to load your garage."; return false; }
            token = auth.AccessToken;
            error = null;
            return true;
        }

        private IEnumerator Send(string verb, string url, string bearer, string body,
            Action<bool, string, string> done, bool returnRepresentation = false)
        {
            using var req = new UnityWebRequest(url, verb) { downloadHandler = new DownloadHandlerBuffer() };
            if (!string.IsNullOrEmpty(body))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                req.SetRequestHeader("Content-Type", "application/json");
            }
            req.SetRequestHeader("apikey", _anonKey);
            req.SetRequestHeader("Authorization", $"Bearer {bearer}");
            if (returnRepresentation) req.SetRequestHeader("Prefer", "return=representation");

            yield return req.SendWebRequest();

            var ok = req.result == UnityWebRequest.Result.Success;
            var text = req.downloadHandler != null ? req.downloadHandler.text : null;
            done?.Invoke(ok, text, ok ? null : (req.error ?? text));
        }

        [Serializable]
        private class InsertRow
        {
            public string user_id;
            public string name;
            public string robot_type_id;
            public string hardware_model_id;
            public string status;
        }
    }
}
