namespace AStarDev.FunctionalParadigm;

/// <summary>
///     Represents a failed <see cref="Exceptional{T}" /> carrying the captured exception.
/// </summary>
public sealed record Failure<T>(Exception Exception) : Exceptional<T>;
