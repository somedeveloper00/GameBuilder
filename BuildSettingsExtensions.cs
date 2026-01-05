using UnityEditor;
using UnityEngine;

namespace GameBuilderEditor
{
    public static class BuildSettingsExtensions
    {
        public static BuildTarget GetBuildTarget(this GameBuilderModel.BuildSettings settings) =>
            settings.buildingPlatform switch
            {
                BuildingPlatform.Windows => BuildTarget.StandaloneWindows64,
                BuildingPlatform.WindowsServer => BuildTarget.StandaloneWindows64,
                BuildingPlatform.Linux => BuildTarget.StandaloneLinux64,
                BuildingPlatform.LinuxServer => BuildTarget.StandaloneLinux64,
                BuildingPlatform.Android => BuildTarget.Android,
                BuildingPlatform.WebGL => BuildTarget.WebGL,
                _ => (BuildTarget)(-1)
            };

        public static int GetSubTarget(this GameBuilderModel.BuildSettings settings) => settings.buildingPlatform switch
        {
            BuildingPlatform.Windows => (int)StandaloneBuildSubtarget.Player,
            BuildingPlatform.WindowsServer => (int)StandaloneBuildSubtarget.Server,
            BuildingPlatform.Linux => (int)StandaloneBuildSubtarget.Player,
            BuildingPlatform.LinuxServer => (int)StandaloneBuildSubtarget.Server,
            BuildingPlatform.Android => 0,
            BuildingPlatform.WebGL => 0,
            _ => -1
        };

        public static BuildTargetGroup GetTargetGroup(this GameBuilderModel.BuildSettings settings) =>
            settings.buildingPlatform switch
            {
                BuildingPlatform.Windows => BuildTargetGroup.Standalone,
                BuildingPlatform.WindowsServer => BuildTargetGroup.Standalone,
                BuildingPlatform.Linux => BuildTargetGroup.Standalone,
                BuildingPlatform.LinuxServer => BuildTargetGroup.Standalone,
                BuildingPlatform.Android => BuildTargetGroup.Android,
                BuildingPlatform.WebGL => BuildTargetGroup.WebGL,
                _ => (BuildTargetGroup)(-1)
            };

        public static string GetBuildPath(this GameBuilderModel.BuildSettings settings)
        {
            try
            {
                return settings.buildingPlatform switch
                {
                    BuildingPlatform.Windows =>
                        string.Format(settings.buildPath, Application.version, ".exe", GameBuilderWindow.CurrentBuildNumber),
                    BuildingPlatform.WindowsServer =>
                        string.Format(settings.buildPath, Application.version, ".exe", GameBuilderWindow.CurrentBuildNumber),
                    BuildingPlatform.Linux =>
                        string.Format(settings.buildPath, Application.version, string.Empty, GameBuilderWindow.CurrentBuildNumber),
                    BuildingPlatform.LinuxServer =>
                        string.Format(settings.buildPath, Application.version, string.Empty, GameBuilderWindow.CurrentBuildNumber),
                    BuildingPlatform.Android =>
                        string.Format(settings.buildPath, Application.version, ".apk", GameBuilderWindow.CurrentBuildNumber),
                    BuildingPlatform.WebGL =>
                        string.Format(settings.buildPath, Application.version, string.Empty, GameBuilderWindow.CurrentBuildNumber),
                    _ => string.Empty
                };
            }
            catch
            {
                return "invalid path";
            }
        }

        public static string GetVersion(this GameBuilderModel.BuildSettings settings)
        {
            return string.Format(settings.versionNumber, PlayerSettings.bundleVersion, GameBuilderWindow.CurrentBuildNumber);
        }
    }
}