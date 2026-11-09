namespace Core;

// Порушення бізнес-правила (наприклад, змінити підтверджене замовлення)
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception inner) : base(message, inner) { }
}