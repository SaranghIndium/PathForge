using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SerapKeremGameKit._UI
{
	public sealed class FailPanel : UIPanel
    {
        [SerializeField] private Image _failIcon;
        [SerializeField] private TextMeshProUGUI _coinText;
        [SerializeField] private Button _restartButton;
		[SerializeField] private Button _mainMenuButton;
        [SerializeField] private UIRootController _uiRoot;

		private void Awake()
		{
			if (_restartButton != null) _restartButton.BindOnClick(this, OnRestartClicked);
			if (_mainMenuButton != null) _mainMenuButton.BindOnClick(this, OnMainMenuClicked);
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			// Auto-unsubscribe handled by ButtonExtensions
		}

        public void Setup(int rewardedCoins, UIRootController uiRoot)
        {
            if (_coinText != null) _coinText.text = rewardedCoins.ToString();
            _uiRoot = uiRoot;
        }

        private void OnRestartClicked()
        {
			if (_uiRoot != null) _uiRoot.OnRestartConfirmed();
        }

		private void OnMainMenuClicked()
		{
			if (_uiRoot != null) _uiRoot.OnMainMenuRequested();
		}

		public void SetUIRoot(UIRootController uiRoot)
		{
			_uiRoot = uiRoot;
		}
    }
}



