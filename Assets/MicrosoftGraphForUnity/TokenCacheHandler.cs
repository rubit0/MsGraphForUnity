using System;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Identity.Client;

namespace MicrosoftGraphForUnity
{
    /// <summary>
    /// Basic handler for storing and retrieving tokens.
    /// </summary>
    public class TokenCacheHandler
    {
        private readonly string _cacheFilePath;
        private readonly object fileLock = new object();

        public TokenCacheHandler(string cacheFilePath)
        {
            _cacheFilePath = cacheFilePath;
        }
        
        public void EnableSerialization(ITokenCache tokenCache)
        {
            tokenCache.SetBeforeAccess(BeforeAccessNotification);
            tokenCache.SetAfterAccess(AfterAccessNotification);
        }

        private void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (fileLock)
            {
                try
                {
                    args.TokenCache.DeserializeMsalV3(File.Exists(_cacheFilePath)
                        ? ProtectedData.Unprotect(File.ReadAllBytes(_cacheFilePath),
                            null,
                            DataProtectionScope.CurrentUser)
                        : null);
                }
                catch (PlatformNotSupportedException ex)
                {
                    //TODO You must implement here your own cryptographic methods
                    args.TokenCache.DeserializeMsalV3(File.Exists(_cacheFilePath)
                        ? File.ReadAllBytes(_cacheFilePath)
                        : null);
                }
            }
        }

        private void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            if (args.HasStateChanged)
            {
                lock (fileLock)
                {
                    try
                    {
                        var data = ProtectedData.Protect(args.TokenCache.SerializeMsalV3(), null,
                            DataProtectionScope.CurrentUser);
                        File.WriteAllBytes(_cacheFilePath, data);
                    }
                    catch (PlatformNotSupportedException ex)
                    {
                        //TODO You must implement here your own cryptographic methods
                        var data = args.TokenCache.SerializeMsalV3();
                        File.WriteAllBytes(_cacheFilePath, data);
                    }
                }
            }
        }
    }
}
