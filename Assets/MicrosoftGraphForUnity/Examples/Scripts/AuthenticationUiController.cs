using UnityEngine;
using UnityEngine.UI;

namespace MicrosoftGraphForUnity.Examples
{
    /// <summary>
    /// Handles authentication callbacks to represent state in the user interface.
    /// </summary>
    public class AuthenticationUiController : MonoBehaviour
    {
        [SerializeField]
        private MicrosoftGraphManager graphManager;
        [Header("UI")] 
        [SerializeField]
        private GameObject authenticationPanel;
        [SerializeField]
        private GameObject deviceCodeAuthenticationPanel;
        [SerializeField]
        private Text deviceCodeText;
        [SerializeField]
        private Button signOutButton;
        [SerializeField]
        private Text userNameLabel;

        private void Awake()
        {
            signOutButton.onClick.AddListener(HandleSignOutButton);
        }

        private async void Start()
        {
            if (!await graphManager.AuthenticationService.CheckIfNeedsToSignIn())
            {
                signOutButton.gameObject.SetActive(true);
                var account = await graphManager.AuthenticationService.GetUserAccount();
                userNameLabel.text = account.Username;
            }
        }

        private async void HandleSignOutButton()
        {
            await graphManager.AuthenticationService.SignOutAsync();
            signOutButton.gameObject.SetActive(false);
        }

        public void HandleInteractiveAuthentication()
        {
            deviceCodeAuthenticationPanel.SetActive(false);
            authenticationPanel.SetActive(true);
        }

        public void HandleDeviceCodeAuthentication(string verificationUrl, string userCode)
        {
            authenticationPanel.SetActive(false);
            deviceCodeAuthenticationPanel.SetActive(true);
            var link = $"<color=purple>{verificationUrl}</color>";
            var code = $"<b>{userCode}</b>";
            deviceCodeText.text = $"To sign in, use a web browser to open {link} and enter the code {code} to authenticate.";
        }

        public async void HandleCompletedAuthentication()
        {
            signOutButton.gameObject.SetActive(true);
            authenticationPanel.gameObject.SetActive(false);
            deviceCodeAuthenticationPanel.gameObject.SetActive(false);
            deviceCodeText.text = "";
            var account = await graphManager.AuthenticationService.GetUserAccount();
            userNameLabel.text = account.Username;
        }
    }
}