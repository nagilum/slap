using Deque.AxeCore.Commons;

namespace Slap.Models;

public class ResultItemNode
{
    /// <summary>
    /// Source HTML.
    /// </summary>
    public string? Html { get; init; }
    
    /// <summary>
    /// Impact assessment.
    /// </summary>
    public string? Impact { get; set; }
    
    /// <summary>
    /// Message.
    /// </summary>
    public string? Message { get; init; }
    
    /// <summary>
    /// Target selector.
    /// </summary>
    public ItemNodeSelector? Target { get; init; }
    
    /// <summary>
    /// XPath selector.
    /// </summary>
    public ItemNodeSelector? XPath { get; init; }
    
    /// <summary>
    /// Initialize a new instance of a <see cref="ResultItemNode"/> class.
    /// </summary>
    /// <param name="node">Result node.</param>
    public ResultItemNode(AxeResultNode node)
    {
        this.Html = node.Html;
        this.Impact = node.Impact;
        this.Message = node.Any.Length > 0
            ? node.Any[0].Message
            : null;

        if (node.Target is not null)
        {
            this.Target = new(node.Target);
        }

        if (node.XPath is not null)
        {
            this.XPath = new(node.XPath);
        }
    }
}