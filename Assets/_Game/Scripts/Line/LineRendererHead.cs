using System;
using UnityEngine;

namespace _Game.Line
{
    [ExecuteAlways]
    public class LineRendererHead : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LineRenderer _lineRenderer;
        [SerializeField] private float _rotationOffset = 0f;

        private LineHeadCollisionDetector _collisionDetector;
        private Line _ownLine;
        private Collider2D _headCollider;
        private bool _isInitialized;
        private Vector2 _previousWorldPosition;

        public event Action<Collider2D> OnHeadCollision;
        public bool IsInitialized => _isInitialized;
        public float RotationOffset => _rotationOffset;

        public void Initialize(LineRenderer lineRenderer, Line ownLine = null)
        {
            if (_isInitialized) return;

            _lineRenderer = lineRenderer;
            _ownLine = ownLine;

            if (_lineRenderer == null)
            {
                enabled = false;
                return;
            }

            EnsureActive();
            SetupPhysicsComponents();
            SetupCollisionDetector();

            _isInitialized = true;
            _previousWorldPosition = transform.position;
            enabled = true;
        }

        private void EnsureActive()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }

        private void SetupPhysicsComponents()
        {
            Rigidbody2D rigidbody = GetComponent<Rigidbody2D>();
            if (rigidbody == null)
            {
                rigidbody = gameObject.AddComponent<Rigidbody2D>();
                rigidbody.bodyType = RigidbodyType2D.Kinematic;
                rigidbody.gravityScale = 0f;
            }

            _headCollider = GetComponent<Collider2D>();
            if (_headCollider == null)
            {
                CircleCollider2D fallbackCollider = gameObject.AddComponent<CircleCollider2D>();
                fallbackCollider.radius = 0.3f;
                _headCollider = fallbackCollider;
            }

            PolygonCollider2D polygonCollider = _headCollider as PolygonCollider2D;
            if (polygonCollider != null && polygonCollider.pathCount == 0)
            {
                polygonCollider.SetPath(0, new[]
                {
                    new Vector2(-0.28f, -0.2f),
                    new Vector2(0.28f, -0.2f),
                    new Vector2(0f, 0.28f)
                });
            }

            _headCollider.isTrigger = true;
        }

        private void SetupCollisionDetector()
        {
            _collisionDetector = GetComponent<LineHeadCollisionDetector>();
            if (_collisionDetector == null)
            {
                _collisionDetector = gameObject.AddComponent<LineHeadCollisionDetector>();
            }

            if (_collisionDetector != null && _ownLine != null)
            {
                _collisionDetector.Initialize(_ownLine);
                _collisionDetector.OnHeadCollision += HandleHeadCollision;
            }
        }

        private void HandleHeadCollision(Collider2D other)
        {
            OnHeadCollision?.Invoke(other);
        }

        private void LateUpdate()
        {
            if (_lineRenderer == null || _lineRenderer.positionCount < 2)
            {
                return;
            }

            int lastIndex = _lineRenderer.positionCount - 1;
            Vector3 endPosition = _lineRenderer.GetPosition(lastIndex);
            Vector3 previousPosition = _lineRenderer.GetPosition(lastIndex - 1);

            transform.localPosition = endPosition;

            Vector3 direction = (endPosition - previousPosition).normalized;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle + _rotationOffset);

            Physics2D.SyncTransforms();
            if (_collisionDetector != null && _headCollider != null)
            {
                Vector2 currentWorldPosition = transform.position;
                _collisionDetector.CheckSweptCollision(_previousWorldPosition, currentWorldPosition, _headCollider);
                _previousWorldPosition = currentWorldPosition;
            }
        }

        private void OnDestroy()
        {
            if (_collisionDetector != null)
            {
                _collisionDetector.OnHeadCollision -= HandleHeadCollision;
            }
        }
    }
}