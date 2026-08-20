using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SerapKeremGameKit._LevelSystem;
using SerapKeremGameKit._Managers;

namespace SerapKeremGameKit._UI
{
	public sealed class WinPanel : UIPanel
    {
        [Header("Stars")]
        [SerializeField] private GameObject _star1;
        [SerializeField] private GameObject _star2;
        [SerializeField] private GameObject _star3;
        [SerializeField] private GameObject _starZeroIcon; // optional icon for 0 stars

        [Header("Coin")]
        [SerializeField] private TextMeshProUGUI _coinText;
        [SerializeField] private TextMeshProUGUI _totalCoinText;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _mainMenuButton;
        [SerializeField] private UIRootController _uiRoot;
        [SerializeField] private CoinFlyAnimator _coinFly;
        [SerializeField] private RectTransform _totalCoinTarget;

        private int _pendingReward;
        private Sequence _pendingSequence;

        private void Awake()
        {
			if (_nextButton != null) _nextButton.BindOnClick(this, OnNextClicked);
			if (_retryButton != null) _retryButton.BindOnClick(this, OnRetryClicked);
			if (_mainMenuButton != null) _mainMenuButton.BindOnClick(this, OnMainMenuClicked);
        }

		protected override void OnDestroy()
		{
			base.OnDestroy();
			// Listener auto-unsubscribed by ButtonExtensions binding component
			if (_pendingSequence != null && _pendingSequence.IsActive())
			{
				_pendingSequence.Kill();
				_pendingSequence = null;
			}
		}

        public void Setup(int stars, int rewardedCoins, int totalCoins, UIRootController uiRoot)
        {
            SetStars(stars);
            if (_coinText != null) _coinText.text = rewardedCoins.ToString();
            if (_totalCoinText != null) _totalCoinText.text = Mathf.Max(0, totalCoins).ToString();
            _uiRoot = uiRoot;
            _pendingReward = rewardedCoins;
            if (_nextButton != null)
            {
                bool isFinalLevel = LevelManager.Instance != null &&
                                     LevelManager.Instance.GameplayLevelCount > 0 &&
                                     LevelManager.Instance.ActiveLevelNumber >= LevelManager.Instance.GameplayLevelCount;
                _nextButton.gameObject.SetActive(!isFinalLevel);
            }

            // Do not start animation here; start it on Next button
        }

        private void OnTotalCoinStep(long value) { }

        private void SetStars(int count)
        {
            if (_star1 != null) _star1.SetActive(count >= 1);
            if (_star2 != null) _star2.SetActive(count >= 2);
            if (_star3 != null) _star3.SetActive(count >= 3);
            if (_starZeroIcon != null) _starZeroIcon.SetActive(count == 0);
        }

        private void OnNextClicked()
        {
            if (_pendingSequence != null && _pendingSequence.IsActive()) return;
            if (_coinFly != null && _totalCoinTarget != null && _coinText != null && _totalCoinText != null)
            {
                long startAmount = 0;
                long.TryParse(_totalCoinText.text, out startAmount);
				_pendingSequence = _coinFly.AnimateAdd(_totalCoinText, _coinText.rectTransform, _totalCoinTarget, startAmount, _pendingReward);
                if (_pendingSequence != null)
                {
					_pendingSequence.SetAutoKill(true).SetLink(gameObject, LinkBehaviour.KillOnDestroy).OnComplete(() =>
                    {
						if (_uiRoot != null) _uiRoot.ProceedNextLevelAfterReward(_pendingReward);
                    });
                    return;
                }
            }
            if (_uiRoot != null) _uiRoot.ProceedNextLevelAfterReward(_pendingReward);
        }

        private void OnRetryClicked()
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