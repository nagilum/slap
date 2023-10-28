using Deque.AxeCore.Commons;
using Slap.Models.Interfaces;

namespace Slap.Models;

public class AccessibilityResult : IAccessibilityResult
{
    /// <summary>
    /// <inheritdoc cref="IAccessibilityResult.Incomplete"/>
    /// </summary>
    public AccessibilityResultItem[]? Incomplete { get; init; }
    
    /// <summary>
    /// <inheritdoc cref="IAccessibilityResult.Violations"/>
    /// </summary>
    public AccessibilityResultItem[]? Violations { get; init; }

    /// <summary>
    /// Initialize a new instance of a <see cref="AccessibilityResult"/> class.
    /// </summary>
    /// <param name="result">Accessibility result.</param>
    public AccessibilityResult(AxeResult result)
    {
        this.Incomplete = result.Incomplete
            .Select(n => new AccessibilityResultItem(n))
            .ToArray();

        this.Violations = result.Violations
            .Select(n => new AccessibilityResultItem(n))
            .ToArray();
    }
}