using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Identity.Client;

namespace MicrosoftGraphForUnity
{
    /// <summary>
    /// Maintains the user session token and gives access to Microsoft Graph API
    /// </summary>
    public class AuthenticationService
    {
        /// <summary>
        /// Indicates when the authentication state has changed.
        /// </summary>
        public event EventHandler<AuthenticationState> OnAuthenticationChanged;

        /// <summary>
        /// Indicates a handler of this service to display the device code to the user.
        /// </summary>
        public event EventHandler<DeviceCodeResult> OnPresentDeviceCode;
        
        /// <summary>
        /// Gets the current application id
        /// </summary>
        public string ClientId { get; }

        /// <summary>
        /// Gets the token of the current user
        /// </summary>
        public string TokenForUser { get; private set; }

        /// <summary>
        /// Gets when current user's token expires
        /// </summary>
        public DateTimeOffset Expiration { get; private set; }

        /// <summary>
        /// Gets the client which allows to interact with the current user
        /// </summary>
        public GraphServiceClient GraphClient { get; private set; }

        /// <summary>
        /// Gets whether the user is logged in or not
        /// </summary>
        public bool IsConnected => TokenForUser != null && Expiration > DateTimeOffset.UtcNow.AddMinutes(1);
        
        /// <summary>
        /// The scopes to use when user grants the application
        /// </summary>
        private string[] grantScopes;

        /// <summary>
        /// Keeps the identity of the current user
        /// </summary>
        private readonly IPublicClientApplication identityClientApp;

        /// <summary>
        /// Init a new instance for consuming MS Graph services.
        /// Only one instance for the entire app lifecycle is needed.
        /// </summary>
        /// <param name="clientId">The clientId (appId) associated in Azure Active Directory</param>
        /// <param name="redirectUri">Optional MSAL redirect URI specified in Azure Active Directory</param>
        /// <param name="scopes">Define scopes that this application can access on MS Graph.</param>
        /// <param name="tokenCacheDirectory">The directory path where to save the token in case System.Security.Cryptography is not supported.</param>
        /// <exception cref="ArgumentException">You must specify a clientId.</exception>
        public AuthenticationService(string clientId, string redirectUri, string[] scopes, string tokenCacheDirectory)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentException("Client id cannot be null or empty", nameof(clientId));
            }

            ClientId = clientId;
            grantScopes = scopes;

            var clientBuilder = PublicClientApplicationBuilder.Create(clientId);
            if (!string.IsNullOrWhiteSpace(redirectUri))
            {
                clientBuilder.WithRedirectUri(redirectUri);
            }
            
            identityClientApp = clientBuilder.Build();
            
#if !WINDOWS_UWP
            var tokenCacheHelper = new TokenCacheHandler(tokenCacheDirectory);
            tokenCacheHelper.EnableSerialization(identityClientApp.UserTokenCache);
#endif

            GraphClient = GetAuthenticatedClient();
        }
        
        /// <summary>
        /// Get the main user account.
        /// </summary>
        public async Task<IAccount> GetUserAccount()
        {
            var accounts = await identityClientApp.GetAccountsAsync();
            if (accounts == null || !accounts.Any())
            {
                return null;
            }

            return accounts.First();
        }

        /// <summary>
        /// Force checking of authentication is needed.
        /// </summary>
        public async Task<bool> CheckIfNeedsToSignIn()
        {
            var accounts = await identityClientApp.GetAccountsAsync();
            if (accounts != null && accounts.Any())
            {
                return false;
            }

            return !IsConnected;
        }

        /// <summary>
        /// Force to signin the user in an interactive authentication flow.
        /// </summary>
        /// <returns></returns>
        public async Task ForceInteractiveSignIn()
        {
            try
            {
                OnAuthenticationChanged?.Invoke(this, AuthenticationState.StartedInteractive);
                var authResult = await identityClientApp.AcquireTokenInteractive(grantScopes).ExecuteAsync();
                // Set access token and expiration
                TokenForUser = authResult.AccessToken;
                Expiration = authResult.ExpiresOn;

                OnAuthenticationChanged?.Invoke(this, AuthenticationState.Completed);
            }
            catch (Exception e) when (e is PlatformNotSupportedException || e is MsalUiRequiredException)
            {

                var authResult = await identityClientApp.AcquireTokenWithDeviceCode(grantScopes, dcr =>
                {
                    OnAuthenticationChanged?.Invoke(this, AuthenticationState.FallbackToDeviceCode);
                    OnPresentDeviceCode?.Invoke(this, dcr);
                    return Task.FromResult(0);
                }).ExecuteAsync();

                // Set access token and expiration
                TokenForUser = authResult.AccessToken;
                Expiration = authResult.ExpiresOn;
                OnAuthenticationChanged?.Invoke(this, AuthenticationState.Completed);
            }
            catch (Exception)
            {
                OnAuthenticationChanged?.Invoke(this, AuthenticationState.Failed);
            }
        }

        /// <summary>
        /// Signs the user out of the service.
        /// </summary>
        public async Task SignOutAsync()
        {
            foreach (IAccount account in await identityClientApp.GetAccountsAsync())
            {
                await identityClientApp.RemoveAsync(account);
            }
            GraphClient = GetAuthenticatedClient();

            // Reset token and expiration
            Expiration = DateTimeOffset.MinValue;
            TokenForUser = null;
            OnAuthenticationChanged?.Invoke(this, AuthenticationState.SignOut);
        }

        /// <summary>
        /// Gets a new instance of <see cref="GraphServiceClient"/>
        /// </summary>
        /// <returns>Instance of <see cref="GraphServiceClient"/></returns>
        private GraphServiceClient GetAuthenticatedClient()
        {
            // Create Microsoft Graph client.
            return new GraphServiceClient(
                "https://graph.microsoft.com/v1.0",
                new DelegateAuthenticationProvider(
                    async (requestMessage) =>
                    {
                        await AcquireTokenForUserAsync();
                        // Set bearer authentication on header
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", TokenForUser);
                    }));
        }

        /// <summary>
        /// Get Token for User.
        /// </summary>
        /// <returns>Token for user.</returns>
        private async Task AcquireTokenForUserAsync()
        {
            // Get an access token for the given context and resourceId. An attempt is first made to 
            // acquire the token silently. If that fails, then we try to acquire the token by prompting the user.
            var accounts = await identityClientApp.GetAccountsAsync();
            if (accounts != null && accounts.Count() > 0)
            {
                try
                {
                    var authResult = await identityClientApp.AcquireTokenSilent(grantScopes, accounts.First()).ExecuteAsync();
                    TokenForUser = authResult.AccessToken;
                    Expiration = authResult.ExpiresOn;
                    OnAuthenticationChanged?.Invoke(this, AuthenticationState.Completed);
                    return;
                }
                catch (Exception)
                {
                    TokenForUser = null;
                    Expiration = DateTimeOffset.MinValue;
                }
            }

            // Cannot get the token silently. Ask user to log in
            if (!IsConnected)
            {
                await ForceInteractiveSignIn();
            }
        }
    }
}
