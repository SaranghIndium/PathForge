using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SerapKeremGameKit._UI
{
    public sealed class PausePanel : UIPanel
    {
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _mainMenuButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private UIRootController _uiRoot;

        private void Awake()
        {
            if (_resumeButton != null) _resumeButton.BindOnClick(this, OnResumeClicked);
            if (_settingsButton != null) _settingsButton.BindOnClick(this, OnSettingsClicked);
            if (_restartButton != null) _restartButton.BindOnClick(this, OnRestartClicked);
            if (_mainMenuButton != null) _mainMenuButton.BindOnClick(this, OnMainMenuClicked);
            if (_closeButton != null) _closeButton.BindOnClick(this, OnCloseClicked);
        }

        private void OnResumeClicked()
        {
            if (_uiRoot != null) _uiRoot.OnResumeRequested();
        }

        private void OnSettingsClicked()
        {
            if (_uiRoot != null) _uiRoot.OnOpenSettings();
        }

        private void OnRestartClicked()
        {
            if (_uiRoot != null) _uiRoot.OnRestartRequested();
        }

        private void OnMainMenuClicked()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenuScene");
        }

        private void OnCloseClicked()
        {
            if (_uiRoot != null)
            {
                _uiRoot.OnResumeRequested();
                return;
            }

            Time.timeScale = 1f;
            Hide();
        }

        public void SetUIRoot(UIRootController uiRoot)
        {
            _uiRoot = uiRoot;
        }
    }
}
