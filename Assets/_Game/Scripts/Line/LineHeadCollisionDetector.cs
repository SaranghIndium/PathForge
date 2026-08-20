using System;
using UnityEngine;

namespace _Game.Line
{
    [RequireComponent(typeof(Collider2D))]
    public class LineHeadCollisionDetector : MonoBehaviour
    {
        public event Action<Collider2D> OnHeadCollision;
        
        private Line _ownLine;
        private Collider2D _headCollider;
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
                _headCollider = col;
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isInitialized || _ownLine == null || _hasCollided) return;
            CheckCollision(other);
        }

        public void CheckSweptCollision(Vector2 start, Vector2 end, Collider2D headCollider)
        {
            if (!_isInitialized || _ownLine == null || _hasCollided || headCollider == null) return;

            Vector2 movement = end - start;
            float distance = movement.magnitude;
            if (distance > 0.0001f)
            {
                ContactFilter2D filter = new ContactFilter2D();
                filter.useTriggers = true;
                RaycastHit2D[] hits = new RaycastHit2D[32];
                int hitCount = headCollider.Cast(-movement / distance, filter, hits, distance);
                for (int i = 0; i < hitCount; i++)
                {
                    CheckCollision(hits[i].collider);
                    if (_hasCollided) return;
                }

                float sweepRadius = headCollider.bounds.extents.magnitude;
                RaycastHit2D[] broadPhaseHits = Physics2D.CircleCastAll(
                    start,
                    sweepRadius,
                    movement / distance,
                    distance);
                foreach (RaycastHit2D hit in broadPhaseHits)
                {
                    CheckCollision(hit.collider);
                    if (_hasCollided) return;
                }
            }

            ContactFilter2D overlapFilter = new ContactFilter2D();
            overlapFilter.useTriggers = true;
            Collider2D[] overlaps = new Collider2D[32];
            int overlapCount = headCollider.Overlap(overlapFilter, overlaps);
            for (int i = 0; i < overlapCount; i++)
            {
                CheckCollision(overlaps[i]);
                if (_hasCollided) return;
            }

            float overlapRadius = headCollider.bounds.extents.magnitude;
            Collider2D[] broadPhaseOverlaps = Physics2D.OverlapCircleAll(end, overlapRadius);
            foreach (Collider2D overlap in broadPhaseOverlaps)
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

            return collider.GetComponentInParent<Line>();
        }

        public void ResetCollision()
        {
            _hasCollided = false;
        }
    }
}
