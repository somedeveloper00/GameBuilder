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

        public static void ExecuteBatch(string path, string command)
        {
            Process.Start("cmd.exe", $"/k cd {path} && cd && {command}");
        }
    }
}
