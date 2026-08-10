using UnityEngine;
using UnityEngine.UI;

namespace _Game.UI
{
    public class HeartUI : MonoBehaviour
    {
        [Header("Heart Sprites")]
        [SerializeField] private Sprite _redHeartSprite;
        [SerializeField] private Sprite _grayHeartSprite;

        [Header("Reskin Colors")]
        [SerializeField] private Color _activeColor = new Color(1f, 0.43f, 0.33f, 1f);
        [SerializeField] private Color _inactiveColor = new Color(0.36f, 0.38f, 0.46f, 0.9f);
        [SerializeField] private float _activeScale = 1f;
        [SerializeField] private float _inactiveScale = 0.9f;

        [Header("Image Component")]
        private Image _heartImage;
        private RectTransform _rectTransform;

        private bool _isActive = true;
        private bool _isInitialized = false;

        public void SetActive(bool active)
        {
            _isActive = active;

            if (_heartImage == null) return;
            
            _heartImage.color = active ? _activeColor : _inactiveColor;

            if (active && _redHeartSprite != null)
            {
                _heartImage.sprite = _redHeartSprite;
            }
            else if (!active && _grayHeartSprite != null)
            {
                _heartImage.sprite = _grayHeartSprite;
            }

            if (_rectTransform != null)
            {
                float targetScale = active ? _activeScale : _inactiveScale;
                _rectTransform.localScale = new Vector3(targetScale, targetScale, 1f);
            }
        }

        public void Initialize()
        {
            if (_isInitialized) return;

            _heartImage = gameObject.GetComponent<Image>();
            _rectTransform = gameObject.GetComponent<RectTransform>();

            if (_heartImage == null)
            {
                Debug.LogWarning($"{name}: Image component is not found. Please assign it in Inspector.", this);
            }
            else
            {
                _heartImage.preserveAspect = true;
            }

            if (_redHeartSprite == null || _grayHeartSprite == null)
            {
                Debug.LogWarning($"{name}: Red or Gray heart sprite is not assigned in Inspector.", this);
            }

            SetActive(true);
            _isInitialized = true;
        }

    }
}
