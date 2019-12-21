using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ThreadSafeConsole
{
    public static class TSConsole
    {
        /* Private fields */
        
        private static readonly object _lockObject = new object();
        private static readonly StringBuilder _buffer = new StringBuilder();

        private static bool _isReading = false;
        private static int _bufferCursorPos = 0;
        private static string _prompt = "";

        /* Public Properties */

        public static string Prompt
        {
            get
            {
                lock (_lockObject)
                {
                    return _prompt;
                }
            }
            set
            {
                lock (_lockObject)
                {
                    if (_isReading)
                        throw new InvalidOperationException("Can't set prompt while reading.");
                    if (string.IsNullOrEmpty(value))
                        throw new ArgumentNullException(nameof(value));
                    _prompt = value;
                }
            }
        }

        public static bool IsReading
        {
            get
            {
                lock (_lockObject)
                {
                    return _isReading;
                }
            }
        }

        /* Public Methods */
        
        public static void WriteLine(string str)
        {
            if (string.IsNullOrEmpty(str))
                throw new ArgumentNullException(nameof(str));

            lock (_lockObject)
            {
                EraseBuffer();
                Console.Write(str);
                RestoreBuffer();
            }
        }

        public static void WriteLine(IEnumerable<TSCString> enumeration)
        {
            if (enumeration is null)
                throw new ArgumentNullException(nameof(enumeration));

            lock (_lockObject)
            {
                ConsoleColor saveForeground = Console.ForegroundColor;
                ConsoleColor saveBackground = Console.BackgroundColor;
                EraseBuffer();
                foreach (var elem in enumeration)
                {
                    if (!string.IsNullOrEmpty(elem.Data))
                    {
                        Console.ForegroundColor = elem.Foreground ?? saveForeground;
                        Console.BackgroundColor = elem.Backgroung ?? saveBackground;
                        Console.Write(elem.Data);
                    }
                }
                Console.ForegroundColor = saveForeground;
                Console.BackgroundColor = saveBackground;
                RestoreBuffer();
            }
        }

        public static void WriteLine(params TSCString[] enumeration)
        {
            WriteLine((IEnumerable<TSCString>)enumeration);
        }

        public static string ReadLine()
        {
            lock (_lockObject)
            {
                // Check if already reading
                if (_isReading)
                    throw new InvalidOperationException("Some thread is already reading.");

                // Change private vars
                _isReading = true;
                _bufferCursorPos = 0;

                Console.Write(_prompt);
            }

            // Main loop
            while (true)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                lock (_lockObject)
                {
                    // Char is not control (Default case)
                    if (!char.IsControl(keyInfo.KeyChar))
                    {
                        int i = _buffer.Length - _bufferCursorPos;
                        Console.Write(keyInfo.KeyChar + _buffer.ToString(_bufferCursorPos, i));
                        MoveCursor(i * -1);

                        _buffer.Insert(_bufferCursorPos, keyInfo.KeyChar.ToString(CultureInfo.InvariantCulture));
                        _bufferCursorPos++;
                        continue;
                    }

                    // If char is control
                    switch (keyInfo.Key)
                    {
                        // [Backspace] Remove symbol before cursor
                        case ConsoleKey.Backspace:
                            if (_bufferCursorPos != 0)
                            {
                                MoveCursor(-1);
                                int i = _buffer.Length - _bufferCursorPos;
                                Console.Write(_buffer.ToString(_bufferCursorPos, i) + ' ');
                                MoveCursor(i * -1 - 1);

                                _buffer.Remove(_bufferCursorPos - 1, 1);
                                _bufferCursorPos--;
                            }
                            continue;

                        // [Del] Remove symbol after cursor
                        case ConsoleKey.Delete:
                            if (_bufferCursorPos != _buffer.Length)
                            {
                                int i = _buffer.Length - _bufferCursorPos;
                                Console.Write(_buffer.ToString(_bufferCursorPos + 1, i - 1) + ' ');
                                MoveCursor(i * -1);

                                _buffer.Remove(_bufferCursorPos, 1);
                            }
                            continue;

                        // [Enter] Clear buffer and return string
                        case ConsoleKey.Enter:
                            if (_buffer.Length != 0)
                            {
                                // Clear buffer on screen
                                MoveCursor((_prompt.Length + _bufferCursorPos) * -1);
                                int i = _prompt.Length + _buffer.Length;
                                Console.Write(new string(' ', i));
                                MoveCursor(i * -1);

                                string result = _buffer.ToString();
                                _isReading = false;
                                _buffer.Clear();
                                _bufferCursorPos = 0;

                                return result;
                            }
                            continue;

                        // [Left Arrow] Move cursor left
                        case ConsoleKey.LeftArrow:
                            if (_bufferCursorPos != 0)
                            {
                                MoveCursor(-1);
                                _bufferCursorPos--;
                            }
                            continue;

                        // [Right Arrow] Move cursor right
                        case ConsoleKey.RightArrow:
                            if (_bufferCursorPos != _buffer.Length)
                            {
                                MoveCursor(1);
                                _bufferCursorPos++;
                            }
                            continue;
                    }
                }
            }
        }

        /* Private Helpers */
        
        private static void EraseBuffer()
        {
            if (_isReading)
            {
                Console.SetCursorPosition(0, Console.CursorTop - (_prompt.Length + _bufferCursorPos) / Console.BufferWidth);
            }
        }

        private static void RestoreBuffer()
        {
            if (Console.CursorLeft != 0)
                Console.Write(new string(' ', Console.BufferWidth - Console.CursorLeft));

            if (_isReading)
            {
                Console.Write(_prompt);
                Console.Write(_buffer.ToString());
                MoveCursor(_bufferCursorPos - _buffer.Length);
            }
        }

        private static void MoveCursor(int move)
        {
            if (move == 0)
                return;

            move += Console.CursorLeft;
            int left = move % Console.BufferWidth;
            int top = Console.CursorTop + move / Console.BufferWidth;
            if (left < 0)
            {
                left += Console.BufferWidth;
                top -= 1;
            }
            Console.SetCursorPosition(left, top);
        }
    }
}
