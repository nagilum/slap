namespace Slap.Handlers;

public interface IUiHandler
{
    /// <summary>
    /// Setup background thread to update UI.
    /// </summary>
    void Setup();

    /// <summary>
    /// Stop the background thread.
    /// </summary>
    void Stop();

    /// <summary>
    /// Update values in the UI, and possibly redraw the whole thing.
    /// </summary>
    void UpdateUi();
}