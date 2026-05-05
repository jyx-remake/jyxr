namespace Game.Core.Story;

public sealed class StoryRuntimeException : Exception
{
    public StoryRuntimeException(string message) : base(message)
    {
    }

    public StoryRuntimeException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
