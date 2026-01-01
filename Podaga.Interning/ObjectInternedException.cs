using System.Diagnostics;

namespace Podaga.Interning;

/// <summary>
/// Thrown when attempting to mutate an interned object.  Unless inherited, this exception is thrown only by
/// <see cref="InternableExtensions.ThrowIfInterned(Podaga.Interning.IInternable)"/>.
/// </summary>
public class ObjectInternedException : InvalidOperationException
{
    private const string DefaultMessage = "Cannot mutate an interned object.";

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="objectInstance">
    /// The instance that caused this exception to be thrown.  Must be provided, otherwise an assertion is triggered in debug mode.
    /// </param>
    /// <param name="message">Optional message (overrides the default message).</param>
    /// <param name="inner">Optional inner exception.</param>
    internal protected ObjectInternedException
        (
        IInternable objectInstance,
        string? message = null,
        Exception? inner = null
        ) : base(message ?? DefaultMessage, inner)
    {
        Debug.Assert(objectInstance is not null, "Object instance must be provided.");
        ObjectInstance = objectInstance;
    }

    /// <summary>
    /// The object that caused this exception to be thrown.
    /// </summary>
    public IInternable ObjectInstance { get; }

    /// <summary>
    /// Adds the type name to the message provided to the ctor.
    /// </summary>
    public override string Message =>
        ObjectInstance is null ?
        base.Message :
        base.Message + Environment.NewLine + $"ObjectInstance is {ObjectInstance.GetType().FullName}";
}
