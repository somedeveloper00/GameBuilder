using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Pool;
using Debug = UnityEngine.Debug;

namespace GameBuilderEditor
{
    public class GameBuilderWindow : EditorWindow
    {
        [MenuItem("Assets/Game Builder")]
        private static void OpenWindow()
        {
            var window = GetWindow<GameBuilderWindow>(c_useUtilityWindow, "Game Builder");
            window.minSize = new(600, 400);
            window.Show();
        }

        public CancellationTokenSource cts;
        public GameBuilderModel model;
        public SerializedObject serializedObject;
        public List<Business> business = new(1);

        public static int SelectedBuildSettingsIndex
        {
            get => EditorPrefs.GetInt("gamebuilder.selectedBuildSettingsIndex", 0);
            set => EditorPrefs.SetInt("gamebuilder.selectedBuildSettingsIndex", value);
        }

        public static int SelectedBuildSettingsRange
        {
            get => EditorPrefs.GetInt("gamebuilder.selectedBuildSettingsRange", 0);
            set => EditorPrefs.SetInt("gamebuilder.selectedBuildSettingsRange", value);
        }

        public static int CurrentBuildNumber
        {
            get => EditorPrefs.GetInt("gamebuilder.currentBuildNumber", 0);
            set => EditorPrefs.SetInt("gamebuilder.currentBuildNumber", value);
        }

