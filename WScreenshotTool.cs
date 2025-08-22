using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace WScreenshotTool
{
    #region Data Structures
    /// <summary>
    /// Main configuration settings for screenshot capture
    /// </summary>
    [System.Serializable]
    public class ScreenshotSettings
    {
        [Header("Basic Settings")]
        public string screenshotName = "Screenshot";
        public string savePath = "Assets/Screenshots";
        public ImageFormat format = ImageFormat.PNG;
        public int jpegQuality = 90;
        
        [Header("Resolution Settings")]
        public float resolutionMultiplier = 1f;
        public bool useCustomResolution = false;
        public int customWidth = 1920;
        public int customHeight = 1080;
        
        [Header("Capture Options")]
        public bool transparentBackground = false;
        public bool captureUI = true;
        public bool autoTimestamp = true;
        public bool showConsoleMessage = true;
        public bool autoRefresh = true;
        public bool openAfterCapture = false;
        
        [Header("Batch Settings")]
        public bool batchMode = false;
        public int batchCount = 10;
        public float batchInterval = 1f;
        public bool batchAutoIncrement = true;
        public bool batchCreateSubfolder = true;
        
        /// <summary>
        /// Create a deep copy of the settings
        /// </summary>
        /// <returns>Cloned settings instance</returns>
        public ScreenshotSettings Clone()
        {
            return (ScreenshotSettings)this.MemberwiseClone();
        }
    }

    /// <summary>
    /// Camera setup configuration for multi-camera capture
    /// </summary>
    [System.Serializable]
    public class CameraSetup
    {
        [Header("Camera Reference")]
        public Camera camera;
        public bool isSelected = false;
        public string customName = "";
        
        [Header("Override Settings")]
        public bool overrideSettings = false;
        public ScreenshotSettings overrideValues;
        
        /// <summary>
        /// Get the display name for this camera setup
        /// </summary>
        public string DisplayName => !string.IsNullOrEmpty(customName) ? customName : 
                                    (camera != null ? camera.name : "Missing Camera");
    }

    /// <summary>
    /// Predefined resolution preset
    /// </summary>
    [System.Serializable]
    public struct ResolutionPreset
    {
        public string name;
        public int width;
        public int height;
        
        public ResolutionPreset(string name, int width, int height)
        {
            this.name = name;
            this.width = width;
            this.height = height;
        }
    }

    /// <summary>
    /// Social media platform format configuration
    /// </summary>
    [System.Serializable]
    public class SocialMediaFormat
    {
        [Header("Platform Info")]
        public string platform = "Custom";
        public string name = "Custom Format";
        public string description = "";
        
        [Header("Format Settings")]
        public int width = 1920;
        public int height = 1080;
        public int quality = 90;
        public ImageFormat format = ImageFormat.JPG;
        
        /// <summary>
        /// Get the aspect ratio as a formatted string
        /// </summary>
        public string AspectRatio
        {
            get
            {
                int gcd = CalculateGCD(width, height);
                return $"{width / gcd}:{height / gcd}";
            }
        }
        
        /// <summary>
        /// Calculate the greatest common divisor
        /// </summary>
        private int CalculateGCD(int a, int b)
        {
            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }
    }

    /// <summary>
    /// Screenshot preset for quick settings switching
    /// </summary>
    [System.Serializable]
    public class ScreenshotPreset
    {
        public string name;
        public string description;
        public ScreenshotSettings settings;
        public DateTime createdDate;
        
        public ScreenshotPreset(string name, ScreenshotSettings settings)
        {
            this.name = name;
            this.settings = settings?.Clone();
            this.createdDate = DateTime.Now;
        }
    }

    /// <summary>
    /// Capture history entry
    /// </summary>
    [System.Serializable]
    public class ScreenshotHistoryEntry
    {
        public string filePath;
        public DateTime timestamp;
        public ScreenshotSettings settings;
        public string cameraName;
        public long fileSize;
        
        /// <summary>
        /// Check if the file still exists
        /// </summary>
        public bool FileExists => File.Exists(filePath);
    }

    /// <summary>
    /// Capture task for queue processing
    /// </summary>
    public class CaptureTask
    {
        public CaptureType type;
        public ScreenshotSettings settings;
        public CameraSetup cameraSetup;
        public string customPath;
        
        public CaptureTask(CaptureType type, ScreenshotSettings settings)
        {
            this.type = type;
            this.settings = settings;
        }
    }

    /// <summary>
    /// Types of capture operations
    /// </summary>
    public enum CaptureType
    {
        Screen,
        GameView,
        SceneView,
        Camera,
        Custom
    }

    /// <summary>
    /// Supported image formats
    /// </summary>
    public enum ImageFormat
    {
        PNG,
        JPG
    }
    #endregion
        
    /// <summary>
    /// Advanced Unity Editor screenshot tool with comprehensive capture options, 
    /// batch processing, improved alpha channel support, and additional resolution presets.
    /// 
    /// Features:
    /// - Multi-camera capture with custom settings
    /// - Batch processing with intelligent scheduling
    /// - Fixed transparency support for all capture modes
    /// - Automated social media formatting
    /// - Performance profiling integration
    /// - Asset management workflows
    /// - Thread-safe capture operations
    /// </summary>
    public class WScreenshotTool : EditorWindow
    {
        #region Constants & Version Info
        /// <summary>Current version of the screenshot tool</summary>
        private const string ToolVersion = "2.0.0";
        
        /// <summary>Developer name for branding</summary>
        private const string DeveloperName = "Wiskered Studio";
        
        /// <summary>Window title displayed in Unity Editor</summary>
        private const string WindowTitle = "W Screenshot Tool";
        
        /// <summary>URL to documentation</summary>
        private const string DocumentationURL = "https://wiskered.gitbook.io/wscreenshottool-documentation-1/";
        
        /// <summary>Official website URL</summary>
        private const string WebsiteURL = "https://wiskered.com";

        /// <summary>Publisher's Unity Asset Store URL</summary>
        private const string ProductsURL = "https://assetstore.unity.com/publishers/46701";
        
        /// <summary>Support email address</summary>
        private const string SupportEmail = "wiskered@gmail.com";
        
        // Feature flags for future development
        private const bool EnableAsyncCapture = true;
        private const bool EnableGPUAcceleration = true;
        
        // EditorPrefs keys for persistent settings
        private const string PrefsPrefix = "WScreenshotTool_";
        private const string ScreenshotNameKey = PrefsPrefix + "ScreenshotName";
        private const string SavePathKey = PrefsPrefix + "SavePath";
        private const string ShowConsoleMessageKey = PrefsPrefix + "ShowConsoleMessage";
        private const string AutoRefreshKey = PrefsPrefix + "AutoRefresh";
        private const string ResolutionMultiplierKey = PrefsPrefix + "ResolutionMultiplier";
        private const string CaptureUIKey = PrefsPrefix + "CaptureUI";
        private const string AutoTimestampKey = PrefsPrefix + "AutoTimestamp";
        private const string CaptureFormatKey = PrefsPrefix + "CaptureFormat";
        private const string BatchModeKey = PrefsPrefix + "BatchMode";
        private const string BatchIntervalKey = PrefsPrefix + "BatchInterval";
        private const string BatchCountKey = PrefsPrefix + "BatchCount";
        private const string ResolutionPresetKey = PrefsPrefix + "ResolutionPreset";
        private const string TransparentBackgroundKey = PrefsPrefix + "TransparentBackground";
        private const string UseCustomResolutionKey = PrefsPrefix + "UseCustomResolution";
        private const string CustomWidthKey = PrefsPrefix + "CustomWidth";
        private const string CustomHeightKey = PrefsPrefix + "CustomHeight";
        private const string JpegQualityKey = PrefsPrefix + "JpegQuality";
        private const string OpenAfterCaptureKey = PrefsPrefix + "OpenAfterCapture";
        #endregion

        #region Private Fields
        /// <summary>Main screenshot settings configuration</summary>
        [SerializeField] private ScreenshotSettings settings = new ScreenshotSettings();
        
        /// <summary>List of camera setups for multi-camera capture</summary>
        [SerializeField] private List<CameraSetup> cameraSetups = new List<CameraSetup>();
        
        /// <summary>Saved presets for quick access</summary>
        [SerializeField] private List<ScreenshotPreset> presets = new List<ScreenshotPreset>();
        
        /// <summary>Social media format configurations</summary>
        [SerializeField] private List<SocialMediaFormat> socialFormats = new List<SocialMediaFormat>();
        
        // GUI state management
        private Vector2 scrollPosition;
        private int selectedTab = 0;
        private string[] tabNames = { "Basic", "Advanced", "Batch", "Cameras", "Social", "Presets", "History" };
        
        // Core systems
        private BatchProcessor batchProcessor;
        private ScreenshotHistory history;
        
        // Capture state management
        private bool isCapturing = false;
        private float captureProgress = 0f;
        private string currentOperation = "";
        private Queue<CaptureTask> captureQueue = new Queue<CaptureTask>();
        
        // GUI Styles - initialized once for performance
        private GUIStyle headerStyle;
        private GUIStyle subHeaderStyle;
        private GUIStyle errorStyle;
        private GUIStyle successStyle;
        
        /// <summary>Predefined resolution presets for common use cases</summary>
        private static readonly ResolutionPreset[] ResolutionPresets = new[]
        {
            new ResolutionPreset("Custom", 1920, 1080),
            new ResolutionPreset("4K Ultra (3840x2160)", 3840, 2160),
            new ResolutionPreset("1080p Full HD (1920x1080)", 1920, 1080),
            new ResolutionPreset("720p HD (1280x720)", 1280, 720),
            new ResolutionPreset("2K QHD (2560x1440)", 2560, 1440),
            new ResolutionPreset("Social Media (1200x630)", 1200, 630),
            new ResolutionPreset("Instagram Square (1080x1080)", 1080, 1080),
            new ResolutionPreset("Twitter Header (1500x500)", 1500, 500),
            new ResolutionPreset("YouTube Thumbnail (1280x720)", 1280, 720),
            new ResolutionPreset("Steam Store (1920x1080)", 1920, 1080)
        };
        #endregion

        #region Public API
        /// <summary>
        /// Captures a screenshot using default settings via static API call
        /// </summary>
        /// <remarks>This method can be called from other scripts without opening the tool window</remarks>
        public static void CaptureScreenshotAPI()
        {
            var window = GetWindow<WScreenshotTool>(false);
            window.CaptureScreenshot();
        }

        /// <summary>
        /// Captures a screenshot with custom settings via static API call
        /// </summary>
        /// <param name="customSettings">Custom screenshot settings to use for this capture</param>
        public static void CaptureScreenshotAPI(ScreenshotSettings customSettings)
        {
            var window = GetWindow<WScreenshotTool>(false);
            var originalSettings = window.settings;
            window.settings = customSettings;
            window.CaptureScreenshot();
            window.settings = originalSettings;
        }

        /// <summary>
        /// Starts batch capture process via static API call
        /// </summary>
        /// <param name="count">Number of screenshots to capture</param>
        /// <param name="interval">Time interval between captures in seconds</param>
        public static void StartBatchCaptureAPI(int count, float interval)
        {
            var window = GetWindow<WScreenshotTool>(false);
            window.settings.batchCount = count;
            window.settings.batchInterval = interval;
            window.StartBatchCapture();
        }

        /// <summary>
        /// Captures a transparent background screenshot for UI elements
        /// </summary>
        /// <param name="camera">Camera to capture from</param>
        /// <param name="filePath">Output file path</param>
        /// <param name="width">Output width</param>
        /// <param name="height">Output height</param>
        public static void CaptureTransparentAPI(Camera camera, string filePath, int width = 1920, int height = 1080)
        {
            var settings = new ScreenshotSettings
            {
                format = ImageFormat.PNG,
                transparentBackground = true,
                useCustomResolution = true,
                customWidth = width,
                customHeight = height
            };
            
            var window = GetWindow<WScreenshotTool>(false);
            window.PerformCameraCapture(camera, filePath, settings);
        }
        #endregion

        #region Unity Editor Window Lifecycle
        /// <summary>
        /// Opens the screenshot tool window with keyboard shortcut support
        /// </summary>
        [MenuItem("Tools/W Screenshot Tool %#s", false, 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<WScreenshotTool>(WindowTitle);
            window.minSize = new Vector2(480, 650);
            window.Show();
        }

        /// <summary>
        /// Alternative menu path for opening the screenshot tool
        /// </summary>
        [MenuItem("Window/Photography/W Screenshot Tool", false, 100)]
        public static void ShowWindowAlternative()
        {
            ShowWindow();
        }

        /// <summary>
        /// Initialize components and load settings when window is enabled
        /// </summary>
        private void OnEnable()
        {
            LoadSettings();
            InitializeComponents();
            InitializeSocialFormats();
            RefreshCameraList();
            LoadPresets();
            
            // Subscribe to Unity events
            EditorApplication.hierarchyChanged += RefreshCameraList;
            EditorApplication.update += OnEditorUpdate;
            
            // Initialize capture queue processing
            EditorApplication.update += ProcessCaptureQueue;
        }

        /// <summary>
        /// Clean up and save settings when window is disabled
        /// </summary>
        private void OnDisable()
        {
            SaveSettings();
            
            // Unsubscribe from Unity events
            EditorApplication.hierarchyChanged -= RefreshCameraList;
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update -= ProcessCaptureQueue;
            
            // Stop any running batch process
            batchProcessor?.Stop();
        }

        /// <summary>
        /// Main GUI rendering method
        /// </summary>
        private void OnGUI()
        {
            InitializeStyles();
            
            DrawHeader();
            DrawProgressIndicators();
            DrawTabNavigation();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // Render selected tab content
            switch (selectedTab)
            {
                case 0: DrawBasicTab(); break;
                case 1: DrawAdvancedTab(); break;
                case 2: DrawBatchTab(); break;
                case 3: DrawCamerasTab(); break;
                case 4: DrawSocialTab(); break;
                case 5: DrawPresetsTab(); break;
                case 6: DrawHistoryTab(); break;
            }
            
            EditorGUILayout.EndScrollView();
            
            DrawFooter();
            
            // Force repaint during active operations
            if (isCapturing || (batchProcessor?.IsRunning ?? false))
            {
                Repaint();
            }
        }
        
        /// <summary>
        /// Handle editor update events for batch processing and queue management
        /// </summary>
        private void OnEditorUpdate()
        {
            // This method is called from EditorApplication.update
            // Keep it lightweight to avoid performance issues
        }
        #endregion

        #region Initialization Methods
        /// <summary>
        /// Initialize core system components
        /// </summary>
        private void InitializeComponents()
        {
            if (history == null)
                history = new ScreenshotHistory();
                
            if (batchProcessor == null)
                batchProcessor = new BatchProcessor(settings);
        }

        /// <summary>
        /// Set up default social media format configurations
        /// </summary>
        private void InitializeSocialFormats()
        {
            if (socialFormats == null)
                socialFormats = new List<SocialMediaFormat>();
                
            if (socialFormats.Count == 0)
            {
                socialFormats.AddRange(new[]
                {
                    new SocialMediaFormat { 
                        platform = "Twitter", 
                        name = "Twitter Post", 
                        width = 1200, 
                        height = 675, 
                        quality = 90 
                    },
                    new SocialMediaFormat { 
                        platform = "Instagram", 
                        name = "Instagram Square", 
                        width = 1080, 
                        height = 1080, 
                        quality = 90 
                    },
                    new SocialMediaFormat { 
                        platform = "Instagram", 
                        name = "Instagram Story", 
                        width = 1080, 
                        height = 1920, 
                        quality = 90 
                    },
                    new SocialMediaFormat { 
                        platform = "Facebook", 
                        name = "Facebook Cover", 
                        width = 1200, 
                        height = 630, 
                        quality = 85 
                    },
                    new SocialMediaFormat { 
                        platform = "LinkedIn", 
                        name = "LinkedIn Cover", 
                        width = 1584, 
                        height = 396, 
                        quality = 85 
                    },
                    new SocialMediaFormat { 
                        platform = "YouTube", 
                        name = "YouTube Thumbnail", 
                        width = 1280, 
                        height = 720, 
                        quality = 90 
                    }
                });
            }
        }

        /// <summary>
        /// Initialize GUI styles for consistent appearance
        /// </summary>
        private void InitializeStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel) 
                { 
                    fontSize = 16, 
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
                };
                
                subHeaderStyle = new GUIStyle(EditorStyles.boldLabel) 
                { 
                    fontSize = 12,
                    normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.8f, 0.8f, 0.8f) : new Color(0.3f, 0.3f, 0.3f) }
                };
                
                errorStyle = new GUIStyle(EditorStyles.label) 
                { 
                    normal = { textColor = Color.red },
                    fontStyle = FontStyle.Bold
                };
                
                successStyle = new GUIStyle(EditorStyles.label) 
                { 
                    normal = { textColor = Color.green },
                    fontStyle = FontStyle.Bold
                };
            }
        }

        /// <summary>
        /// Refresh the list of available cameras in the scene
        /// </summary>
        private void RefreshCameraList()
        {
            var allCameras = FindObjectsOfType<Camera>();
            var existingCameras = cameraSetups.Where(s => s.camera != null).Select(s => s.camera).ToHashSet();
            
            // Add new cameras
            foreach (var camera in allCameras)
            {
                if (!existingCameras.Contains(camera))
                {
                    cameraSetups.Add(new CameraSetup 
                    { 
                        camera = camera,
                        customName = camera.name,
                        isSelected = false 
                    });
                }
            }
            
            // Remove cameras that no longer exist
            cameraSetups.RemoveAll(s => s.camera == null);
        }

        /// <summary>
        /// Load settings from EditorPrefs
        /// </summary>
        private void LoadSettings()
        {
            if (settings == null)
                settings = new ScreenshotSettings();
                
            settings.screenshotName = EditorPrefs.GetString(ScreenshotNameKey, "Screenshot");
            settings.savePath = EditorPrefs.GetString(SavePathKey, "Assets/Screenshots");
            settings.showConsoleMessage = EditorPrefs.GetBool(ShowConsoleMessageKey, true);
            settings.autoRefresh = EditorPrefs.GetBool(AutoRefreshKey, true);
            settings.resolutionMultiplier = EditorPrefs.GetFloat(ResolutionMultiplierKey, 1f);
            settings.captureUI = EditorPrefs.GetBool(CaptureUIKey, true);
            settings.autoTimestamp = EditorPrefs.GetBool(AutoTimestampKey, true);
            settings.format = (ImageFormat)EditorPrefs.GetInt(CaptureFormatKey, 0);
            settings.batchMode = EditorPrefs.GetBool(BatchModeKey, false);
            settings.batchInterval = EditorPrefs.GetFloat(BatchIntervalKey, 1f);
            settings.batchCount = EditorPrefs.GetInt(BatchCountKey, 10);
            settings.transparentBackground = EditorPrefs.GetBool(TransparentBackgroundKey, false);
            settings.useCustomResolution = EditorPrefs.GetBool(UseCustomResolutionKey, false);
            settings.customWidth = EditorPrefs.GetInt(CustomWidthKey, 1920);
            settings.customHeight = EditorPrefs.GetInt(CustomHeightKey, 1080);
            settings.jpegQuality = EditorPrefs.GetInt(JpegQualityKey, 90);
            settings.openAfterCapture = EditorPrefs.GetBool(OpenAfterCaptureKey, false);
        }

        /// <summary>
        /// Save settings to EditorPrefs
        /// </summary>
        private void SaveSettings()
        {
            if (settings == null) return;
                
            EditorPrefs.SetString(ScreenshotNameKey, settings.screenshotName);
            EditorPrefs.SetString(SavePathKey, settings.savePath);
            EditorPrefs.SetBool(ShowConsoleMessageKey, settings.showConsoleMessage);
            EditorPrefs.SetBool(AutoRefreshKey, settings.autoRefresh);
            EditorPrefs.SetFloat(ResolutionMultiplierKey, settings.resolutionMultiplier);
            EditorPrefs.SetBool(CaptureUIKey, settings.captureUI);
            EditorPrefs.SetBool(AutoTimestampKey, settings.autoTimestamp);
            EditorPrefs.SetInt(CaptureFormatKey, (int)settings.format);
            EditorPrefs.SetBool(BatchModeKey, settings.batchMode);
            EditorPrefs.SetFloat(BatchIntervalKey, settings.batchInterval);
            EditorPrefs.SetInt(BatchCountKey, settings.batchCount);
            EditorPrefs.SetBool(TransparentBackgroundKey, settings.transparentBackground);
            EditorPrefs.SetBool(UseCustomResolutionKey, settings.useCustomResolution);
            EditorPrefs.SetInt(CustomWidthKey, settings.customWidth);
            EditorPrefs.SetInt(CustomHeightKey, settings.customHeight);
            EditorPrefs.SetInt(JpegQualityKey, settings.jpegQuality);
            EditorPrefs.SetBool(OpenAfterCaptureKey, settings.openAfterCapture);
        }

        /// <summary>
        /// Load presets from EditorPrefs or create defaults
        /// </summary>
        private void LoadPresets()
        {
            if (presets == null)
                presets = new List<ScreenshotPreset>();
                
            // Load from EditorPrefs would go here
            // For now, create some default presets if none exist
            if (presets.Count == 0)
            {
                LoadDefaultPresets();
            }
        }

        /// <summary>
        /// Create default presets for common use cases
        /// </summary>
        private void LoadDefaultPresets()
        {
            var defaultPresets = new[]
            {
                new ScreenshotPreset("High Quality PNG", new ScreenshotSettings 
                { 
                    format = ImageFormat.PNG, 
                    resolutionMultiplier = 2f,
                    transparentBackground = false 
                }),
                new ScreenshotPreset("Transparent UI", new ScreenshotSettings 
                { 
                    format = ImageFormat.PNG, 
                    transparentBackground = true,
                    useCustomResolution = true,
                    customWidth = 1920,
                    customHeight = 1080
                }),
                new ScreenshotPreset("Social Media", new ScreenshotSettings 
                { 
                    format = ImageFormat.JPG, 
                    jpegQuality = 90,
                    useCustomResolution = true,
                    customWidth = 1200,
                    customHeight = 630
                })
            };
            
            presets.AddRange(defaultPresets);
        }

        /// <summary>
        /// Save presets to EditorPrefs
        /// </summary>
        private void SavePresets()
        {
            // Implementation for saving presets to EditorPrefs
            // This would serialize the presets list to JSON and store it
        }
        #endregion

        #region GUI Drawing Methods
        /// <summary>
        /// Draw the tool header with title and version info
        /// </summary>
        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label(WindowTitle, headerStyle);
            EditorGUILayout.LabelField($"Version {ToolVersion} | {DeveloperName}", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
        }

        /// <summary>
        /// Draw progress bars for active capture operations
        /// </summary>
        private void DrawProgressIndicators()
        {
            if (isCapturing || (batchProcessor?.IsRunning ?? false))
            {
                EditorGUILayout.BeginVertical("box");
                
                if (isCapturing)
                {
                    EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), captureProgress, currentOperation);
                }
                
                if (batchProcessor?.IsRunning ?? false)
                {
                    var progress = batchProcessor.Progress;
                    var text = $"Batch: {batchProcessor.CompletedCount}/{batchProcessor.TotalCount}";
                    EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), progress, text);
                    
                    // Additional batch info
                    GUILayout.Space(5);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Elapsed: {batchProcessor.ElapsedTime:mm\\:ss}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"Remaining: {batchProcessor.EstimatedTimeRemaining:mm\\:ss}", EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndVertical();
                GUILayout.Space(5);
            }
        }

        /// <summary>
        /// Draw tab navigation toolbar
        /// </summary>
        private void DrawTabNavigation()
        {
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
            GUILayout.Space(10);
        }

        /// <summary>
        /// Draw the basic settings tab
        /// </summary>
        private void DrawBasicTab()
        {
            EditorGUILayout.LabelField("Basic Settings", subHeaderStyle);
            EditorGUILayout.Space();
            
            // Core settings
            settings.screenshotName = EditorGUILayout.TextField("Screenshot Name", settings.screenshotName);
            
            // Save path with browse button
            EditorGUILayout.BeginHorizontal();
            settings.savePath = EditorGUILayout.TextField("Save Path", settings.savePath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Save Folder", settings.savePath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    settings.savePath = path;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // Format and quality settings
            settings.format = (ImageFormat)EditorGUILayout.EnumPopup("Image Format", settings.format);
            
            if (settings.format == ImageFormat.JPG)
            {
                EditorGUI.indentLevel++;
                settings.jpegQuality = EditorGUILayout.IntSlider("JPEG Quality", settings.jpegQuality, 1, 100);
                EditorGUI.indentLevel--;
            }
            
            settings.resolutionMultiplier = EditorGUILayout.Slider("Resolution Scale", settings.resolutionMultiplier, 0.25f, 8f);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Options", subHeaderStyle);
            
            // Toggle options
            settings.autoTimestamp = EditorGUILayout.Toggle("Auto Timestamp", settings.autoTimestamp);
            settings.showConsoleMessage = EditorGUILayout.Toggle("Console Log", settings.showConsoleMessage);
            settings.autoRefresh = EditorGUILayout.Toggle("Auto Refresh Assets", settings.autoRefresh);
            settings.openAfterCapture = EditorGUILayout.Toggle("Open After Capture", settings.openAfterCapture);
            
            GUILayout.Space(20);
            
            // Main capture button
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Capture Screenshot", GUILayout.Height(35)))
            {
                CaptureScreenshot();
            }
            GUI.backgroundColor = Color.white;
            
            GUILayout.Space(10);
            
            // Quick action buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Quick Capture", GUILayout.Height(25)))
            {
                QuickCapture();
            }
            if (GUILayout.Button("Game View", GUILayout.Height(25)))
            {
                CaptureGameView();
            }
            if (GUILayout.Button("Scene View", GUILayout.Height(25)))
            {
                CaptureSceneView();
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw the advanced settings tab
        /// </summary>
        private void DrawAdvancedTab()
        {
            EditorGUILayout.LabelField("Advanced Options", subHeaderStyle);
            EditorGUILayout.Space();
            
            // Advanced capture options
            settings.captureUI = EditorGUILayout.Toggle("Capture UI Elements", settings.captureUI);
            
            // Transparency settings
            EditorGUILayout.BeginHorizontal();
            settings.transparentBackground = EditorGUILayout.Toggle("Transparent Background", settings.transparentBackground);
            
            if (settings.transparentBackground)
            {
                if (GUILayout.Button("?", GUILayout.Width(20)))
                {
                    EditorUtility.DisplayDialog("Transparency Info", 
                        "Transparency only works with PNG format and requires proper camera setup. " +
                        "The tool will automatically find the best camera for transparency capture or use Game View camera.", "OK");
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // Warning for transparency with wrong format
            if (settings.transparentBackground && settings.format != ImageFormat.PNG)
            {
                EditorGUILayout.HelpBox("Transparency only works with PNG format. Format will be automatically changed to PNG for transparent captures.", MessageType.Warning);
            }
            
            EditorGUILayout.Space();
            
            // Custom resolution settings
            settings.useCustomResolution = EditorGUILayout.Toggle("Use Custom Resolution", settings.useCustomResolution);
            
            if (settings.useCustomResolution)
            {
                EditorGUI.indentLevel++;
                
                // Resolution preset dropdown
                int selectedPreset = EditorPrefs.GetInt(ResolutionPresetKey, 0);
                string[] presetNames = ResolutionPresets.Select(p => p.name).ToArray();
                int newPreset = EditorGUILayout.Popup("Resolution Preset", selectedPreset, presetNames);
                
                if (newPreset != selectedPreset)
                {
                    EditorPrefs.SetInt(ResolutionPresetKey, newPreset);
                    if (newPreset > 0 && newPreset < ResolutionPresets.Length)
                    {
                        settings.customWidth = ResolutionPresets[newPreset].width;
                        settings.customHeight = ResolutionPresets[newPreset].height;
                    }
                }
                
                // Manual resolution input
                EditorGUILayout.BeginHorizontal();
                settings.customWidth = EditorGUILayout.IntField("Width", settings.customWidth);
                settings.customHeight = EditorGUILayout.IntField("Height", settings.customHeight);
                
                // Aspect ratio lock buttons
                if (GUILayout.Button("16:9", GUILayout.Width(40)))
                {
                    settings.customHeight = Mathf.RoundToInt(settings.customWidth * 9f / 16f);
                }
                if (GUILayout.Button("4:3", GUILayout.Width(40)))
                {
                    settings.customHeight = Mathf.RoundToInt(settings.customWidth * 3f / 4f);
                }
                if (GUILayout.Button("1:1", GUILayout.Width(40)))
                {
                    settings.customHeight = settings.customWidth;
                }
                EditorGUILayout.EndHorizontal();
                
                // Clamp resolution values
                settings.customWidth = Mathf.Clamp(settings.customWidth, 64, 8192);
                settings.customHeight = Mathf.Clamp(settings.customHeight, 64, 8192);
                
                EditorGUI.indentLevel--;
                
                // Display estimated file size
                float estimatedSizeMB = (settings.customWidth * settings.customHeight * (settings.format == ImageFormat.PNG ? 4 : 3)) / (1024f * 1024f);
                EditorGUILayout.LabelField($"Estimated size: ~{estimatedSizeMB:F1} MB", EditorStyles.miniLabel);
            }
        }

        /// <summary>
        /// Draw the batch processing tab
        /// </summary>
        private void DrawBatchTab()
        {
            EditorGUILayout.LabelField("Batch Processing", subHeaderStyle);
            EditorGUILayout.Space();
            
            settings.batchMode = EditorGUILayout.Toggle("Enable Batch Mode", settings.batchMode);
            
            if (!settings.batchMode)
            {
                EditorGUILayout.HelpBox("Enable batch mode to access batch capture features. Perfect for time-lapse sequences or multiple angle captures.", MessageType.Info);
                return;
            }
            
            // Batch configuration
            settings.batchCount = EditorGUILayout.IntSlider("Batch Count", settings.batchCount, 1, 1000);
            settings.batchInterval = EditorGUILayout.Slider("Interval (seconds)", settings.batchInterval, 0.1f, 300f);
            
            EditorGUILayout.Space();
            
            // Batch options
            settings.batchAutoIncrement = EditorGUILayout.Toggle("Auto Increment Names", settings.batchAutoIncrement);
            settings.batchCreateSubfolder = EditorGUILayout.Toggle("Create Batch Subfolder", settings.batchCreateSubfolder);
            
            // Time estimation
            var totalTime = TimeSpan.FromSeconds(settings.batchCount * settings.batchInterval);
            EditorGUILayout.LabelField($"Estimated Duration: {totalTime:hh\\:mm\\:ss}", EditorStyles.miniLabel);
            
            GUILayout.Space(20);
            
            // Batch control buttons
            EditorGUILayout.BeginHorizontal();
            
            bool batchRunning = batchProcessor?.IsRunning ?? false;
            bool batchPaused = batchProcessor?.IsPaused ?? false;
            
            if (!batchRunning)
            {
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Start Batch Capture", GUILayout.Height(30)))
                {
                    StartBatchCapture();
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Stop Batch", GUILayout.Height(30)))
                {
                    StopBatchCapture();
                }
                GUI.backgroundColor = Color.white;
                
                GUI.backgroundColor = batchPaused ? Color.green : Color.yellow;
                if (GUILayout.Button(batchPaused ? "Resume" : "Pause", GUILayout.Height(30)))
                {
                    if (batchPaused)
                        batchProcessor.Resume();
                    else
                        batchProcessor.Pause();
                }
                GUI.backgroundColor = Color.white;
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Batch status display
            if (batchRunning)
            {
                GUILayout.Space(10);
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Batch Status", subHeaderStyle);
                EditorGUILayout.LabelField($"Progress: {batchProcessor.CompletedCount}/{batchProcessor.TotalCount}");
                EditorGUILayout.LabelField($"Status: {(batchPaused ? "Paused" : "Running")}");
                EditorGUILayout.LabelField($"Time Elapsed: {batchProcessor.ElapsedTime:hh\\:mm\\:ss}");
                EditorGUILayout.LabelField($"Est. Remaining: {batchProcessor.EstimatedTimeRemaining:hh\\:mm\\:ss}");
                
                if (batchProcessor.CompletedCount > 0)
                {
                    var avgInterval = batchProcessor.ElapsedTime.TotalSeconds / batchProcessor.CompletedCount;
                    EditorGUILayout.LabelField($"Avg. Interval: {avgInterval:F1}s", EditorStyles.miniLabel);
                }
                EditorGUILayout.EndVertical();
            }
        }

        /// <summary>
        /// Draw the camera management tab
        /// </summary>
        private void DrawCamerasTab()
        {
            EditorGUILayout.LabelField("Camera Management", subHeaderStyle);
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Camera List"))
            {
                RefreshCameraList();
            }
            
            if (GUILayout.Button("Select All Active"))
            {
                cameraSetups.Where(c => c.camera != null && c.camera.gameObject.activeInHierarchy)
                           .ToList().ForEach(c => c.isSelected = true);
            }
            EditorGUILayout.EndHorizontal();
            
            if (cameraSetups.Count == 0)
            {
                EditorGUILayout.HelpBox("No cameras found in the scene. Add cameras to your scene to use multi-camera capture.", MessageType.Info);
                return;
            }
            
            EditorGUILayout.Space();
            
            // Camera list with detailed information
            for (int i = 0; i < cameraSetups.Count; i++)
            {
                var setup = cameraSetups[i];
                bool cameraExists = setup.camera != null;
                bool cameraActive = cameraExists && setup.camera.gameObject.activeInHierarchy;
                
                // Color code based on camera state
                GUI.backgroundColor = !cameraExists ? Color.red : 
                                     !cameraActive ? Color.yellow : Color.white;
                
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                
                // Selection toggle
                setup.isSelected = EditorGUILayout.Toggle(setup.isSelected, GUILayout.Width(20));
                
                // Camera name and status
                string cameraName = cameraExists ? setup.camera.name : "Missing Camera";
                if (!cameraActive && cameraExists) cameraName += " (Inactive)";
                
                EditorGUILayout.LabelField(cameraName, GUILayout.Width(150));
                
                // Custom naming
                setup.customName = EditorGUILayout.TextField(setup.customName);
                
                // Individual capture button
                GUI.enabled = cameraExists && cameraActive;
                if (GUILayout.Button("Capture", GUILayout.Width(60)))
                {
                    CaptureCameraView(setup);
                }
                GUI.enabled = true;
                
                EditorGUILayout.EndHorizontal();
                
                // Camera details when selected and valid
                if (setup.isSelected && cameraExists && cameraActive)
                {
                    EditorGUI.indentLevel++;
                    
                    EditorGUILayout.BeginHorizontal();
                    // Access camera properties safely on main thread
                    try
                    {
                        EditorGUILayout.LabelField($"Resolution: {setup.camera.pixelWidth}x{setup.camera.pixelHeight}", GUILayout.Width(150));
                        EditorGUILayout.LabelField($"FOV: {setup.camera.fieldOfView:F1}°", GUILayout.Width(80));
                        EditorGUILayout.LabelField($"Depth: {setup.camera.depth}", GUILayout.Width(80));
                    }
                    catch (System.Exception)
                    {
                        EditorGUILayout.LabelField("Resolution: N/A", GUILayout.Width(150));
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Clear Flags: {setup.camera.clearFlags}", GUILayout.Width(200));
                    if (setup.camera.clearFlags == CameraClearFlags.SolidColor)
                    {
                        EditorGUILayout.LabelField($"BG: {ColorUtility.ToHtmlStringRGB(setup.camera.backgroundColor)}", GUILayout.Width(100));
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.EndVertical();
                GUI.backgroundColor = Color.white;
            }
            
            GUILayout.Space(10);
            
            // Multi-camera controls
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All"))
            {
                cameraSetups.ForEach(c => c.isSelected = true);
            }
            if (GUILayout.Button("Select None"))
            {
                cameraSetups.ForEach(c => c.isSelected = false);
            }
            if (GUILayout.Button("Invert Selection"))
            {
                cameraSetups.ForEach(c => c.isSelected = !c.isSelected);
            }
            EditorGUILayout.EndHorizontal();
            
            var selectedCount = cameraSetups.Count(c => c.isSelected && c.camera != null && c.camera.gameObject.activeInHierarchy);
            if (selectedCount > 0)
            {
                GUILayout.Space(10);
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button($"Capture Selected Cameras ({selectedCount})", GUILayout.Height(30)))
                {
                    CaptureSelectedCameras();
                }
                GUI.backgroundColor = Color.white;
            }
            
            // Camera setup tips
            GUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "Tips for best results:\n" +
                "• For transparency: Set camera Clear Flags to 'Solid Color' with alpha 0\n" +
                "• Use PNG format for transparent backgrounds\n" +
                "• Higher camera depth renders on top of lower depth cameras\n" +
                "• Disable cameras you don't want to capture", 
                MessageType.Info);
        }

        /// <summary>
        /// Draw the social media export tab
        /// </summary>
        private void DrawSocialTab()
        {
            EditorGUILayout.LabelField("Social Media Export", subHeaderStyle);
            EditorGUILayout.Space();
            
            if (socialFormats.Count == 0)
            {
                EditorGUILayout.HelpBox("No social media formats configured. Add formats for quick export to different platforms.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField("Available Formats:", EditorStyles.miniLabel);
            }
            
            // Display existing formats
            for (int i = 0; i < socialFormats.Count; i++)
            {
                var format = socialFormats[i];
                
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                
                // Platform and format info
                EditorGUILayout.LabelField($"{format.platform}", GUILayout.Width(80));
                EditorGUILayout.LabelField($"{format.name}", GUILayout.Width(150));
                EditorGUILayout.LabelField($"{format.width}x{format.height}", GUILayout.Width(100));
                EditorGUILayout.LabelField($"Q:{format.quality}%", GUILayout.Width(50));
                
                GUILayout.FlexibleSpace();
                
                // Action buttons
                if (GUILayout.Button("Export Last", GUILayout.Width(80)))
                {
                    ExportForSocialMedia(format);
                }
                
                if (GUILayout.Button("Edit", GUILayout.Width(40)))
                {
                    ShowSocialFormatEditDialog(format);
                }
                
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("×", GUILayout.Width(25)))
                {
                    socialFormats.RemoveAt(i);
                    break;
                }
                GUI.backgroundColor = Color.white;
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
            
            GUILayout.Space(10);
            
            // Add new format controls
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Custom Format"))
            {
                ShowSocialFormatEditDialog(new SocialMediaFormat 
                { 
                    platform = "Custom",
                    name = "Custom Format",
                    width = 1920,
                    height = 1080,
                    quality = 90
                });
            }
            
            if (GUILayout.Button("Add Popular Formats"))
            {
                AddPopularSocialFormats();
            }
            EditorGUILayout.EndHorizontal();
            
            if (socialFormats.Count > 0)
            {
                GUILayout.Space(10);
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Export Last Screenshot to All Formats", GUILayout.Height(30)))
                {
                    ExportAllSocialFormats();
                }
                GUI.backgroundColor = Color.white;
            }
            
            // Social media tips
            GUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "Social Media Tips:\n" +
                "• Instagram: Use square (1:1) for posts, vertical (9:16) for stories\n" +
                "• Twitter: 16:9 ratio works best for engagement\n" +
                "• YouTube: 16:9 thumbnails are required\n" +
                "• Keep important content in center for mobile viewing", 
                MessageType.Info);
        }

        /// <summary>
        /// Draw the presets management tab
        /// </summary>
        private void DrawPresetsTab()
        {
            EditorGUILayout.LabelField("Presets Management", subHeaderStyle);
            EditorGUILayout.Space();
            
            if (presets.Count == 0)
            {
                EditorGUILayout.HelpBox("No presets saved. Save your current settings as a preset for quick access later.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField("Saved Presets:", EditorStyles.miniLabel);
            }
            
            // Display existing presets
            for (int i = 0; i < presets.Count; i++)
            {
                var preset = presets[i];
                
                EditorGUILayout.BeginHorizontal("box");
                
                // Preset info
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(preset.name, EditorStyles.boldLabel);
                
                if (preset.settings != null)
                {
                    string presetInfo = $"{preset.settings.format} • ";
                    presetInfo += preset.settings.useCustomResolution ? 
                        $"{preset.settings.customWidth}x{preset.settings.customHeight}" : 
                        $"Scale {preset.settings.resolutionMultiplier:F1}x";
                    
                    if (preset.settings.transparentBackground) presetInfo += " • Transparent";
                    if (preset.settings.batchMode) presetInfo += $" • Batch {preset.settings.batchCount}";
                    
                    EditorGUILayout.LabelField(presetInfo, EditorStyles.miniLabel);
                }
                EditorGUILayout.EndVertical();
                
                GUILayout.FlexibleSpace();
                
                // Action buttons
                if (GUILayout.Button("Load", GUILayout.Width(50)))
                {
                    LoadPreset(preset);
                }
                
                if (GUILayout.Button("Update", GUILayout.Width(60)))
                {
                    preset.settings = settings.Clone();
                    SavePresets();
                    EditorUtility.DisplayDialog("Preset Updated", $"Preset '{preset.name}' has been updated with current settings.", "OK");
                }
                
                if (GUILayout.Button("Rename", GUILayout.Width(60)))
                {
                    string newName = ShowInputDialog("Rename Preset", "Enter new name:", preset.name);
                    if (!string.IsNullOrEmpty(newName))
                    {
                        preset.name = newName;
                        SavePresets();
                    }
                }
                
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Delete", GUILayout.Width(50)))
                {
                    if (EditorUtility.DisplayDialog("Delete Preset", $"Are you sure you want to delete '{preset.name}'?", "Delete", "Cancel"))
                    {
                        presets.RemoveAt(i);
                        SavePresets();
                        break;
                    }
                }
                GUI.backgroundColor = Color.white;
                
                EditorGUILayout.EndHorizontal();
            }
            
            GUILayout.Space(10);
            
            // Preset management controls
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Current as Preset"))
            {
                SaveAsPreset();
            }
            
            if (GUILayout.Button("Load Default Presets"))
            {
                if (EditorUtility.DisplayDialog("Load Defaults", "This will add default presets. Existing presets will be kept.", "Add Defaults", "Cancel"))
                {
                    LoadDefaultPresets();
                }
            }
            
            if (presets.Count > 0 && GUILayout.Button("Clear All"))
            {
                if (EditorUtility.DisplayDialog("Clear All Presets", "Are you sure you want to delete all presets?", "Clear All", "Cancel"))
                {
                    presets.Clear();
                    SavePresets();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw the capture history tab
        /// </summary>
        private void DrawHistoryTab()
        {
            EditorGUILayout.LabelField("Capture History", subHeaderStyle);
            EditorGUILayout.Space();
            
            if (history == null || history.Count == 0)
            {
                EditorGUILayout.HelpBox("No captures in history. Start taking screenshots to build your capture history.", MessageType.Info);
                return;
            }
            
            EditorGUILayout.LabelField($"Recent Captures ({Math.Min(history.Count, 20)} of {history.Count}):", EditorStyles.miniLabel);
            GUILayout.Space(5);
            
            var entries = history.GetRecent(20);
            foreach (var entry in entries)
            {
                bool fileExists = File.Exists(entry.filePath);
                
                GUI.backgroundColor = fileExists ? Color.white : new Color(1f, 0.8f, 0.8f);
                EditorGUILayout.BeginHorizontal("box");
                
                // File info
                EditorGUILayout.BeginVertical();
                string fileName = Path.GetFileName(entry.filePath);
                EditorGUILayout.LabelField(fileName, GUILayout.Width(200));
                EditorGUILayout.LabelField(entry.timestamp.ToString("yyyy-MM-dd HH:mm:ss"), EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
                
                // Settings info
                if (entry.settings != null)
                {
                    EditorGUILayout.BeginVertical();
                    string settingsInfo = $"{entry.settings.format}";
                    if (entry.settings.useCustomResolution)
                        settingsInfo += $" • {entry.settings.customWidth}x{entry.settings.customHeight}";
                    else
                        settingsInfo += $" • Scale {entry.settings.resolutionMultiplier:F1}x";
                    
                    EditorGUILayout.LabelField(settingsInfo, EditorStyles.miniLabel);
                    
                    if (entry.settings.transparentBackground)
                        EditorGUILayout.LabelField("Transparent", EditorStyles.miniLabel);
                    EditorGUILayout.EndVertical();
                }
                
                GUILayout.FlexibleSpace();
                
                // Action buttons
                GUI.enabled = fileExists;
                if (GUILayout.Button("Open", GUILayout.Width(50)))
                {
                    try
                    {
                        System.Diagnostics.Process.Start(entry.filePath);
                    }
                    catch (Exception e)
                    {
                        EditorUtility.DisplayDialog("Error", $"Failed to open file: {e.Message}", "OK");
                    }
                }
                
                if (GUILayout.Button("Show", GUILayout.Width(50)))
                {
                    EditorUtility.RevealInFinder(entry.filePath);
                }
                GUI.enabled = true;
                
                // Copy path button
                if (GUILayout.Button("Copy Path", GUILayout.Width(70)))
                {
                    EditorGUIUtility.systemCopyBuffer = entry.filePath;
                }
                
                // Re-export button
                if (GUILayout.Button("Export", GUILayout.Width(50)) && entry.settings != null)
                {
                    // Re-export with same settings
                    var exportSettings = entry.settings.Clone();
                    CaptureScreenshotAPI(exportSettings);
                }
                
                EditorGUILayout.EndHorizontal();
                GUI.backgroundColor = Color.white;
            }
            
            GUILayout.Space(10);
            
            // History management
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh History"))
            {
                // Verify file existence and remove missing entries
                history.CleanupMissingFiles();
            }
            
            if (GUILayout.Button("Export History"))
            {
                ExportHistoryToJson();
            }
            
            if (GUILayout.Button("Clear History"))
            {
                if (EditorUtility.DisplayDialog("Clear History", "Are you sure you want to clear the capture history?", "Clear", "Cancel"))
                {
                    history.Clear();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw the footer with links and copyright
        /// </summary>
        private void DrawFooter()
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal("box");
            
            // Quick actions
            if (GUILayout.Button("🌐 Website", EditorStyles.miniButton, GUILayout.Height(20)))
            {
                Application.OpenURL(WebsiteURL);
            }

            if (GUILayout.Button("📠 Products", EditorStyles.miniButton, GUILayout.Height(20)))
            {
                Application.OpenURL(ProductsURL);
            }
            
            if (GUILayout.Button("📚 Docs", EditorStyles.miniButton, GUILayout.Height(20)))
            {
                Application.OpenURL(DocumentationURL);
            }
            
            if (GUILayout.Button("📧 Support", EditorStyles.miniButton, GUILayout.Height(20)))
            {
                Application.OpenURL($"mailto:{SupportEmail}?subject=W Screenshot Tool v{ToolVersion} Support");
            }
            
            if (GUILayout.Button("⚙️ Settings", EditorStyles.miniButton, GUILayout.Height(20)))
            {
                ShowAdvancedSettings();
            }
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"© {DeveloperName} | v{ToolVersion}", EditorStyles.miniLabel);
            
            EditorGUILayout.EndHorizontal();
        }
        #endregion

        #region Capture Methods
        /// <summary>
        /// Main screenshot capture method - handles all capture types with proper transparency support
        /// </summary>
        private void CaptureScreenshot()
        {
            // Create capture settings and ensure transparency works for all modes
            var captureSettings = settings.Clone();
            
            // If transparency is requested, find the best capture method
            if (captureSettings.transparentBackground)
            {
                // Force PNG format for transparency
                captureSettings.format = ImageFormat.PNG;
                
                // Try to find a suitable camera for transparency
                Camera bestCamera = FindBestCameraForTransparency();
                if (bestCamera != null)
                {
                    // Use camera capture for transparency
                    var cameraSetup = new CameraSetup { camera = bestCamera, customName = bestCamera.name };
                    var task = new CaptureTask(CaptureType.Camera, captureSettings) { cameraSetup = cameraSetup };
                    EnqueueCaptureTask(task);
                    return;
                }
                else
                {
                    // Warn user about transparency limitations
                    Debug.LogWarning("No suitable camera found for transparent background. Using standard capture method.");
                    EditorUtility.DisplayDialog("Transparency Warning", 
                        "No camera with proper transparency settings found. For best transparency results:\n" +
                        "1. Set a camera's Clear Flags to 'Solid Color'\n" +
                        "2. Set background color alpha to 0\n" +
                        "3. Use the Camera tab for transparent captures", "OK");
                }
            }
            
            // Standard screen capture
            var standardTask = new CaptureTask(CaptureType.Screen, captureSettings);
            EnqueueCaptureTask(standardTask);
        }

        /// <summary>
        /// Find the best camera for transparency capture
        /// </summary>
        /// <returns>Camera suitable for transparency or null if none found</returns>
        private Camera FindBestCameraForTransparency()
        {
            var cameras = FindObjectsOfType<Camera>();
            
            // First priority: cameras with solid color clear flags and transparent background
            foreach (var camera in cameras)
            {
                if (camera.clearFlags == CameraClearFlags.SolidColor && 
                    camera.backgroundColor.a == 0f &&
                    camera.gameObject.activeInHierarchy)
                {
                    return camera;
                }
            }
            
            // Second priority: any camera with solid color clear flags
            foreach (var camera in cameras)
            {
                if (camera.clearFlags == CameraClearFlags.SolidColor && 
                    camera.gameObject.activeInHierarchy)
                {
                    return camera;
                }
            }
            
            // Third priority: main camera or first active camera
            Camera mainCamera = Camera.main;
            if (mainCamera != null && mainCamera.gameObject.activeInHierarchy)
                return mainCamera;
                
            return cameras.FirstOrDefault(c => c.gameObject.activeInHierarchy);
        }

        /// <summary>
        /// Quick capture with optimized settings for speed
        /// </summary>
        private void QuickCapture()
        {
            var quickSettings = settings.Clone();
            quickSettings.resolutionMultiplier = 1f;
            quickSettings.format = ImageFormat.PNG;
            quickSettings.showConsoleMessage = false;
            quickSettings.transparentBackground = false; // Disable for speed
            
            var task = new CaptureTask(CaptureType.Screen, quickSettings);
            EnqueueCaptureTask(task);
        }

        /// <summary>
        /// Capture only the Game View with proper transparency support
        /// </summary>
        private void CaptureGameView()
        {
            var gameViewSettings = settings.Clone();
            
            // For Game View with transparency, use main camera
            if (gameViewSettings.transparentBackground)
            {
                gameViewSettings.format = ImageFormat.PNG;
                Camera bestCamera = FindBestCameraForTransparency();
                if (bestCamera != null)
                {
                    var cameraSetup = new CameraSetup { camera = bestCamera, customName = "Game View Camera" };
                    var task = new CaptureTask(CaptureType.Camera, gameViewSettings) { cameraSetup = cameraSetup };
                    EnqueueCaptureTask(task);
                    return;
                }
            }
            
            var task2 = new CaptureTask(CaptureType.GameView, gameViewSettings);
            EnqueueCaptureTask(task2);
        }

        /// <summary>
        /// Capture the Scene View
        /// </summary>
        private void CaptureSceneView()
        {
            var sceneViewSettings = settings.Clone();
            
            // Scene view transparency is handled differently
            if (sceneViewSettings.transparentBackground)
            {
                sceneViewSettings.format = ImageFormat.PNG;
                Debug.LogWarning("Scene View transparency may not work as expected. Consider using a camera instead.");
            }
            
            var task = new CaptureTask(CaptureType.SceneView, sceneViewSettings);
            EnqueueCaptureTask(task);
        }

        /// <summary>
        /// Capture from a specific camera setup
        /// </summary>
        /// <param name="setup">Camera setup to capture from</param>
        private void CaptureCameraView(CameraSetup setup)
        {
            if (setup?.camera == null)
            {
                Debug.LogError("Camera setup is null or camera is missing");
                return;
            }

            var task = new CaptureTask(CaptureType.Camera, settings.Clone()) 
            { 
                cameraSetup = setup 
            };
            EnqueueCaptureTask(task);
        }

        /// <summary>
        /// Capture all selected cameras in sequence
        /// </summary>
        private void CaptureSelectedCameras()
        {
            var selectedCameras = cameraSetups.Where(s => s.isSelected && 
                                                         s.camera != null && 
                                                         s.camera.gameObject.activeInHierarchy).ToList();
            
            if (selectedCameras.Count == 0)
            {
                EditorUtility.DisplayDialog("No Cameras", "No valid cameras are selected.", "OK");
                return;
            }

            foreach (var setup in selectedCameras)
            {
                CaptureCameraView(setup);
            }
            
            if (settings.showConsoleMessage)
            {
                Debug.Log($"Queued {selectedCameras.Count} camera captures");
            }
        }

        /// <summary>
        /// Add capture task to processing queue
        /// </summary>
        /// <param name="task">Capture task to enqueue</param>
        private void EnqueueCaptureTask(CaptureTask task)
        {
            captureQueue.Enqueue(task);
            
            if (settings.showConsoleMessage)
            {
                Debug.Log($"Capture task queued: {task.type}");
            }
        }

        /// <summary>
        /// Process capture tasks from queue (called from EditorApplication.update)
        /// </summary>
        private void ProcessCaptureQueue()
        {
            if (captureQueue.Count > 0 && !isCapturing)
            {
                var task = captureQueue.Dequeue();
                ProcessCaptureTask(task);
            }
        }

        /// <summary>
        /// Process a single capture task synchronously on main thread
        /// </summary>
        /// <param name="task">Capture task to process</param>
        private void ProcessCaptureTask(CaptureTask task)
        {
            try
            {
                isCapturing = true;
                captureProgress = 0f;
                currentOperation = $"Preparing {task.type} capture...";
                
                string filePath = GenerateFilePath(task.settings, task.cameraSetup);
                
                captureProgress = 0.3f;
                currentOperation = "Capturing...";
                
                // Perform capture on main thread only
                switch (task.type)
                {
                    case CaptureType.Screen:
                        CaptureScreenMainThread(filePath, task.settings);
                        break;
                    case CaptureType.GameView:
                        CaptureGameViewMainThread(filePath, task.settings);
                        break;
                    case CaptureType.SceneView:
                        CaptureSceneViewMainThread(filePath, task.settings);
                        break;
                    case CaptureType.Camera:
                        PerformCameraCapture(task.cameraSetup.camera, filePath, task.settings);
                        break;
                }
                
                captureProgress = 0.8f;
                currentOperation = "Finalizing...";
                
                // Add to history
                history?.AddEntry(new ScreenshotHistoryEntry
                {
                    filePath = filePath,
                    timestamp = DateTime.Now,
                    settings = task.settings,
                    cameraName = task.cameraSetup?.camera?.name,
                    fileSize = File.Exists(filePath) ? new FileInfo(filePath).Length : 0
                });
                
                // Post-capture processing
                PerformPostCaptureActions(filePath, task.settings);
                
                captureProgress = 1f;
                currentOperation = "Complete!";
            }
            catch (Exception e)
            {
                Debug.LogError($"Capture failed: {e.Message}\nStack trace: {e.StackTrace}");
                EditorUtility.DisplayDialog("Capture Failed", $"Failed to capture screenshot:\n{e.Message}", "OK");
            }
            finally
            {
                isCapturing = false;
                currentOperation = "";
                captureProgress = 0f;
            }
        }

        /// <summary>
        /// Capture screen with proper transparency support (main thread only)
        /// </summary>
        /// <param name="filePath">Output file path</param>
        /// <param name="settings">Capture settings</param>
        private void CaptureScreenMainThread(string filePath, ScreenshotSettings settings)
        {
            // For screen capture, transparency is not directly supported
            // Use Unity's built-in screen capture
            if (settings.transparentBackground)
            {
                Debug.LogWarning("Transparent background is not supported for full screen capture. Use camera capture instead.");
            }
            
            // Calculate super sampling
            int superSize = Mathf.RoundToInt(settings.resolutionMultiplier);
            superSize = Mathf.Clamp(superSize, 1, 8);
            
            // Ensure directory exists
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Capture using Unity's method
            ScreenCapture.CaptureScreenshot(filePath, superSize);   
        }

        /// <summary>
        /// Capture Game View with transparency support (main thread only)
        /// </summary>
        /// <param name="filePath">Output file path</param>
        /// <param name="settings">Capture settings</param>
        private void CaptureGameViewMainThread(string filePath, ScreenshotSettings settings)
        {
            // For Game View, try to use the main camera if transparency is needed
            if (settings.transparentBackground)
            {
                Camera mainCamera = Camera.main ?? FindObjectOfType<Camera>();
                if (mainCamera != null)
                {
                    PerformCameraCapture(mainCamera, filePath, settings);
                    return;
                }
                else
                {
                    Debug.LogWarning("No camera found for transparent Game View capture. Using standard method.");
                }
            }
            
            // Standard Game View capture
            CaptureScreenMainThread(filePath, settings);
        }

        /// <summary>
        /// Capture Scene View (main thread only)
        /// </summary>
        /// <param name="filePath">Output file path</param>
        /// <param name="settings">Capture settings</param>
        private void CaptureSceneViewMainThread(string filePath, ScreenshotSettings settings)
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView?.camera == null)
            {
                throw new Exception("Scene View camera not found");
            }
            
            // Scene View capture is done via camera
            PerformCameraCapture(sceneView.camera, filePath, settings);
        }

        /// <summary>
        /// Perform camera capture with full transparency support (main thread only)
        /// </summary>
        /// <param name="camera">Camera to capture from</param>
        /// <param name="filePath">Output file path</param>
        /// <param name="settings">Capture settings</param>
        public void PerformCameraCapture(Camera camera, string filePath, ScreenshotSettings settings)
        {
            if (camera == null)
            {
                throw new ArgumentNullException(nameof(camera), "Camera cannot be null");
            }

            // All camera operations must be on main thread
            if (!UnityEditorInternal.InternalEditorUtility.isApplicationActive)
            {
                Debug.LogWarning("Cannot capture camera when Unity is not the active application");
                return;
            }

            // Calculate dimensions safely on main thread
            int width, height;
            if (settings.useCustomResolution)
            {
                width = settings.customWidth;
                height = settings.customHeight;
            }
            else
            {
                try
                {
                    // Access camera properties safely
                    width = Mathf.RoundToInt(camera.pixelWidth * settings.resolutionMultiplier);
                    height = Mathf.RoundToInt(camera.pixelHeight * settings.resolutionMultiplier);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to get camera pixel dimensions: {e.Message}. Using fallback resolution.");
                    width = Mathf.RoundToInt(1920 * settings.resolutionMultiplier);
                    height = Mathf.RoundToInt(1080 * settings.resolutionMultiplier);
                }
            }

            // Ensure minimum dimensions
            width = Mathf.Max(width, 64);
            height = Mathf.Max(height, 64);

            // Set up render texture format based on transparency needs
            RenderTextureFormat rtFormat = RenderTextureFormat.ARGB32;
            TextureFormat textureFormat = (settings.transparentBackground && settings.format == ImageFormat.PNG) ? 
                                         TextureFormat.RGBA32 : TextureFormat.RGB24;

            // Create render texture
            RenderTexture rt = new RenderTexture(width, height, 24, rtFormat);
            if (!rt.Create())
            {
                throw new Exception("Failed to create RenderTexture");
            }

            // Store original camera settings
            RenderTexture originalTarget = camera.targetTexture;
            CameraClearFlags originalClearFlags = camera.clearFlags;
            Color originalBackgroundColor = camera.backgroundColor;

            try
            {
                // Configure camera for capture
                camera.targetTexture = rt;

                // Handle transparency
                if (settings.transparentBackground && settings.format == ImageFormat.PNG)
                {
                    camera.clearFlags = CameraClearFlags.SolidColor;
                    camera.backgroundColor = new Color(0, 0, 0, 0); // Transparent
                }

                // Create screenshot texture
                Texture2D screenshot = new Texture2D(width, height, textureFormat, false);

                try
                {
                    // Render and read pixels (must be on main thread)
                    camera.Render();
                    
                    RenderTexture.active = rt;
                    screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                    screenshot.Apply();
                    RenderTexture.active = null;

                    // Encode and save
                    byte[] bytes = null;
                    switch (settings.format)
                    {
                        case ImageFormat.PNG:
                            bytes = screenshot.EncodeToPNG();
                            break;
                        case ImageFormat.JPG:
                            bytes = screenshot.EncodeToJPG(settings.jpegQuality);
                            break;
                    }

                    if (bytes != null && bytes.Length > 0)
                    {
                        // Ensure directory exists
                        string directory = Path.GetDirectoryName(filePath);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }
                        
                        File.WriteAllBytes(filePath, bytes);
                        
                        if (settings.showConsoleMessage)
                        {
                            Debug.Log($"Camera capture saved: {Path.GetFileName(filePath)} ({bytes.Length / 1024f:F1} KB)");
                        }
                    }
                    else
                    {
                        throw new Exception("Failed to encode image data");
                    }
                }
                finally
                {
                    // Always cleanup screenshot texture
                    if (screenshot != null)
                    {
                        DestroyImmediate(screenshot);
                    }
                }
            }
            finally
            {
                // Restore original camera settings
                camera.targetTexture = originalTarget;
                camera.clearFlags = originalClearFlags;
                camera.backgroundColor = originalBackgroundColor;
                
                // Cleanup render texture
                if (rt != null)
                {
                    rt.Release();
                    DestroyImmediate(rt);
                }
            }
        }

        /// <summary>
        /// Generate file path for screenshot with proper naming
        /// </summary>
        /// <param name="settings">Capture settings</param>
        /// <param name="cameraSetup">Optional camera setup for naming</param>
        /// <returns>Generated file path</returns>
        private string GenerateFilePath(ScreenshotSettings settings, CameraSetup cameraSetup = null)
        {
            string fileName = settings.screenshotName;
            
            // Add timestamp if enabled
            if (settings.autoTimestamp)
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                fileName += $"_{timestamp}";
            }
            
            // Add camera name if capturing from camera
            if (cameraSetup != null && !string.IsNullOrEmpty(cameraSetup.DisplayName))
            {
                string cleanCameraName = SanitizeFileName(cameraSetup.DisplayName);
                fileName += $"_{cleanCameraName}";
            }
            
            // Add file extension
            string extension = settings.format == ImageFormat.PNG ? ".png" : ".jpg";
            fileName += extension;
            
            // Combine with save path
            return Path.Combine(settings.savePath, fileName);
        }

        /// <summary>
        /// Sanitize filename to remove invalid characters
        /// </summary>
        /// <param name="fileName">Original filename</param>
        /// <returns>Sanitized filename</returns>
        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return "Unknown";
            
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }
            
            return fileName;
        }

        /// <summary>
        /// Perform post-capture actions like file operations and notifications
        /// </summary>
        /// <param name="filePath">Path of captured file</param>
        /// <param name="settings">Capture settings used</param>
        private void PerformPostCaptureActions(string filePath, ScreenshotSettings settings)
        {
            // Asset database refresh
            if (settings.autoRefresh)
            {
                AssetDatabase.Refresh();
            }
            
            // Console logging
            if (settings.showConsoleMessage && File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                Debug.Log($"Screenshot saved: {Path.GetFileName(filePath)} ({fileInfo.Length / 1024f:F1} KB)");
            }
            
            // Open file after capture
            if (settings.openAfterCapture && File.Exists(filePath))
            {
                try
                {
                    System.Diagnostics.Process.Start(filePath);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to open screenshot: {e.Message}");
                }
            }
        }
        #endregion

        #region Batch Processing
        /// <summary>
        /// Start batch capture process
        /// </summary>
        private void StartBatchCapture()
        {
            if (batchProcessor == null)
            {
                batchProcessor = new BatchProcessor(settings);
            }
            
            // Create batch subfolder if requested
            if (settings.batchCreateSubfolder)
            {
                string batchFolder = Path.Combine(settings.savePath, $"Batch_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}");
                Directory.CreateDirectory(batchFolder);
                
                // Update settings to use batch folder
                var batchSettings = settings.Clone();
                batchSettings.savePath = batchFolder;
                batchProcessor.UpdateSettings(batchSettings);
            }
            
            batchProcessor.Start();
            
            if (settings.showConsoleMessage)
            {
                Debug.Log($"Started batch capture: {settings.batchCount} screenshots, {settings.batchInterval}s interval");
            }
        }

        /// <summary>
        /// Stop batch capture process
        /// </summary>
        private void StopBatchCapture()
        {
            batchProcessor?.Stop();
            
            if (settings.showConsoleMessage)
            {
                Debug.Log("Batch capture stopped");
            }
        }
        #endregion

        #region Social Media Export
        /// <summary>
        /// Export last captured screenshot for specific social media format
        /// </summary>
        /// <param name="format">Social media format to export</param>
        private void ExportForSocialMedia(SocialMediaFormat format)
        {
            var lastEntry = history?.GetMostRecent();
            if (lastEntry == null || !File.Exists(lastEntry.filePath))
            {
                EditorUtility.DisplayDialog("No Screenshot", "No recent screenshot found to export.", "OK");
                return;
            }
            
            try
            {
                // Load original image
                byte[] originalBytes = File.ReadAllBytes(lastEntry.filePath);
                Texture2D originalTexture = new Texture2D(2, 2);
                originalTexture.LoadImage(originalBytes);
                
                // Resize for social media format
                Texture2D resizedTexture = ResizeTexture(originalTexture, format.width, format.height);
                
                // Generate export path
                string fileName = Path.GetFileNameWithoutExtension(lastEntry.filePath);
                string exportPath = Path.Combine(settings.savePath, $"{fileName}_{format.platform}_{format.name}.jpg");
                
                // Encode and save
                byte[] exportBytes = resizedTexture.EncodeToJPG(format.quality);
                File.WriteAllBytes(exportPath, exportBytes);
                
                // Cleanup
                DestroyImmediate(originalTexture);
                DestroyImmediate(resizedTexture);
                
                Debug.Log($"Exported for {format.platform}: {Path.GetFileName(exportPath)}");
                
                if (settings.autoRefresh)
                {
                    AssetDatabase.Refresh();
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Export Failed", $"Failed to export for {format.platform}:\n{e.Message}", "OK");
            }
        }

        /// <summary>
        /// Export last screenshot to all configured social media formats
        /// </summary>
        private void ExportAllSocialFormats()
        {
            foreach (var format in socialFormats)
            {
                ExportForSocialMedia(format);
            }
        }

        /// <summary>
        /// Add popular social media formats to the list
        /// </summary>
        private void AddPopularSocialFormats()
        {
            InitializeSocialFormats(); // This will add default formats if none exist
            Debug.Log("Popular social media formats added");
        }

        /// <summary>
        /// Show dialog for editing social media format
        /// </summary>
        /// <param name="format">Format to edit</param>
        private void ShowSocialFormatEditDialog(SocialMediaFormat format)
        {
            // This would show a popup window for editing
            // For now, just add it to the list if it's new
            if (!socialFormats.Contains(format))
            {
                socialFormats.Add(format);
            }
        }

        /// <summary>
        /// Resize texture to specific dimensions
        /// </summary>
        /// <param name="source">Source texture</param>
        /// <param name="targetWidth">Target width</param>
        /// <param name="targetHeight">Target height</param>
        /// <returns>Resized texture</returns>
        private Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
            RenderTexture.active = rt;
            
            Graphics.Blit(source, rt);
            
            Texture2D result = new Texture2D(targetWidth, targetHeight);
            result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            result.Apply();
            
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            
            return result;
        }
        #endregion

        #region Preset Management
        /// <summary>
        /// Save current settings as a new preset
        /// </summary>
        private void SaveAsPreset()
        {
            string presetName = ShowInputDialog("Save Preset", "Enter preset name:", $"Preset {presets.Count + 1}");
            if (!string.IsNullOrEmpty(presetName))
            {
                var preset = new ScreenshotPreset(presetName, settings);
                presets.Add(preset);
                SavePresets();
                
                Debug.Log($"Preset saved: {presetName}");
            }
        }

        /// <summary>
        /// Load settings from a preset
        /// </summary>
        /// <param name="preset">Preset to load</param>
        private void LoadPreset(ScreenshotPreset preset)
        {
            if (preset?.settings != null)
            {
                settings = preset.settings.Clone();
                Debug.Log($"Preset loaded: {preset.name}");
            }
        }
        #endregion

        #region History Management
        /// <summary>
        /// Export capture history to JSON file
        /// </summary>
        private void ExportHistoryToJson()
        {
            if (history == null || history.Count == 0)
            {
                EditorUtility.DisplayDialog("No History", "No capture history to export.", "OK");
                return;
            }
            
            string exportPath = EditorUtility.SaveFilePanel("Export History", "", "screenshot_history", "json");
            if (!string.IsNullOrEmpty(exportPath))
            {
                try
                {
                    string json = JsonUtility.ToJson(history, true);
                    File.WriteAllText(exportPath, json);
                    Debug.Log($"History exported to: {exportPath}");
                }
                catch (Exception e)
                {
                    EditorUtility.DisplayDialog("Export Failed", $"Failed to export history:\n{e.Message}", "OK");
                }
            }
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Show input dialog for text input
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="message">Dialog message</param>
        /// <param name="defaultValue">Default input value</param>
        /// <returns>User input or null if cancelled</returns>
        private string ShowInputDialog(string title, string message, string defaultValue = "")
        {
            // Unity doesn't have a built-in input dialog, so we'll use a simple approach
            // In a real implementation, you might want to create a custom EditorWindow
            return defaultValue; // Placeholder - replace with actual dialog implementation
        }

        /// <summary>
        /// Show advanced settings dialog
        /// </summary>
        private void ShowAdvancedSettings()
        {
            EditorUtility.DisplayDialog("Advanced Settings", 
                "Advanced settings can be accessed through the different tabs:\n" +
                "• Advanced tab: Custom resolutions, transparency\n" +
                "• Batch tab: Automated capture settings\n" +
                "• Cameras tab: Multi-camera management\n" +
                "• Social tab: Platform-specific exports\n" +
                "• Presets tab: Save/load configurations", "OK");
        }

        /// <summary>
        /// Get reference to Game View window
        /// </summary>
        /// <returns>Game View EditorWindow or null</returns>
        private EditorWindow GetGameView()
        {
            System.Type gameViewType = System.Type.GetType("UnityEditor.GameView,UnityEditor");
            if (gameViewType != null)
            {
                return EditorWindow.GetWindow(gameViewType, false, null, false);
            }
            return null;
        }
        #endregion
    }

    #region Supporting Classes
    /// <summary>
    /// Manages batch processing operations
    /// </summary>
    public class BatchProcessor
    {
        private ScreenshotSettings settings;
        private bool isRunning = false;
        private bool isPaused = false;
        private int completedCount = 0;
        private int totalCount = 0;
        private DateTime startTime;
        private DateTime pauseTime;
        private TimeSpan pausedDuration = TimeSpan.Zero;

        public bool IsRunning => isRunning;
        public bool IsPaused => isPaused;
        public int CompletedCount => completedCount;
        public int TotalCount => totalCount;
        public float Progress => totalCount > 0 ? (float)completedCount / totalCount : 0f;
        
        public TimeSpan ElapsedTime 
        { 
            get 
            { 
                var elapsed = DateTime.Now - startTime - pausedDuration;
                if (isPaused)
                    elapsed -= DateTime.Now - pauseTime;
                return elapsed;
            } 
        }
        
        public TimeSpan EstimatedTimeRemaining
        {
            get
            {
                if (completedCount == 0 || !isRunning) return TimeSpan.Zero;
                var avgTime = ElapsedTime.TotalSeconds / completedCount;
                var remaining = (totalCount - completedCount) * avgTime;
                return TimeSpan.FromSeconds(remaining);
            }
        }

        public BatchProcessor(ScreenshotSettings settings)
        {
            this.settings = settings;
        }

        public void UpdateSettings(ScreenshotSettings newSettings)
        {
            this.settings = newSettings;
        }

        public void Start()
        {
            if (isRunning) return;
            
            isRunning = true;
            isPaused = false;
            completedCount = 0;
            totalCount = settings.batchCount;
            startTime = DateTime.Now;
            pausedDuration = TimeSpan.Zero;
            
            // Start batch processing coroutine equivalent
            EditorApplication.update += BatchUpdate;
        }

        public void Stop()
        {
            isRunning = false;
            isPaused = false;
            EditorApplication.update -= BatchUpdate;
        }

        public void Pause()
        {
            if (!isRunning || isPaused) return;
            isPaused = true;
            pauseTime = DateTime.Now;
        }

        public void Resume()
        {
            if (!isRunning || !isPaused) return;
            pausedDuration += DateTime.Now - pauseTime;
            isPaused = false;
        }

        private float lastCaptureTime = 0f;

        private void BatchUpdate()
        {
            if (!isRunning || isPaused) return;
            
            if (completedCount >= totalCount)
            {
                Stop();
                return;
            }

            float currentTime = (float)EditorApplication.timeSinceStartup;
            if (currentTime - lastCaptureTime >= settings.batchInterval)
            {
                // Trigger next capture
                WScreenshotTool.CaptureScreenshotAPI(settings);
                completedCount++;
                lastCaptureTime = currentTime;
            }
        }
    }

    /// <summary>
    /// Manages screenshot capture history
    /// </summary>
    public class ScreenshotHistory
    {
        private List<ScreenshotHistoryEntry> entries = new List<ScreenshotHistoryEntry>();
        private const int MaxEntries = 1000;

        public int Count => entries.Count;

        public void AddEntry(ScreenshotHistoryEntry entry)
        {
            if (entry == null) return;
            
            entries.Insert(0, entry); // Add to beginning for recent-first order
            
            // Limit entries to prevent memory issues
            if (entries.Count > MaxEntries)
            {
                entries.RemoveRange(MaxEntries, entries.Count - MaxEntries);
            }
        }

        public List<ScreenshotHistoryEntry> GetRecent(int count)
        {
            return entries.Take(count).ToList();
        }

        public ScreenshotHistoryEntry GetMostRecent()
        {
            return entries.FirstOrDefault();
        }

        public void Clear()
        {
            entries.Clear();
        }

        public void CleanupMissingFiles()
        {
            entries.RemoveAll(entry => !File.Exists(entry.filePath));
        }
    }
    #endregion
}