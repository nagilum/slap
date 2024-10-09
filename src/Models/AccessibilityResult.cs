using Deque.AxeCore.Commons;

namespace Slap.Models;

public class AccessibilityResult(
    AxeResult result) : IAccessibilityResult
{
    /// <summary>
    /// <inheritdoc cref="IAccessibilityResult.Incomplete"/>
    /// </summary>
    public AccessibilityResultItem[] Incomplete { get; } =
        result.Incomplete
            .Select(n => new AccessibilityResultItem(n))
            .ToArray();

    /// <summary>
    /// <inheritdoc cref="IAccessibilityResult.Violations"/>
    /// </summary>
    public AccessibilityResultItem[] Violations { get; } =
        result.Violations
            .Select(n => new AccessibilityResultItem(n))
            .ToArray();
}