using System.Numerics;

namespace AStarDev.GuardClauses;

/// <summary>The root <seealso href="GuardAgainst"></seealso> class.</summary>
public static class GuardAgainst
{
    /// <summary>This method will check whether the specified object is null or not.</summary>
    /// <typeparam name="T">
    ///     Specifies the generic object to check for null.
    /// </typeparam>
    /// <param name="objectToCheck">The object to check for null.</param>
    /// <returns>The original object if it is not null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the object is, in fact, null.</exception>
    public static T Null<T>(T objectToCheck) => objectToCheck is null ? throw new ArgumentNullException(nameof(objectToCheck)) : objectToCheck;

    /// <summary>This method will check whether the specified numeric value is negative or not.</summary>
    /// <typeparam name="T">The type of the numeric value being checked.</typeparam>
    /// <param name="objectToCheck">The numeric value to check for negativity.</param>
    /// <returns>The original numeric value if it is not negative.</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static INumber<T> Negative<T>(T objectToCheck) where T : INumber<T> => objectToCheck < T.Zero ? throw new ArgumentOutOfRangeException(nameof(objectToCheck)) : objectToCheck;
}
