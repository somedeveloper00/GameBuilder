using System.Threading;
using UnityEditor;
using UnityEngine;

namespace GameBuilderEditor
{
    /// <summary>
    /// This class is responsible for automating the build process. It uses <see cref="GameBuilderWindow"/> to process the build itself, 
    /// and uses git to check for new commits. If a new commit is detected, it will start a timer and build the game after the timer is up.
    /// </summary>
    public sealed class GameBuildAutomate : EditorWindow
    {
        public static float checkIntervals = 10;
        public static int buildTimer = 10;
        private string _lastCommitHash;
        private float _remainingTimeForNewBuild;
        private CancellationTokenSource _cts;
        private bool _buildOnNextGui;

        [MenuItem("Assets/Automate Build")]
        private static void ShowWindow()
        {
            GetWindow(typeof(GameBuilderWindow));
            GetWindow(typeof(GameBuildAutomate));
        }

        private void OnEnable()
        {
            RestartBackgroundCheckThread();
        }

        private void OnDisable()
        {
            _cts?.Cancel();
        }

        private void OnGUI()
        {
            checkIntervals = EditorGUILayout.FloatField("Check Intervals", checkIntervals);
            checkIntervals = Mathf.Max(2, checkIntervals);
            buildTimer = EditorGUILayout.IntField("Build Timer", buildTimer);
            EditorGUILayout.LabelField($"Last Commit Hash: ", _lastCommitHash);

            if (_remainingTimeForNewBuild > 0)
            {
                EditorGUILayout.LabelField($"New build starting in {_remainingTimeForNewBuild}");
                if (GUILayout.Button("Cancel"))
                {
                    _remainingTimeForNewBuild = 0;
                    RestartBackgroundCheckThread();
                }
            }

            if (_buildOnNextGui)
            {
                _buildOnNextGui = false;
                Build();
                RestartBackgroundCheckThread();
            }
        }

        private void Build()
        {
            var gameBuilderWindow = GetWindow(typeof(GameBuilderWindow)) as GameBuilderWindow;
            _ = gameBuilderWindow.PerformBuild_Business();
        }

        private void RestartBackgroundCheckThread()
        {
            _cts?.Cancel();
            _cts = new();

            new Thread(() =>
            {
                var token = _cts.Token;
                while (true)
                {
                    Thread.Sleep((int)(checkIntervals * 1000));
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }
                    Debug.Log("checking for new commits...");
                    if (BranchUpdated())
                    {
                        Debug.Log("starting build");
                        _remainingTimeForNewBuild = buildTimer;
                        while (_remainingTimeForNewBuild > 0)
                        {
                            Thread.Sleep(1000);
                            Debug.Log(_remainingTimeForNewBuild);
                            _remainingTimeForNewBuild--;
                        }
                        if (token.IsCancellationRequested)
                        {
                            Debug.Log("cancelled build");
                            return;
                        }
                        _buildOnNextGui = true;
                        return;
                    }
                }
            }).Start();
        }

        private bool BranchUpdated()
        {
            GitPull();
            var currentCommitHash = GetCommitHash();
            if (currentCommitHash != _lastCommitHash)
            {
                Debug.Log($"new commit detected: {_lastCommitHash} -> {currentCommitHash}");
                _lastCommitHash = currentCommitHash;
                return true;
            }

            return false;
        }

        private void GitPull()
        {
            // perform git pull
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "git";
            process.StartInfo.Arguments = "pull";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.WaitForExit();
        }

        private string GetCommitHash()
        {
            // get current commit hash
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "git";
            process.StartInfo.Arguments = "rev-parse HEAD";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.WaitForExit();
            return process.StandardOutput.ReadToEnd().Trim();
        }
    }
}