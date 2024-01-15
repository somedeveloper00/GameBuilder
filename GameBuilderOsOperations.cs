using System;
using System.Diagnostics;
using System.IO;

namespace GameBuilderEditor
{
    public static class GameBuilderOsOperations
    {
        public static void OpenTerminalAtDirectory(string path)
        {
            new Process
            {
                StartInfo = new()
                {
                    CreateNoWindow = false,
#if UNITY_EDITOR_WIN
                    FileName = "cmd",
                    Arguments = $"/k cd \"{path}\"",
#else
#warning not supported
#endif

                    WindowStyle = ProcessWindowStyle.Normal,
                }
            }.Start();
        }

        public static void OpenFile(string path)
        {
            new Process
            {
                StartInfo = new()
                {
                    FileName = path
                }
            }.Start();
        }

        public static string ExecuteBatch(string command)
        {
            var proc = new Process();
            proc.StartInfo.FileName =
#if UNITY_EDITOR_WIN
                "cmd.exe";
#elif UNITY_EDITOR_LINUX || UNITY_EDITOR_OSX
                "/bin/bash";
#else
#warning not supported 
                null;
#endif
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.CreateNoWindow = true;
            proc.Start();
            using (var writer = proc.StandardInput)
            {
                if (writer.BaseStream.CanWrite)
                {
                    var lines = command.Split(Environment.NewLine);
                    foreach (var line in lines)
                    {
                        writer.WriteLine(line);
                    }
                }
            }
            return proc.StandardOutput.ReadToEnd();
        }
    }
}
