using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build.Reporting;
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

        public int SelectedBuildSettingsIndex
        {
            get => EditorPrefs.GetInt("gamebuilder.selectedBuildSettingsIndex", 0);
            set => EditorPrefs.SetInt("gamebuilder.selectedBuildSettingsIndex", value);
        }

        private const bool c_useUtilityWindow = false;
        private const string c_modelDir_0 = "Assets";
        private const string c_modelDir_1 = "Editor";
        private const string c_modelFilename = "GameBuilderModel.asset";
        private const string c_preLog = "[game builder] ";
        private const int c_createModel_retryDelay = 1000;

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
                            Debug.LogWarningFormat("{0}could not create {1} from {2}", c_preLog, nameof(SerializedObject), nameof(model));
                            return;
                        }
                    }

                    serializedObject.Update();
                    DrawWindow();
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private void DrawWindow()
        {
            var buildSettingsProp = serializedObject.FindProperty(nameof(GameBuilderModel.buildSettings));
            if (buildSettingsProp == null)
            {
                return;
            }

            using (new GUILayout.HorizontalScope(GUILayout.ExpandHeight(true)))
            {
                DrawBuildSettingsSelection(buildSettingsProp);

                SelectedBuildSettingsIndex = Mathf.Clamp(SelectedBuildSettingsIndex, 0, model.buildSettings.Length - 1);

                // show selected preset
                if (SelectedBuildSettingsIndex >= 0 && SelectedBuildSettingsIndex < buildSettingsProp.arraySize)
                {
                    using (new GUILayout.VerticalScope())
                    {
                        DrawBuildSettingsConfiguration(SelectedBuildSettingsIndex);
                    }
                }
            }

            DrawBuildLayout();
        }

        private void DrawBuildSettingsSelection(SerializedProperty buildSettingsProp)
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(220)))
            {
                // draw build settings
                GUILayout.Label("Build Settings", EditorStyles.boldLabel);
                using (var scroll = new GUILayout.ScrollViewScope(_buildSettingsPresets_scrollPos))
                {
                    _buildSettingsPresets_scrollPos = scroll.scrollPosition;
                    for (int i = 0; i < buildSettingsProp.arraySize; i++)
                    {
                        var preset = buildSettingsProp.GetArrayElementAtIndex(i);

                        var height = Mathf.Max(30,
                            EditorStyles.wordWrappedLabel.CalcHeight(new(model.buildSettings[i].label), 70));

                        var mainRect = EditorGUILayout.GetControlRect(GUILayout.Width(205), GUILayout.Height(height));

                        if (SelectedBuildSettingsIndex == i)
                        {
                            EditorGUI.DrawRect(mainRect, new(0, 0, 1, 0.2f));
                        }

                        mainRect.x += 6;
                        mainRect.width -= 12;
                        var rect = mainRect;

                        rect.width -= 20 + 20 + 40;
                        EditorGUI.LabelField(rect,
                            preset.FindPropertyRelative(nameof(GameBuilderModel.BuildSettings.label)).stringValue,
                            EditorStyles.wordWrappedLabel);

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
                        if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                            mainRect.Contains(Event.current.mousePosition))
                        {
                            SelectedBuildSettingsIndex = i;
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
        }

        private void DrawBuildSettingsConfiguration(int index)
        {
            var buildSettings = model.buildSettings[index];
            var buildSettingsProp = serializedObject.FindProperty(nameof(GameBuilderModel.buildSettings)).GetArrayElementAtIndex(index);
            var buildingPlatformProp = buildSettingsProp.FindPropertyRelative(nameof(GameBuilderModel.BuildSettings.buildingPlatform));
            var scenesProp = buildSettingsProp.FindPropertyRelative(nameof(GameBuilderModel.BuildSettings.scenes));
            var scriptingDefinesProp = buildSettingsProp.FindPropertyRelative(nameof(GameBuilderModel.BuildSettings.scriptingDefines));
            var labelProp = buildSettingsProp.FindPropertyRelative(nameof(GameBuilderModel.BuildSettings.label));
            var buildOptionsProp = buildSettingsProp.FindPropertyRelative(nameof(GameBuilderModel.BuildSettings.buildOptions));
            var openInTerminalProp = buildSettingsProp.FindPropertyRelative(nameof(GameBuilderModel.BuildSettings.openInTerminal));
            var instancesToRunProp = buildSettingsProp.FindPropertyRelative(nameof(GameBuilderModel.BuildSettings.instancesToRun));
            var compressFilesProp = buildSettingsProp.FindPropertyRelative(nameof(GameBuilderModel.BuildSettings.compressFiles));
            var compressFilePathProp = buildSettingsProp.FindPropertyRelative(nameof(GameBuilderModel.BuildSettings.compressFilePath));
            var compressionLevelProp = buildSettingsProp.FindPropertyRelative(nameof(GameBuilderModel.BuildSettings.compressionLevel));
            var buildPathProp = buildSettingsProp.FindPropertyRelative(nameof(GameBuilderModel.BuildSettings.buildPath));

            EditorGUILayout.PropertyField(buildingPlatformProp);
            EditorGUILayout.PropertyField(scenesProp);
            EditorGUILayout.PropertyField(scriptingDefinesProp);
            EditorGUILayout.PropertyField(labelProp);
            EditorGUILayout.PropertyField(openInTerminalProp);
            EditorGUILayout.PropertyField(instancesToRunProp);
            EditorGUILayout.PropertyField(buildPathProp);
            EditorGUILayout.PropertyField(compressFilesProp);
            if (compressFilesProp.boolValue)
            {
                EditorGUILayout.PropertyField(compressFilePathProp);
                EditorGUILayout.PropertyField(compressionLevelProp);
                string compressedPath = buildSettings.GetCompressedFilePath();
                EditorGUILayout.LabelField($"compresssed path: {compressedPath}");
            }
            EditorGUILayout.PropertyField(buildOptionsProp);
            using var scroll = new GUILayout.ScrollViewScope(_infoScrollPos);
            using (new EditorGUI.DisabledGroupScope(true))
            {
                _infoScrollPos = scroll.scrollPosition;
                GUILayout.Label(buildSettings.Info, EditorStyles.wordWrappedLabel);
            }
        }

        private void DrawBuildLayout()
        {
            SelectedBuildSettingsIndex = Mathf.Clamp(SelectedBuildSettingsIndex, 0, model.buildSettings.Length - 1);
            if (model.buildSettings.Length > 0)
            {
                var buildSettings = model.buildSettings[SelectedBuildSettingsIndex];

                using (new GUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
                {
                    string path = buildSettings.GetBuildPath();
                    GUILayout.Label(path);

                    if (GUILayout.Button("Copy Full"))
                    {
                        GUIUtility.systemCopyBuffer = Path.GetFullPath(path);
                    }
                    using (new EditorGuiUtilities.LabelWidth(70))
                        PlayerSettings.bundleVersion = EditorGUILayout.TextField("version", PlayerSettings.bundleVersion);
                }

                if (buildSettings.buildingPlatform == BuildingPlatform.Android &&
                    !buildSettings.buildOptions.ContainsFast(BuildOptions.Development))
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        PlayerSettings.Android.keystorePass = EditorGUILayout.PasswordField("Keystore Password", PlayerSettings.Android.keystorePass);
                        PlayerSettings.Android.keyaliasPass = EditorGUILayout.PasswordField("Key Alias Password", PlayerSettings.Android.keyaliasPass);
                    }
                }

                if (GUILayout.Button("Perform Build", GUILayout.Height(30), GUILayout.ExpandWidth(true)))
                {
                    PerformBuild_Business().ConfigureAwait(false);
                }
            }
        }

        public async Task PerformBuild_Business()
        {
            if (model == null || model.buildSettings == null)
            {
                Debug.LogWarningFormat("{0}invalid model", c_preLog);
                return;
            }

            var buildSettings = model.buildSettings[SelectedBuildSettingsIndex];

            var buildTarget = buildSettings.GetBuildTarget();
            var subTarget = buildSettings.GetSubTarget();
            var targetGroup = buildSettings.GetTargetGroup();
            var buildPath = buildSettings.GetBuildPath();
            if (subTarget == -1 || (int)targetGroup == -1 || string.IsNullOrEmpty(buildPath))
            {
                Debug.LogWarningFormat("{0}invalid model", c_preLog);
                return;
            }

            var performBuildBusiness = new Business()
            {
                onGui = () => { GUILayout.Label("Building in progresss"); }
            };
            business.Add(performBuildBusiness);
            var r = await PerformBuild(
                buildOptions: buildSettings.buildOptions,
                scenes: buildSettings.scenes.Select(s => AssetDatabase.GetAssetPath(s)).ToArray(),
                target: buildTarget,
                subTarget: subTarget,
                targetGroup: targetGroup,
                extraScriptingDefines: buildSettings.scriptingDefines,
                buildPath: buildPath
            );
            Debug.LogFormat("{0}build finished. \'{1}\' duration:\'{2} seconds\'", c_preLog, r.summary.result,
                r.summary.totalTime.TotalSeconds);

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
                Debug.LogFormat("{0}stripping info:\n{1}", c_preLog,
                    JsonConvert.SerializeObject(r.strippingInfo, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }


            // post build
            if (r.summary.result == BuildResult.Succeeded)
            {
                var fileInfo = new FileInfo(r.summary.outputPath);
                var compressedFilePath = buildSettings.GetCompressedFilePath();

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
            }

            business.Remove(performBuildBusiness);
        }

        private async Task<BuildReport> PerformBuild(BuildOptions buildOptions, string[] scenes,
            BuildTarget target, int subTarget, BuildTargetGroup targetGroup, string[] extraScriptingDefines, string buildPath)
        {
            BuildPlayerOptions options = new()
            {
                options = buildOptions,
                scenes = scenes,
                target = target,
                subtarget = subTarget,
                targetGroup = targetGroup,
                extraScriptingDefines = extraScriptingDefines,
                locationPathName = buildPath,
            };
            var result = BuildPipeline.BuildPlayer(options);
            await Task.Yield();
            return result;
        }

        private async void CreateModel_Business()
        {
            var content = new GUIContent("Creating new model",
                $"creating an instance of {nameof(GameBuilderModel)} at {Path.Combine(c_modelDir_0, c_modelDir_1, c_modelFilename)}");
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
            Debug.LogWarningFormat("{0}error while fetching mode. retrying in {1} milliseconds", c_preLog,
                c_createModel_retryDelay);
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
    }
}