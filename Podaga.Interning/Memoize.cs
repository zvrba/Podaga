namespace Podaga.Interning;

/// <summary>
/// Memoized functions must conform to this delegate.
/// </summary>
/// <param name="input">Input value.</param>
/// <param name="recurse">
/// An instance on which <see cref="Memoize{TInput, TOutput}.Run(TInput)"/> method can be used to invoke self
/// recursively, with memoization.  (Outputs computed for previous inputs won't be recomputed.)
/// </param>
/// <typeparam name="TInput">Function input type.</typeparam>
/// <typeparam name="TOutput">Function output type.</typeparam>
/// <seealso cref="Memoize{TInput, TOutput}"/>
public delegate TOutput Memoized<TInput, TOutput>(TInput input, Memoize<TInput, TOutput> recurse)
    where TInput : notnull;


/// <summary>
/// Generic wrapper for memoized execution of methods.  Each instance carries own memoization history.
/// The constructor MUST be explicitly invoked; a <c>default</c> instance is invalid to use.
/// </summary>
/// <typeparam name="TInput">Function input type.</typeparam>
/// <typeparam name="TOutput">Function output type.</typeparam>
/// <seealso cref="Memoized{TInput, TOutput}"/>
/// <param name="memoized">Delegate whose computations should be memoized.</param>
public readonly struct Memoize<TInput, TOutput>(Memoized<TInput, TOutput> memoized) where TInput : notnull
{
    private readonly Dictionary<TInput, TOutput> memo = new();

    /// <summary>
    /// True if <c>this</c> is uninitialized, i.e., <c>default</c>.
    /// </summary>
    public bool IsNull => memo is null;

    /// <summary>
    /// Runs the memoized method (passed to ctor) using <c>this</c> as memoization space.
    /// </summary>
    /// <param name="input">Input value.</param>
    /// <returns>Value returned by the memoized delegate.</returns>
    public TOutput Run(TInput input)
    {
        if (!memo.TryGetValue(input, out var output))
            output = memo[input] = memoized(input, this);
        return output;
    }
}
