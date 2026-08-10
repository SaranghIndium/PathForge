using SerapKeremGameKit._Camera;
using SerapKeremGameKit._InputSystem;
using SerapKeremGameKit._Logging;
using SerapKeremGameKit._Managers;
using SerapKeremGameKit._UI;
using System.Collections;
using TriInspector;
using UnityEngine;
using Array2DEditor;
using _Game.Line;
using _Game.UI;
using System.Collections.Generic;



namespace SerapKeremGameKit._LevelSystem
{
    public class Level : MonoBehaviour
    {

        [Title("Grid Settings"), PropertyOrder(2)]
        [SerializeField] private Array2DInt _tileSizeArray;

        [Title("Time Settings")]
        [SerializeField, Min(0f)] private float _levelTime = 120f;
        public float LevelTime => _levelTime;

        [ReadOnly]
        [SerializeField] private bool _isLevelWon;

        [Title("Money Settings")]
        [SerializeField] private long _money = 10;
        public long Money => _money;


        private Coroutine _winCoroutine;
        private Coroutine _loseCoroutine;

        [SerializeField] private LineManager _lineManager;
        public LineManager LineManager { get => _lineManager; set => _lineManager = value; }

        [SerializeField] private Transform _linesParent;

        [Title("Procedural Progression")]
        [SerializeField] private bool _useProceduralProgression = true;
        [SerializeField, Min(1)] private int _simpleLevelCount = 5;
        [SerializeField] private Vector2 _playAreaSize = new Vector2(10f, 8f);
        [SerializeField] private float _smallSquareSize = 1.2f;
        [SerializeField] private float _gridStep = 0.8f;

        public virtual void Load()
        {
            gameObject.SetActive(true);
            _isLevelWon = false;
            if (_winCoroutine != null) { StopCoroutine(_winCoroutine); _winCoroutine = null; }
            if (_loseCoroutine != null) { StopCoroutine(_loseCoroutine); _loseCoroutine = null; }
            
            UnsubscribeFromEvents();
            Initialize();
        }

        private void Initialize()
        {
            InitializeCamera();
            InitializeLines();
        }

        private void InitializeLines()
        {
            if (_useProceduralProgression)
            {
                BuildProgressiveLineLayout();
            }

            if (_lineManager != null)
            {
                _lineManager.InitializeLines(transform);
            }
            else
            {
                TraceLogger.LogWarning("LineManager is not initialized. Lines will not be initialized.", this);
            }
        }

        private void BuildProgressiveLineLayout()
        {
            if (_linesParent == null)
            {
                TraceLogger.LogWarning("Lines parent is not assigned. Cannot build procedural progression.", this);
                return;
            }

            Line templateLine = _linesParent.GetComponentInChildren<Line>(true);
            if (templateLine == null)
            {
                GameObject templatePrefab = Resources.Load<GameObject>("Line/Line (1)");
                if (templatePrefab != null)
                {
                    GameObject created = Instantiate(templatePrefab, _linesParent);
                    templateLine = created.GetComponent<Line>();
                }
            }

            if (templateLine == null)
            {
                TraceLogger.LogWarning("No line template found for procedural generation.", this);
                return;
            }

            int designedLevel = LevelManager.Instance != null
                ? LevelManager.Instance.CurrentDesignedLevelNumber
                : 1;

            bool isSimple = designedLevel <= _simpleLevelCount;
            int difficulty = Mathf.Max(0, designedLevel - _simpleLevelCount);

            int lineCount = isSimple
                ? Mathf.Clamp(1 + (designedLevel - 1) / 2, 1, 3)
                : Mathf.Clamp(2 + difficulty / 4, 3, 8);

            EnsureLineInstanceCount(templateLine, lineCount);

            Line[] lines = _linesParent.GetComponentsInChildren<Line>(true);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == null) continue;

                LineRenderer lineRenderer = lines[i].GetComponent<LineRenderer>();
                if (lineRenderer == null) continue;

                Vector3 center = ComputeCenterForIndex(i, lines.Length);
                Vector3[] points = isSimple
                    ? GenerateSimpleLineOrSmallSquare(i, designedLevel, center)
                    : GenerateProgressivePath(i, designedLevel, difficulty, center);

                if (points == null || points.Length < 2)
                {
                    points = new[] { center + Vector3.left, center + Vector3.right };
                }

                lineRenderer.useWorldSpace = false;
                lineRenderer.positionCount = points.Length;
                lineRenderer.SetPositions(points);

