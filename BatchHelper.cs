using System;
using UnityEditor;
using UnityEngine;

namespace GameBuilder
{
    /// <summary>
    /// A class to be used by unity commandline batch mode
    /// </summary>
    public class BatchHelper
    {
        public static void BuildLinuxServer() => BuildForPlatform(GameBuilderWindow.BuildingPlatform.Linux_Server);
        public static void BuildWindowsServer() => BuildForPlatform(GameBuilderWindow.BuildingPlatform.Windows_Server);

        private static void BuildForPlatform(GameBuilderWindow.BuildingPlatform platform)
        {
            var model = GameBuilderWindow.CreateModel(default).Result;
            GameBuilderModel.BuildingPlatform = platform;
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-buildPresetIndex" && int.TryParse(args[i + 1], out var index) &&
                    model.buildSettings.Length > index)
                {
                    GameBuilderModel.SelectedBuildSettingsIndex = index;
                }
            }
            Debug.Log(
                $"Building for {GameBuilderModel.BuildingPlatform} using settings " +
                $"{GameBuilderModel.SelectedBuildSettingsIndex}" +
                $"({model.buildSettings[GameBuilderModel.SelectedBuildSettingsIndex].label})");
            GameBuilderWindow.PerformBuild_Business(model).Wait();
            EditorApplication.Exit(0);
        }
    }
}