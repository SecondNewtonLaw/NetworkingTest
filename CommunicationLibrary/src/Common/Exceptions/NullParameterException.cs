using System.Runtime.Serialization;

namespace CommunicationLibrary;

[Serializable]
public class NullParameterException : Exception
{
    public NullParameterException()
    {
    }

    public NullParameterException(string? message) : base(message)
    {
    }

    public NullParameterException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    protected NullParameterException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
