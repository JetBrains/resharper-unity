using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Reflection;
using System;
using UnityEditorInternal;

/// <summary>
/// Automated profiler test system that runs performance tests, captures profiler data,
/// and automatically navigates to target method samples in the profiler.
/// </summary>
[InitializeOnLoad]
public static class ProfilerAutomation
{
    #region Constants

    private const string LOG_PREFIX = "[ProfilerAutomation]";
    private const int HIERARCHY_REBUILD_WAIT_MS = 1000;

    // SessionState Keys
    private const string STATE_KEY = "ProfilerAutomation_State";
    private const string START_FRAME_KEY = "ProfilerAutomation_StartFrame";
    private const string CONFIG_SCRIPT_NAME_KEY = "ProfilerAutomation_ScriptName";
    private const string CONFIG_FRAMES_TO_RUN_KEY = "ProfilerAutomation_FramesToRun";

    #endregion

    #region Configuration

    /// <summary>
    /// Configuration for profiler test execution
    /// </summary>
    public class TestConfiguration
    {
        public string ScenePath = "Assets/Scenes/ProfilerTestScene.unity";
        public string TargetScriptName = "UnoptimizedMonoBehaviour";
        public Type ComponentType = typeof(UnoptimizedMonoBehaviour);
        public int FramesToRun = 100;
        public string TestObjectName = "PerformanceTestObject";
    }

    #endregion

    #region State Management

    private enum TestState
    {
        Idle,
        Starting,
        Running,
        Stopping,
        Analyzing
    }

    private static TestState CurrentState
    {
        get => (TestState)SessionState.GetInt(STATE_KEY, (int)TestState.Idle);
        set => SessionState.SetInt(STATE_KEY, (int)value);
    }

    #endregion

    #region Profiler Analysis State

    // Reflection objects for TreeViewController access
    private static object profilerTreeViewController;
    private static EventInfo itemDoubleClickedEvent;
    private static Delegate itemDoubleClickedDelegate;

    #endregion

    #region Initialization

    static ProfilerAutomation()
    {
        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.update += OnEditorUpdate;
        LogInfo("ProfilerAutomation initialized");
    }

    #endregion

    #region Public API - Menu Items

    [MenuItem("Tools/Run Profiler Test")]
    public static void RunProfilerTest()
    {
        LogInfo("=== Starting Profiler Test ===");
        CurrentState = TestState.Idle;
        RunProfilerTest(new TestConfiguration());
    }

    /// <summary>
    /// Command-line entry point for running profiler tests
    /// Call with: Unity -executeMethod ProfilerAutomation.RunProfilerTestFromCommandLine
    /// </summary>
    public static void RunProfilerTestFromCommandLine()
    {
        LogInfo("=== Starting Profiler Test from Command Line ===");
        LogInfo("Waiting for Unity to be ready...");

        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                LogInfo("Unity is still compiling or updating, waiting...");
                EditorApplication.delayCall += RunProfilerTestFromCommandLine;
                return;
            }