        public float LeftPaneWidth
        {
            get => EditorPrefs.GetFloat("gamebuilder.leftPaneWidth", 220);
            set => EditorPrefs.SetFloat("gamebuilder.leftPaneWidth", value);
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
        private Vector2 _postProcessorScrollPos;
        private Texture2D _splitterDefault;
        private Texture2D _splitterHover;
        private Texture2D _splitterDragging;
        private bool _resizingLeftPane;
        private bool _draggingItem;
        private bool _draggingRangeSelection;
        private CancellationTokenSource _repaintCts;

        private void OnDisable() => _repaintCts?.Cancel();

        private void OnGUI()
        {
            HandleRepaintTask();
            EnsureTexturesInitialized();
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
            using var scroll = new GUILayout.ScrollViewScope(_mainScrollPos);
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
                CreateModel_Business();

            if (model != null)
            {
                if (serializedObject == null)
                {
                    serializedObject = new(model);
                    if (serializedObject == null)
                    {
                        Debug.LogWarningFormat($"{c_preLog}could not create {{0}} from {{1}}", nameof(SerializedObject), nameof(model));
                        return;
                    }
                }

                serializedObject.Update();
                HandleNavigationShortcuts();
                DrawWindow();
                serializedObject.ApplyModifiedProperties();
            }

        }

        private void HandleRepaintTask()
        {
            if (Event.current.type == EventType.MouseEnterWindow)
            {
                _repaintCts?.Cancel();
                _repaintCts = new();
                var token = _repaintCts.Token;
                _ = StartRepaintTask(token);
            }
            else if (Event.current.type == EventType.MouseLeaveWindow)
            {
                _repaintCts?.Cancel();
                _repaintCts = null;
            }

            async Task StartRepaintTask(CancellationToken token)
            {
                while (!token.IsCancellationRequested)
                {
                    Repaint();
                    await Task.Delay(100);
                }
            }
        }

        private void EnsureTexturesInitialized()
        {
            _splitterDefault = new Texture2D(3, 3);
            _splitterDefault.SetPixels32(new Color32[]
            {
                new(125, 125, 125, 125), new(125, 125, 125, 255), new(125, 125, 125, 125),
                new(125, 125, 125, 125), new(125, 125, 125, 255), new(125, 125, 125, 125),
                new(125, 125, 125, 125), new(125, 125, 125, 255), new(125, 125, 125, 125),
            });
            _splitterDefault.Apply();
            _splitterHover = new Texture2D(3, 3);
            _splitterHover.SetPixels32(new Color32[]
            {
                new(170, 170, 170, 50), new(170, 170, 170, 255), new(170, 170, 170, 50),
                new(170, 170, 170, 50), new(170, 170, 170, 255), new(170, 170, 170, 50),
                new(170, 170, 170, 50), new(170, 170, 170, 255), new(170, 170, 170, 50),
            });
            _splitterHover.Apply();
            _splitterDragging = new Texture2D(3, 3);
            _splitterDragging.SetPixels32(new Color32[]
            {
                new(200, 200, 200, 25), new(200, 200, 200, 255), new(200, 200, 200, 25),
                new(200, 200, 200, 25), new(200, 200, 200, 255), new(200, 200, 200, 25),
                new(200, 200, 200, 25), new(200, 200, 200, 255), new(200, 200, 200, 25),
            });
            _splitterDragging.Apply();
        }

        private void HandleNavigationShortcuts()
        {
            if (Event.current.type != EventType.KeyDown)
                return;
            if (Event.current.keyCode == KeyCode.UpArrow)
            {
                SelectedBuildSettingsIndex = Mathf.Max(SelectedBuildSettingsIndex - 1, 0);
                Repaint();
            }
            else if (Event.current.keyCode == KeyCode.DownArrow)
            {
                SelectedBuildSettingsIndex = Mathf.Min(SelectedBuildSettingsIndex + 1, model.buildSettings.Length - 1);
                Repaint();
            }
            else if (Event.current.keyCode == KeyCode.PageUp)
            {
                SelectedBuildSettingsIndex = 0;
                Repaint();
            }
            else if (Event.current.keyCode == KeyCode.PageDown)
            {
                SelectedBuildSettingsIndex = model.buildSettings.Length - 1;
                Repaint();
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

                // splitter
                var rect = EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true), GUILayout.Width(11));
                GUILayout.Space(11);
                EditorGUILayout.EndHorizontal();
                bool isHoveringOverSplitter = false;
                // handle splitter mouse drag
                if (Event.current != null)
                {
                    isHoveringOverSplitter = rect.Contains(Event.current.mousePosition);
                    if (isHoveringOverSplitter)
                        EditorGUIUtility.AddCursorRect(rect, MouseCursor.SplitResizeLeftRight);


                    switch (Event.current.rawType)
                    {
                        case EventType.MouseDown:
                            if (isHoveringOverSplitter)
                            {
                                _resizingLeftPane = true;
                                Repaint();
                            }
                            break;
                        case EventType.MouseDrag:
                            if (_resizingLeftPane)
                            {
                                LeftPaneWidth += Event.current.delta.x;
                                Repaint();
                            }
                            break;
                        case EventType.MouseUp:
                            if (_resizingLeftPane)
                                Repaint();
                            _resizingLeftPane = false;
                            break;
                    }
                }
                rect.x += 3; // small but wide enough to not harm OLED monitors
                rect.width -= 6;
                GUI.DrawTexture(rect, _resizingLeftPane ? _splitterDragging : isHoveringOverSplitter ? _splitterHover : _splitterDefault);

                SelectedBuildSettingsIndex = Mathf.Clamp(SelectedBuildSettingsIndex, 0, model.buildSettings.Length - 1);

                // show selected preset
                if (SelectedBuildSettingsIndex >= 0 && SelectedBuildSettingsIndex < buildSettingsProp.arraySize)
                {
                    using (new GUILayout.VerticalScope())
                    {
                        if (SelectedBuildSettingsRange == 0)
                        {
                            DrawBuildSettingsConfiguration(SelectedBuildSettingsIndex);
                        }
                    }
                }
            }

            DrawBuildLayout();
        }

