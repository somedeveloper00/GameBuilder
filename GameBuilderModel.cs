﻿using System;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GameBuilder
{
    public sealed class GameBuilderModel : ScriptableObject
    {
        public PlatformOptions android;
        public PlatformOptions windows;
        public PlatformOptions linux;
        public PlatformOptions windowsServer;
        public PlatformOptions linuxServer;

        public BuildSettings[] buildSettings = Array.Empty<BuildSettings>();
        public BuildHistory[] history = Array.Empty<BuildHistory>();

        public static int SelectedBuildSettingsIndex
        {
            get => EditorPrefs.GetInt("gamebuilder.selectedBuildSettingsIndex", 0);
            set => EditorPrefs.SetInt("gamebuilder.selectedBuildSettingsIndex", value);
        }
        public static GameBuilderWindow.BuildingPlatform BuildingPlatform
        {
            get => (GameBuilderWindow.BuildingPlatform)EditorPrefs.GetInt("gamebuilder.buildingPlatform", 0);
            set => EditorPrefs.SetInt("gamebuilder.buildingPlatform", (int)value);
        }

        [Serializable]
        public class PlatformOptions
        {
            public string platformName;
            public string platformShortName;
            public SceneAsset[] scenes;
            public string[] scriptingDefines;
        }

        [Serializable]
        public class BuildSettings
        {
            [Tooltip(
                "{0}: platform name\n" +
                "{1}: platform short name without uppercase and without space\n" +
                "{2}: version\n" +
                "{3}: platform-specific file extension\n" +
                "{4}: build number (from history)")]
            public string buildPath = "Builds/{0}/{1}_{2}/app{3}";
            public string label = "New Settings";
            public BuildOptions buildOptions;

            [Tooltip("opens build folder in terminal if succeeded")]
            public bool openInTerminal;

            [Tooltip("Instances to run if succeeded")]
            public int instancesToRun;

            [Tooltip("Whether or not to compress game files")]
            public bool compressFiles;

            [Tooltip(
                "{0}: platform name\n" +
                "{1}: platform short name without uppercase and without space\n" +
                "{2}: version\n" +
                "{3}: platform-specific file extension\n" +
                "{4}: build number (from history)\n"+
                "{5}: compression method file extension")]
            public string compressFilePath;

            [Tooltip("compression level for compressing the files.")]
            public System.IO.Compression.CompressionLevel compressionLevel;

            [Tooltip("{0}: full output path\n" +
                     "{1}: output directory path\n" +
                     "{2}: build version\n" +
                     "{3}: build number (from history)\n"+
                     "{4}: compression method file extension")]
            [TextArea(3, 10)]
            public string postBuildCommand;

            public string info
            {
                get
                {
                    StringBuilder sb = new();
                    if (buildOptions.ContainsFast(BuildOptions.None))
                        sb.AppendLine("None:\n    Perform the specified build without any special settings or extra tasks.");
                    if (buildOptions.ContainsFast(BuildOptions.Development))
                        sb.AppendLine("Development:\n    Build a development version of the player.");
                    if (buildOptions.ContainsFast(BuildOptions.AutoRunPlayer))
                        sb.AppendLine("AutoRunPlayer:\n    Run the built player.");
                    if (buildOptions.ContainsFast(BuildOptions.ShowBuiltPlayer))
                        sb.AppendLine("ShowBuiltPlayer:\n    Show the built player.");
                    if (buildOptions.ContainsFast(BuildOptions.BuildAdditionalStreamedScenes))
                        sb.AppendLine("BuildAdditionalStreamedScenes:\n    Build a compressed asset bundle that contains streamed Scenes loadable with the UnityWebRequest class.");
                    if (buildOptions.ContainsFast(BuildOptions.AcceptExternalModificationsToPlayer))
                        sb.AppendLine("AcceptExternalModificationsToPlayer:\n    Used when building Xcode (iOS) or Eclipse (Android) projects.");
                    if (buildOptions.ContainsFast(BuildOptions.InstallInBuildFolder))
                        sb.AppendLine("InstallInBuildFolder:\n    ");
                    if (buildOptions.ContainsFast(BuildOptions.CleanBuildCache))
                        sb.AppendLine("CleanBuildCache:\n    Clear all cached build results, resulting in a full rebuild of all scripts and all player data.");
                    if (buildOptions.ContainsFast(BuildOptions.ConnectWithProfiler))
                        sb.AppendLine("ConnectWithProfiler:\n    Start the player with a connection to the profiler in the editor.");
                    if (buildOptions.ContainsFast(BuildOptions.AllowDebugging))
                        sb.AppendLine("AllowDebugging:\n    Allow script debuggers to attach to the player remotely.");
                    if (buildOptions.ContainsFast(BuildOptions.SymlinkSources))
                        sb.AppendLine("SymlinkSources:\n    Symlink sources when generating the project. This is useful if you're changing source files inside the generated project and want to bring the changes back into your Unity project or a package.");
                    if (buildOptions.ContainsFast(BuildOptions.UncompressedAssetBundle))
                        sb.AppendLine("UncompressedAssetBundle:\n    Don't compress the data when creating the asset bundle.");
                    if (buildOptions.ContainsFast(BuildOptions.ConnectToHost))
                        sb.AppendLine("ConnectToHost:\n    Sets the Player to connect to the Editor.");
                    if (buildOptions.ContainsFast(BuildOptions.CustomConnectionID))
                        sb.AppendLine("CustomConnectionID:\n    Determines if the player should be using the custom connection ID.");
                    if (buildOptions.ContainsFast(BuildOptions.BuildScriptsOnly))
                        sb.AppendLine("BuildScriptsOnly:\n    Only build the scripts in a Project.");
                    if (buildOptions.ContainsFast(BuildOptions.PatchPackage))
                        sb.AppendLine("PatchPackage:\n    Patch a Development app package rather than completely rebuilding it. Supported platforms: Android.");
                    if (buildOptions.ContainsFast(BuildOptions.ForceEnableAssertions))
                        sb.AppendLine("ForceEnableAssertions:\n    Include assertions in the build. By default, the assertions are only included in development builds.");
                    if (buildOptions.ContainsFast(BuildOptions.CompressWithLz4))
                        sb.AppendLine("CompressWithLz4:\n    Use chunk-based LZ4 compression when building the Player.");
                    if (buildOptions.ContainsFast(BuildOptions.CompressWithLz4HC))
                        sb.AppendLine("CompressWithLz4HC:\n    Use chunk-based LZ4 high-compression when building the Player.");
                    if (buildOptions.ContainsFast(BuildOptions.ComputeCRC))
                        sb.AppendLine("ComputeCRC:\n    ");
                    if (buildOptions.ContainsFast(BuildOptions.StrictMode))
                        sb.AppendLine("StrictMode:\n    Do not allow the build to succeed if any errors are reporting during it.");
                    if (buildOptions.ContainsFast(BuildOptions.IncludeTestAssemblies))
                        sb.AppendLine("IncludeTestAssemblies:\n    Build will include Assemblies for testing.");
                    if (buildOptions.ContainsFast(BuildOptions.NoUniqueIdentifier))
                        sb.AppendLine("NoUniqueIdentifier:\n    Will force the buildGUID to all zeros.");
                    if (buildOptions.ContainsFast(BuildOptions.WaitForPlayerConnection))
                        sb.AppendLine("WaitForPlayerConnection:\n    Sets the Player to wait for player connection on player start.");
                    if (buildOptions.ContainsFast(BuildOptions.EnableCodeCoverage))
                        sb.AppendLine("EnableCodeCoverage:\n    Enables code coverage. You can use this as a complimentary way of enabling code coverage on platforms that do not support command line arguments.");
                    if (buildOptions.ContainsFast(BuildOptions.EnableDeepProfilingSupport))
                        sb.AppendLine("EnableDeepProfilingSupport:\n    Enables Deep Profiling support in the player.");
                    if (buildOptions.ContainsFast(BuildOptions.DetailedBuildReport))
                        sb.AppendLine("DetailedBuildReport:\n    Generates more information in the BuildReport.");
                    if (buildOptions.ContainsFast(BuildOptions.ShaderLivelinkSupport))
                        sb.AppendLine("ShaderLivelinkSupport:\n    Enable Shader Livelink support.");


                    return sb.ToString();
                }
            }
        }

        [Serializable]
        public class BuildHistory
        {
            public ulong size;
        }
    }

    public static class BuildOptionsExtension
    {
        public static bool ContainsFast(this BuildOptions self, BuildOptions other) => (self & other) == other;
    }
}