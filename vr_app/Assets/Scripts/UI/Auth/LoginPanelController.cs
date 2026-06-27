using TMPro;
using UnityEngine;
using UnityEngine.UI;
using OmniBot.VR.Core;
using OmniBot.VR.Core.Platform;

namespace OmniBot.VR.UI.Auth
{
    /// <summary>
    /// In-headset sign-in, mirroring website/app/login/page.tsx ("Sign in to OhhO").
    /// The site uses an email magic link; in VR the same Supabase backend issues a
    /// one-time code the user types — see <see cref="SupabaseAuthService"/>.
    ///
    /// Wire the serialized fields to a world-space glass panel themed with
    /// <see cref="OhhoTheme"/> (cyan primary button, JetBrains Mono labels).
    /// </summary>
    public class LoginPanelController : MonoBehaviour
    {
        [Header("Email step")]
        [SerializeField] private TMP_InputField emailField;
        [SerializeField] private Button sendCodeButton;

        [Header("Code step (revealed after a code is sent)")]
        [SerializeField] private GameObject codeStep;
        [SerializeField] private TMP_InputField codeField;
        [SerializeField] private Button verifyButton;

        [Header("Feedback")]
        [SerializeField] private TMP_Text statusText;

        private SupabaseAuthService Auth => SupabaseAuthService.Instance;

        private void OnEnable()
        {
            if (codeStep != null) codeStep.SetActive(false);
            SetStatus("Sign in to OhhO");
            ApplyAccent();
            if (sendCodeButton != null) sendCodeButton.onClick.AddListener(OnSendCode);
            if (verifyButton != null) verifyButton.onClick.AddListener(OnVerify);
        }

        private void OnDisable()
        {
            if (sendCodeButton != null) sendCodeButton.onClick.RemoveListener(OnSendCode);
            if (verifyButton != null) verifyButton.onClick.RemoveListener(OnVerify);
        }

        private void OnSendCode()
        {
            var email = emailField != null ? emailField.text.Trim() : null;
            if (string.IsNullOrEmpty(email)) { SetStatus("Enter your email."); return; }
            if (Auth == null) { SetStatus("Auth unavailable."); return; }

            SetBusy(true, "Sending code…");
            Auth.RequestCode(email, (ok, err) =>
            {
                SetBusy(false);
                if (ok)
                {
                    if (codeStep != null) codeStep.SetActive(true);
                    SetStatus($"Check {email} for a 6-digit code.");
                }
                else SetStatus(err ?? "Couldn't send the code.");
            });
        }

        private void OnVerify()
        {
            var email = emailField != null ? emailField.text.Trim() : null;
            var code = codeField != null ? codeField.text.Trim() : null;
            if (string.IsNullOrEmpty(code)) { SetStatus("Enter the code from your email."); return; }
            if (Auth == null) { SetStatus("Auth unavailable."); return; }

            SetBusy(true, "Verifying…");
            Auth.VerifyCode(email, code, (ok, err) =>
            {
                SetBusy(false);
                // On success, SupabaseAuthService raises OnAuthChanged → OhhoVrApp
                // routes to the console; we only surface failures here.
                if (!ok) SetStatus(err ?? "That code didn't work.");
            });
        }

        private void SetBusy(bool busy, string message = null)
        {
            if (sendCodeButton != null) sendCodeButton.interactable = !busy;
            if (verifyButton != null) verifyButton.interactable = !busy;
            if (message != null) SetStatus(message);
        }

        private void SetStatus(string s)
        {
            if (statusText != null) statusText.text = s;
        }

        private void ApplyAccent()
        {
            // Primary action uses the OhhO cyan, like the website's sign-in button.
            if (sendCodeButton != null && sendCodeButton.targetGraphic != null)
                sendCodeButton.targetGraphic.color = OhhoTheme.Cyan;
        }
    }
}
