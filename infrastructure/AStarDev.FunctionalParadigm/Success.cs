namespace AStarDev.FunctionalParadigm;

/// <summary>
///     Represents a successful <see cref="Exceptional{T}" /> carrying a value.
/// </summary>
public sealed record Success<T>(T Value) : Exceptional<T>;
