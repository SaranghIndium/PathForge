using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SerapKeremGameKit._UI;

namespace _Game.UI
{
    public class HeartPanel : MonoBehaviour
    {
        [Header("Heart References")]
        [SerializeField] private List<HeartUI> _hearts = new List<HeartUI>();

        [Header("Reskin Layout")]
        [SerializeField] private bool _removeBackgroundImage = true;
        [SerializeField] private float _heartSpacing = 12f;
        [SerializeField] private Vector2 _heartSize = new Vector2(30f, 30f);

        private bool _isInitialized = false;

        private int MaxHearts
        {
            get
            {
                if (LivesManager.IsInitialized && LivesManager.Instance != null)
                {
                    return LivesManager.Instance.MaxLivesCount;
                }
                return 5; // Fallback default
            }
        }

        public void Initialize()
        {
            if (_isInitialized) return;

            CleanupBackgroundAndLayout();

            int expectedHearts = MaxHearts;
            if (_hearts.Count != expectedHearts)
            {
                Debug.LogWarning($"{name}: Expected {expectedHearts} hearts, but found {_hearts.Count}. Please assign {expectedHearts} HeartUI components in Inspector.", this);
            }

            foreach (var heart in _hearts)
            {
                if (heart != null)
                {
                    heart.Initialize();
                }
            }

            _isInitialized = true;
        }

        private void CleanupBackgroundAndLayout()
        {
            if (_removeBackgroundImage)
            {
                Image backgroundImage = GetComponent<Image>();
                if (backgroundImage != null)
                {
                    backgroundImage.enabled = false;
                }
            }

            HorizontalLayoutGroup layout = GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = _heartSpacing;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            foreach (HeartUI heart in _hearts)
            {
                if (heart == null) continue;

                RectTransform heartRect = heart.GetComponent<RectTransform>();
                if (heartRect != null)
                {
                    heartRect.anchorMin = new Vector2(0.5f, 0.5f);
                    heartRect.anchorMax = new Vector2(0.5f, 0.5f);
                    heartRect.pivot = new Vector2(0.5f, 0.5f);
                    heartRect.sizeDelta = _heartSize;
                }

                LayoutElement layoutElement = heart.GetComponent<LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = heart.gameObject.AddComponent<LayoutElement>();
                }

                layoutElement.preferredWidth = _heartSize.x;
                layoutElement.preferredHeight = _heartSize.y;
                layoutElement.flexibleWidth = 0f;
                layoutElement.flexibleHeight = 0f;
            }
        }

        public void UpdateHearts(int activeLives)
        {
            for (int i = 0; i < _hearts.Count; i++)
            {
                if (_hearts[i] != null)
                {
                    bool isActive = i < activeLives;
                    _hearts[i].SetActive(isActive);
                }
            }
        }

        public void ResetHearts()
        {
            UpdateHearts(MaxHearts);
        }
    }
}
