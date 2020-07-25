# Microsoft Graph for Unity
This package brings Microsoft Graph with MSAL (authentication) to Unity (2019.4 LTS).  
You can access all MS Graph APIs like OneDrive or Teams.

## Setup
Add the MicrosoftGraphManager component to a GameObject in your scene.  
Then provide the AppId, Redirect url and the desired access scopes.

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
| UWP/HoloLens  | Interactive |
| Win32/Editor  | Device Code |

## Authentication
Authentication is handled for you by this library, just just need to provide some basic UI to handle device code flow, the example scene illustrates how to use it.

### Important note regarding encryption
Storing the authentication token happens on Windows platform in a secure manner, on other platforms the token is stored without encryption. Please take a look at TokenCacheHandler to add your own encryption.