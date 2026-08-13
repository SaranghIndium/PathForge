using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _Game.UI
{
    /// <summary>
    /// Applies a broad runtime reskin pass so existing scenes get a cleaner visual style
    /// without manual prefab/scene setup.
    /// </summary>
    public sealed class RuntimeReskinInstaller : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField] private Color _cameraBackground = new Color(0.05f, 0.08f, 0.12f, 1f);

        [Header("UI Theme")]
        [SerializeField] private Color _uiPanelColor = new Color(0.12f, 0.16f, 0.23f, 0.9f);
        [SerializeField] private Color _uiTextColor = new Color(0.92f, 0.96f, 1f, 1f);
        [SerializeField] private Color _buttonColor = new Color(0.12f, 0.62f, 0.82f, 1f);
        [SerializeField] private Color _buttonHighlightColor = new Color(0.2f, 0.78f, 0.96f, 1f);

        private static RuntimeReskinInstaller _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;

            GameObject root = new GameObject("RuntimeReskinInstaller");
            _instance = root.AddComponent<RuntimeReskinInstaller>();
            DontDestroyOnLoad(root);
            _instance.ApplySceneReskin(SceneManager.GetActiveScene());
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplySceneReskin(scene);
        }

        private void ApplySceneReskin(Scene scene)
        {
            if (!scene.IsValid()) return;

            ApplyCameraReskin();
        }

        private void ApplyCameraReskin()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;

            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = _cameraBackground;
        }

    }
}