        private void DrawBuildSettingsSelection(SerializedProperty buildSettingsProp)
        {
            using var layoutScope = new EditorGUILayout.VerticalScope(GUILayout.Width(LeftPaneWidth));

            // draw build settings
            GUILayout.Space(10);
            GUILayout.Label("Build Settings", EditorStyles.boldLabel);
            bool? parentExpanded = null;
            using (var scroll = new GUILayout.ScrollViewScope(_buildSettingsPresets_scrollPos))
            {
                _buildSettingsPresets_scrollPos = scroll.scrollPosition;
                for (int i = 0; i < buildSettingsProp.arraySize; i++)
                {
                    var preset = buildSettingsProp.GetArrayElementAtIndex(i);
                    var label = preset.FindPropertyRelative(nameof(GameBuilderModel.BuildSettings.label)).stringValue;
                    var isGroupHeaderProp = preset.FindPropertyRelative(nameof(GameBuilderModel.BuildSettings.isGroupHeader));
                    if (!isGroupHeaderProp.boolValue && parentExpanded == false) // exit early to not do any layouting
                        continue;

                    var mainRect = EditorGUILayout.GetControlRect(GUILayout.Height(isGroupHeaderProp.boolValue ? 25 : 20));
                    var mouseHovering = Event.current != null && mainRect.Contains(Event.current.mousePosition);

                    // background
                    if (SelectedBuildSettingsIndex == i)
                        EditorGUI.DrawRect(mainRect, EditorGUIUtility.isProSkin ? new Color32(44, 93, 135, 255) : new Color32(98, 144, 216, 255));
                    else if (
                        (SelectedBuildSettingsRange > 0 && i > SelectedBuildSettingsIndex && i <= SelectedBuildSettingsIndex + SelectedBuildSettingsRange) ||
                        (SelectedBuildSettingsRange < 0 && i < SelectedBuildSettingsIndex && i >= SelectedBuildSettingsIndex + SelectedBuildSettingsRange))
                        EditorGUI.DrawRect(mainRect, new(0, 0, 1, 0.3f));
                    else if (mouseHovering)
                        EditorGUI.DrawRect(mainRect, EditorGUIUtility.isProSkin ? new(0.2f, 0.2f, 0.2f) : new(0.8f, 0.8f, 0.8f));

                    if (isGroupHeaderProp.boolValue)
                    {
                        isGroupHeaderProp.isExpanded = EditorGUI.Foldout(mainRect, isGroupHeaderProp.isExpanded, GUIContent.none);
                        mainRect.x += i != SelectedBuildSettingsIndex && mouseHovering && _draggingItem ? 60 : 20;
                        GUI.Label(mainRect, label, EditorStyles.boldLabel);
                        mainRect.x -= i != SelectedBuildSettingsIndex && mouseHovering && _draggingItem ? 60 : 20;
                        parentExpanded = isGroupHeaderProp.isExpanded;
                    }
                    else
                    {
                        var rect = mainRect;
                        rect.width -= 20 + 20 + 40;
                        if (parentExpanded == true) // indent a little if this is a child
                            rect.x += 7;
                        if (i != SelectedBuildSettingsIndex && mouseHovering && _draggingItem)
                            rect.x += 60;
                        EditorGUI.LabelField(rect, label);
                    }

                    { // buttons
                        var rect = mainRect;
                        rect.x += rect.width - 20 - 20 - 40;
                        rect.width = 20;
                        if (GUI.Button(rect, "↑"))
                        {
                            if (SelectedBuildSettingsIndex == i)
                                SelectedBuildSettingsIndex--;
                            buildSettingsProp.MoveArrayElement(i, i - 1);
                            serializedObject.ApplyModifiedProperties();
                            Repaint();
                            break;
                        }
                        rect.x += rect.width;
                        if (GUI.Button(rect, "↓"))
                        {
                            if (SelectedBuildSettingsIndex == i)
                                SelectedBuildSettingsIndex++;
                            buildSettingsProp.MoveArrayElement(i, i + 1);
                            serializedObject.ApplyModifiedProperties();
                            Repaint();
                            break;
                        }
                        rect.x += rect.width;
                        rect.width = 40;
                        if (GUI.Button(rect, "Del"))
                        {
                            buildSettingsProp.DeleteArrayElementAtIndex(i);
                            serializedObject.ApplyModifiedProperties();
                            if (i <= SelectedBuildSettingsIndex)
                                SelectedBuildSettingsIndex--;
                            Repaint();
                            break;
                        }
                    }

                    if (Event.current != null)
                    {
                        if (mouseHovering)
                        {
                            // start move
                            if (!_draggingRangeSelection && i == SelectedBuildSettingsIndex && mouseHovering && Event.current.type == EventType.MouseDrag)
                            {
                                _draggingItem = true;
                            }
                            if (!_draggingItem)
                            {
                                // range select
                                if (Event.current.type is EventType.MouseDown or EventType.MouseDrag && Event.current.shift)
                                {
                                    _draggingRangeSelection = true;
                                    SelectedBuildSettingsRange = i - SelectedBuildSettingsIndex;
                                }
                                // normal select
                                else if (!_draggingRangeSelection && Event.current.type is EventType.MouseUp or EventType.MouseDrag && !Event.current.shift)
                                {
                                    SelectedBuildSettingsIndex = i;
                                    SelectedBuildSettingsRange = 0;
                                }
                            }

                            // move preview
                            if (i != SelectedBuildSettingsIndex && _draggingItem)
                            {
                                var rect = mainRect;
                                if (i > SelectedBuildSettingsIndex)
                                    rect.y += rect.height - 2;
                                rect.height = 2;
                                EditorGUI.DrawRect(rect, new(1, 1, 1, 1));
                                rect = mainRect;
                                if (i < SelectedBuildSettingsIndex)
                                    rect.y += 4;
                                EditorGUI.LabelField(rect, "move here", EditorStyles.miniBoldLabel);
                                Repaint();
                            }
                            if (Event.current.type == EventType.MouseUp)
                            {
                                // perform move
                                if (_draggingItem && i != SelectedBuildSettingsIndex)
                                {
                                    GetSelectionRange(out var leastIndex, out var mostIndex);
                                    int increment;
                                    if (i < SelectedBuildSettingsIndex)
                                    {
                                        // move from least to most
                                        increment = +1;
                                    }
                                    else
                                    {
                                        // move from most to least
                                        increment = -1;
                                        (leastIndex, mostIndex) = (mostIndex, leastIndex);
                                    }
                                    for (int j = leastIndex; j != mostIndex + increment; j += increment)
                                    {
                                        int fromIndex = j;
                                        int toIndex = fromIndex + (i - SelectedBuildSettingsIndex);

                                        // ignore out of bounds
                                        if (fromIndex < 0 || fromIndex >= buildSettingsProp.arraySize || toIndex < 0 || toIndex >= buildSettingsProp.arraySize)
                                            break;

                                        buildSettingsProp.MoveArrayElement(fromIndex, toIndex);
                                        var sb = new StringBuilder();
                                        for (int k = 0; k < buildSettingsProp.arraySize; k++)
                                            sb.AppendLine(buildSettingsProp.GetArrayElementAtIndex(k).name);
                                    }
                                    serializedObject.ApplyModifiedProperties();
                                    SelectedBuildSettingsIndex = i; // keep old selection
                                }
                                _draggingItem = false;
                                Repaint();
                            }
                        }
                    }
                }

                if (Event.current != null && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                {
                    _draggingItem = false; // cancel a move
                    SelectedBuildSettingsRange = 0; // deselect range
                    Event.current.Use();
                }
            }

            if (GUILayout.Button("New"))
            {
                if (model.buildSettings.Length > 0) // duplicate the whole range
                {
                    serializedObject.ApplyModifiedProperties();

                    GetSelectionRange(out var leastIndex, out var mostIndex);
                    Undo.RecordObject(model, $"duplicated {leastIndex} to {mostIndex}");

                    // copy
                    var tmp = ListPool<GameBuilderModel.BuildSettings>.Get();
                    tmp.AddRange(model.buildSettings);
                    for (int i = mostIndex; i >= leastIndex; i--)
                        tmp.Insert(mostIndex + 1, JsonUtility.FromJson<GameBuilderModel.BuildSettings>(JsonUtility.ToJson(model.buildSettings[i])));
                    model.buildSettings = tmp.ToArray();
                    ListPool<GameBuilderModel.BuildSettings>.Release(tmp);

                    serializedObject.Update();
                    SelectedBuildSettingsIndex += 1 + Mathf.Abs(SelectedBuildSettingsRange);
                }
                else
                {
                    buildSettingsProp.arraySize++;
                    serializedObject.ApplyModifiedProperties();
                }
            }

            // finish dragging selection range
            if (_draggingRangeSelection && Event.current.type == EventType.MouseUp)
                _draggingRangeSelection = false;
        }

        private void DrawBuildSettingsConfiguration(int index)
        {
            var buildSettings = model.buildSettings[index];
            var buildSettingsProp =
                serializedObject.FindProperty(nameof(GameBuilderModel.buildSettings))
                    .GetArrayElementAtIndex(index);
            var isGroupHeaderProp = buildSettingsProp.FindPropertyRelative(nameof(GameBuilderModel.BuildSettings.isGroupHeader));
            var labelProp = buildSettingsProp.FindPropertyRelative(nameof(GameBuilderModel.BuildSettings.label));
            var buildingPlatformProp =
                buildSettingsProp.FindPropertyRelative(nameof(GameBuilderModel.BuildSettings.buildingPlatform));
            var scenesProp = buildSettingsProp.FindPropertyRelative(nameof(GameBuilderModel.BuildSettings.scenes));
            var scriptingDefinesProp =
                buildSettingsProp.FindPropertyRelative(nameof(GameBuilderModel.BuildSettings.scriptingDefines));
            var buildOptionsProp =
                buildSettingsProp.FindPropertyRelative(nameof(GameBuilderModel.BuildSettings.buildOptions));
            var postProcessorsProp =
                buildSettingsProp.FindPropertyRelative(nameof(GameBuilderModel.BuildSettings.postProcessors));
            var buildPathProp =
                buildSettingsProp.FindPropertyRelative(nameof(GameBuilderModel.BuildSettings.buildPath));
            var versionNumberProp =
                buildSettingsProp.FindPropertyRelative(nameof(GameBuilderModel.BuildSettings.versionNumber));

            EditorGUILayout.PropertyField(labelProp);
            EditorGUILayout.PropertyField(isGroupHeaderProp);
            if (isGroupHeaderProp.boolValue)
                return;

            EditorGUILayout.PropertyField(buildingPlatformProp);
            using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(scenesProp);
                using (new GUILayout.VerticalScope())
                {
                    float t = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 80;
                    EditorGUILayout.PropertyField(scriptingDefinesProp);
                    EditorGUILayout.PropertyField(buildPathProp);
                    using (new EditorGuiUtilities.LabelWidth(110))
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            EditorGUILayout.PropertyField(versionNumberProp);
                            using (new EditorGUI.DisabledGroupScope(true))
                                GUILayout.Label(buildSettings.GetVersion());
                        }
                    }

                    EditorGUILayout.PropertyField(buildOptionsProp);
                    EditorGUIUtility.labelWidth = t;
                    using (new EditorGUI.DisabledGroupScope(true))
                    {
                        using (var scroll = new GUILayout.ScrollViewScope(_infoScrollPos))
                        {
                            _infoScrollPos = scroll.scrollPosition;
                            GUILayout.Label(buildSettings.Info, EditorStyles.wordWrappedLabel);
                        }
                    }
                }
            }

            // draw post processors
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // header
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(new GUIContent(postProcessorsProp.displayName, postProcessorsProp.tooltip),
                        EditorStyles.largeLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("+"))
                    {
                        postProcessorsProp.arraySize++;
                        serializedObject.ApplyModifiedProperties();
                    }
                }

                DrawPostProcessorListLayout(postProcessorsProp);
            }
        }

        private void DrawPostProcessorListLayout(SerializedProperty postProcessorsProp)
        {
            using (var scroll = new GUILayout.ScrollViewScope(_postProcessorScrollPos))
            {
                _postProcessorScrollPos = scroll.scrollPosition;
                // array
                for (int i = 0; i < postProcessorsProp.arraySize; i++)
                {
                    var ppProp = postProcessorsProp.GetArrayElementAtIndex(i);

                    // header
                    using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        ppProp.isExpanded = EditorGUILayout.Foldout(ppProp.isExpanded, GUIContent.none, true);
                        if (GUILayout.Button("X"))
                        {
                            postProcessorsProp.DeleteArrayElementAtIndex(i);
                            break;
                        }

                        GUILayout.Label(ppProp.managedReferenceValue != null
                            ? ObjectNames.NicifyVariableName(ppProp.managedReferenceValue?.GetType().Name)
                            : "empty", EditorStyles.boldLabel);

                        GUILayout.FlexibleSpace();

                        // change type
                        if (GUILayout.Button("Change Type", EditorStyles.toolbarDropDown))
                        {
                            int cachedIndex = i;
                            var menu = new GenericMenu();
                            foreach (var type in
                                     AppDomain.CurrentDomain
                                         .GetAssemblies()
                                         .SelectMany(a => a.GetTypes())
                                         .Where(t => !t.IsAbstract && t.IsValueType &&
                                                     typeof(IBuildPostProcessor).IsAssignableFrom(t)))
                            {
                                bool selected = type == ppProp.managedReferenceValue?.GetType();
                                if (selected)
                                    menu.AddDisabledItem(new GUIContent(ObjectNames.NicifyVariableName(type.Name)),
                                        true);
                                else
                                    menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(type.Name)), false, () =>
                                    {
                                        serializedObject.UpdateIfRequiredOrScript();
                                        postProcessorsProp.GetArrayElementAtIndex(cachedIndex).managedReferenceValue =
                                            Activator.CreateInstance(type);
                                        serializedObject.ApplyModifiedProperties();
                                    });
                            }
                            menu.ShowAsContext();
                        }
                        if (i > 0 && GUILayout.Button("\u2191"))
                        {
                            var t1 = postProcessorsProp.GetArrayElementAtIndex(i);
                            var t2 = postProcessorsProp.GetArrayElementAtIndex(i - 1);
                            var v1 = t1.managedReferenceValue;
                            var v2 = t2.managedReferenceValue;
                            t1.managedReferenceValue = v2;
                            t2.managedReferenceValue = v1;
                            serializedObject.ApplyModifiedProperties();
                        }
                        if (i < postProcessorsProp.arraySize - 1 && GUILayout.Button("\u2193"))
                        {
                            var t1 = postProcessorsProp.GetArrayElementAtIndex(i);
                            var t2 = postProcessorsProp.GetArrayElementAtIndex(i + 1);
                            var v1 = t1.managedReferenceValue;
                            var v2 = t2.managedReferenceValue;
                            t1.managedReferenceValue = v2;
                            t2.managedReferenceValue = v1;
                            serializedObject.ApplyModifiedProperties();
                        }
                    }
                    // body
                    if (ppProp.isExpanded && ppProp.managedReferenceValue != null)
                    {
                        EditorGUILayout.PropertyField(ppProp, GUIContent.none, true);
                    }
                }
            }
        }

        private async void DrawBuildLayout()
        {
            SelectedBuildSettingsIndex = Mathf.Clamp(SelectedBuildSettingsIndex, 0, model.buildSettings.Length - 1);
            if (model.buildSettings.Length > 0)
            {
                var buildSettings = model.buildSettings[SelectedBuildSettingsIndex];
                using (new GUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
                {
                    string path = buildSettings.GetBuildPath();
                    GUILayout.Label(path);

                    if (GUILayout.Button("Copy Full", GUILayout.Width(80)))
                    {
                        GUIUtility.systemCopyBuffer = Path.GetDirectoryName(path);
                    }

                    using (new EditorGuiUtilities.LabelWidth(60))
                        PlayerSettings.bundleVersion =
                            EditorGUILayout.TextField("version", PlayerSettings.bundleVersion);
                    if (GUILayout.Button("↑", GUILayout.Width(15)))
                    {
                        PlayerSettings.bundleVersion =
                            StringUtils.IncrementIntegerInString(PlayerSettings.bundleVersion, 1);
                    }
                    if (GUILayout.Button("↓", GUILayout.Width(15)))
                    {
                        PlayerSettings.bundleVersion =
                            StringUtils.IncrementIntegerInString(PlayerSettings.bundleVersion, -1);
                    }
                }

                if (buildSettings.buildingPlatform == BuildingPlatform.Android &&
                    !buildSettings.buildOptions.ContainsFast(BuildOptions.Development))
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        PlayerSettings.Android.keystorePass =
                            EditorGUILayout.PasswordField("Keystore Password", PlayerSettings.Android.keystorePass);
                        PlayerSettings.Android.keyaliasPass = EditorGUILayout.PasswordField("Key Alias Password",
                            PlayerSettings.Android.keyaliasPass);
                    }
                }
                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Perform Build", GUILayout.Height(30), GUILayout.ExpandWidth(true)))
                        BuildSelectedConfigs();
                    if (GUILayout.Button("Clean", GUILayout.Height(30), GUILayout.Width(50)))
                        CleanBuildDirectory();
                }
            }
        }

        private async void BuildSelectedConfigs()
        {
            try
            {
                GetSelectionRange(out int leastIndex, out int mostIndex);
                for (int i = leastIndex; i <= mostIndex; i++)
                    await PerformBuild_Business(i).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void CleanBuildDirectory()
        {
            GetSelectionRange(out var leastIndex, out var mostIndex);
            for (int i = leastIndex; i <= mostIndex; i++)
            {
                var buildSettings = model.buildSettings[i];
                var buildPath = Path.GetDirectoryName(buildSettings.GetBuildPath());
                if (Directory.Exists(buildPath))
                    Directory.Delete(buildPath, true);
                else if (File.Exists(buildPath))
                    File.Delete(buildPath);
            }
        }

        public async Task PerformBuild_Business(int index)
        {
            if (model == null || model.buildSettings == null)
            {
                Debug.LogWarningFormat($"{c_preLog}invalid model");
                return;
            }

            var buildSettings = model.buildSettings[index];
            if (buildSettings.isGroupHeader)
                return;

            var buildTarget = buildSettings.GetBuildTarget();
            var subTarget = buildSettings.GetSubTarget();
            var targetGroup = buildSettings.GetTargetGroup();
            var buildPath = buildSettings.GetBuildPath();
            if (subTarget == -1 || (int)targetGroup == -1 || string.IsNullOrEmpty(buildPath))
            {
                Debug.LogWarningFormat($"{c_preLog}invalid model");
                return;
            }

            var performBuildBusiness = new Business()
            {
                onGui = () => { GUILayout.Label($"Building in progresss ({buildSettings.label})"); }
            };

            var preVersion = PlayerSettings.bundleVersion;
            PlayerSettings.bundleVersion = buildSettings.GetVersion();

            business.Add(performBuildBusiness);
            try
            {
                Debug.LogFormat($"{c_preLog}building config \"{{0}}\" ver.{{1}}", buildSettings.label, buildSettings.GetVersion());
                var r = await PerformBuild(
                    buildOptions: buildSettings.buildOptions,
                    scenes: buildSettings.scenes.Select(AssetDatabase.GetAssetPath).ToArray(),
                    target: buildTarget,
                    subTarget: subTarget,
                    targetGroup: targetGroup,
                    extraScriptingDefines: buildSettings.scriptingDefines,
                    buildPath: buildPath,
                    postProcessors: buildSettings.postProcessors
                );
                CurrentBuildNumber++;

                if (r != null)
                {
                    Debug.LogFormat($"{c_preLog}build finished. \'{{0}}\' duration:\'{{1}} seconds\'",
                        r.summary.result, r.summary.totalTime.TotalSeconds);
                }
            }
            finally
            {
                PlayerSettings.bundleVersion = preVersion;
            }
            business.Remove(performBuildBusiness);
        }

        private static Task<BuildReport> PerformBuild(BuildOptions buildOptions, string[] scenes, BuildTarget target, int subTarget, BuildTargetGroup targetGroup, string[] extraScriptingDefines, string buildPath, IBuildPostProcessor[] postProcessors)
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
            if (!BuildPipeline.IsBuildTargetSupported(targetGroup, target))
            {
                Debug.LogWarning($"{c_preLog}Build target is not installed");
                return null;
            }
            BuildPostProcessorInitializer.postProcessors = postProcessors;
            var result = BuildPipeline.BuildPlayer(options);
            return Task.FromResult(result);
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

            Debug.LogFormat($"{c_preLog}creating new model at {{0}}", assetPath);
            AssetDatabase.CreateAsset(CreateInstance<GameBuilderModel>(), assetPath);
            AssetDatabase.Refresh();

            asset = AssetDatabase.LoadAssetAtPath<GameBuilderModel>(assetPath);
            if (asset != null)
            {
                return asset;
            }

            // error
            Debug.LogWarningFormat($"{c_preLog}error while fetching mode. retrying in {{0}} milliseconds",
                c_createModel_retryDelay);
            AssetDatabase.Refresh();
            await Task.Delay(c_createModel_retryDelay, ct);

            if (ct.IsCancellationRequested)
            {
                return null;
            }

            return await CreateModel(ct);
        }

        private static void GetSelectionRange(out int leastIndex, out int mostIndex)
        {
            leastIndex = SelectedBuildSettingsRange < 0 ? SelectedBuildSettingsIndex + SelectedBuildSettingsRange : SelectedBuildSettingsIndex;
            mostIndex = SelectedBuildSettingsRange > 0 ? SelectedBuildSettingsIndex + SelectedBuildSettingsRange : SelectedBuildSettingsIndex;
        }

        public class Business
        {
            public Action onGui;
        }
    }
}