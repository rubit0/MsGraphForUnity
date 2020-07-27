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
        private readonly string cacheFilePath;
        private readonly object fileLock = new object();

        public TokenCacheHandler(string cacheDirectoryPath)
        {
            if (!Directory.Exists(cacheDirectoryPath))
            {
                Directory.CreateDirectory(cacheDirectoryPath);
            }

            cacheFilePath = cacheDirectoryPath + "msalcache.bin3";
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
                    args.TokenCache.DeserializeMsalV3(File.Exists(cacheFilePath)
                        ? ProtectedData.Unprotect(File.ReadAllBytes(cacheFilePath),
                            null,
                            DataProtectionScope.CurrentUser)
                        : null);
                }
                catch (PlatformNotSupportedException ex)
                {
                    //TODO You must implement here your own cryptographic methods
                    args.TokenCache.DeserializeMsalV3(File.Exists(cacheFilePath)
                        ? File.ReadAllBytes(cacheFilePath)
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
                        File.WriteAllBytes(cacheFilePath, data);
                    }
                    catch (PlatformNotSupportedException ex)
                    {
                        //TODO You must implement here your own cryptographic methods
                        var data = args.TokenCache.SerializeMsalV3();
                        File.WriteAllBytes(cacheFilePath, data);
                    }
                }
            }
        }
    }
}
