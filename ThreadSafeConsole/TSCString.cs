using System;

namespace ThreadSafeConsole
{
    public class TSCString
    {
        /* Public Properties */
        
        public string Data { get; set; }
        public ConsoleColor? Foreground { get; set; }
        public ConsoleColor? Backgroung { get; set; }

        /* Constructors */
        public TSCString() { }
        public TSCString(string data) => Data = data;
        public TSCString(string data, ConsoleColor fg) : this(data) => Foreground = fg;
        public TSCString(string data, ConsoleColor fg, ConsoleColor bg) : this(data, fg) => Backgroung = bg;
    }
}
