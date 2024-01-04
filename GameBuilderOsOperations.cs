using System.Diagnostics;

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

        public static string ExecuteBatch(string path, string command)
        {
            var proc = new Process();
            proc.StartInfo.FileName = path;
            proc.StartInfo.Arguments = command;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.CreateNoWindow = true;
            proc.Start();
            return proc.StandardOutput.ReadToEnd();
        }
    }
}
