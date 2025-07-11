using System;
using System.IO;
using System.Text;

public static class Logger
{
    private static DualWriter _dualWriter;

    public static void Initialize()
    {
        if (_dualWriter != null) return;

        _dualWriter = new DualWriter(Console.Out);
        Console.SetOut(_dualWriter);
    }

    public static void SaveLog(string filePath)
    {
        if (_dualWriter == null) return;

        File.WriteAllText(filePath, _dualWriter.GetLog(), Encoding.UTF8);
    }

    private class DualWriter : TextWriter
    {
        private readonly TextWriter _consoleOut;
        private readonly StringBuilder _logBuffer = new();

        public DualWriter(TextWriter consoleOut)
        {
            _consoleOut = consoleOut;
        }

        public override Encoding Encoding => _consoleOut.Encoding;

        public string GetLog() => _logBuffer.ToString();

        public override void Write(char value)
        {
            _consoleOut.Write(value);
            _logBuffer.Append(value);
        }

        public override void Write(string value)
        {
            _consoleOut.Write(value);
            _logBuffer.Append(value);
        }

        public override void WriteLine(string value)
        {
            _consoleOut.WriteLine(value);
            _logBuffer.AppendLine(value);
        }
    }
}
