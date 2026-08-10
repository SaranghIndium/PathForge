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
            ApplyUIReskin();
        }

        private void ApplyCameraReskin()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;

            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = _cameraBackground;
        }

        private void ApplyUIReskin()
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < canvases.Length; i++)
            {
                RestyleCanvas(canvases[i]);
            }
        }

        private void RestyleCanvas(Canvas canvas)
        {
            if (canvas == null) return;

            Image[] images = canvas.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                RestyleImage(images[i]);
            }

            Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                RestyleButton(buttons[i]);
            }

            TextMeshProUGUI[] textElements = canvas.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < textElements.Length; i++)
            {
                RestyleText(textElements[i]);
            }
        }

        private void RestyleImage(Image image)
        {
            if (image == null) return;
            if (image.GetComponent<HeartUI>() != null) return;

            string objectName = image.gameObject.name;
            bool isPanelLike = objectName.Contains("Panel") || objectName.Contains("Container") || objectName.Contains("Background");
            if (isPanelLike)
            {
                image.color = _uiPanelColor;
            }
        }

        private void RestyleButton(Button button)
        {
            if (button == null) return;

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = _buttonColor;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = _buttonColor;
            colors.highlightedColor = _buttonHighlightColor;
            colors.selectedColor = _buttonHighlightColor;
            colors.pressedColor = Color.Lerp(_buttonColor, Color.black, 0.2f);
            colors.disabledColor = new Color(0.3f, 0.35f, 0.45f, 0.8f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }

        private void RestyleText(TextMeshProUGUI text)
        {
            if (text == null) return;

            text.color = _uiTextColor;
            text.fontStyle = FontStyles.Bold;
        }
    }
}