            LogInfo("Unity is ready, starting profiler test");
            CurrentState = TestState.Idle;
            RunProfilerTest(new TestConfiguration());
        };
    }

    [MenuItem("Tools/Analyze Profiler Data", false, 1000)]
    public static void AnalyzeProfilerData()
    {
        LogInfo("=== Manual Profiler Analysis Triggered ===");
        AnalyzeProfilerData(nameof(UnoptimizedMonoBehaviour) + ".Update");
    }

    #endregion

    #region Public API - Core Methods

    public static void RunProfilerTest(TestConfiguration config)
    {
        LogInfo($"RunProfilerTest called with config: Scene={config.ScenePath}, Component={config.ComponentType?.Name}, Frames={config.FramesToRun}");

        if (CurrentState != TestState.Idle)
        {
            LogWarning($"Profiler test is already running (Current State: {CurrentState})");
            return;
        }

        if (config.ComponentType == null)
        {
            LogError("ComponentType cannot be null");
            return;
        }

        CurrentState = TestState.Starting;
        LogInfo($"State changed to: {CurrentState}");

        // Store configuration in SessionState
        SessionState.SetString(CONFIG_SCRIPT_NAME_KEY, config.TargetScriptName);
        SessionState.SetInt(CONFIG_FRAMES_TO_RUN_KEY, config.FramesToRun);
        LogInfo($"Stored configuration - TargetScript: {config.TargetScriptName}, FramesToRun: {config.FramesToRun}");

        try
        {
            SetupScene(config);
            SetupProfiler();
            StartPlayMode();
        }
        catch (Exception e)
        {
            LogError($"Failed to start profiler test: {e.Message}\n{e.StackTrace}");
            CleanupState();
        }
    }

    #endregion

    #region Scene Setup

    private static void SetupScene(TestConfiguration config)
    {
        LogInfo($"Setting up scene: {config.ScenePath}");

        // Create new scene
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        LogInfo($"Created new scene: {newScene.name}");

        // Create GameObject with target component
        GameObject testObj = new GameObject(config.TestObjectName);
        LogInfo($"Created GameObject: {testObj.name}");

        if (!testObj.TryGetComponent(config.ComponentType, out _))
        {
            testObj.AddComponent(config.ComponentType);
            LogInfo($"Added component: {config.ComponentType.Name}");
        }

        // Save scene
        if (!EditorSceneManager.SaveScene(newScene, config.ScenePath))
        {
            throw new Exception($"Failed to save scene to {config.ScenePath}");
        }

        LogInfo($"Scene saved successfully to: {config.ScenePath}");
    }

    #endregion

    #region Profiler Control

    private static void SetupProfiler()
    {
        LogInfo("Setting up profiler...");

        // Open Profiler Window
        var profilerWindow = EditorWindow.GetWindow<ProfilerWindow>();
        profilerWindow.Show();
        LogInfo("Profiler window opened");

        // Clear profiler data
        ProfilerDriver.ClearAllFrames();
        LogInfo("Cleared all profiler frames");

        // Enable profiling
        ProfilerDriver.enabled = true;
        LogInfo("Profiler enabled");
    }

    private static void StartPlayMode()
    {
        LogInfo("Starting play mode...");
        EditorApplication.isPlaying = true;
    }

    private static void StopPlayMode()
    {
        LogInfo("Stopping play mode...");
        CurrentState = TestState.Stopping;
        EditorApplication.ExitPlaymode();
    }

    #endregion

    #region Update Loop

    private static void OnEditorUpdate()
    {
        var state = CurrentState;

        if (state == TestState.Idle)
            return;

        if (state == TestState.Starting && EditorApplication.isPlaying)
        {
            OnPlayModeStarted();
        }
        else if (state == TestState.Running)
        {
            UpdateRunningState();
        }
        else if (state == TestState.Stopping && !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            OnPlayModeStopped();
        }
    }

    private static void OnPlayModeStarted()
    {
        CurrentState = TestState.Running;
        SessionState.SetInt(START_FRAME_KEY, Time.frameCount);
        LogInfo($"Play mode started - Initial frame: {Time.frameCount}, State: {CurrentState}");
    }

    private static void UpdateRunningState()
    {
        if (!EditorApplication.isPlaying)
            return;

        int startFrame = SessionState.GetInt(START_FRAME_KEY, 0);
        int framesToRun = SessionState.GetInt(CONFIG_FRAMES_TO_RUN_KEY, 400);
        int framesElapsed = ProfilerDriver.lastFrameIndex - ProfilerDriver.firstFrameIndex;

        if (framesElapsed >= framesToRun)
        {
            LogInfo($"Target frames reached - Elapsed: {framesElapsed}/{framesToRun}");
            StopPlayMode();
        }
    }

    private static void OnPlayModeStopped()
    {
        CurrentState = TestState.Analyzing;
        LogInfo($"Play mode stopped - State: {CurrentState}");

        // Disable profiler recording so that Rider's SnapshotCollectorDaemon can process
        // the captured data. The daemon's Update() skips processing while
        // ProfilerDriver.enabled && ProfilerDriver.profileEditor is true.
        ProfilerDriver.enabled = false;
        LogInfo("Profiler recording disabled after play mode stop");

        // Focus the profiler window so that SnapshotCollectorDaemon detects it
        // via ProfilerWindowEventsHandler.UpdateFocusedProfilerWindow()
        var profilerWindow = EditorWindow.GetWindow<ProfilerWindow>();
        if (profilerWindow != null)
        {
            profilerWindow.Focus();
            LogInfo("Profiler window focused after play mode stop");
        }

        string scriptName = SessionState.GetString(CONFIG_SCRIPT_NAME_KEY, "");
        LogInfo($"Retrieved target script from session: {scriptName}");

        EditorApplication.delayCall += () =>
        {
            AnalyzeProfilerData(scriptName + ".Update");
            CleanupState();
        };
    }

    #endregion

    #region Profiler Analysis - Main Flow

    private static void AnalyzeProfilerData(string scriptName)
    {
        LogInfo($"=== Starting Profiler Analysis for: {scriptName} ===");

        try
        {
            var profilerWindow = EditorWindow.GetWindow<ProfilerWindow>();

            if (profilerWindow == null)
            {
                LogError("Profiler window not found");
                return;
            }

            var selectedFrameIndex = profilerWindow.selectedFrameIndex;
            LogInfo($"Profiler window found - Selected frame: {selectedFrameIndex}");

            // Get the CPU module to apply filter
            var cpuModule = profilerWindow.GetFrameTimeViewSampleSelectionController("CPU Usage");
            if (cpuModule != null)
            {
                LogInfo($"CPU module obtained - Type: {cpuModule.GetType().Name}");

                // Apply search filter
                cpuModule.sampleNameSearchFilter = scriptName;
                LogInfo($"Applied sample name filter: '{scriptName}'");

                // Force repaint to ensure filter is applied
                profilerWindow.Repaint();
                LogInfo("Profiler window repaint requested");

                // Wait for hierarchy rebuild
                WaitAndContinueAnalysis(profilerWindow, scriptName, HIERARCHY_REBUILD_WAIT_MS);
            }
            else
            {
                LogWarning("Could not get CPU module - Continuing without filter");
                ContinueAnalysisAfterFilter(profilerWindow, scriptName);
            }
        }
        catch (Exception e)
        {
            LogError($"Error in AnalyzeProfilerData: {e.Message}\n{e.StackTrace}");
        }
    }

    private static async void WaitAndContinueAnalysis(ProfilerWindow profilerWindow, string scriptName, int delayMs)
    {
        try
        {
            LogInfo($"Waiting {delayMs}ms for profiler hierarchy to rebuild...");

            await System.Threading.Tasks.Task.Delay(delayMs);

            LogInfo("Wait completed, continuing analysis on main thread");

            // Continue on the main thread
            EditorApplication.delayCall += () =>
            {
                ContinueAnalysisAfterFilter(profilerWindow, scriptName);
            };
        }
        catch (Exception e)
        {
            LogError($"Error in WaitAndContinueAnalysis: {e.Message}\n{e.StackTrace}");
        }
    }

    private static void ContinueAnalysisAfterFilter(ProfilerWindow profilerWindow, string scriptName)
    {
        LogInfo("=== Continuing Analysis After Filter ===");

        try
        {
            // Setup TreeViewController and hook into double-click events
            if (SetupTreeViewControllerHook(profilerWindow, scriptName))
            {
                LogInfo("TreeViewController hook established successfully");

                // Try to find and automatically double-click the target script sample
                if (FindAndSelectTargetSample(scriptName))
                {
                    LogInfo($"✓ Successfully found and selected target: {scriptName}");
                }
                else
                {
                    LogWarning($"Could not automatically select '{scriptName}' - Manual navigation required");
                }
            }
            else
            {
                LogWarning("Failed to setup TreeViewController hook - Manual navigation required");
            }
        }
        catch (Exception e)
        {
            LogError($"Error in ContinueAnalysisAfterFilter: {e.Message}\n{e.StackTrace}");
        }
    }

    #endregion

    #region Profiler Analysis - TreeView Setup

    private static bool SetupTreeViewControllerHook(ProfilerWindow profilerWindow, string scriptName)
    {
        LogInfo("=== Setting up TreeViewController Hook ===");

        try
        {
            // Step 1: Get CPU Module
            var cpuModule = profilerWindow.GetFrameTimeViewSampleSelectionController("CPU Usage");
            if (cpuModule == null)
            {
                LogWarning("Failed to get CPU module");
                return false;
            }
            LogInfo($"✓ CPU Module obtained: {cpuModule.GetType().Name}");

            // Apply filter again to ensure it's set
            cpuModule.sampleNameSearchFilter = scriptName;

            // Step 2: Get FrameDataHierarchyView
            var frameDataHierarchyViewField = cpuModule.GetType().GetField("m_FrameDataHierarchyView",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (frameDataHierarchyViewField == null)
            {
                LogWarning("Failed to find m_FrameDataHierarchyView field");
                return false;
            }

            var frameDataHierarchyView = frameDataHierarchyViewField.GetValue(cpuModule);
            if (frameDataHierarchyView == null)
            {
                LogWarning("FrameDataHierarchyView is null");
                return false;
            }
            LogInfo($"✓ FrameDataHierarchyView obtained: {frameDataHierarchyView.GetType().Name}");

            // Step 3: Initialize hierarchy view
            var initIfNeededMethod = frameDataHierarchyView.GetType().GetMethod("InitIfNeeded",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (initIfNeededMethod != null)
            {
                initIfNeededMethod.Invoke(frameDataHierarchyView, null);
                LogInfo("✓ Called InitIfNeeded on FrameDataHierarchyView");
            }
            else
            {
                LogWarning("InitIfNeeded method not found");
            }

            // Step 4: Get ProfilerFrameDataTreeView
            var treeViewField = frameDataHierarchyView.GetType().GetField("m_TreeView",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (treeViewField == null)
            {
                LogWarning("Failed to find m_TreeView field in FrameDataHierarchyView");
                return false;
            }

            var profilerFrameDataTreeView = treeViewField.GetValue(frameDataHierarchyView);
            if (profilerFrameDataTreeView == null)
            {
                LogWarning("ProfilerFrameDataTreeView is null");
                return false;
            }
            LogInfo($"✓ ProfilerFrameDataTreeView obtained: {profilerFrameDataTreeView.GetType().Name}");

            // Step 5: Get TreeViewController from base type
            var treeViewControllerField = profilerFrameDataTreeView.GetType().BaseType?.GetField("m_TreeView",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (treeViewControllerField == null)
            {
                LogWarning("Failed to find m_TreeView field in ProfilerFrameDataTreeView base type");
                return false;
            }

            profilerTreeViewController = treeViewControllerField.GetValue(profilerFrameDataTreeView);
            if (profilerTreeViewController == null)
            {
                LogWarning("TreeViewController is null");
                return false;
            }
            LogInfo($"✓ TreeViewController obtained: {profilerTreeViewController.GetType().Name}");

            // Step 6: Hook into double-click callback
            var itemDoubleClickedProperty = profilerTreeViewController.GetType().GetProperty("itemDoubleClickedCallback",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (itemDoubleClickedProperty == null)
            {
                LogWarning("Failed to find itemDoubleClickedCallback property");
                return false;
            }
            LogInfo($"✓ itemDoubleClickedCallback property found: {itemDoubleClickedProperty.PropertyType}");

            // Subscribe to the callback
            var existingCallback = itemDoubleClickedProperty.GetValue(profilerTreeViewController) as Action<int>;
            Action<int> newCallback = (itemId) => OnTreeViewItemDoubleClicked(itemId, scriptName);

            // Combine existing callback with new one
            var combinedCallback = existingCallback + newCallback;
            itemDoubleClickedProperty.SetValue(profilerTreeViewController, combinedCallback);

            // Store for cleanup
            itemDoubleClickedDelegate = newCallback;

            LogInfo("✓ Successfully hooked into itemDoubleClickedCallback");
            return true;
        }
        catch (Exception e)
        {
            LogError($"Failed to setup TreeViewController hook: {e.Message}\n{e.StackTrace}");
            return false;
        }
    }

    #endregion

    #region Profiler Analysis - Sample Navigation

    private static bool FindAndSelectTargetSample(string scriptName)
    {
        LogInfo($"=== Searching for target sample: {scriptName} ===");

        try
        {
            if (profilerTreeViewController == null)
            {
                LogWarning("TreeViewController is not initialized");
                return false;
            }

            var treeViewType = profilerTreeViewController.GetType();

            // Step 1: Get data source
            var dataProperty = treeViewType.GetProperty("data", BindingFlags.Public | BindingFlags.Instance);
            if (dataProperty == null)
            {
                LogWarning("Failed to find 'data' property on TreeViewController");
                return false;
            }

            var dataSource = dataProperty.GetValue(profilerTreeViewController);
            if (dataSource == null)
            {
                LogWarning("TreeView data source is null");
                return false;
            }
            LogInfo($"✓ Data source obtained: {dataSource.GetType().Name}");

            // Step 2: Get root item
            var rootProperty = dataSource.GetType().GetProperty("root", BindingFlags.Public | BindingFlags.Instance);
            if (rootProperty == null)
            {
                LogWarning("Failed to find 'root' property on data source");
                return false;
            }

            var rootItem = rootProperty.GetValue(dataSource);
            if (rootItem == null)
            {
                LogWarning("Root item is null");
                return false;
            }
            LogInfo($"✓ Root item obtained: {rootItem.GetType().Name}");

            // Step 3: Search for target script in the tree
            LogInfo($"Searching tree for item containing: '{scriptName}'");
            var targetItemId = FindItemIdByName(rootItem, scriptName);
            if (targetItemId == -1)
            {
                LogWarning($"No item found with name containing '{scriptName}'");
                return false;
            }

            LogInfo($"✓ Found target item - ID: {targetItemId}");

            // Step 4: Programmatically trigger double-click
            return EmulateDoubleClick(targetItemId);
        }
        catch (Exception e)
        {
            LogError($"Error finding target sample: {e.Message}\n{e.StackTrace}");
            return false;
        }
    }

    private static int FindItemIdByName(object item, string searchName)
    {
        try
        {
            var itemType = item.GetType();

            // Check display name
            var displayNameProperty = itemType.GetProperty("displayName", BindingFlags.Public | BindingFlags.Instance);
            if (displayNameProperty != null)
            {
                var displayName = displayNameProperty.GetValue(item) as string;
                if (!string.IsNullOrEmpty(displayName) && displayName.Contains(searchName))
                {
                    var idProperty = itemType.GetProperty("id", BindingFlags.Public | BindingFlags.Instance);
                    if (idProperty != null)
                    {
                        int id = (int)idProperty.GetValue(item);
                        LogInfo($"  Found matching item: '{displayName}' (ID: {id})");
                        return id;
                    }
                }
            }

            // Search children recursively
            var childrenProperty = itemType.GetProperty("children", BindingFlags.Public | BindingFlags.Instance);
            if (childrenProperty != null)
            {
                var children = childrenProperty.GetValue(item) as System.Collections.IEnumerable;
                if (children != null)
                {
                    foreach (var child in children)
                    {
                        var result = FindItemIdByName(child, searchName);
                        if (result != -1)
                            return result;
                    }
                }
            }

            return -1;
        }
        catch (Exception e)
        {
            LogWarning($"Error searching item: {e.Message}");
            return -1;
        }
    }

    private static bool EmulateDoubleClick(int itemId)
    {
        LogInfo($"=== Emulating Double-Click on Item ID: {itemId} ===");

        try
        {
            if (profilerTreeViewController == null)
            {
                LogWarning("TreeViewController is not initialized");
                return false;
            }

            var treeViewType = profilerTreeViewController.GetType();

            // Step 1: Set selection
            try
            {
                var setSelectionMethod = treeViewType.GetMethod("SetSelection",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    new Type[] { typeof(int[]), typeof(bool) },
                    null);

                if (setSelectionMethod != null)
                {
                    setSelectionMethod.Invoke(profilerTreeViewController, new object[] { new int[] { itemId }, true });
                    LogInfo($"✓ Set selection to item ID: {itemId}");
                }
                else
                {
                    LogWarning("SetSelection method not found");
                }
            }
            catch (Exception ex)
            {
                LogWarning($"Could not set selection: {ex.Message}");
            }

            // Step 2: Invoke double-click callback
            var itemDoubleClickedProperty = treeViewType.GetProperty("itemDoubleClickedCallback",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (itemDoubleClickedProperty != null)
            {
                LogInfo($"✓ Found itemDoubleClickedCallback property: {itemDoubleClickedProperty.PropertyType}");

                var callback = itemDoubleClickedProperty.GetValue(profilerTreeViewController);
                if (callback != null)
                {
                    LogInfo($"✓ Callback retrieved: {callback.GetType()}");

                    // Try direct cast to Action<int>
                    if (callback is Action<int> action)
                    {
                        action(itemId);
                        LogInfo($"✓ Invoked itemDoubleClickedCallback via Action<int> for item ID: {itemId}");
                        return true;
                    }
                    else
                    {
                        // Fallback: Dynamic invoke
                        try
                        {
                            var invokeMethod = callback.GetType().GetMethod("Invoke");
                            if (invokeMethod != null)
                            {
                                invokeMethod.Invoke(callback, new object[] { itemId });
                                LogInfo($"✓ Dynamically invoked itemDoubleClickedCallback for item ID: {itemId}");
                                return true;
                            }
                        }
                        catch (Exception ex)
                        {
                            LogWarning($"Dynamic invoke failed: {ex.Message}");
                        }
                    }
                }
                else
                {
                    LogWarning("itemDoubleClickedCallback is null - No callback registered");
                }
            }
            else
            {
                LogWarning("Could not find itemDoubleClickedCallback property");
            }

            // Step 3: Fallback methods
            var handleDoubleClickMethod = treeViewType.GetMethod("HandleItemDoubleClick",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (handleDoubleClickMethod == null)
            {
                handleDoubleClickMethod = treeViewType.GetMethod("OnItemDoubleClicked",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            }

            if (handleDoubleClickMethod != null)
            {
                handleDoubleClickMethod.Invoke(profilerTreeViewController, new object[] { itemId });
                LogInfo($"✓ Invoked fallback double-click handler for item ID: {itemId}");
                return true;
            }

            LogWarning("No method found to trigger double-click");
            return false;
        }
        catch (Exception e)
        {
            LogError($"Error emulating double-click: {e.Message}\n{e.StackTrace}");
            return false;
        }
    }

    #endregion

    #region Profiler Analysis - Event Handlers

    private static void OnTreeViewItemDoubleClicked(int itemId, string scriptName)
    {
        LogInfo($"=== TreeView Item Double-Clicked: ID={itemId} ===");

        try
        {
            // Get selected property path from ProfilerDriver
            var selectedPropertyPath = ProfilerDriver.selectedPropertyPath;

            if (!string.IsNullOrEmpty(selectedPropertyPath))
            {
                LogInfo($"Selected property path: {selectedPropertyPath}");

                if (selectedPropertyPath.Contains(scriptName))
                {
                    LogInfo($"✓ Confirmed selection matches target script: {scriptName}");
                    // Additional actions could be triggered here (e.g., open script file)
                }
                else
                {
                    LogWarning($"Selected path does not contain target script: {scriptName}");
                }
            }
            else
            {
                LogWarning("Selected property path is empty");
            }
        }
        catch (Exception e)
        {
            LogError($"Error handling TreeView double-click: {e.Message}\n{e.StackTrace}");
        }
    }

    private static void UnsubscribeTreeViewControllerHook()
    {
        if (profilerTreeViewController != null && itemDoubleClickedDelegate != null)
        {
            LogInfo("Unsubscribing from TreeViewController hook...");

            try
            {
                var itemDoubleClickedProperty = profilerTreeViewController.GetType().GetProperty("itemDoubleClickedCallback",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (itemDoubleClickedProperty != null)
                {
                    var existingCallback = itemDoubleClickedProperty.GetValue(profilerTreeViewController) as Action<int>;
                    if (existingCallback != null)
                    {
                        var updatedCallback = existingCallback - (itemDoubleClickedDelegate as Action<int>);
                        itemDoubleClickedProperty.SetValue(profilerTreeViewController, updatedCallback);
                        LogInfo("✓ Unsubscribed from TreeViewController.itemDoubleClickedCallback");
                    }
                }
            }
            catch (Exception e)
            {
                LogWarning($"Error unsubscribing from TreeViewController: {e.Message}");
            }
            finally
            {
                profilerTreeViewController = null;
                itemDoubleClickedEvent = null;
                itemDoubleClickedDelegate = null;
            }
        }
    }

    #endregion

    #region Cleanup

    private static void CleanupState()
    {
        LogInfo("=== Cleaning up ProfilerAutomation state ===");

        UnsubscribeTreeViewControllerHook();
        CurrentState = TestState.Idle;
        SessionState.SetInt(START_FRAME_KEY, 0);
        SessionState.SetString(CONFIG_SCRIPT_NAME_KEY, "");
        SessionState.SetInt(CONFIG_FRAMES_TO_RUN_KEY, 0);

        LogInfo("✓ Profiler test completed and state cleaned up");
    }

    #endregion

    #region Logging Helpers

    private static void LogInfo(string message)
    {
        Debug.Log($"{LOG_PREFIX} {message}");
    }

    private static void LogWarning(string message)
    {
        Debug.LogWarning($"{LOG_PREFIX} {message}");
    }

    private static void LogError(string message)
    {
        Debug.LogError($"{LOG_PREFIX} {message}");
    }

    #endregion
}
