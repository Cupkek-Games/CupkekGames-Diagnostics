using UnityEngine;
using UnityEngine.InputSystem;

namespace CupkekGames.Diagnostics
{
    public class MonitorSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject _monitorPrefab;
        [SerializeField] private string _toggleActionName = "ToggleMonitor";

        private GameObject _currentMonitor;
        private bool _isMonitorActive = false;
        private InputAction _toggleAction;

        private void Awake()
        {
            // Get the input action by name from the input system
            _toggleAction = InputSystem.actions.FindAction(_toggleActionName);

            if (_toggleAction == null)
            {
                Debug.LogWarning($"MonitorSpawner: Input action '{_toggleActionName}' not found in Input System!");
            }
        }

        private void OnEnable()
        {
            if (_toggleAction != null)
            {
                // Subscribe to the action's performed event
                _toggleAction.performed += OnTogglePerformed;
            }
        }

        private void OnDisable()
        {
            if (_toggleAction != null)
            {
                // Unsubscribe from the action's performed event
                _toggleAction.performed -= OnTogglePerformed;
            }
        }

        private void OnTogglePerformed(InputAction.CallbackContext context)
        {
            ToggleMonitor();
        }

        private void ToggleMonitor()
        {
            if (_isMonitorActive)
            {
                DespawnMonitor();
            }
            else
            {
                SpawnMonitor();
            }
        }

        private void SpawnMonitor()
        {
            if (_monitorPrefab == null)
            {
                Debug.LogWarning("MonitorSpawner: Monitor prefab is not assigned!");
                return;
            }

            if (_currentMonitor == null)
            {
                _currentMonitor = Instantiate(_monitorPrefab);
                _currentMonitor.name = "Monitor (Runtime)";
                DontDestroyOnLoad(_currentMonitor);
                _isMonitorActive = true;

                Debug.Log("Monitor spawned. Press F3 to toggle.");
            }
        }

        private void DespawnMonitor()
        {
            if (_currentMonitor != null)
            {
                Destroy(_currentMonitor);
                _currentMonitor = null;
                _isMonitorActive = false;

                Debug.Log("Monitor despawned. Press F3 to toggle.");
            }
        }

        private void OnDestroy()
        {
            // Clean up monitor when spawner is destroyed
            if (_currentMonitor != null)
            {
                Destroy(_currentMonitor);
            }
        }
    }
}
