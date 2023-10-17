using Deque.AxeCore.Commons;

namespace Slap.Models;

public class AccessibilityResult
{
    /// <summary>
    /// These results were aborted and require further testing. 
    /// This can happen either because of technical restrictions to what the rule can test, or because a javascript error occurred.
    /// </summary>
    public AxeResultItem[]? Incomplete { get; init; }
    
    /// <summary>
    /// These results indicate what elements failed the rules.
    /// </summary>
    public AxeResultItem[]? Violations { get; init; }
}