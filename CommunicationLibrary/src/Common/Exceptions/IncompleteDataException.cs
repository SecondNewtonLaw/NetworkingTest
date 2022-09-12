using System.Runtime.Serialization;

namespace CommunicationLibrary;
[Serializable]
public class IncompleteDataException : Exception
{
    public IncompleteDataException()
    {
    }

    public IncompleteDataException(string? message) : base(message)
    {
    }

    public IncompleteDataException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    protected IncompleteDataException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}