                lines[i].gameObject.name = $"Line_{designedLevel}_{i + 1}";
                lines[i].gameObject.SetActive(true);
            }
        }

        private void EnsureLineInstanceCount(Line templateLine, int targetCount)
        {
            if (templateLine == null) return;

            List<Line> existing = new List<Line>(_linesParent.GetComponentsInChildren<Line>(true));
            if (existing.Count == 0)
            {
                existing.Add(templateLine);
            }

            Line first = existing[0];
            first.gameObject.SetActive(true);

            while (existing.Count < targetCount)
            {
                GameObject clone = Instantiate(first.gameObject, _linesParent);
                clone.SetActive(true);
                Line cloneLine = clone.GetComponent<Line>();
                if (cloneLine != null)
                {
                    existing.Add(cloneLine);
                }
                else
                {
                    break;
                }
            }

            for (int i = targetCount; i < existing.Count; i++)
            {
                if (existing[i] == null) continue;
                Destroy(existing[i].gameObject);
            }
        }

        private Vector3 ComputeCenterForIndex(int index, int totalCount)
        {
            if (totalCount <= 1)
            {
                return Vector3.zero;
            }

            int columns = Mathf.CeilToInt(Mathf.Sqrt(totalCount));
            int rows = Mathf.CeilToInt((float)totalCount / columns);

            int col = index % columns;
            int row = index / columns;

            float xSpacing = _playAreaSize.x / Mathf.Max(1, columns);
            float ySpacing = _playAreaSize.y / Mathf.Max(1, rows);

            float x = -_playAreaSize.x * 0.5f + xSpacing * (col + 0.5f);
            float y = _playAreaSize.y * 0.5f - ySpacing * (row + 0.5f);

            return new Vector3(x, y, 0f);
        }

        private Vector3[] GenerateSimpleLineOrSmallSquare(int lineIndex, int levelNumber, Vector3 center)
        {
            bool makeSquare = ((lineIndex + levelNumber) % 2 == 0);

            if (!makeSquare)
            {
                float length = Mathf.Lerp(1.6f, 2.6f, (levelNumber - 1) / Mathf.Max(1f, (float)_simpleLevelCount - 1f));
                Vector3 start = center + new Vector3(-length * 0.5f, 0f, 0f);
                Vector3 end = center + new Vector3(length * 0.5f, 0f, 0f);
                return new[] { start, end };
            }

            float size = _smallSquareSize + 0.12f * (levelNumber - 1);
            Vector3 bl = center + new Vector3(-size * 0.5f, -size * 0.5f, 0f);
            Vector3 br = center + new Vector3(size * 0.5f, -size * 0.5f, 0f);
            Vector3 tr = center + new Vector3(size * 0.5f, size * 0.5f, 0f);
            Vector3 tl = center + new Vector3(-size * 0.5f, size * 0.5f, 0f);

            return new[] { bl, br, tr, tl, bl };
        }

        private Vector3[] GenerateProgressivePath(int lineIndex, int designedLevel, int difficulty, Vector3 center)
        {
            int pointCount = Mathf.Clamp(4 + difficulty / 2 + (lineIndex % 3), 5, 18);
            float extentX = Mathf.Lerp(1.8f, _playAreaSize.x * 0.42f, Mathf.Clamp01(difficulty / 35f));
            float extentY = Mathf.Lerp(1.4f, _playAreaSize.y * 0.42f, Mathf.Clamp01(difficulty / 35f));

            int seed = designedLevel * 97 + lineIndex * 31;
            Random.State cached = Random.state;
            Random.InitState(seed);

            Vector3[] points = new Vector3[pointCount];
            Vector3 current = center + new Vector3(
                Random.Range(-extentX * 0.35f, extentX * 0.35f),
                Random.Range(-extentY * 0.35f, extentY * 0.35f),
                0f
            );

            points[0] = SnapToGrid(current);

            for (int i = 1; i < pointCount; i++)
            {
                bool diagonal = difficulty > 8 && Random.value > 0.55f;
                int dx = diagonal ? Random.Range(-1, 2) : (Random.value > 0.5f ? 1 : -1);
                int dy = diagonal ? Random.Range(-1, 2) : (Random.value > 0.5f ? 1 : -1);

                if (!diagonal)
                {
                    if (Random.value > 0.5f) dy = 0;
                    else dx = 0;
                }

                Vector3 next = current + new Vector3(dx * _gridStep, dy * _gridStep, 0f);
                next.x = Mathf.Clamp(next.x, center.x - extentX, center.x + extentX);
                next.y = Mathf.Clamp(next.y, center.y - extentY, center.y + extentY);

                if (Vector3.Distance(next, current) < 0.15f)
                {
                    next.x += _gridStep;
                }

                current = next;
                points[i] = SnapToGrid(current);
            }

            if (difficulty > 20 && points.Length > 6)
            {
                points[points.Length - 1] = points[0] + new Vector3(_gridStep * 0.5f, 0f, 0f);
            }

            Random.state = cached;
            return points;
        }

        private Vector3 SnapToGrid(Vector3 point)
        {
            float x = Mathf.Round(point.x / _gridStep) * _gridStep;
            float y = Mathf.Round(point.y / _gridStep) * _gridStep;
            return new Vector3(x, y, 0f);
        }

        private void InitializeCamera()
        {
            if (CameraManager.Instance == null)
            {
                TraceLogger.LogError("CameraManager.Instance is null! Cannot initialize camera position.", this);
                return;
            }

            if (_linesParent == null)
            {
                TraceLogger.LogWarning("Lines parent is not assigned in Inspector. Camera will not be fitted to lines.", this);
                return;
            }

            CameraManager.Instance.FitCameraToLines(_linesParent);
        }

        public virtual void Play()
        {
            if (InputHandler.Instance != null)
            {
                InputHandler.Instance.UnlockInput();
            }

            _isLevelWon = false;
            
            if (_winCoroutine != null) 
            { 
                StopCoroutine(_winCoroutine); 
                _winCoroutine = null; 
            }
            
            if (_loseCoroutine != null) 
            { 
                StopCoroutine(_loseCoroutine); 
                _loseCoroutine = null; 
            }

            UnsubscribeFromEvents();

            SubscribeToLivesManager();

            if (_lineManager != null)
            {
                _lineManager.OnAllLinesRemoved += HandleAllLinesRemoved;
            }

            InitializeHUD();
        }

        private void InitializeHUD()
        {
            UIRootController uiRoot = FindFirstObjectByType<UIRootController>();
            if (uiRoot != null)
            {
                uiRoot.InitializeHUD();
            }
        }

        private void SubscribeToLivesManager()
        {
            if (LivesManager.IsInitialized && LivesManager.Instance != null)
            {
                LivesManager.Instance.Initialize();
                LivesManager.Instance.OnLivesDepleted += HandleLivesDepleted;
                
                if (LivesManager.Instance.CurrentLives <= 0)
                {
                    HandleLivesDepleted();
                }
            }
            else
            {
                StartCoroutine(SubscribeToLivesManagerCoroutine());
            }
        }

        private IEnumerator SubscribeToLivesManagerCoroutine()
        {
            int maxAttempts = 10;
            int attempts = 0;
            
            while (attempts < maxAttempts)
            {
                if (LivesManager.IsInitialized && LivesManager.Instance != null)
                {
                    LivesManager.Instance.Initialize();
                    LivesManager.Instance.OnLivesDepleted += HandleLivesDepleted;
                    
                    if (LivesManager.Instance.CurrentLives <= 0)
                    {
                        HandleLivesDepleted();
                    }
                    
                    yield break;
                }
                
                yield return null;
                attempts++;
            }

            TraceLogger.LogWarning("LivesManager is not initialized after multiple attempts. Fail condition may not work.", this);
        }


        private void HandleLivesDepleted()
        {
            if (_loseCoroutine != null) return;
            CheckLoseCondition();
        }

        private void HandleAllLinesRemoved()
        {
            CheckWinCondition();
        }

        public void CheckWinCondition()
        {
            if (_isLevelWon) return;

            _isLevelWon = true;
            _winCoroutine = StartCoroutine(WinCoroutine());
        }

        private IEnumerator WinCoroutine()
        {
            if (InputHandler.Instance != null) InputHandler.Instance.LockInput();
            yield return new WaitForSeconds(0.5f);
            LevelManager.Instance.Win();
        }

        public void CheckLoseCondition()
        {
            if (_loseCoroutine != null) return;

            _loseCoroutine = StartCoroutine(LoseCoroutine());
        }

        private IEnumerator LoseCoroutine()
        {
            if (InputHandler.Instance != null) InputHandler.Instance.LockInput();
            yield return new WaitForSeconds(0.5f);

            LevelManager.Instance.Lose();
        }

        private void UnsubscribeFromEvents()
        {
            if (LivesManager.IsInitialized && LivesManager.Instance != null)
            {
                LivesManager.Instance.OnLivesDepleted -= HandleLivesDepleted;
            }

            if (_lineManager != null)
            {
                _lineManager.OnAllLinesRemoved -= HandleAllLinesRemoved;
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
    }
}