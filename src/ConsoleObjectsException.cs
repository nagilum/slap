namespace Slap
{
    public class ConsoleObjectsException : Exception
    {
        /// <summary>
        /// Objects to write to console.
        /// </summary>
        public object[] Objects { get; set; }

        /// <summary>
        /// Init a new instance of ConsoleObjectsException.
        /// </summary>
        /// <param name="objects">Objects to write to console.</param>
        public ConsoleObjectsException(params object[] objects)
        {
            Objects = objects;
        }
    }
}