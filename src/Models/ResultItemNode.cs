using Deque.AxeCore.Commons;
using Slap.Models.Interfaces;

namespace Slap.Models;

public class ResultItemNode : IResultItemNode
{
    /// <summary>
    /// <inheritdoc cref="IResultItemNode.Html"/>
    /// </summary>
    public string? Html { get; init; }
    
    /// <summary>
    /// <inheritdoc cref="IResultItemNode.Impact"/>
    /// </summary>
    public string? Impact { get; init; }
    
    /// <summary>
    /// <inheritdoc cref="IResultItemNode.Message"/>
    /// </summary>
    public string? Message { get; init; }
    
    /// <summary>
    /// <inheritdoc cref="IResultItemNode.Target"/>
    /// </summary>
    public ItemNodeSelector? Target { get; init; }
    
    /// <summary>
    /// <inheritdoc cref="IResultItemNode.XPath"/>
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