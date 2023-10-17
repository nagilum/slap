using Deque.AxeCore.Commons;

namespace Slap.Models;

public class AccessibilityResult
{
    /// <summary>
    /// These results were aborted and require further testing. 
    /// This can happen either because of technical restrictions to what the rule can test, or because a javascript error occurred.
    /// </summary>
    public AccessibilityResultItem[]? Incomplete { get; init; }
    
    /// <summary>
    /// These results indicate what elements failed the rules.
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