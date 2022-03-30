namespace Slap
{
    public static class ConsoleEx
    {
        /// <summary>
        /// Write a custom ConsoleObjectsException objects to console.
        /// </summary>
        /// <param name="ex">ConsoleObjectsException.</param>
        public static void WriteException(ConsoleObjectsException ex)
        {
            var list = new List<object>
            {
                ConsoleColor.Red,
                "Error",
                Environment.NewLine,
                (byte) 0x00
            };

            list.AddRange(ex.Objects);

            if (!list.Last().Equals(Environment.NewLine))
            {
                list.Add(Environment.NewLine);
            }

            WriteObjects(list.ToArray());
        }

        /// <summary>
        /// Write a generic error to console.
        /// </summary>
        /// <param name="ex">Exception.</param>
        public static void WriteException(Exception ex)
        {
            var list = new List<object>
            {
                ConsoleColor.Red,
                "Error",
                Environment.NewLine,
                (byte) 0x00
            };

            while (true)
            {
                list.Add(ex.Message);
                list.Add(Environment.NewLine);

                if (ex.InnerException == null)
                {
                    break;
                }

                ex = ex.InnerException;
            }

            WriteObjects(list.ToArray());
        }

        /// <summary>
        /// Write objects to console.
        /// </summary>
        /// <param name="objects">Objects to write.</param>
        public static void WriteObjects(params object[] objects)
        {
            foreach (object obj in objects)
            {
                // Check for foreground color.
                if (obj is ConsoleColor cc)
                {
                    Console.ForegroundColor = cc;
                }

                // Check for color-reset.
                else if (obj is byte b &&
                         b == 0x00)
                {
                    Console.ResetColor();
                }

                // Treat rest as text.
                else
                {
                    Console.Write(obj);
                }
            }

            // Always reset the color after manipulating the console.
            Console.ResetColor();
        }
    }
}