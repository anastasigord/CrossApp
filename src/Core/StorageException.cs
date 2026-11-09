namespace Core;

// Технічна помилка сховища/файлу. Початковий виняток лежить в InnerException
public sealed class StorageException : Exception
{
    public StorageException(string message) : base(message) { }
    public StorageException(string message, Exception inner) : base(message, inner) { }
}