namespace Sanitization;

public interface ISanitizer
{
    object Sanitize(object input);
}

public interface ISanitizer<T> : ISanitizer
{
    T Sanitize(T input);

    object ISanitizer.Sanitize(object input) => Sanitize((T)input)!;
}
