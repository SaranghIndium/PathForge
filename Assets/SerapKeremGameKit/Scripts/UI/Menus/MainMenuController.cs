using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SerapKeremGameKit._UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("Routing")]
        [SerializeField] private string _gameSceneName = "GameScene";

        [Header("References")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _howToPlayButton;
        [SerializeField] private GameObject _howToPlayPanel;
        [SerializeField] private Button _howToPlayCloseButton;
        [SerializeField] private Button _quitButton;

        private void Awake()
        {
            if (_playButton != null) _playButton.onClick.AddListener(OnPlayClicked);
            if (_howToPlayButton != null) _howToPlayButton.onClick.AddListener(OnHowToPlayClicked);
            if (_howToPlayCloseButton != null) _howToPlayCloseButton.onClick.AddListener(OnCloseHowToPlayClicked);
            if (_quitButton != null) _quitButton.onClick.AddListener(OnQuitClicked);

            if (_howToPlayPanel != null)
            {
                _howToPlayPanel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_playButton != null) _playButton.onClick.RemoveListener(OnPlayClicked);
            if (_howToPlayButton != null) _howToPlayButton.onClick.RemoveListener(OnHowToPlayClicked);
            if (_quitButton != null) _quitButton.onClick.RemoveListener(OnQuitClicked);
        }

        private void OnPlayClicked()
        {
            if (string.IsNullOrEmpty(_gameSceneName))
            {
                Debug.LogWarning("MainMenuController: Game scene name is not set.", this);
                return;
            }

            SceneManager.LoadScene(_gameSceneName);
        }

        private void OnHowToPlayClicked()
        {
            if (_howToPlayPanel == null)
            {
                Debug.LogWarning("MainMenuController: How To Play panel is not assigned.", this);
                return;
            }

            _howToPlayPanel.SetActive(!_howToPlayPanel.activeSelf);
        }

        private void OnCloseHowToPlayClicked()
        {
            if (_howToPlayPanel == null)
            {
                Debug.LogWarning("MainMenuController: How To Play panel is not assigned.", this);
                return;
            }

            _howToPlayPanel.SetActive(false);
        }

        private void OnQuitClicked()
        {
            Application.Quit();
        }

        public void ShowHowToPlayPanel()
        {
            if (_howToPlayPanel != null)
                _howToPlayPanel.SetActive(true);
        }

        public void HideHowToPlayPanel()
        {
            if (_howToPlayPanel != null)
                _howToPlayPanel.SetActive(false);
        }
    }
}
