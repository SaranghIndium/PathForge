using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace _Game.EditorTools
{
    /// <summary>
    /// Generates 40 hand-designed test level prefabs where every arrow is a stroke of one
    /// meaningful figure (square, diamond, cross, letters, house, nested rings, windmill, ...).
    /// Arrows are grid-aligned, never share an edge, and pack tightly around the play area.
    /// </summary>
    public static class LevelPrefabGenerator
    {
        private const string BasePrefabPath = "Assets/_Game/Resources/Levels/Level_Base.prefab";
        private const string LinePrefabPath = "Assets/_Game/Resources/Line/Line (1).prefab";
        private const string OutputFolderParent = "Assets";
        private const string OutputFolderName = "TestNewLevelsPrefab";
        private const string OutputFolder = OutputFolderParent + "/" + OutputFolderName;
        private const string LevelNamePrefix = "TestLevel_";
        private const int LevelCount = 40;

        [MenuItem("Tools/PathForge/Generate 40 Test Level Prefabs")]
        public static void GenerateAll()
        {
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefabPath);
            var linePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LinePrefabPath);
            if (basePrefab == null)
            {
                Debug.LogError($"[LevelPrefabGenerator] Base prefab not found at {BasePrefabPath}");
                return;
            }
            if (linePrefab == null)
            {
                Debug.LogError($"[LevelPrefabGenerator] Line prefab not found at {LinePrefabPath}");
                return;
            }

            if (!AssetDatabase.IsValidFolder(OutputFolder))
            {
                AssetDatabase.CreateFolder(OutputFolderParent, OutputFolderName);
            }

            int successCount = 0;
            try
            {
                AssetDatabase.StartAssetEditing();
                for (int i = 1; i <= LevelCount; i++)
                {
                    if (GenerateOne(i, linePrefab)) successCount++;
                    EditorUtility.DisplayProgressBar(
                        "Generating Test Levels",
                        $"{LevelNamePrefix}{i:00}.prefab",
                        (float)i / LevelCount);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[LevelPrefabGenerator] Generated {successCount}/{LevelCount} prefabs in {OutputFolder}");
        }

        private static bool GenerateOne(int levelNumber, GameObject linePrefab)
        {
            string levelName = $"{LevelNamePrefix}{levelNumber:00}";
            string dstPath = $"{OutputFolder}/{levelName}.prefab";

            GameObject root = PrefabUtility.LoadPrefabContents(BasePrefabPath);
            if (root == null)
            {
                Debug.LogError($"[LevelPrefabGenerator] Failed to load base prefab contents for {levelName}");
                return false;
            }

            try
            {
                root.name = levelName;

                Transform linesParent = FindDeep(root.transform, "LINES");
                if (linesParent == null)
                {
                    Debug.LogError($"[LevelPrefabGenerator] LINES parent not found in Level_Base for {levelName}");
                    return false;
                }

                for (int c = linesParent.childCount - 1; c >= 0; c--)
                {
                    UnityEngine.Object.DestroyImmediate(linesParent.GetChild(c).gameObject);
                }

                List<Vector3[]> polylines = LevelFigureLibrary.Build(levelNumber);
                for (int i = 0; i < polylines.Count; i++)
                {
                    Vector3[] pts = polylines[i];
                    if (pts == null || pts.Length < 2) continue;

                    GameObject lineGO = (GameObject)PrefabUtility.InstantiatePrefab(linePrefab, linesParent);
                    lineGO.name = $"Line ({i + 1})";
                    lineGO.transform.localPosition = Vector3.zero;
                    lineGO.transform.localRotation = Quaternion.identity;
                    lineGO.transform.localScale = Vector3.one;

                    var lr = lineGO.GetComponent<LineRenderer>();
                    if (lr == null)
                    {
                        Debug.LogWarning($"[LevelPrefabGenerator] Line prefab missing LineRenderer for {levelName}");
                        continue;
                    }
                    lr.useWorldSpace = false;
                    lr.positionCount = pts.Length;
                    lr.SetPositions(pts);
                }

                PrefabUtility.SaveAsPrefabAsset(root, dstPath, out bool ok);
                return ok;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LevelPrefabGenerator] {levelName} failed: {ex}");
                return false;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Transform FindDeep(Transform t, string targetName)
        {
            if (t.name == targetName) return t;
            for (int i = 0; i < t.childCount; i++)
            {
                var r = FindDeep(t.GetChild(i), targetName);
                if (r != null) return r;
            }
            return null;
        }
    }

    /// <summary>
    /// Hand-curated figure library. Each level is one recognizable picture whose strokes are
    /// short polylines (each becomes an arrow). Strokes may share endpoints but never a segment.
    /// </summary>
    internal static class LevelFigureLibrary
    {
        public static List<Vector3[]> Build(int level)
        {
            Vector2Int c0 = Vector2Int.zero;
            List<Vector2Int[]> strokes;

            switch (level)
            {
                // Tier 1 — introductions (single-figure basics)
                case 1: strokes = SquareRing(c0, 2); break;
                case 2: strokes = MazeReference(); break;
                case 3: strokes = RectangleRing(c0, 4, 2); break;
                case 4: strokes = DiamondRing(c0, 2); break;
                case 5: strokes = TShape(new Vector2Int(0, 2), 3, 4); break;
                case 6: strokes = LShape(new Vector2Int(-3, -2), 4, 5); break;
                case 7: strokes = UShape(c0, 3, 2); break;
                case 8: strokes = HLetter(c0, 2, 2); break;

                // Tier 2 — richer single figures
                case 9: strokes = ILetter(c0, 2, 2); break;
                case 10: strokes = ELetter(new Vector2Int(-2, -2), 3, 2); break;
                case 11: strokes = SquareRing(c0, 3); break;
                case 12: strokes = DiamondRing(c0, 3); break;
                case 13: strokes = CrossInSquare(c0, 3); break;
                case 14: strokes = Windmill(c0, 3); break;
                case 15: strokes = NestedSquares(c0, 1, 3); break;
                case 16: strokes = NestedDiamonds(c0, 1, 3); break;

                // Tier 3 — combined figures / patterns
                case 17: strokes = House(new Vector2Int(0, -2), 6, 3); break;
                case 18: strokes = GridOfSquares(2, 2, 2, 2, new Vector2Int(-3, -3)); break;
                case 19: strokes = DiamondInSquare(c0, 3); break;
                case 20: strokes = NestedSquares(c0, 1, 2, 3); break;
                case 21: strokes = NestedDiamonds(c0, 1, 2, 3); break;
                case 22: strokes = StaircaseMulti(new Vector2Int(-3, -2), 5); break;
                case 23: strokes = ZigzagWave(new Vector2Int(-4, 0), 8); break;
                case 24: strokes = RectangleRing(c0, 5, 3); break;

                // Tier 4 — denser
                case 25: strokes = SquareRing(c0, 4); break;
                case 26: strokes = DiamondRing(c0, 4); break;
                case 27: strokes = CrossInSquare(c0, 4); break;
                case 28: strokes = Windmill(c0, 4); break;
                case 29: strokes = NestedSquares(c0, 1, 2, 3, 4); break;
                case 30: strokes = GridOfSquares(3, 2, 1, 1, new Vector2Int(-4, -2)); break;
                case 31: strokes = HLetter(c0, 3, 3); break;
                case 32: strokes = DiamondInSquare(c0, 4); break;

                // Tier 5 — masterpieces
                case 33: strokes = NestedDiamonds(c0, 1, 2, 3, 4); break;
                case 34: strokes = GridOfSquares(3, 3, 1, 1, new Vector2Int(-4, -3)); break;
                case 35: strokes = WindmillInSquare(c0, 4); break;
                case 36: strokes = CrossInDiamond(c0, 4); break;
                case 37: strokes = NestedSquaresWithCross(c0, new[] { 2, 4 }); break;
                case 38: strokes = NestedDiamondsWithSquare(c0, new[] { 2, 3 }, 4); break;
                case 39: strokes = MasterCombo1(c0); break;
                case 40: strokes = MasterCombo2(c0); break;

                default: strokes = SquareRing(c0, 3); break;
            }

            ValidateNonOverlapping(strokes, level);

            var result = new List<Vector3[]>(strokes.Count);
            foreach (var poly in strokes)
            {
                var arr = new Vector3[poly.Length];
                for (int i = 0; i < poly.Length; i++)
                {
                    arr[i] = new Vector3(poly[i].x, poly[i].y, 0f);
                }
                result.Add(arr);
            }
            return result;
        }

        // ── Figure factories ────────────────────────────────────────────────

        private static List<Vector2Int[]> SquareRing(Vector2Int center, int halfSize)
        {
            int r = halfSize;
            var bl = new Vector2Int(center.x - r, center.y - r);
            var br = new Vector2Int(center.x + r, center.y - r);
            var tr = new Vector2Int(center.x + r, center.y + r);
            var tl = new Vector2Int(center.x - r, center.y + r);
            return new List<Vector2Int[]>
            {
                new[] { bl, br },
                new[] { br, tr },
                new[] { tr, tl },
                new[] { tl, bl },
            };
        }

        private static List<Vector2Int[]> RectangleRing(Vector2Int center, int halfW, int halfH)
        {
            var bl = new Vector2Int(center.x - halfW, center.y - halfH);
            var br = new Vector2Int(center.x + halfW, center.y - halfH);
            var tr = new Vector2Int(center.x + halfW, center.y + halfH);
            var tl = new Vector2Int(center.x - halfW, center.y + halfH);
            return new List<Vector2Int[]>
            {
                new[] { bl, br },
                new[] { br, tr },
                new[] { tr, tl },
                new[] { tl, bl },
            };
        }

        private static List<Vector2Int[]> Cross(Vector2Int center, int armX, int armY)
        {
            return new List<Vector2Int[]>
            {
                new[] { new Vector2Int(center.x - armX, center.y), new Vector2Int(center.x + armX, center.y) },
                new[] { new Vector2Int(center.x, center.y - armY), new Vector2Int(center.x, center.y + armY) },
            };
        }

        private static List<Vector2Int[]> DiamondRing(Vector2Int center, int r)
        {
            var top = new Vector2Int(center.x, center.y + r);
            var right = new Vector2Int(center.x + r, center.y);
            var bottom = new Vector2Int(center.x, center.y - r);
            var left = new Vector2Int(center.x - r, center.y);
            return new List<Vector2Int[]>
            {
                StepLine(top, right),
                StepLine(right, bottom),
                StepLine(bottom, left),
                StepLine(left, top),
            };
        }

        private static List<Vector2Int[]> TShape(Vector2Int top, int halfW, int height)
        {
            var left = new Vector2Int(top.x - halfW, top.y);
            var right = new Vector2Int(top.x + halfW, top.y);
            var stemBottom = new Vector2Int(top.x, top.y - height);
            return new List<Vector2Int[]>
            {
                new[] { left, right },
                new[] { top, stemBottom },
            };
        }

        private static List<Vector2Int[]> LShape(Vector2Int corner, int height, int width)
        {
            var top = new Vector2Int(corner.x, corner.y + height);
            var right = new Vector2Int(corner.x + width, corner.y);
            return new List<Vector2Int[]>
            {
                new[] { top, corner },
                new[] { corner, right },
            };
        }

        private static List<Vector2Int[]> UShape(Vector2Int center, int halfW, int halfH)
        {
            var tl = new Vector2Int(center.x - halfW, center.y + halfH);
            var bl = new Vector2Int(center.x - halfW, center.y - halfH);
            var br = new Vector2Int(center.x + halfW, center.y - halfH);
            var tr = new Vector2Int(center.x + halfW, center.y + halfH);
            return new List<Vector2Int[]>
            {
                new[] { tl, bl },
                new[] { bl, br },
                new[] { br, tr },
            };
        }

        private static List<Vector2Int[]> HLetter(Vector2Int center, int halfW, int halfH)
        {
            return new List<Vector2Int[]>
            {
                new[] { new Vector2Int(center.x - halfW, center.y - halfH), new Vector2Int(center.x - halfW, center.y + halfH) },
                new[] { new Vector2Int(center.x + halfW, center.y - halfH), new Vector2Int(center.x + halfW, center.y + halfH) },
                new[] { new Vector2Int(center.x - halfW, center.y),         new Vector2Int(center.x + halfW, center.y) },
            };
        }

        private static List<Vector2Int[]> ILetter(Vector2Int center, int halfW, int halfH)
        {
            return new List<Vector2Int[]>
            {
                new[] { new Vector2Int(center.x - halfW, center.y + halfH), new Vector2Int(center.x + halfW, center.y + halfH) },
                new[] { new Vector2Int(center.x, center.y + halfH),         new Vector2Int(center.x, center.y - halfH) },
                new[] { new Vector2Int(center.x - halfW, center.y - halfH), new Vector2Int(center.x + halfW, center.y - halfH) },
            };
        }

        private static List<Vector2Int[]> ELetter(Vector2Int bl, int width, int halfH)
        {
            var tl = new Vector2Int(bl.x, bl.y + 2 * halfH);
            var tr = new Vector2Int(bl.x + width, bl.y + 2 * halfH);
            var ml = new Vector2Int(bl.x, bl.y + halfH);
            var mr = new Vector2Int(bl.x + width - 1, bl.y + halfH);
            var br = new Vector2Int(bl.x + width, bl.y);
            return new List<Vector2Int[]>
            {
                new[] { bl, tl },
                new[] { tl, tr },
                new[] { ml, mr },
                new[] { bl, br },
            };
        }

        private static List<Vector2Int[]> Windmill(Vector2Int center, int arm)
        {
            var c = center;
            return new List<Vector2Int[]>
            {
                new[] { c, new Vector2Int(c.x + arm, c.y), new Vector2Int(c.x + arm, c.y + 1) },
                new[] { c, new Vector2Int(c.x, c.y + arm), new Vector2Int(c.x - 1, c.y + arm) },
                new[] { c, new Vector2Int(c.x - arm, c.y), new Vector2Int(c.x - arm, c.y - 1) },
                new[] { c, new Vector2Int(c.x, c.y - arm), new Vector2Int(c.x + 1, c.y - arm) },
            };
        }

        private static List<Vector2Int[]> CrossInSquare(Vector2Int center, int r)
        {
            var list = SquareRing(center, r);
            list.AddRange(Cross(center, r, r));
            return list;
        }

        private static List<Vector2Int[]> CrossInDiamond(Vector2Int center, int r)
        {
            var list = DiamondRing(center, r);
            list.AddRange(Cross(center, r - 1, r - 1));
            return list;
        }

        private static List<Vector2Int[]> DiamondInSquare(Vector2Int center, int r)
        {
            var list = SquareRing(center, r);
            list.AddRange(DiamondRing(center, r - 1));
            return list;
        }

        private static List<Vector2Int[]> NestedSquares(Vector2Int center, params int[] radii)
        {
            var list = new List<Vector2Int[]>();
            foreach (var r in radii) list.AddRange(SquareRing(center, r));
            return list;
        }

        private static List<Vector2Int[]> NestedDiamonds(Vector2Int center, params int[] radii)
        {
            var list = new List<Vector2Int[]>();
            foreach (var r in radii) list.AddRange(DiamondRing(center, r));
            return list;
        }

        private static List<Vector2Int[]> NestedSquaresWithCross(Vector2Int center, int[] radii)
        {
            var list = NestedSquares(center, radii);
            int outer = radii.Max();
            list.AddRange(Cross(center, outer, outer));
            return list;
        }

        private static List<Vector2Int[]> NestedDiamondsWithSquare(Vector2Int center, int[] diamondRadii, int squareR)
        {
            var list = NestedDiamonds(center, diamondRadii);
            list.AddRange(SquareRing(center, squareR));
            return list;
        }

        private static List<Vector2Int[]> WindmillInSquare(Vector2Int center, int r)
        {
            var list = SquareRing(center, r);
            list.AddRange(Windmill(center, r - 1));
            return list;
        }

        private static List<Vector2Int[]> House(Vector2Int floorCenter, int width, int wallHeight)
        {
            int hw = width / 2;
            var bl = new Vector2Int(floorCenter.x - hw, floorCenter.y);
            var br = new Vector2Int(floorCenter.x + hw, floorCenter.y);
            var tl = new Vector2Int(floorCenter.x - hw, floorCenter.y + wallHeight);
            var tr = new Vector2Int(floorCenter.x + hw, floorCenter.y + wallHeight);
            var peak = new Vector2Int(floorCenter.x, floorCenter.y + wallHeight + hw);
            return new List<Vector2Int[]>
            {
                new[] { bl, br },
                new[] { br, tr },
                new[] { tl, bl },
                StepLine(tl, peak),
                StepLine(peak, tr),
            };
        }

        private static List<Vector2Int[]> GridOfSquares(int cols, int rows, int cellSize, int gap, Vector2Int origin)
        {
            var list = new List<Vector2Int[]>();
            int pitch = cellSize + gap;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int x = origin.x + c * pitch;
                    int y = origin.y + r * pitch;
                    var bl = new Vector2Int(x, y);
                    var br = new Vector2Int(x + cellSize, y);
                    var tr = new Vector2Int(x + cellSize, y + cellSize);
                    var tl = new Vector2Int(x, y + cellSize);
                    list.Add(new[] { bl, br, tr, tl, bl });
                }
            }
            return list;
        }

        private static List<Vector2Int[]> StaircaseMulti(Vector2Int start, int steps)
        {
            var list = new List<Vector2Int[]>();
            var cur = start;
            for (int i = 0; i < steps; i++)
            {
                var next = new Vector2Int(cur.x + 1, cur.y);
                list.Add(new[] { cur, next });
                cur = next;
                next = new Vector2Int(cur.x, cur.y + 1);
                list.Add(new[] { cur, next });
                cur = next;
            }
            return list;
        }

        private static List<Vector2Int[]> ZigzagWave(Vector2Int start, int teeth)
        {
            var pts = new List<Vector2Int> { start };
            var cur = start;
            bool up = true;
            for (int i = 0; i < teeth; i++)
            {
                cur = new Vector2Int(cur.x, cur.y + (up ? 1 : -1));
                pts.Add(cur);
                cur = new Vector2Int(cur.x + 1, cur.y);
                pts.Add(cur);
                up = !up;
            }
            return new List<Vector2Int[]> { pts.ToArray() };
        }

        private static List<Vector2Int[]> MasterCombo1(Vector2Int center)
        {
            var list = SquareRing(center, 4);
            list.AddRange(DiamondRing(center, 2));
            list.AddRange(Cross(center, 4, 4));
            return list;
        }

        private static List<Vector2Int[]> MasterCombo2(Vector2Int center)
        {
            var list = NestedSquares(center, 2, 4);
            list.AddRange(DiamondRing(center, 3));
            return list;
        }

        // Complex maze layout mirroring Assets/GameImages/2.png: stepped upper walls,
        // parallel corridors, central Z arrow, packed lower compartments, bottom bar.
        private static List<Vector2Int[]> MazeReference()
        {
            return new List<Vector2Int[]>
            {
                // Upper stepped wall (left side)
                new[] { new Vector2Int(-5,  4), new Vector2Int(-1,  4), new Vector2Int(-1,  3) },
                new[] { new Vector2Int(-5,  4), new Vector2Int(-5,  2) },
                new[] { new Vector2Int(-3,  3), new Vector2Int(-3,  2), new Vector2Int(-1,  2) },

                // Upper stepped wall (right side)
                new[] { new Vector2Int( 0,  4), new Vector2Int( 4,  4), new Vector2Int( 4,  3) },
                new[] { new Vector2Int( 5,  4), new Vector2Int( 5,  2) },
                new[] { new Vector2Int( 2,  3), new Vector2Int( 2,  2), new Vector2Int( 4,  2) },

                // Middle parallel corridors
                new[] { new Vector2Int(-4,  2), new Vector2Int(-4,  0) },
                new[] { new Vector2Int(-2,  1), new Vector2Int(-2,  0) },

                // Central Z arrow (the highlighted stroke in the reference)
                new[] { new Vector2Int( 0,  2), new Vector2Int( 0,  1), new Vector2Int( 3,  1), new Vector2Int( 3,  0) },
                new[] { new Vector2Int( 5,  1), new Vector2Int( 5,  0) },

                // Lower-left maze compartment
                new[] { new Vector2Int(-5, -1), new Vector2Int(-5, -4), new Vector2Int(-3, -4) },
                new[] { new Vector2Int(-4, -2), new Vector2Int(-2, -2), new Vector2Int(-2, -3) },
                new[] { new Vector2Int(-3, -3), new Vector2Int(-1, -3), new Vector2Int(-1, -4) },

                // Lower-right maze compartment
                new[] { new Vector2Int( 1, -1), new Vector2Int( 1, -4) },
                new[] { new Vector2Int( 2, -2), new Vector2Int( 4, -2), new Vector2Int( 4, -4) },
                new[] { new Vector2Int( 3, -3), new Vector2Int( 5, -3) },

                // Bottom bar (split with a gap)
                new[] { new Vector2Int(-5, -5), new Vector2Int( 0, -5) },
                new[] { new Vector2Int( 2, -5), new Vector2Int( 5, -5) },
            };
        }

        // Traces a straight axis-aligned or 45° step-line from a to b as adjacent unit points.
        private static Vector2Int[] StepLine(Vector2Int a, Vector2Int b)
        {
            int dx = Math.Sign(b.x - a.x);
            int dy = Math.Sign(b.y - a.y);
            int steps = Math.Max(Math.Abs(b.x - a.x), Math.Abs(b.y - a.y));
            var pts = new Vector2Int[steps + 1];
            pts[0] = a;
            var cur = a;
            for (int i = 0; i < steps; i++)
            {
                cur = new Vector2Int(cur.x + dx, cur.y + dy);
                pts[i + 1] = cur;
            }
            return pts;
        }

        // ── Non-overlap validation (safety net for hand-authored figures) ────

        private static void ValidateNonOverlapping(List<Vector2Int[]> strokes, int level)
        {
            var occupied = new HashSet<long>();
            for (int s = 0; s < strokes.Count; s++)
            {
                var poly = strokes[s];
                for (int i = 0; i < poly.Length - 1; i++)
                {
                    foreach (var key in UnitEdges(poly[i], poly[i + 1]))
                    {
                        if (!occupied.Add(key))
                        {
                            Debug.LogWarning($"[LevelFigureLibrary] Level {level}: stroke {s} overlaps a prior edge.");
                        }
                    }
                }
            }
        }

        private static IEnumerable<long> UnitEdges(Vector2Int a, Vector2Int b)
        {
            int dxTotal = b.x - a.x;
            int dyTotal = b.y - a.y;
            int adx = Math.Abs(dxTotal);
            int ady = Math.Abs(dyTotal);
            if (adx == 0 && ady == 0) yield break;
            if (dxTotal != 0 && dyTotal != 0 && adx != ady) yield break;

            int dx = Math.Sign(dxTotal);
            int dy = Math.Sign(dyTotal);
            int steps = Math.Max(adx, ady);
            var cur = a;
            for (int i = 0; i < steps; i++)
            {
                var next = new Vector2Int(cur.x + dx, cur.y + dy);
                yield return EdgeKey(cur, next);
                cur = next;
            }
        }

        private static long EdgeKey(Vector2Int a, Vector2Int b)
        {
            if (a.x > b.x || (a.x == b.x && a.y > b.y))
            {
                (a, b) = (b, a);
            }
            const long OFFSET = 1000;
            return ((long)(a.x + OFFSET) << 48)
                 | ((long)(a.y + OFFSET) << 32)
                 | ((long)(b.x + OFFSET) << 16)
                 | (long)(b.y + OFFSET);
        }
    }
}
