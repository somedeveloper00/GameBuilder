using System;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GameBuilderEditor
{
    public sealed class GameBuilderModel : ScriptableObject
    {
        public BuildSettings[] buildSettings = Array.Empty<BuildSettings>();

        [Serializable]
        public class BuildSettings
        {
            public BuildingPlatform buildingPlatform;
            public SceneAsset[] scenes;
            public string[] scriptingDefines;

            [Tooltip(
                "{0}: version\n" +
                "{1}: platform-specific file extension")]
            public string buildPath = "Builds/{0}/Game{1}";

            public string label = "New Settings";

            public BuildOptions buildOptions;

            [Tooltip("opens build folder in terminal if succeeded")]
            public bool openInTerminal;

            [Tooltip("Instances to run if succeeded")]
            public int instancesToRun;

            [Tooltip("Whether or not to compress game files")]
            public bool compressFiles;

            [Tooltip(
                "{0}: version\n" +
                "{1}: platform-specific file extension\n" +
                "{2}: compression method file extension")]
            public string compressFilePath;

            [Tooltip("compression level for compressing the files.")]
            public System.IO.Compression.CompressionLevel compressionLevel;

            public string Info
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
    }

    public enum BuildingPlatform
    {
        Windows,
        WindowsServer,
        Linux,
        LinuxServer,
        Android,
        WebGL
    }

    public static class BuildOptionsExtension
    {
        public static bool ContainsFast(this BuildOptions self, BuildOptions other) => (self & other) == other;
    }
}
