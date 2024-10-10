using Slap.Extenders;

namespace Slap.Handlers;

public class UiHandler : IUiHandler
{
    /// <summary>
    /// Default foreground color.
    /// </summary>
    private ConsoleColor DefaultForegroundColor { get; } = Console.ForegroundColor;
    
    /// <summary>
    /// Whether stop has been requested.
    /// </summary>
    private bool IsStopRequested { get; set; }
    
    /// <summary>
    /// Last count of response type, for clear-check.
    /// </summary>
    private int LastResponseTypeCount { get; set; }
    
    /// <summary>
    /// UI thread.
    /// </summary>
    private Thread? UiThread { get; set; }
    
    /// <summary>
    /// Window height.
    /// </summary>
    private int WindowHeight { get; set; }
    
    /// <summary>
    /// Window width.
    /// </summary>
    private int WindowWidth { get; set; }
    
    /// <summary>
    /// <inheritdoc cref="IUiHandler.Setup"/>
    /// </summary>
    public void Setup()
    {
        this.UiThread = new Thread(() =>
        {
            while (!this.IsStopRequested)
            {
                try
                {
                    this.UpdateUi();
                    Thread.Sleep(100);
                }
                catch
                {
                    break;
                }
            }
        });

        this.UiThread.Start();
    }

    /// <summary>
    /// <inheritdoc cref="IUiHandler.Stop"/>
    /// </summary>
    public void Stop()
    {
        this.IsStopRequested = true;
    }

    /// <summary>
    /// <inheritdoc cref="IUiHandler.UpdateUi"/>
    /// </summary>
    public void UpdateUi()
    {
        var clear = false;

        if (this.WindowHeight != Console.WindowHeight ||
            this.WindowWidth != Console.WindowWidth)
        {
            this.WindowHeight = Console.WindowHeight;
            this.WindowWidth = Console.WindowWidth;
            
            clear = true;
        }

        var top = 6;
        var responseTypeCounts = Globals.ResponseTypeCounts
            .OrderBy(n => n.Key)
            .ToDictionary(n => n.Key, n => n.Value);

        if (clear)
        {
            Console.ResetColor();
            Console.Clear();
            
            Write(0, 0, ConsoleColor.White, $"{Program.Name} v{Program.Version}");
            Write(1, 0, DefaultForegroundColor, "Press CTRL+C to abort");
            Write(1, 6, ConsoleColor.Magenta, "CTRL+C");

            Write(3, 0, DefaultForegroundColor, $"Started:                    Elapsed:");
            Write(4, 0, DefaultForegroundColor, "Pending:                    Finished:                    Total:");
            
            Write(3, 9, ConsoleColor.Cyan, Globals.Started.ToString("HH:mm:ss"));
        }

        if (this.LastResponseTypeCount != responseTypeCounts.Count)
        {
            foreach (var (text, _) in responseTypeCounts)
            {
                var color = ConsoleColor.Red;

                if (text.StartsWith('2'))
                {
                    color = ConsoleColor.Green;
                }
                else if (text.StartsWith('3'))
                {
                    color = ConsoleColor.Yellow;
                }
                
                Write(top++, 9, color, text);
            }

            this.LastResponseTypeCount = responseTypeCounts.Count;
            top = 6;
        }

        var elapsed = DateTime.Now - Globals.Started;
        
        Write(3, 38, ConsoleColor.Cyan, elapsed.ToHumanReadable());
        
        var pending = Globals.QueueEntries.Count(n => !n.Finished.HasValue);
        var finished = Globals.QueueEntries.Count(n => n.Finished.HasValue);
        var total = pending + finished;
        
        var pendingPercentage = total > 0 ? 100.00 / total * pending : 0;
        var finishedPercentage = total > 0 ? 100.00 / total * finished : 0;
        
        Write(4, 9, pending is 0 ? DefaultForegroundColor : ConsoleColor.Yellow, $"{pending} ({(int)pendingPercentage}%)     ");
        Write(4, 38, finishedPercentage is 100 ? DefaultForegroundColor : ConsoleColor.Yellow, $"{finished} ({(int)finishedPercentage}%)     ");
        Write(4, 64, ConsoleColor.Yellow, total.ToString());
        
        foreach (var (_, count) in responseTypeCounts)
        {
            Write(top++, 0, ConsoleColor.White, count.ToString());
        }
    }

    /// <summary>
    /// Write text at a specific position with a given color.
    /// </summary>
    /// <param name="top">Top location.</param>
    /// <param name="left">Left location.</param>
    /// <param name="color">Foreground color.</param>
    /// <param name="text">Text to write.</param>
    private void Write(int top, int left, ConsoleColor color, string text)
    {
        Console.CursorTop = top;
        Console.CursorLeft = left;
        Console.ForegroundColor = color;
        Console.Write(text);
    }
}