using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Graph;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using Application = UnityEngine.Application;

namespace MicrosoftGraphForUnity
{
    /// <summary>
    /// Use this manager in your scene to get access to microsoft graph services and handling of authentication.
    /// </summary>
    public class MicrosoftGraphManager : MonoBehaviour
    {
        [Serializable]
        public class DeviceCodeEvent : UnityEvent<string, string> { }
        
        public AuthenticationService AuthenticationService { private set; get; }
        public GraphServiceClient Client => AuthenticationService.GraphClient;
        
        [Header("App settings")]
        [SerializeField]
        private string appId;
        [SerializeField]
        private string redirectUrl;
        [SerializeField]
        private string[] scopes = new[] { "User.Read", "Files.ReadWrite" };

        [Header("Authentication Callbacks")]
        [SerializeField]
        private UnityEvent onInteractiveAuthenticationStarted;
        [SerializeField]
        private DeviceCodeEvent onDeviceCodeAuthenticationStarted;
        [SerializeField]
        private UnityEvent onAuthenticationFinished;
        [SerializeField]
        private UnityEvent onAuthenticationFailed;
        [SerializeField]
        private UnityEvent onUserSignOut;
        
        private Queue<Action> backgroundThreadActions = new Queue<Action>();

        private void Awake()
        {
            Assert.IsTrue(appId.Length > 0, "You must provide a application Id.");
            Assert.IsTrue(scopes.Length > 0, "You must specify at least one scope.");
            
            Debug.Log(Application.persistentDataPath + "/token/");
            
            AuthenticationService = new AuthenticationService(appId, redirectUrl, scopes, Application.persistentDataPath + "/token/");
            AuthenticationService.OnAuthenticationChanged += (sender, state) =>
            {
                switch (state)
                {
                    case AuthenticationState.StartedInteractive:
                        SyncBackgroundAction(() => onInteractiveAuthenticationStarted?.Invoke());
                        break;
                    case AuthenticationState.FallbackToDeviceCode:
                        break;
                    case AuthenticationState.Completed:
                        SyncBackgroundAction(() => onAuthenticationFinished?.Invoke());
                        break;
                    case AuthenticationState.Failed:
                        SyncBackgroundAction(() => onAuthenticationFailed?.Invoke());
                        break;
                    case AuthenticationState.SignOut:
                        SyncBackgroundAction(() => onUserSignOut?.Invoke());
                        break;
                }
            };

            AuthenticationService.OnPresentDeviceCode += (sender, deviceCode) =>
            {
                SyncBackgroundAction(() => onDeviceCodeAuthenticationStarted?.Invoke(deviceCode.VerificationUrl, deviceCode.UserCode));
            };
        }
        
        private void OnEnable()
        {
            StartCoroutine(SyncThreadContextCoroutine());
        }
        
        private void SyncBackgroundAction(Action action)
        {
            lock (backgroundThreadActions)
            {
                backgroundThreadActions.Enqueue(action);
            }
        }

        private IEnumerator SyncThreadContextCoroutine()
        {
            while (gameObject.activeInHierarchy)
            {
                lock (backgroundThreadActions)
                {
                    while (backgroundThreadActions.Count > 0)
                    {
                        var action = backgroundThreadActions.Dequeue();
                        action();
                    }
                }

                yield return null;
            }
        }
    }
}
