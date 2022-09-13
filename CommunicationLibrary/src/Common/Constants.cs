namespace CommunicationLibrary;

/// <summary>
/// Class containing several constant values used through out the library
/// </summary>
public struct Constants
{
    /// <summary>
    /// Constant value representing the timeout before the next recieve attempt is made.
    /// </summary>
    public const int RECIEVE_TIMEOUT = 1666;

    /// <summary>
    /// Constant value representing the recieve and send buffer size used throughout the program.
    /// </summary>
    public const int BUF_SIZE = 131072; // 8192 bytes times 16
}