using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Profiling;

namespace CupkekGames.Diagnostics
{
    public class MonitorController : MonoBehaviour
    {
        private UIDocument _uiDocument;
        [SerializeField] private float _updateInterval = 0.5f; // Update display every 0.5 seconds

        // UI Elements
        private Label _fpsLabel;
        private Label _memoryLabel;
        private Label _frameTimeLabel;
        private Label _resolutionLabel;
        private Label _graphicsAPILabel;
        private Label _gpuLabel;
        private Label _vramLabel;
        private Label _cpuLabel;
        private Label _osLabel;

        // FPS calculation variables
        private float _accumulatedTime = 0f;
        private int _frameCount = 0;
        private float _currentFPS = 0f;
        private float _timeSinceLastUpdate = 0f;

        // Frame time tracking
        private float _frameTimeAccumulator = 0f;
        private float _averageFrameTime = 0f;

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();

            // Get all UI elements
            _fpsLabel = _uiDocument.rootVisualElement.Q<VisualElement>("FPS").Q<Label>(null, "value");
            _memoryLabel = _uiDocument.rootVisualElement.Q<VisualElement>("Memory").Q<Label>(null, "value");
            _frameTimeLabel = _uiDocument.rootVisualElement.Q<VisualElement>("FrameTime").Q<Label>(null, "value");
            _resolutionLabel = _uiDocument.rootVisualElement.Q<VisualElement>("Resolution").Q<Label>(null, "value");
            _graphicsAPILabel = _uiDocument.rootVisualElement.Q<VisualElement>("GraphicsAPI").Q<Label>(null, "value");
            _gpuLabel = _uiDocument.rootVisualElement.Q<VisualElement>("GPU").Q<Label>(null, "value");
            _vramLabel = _uiDocument.rootVisualElement.Q<VisualElement>("VRAM").Q<Label>(null, "value");
            _cpuLabel = _uiDocument.rootVisualElement.Q<VisualElement>("CPU").Q<Label>(null, "value");
            _osLabel = _uiDocument.rootVisualElement.Q<VisualElement>("OS").Q<Label>(null, "value");

            // Initialize system info (these don't change during runtime)
            InitializeSystemInfo();
        }

        private void Update()
        {
            _frameCount++;
            _accumulatedTime += Time.unscaledDeltaTime;
            _timeSinceLastUpdate += Time.unscaledDeltaTime;

            // Track frame time
            float frameTime = Time.unscaledDeltaTime * 1000f; // Convert to milliseconds
            _frameTimeAccumulator += frameTime;

            // Update display at specified intervals
            if (_timeSinceLastUpdate >= _updateInterval)
            {
                // Calculate FPS
                _currentFPS = _frameCount / _accumulatedTime;
                _fpsLabel.text = _currentFPS.ToString("F1");

                // Calculate average frame time
                _averageFrameTime = _frameTimeAccumulator / _frameCount;
                _frameTimeLabel.text = _averageFrameTime.ToString("F1") + " ms";

                // Get memory usage
                long memoryUsage = Profiler.GetTotalAllocatedMemoryLong();
                _memoryLabel.text = (memoryUsage / (1024f * 1024f)).ToString("F1") + " MB";

                // Reset counters
                _frameCount = 0;
                _accumulatedTime = 0f;
                _timeSinceLastUpdate = 0f;
                _frameTimeAccumulator = 0f;
            }
        }

        private void InitializeSystemInfo()
        {
            // Screen resolution
            _resolutionLabel.text = $"{Screen.width}x{Screen.height}@{Screen.currentResolution.refreshRate}Hz";

            // Graphics API
            _graphicsAPILabel.text = SystemInfo.graphicsDeviceType.ToString();

            // GPU info
            _gpuLabel.text = SystemInfo.graphicsDeviceName;

            // VRAM (approximate)
            int vramMB = SystemInfo.graphicsMemorySize;
            _vramLabel.text = vramMB > 0 ? $"{vramMB} MB" : "Unknown";

            // CPU info
            _cpuLabel.text = $"{SystemInfo.processorType} ({SystemInfo.processorCount} cores)";

            // OS info
            _osLabel.text = $"{SystemInfo.operatingSystem} ({SystemInfo.operatingSystemFamily})";
        }
    }
}
