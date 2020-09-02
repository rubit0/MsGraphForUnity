# Microsoft Graph for Unity
This package brings Microsoft Graph with MSAL (authentication) to Unity (2019.4 LTS).  
You can access all MS Graph APIs like OneDrive, SharePoint for Business, Microsoft Teams and more.

## Setup

### Azure Portal
First you need to **register your native application** to grant access with MS Graph APIs.  
[Learn how to register your application for MS Graph](https://docs.microsoft.com/en-US/graph/auth-register-app-v2)

Once you have registered your application, make sure the option **Default client type** under the *Authentication* section is set to true.

Now go the *API permissions* section. Here you can add permissions for all MS Graph APIs your application should be able to access.  
We recommend to add at least the following permissions:

- *User.Read*
- *offline_access*

For file access from OneDrive add additionally the permissions *Files.ReadWrite.All*.

#### Work accounts and file access
When a user signs in with a work account, then the drive/file API uses SharePoint as backend and not OneDrive.  
In practice this means the App registration needs additional **API permissions for SharePoint**, specifically *Sites.Read.All* to list drive items or *Sites.ReadWrite.All* for storing new files.

### Unity
Add the **MicrosoftGraphManager** component to a GameObject in your scene.  
Then provide the ***App Id*, *Redirect Url* and the desired *access scopes*** which you can retrieve from the app registration in the azure portal. Make sure that the specified *access scopes* do not exceed what is specified in the app registration.

## Example scene
This package includes an example scene that deals with all relevant aspects and works as a great starting point.
In this example the user can query for files of this personal OneDrive file and presents a list of found items with a thumbnail.

## Supported platforms
Generally it should work on all platforms, at least with device code flow as a fallback.  
Verified working platforms:

| OS  | Authentication Flow |
| ------------- |-------------|
| Android  | Device Code |
| Android/Oculus Quest  | Device Code |
| iOS  | Device Code |
| UWP/HoloLens  | Interactive |
| Win32/Editor  | Device Code |

## Authentication
Authentication is handled for you by this library, just just need to provide some basic UI to handle device code flow, the example scene illustrates how to use it.

### Important note regarding encryption
Storing the authentication token happens on Windows platform in a secure manner, on other platforms the token is stored without encryption. Please take a look at TokenCacheHandler to add your own encryption.
