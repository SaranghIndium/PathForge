using System.Collections.Generic;
using System.Linq;
using SerapKeremGameKit._LevelSystem;
using UnityEditor;
using UnityEngine;
using LineComp = _Game.Line.Line;

namespace _Game.EditorTools
{
    /// <summary>Preset outlines that all arrows in a level can be laid out along.</summary>
    internal enum ShapePreset
    {
        Rectangle,
        Square,
        Diamond,
        Triangle,
        Pentagon,
        Hexagon,
        Circle,
        Star,
        Cross,
        LShape,
        UShape,
        ZShape,
        Stairs,
        Zigzag,
        StraightLine
    }

    /// <summary>
    /// Scene-view helpers for authoring levels by hand: direction gizmos, numbered labels,
    /// per-line quick actions, and a Level Author window (Tools > PathForge > Level Author).
    /// </summary>
    internal static class LineAuthoringHelpers
    {
        public const string LinePrefabPath = "Assets/_Game/Resources/Line/Line (1).prefab";
        public const string GridSizePrefKey = "PathForge.LevelAuthor.GridSize";

        public static float GridSize
        {
            get => EditorPrefs.GetFloat(GridSizePrefKey, 1f);
            set => EditorPrefs.SetFloat(GridSizePrefKey, Mathf.Max(0.01f, value));
        }

        public static Vector3 SnapVector(Vector3 v, float grid)
        {
            return new Vector3(
                Mathf.Round(v.x / grid) * grid,
                Mathf.Round(v.y / grid) * grid,
                Mathf.Round(v.z / grid) * grid);
        }

        public static void SnapAllPositions(LineRenderer lr, float grid)
        {
            if (lr == null) return;
            int n = lr.positionCount;
            for (int i = 0; i < n; i++)
            {
                lr.SetPosition(i, SnapVector(lr.GetPosition(i), grid));
            }
            EditorUtility.SetDirty(lr);
        }

        public static void ReverseDirection(LineRenderer lr)
        {
            if (lr == null) return;
            int n = lr.positionCount;
            var arr = new Vector3[n];
            for (int i = 0; i < n; i++) arr[i] = lr.GetPosition(n - 1 - i);
            lr.SetPositions(arr);
            EditorUtility.SetDirty(lr);
        }

        public static void AppendPoint(LineRenderer lr, float grid)
        {
            if (lr == null) return;
            int n = lr.positionCount;
            Vector3 last = n > 0 ? lr.GetPosition(n - 1) : Vector3.zero;
            Vector3 dir = n >= 2 ? (last - lr.GetPosition(n - 2)) : Vector3.right;
            if (dir.sqrMagnitude < 0.001f) dir = Vector3.right;
            Vector3 step = dir.normalized * grid;
            lr.positionCount = n + 1;
            lr.SetPosition(n, SnapVector(last + step, grid));
            EditorUtility.SetDirty(lr);
        }

        public static void RemoveLastPoint(LineRenderer lr)
        {
            if (lr == null) return;
            int n = lr.positionCount;
            if (n <= 2) return;
            lr.positionCount = n - 1;
            EditorUtility.SetDirty(lr);
        }

        public static void Rotate90(LineRenderer lr, bool clockwise, float grid)
        {
            if (lr == null) return;
            int n = lr.positionCount;
            Vector3 c = Vector3.zero;
            for (int i = 0; i < n; i++) c += lr.GetPosition(i);
            c /= Mathf.Max(1, n);
            c = SnapVector(c, grid);
            for (int i = 0; i < n; i++)
            {
                Vector3 p = lr.GetPosition(i) - c;
                Vector3 r = clockwise ? new Vector3(p.y, -p.x, p.z) : new Vector3(-p.y, p.x, p.z);
                lr.SetPosition(i, SnapVector(r + c, grid));
            }
            EditorUtility.SetDirty(lr);
        }

        public static void Mirror(LineRenderer lr, bool horizontal, float grid)
        {
            if (lr == null) return;
            int n = lr.positionCount;
            Vector3 c = Vector3.zero;
            for (int i = 0; i < n; i++) c += lr.GetPosition(i);
            c /= Mathf.Max(1, n);
            c = SnapVector(c, grid);
            for (int i = 0; i < n; i++)
            {
                Vector3 p = lr.GetPosition(i) - c;
                Vector3 m = horizontal ? new Vector3(-p.x, p.y, p.z) : new Vector3(p.x, -p.y, p.z);
                lr.SetPosition(i, SnapVector(m + c, grid));
            }
            EditorUtility.SetDirty(lr);
        }

        public static GameObject AddArrowUnder(Transform linesParent, Vector3 localOrigin, float grid)
        {
            if (linesParent == null) return null;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LinePrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[LevelAuthor] Line prefab not found at {LinePrefabPath}");
                return null;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, linesParent);
            Undo.RegisterCreatedObjectUndo(go, "Add Arrow");
            go.name = $"Line ({linesParent.childCount})";
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var lr = go.GetComponent<LineRenderer>();
            if (lr != null)
            {
                lr.useWorldSpace = false;
                lr.positionCount = 2;
                Vector3 a = SnapVector(localOrigin, grid);
                Vector3 b = SnapVector(localOrigin + new Vector3(grid * 2f, 0f, 0f), grid);
                lr.SetPosition(0, a);
                lr.SetPosition(1, b);
                EditorUtility.SetDirty(lr);
            }
            return go;
        }

