using System.Runtime.Serialization;

namespace CommunicationLibrary;
[Serializable]
public class NotRunningException : Exception
{
    public NotRunningException()
    {
    }

    public NotRunningException(string? message) : base(message)
    {
    }

    public NotRunningException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    protected NotRunningException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}