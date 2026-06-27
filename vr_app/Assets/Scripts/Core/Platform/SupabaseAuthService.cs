using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace OmniBot.VR.Core.Platform
{
    /// <summary>
    /// In-headset sign-in against the same Supabase project the website uses.
    ///
    /// The site signs in with an email magic link; in a headset, typing a link is
    /// awkward, so we use the email **OTP** path of the very same GoTrue backend:
    /// the user enters their email, receives a short code, and types it. The
    /// resulting access token is then used as a bearer for PostgREST (the user's
    /// garage of robots) — exactly the same identity the website grants.
    ///
    /// Configure from the manifest's <see cref="VrAuthConfig"/> once it loads.
    /// </summary>
    public class SupabaseAuthService : MonoBehaviour
    {
        public static SupabaseAuthService Instance { get; private set; }

        private const string RefreshKey = "ohho.supabase.refresh_token";

        private string _url;      // https://<ref>.supabase.co
        private string _anonKey;

        public string AccessToken { get; private set; }
        public string RefreshToken { get; private set; }
        public string UserId { get; private set; }
        public string UserEmail { get; private set; }
        public bool IsSignedIn => !string.IsNullOrEmpty(AccessToken);

        /// <summary>Fired whenever sign-in state changes (true = signed in).</summary>
        public event Action<bool> OnAuthChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>Point the service at a Supabase project (from the manifest).</summary>
        public void Configure(VrAuthConfig auth)
        {
            if (auth == null) return;
            _url = auth.Url?.TrimEnd('/');
            _anonKey = auth.AnonKey;
        }

        /// <summary>True once <see cref="Configure"/> has supplied a project.</summary>
        public bool IsConfigured => !string.IsNullOrEmpty(_url) && !string.IsNullOrEmpty(_anonKey);

        // ── Step 1: email a one-time code ─────────────────────────────────────
        public void RequestCode(string email, Action<bool, string> done)
        {
            if (!IsConfigured) { done?.Invoke(false, "Auth not configured yet."); return; }
            StartCoroutine(PostJson(
                $"{_url}/auth/v1/otp",
                JsonConvert.SerializeObject(new OtpRequest { email = email, create_user = true }),
                authBearer: null,
                (ok, body, err) => done?.Invoke(ok, ok ? null : Describe(body, err))));
        }

        // ── Step 2: verify the code → session ─────────────────────────────────
        public void VerifyCode(string email, string code, Action<bool, string> done)
        {
            if (!IsConfigured) { done?.Invoke(false, "Auth not configured yet."); return; }
            StartCoroutine(PostJson(
                $"{_url}/auth/v1/verify",
                JsonConvert.SerializeObject(new VerifyRequest { type = "email", email = email, token = code }),
                authBearer: null,
                (ok, body, err) =>
                {
                    if (ok && TryApplySession(body)) done?.Invoke(true, null);
                    else done?.Invoke(false, Describe(body, err));
                }));
        }

        /// <summary>Restore a session from a persisted refresh token, if any.</summary>
        public void RestoreSession(Action<bool> done = null)
        {
            var refresh = PlayerPrefs.GetString(RefreshKey, null);
            if (string.IsNullOrEmpty(refresh) || !IsConfigured) { done?.Invoke(false); return; }
            StartCoroutine(PostJson(
                $"{_url}/auth/v1/token?grant_type=refresh_token",
                JsonConvert.SerializeObject(new RefreshRequest { refresh_token = refresh }),
                authBearer: null,
                (ok, body, _) => done?.Invoke(ok && TryApplySession(body))));
        }

        public void SignOut()
        {
            AccessToken = RefreshToken = UserId = UserEmail = null;
            PlayerPrefs.DeleteKey(RefreshKey);
            PlayerPrefs.Save();
            OnAuthChanged?.Invoke(false);
        }

        // ── internals ─────────────────────────────────────────────────────────

        private bool TryApplySession(string body)
        {
            try
            {
                var s = JsonConvert.DeserializeObject<SessionResponse>(body);
                if (s == null || string.IsNullOrEmpty(s.access_token)) return false;
                AccessToken = s.access_token;
                RefreshToken = s.refresh_token;
                UserId = s.user?.id;
                UserEmail = s.user?.email;
                if (!string.IsNullOrEmpty(RefreshToken))
                {
                    PlayerPrefs.SetString(RefreshKey, RefreshToken);
                    PlayerPrefs.Save();
                }
                OnAuthChanged?.Invoke(true);
                return true;
            }
            catch { return false; }
        }

        private IEnumerator PostJson(string url, string json, string authBearer, Action<bool, string, string> done)
        {
            using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("apikey", _anonKey);
            req.SetRequestHeader("Authorization", $"Bearer {authBearer ?? _anonKey}");

            yield return req.SendWebRequest();

            var ok = req.result == UnityWebRequest.Result.Success;
            done?.Invoke(ok, req.downloadHandler != null ? req.downloadHandler.text : null, req.error);
        }

        private static string Describe(string body, string err)
        {
            if (!string.IsNullOrEmpty(body))
            {
                try
                {
                    var e = JsonConvert.DeserializeObject<ErrorResponse>(body);
                    var msg = e?.error_description ?? e?.msg ?? e?.error;
                    if (!string.IsNullOrEmpty(msg)) return msg;
                }
                catch { /* fall through */ }
            }
            return string.IsNullOrEmpty(err) ? "Sign-in failed." : err;
        }

        // ── wire models (snake_case to match GoTrue JSON) ─────────────────────
        [Serializable] private class OtpRequest { public string email; public bool create_user; }
        [Serializable] private class VerifyRequest { public string type; public string email; public string token; }
        [Serializable] private class RefreshRequest { public string refresh_token; }

        private class SessionResponse
        {
            public string access_token;
            public string refresh_token;
            public int expires_in;
            public string token_type;
            public SupabaseUser user;
        }

        private class SupabaseUser { public string id; public string email; }
        private class ErrorResponse { public string error; public string error_description; public string msg; }
    }
}