        public static Transform GetLinesParent(Level level)
        {
            if (level == null) return null;

            var so = new SerializedObject(level);
            var prop = so.FindProperty("_linesParent");
            var parent = prop?.objectReferenceValue as Transform;
            if (parent != null) return parent;

            // Fallback for Levels that use a child object named "LINES".
            parent = level.transform.Find("LINES");
            if (parent != null) return parent;

            return FindDeep(level.transform, "LINES") ?? FindAnyLinesContainer(level.transform);
        }

        public static LineComp[] GetArrows(Level level)
        {
            var parent = GetLinesParent(level);
            if (parent != null) return parent.GetComponentsInChildren<LineComp>(true);
            return level != null ? level.GetComponentsInChildren<LineComp>(true) : System.Array.Empty<LineComp>();
        }

        private static Transform FindDeep(Transform root, string targetName)
        {
            if (root == null) return null;
            if (root.name == targetName) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var child = FindDeep(root.GetChild(i), targetName);
                if (child != null) return child;
            }
            return null;
        }

        private static Transform FindAnyLinesContainer(Transform root)
        {
            if (root == null) return null;
            if (root.GetComponentsInChildren<LineComp>(true).Length > 0)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var container = FindAnyLinesContainer(root.GetChild(i));
                if (container != null) return container;
            }
            return null;
        }

        public static Color ColorForIndex(int index)
        {
            return Color.HSVToRGB(((index * 0.137f) % 1f + 1f) % 1f, 0.75f, 1f);
        }

        public static Vector3[] GetShapePolyline(ShapePreset preset, float size)
        {
            float s = Mathf.Max(0.1f, size);
            switch (preset)
            {
                case ShapePreset.Rectangle:
                    return Closed(new[]
                    {
                        new Vector3(-s, -s * 0.6f, 0f), new Vector3(s, -s * 0.6f, 0f),
                        new Vector3(s, s * 0.6f, 0f), new Vector3(-s, s * 0.6f, 0f)
                    });
                case ShapePreset.Square:
                    return Closed(new[]
                    {
                        new Vector3(-s, -s, 0f), new Vector3(s, -s, 0f),
                        new Vector3(s, s, 0f), new Vector3(-s, s, 0f)
                    });
                case ShapePreset.Diamond:
                    return Closed(new[]
                    {
                        new Vector3(0f, -s, 0f), new Vector3(s, 0f, 0f),
                        new Vector3(0f, s, 0f), new Vector3(-s, 0f, 0f)
                    });
                case ShapePreset.Triangle:
                    return Closed(new[]
                    {
                        new Vector3(-s, -s * 0.6f, 0f), new Vector3(s, -s * 0.6f, 0f),
                        new Vector3(0f, s * 0.9f, 0f)
                    });
                case ShapePreset.Pentagon: return RegularPolygon(5, s);
                case ShapePreset.Hexagon: return RegularPolygon(6, s);
                case ShapePreset.Circle: return RegularPolygon(24, s);
                case ShapePreset.Star: return StarPolygon(5, s, s * 0.42f);
                case ShapePreset.Cross:
                {
                    float t = s * 0.35f;
                    return Closed(new[]
                    {
                        new Vector3(-t, -s, 0f), new Vector3(t, -s, 0f),
                        new Vector3(t, -t, 0f), new Vector3(s, -t, 0f),
                        new Vector3(s, t, 0f), new Vector3(t, t, 0f),
                        new Vector3(t, s, 0f), new Vector3(-t, s, 0f),
                        new Vector3(-t, t, 0f), new Vector3(-s, t, 0f),
                        new Vector3(-s, -t, 0f), new Vector3(-t, -t, 0f)
                    });
                }
                case ShapePreset.LShape:
                    return new[]
                    {
                        new Vector3(-s, s, 0f), new Vector3(-s, -s, 0f), new Vector3(s, -s, 0f)
                    };
                case ShapePreset.UShape:
                    return new[]
                    {
                        new Vector3(-s, s, 0f), new Vector3(-s, -s, 0f),
                        new Vector3(s, -s, 0f), new Vector3(s, s, 0f)
                    };
                case ShapePreset.ZShape:
                    return new[]
                    {
                        new Vector3(-s, s, 0f), new Vector3(s, s, 0f),
                        new Vector3(-s, -s, 0f), new Vector3(s, -s, 0f)
                    };
                case ShapePreset.Stairs:
                {
                    var pts = new List<Vector3>();
                    float step = s * 0.5f;
                    Vector3 p = new Vector3(-s, -s, 0f);
                    pts.Add(p);
                    for (int i = 0; i < 4; i++)
                    {
                        p += new Vector3(step, 0f, 0f); pts.Add(p);
                        p += new Vector3(0f, step, 0f); pts.Add(p);
                    }
                    return pts.ToArray();
                }
                case ShapePreset.Zigzag:
                {
                    var pts = new List<Vector3>();
                    int waves = 5;
                    float step = (s * 2f) / waves;
                    for (int i = 0; i <= waves; i++)
                    {
                        float x = -s + step * i;
                        float y = (i % 2 == 0) ? -s * 0.5f : s * 0.5f;
                        pts.Add(new Vector3(x, y, 0f));
                    }
                    return pts.ToArray();
                }
                case ShapePreset.StraightLine:
                    return new[] { new Vector3(-s, 0f, 0f), new Vector3(s, 0f, 0f) };
            }
            return new[] { Vector3.zero, Vector3.right };
        }

        private static Vector3[] RegularPolygon(int sides, float radius)
        {
            var arr = new Vector3[sides + 1];
            for (int i = 0; i < sides; i++)
            {
                float a = Mathf.PI * 2f * i / sides + Mathf.PI * 0.5f;
                arr[i] = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
            }
            arr[sides] = arr[0];
            return arr;
        }

        private static Vector3[] StarPolygon(int points, float outer, float inner)
        {
            int n = points * 2;
            var arr = new Vector3[n + 1];
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.PI * 2f * i / n + Mathf.PI * 0.5f;
                float r = (i % 2 == 0) ? outer : inner;
                arr[i] = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
            }
            arr[n] = arr[0];
            return arr;
        }

        private static Vector3[] Closed(Vector3[] pts)
        {
            var arr = new Vector3[pts.Length + 1];
            System.Array.Copy(pts, arr, pts.Length);
            arr[pts.Length] = pts[0];
            return arr;
        }

        /// <summary>
        /// Splits a polyline into arrowCount equal-length sub-polylines (each containing every
        /// polygon vertex it crosses so corners are preserved).
        /// </summary>
        public static List<Vector3[]> DistributeAlongPolyline(Vector3[] polyline, int arrowCount, float grid)
        {
            var result = new List<Vector3[]>();
            if (polyline == null || polyline.Length < 2 || arrowCount <= 0) return result;

            float total = 0f;
            for (int i = 0; i < polyline.Length - 1; i++)
                total += Vector3.Distance(polyline[i], polyline[i + 1]);
            if (total < 0.0001f) return result;

            float segLen = total / arrowCount;
            int edgeIdx = 0;
            Vector3 pos = polyline[0];

            for (int a = 0; a < arrowCount; a++)
            {
                var pts = new List<Vector3> { pos };
                float remaining = segLen;

                while (remaining > 0.0001f && edgeIdx < polyline.Length - 1)
                {
                    Vector3 next = polyline[edgeIdx + 1];
                    float dist = Vector3.Distance(pos, next);
                    if (dist <= remaining + 0.0001f)
                    {
                        pos = next;
                        pts.Add(pos);
                        remaining -= dist;
                        edgeIdx++;
                    }
                    else
                    {
                        Vector3 dir = (next - pos).normalized;
                        pos += dir * remaining;
                        pts.Add(pos);
                        remaining = 0f;
                    }
                }

                if (a == arrowCount - 1)
                {
                    Vector3 finalPt = polyline[polyline.Length - 1];
                    if (Vector3.Distance(pts[pts.Count - 1], finalPt) > 0.0001f)
                        pts.Add(finalPt);
                }

                var snapped = new List<Vector3>();
                foreach (var p in pts)
                {
                    var sp = SnapVector(p, grid);
                    if (snapped.Count == 0 || Vector3.Distance(snapped[snapped.Count - 1], sp) > 0.0001f)
                        snapped.Add(sp);
                }
                while (snapped.Count < 2)
                    snapped.Add(snapped[snapped.Count - 1] + Vector3.right * grid);

                result.Add(snapped.ToArray());
            }

            return result;
        }

        public static bool IsClosedPolyline(Vector3[] polyline)
        {
            return polyline != null && polyline.Length >= 3 &&
                   Vector3.Distance(polyline[0], polyline[polyline.Length - 1]) < 0.0001f;
        }

        /// <summary>Shrinks both endpoints of a polyline inward by `gap` (never past neighbor point).</summary>
        public static Vector3[] ShortenStroke(Vector3[] pts, float gap)
        {
            if (pts == null || pts.Length < 2 || gap <= 0f) return pts;
            var arr = (Vector3[])pts.Clone();
            Vector3 dirS = arr[1] - arr[0];
            float dS = dirS.magnitude;
            if (dS > gap * 1.05f)
                arr[0] = arr[0] + dirS / dS * gap;
            int n = arr.Length - 1;
            Vector3 dirE = arr[n] - arr[n - 1];
            float dE = dirE.magnitude;
            if (dE > gap * 1.05f)
                arr[n] = arr[n] - dirE / dE * gap;
            return arr;
        }

        /// <summary>One arrow per polygon edge, endpoints shrunk by gap.</summary>
        public static List<Vector3[]> OutlineOneArrowPerEdge(Vector3[] polyline, float gap, float grid)
        {
            var result = new List<Vector3[]>();
            if (polyline == null || polyline.Length < 2) return result;
            int edges = polyline.Length - 1;
            for (int i = 0; i < edges; i++)
            {
                var a = SnapVector(polyline[i], grid);
                var b = SnapVector(polyline[i + 1], grid);
                if (Vector3.Distance(a, b) < 0.0001f) continue;
                result.Add(ShortenStroke(new[] { a, b }, gap));
            }
            return result;
        }

        /// <summary>
        /// Fills the interior of a closed polygon with horizontal scanline arrows.
        /// Picks the largest interior x-interval per scanline; endpoints inset by gap.
        /// </summary>
        public static List<Vector3[]> ScanlineInterior(Vector3[] polygon, int count, float gap, float grid)
        {
            var result = new List<Vector3[]>();
            if (polygon == null || polygon.Length < 3 || count <= 0) return result;

            int last = IsClosedPolyline(polygon) ? polygon.Length - 1 : polygon.Length;
            float ymin = float.PositiveInfinity, ymax = float.NegativeInfinity;
            for (int i = 0; i < last; i++)
            {
                if (polygon[i].y < ymin) ymin = polygon[i].y;
                if (polygon[i].y > ymax) ymax = polygon[i].y;
            }
            float margin = Mathf.Max(gap, grid * 0.25f);
            float yLo = ymin + margin;
            float yHi = ymax - margin;
            if (yHi - yLo < grid) return result;

            for (int i = 0; i < count; i++)
            {
                float t = (i + 1f) / (count + 1f);
                float y = Mathf.Lerp(yLo, yHi, t);

                var xs = new List<float>();
                for (int e = 0; e < last; e++)
                {
                    Vector3 a = polygon[e];
                    Vector3 b = polygon[(e + 1) % last];
                    if ((a.y > y) == (b.y > y)) continue;
                    float k = (y - a.y) / (b.y - a.y);
                    xs.Add(a.x + k * (b.x - a.x));
                }
                if (xs.Count < 2) continue;
                xs.Sort();

                float bestLen = 0f;
                float bestX0 = 0f, bestX1 = 0f;
                for (int k = 0; k + 1 < xs.Count; k += 2)
                {
                    float len = xs[k + 1] - xs[k];
                    if (len > bestLen)
                    {
                        bestLen = len;
                        bestX0 = xs[k];
                        bestX1 = xs[k + 1];
                    }
                }
                if (bestLen < grid + gap * 2f) continue;

                Vector3 p0 = SnapVector(new Vector3(bestX0, y, 0f), grid);
                Vector3 p1 = SnapVector(new Vector3(bestX1, y, 0f), grid);
                if (Vector3.Distance(p0, p1) < grid) continue;
                result.Add(ShortenStroke(new[] { p0, p1 }, gap));
            }
            return result;
        }
    }

    /// <summary>
    /// Draws a direction arrowhead + stroke number for every Line in the scene so authors
    /// can see arrow direction at a glance even when the object isn't selected.
    /// </summary>
    internal static class LineSceneGizmos
    {
        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected | GizmoType.Active | GizmoType.Pickable)]
        private static void DrawLineGizmo(LineComp line, GizmoType type)
        {
            if (line == null) return;
            var lr = line.LineRenderer != null ? line.LineRenderer : line.GetComponent<LineRenderer>();
            if (lr == null || lr.positionCount < 2) return;

            int index = line.transform.GetSiblingIndex();
            Color color = LineAuthoringHelpers.ColorForIndex(index);
            Transform t = line.transform;
            int count = lr.positionCount;

            Vector3 tip = t.TransformPoint(lr.GetPosition(count - 1));
            Vector3 preTip = t.TransformPoint(lr.GetPosition(count - 2));
            Vector3 dir = (tip - preTip);
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.right;
            dir.Normalize();
            Vector3 side = Vector3.Cross(dir, Vector3.forward).normalized;
            float headSize = 0.35f;
            Vector3 baseL = tip - dir * headSize + side * (headSize * 0.6f);
            Vector3 baseR = tip - dir * headSize - side * (headSize * 0.6f);

            Gizmos.color = color;
            Gizmos.DrawLine(tip, baseL);
            Gizmos.DrawLine(tip, baseR);
            Gizmos.DrawLine(baseL, baseR);

            for (int i = 0; i < count - 1; i++)
            {
                Vector3 a = t.TransformPoint(lr.GetPosition(i));
                Vector3 b = t.TransformPoint(lr.GetPosition(i + 1));
                Vector3 mid = (a + b) * 0.5f;
                Vector3 d = (b - a);
                if (d.sqrMagnitude < 0.0001f) continue;
                d.Normalize();
                Vector3 r = Vector3.Cross(d, Vector3.forward).normalized;
                float s = 0.12f;
                Gizmos.DrawLine(mid + d * s, mid - d * s + r * s);
                Gizmos.DrawLine(mid + d * s, mid - d * s - r * s);

                Handles.color = color;
                Handles.DrawSolidDisc(a, Vector3.forward, 0.06f);
            }

            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = color },
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter,
            };
            Vector3 start = t.TransformPoint(lr.GetPosition(0));
            Handles.Label(start + new Vector3(-0.35f, 0.35f, 0f), $"#{index + 1}", style);
        }
    }

    /// <summary>
    /// Adds authoring buttons (Add/Remove point, Snap, Reverse, Rotate, Mirror) to Line's inspector
    /// plus a per-point editor and full Scene-view drag/click editing.
    /// </summary>
    [CustomEditor(typeof(LineComp))]
    [CanEditMultipleObjects]
    internal class LineAuthoringInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Level Authoring", EditorStyles.boldLabel);

            float grid = EditorGUILayout.FloatField("Grid Size", LineAuthoringHelpers.GridSize);
            if (!Mathf.Approximately(grid, LineAuthoringHelpers.GridSize))
            {
                LineAuthoringHelpers.GridSize = grid;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Snap to Grid")) ApplyToAll("Snap Line to Grid", lr => LineAuthoringHelpers.SnapAllPositions(lr, LineAuthoringHelpers.GridSize));
            if (GUILayout.Button("Reverse Direction")) ApplyToAll("Reverse Line", LineAuthoringHelpers.ReverseDirection);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Point at End")) ApplyToAll("Add Point", lr => LineAuthoringHelpers.AppendPoint(lr, LineAuthoringHelpers.GridSize));
            if (GUILayout.Button("Remove Last Point")) ApplyToAll("Remove Point", LineAuthoringHelpers.RemoveLastPoint);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Rotate 90° CW")) ApplyToAll("Rotate Line", lr => LineAuthoringHelpers.Rotate90(lr, true, LineAuthoringHelpers.GridSize));
            if (GUILayout.Button("Rotate 90° CCW")) ApplyToAll("Rotate Line", lr => LineAuthoringHelpers.Rotate90(lr, false, LineAuthoringHelpers.GridSize));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Mirror X")) ApplyToAll("Mirror Line", lr => LineAuthoringHelpers.Mirror(lr, true, LineAuthoringHelpers.GridSize));
            if (GUILayout.Button("Mirror Y")) ApplyToAll("Mirror Line", lr => LineAuthoringHelpers.Mirror(lr, false, LineAuthoringHelpers.GridSize));
            EditorGUILayout.EndHorizontal();

            if (targets.Length == 1)
            {
                DrawPointList();
                DrawLengthEditor();
            }

            EditorGUILayout.HelpBox(
                "Scene view controls (with a Line selected):\n" +
                "  • Drag any yellow dot to move a point (grid-snapped).\n" +
                "  • Shift + Left-Click anywhere to APPEND a point at cursor.\n" +
                "  • Click the small white dot on a segment to INSERT a point there.\n" +
                "  • Alt + Left-Click on a point to DELETE it.",
                MessageType.Info);
        }

        private void DrawPointList()
        {
            var line = target as LineComp;
            if (line == null) return;
            var lr = line.LineRenderer != null ? line.LineRenderer : line.GetComponent<LineRenderer>();
            if (lr == null) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Points ({lr.positionCount})", EditorStyles.boldLabel);

            int count = lr.positionCount;
            float g = LineAuthoringHelpers.GridSize;
            int insertAfter = -1;
            int deleteIndex = -1;

            for (int i = 0; i < count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label($"P{i}", GUILayout.Width(28));
                    Vector3 pos = lr.GetPosition(i);
                    EditorGUI.BeginChangeCheck();
                    Vector2 v = EditorGUILayout.Vector2Field(GUIContent.none, new Vector2(pos.x, pos.y));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(lr, "Edit Line Point");
                        lr.SetPosition(i, new Vector3(v.x, v.y, pos.z));
                        EditorUtility.SetDirty(lr);
                    }
                    if (GUILayout.Button("+", GUILayout.Width(24))) insertAfter = i;
                    using (new EditorGUI.DisabledScope(count <= 2))
                    {
                        if (GUILayout.Button("X", GUILayout.Width(24))) deleteIndex = i;
                    }
                }
            }

            if (insertAfter >= 0)
            {
                Undo.RecordObject(lr, "Insert Line Point");
                Vector3 a = lr.GetPosition(insertAfter);
                Vector3 b = insertAfter + 1 < count ? lr.GetPosition(insertAfter + 1) : a + Vector3.right * g;
                Vector3 mid = LineAuthoringHelpers.SnapVector((a + b) * 0.5f, g);
                var arr = new Vector3[count + 1];
                for (int j = 0; j <= insertAfter; j++) arr[j] = lr.GetPosition(j);
                arr[insertAfter + 1] = mid;
                for (int j = insertAfter + 1; j < count; j++) arr[j + 1] = lr.GetPosition(j);
                lr.positionCount = count + 1;
                lr.SetPositions(arr);
                EditorUtility.SetDirty(lr);
            }
            else if (deleteIndex >= 0 && count > 2)
            {
                Undo.RecordObject(lr, "Delete Line Point");
                var arr = new Vector3[count - 1];
                for (int j = 0, k = 0; j < count; j++)
                {
                    if (j == deleteIndex) continue;
                    arr[k++] = lr.GetPosition(j);
                }
                lr.positionCount = count - 1;
                lr.SetPositions(arr);
                EditorUtility.SetDirty(lr);
            }
        }

        private void DrawLengthEditor()
        {
            var line = target as LineComp;
            if (line == null) return;
            var lr = line.LineRenderer != null ? line.LineRenderer : line.GetComponent<LineRenderer>();
            if (lr == null || lr.positionCount < 2) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Length", EditorStyles.boldLabel);

            Vector3 start = lr.GetPosition(0);
            Vector3 end = lr.GetPosition(lr.positionCount - 1);
            float chord = Vector3.Distance(start, end);
            float newChord = EditorGUILayout.FloatField("Start→End Distance", chord);
            if (!Mathf.Approximately(newChord, chord) && newChord > 0.001f)
            {
                Undo.RecordObject(lr, "Set Line Length");
                Vector3 dir = (end - start).sqrMagnitude > 0.0001f ? (end - start).normalized : Vector3.right;
                Vector3 newEnd = LineAuthoringHelpers.SnapVector(start + dir * newChord, LineAuthoringHelpers.GridSize);
                lr.SetPosition(lr.positionCount - 1, newEnd);
                EditorUtility.SetDirty(lr);
            }

            if (GUILayout.Button("Straighten (Keep Endpoints)"))
            {
                Undo.RecordObject(lr, "Straighten Line");
                int n = lr.positionCount;
                Vector3 a = lr.GetPosition(0);
                Vector3 b = lr.GetPosition(n - 1);
                for (int i = 1; i < n - 1; i++)
                {
                    float t = i / (float)(n - 1);
                    lr.SetPosition(i, LineAuthoringHelpers.SnapVector(Vector3.Lerp(a, b, t), LineAuthoringHelpers.GridSize));
                }
                EditorUtility.SetDirty(lr);
            }
        }

        private void OnSceneGUI()
        {
            var line = target as LineComp;
            if (line == null) return;
            var lr = line.LineRenderer != null ? line.LineRenderer : line.GetComponent<LineRenderer>();
            if (lr == null || lr.positionCount < 1) return;

            Transform tr = line.transform;
            float grid = LineAuthoringHelpers.GridSize;
            Event e = Event.current;
            int count = lr.positionCount;
            Color mainColor = LineAuthoringHelpers.ColorForIndex(tr.GetSiblingIndex());

            // Shift+Left-Click: append point at cursor.
            if (e.type == EventType.MouseDown && e.button == 0 && e.shift && !e.alt && !e.control)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                Plane plane = new Plane(Vector3.forward, Vector3.zero);
                if (plane.Raycast(ray, out float dist))
                {
                    Vector3 hit = ray.GetPoint(dist);
                    Vector3 local = tr.InverseTransformPoint(hit);
                    local.z = 0f;
                    local = LineAuthoringHelpers.SnapVector(local, grid);
                    Undo.RecordObject(lr, "Append Line Point");
                    lr.positionCount = count + 1;
                    lr.SetPosition(count, local);
                    EditorUtility.SetDirty(lr);
                    e.Use();
                    return;
                }
            }

            // Draggable point handles.
            for (int i = 0; i < count; i++)
            {
                Vector3 world = tr.TransformPoint(lr.GetPosition(i));
                float size = HandleUtility.GetHandleSize(world) * 0.12f;

                // Alt+Left-Click on this point: delete.
                if (e.type == EventType.MouseDown && e.alt && !e.shift && e.button == 0 && count > 2)
                {
                    if (Vector2.Distance(HandleUtility.WorldToGUIPoint(world), e.mousePosition) < 14f)
                    {
                        Undo.RecordObject(lr, "Delete Line Point");
                        var arr = new Vector3[count - 1];
                        for (int j = 0, k = 0; j < count; j++)
                        {
                            if (j == i) continue;
                            arr[k++] = lr.GetPosition(j);
                        }
                        lr.positionCount = count - 1;
                        lr.SetPositions(arr);
                        EditorUtility.SetDirty(lr);
                        e.Use();
                        return;
                    }
                }

                Handles.color = i == 0 ? Color.green : (i == count - 1 ? Color.red : Color.yellow);
                EditorGUI.BeginChangeCheck();
                Vector3 newWorld = Handles.FreeMoveHandle(world, size, Vector3.zero, Handles.SphereHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    Vector3 local = tr.InverseTransformPoint(newWorld);
                    local.z = 0f;
                    local = LineAuthoringHelpers.SnapVector(local, grid);
                    Undo.RecordObject(lr, "Move Line Point");
                    lr.SetPosition(i, local);
                    EditorUtility.SetDirty(lr);
                }

                var labelStyle = new GUIStyle(EditorStyles.miniBoldLabel) { normal = { textColor = mainColor } };
                Handles.Label(world + new Vector3(size, size, 0f), $"P{i}", labelStyle);
            }

            // Insert-point midpoint buttons.
            for (int i = 0; i < count - 1; i++)
            {
                Vector3 a = tr.TransformPoint(lr.GetPosition(i));
                Vector3 b = tr.TransformPoint(lr.GetPosition(i + 1));
                Vector3 mid = (a + b) * 0.5f;
                float size = HandleUtility.GetHandleSize(mid) * 0.07f;
                Handles.color = Color.white;
                if (Handles.Button(mid, Quaternion.identity, size, size * 1.5f, Handles.DotHandleCap))
                {
                    Undo.RecordObject(lr, "Insert Line Point");
                    Vector3 midLocal = LineAuthoringHelpers.SnapVector(
                        (lr.GetPosition(i) + lr.GetPosition(i + 1)) * 0.5f, grid);
                    var arr = new Vector3[count + 1];
                    for (int j = 0; j <= i; j++) arr[j] = lr.GetPosition(j);
                    arr[i + 1] = midLocal;
                    for (int j = i + 1; j < count; j++) arr[j + 1] = lr.GetPosition(j);
                    lr.positionCount = count + 1;
                    lr.SetPositions(arr);
                    EditorUtility.SetDirty(lr);
                    return;
                }
            }
        }

        private void ApplyToAll(string undoLabel, System.Action<LineRenderer> action)
        {
            foreach (var t in targets)
            {
                var line = t as LineComp;
                if (line == null) continue;
                var lr = line.LineRenderer != null ? line.LineRenderer : line.GetComponent<LineRenderer>();
                if (lr == null) continue;
                Undo.RecordObject(lr, undoLabel);
                action(lr);
            }
        }
    }

    /// <summary>
    /// Floating window for authoring the currently selected Level: add arrows, snap all,
    /// reverse all, jump to any arrow, adjust grid size.
    /// </summary>
    internal class LevelAuthorWindow : EditorWindow
    {
        private Level _level;
        private Vector2 _scroll;
        private ShapePreset _shapePreset = ShapePreset.Rectangle;
        private float _shapeSize = 3f;
        private int _shapeArrowCount = 8;
        private float _shapeGap = 0.5f;
        private bool _shapeCreateArrows = true;

        [MenuItem("Tools/PathForge/Level Author")]
        public static void Open()
        {
            var win = GetWindow<LevelAuthorWindow>("Level Author");
            win.minSize = new Vector2(320, 240);
            win.Show();
        }

        private void OnEnable()
        {
            Selection.selectionChanged += Repaint;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= Repaint;
        }

        private void OnGUI()
        {
            _level = ResolveSelectedLevel();

            EditorGUILayout.LabelField("Target Level", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Selected Level", _level, typeof(Level), true);
            }
            EditorGUILayout.HelpBox("Select a GameObject with a Level component (in scene or Prefab Mode).", MessageType.None);

            EditorGUILayout.Space();
            float grid = EditorGUILayout.FloatField("Grid Size", LineAuthoringHelpers.GridSize);
            if (!Mathf.Approximately(grid, LineAuthoringHelpers.GridSize))
            {
                LineAuthoringHelpers.GridSize = grid;
            }

            using (new EditorGUI.DisabledScope(_level == null))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Level Actions", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add Arrow")) AddArrow();
                if (GUILayout.Button("Snap All to Grid")) SnapAll();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Reverse All")) ReverseAll();
                if (GUILayout.Button("Delete All Arrows")) DeleteAll();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Shape Arrangement", EditorStyles.boldLabel);
                _shapePreset = (ShapePreset)EditorGUILayout.EnumPopup("Shape", _shapePreset);
                _shapeSize = Mathf.Max(0.1f, EditorGUILayout.FloatField("Size", _shapeSize));
                _shapeGap = Mathf.Clamp(EditorGUILayout.Slider(
                    new GUIContent("Gap Between Arrows", "Min 0.5, Max 1. Space between every arrow head and any other arrow tail."),
                    _shapeGap, 0.5f, 1f), 0.5f, 1f);
                _shapeCreateArrows = EditorGUILayout.Toggle(
                    new GUIContent("Auto-Create Arrows",
                        "If ON, matches the arrow count below by adding/removing arrows before applying."),
                    _shapeCreateArrows);
                using (new EditorGUI.DisabledScope(!_shapeCreateArrows))
                {
                    _shapeArrowCount = Mathf.Max(1, EditorGUILayout.IntField("Arrow Count", _shapeArrowCount));
                }
                if (GUILayout.Button("Apply Shape to All Arrows")) ApplyShape();
                EditorGUILayout.HelpBox(
                    "Uses one arrow per shape edge for the outline; extra arrows become horizontal fill lines inside. Every arrow is shortened by 'Gap' so heads never touch tails.",
                    MessageType.None);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Arrows in Level", EditorStyles.boldLabel);
                DrawArrowList();
            }
        }

        private Level ResolveSelectedLevel()
        {
            if (Selection.activeGameObject == null) return null;
            var lvl = Selection.activeGameObject.GetComponentInParent<Level>();
            return lvl;
        }

        private void AddArrow()
        {
            var parent = LineAuthoringHelpers.GetLinesParent(_level);
            if (parent == null)
            {
                Debug.LogError("[LevelAuthor] Selected Level has no _linesParent assigned.");
                return;
            }
            var go = LineAuthoringHelpers.AddArrowUnder(parent, Vector3.zero, LineAuthoringHelpers.GridSize);
            if (go != null) Selection.activeGameObject = go;
        }

        private void SnapAll()
        {
            foreach (var line in LineAuthoringHelpers.GetArrows(_level))
            {
                var lr = line.LineRenderer != null ? line.LineRenderer : line.GetComponent<LineRenderer>();
                if (lr == null) continue;
                Undo.RecordObject(lr, "Snap All Arrows");
                LineAuthoringHelpers.SnapAllPositions(lr, LineAuthoringHelpers.GridSize);
            }
        }

        private void ReverseAll()
        {
            foreach (var line in LineAuthoringHelpers.GetArrows(_level))
            {
                var lr = line.LineRenderer != null ? line.LineRenderer : line.GetComponent<LineRenderer>();
                if (lr == null) continue;
                Undo.RecordObject(lr, "Reverse All Arrows");
                LineAuthoringHelpers.ReverseDirection(lr);
            }
        }

        private void DeleteAll()
        {
            if (!EditorUtility.DisplayDialog("Delete All Arrows",
                    $"Remove every arrow under '{_level.name}'?", "Delete", "Cancel"))
            {
                return;
            }
            var parent = LineAuthoringHelpers.GetLinesParent(_level);
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(parent.GetChild(i).gameObject);
            }
        }

        private void ApplyShape()
        {
            var parent = LineAuthoringHelpers.GetLinesParent(_level);
            if (parent == null)
            {
                Debug.LogError("[LevelAuthor] Selected Level has no _linesParent assigned.");
                return;
            }

            float grid = LineAuthoringHelpers.GridSize;
            float gap = _shapeGap;

            var polyline = LineAuthoringHelpers.GetShapePolyline(_shapePreset, _shapeSize);
            bool closed = LineAuthoringHelpers.IsClosedPolyline(polyline);
            int edgeCount = polyline.Length - 1;

            List<Vector3[]> strokes;
            int targetCount = _shapeCreateArrows ? _shapeArrowCount : LineAuthoringHelpers.GetArrows(_level).Length;
            if (targetCount <= 0) targetCount = 1;

            if (closed && targetCount > edgeCount)
            {
                strokes = LineAuthoringHelpers.OutlineOneArrowPerEdge(polyline, gap, grid);
                var interior = LineAuthoringHelpers.ScanlineInterior(polyline, targetCount - strokes.Count, gap, grid);
                strokes.AddRange(interior);
            }
            else
            {
                strokes = LineAuthoringHelpers.DistributeAlongPolyline(polyline, targetCount, grid);
                for (int i = 0; i < strokes.Count; i++)
                    strokes[i] = LineAuthoringHelpers.ShortenStroke(strokes[i], gap);
            }

            if (strokes.Count == 0)
            {
                Debug.LogWarning("[LevelAuthor] Shape generation produced no strokes (size/gap too small for grid?).");
                return;
            }

            if (_shapeCreateArrows)
            {
                int existing = LineAuthoringHelpers.GetArrows(_level).Length;
                for (int i = existing - 1; i >= strokes.Count; i--)
                    Undo.DestroyObjectImmediate(parent.GetChild(i).gameObject);
                for (int i = existing; i < strokes.Count; i++)
                    LineAuthoringHelpers.AddArrowUnder(parent, Vector3.zero, grid);
            }

            var arrows = LineAuthoringHelpers.GetArrows(_level);
            int applyCount = Mathf.Min(strokes.Count, arrows.Length);
            for (int i = 0; i < applyCount; i++)
            {
                var line = arrows[i];
                var lr = line.LineRenderer != null ? line.LineRenderer : line.GetComponent<LineRenderer>();
                if (lr == null) continue;

                Undo.RecordObject(line.transform, "Apply Shape");
                line.transform.localPosition = Vector3.zero;
                line.transform.localRotation = Quaternion.identity;
                line.transform.localScale = Vector3.one;

                Undo.RecordObject(lr, "Apply Shape");
                lr.useWorldSpace = false;
                var pts = strokes[i];
                lr.positionCount = pts.Length;
                lr.SetPositions(pts);
                EditorUtility.SetDirty(lr);
                EditorUtility.SetDirty(line);
            }

            if (arrows.Length > strokes.Count)
            {
                Debug.LogWarning($"[LevelAuthor] {arrows.Length - strokes.Count} arrow(s) left untouched (shape couldn't fit them). Consider fewer arrows or a larger size.");
            }
        }

        private void DrawArrowList()
        {
            var arrows = LineAuthoringHelpers.GetArrows(_level);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < arrows.Length; i++)
            {
                var line = arrows[i];
                if (line == null) continue;
                var lr = line.LineRenderer != null ? line.LineRenderer : line.GetComponent<LineRenderer>();
                int pointCount = lr != null ? lr.positionCount : 0;

                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    var oldColor = GUI.color;
                    GUI.color = LineAuthoringHelpers.ColorForIndex(i);
                    GUILayout.Label($"#{i + 1}", GUILayout.Width(30));
                    GUI.color = oldColor;

                    GUILayout.Label($"{line.name}  ({pointCount} pts)", GUILayout.MinWidth(140));

                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        Selection.activeGameObject = line.gameObject;
                        EditorGUIUtility.PingObject(line.gameObject);
                        SceneView.lastActiveSceneView?.FrameSelected();
                    }
                    if (GUILayout.Button("Reverse", GUILayout.Width(70)) && lr != null)
                    {
                        Undo.RecordObject(lr, "Reverse Arrow");
                        LineAuthoringHelpers.ReverseDirection(lr);
                    }
                    if (GUILayout.Button("X", GUILayout.Width(24)))
                    {
                        Undo.DestroyObjectImmediate(line.gameObject);
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
