using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GameBuilderEditor
{
    public class GameBuilderWindow : EditorWindow
    {
        [MenuItem("Assets/Game Builder")]
        private static void OpenWindow()
        {
            var window = GetWindow<GameBuilderWindow>(c_useUtilityWindow, "Game Builder");
            window.Show();
        }

        public CancellationTokenSource cts;
        public GameBuilderModel model;
        public SerializedObject serializedObject;
        public List<Business> business = new(1);

        private const bool c_useUtilityWindow = false;
        private const string c_modelDir_0 = "Assets";
        private const string c_modelDir_1 = "Editor";
        private const string c_modelFilename = "GameBuilderModel.asset";
        private const string c_preLog = "[game builder]: ";
        private const int c_createModel_retryDelay = 1000;
        private static readonly GUIContent[] s_platformContents = new GUIContent[]
        {
            new("Windows"),
            new("Windows\nServer"),
            new("Linux"),
            new("Linux\nServer"),
            new("Android")
        };

        private int _selectedEditingPlatformIndex;
        private Vector2 _infoScrollPos;
        private Vector2 _mainScrollPos;
        private Vector2 _buildSettingsPresets_scrollPos;

        private void OnGUI()
        {
            var scriptsCompiling = EditorApplication.isCompiling;
            if (scriptsCompiling)
            {
                GUILayout.FlexibleSpace();
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("cant use this window while scripts are compiling.");
                    GUILayout.FlexibleSpace();
                }
                GUILayout.FlexibleSpace();
                return;
            }

            cts ??= new();
            using (var scroll = new GUILayout.ScrollViewScope(_mainScrollPos))
            {
                _mainScrollPos = scroll.scrollPosition;
                if (business.Count > 0)
                {
                    GUILayout.Label("Busy...", EditorStyles.largeLabel);
                    foreach (var b in business)
                    {
                        try
                        {
                            b.onGui();
                        }
                        catch { }
                        EditorGUILayout.Separator();
                    }
                    return;
                }

                if (model == null)
                {
                    CreateModel_Business();
                }

                if (model != null)
                {
                    if (serializedObject == null)
                    {
                        serializedObject = new(model);
                        if (serializedObject == null)
                        {
                            Debug.LogWarningFormat("{0} could not create {1} from {2}", c_preLog, nameof(SerializedObject), nameof(model));
                            return;
                        }
                    }

                    // serializedObject is good here

                    serializedObject.Update();
                    _selectedEditingPlatformIndex = GUILayout.SelectionGrid(_selectedEditingPlatformIndex, s_platformContents, 6);
                    var prop = _selectedEditingPlatformIndex switch
                    {
                        1 => serializedObject.FindProperty(nameof(model.windowsServer)),
                        2 => serializedObject.FindProperty(nameof(model.linux)),
                        3 => serializedObject.FindProperty(nameof(model.linuxServer)),
                        4 => serializedObject.FindProperty(nameof(model.android)),
                        _ => serializedObject.FindProperty(nameof(model.windows)),
                    };

                    if (prop == null)
                    {
                        GUILayout.Label("unknown platform");
                    }
                    else
                    {
                        DrawPlatformOptions(prop);
                    }

                    EditorGUILayout.Separator();
                    DrawBuild();

                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private void DrawBuild()
        {
            var buildSettingsProp = serializedObject.FindProperty(nameof(GameBuilderModel.buildSettings));
            if (buildSettingsProp == null)
            {
                return;
            }

            GUILayout.Label("Build", EditorStyles.largeLabel);
            using (new GUILayout.HorizontalScope(GUILayout.ExpandHeight(true)))
            {
                using (new GUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(220)))
                {
                    // draw presets
                    GUILayout.Label("Presets", EditorStyles.boldLabel);
                    using (var scroll = new GUILayout.ScrollViewScope(_buildSettingsPresets_scrollPos))
                    {
                        _buildSettingsPresets_scrollPos = scroll.scrollPosition;
                        for (int i = 0; i < buildSettingsProp.arraySize; i++)
                        {
                            var preset = buildSettingsProp.GetArrayElementAtIndex(i);

                            var height = Mathf.Max(30, EditorStyles.wordWrappedLabel.CalcHeight(new(model.buildSettings[i].label), 70));

                            var mainRect = EditorGUILayout.GetControlRect(GUILayout.Width(205), GUILayout.Height(height));

                            if (model.SelectedBuildSettingsIndex == i)
                            {
                                EditorGUI.DrawRect(mainRect, new(0, 0, 1, 0.2f));
                            }

                            mainRect.x += 6;
                            mainRect.width -= 12;
                            var rect = mainRect;

                            rect.width -= 20 + 20 + 40;
                            EditorGUI.LabelField(rect, preset.FindPropertyRelative(nameof(GameBuilderModel.BuildSettings.label)).stringValue, EditorStyles.wordWrappedLabel);

                            rect.height = 20;
                            rect.y = mainRect.y + (mainRect.height - rect.height) / 2f;

                            rect.x += rect.width;
                            rect.width = 20;
                            if (GUI.Button(rect, "↑"))
                            {
                                buildSettingsProp.MoveArrayElement(i, i - 1);
                                serializedObject.ApplyModifiedProperties();
                                break;
                            }
                            rect.x += rect.width;
                            if (GUI.Button(rect, "↓"))
                            {
                                buildSettingsProp.MoveArrayElement(i, i + 1);
                                serializedObject.ApplyModifiedProperties();
                                break;
                            }
                            rect.x += rect.width;
                            rect.width = 40;
                            if (GUI.Button(rect, "Del"))
                            {
                                buildSettingsProp.DeleteArrayElementAtIndex(i);
                                serializedObject.ApplyModifiedProperties();
                                break;
                            }

                            // click
                            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && mainRect.Contains(Event.current.mousePosition))
                            {
                                model.SelectedBuildSettingsIndex = i;
                                Repaint();
                            }
                        }
                    }

                    if (GUILayout.Button("New"))
                    {
                        buildSettingsProp.arraySize++;
                        serializedObject.ApplyModifiedProperties();
                    }
                }

                model.SelectedBuildSettingsIndex = Mathf.Clamp(model.SelectedBuildSettingsIndex, 0, model.buildSettings.Length - 1);

                // show selected preset
                if (model.SelectedBuildSettingsIndex >= 0 && model.SelectedBuildSettingsIndex < buildSettingsProp.arraySize)
                {
                    using (new GUILayout.VerticalScope())
                    {
                        DrawBuildSettings(model.SelectedBuildSettingsIndex);
                    }
                }
            }

            model.SelectedBuildSettingsIndex = Mathf.Clamp(model.SelectedBuildSettingsIndex, 0, model.buildSettings.Length - 1);
            if (model.buildSettings.Length > 0)
            {
                var buildSettings = model.buildSettings[model.SelectedBuildSettingsIndex];

                using (new GUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
                {
                    var buildOptions = model.BuildingPlatform.GetOptions(model);
                    string path = model.BuildingPlatform.GetBuildPath(buildOptions, buildSettings, model.history.Length + 1);
                    GUILayout.Label(path);

                    if (GUILayout.Button("Copy Full"))
                    {
                        GUIUtility.systemCopyBuffer = Path.GetFullPath(path);
                    }
                    using (new EditorGuiUtilities.LabelWidth(70))
                        PlayerSettings.bundleVersion = EditorGUILayout.TextField("version", PlayerSettings.bundleVersion);
                }

                model.BuildingPlatform = (BuildingPlatform)EditorGUILayout.EnumPopup("Build Platform", model.BuildingPlatform);

                if (GUILayout.Button("Perform Build", GUILayout.Height(30), GUILayout.ExpandWidth(true)))
                {
                    PerformBuild_Business().ConfigureAwait(false);
                }
            }
        }

        private void DrawBuildSettings(int index)
        {
            var buildSettings = model.buildSettings[index];
            var buildSettingsProp = serializedObject.FindProperty(nameof(GameBuilderModel.buildSettings)).GetArrayElementAtIndex(index);
            var labelProp = buildSettingsProp.FindPropertyRelative(nameof(GameBuilderModel.BuildSettings.label));
            var buildOptionsProp = buildSettingsProp.FindPropertyRelative(nameof(GameBuilderModel.BuildSettings.buildOptions));
            var openInTerminalProp = buildSettingsProp.FindPropertyRelative(nameof(GameBuilderModel.BuildSettings.openInTerminal));
            var instancesToRunProp = buildSettingsProp.FindPropertyRelative(nameof(GameBuilderModel.BuildSettings.instancesToRun));
            var postBuildCommandProp = buildSettingsProp.FindPropertyRelative(nameof(GameBuilderModel.BuildSettings.postBuildCommand));
            var compressFilesProp = buildSettingsProp.FindPropertyRelative(nameof(GameBuilderModel.BuildSettings.compressFiles));
            var compressFilePathProp = buildSettingsProp.FindPropertyRelative(nameof(GameBuilderModel.BuildSettings.compressFilePath));
            var compressionLevelProp = buildSettingsProp.FindPropertyRelative(nameof(GameBuilderModel.BuildSettings.compressionLevel));
            var buildPathProp = buildSettingsProp.FindPropertyRelative(nameof(GameBuilderModel.BuildSettings.buildPath));

            EditorGUILayout.PropertyField(labelProp);
            EditorGUILayout.PropertyField(openInTerminalProp);
            EditorGUILayout.PropertyField(instancesToRunProp);
            EditorGUILayout.PropertyField(buildPathProp);
            EditorGUILayout.PropertyField(compressFilesProp);
            if (compressFilesProp.boolValue)
            {
                EditorGUILayout.PropertyField(compressFilePathProp);
                EditorGUILayout.PropertyField(compressionLevelProp);
                var platformOptions = model.BuildingPlatform.GetOptions(model);
                string compressedPath = model.BuildingPlatform.GetCompressedFilePath(platformOptions, buildSettings, model.history.Length + 1);
                EditorGUILayout.LabelField($"compresssed path: {compressedPath}");
            }
            EditorGUILayout.PropertyField(postBuildCommandProp, GUILayout.Height(100));
            EditorGUILayout.PropertyField(buildOptionsProp);
            using var scroll = new GUILayout.ScrollViewScope(_infoScrollPos);
            using (new EditorGUI.DisabledGroupScope(true))
            {
                _infoScrollPos = scroll.scrollPosition;
                GUILayout.Label(buildSettings.info, EditorStyles.wordWrappedLabel);
            }
        }

        private void DrawPlatformOptions(SerializedProperty prop)
        {
            EditorGUILayout.PropertyField(prop);
        }

        public async Task PerformBuild_Business()
        {
            if (model == null || model.buildSettings == null)
            {
                Debug.LogWarningFormat("{0}invalid model", c_preLog);
                return;
            }

            var buildSettings = model.buildSettings[model.SelectedBuildSettingsIndex];

            var platformOptions = model.BuildingPlatform.GetOptions(model);
            var buildTarget = model.BuildingPlatform.GetBuildTarget();
            var subTarget = model.BuildingPlatform.GetSubTarget();
            var targetGroup = model.BuildingPlatform.GetTargetGroup();
            var buildPath = model.BuildingPlatform.GetBuildPath(platformOptions, buildSettings, model.history.Length + 1);
            if (platformOptions == null || (int)buildTarget == -1 || subTarget == -1 || (int)targetGroup == -1 || string.IsNullOrEmpty(buildPath))
            {
                Debug.LogWarningFormat("{0}invalid model", c_preLog);
                return;
            }

            var performBuildBusiness = new Business()
            {
                onGui = () =>
                {
                    GUILayout.Label("Building in progresss");
                }
            };
            business.Add(performBuildBusiness);
            var r = await PerformBuild(
                buildOptions: buildSettings.buildOptions,
                scenes: platformOptions.scenes.Select(s => AssetDatabase.GetAssetPath(s)).ToArray(),
                target: buildTarget,
                subTarget: subTarget,
                targetGroup: targetGroup,
                extraScriptingDefines: platformOptions.scriptingDefines,
                buildPath: buildPath,
                ct: cts.Token
            );
            Debug.LogFormat("{0}build finished. \'{1}\' duration:\'{2} seconds\'", c_preLog, r.summary.result, r.summary.totalTime.TotalSeconds);

            // log reports
            try
            {
                Debug.LogFormat("{0}summery:\n{1}", c_preLog, JsonConvert.SerializeObject(r.summary, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            try
            {
                Debug.LogFormat("{0}steps:\n{1}", c_preLog, JsonConvert.SerializeObject(r.steps, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            try
            {
                Debug.LogFormat("{0}files:\n{1}", c_preLog, JsonConvert.SerializeObject(r.GetFiles(), Formatting.Indented));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            try
            {
                Debug.LogFormat("{0}stripping info:\n{1}", c_preLog, JsonConvert.SerializeObject(r.strippingInfo, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }


            // post build
            if (r.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                serializedObject?.ApplyModifiedProperties();
                Array.Resize(ref model.history, model.history.Length + 1);
                model.history[^1] = new()
                {
                    size = r.summary.totalSize,
                };
                serializedObject?.Update();

                var fileInfo = new FileInfo(r.summary.outputPath);

                var compressedFilePath = model.BuildingPlatform.GetCompressedFilePath(platformOptions, buildSettings, model.history.Length);

                // open in terminal
                if (buildSettings.openInTerminal)
                {
                    GameBuilderOsOperations.OpenTerminalAtDirectory(fileInfo.Directory.FullName);
                }

                // run instances
                for (int i = 0; i < buildSettings.instancesToRun; i++)
                {
                    GameBuilderOsOperations.OpenFile(fileInfo.FullName);
                }

                // compress
                if (buildSettings.compressFiles)
                {
                    try
                    {
                        var files = Directory.GetFiles(fileInfo.Directory.FullName, "**", SearchOption.AllDirectories)
                            .Where(f => !f.Contains("DoNotShip"))
                            .ToArray();
                        GameBuilderCompression.ZipFiles(files, compressedFilePath, buildSettings.compressionLevel);
                        Debug.LogFormat("successfully compressed into {0}", compressedFilePath);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("error while compressing. see the next log for exception");
                        Debug.LogException(ex);
                    }
                }

                // post build commands
                if (!string.IsNullOrEmpty(buildSettings.postBuildCommand))
                {
                    try
                    {
                        var cmd = string.Format(buildSettings.postBuildCommand, fileInfo.FullName, fileInfo.Directory.FullName,
                            Application.version, model.history.Length, "zip");
                        GameBuilderOsOperations.ExecuteBatch(cmd);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
            }

            business.Remove(performBuildBusiness);
        }

        private async Task<UnityEditor.Build.Reporting.BuildReport> PerformBuild(BuildOptions buildOptions, string[] scenes, BuildTarget target, int subTarget, BuildTargetGroup targetGroup, string[] extraScriptingDefines, string buildPath, CancellationToken ct)
        {
            BuildPlayerOptions options = new()
            {
                options = buildOptions,
                scenes = scenes,
                target = target,
                subtarget = subTarget,
                targetGroup = targetGroup,
                extraScriptingDefines = extraScriptingDefines,
                locationPathName = buildPath
            };
            var result = BuildPipeline.BuildPlayer(options);
            await Task.Yield();
            return result;
        }

        private async void CreateModel_Business()
        {
            var content = new GUIContent("Creating new model", $"creating an instance of {nameof(GameBuilderModel)} at {Path.Combine(c_modelDir_0, c_modelDir_1, c_modelFilename)}");
            Business createModelBusiness = new()
            {
                onGui = () =>
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(content);
                        GUILayout.FlexibleSpace();
                    }
                }
            };
            business.Add(createModelBusiness);
            model = await CreateModel(cts.Token);
            business.Remove(createModelBusiness);
        }

        private async Task<GameBuilderModel> CreateModel(CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                return null;
            }

            if (!AssetDatabase.IsValidFolder(Path.Combine(c_modelDir_0, c_modelDir_1)))
            {
                AssetDatabase.CreateFolder(c_modelDir_0, c_modelDir_1);
                AssetDatabase.Refresh();
            }

            string assetPath = Path.Combine(c_modelDir_0, c_modelDir_1, c_modelFilename);
            var asset = AssetDatabase.LoadAssetAtPath<GameBuilderModel>(assetPath);
            if (asset != null)
            {
                return asset;
            }

            Debug.LogFormat("{0}creating new model at {1}", c_preLog, assetPath);
            AssetDatabase.CreateAsset(CreateInstance<GameBuilderModel>(), assetPath);
            AssetDatabase.Refresh();

            asset = AssetDatabase.LoadAssetAtPath<GameBuilderModel>(assetPath);
            if (asset != null)
            {
                return asset;
            }

            // error
            Debug.LogWarningFormat("{0} error while fetching mode. retrying in {1} milliseconds", c_preLog, c_createModel_retryDelay);
            AssetDatabase.Refresh();
            await Task.Delay(c_createModel_retryDelay, ct);

            if (ct.IsCancellationRequested)
            {
                return null;
            }

            return await CreateModel(ct);
        }

        public class Business
        {
            public Action onGui;
        }

        public enum BuildingPlatform
        {
            Windows,
            Windows_Server,
            Linux,
            Linux_Server,
            Android
        }
    }

    public static class BuildingPlatformExtensions
    {
        public static GameBuilderModel.PlatformOptions GetOptions(this GameBuilderWindow.BuildingPlatform bp, GameBuilderModel model) => bp switch
        {
            GameBuilderWindow.BuildingPlatform.Windows => model.windows,
            GameBuilderWindow.BuildingPlatform.Windows_Server => model.windowsServer,
            GameBuilderWindow.BuildingPlatform.Linux => model.linux,
            GameBuilderWindow.BuildingPlatform.Linux_Server => model.linuxServer,
            GameBuilderWindow.BuildingPlatform.Android => model.android,
            _ => null
        };
        public static BuildTarget GetBuildTarget(this GameBuilderWindow.BuildingPlatform bp) => bp switch
        {
            GameBuilderWindow.BuildingPlatform.Windows => BuildTarget.StandaloneWindows64,
            GameBuilderWindow.BuildingPlatform.Windows_Server => BuildTarget.StandaloneWindows64,
            GameBuilderWindow.BuildingPlatform.Linux => BuildTarget.StandaloneLinux64,
            GameBuilderWindow.BuildingPlatform.Linux_Server => BuildTarget.StandaloneLinux64,
            GameBuilderWindow.BuildingPlatform.Android => BuildTarget.Android,
            _ => (BuildTarget)(-1)
        };
        public static int GetSubTarget(this GameBuilderWindow.BuildingPlatform bp) => bp switch
        {
            GameBuilderWindow.BuildingPlatform.Windows => (int)StandaloneBuildSubtarget.Player,
            GameBuilderWindow.BuildingPlatform.Windows_Server => (int)StandaloneBuildSubtarget.Server,
            GameBuilderWindow.BuildingPlatform.Linux => (int)StandaloneBuildSubtarget.Player,
            GameBuilderWindow.BuildingPlatform.Linux_Server => (int)StandaloneBuildSubtarget.Server,
            GameBuilderWindow.BuildingPlatform.Android => 0,
            _ => -1
        };
        public static BuildTargetGroup GetTargetGroup(this GameBuilderWindow.BuildingPlatform bp) => bp switch
        {
            GameBuilderWindow.BuildingPlatform.Windows => BuildTargetGroup.Standalone,
            GameBuilderWindow.BuildingPlatform.Windows_Server => BuildTargetGroup.Standalone,
            GameBuilderWindow.BuildingPlatform.Linux => BuildTargetGroup.Standalone,
            GameBuilderWindow.BuildingPlatform.Linux_Server => BuildTargetGroup.Standalone,
            GameBuilderWindow.BuildingPlatform.Android => BuildTargetGroup.Android,
            _ => (BuildTargetGroup)(-1)
        };
        public static string GetBuildPath(this GameBuilderWindow.BuildingPlatform bp, GameBuilderModel.PlatformOptions ps, GameBuilderModel.BuildSettings bs, int buildNumber)
        {
            try
            {
                return bp switch
                {
                    GameBuilderWindow.BuildingPlatform.Windows =>
                        string.Format(bs.buildPath, ps.platformName, ps.platformShortName, Application.version, ".exe", buildNumber),
                    GameBuilderWindow.BuildingPlatform.Windows_Server =>
                        string.Format(bs.buildPath, ps.platformName, ps.platformShortName, Application.version, ".exe", buildNumber),
                    GameBuilderWindow.BuildingPlatform.Linux =>
                        string.Format(bs.buildPath, ps.platformName, ps.platformShortName, Application.version, string.Empty, buildNumber),
                    GameBuilderWindow.BuildingPlatform.Linux_Server =>
                        string.Format(bs.buildPath, ps.platformName, ps.platformShortName, Application.version, string.Empty, buildNumber),
                    GameBuilderWindow.BuildingPlatform.Android =>
                        string.Format(bs.buildPath, ps.platformName, ps.platformShortName, Application.version, ".apk", buildNumber),
                    _ => string.Empty
                };
            }
            catch
            {
                return "invalid path";
            }
        }

        public static string GetCompressedFilePath(this GameBuilderWindow.BuildingPlatform bp, GameBuilderModel.PlatformOptions ps, GameBuilderModel.BuildSettings bs, int buildNumber)
        {
            try
            {
                return bp switch
                {
                    GameBuilderWindow.BuildingPlatform.Windows =>
                        string.Format(bs.compressFilePath, ps.platformName, ps.platformShortName, Application.version, ".exe", buildNumber, ".zip"),
                    GameBuilderWindow.BuildingPlatform.Windows_Server =>
                        string.Format(bs.compressFilePath, ps.platformName, ps.platformShortName, Application.version, ".exe", buildNumber, ".zip"),
                    GameBuilderWindow.BuildingPlatform.Linux =>
                        string.Format(bs.compressFilePath, ps.platformName, ps.platformShortName, Application.version, string.Empty, buildNumber, ".zip"),
                    GameBuilderWindow.BuildingPlatform.Linux_Server =>
                        string.Format(bs.compressFilePath, ps.platformName, ps.platformShortName, Application.version, string.Empty, buildNumber, ".zip"),
                    GameBuilderWindow.BuildingPlatform.Android =>
                        string.Format(bs.compressFilePath, ps.platformName, ps.platformShortName, Application.version, ".apk", buildNumber, ".zip"),
                    _ => string.Empty
                };

            }
            catch
            {
                return "invalid path";
            }
        }
    }
}
