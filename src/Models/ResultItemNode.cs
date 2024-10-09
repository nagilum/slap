using Deque.AxeCore.Commons;

namespace Slap.Models;

public class ResultItemNode(
    AxeResultNode node) : IResultItemNode
{
    /// <summary>
    /// <inheritdoc cref="IResultItemNode.Html"/>
    /// </summary>
    public string? Html { get; } = node.Html;
    
    /// <summary>
    /// <inheritdoc cref="IResultItemNode.Impact"/>
    /// </summary>
    public string? Impact { get; } = node.Impact;

    /// <summary>
    /// <inheritdoc cref="IResultItemNode.Message"/>
    /// </summary>
    public string? Message { get; } = node.Any.FirstOrDefault()?.Message;

    /// <summary>
    /// <inheritdoc cref="IResultItemNode.Target"/>
    /// </summary>
    public ItemNodeSelector? Target { get; } = new(node.Target);

    /// <summary>
    /// <inheritdoc cref="IResultItemNode.XPath"/>
    /// </summary>
    public ItemNodeSelector? XPath { get; } = new(node.XPath);
}