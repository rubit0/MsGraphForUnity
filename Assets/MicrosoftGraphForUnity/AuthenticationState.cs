namespace MicrosoftGraphForUnity
{
    /// <summary>
    /// Represents basic authentication state.
    /// </summary>
    public enum AuthenticationState
    {
        StartedInteractive,
        FallbackToDeviceCode,
        Completed,
        Failed,
        SignOut
    }
}