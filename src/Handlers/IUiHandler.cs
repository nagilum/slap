namespace Slap.Handlers;

public interface IUiHandler
{
    /// <summary>
    /// Setup background thread to update UI.
    /// </summary>
    void Setup(CancellationToken cancellationToken);
}