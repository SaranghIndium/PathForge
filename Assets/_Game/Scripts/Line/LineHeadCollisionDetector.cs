using System;
using UnityEngine;

namespace _Game.Line
{
    [RequireComponent(typeof(Collider2D))]
    public class LineHeadCollisionDetector : MonoBehaviour
    {
        public event Action<Collider2D> OnHeadCollision;
        
        private Line _ownLine;
        private bool _isInitialized;
        private bool _hasCollided = false;

        public void Initialize(Line ownLine)
        {
            _ownLine = ownLine;
            _isInitialized = true;
            _hasCollided = false;
            
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isInitialized || _ownLine == null || _hasCollided) return;
            CheckCollision(other);
        }

        public void CheckSweptCollision(Vector2 start, Vector2 end, float radius)
        {
            if (!_isInitialized || _ownLine == null || _hasCollided) return;

            Vector2 movement = end - start;
            float distance = movement.magnitude;
            if (distance > 0.0001f)
            {
                RaycastHit2D[] hits = Physics2D.CircleCastAll(start, radius, movement / distance, distance);
                foreach (RaycastHit2D hit in hits)
                {
                    CheckCollision(hit.collider);
                    if (_hasCollided) return;
                }
            }

            Collider2D[] overlaps = Physics2D.OverlapCircleAll(end, radius);
            foreach (Collider2D overlap in overlaps)
            {
                CheckCollision(overlap);
                if (_hasCollided) return;
            }
        }

        private void CheckCollision(Collider2D other)
        {
            if (other == null || _hasCollided) return;

            Line otherLine = GetLineFromCollider(other);
            if (otherLine == null || otherLine == _ownLine)
            {
                return;
            }

            _hasCollided = true;
            OnHeadCollision?.Invoke(other);
        }

        private static Line GetLineFromCollider(Collider2D collider)
        {
            if (collider == null) return null;

            Line line = collider.GetComponent<Line>();
            if (line == null && collider.transform.parent != null)
            {
                line = collider.transform.parent.GetComponent<Line>();
            }
            return line;
        }

        public void ResetCollision()
        {
            _hasCollided = false;
        }
    }
}
