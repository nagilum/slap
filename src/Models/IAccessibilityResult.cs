namespace Slap.Models;

public interface IAccessibilityResult
{
    /// <summary>
    /// These results were aborted and require further testing.
    /// This can happen either because of technical restrictions to what the rule can test, or because a javascript error occurred.
    /// </summary>
    AccessibilityResultItem[] Incomplete { get; }
    
    /// <summary>
    /// These results indicate what elements failed the rules.
    /// </summary>
    AccessibilityResultItem[] Violations { get; }
}