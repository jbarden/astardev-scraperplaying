namespace AStarDev.FunctionalParadigm;

/// <summary>
///    Represents a type with a single value, used to indicate the absence of a meaningful value.
/// </summary>
public record UnitFp
{
    /// <summary>
    ///   The single instance of <see cref="UnitFp" />.
    /// </summary>
    public static readonly UnitFp Instance = new();
}
#pragma warning restore CA1031 // Do not catch general exception types
