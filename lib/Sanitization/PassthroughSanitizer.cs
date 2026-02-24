namespace Sanitization;

public class PassthroughSanitizer<T> : ISanitizer<T>
{
    public T Sanitize(T input) => input;
}
