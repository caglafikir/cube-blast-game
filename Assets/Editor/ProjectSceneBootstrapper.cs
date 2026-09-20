using System;
using System.IO;
using DreamGames.Match.Core;
using DreamGames.Match.Grid;
using DreamGames.Match.Items;
using DreamGames.Match.Items.Obstacles;
using DreamGames.Match.Items.SpecialItems;
using DreamGames.Match.Level;
using DreamGames.Match.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DreamGames.Match.EditorTools
{
    /// <summary>
    /// Builds MainScene and LevelScene from scratch: generates placeholder sprites, creates
    /// item prefabs, lays out the UI, wires references, and registers both scenes. Safe to re-run.
    /// </summary>
    public static class ProjectSceneBootstrapper
    {
        private const string ArtFolder = "Assets/GeneratedArt";
        private const string PrefabFolder = "Assets/Prefabs";
        private const string ScenesFolder = "Assets/Scenes";
        private const float CellSize = 1f;
        private const int BackgroundArtWidth = 480;
        private const int BackgroundArtHeight = 854;

        [MenuItem("Dream Games/Setup Project (Build Scenes and Prefabs)")]
        public static void SetupProject()
        {
            EnsureFolder(ArtFolder);
            EnsureFolder(PrefabFolder);
            EnsureFolder(ScenesFolder);

            var art = GenerateArt();
            var prefabs = BuildPrefabs(art);

            string mainScenePath = BuildMainScene(art);
            string levelScenePath = BuildLevelScene(prefabs, art);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(mainScenePath, true),
                new EditorBuildSettingsScene(levelScenePath, true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Dream Games",
                "Project setup complete.\n\nMainScene and LevelScene were generated with placeholder " +
                "square sprites (no art was supplied with the case study). Open MainScene and press " +
                "Play to try it.",
                "OK");
        }

        // ---------------------------------------------------------------
        // Art generation
        // ---------------------------------------------------------------

        private class ArtSet
        {
            public Sprite red, green, blue, yellow;
            public Sprite hRocket, vRocket, tnt;
            public Sprite vase, stone, chaliceDoor;
            public Sprite[] chaliceTopLeft;     // index = remaining cups
            public Sprite[] chaliceTopRight;
            public Sprite[] chaliceBottomLeft;
            public Sprite[] chaliceBottomRight;
            public Sprite background, panel, buttonBg;
        }

        private static ArtSet GenerateArt()
        {
            var a = new ArtSet
            {
                red = CubeColorSprite("cube_red", new Color(0.85f, 0.20f, 0.20f)),
                green = CubeColorSprite("cube_green", new Color(0.20f, 0.70f, 0.25f)),
                blue = CubeColorSprite("cube_blue", new Color(0.20f, 0.45f, 0.90f)),
                yellow = CubeColorSprite("cube_yellow", new Color(0.95f, 0.80f, 0.15f)),

                hRocket = RocketSprite("rocket_h", new Color(0.78f, 0.80f, 0.84f), new Color(0.85f, 0.25f, 0.25f), horizontal: true),
                vRocket = RocketSprite("rocket_v", new Color(0.78f, 0.80f, 0.84f), new Color(0.30f, 0.55f, 0.90f), horizontal: false),
                tnt = TntSprite("tnt"),

                vase = VaseSprite("vase"),
                stone = StoneSprite("stone"),
                chaliceDoor = ChestDoorSprite("chalice_door"),

                background = AreaBackgroundSprite("background_area"),
                panel = SquareSprite("ui_panel", new Color(0.10f, 0.10f, 0.12f, 0.92f), 8),
                buttonBg = SquareSprite("ui_button", new Color(0.95f, 0.65f, 0.15f), 8)
            };

            a.chaliceTopLeft = new Sprite[4];
            for (int i = 0; i <= 3; i++) a.chaliceTopLeft[i] = ChaliceCabinetSprite($"chalice_tl_{i}", ChaliceBoxCorner.TopLeft, 3, i);

            a.chaliceTopRight = new Sprite[3];
            for (int i = 0; i <= 2; i++) a.chaliceTopRight[i] = ChaliceCabinetSprite($"chalice_tr_{i}", ChaliceBoxCorner.TopRight, 2, i);

            a.chaliceBottomLeft = new Sprite[4];
            for (int i = 0; i <= 3; i++) a.chaliceBottomLeft[i] = ChaliceCabinetSprite($"chalice_bl_{i}", ChaliceBoxCorner.BottomLeft, 3, i);

            a.chaliceBottomRight = new Sprite[3];
            for (int i = 0; i <= 2; i++) a.chaliceBottomRight[i] = ChaliceCabinetSprite($"chalice_br_{i}", ChaliceBoxCorner.BottomRight, 2, i);

            return a;
        }

        private static float SdRoundBox(float px, float py, float halfW, float halfH, float radius)
        {
            float qx = Mathf.Abs(px) - halfW + radius;
            float qy = Mathf.Abs(py) - halfH + radius;
            float ax = Mathf.Max(qx, 0f);
            float ay = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ax * ax + ay * ay) + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
        }

        private static float SdCircle(float px, float py, float radius) => Mathf.Sqrt(px * px + py * py) - radius;

        private static Color Darken(Color c, float f) => new Color(c.r * f, c.g * f, c.b * f, c.a);
        private static Color Lighten(Color c, float f) => new Color(Mathf.Min(1f, c.r * f), Mathf.Min(1f, c.g * f), Mathf.Min(1f, c.b * f), c.a);
        private static Color Blend(Color a, Color b, float t) => Color.Lerp(a, b, Mathf.Clamp01(t));

        /// <summary>1px-ish antialiasing: d &lt;= -0.5 fully inside, d &gt;= 0.5 fully outside.</summary>
        private static float EdgeAlpha(float d) => Mathf.Clamp01(0.5f - d);

        /// <summary>Rasterizes a shape from a per-pixel paint function into a saved Sprite (0,0 = texture center).</summary>
        private static Sprite RasterSprite(string name, int size, Func<float, float, Color> paint)
        {
            return RasterSpriteRect(name, size, size, paint);
        }

        /// <summary>Same as RasterSprite but for a non-square canvas.</summary>
        private static Sprite RasterSpriteRect(string name, int width, int height, Func<float, float, Color> paint)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float px = x + 0.5f - width / 2f;
                float py = y + 0.5f - height / 2f;
                tex.SetPixel(x, y, paint(px, py));
            }
            tex.Apply();
            return SaveSprite(tex, name);
        }

        private static Sprite SquareSprite(string name, Color fill, int size = 64)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color border = fill * 0.75f; border.a = fill.a;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool edge = x < 2 || y < 2 || x >= size - 2 || y >= size - 2;
                tex.SetPixel(x, y, edge ? border : fill);
            }
            tex.Apply();
            return SaveSprite(tex, name);
        }

        /// <summary>Rounded "candy" cube with a border shade and a soft glossy highlight.</summary>
        private static Sprite CubeColorSprite(string name, Color fill, int size = 64)
        {
            Color border = Darken(fill, 0.62f);
            Color shine = Lighten(fill, 1.4f);
            float half = size * 0.40f;
            float radius = size * 0.16f;
            float borderBand = size * 0.06f;

            return RasterSprite(name, size, (px, py) =>
            {
                float d = SdRoundBox(px, py, half, half, radius);
                if (d > 0.75f) return Color.clear;

                Color col = d > -borderBand ? border : fill;

                float shineD = SdCircle(px + half * 0.35f, py - half * 0.40f, half * 0.38f);
                if (shineD < 0f) col = Blend(col, shine, 0.55f * Mathf.Clamp01(-shineD / (half * 0.38f)));

                return new Color(col.r, col.g, col.b, EdgeAlpha(d));
            });
        }

        /// <summary>Rocket: capsule body along the orientation axis, colored nose cone, window and tail fins.</summary>
        private static Sprite RocketSprite(string name, Color bodyColor, Color accentColor, bool horizontal, int size = 64)
        {
            float halfLong = size * 0.36f;
            float halfShort = size * 0.13f;
            float noseLen = size * 0.20f;
            float tailX = -halfLong + size * 0.04f;

            return RasterSprite(name, size, (px, py) =>
            {
                float lx = horizontal ? px : py;
                float ly = horizontal ? py : -px;

                float bodyD = SdRoundBox(lx, ly, halfLong - noseLen * 0.5f, halfShort, halfShort);
                bool inBody = bodyD <= 0f;

                bool inNose = false;
                float noseBaseX = halfLong - noseLen;
                if (lx > noseBaseX && lx <= halfLong + 1f)
                {
                    float t = Mathf.Clamp01((lx - noseBaseX) / noseLen);
                    float halfWidthAtX = halfShort * (1f - t);
                    if (Mathf.Abs(ly) <= halfWidthAtX) inNose = true;
                }

                bool inFin = lx < tailX && lx > tailX - size * 0.10f &&
                             Mathf.Abs(ly) > halfShort * 0.55f && Mathf.Abs(ly) < halfShort * 1.9f;

                if (!inBody && !inNose && !inFin) return Color.clear;

                Color col = bodyColor;
                if (inNose || inFin) col = accentColor;

                float winD = SdCircle(lx - halfLong * 0.05f, ly, halfShort * 0.5f);
                if (inBody && winD < 0f) col = Lighten(accentColor, 1.25f);

                float alpha = inNose || inFin ? 1f : EdgeAlpha(bodyD);
                return new Color(col.r, col.g, col.b, alpha);
            });
        }

        /// <summary>TNT crate: rounded box with a hazard band, plus a fuse and spark on top.</summary>
        private static Sprite TntSprite(string name, int size = 64)
        {
            Color crate = new Color(0.55f, 0.12f, 0.10f);
            Color band = new Color(0.95f, 0.85f, 0.15f);
            Color fuseColor = new Color(0.15f, 0.13f, 0.12f);
            Color spark = new Color(1f, 0.85f, 0.35f);

            float half = size * 0.32f;
            float radius = size * 0.07f;
            float boxOffsetY = -size * 0.08f;

            return RasterSprite(name, size, (px, py) =>
            {
                float by = py - boxOffsetY;
                float d = SdRoundBox(px, by, half, half * 0.82f, radius);

                if (d <= 0f)
                {
                    Color col = crate;
                    if (Mathf.Abs(by) < half * 0.28f) col = band;
                    if (d > -size * 0.045f) col = Darken(col, 0.72f);
                    return new Color(col.r, col.g, col.b, EdgeAlpha(d));
                }

                float fuseX = px - size * 0.08f;
                float fuseTop = boxOffsetY - half;
                bool onFuse = Mathf.Abs(fuseX) < size * 0.025f && py < fuseTop && py > fuseTop - size * 0.16f;
                if (onFuse) return fuseColor;

                float sparkD = SdCircle(px - size * 0.08f, py - (fuseTop - size * 0.16f), size * 0.05f);
                if (sparkD < 0f) return spark;

                return Color.clear;
            });
        }

        /// <summary>Vase: a rounded silhouette built from a piecewise foot/belly/neck/rim width profile.</summary>
        private static Sprite VaseSprite(string name, int size = 64)
        {
            Color body = new Color(0.62f, 0.38f, 0.20f);
            Color darkEdge = Darken(body, 0.62f);
            Color highlight = Lighten(body, 1.35f);
            float halfSize = size * 0.42f;

            float[] ys = { 0f, 0.15f, 0.55f, 0.78f, 0.90f, 1f };
            float[] ws = { 0.48f, 0.80f, 0.95f, 0.40f, 0.55f, 0.62f };

            float WidthAt(float t)
            {
                for (int i = 0; i < ys.Length - 1; i++)
                {
                    if (t >= ys[i] && t <= ys[i + 1])
                    {
                        float local = (t - ys[i]) / (ys[i + 1] - ys[i]);
                        return Mathf.Lerp(ws[i], ws[i + 1], local);
                    }
                }
                return ws[ys.Length - 1];
            }

            return RasterSprite(name, size, (px, py) =>
            {
                if (py < -halfSize || py > halfSize) return Color.clear;
                float t = (py + halfSize) / (2f * halfSize);
                float halfWidth = WidthAt(t) * halfSize;
                float dist = Mathf.Abs(px) - halfWidth;
                if (dist > 0.75f) return Color.clear;

                Color col = body;
                if (dist > -size * 0.05f) col = darkEdge;
                else if (px < -halfWidth * 0.25f && px > -halfWidth * 0.65f) col = Blend(col, highlight, 0.5f);
                if (t > 0.86f) col = Darken(body, 0.75f);

                return new Color(col.r, col.g, col.b, EdgeAlpha(dist));
            });
        }

        /// <summary>Stone: an organic blob made from a union of overlapping circles, plus a crack and a highlight.</summary>
        private static Sprite StoneSprite(string name, int size = 64)
        {
            Color rock = new Color(0.52f, 0.52f, 0.55f);
            Color darkEdge = Darken(rock, 0.62f);
            Color light = Lighten(rock, 1.3f);

            (float x, float y, float r)[] blobs =
            {
                (0f, 0f, size * 0.30f),
                (size * 0.16f, size * 0.10f, size * 0.24f),
                (-size * 0.14f, -size * 0.08f, size * 0.24f),
                (size * 0.05f, -size * 0.18f, size * 0.20f),
                (-size * 0.10f, size * 0.16f, size * 0.20f),
            };

            return RasterSprite(name, size, (px, py) =>
            {
                float minD = float.MaxValue;
                foreach (var b in blobs)
                {
                    float d = SdCircle(px - b.x, py - b.y, b.r);
                    if (d < minD) minD = d;
                }
                if (minD > 0.75f) return Color.clear;

                Color col = minD > -size * 0.06f ? darkEdge : rock;

                float crackDist = Mathf.Abs(px - py * 0.4f - size * 0.02f);
                if (crackDist < size * 0.015f && minD < -size * 0.05f) col = Darken(rock, 0.5f);

                float hlD = SdCircle(px - size * 0.12f, py - size * 0.14f, size * 0.10f);
                if (hlD < 0f) col = Blend(col, light, 0.4f);

                return new Color(col.r, col.g, col.b, EdgeAlpha(minD));
            });
        }

        /// <summary>Chalice box, door phase: a wooden crate with metal corner brackets and a lock.</summary>
        private static Sprite ChestDoorSprite(string name, int size = 64)
        {
            Color wood = new Color(0.42f, 0.27f, 0.15f);
            Color woodDark = Darken(wood, 0.6f);
            Color metal = new Color(0.75f, 0.65f, 0.35f);
            float half = size * 0.42f;

            return RasterSprite(name, size, (px, py) =>
            {
                float d = SdRoundBox(px, py, half, half, size * 0.06f);
                if (d > 0.75f) return Color.clear;

                Color col = wood;
                if (Mathf.Repeat(py + size * 0.5f, size * 0.22f) < size * 0.02f) col = woodDark;

                bool nearCorner = Mathf.Abs(px) > half * 0.55f && Mathf.Abs(py) > half * 0.55f;
                if (nearCorner) col = metal;

                float lockD = SdCircle(px, py - size * 0.02f, size * 0.10f);
                if (lockD < 0f) col = metal;

                if (d > -size * 0.05f) col = Darken(col, 0.75f);

                return new Color(col.r, col.g, col.b, EdgeAlpha(d));
            });
        }

        /// <summary>Draws one quadrant of a shared 2x2 chalice cabinet; the 4 quadrants tile into one cabinet.</summary>
        private static Sprite ChaliceCabinetSprite(string name, ChaliceBoxCorner corner, int segmentTotal, int filledCount, int size = 64)
        {
            bool isLeft = corner == ChaliceBoxCorner.TopLeft || corner == ChaliceBoxCorner.BottomLeft;
            bool isTop = corner == ChaliceBoxCorner.TopLeft || corner == ChaliceBoxCorner.TopRight;

            Color cabinetBody = new Color(0.16f, 0.36f, 0.30f);
            Color cabinetFrame = new Color(0.80f, 0.68f, 0.32f);
            Color shelfWood = new Color(0.10f, 0.24f, 0.20f);
            Color gold = new Color(0.95f, 0.78f, 0.25f);
            Color goldDark = Darken(gold, 0.65f);

            float half = size * 0.5f;   // this quadrant's half-extent
            float bigHalf = size;       // full 2x2 cabinet's half-extent
            float offsetX = isLeft ? -half : half;
            float offsetY = isTop ? half : -half;

            float cellW = (half * 2f) / segmentTotal;
            float cupRadius = (half * 2f / 3f) * 0.30f; // fixed size regardless of segmentTotal

            return RasterSprite(name, size, (px, py) =>
            {
                float bigX = px + offsetX;
                float bigY = py + offsetY;

                float bg = SdRoundBox(bigX, bigY, bigHalf, bigHalf, size * 0.14f);
                if (bg > 0.75f) return Color.clear;

                Color col = bg > -size * 0.06f ? cabinetFrame : cabinetBody;

                if (Mathf.Abs(bigY) < size * 0.05f) col = shelfWood;

                for (int i = 0; i < segmentTotal; i++)
                {
                    if (i >= filledCount) continue; // already collected

                    float cx = -half + cellW * (i + 0.5f);
                    float lx = px - cx;
                    float ly = py;

                    float bowlTopY = cupRadius * 1.1f;
                    float bowlBottomY = -cupRadius * 0.2f;
                    if (ly <= bowlTopY && ly >= bowlBottomY)
                    {
                        float t = Mathf.InverseLerp(bowlBottomY, bowlTopY, ly);
                        float w = Mathf.Lerp(cupRadius * 0.22f, cupRadius * 0.9f, t);
                        if (Mathf.Abs(lx) <= w) col = gold;
                    }
                    if (ly < bowlBottomY && ly > bowlBottomY - cupRadius * 0.7f && Mathf.Abs(lx) < cupRadius * 0.16f)
                        col = gold;
                    if (ly <= bowlBottomY - cupRadius * 0.7f && ly > bowlBottomY - cupRadius * 0.9f && Mathf.Abs(lx) < cupRadius * 0.6f)
                        col = goldDark;
                }

                return new Color(col.r, col.g, col.b, EdgeAlpha(bg));
            });
        }

        /// <summary>MainScene background: a painterly area scene (sky, hills, a tree and a cottage), portrait-shaped.</summary>
        private static Sprite AreaBackgroundSprite(string name, int width = BackgroundArtWidth, int height = BackgroundArtHeight)
        {
            Color skyTop = new Color(0.35f, 0.58f, 0.88f);
            Color skyHorizon = new Color(0.85f, 0.88f, 0.70f);
            Color sunColor = new Color(1f, 0.96f, 0.80f);
            Color hillFar = new Color(0.42f, 0.62f, 0.36f);
            Color hillNear = new Color(0.28f, 0.50f, 0.24f);
            Color groundTop = new Color(0.30f, 0.55f, 0.26f);
            Color groundBottom = new Color(0.16f, 0.32f, 0.15f);
            Color trunk = new Color(0.40f, 0.26f, 0.14f);
            Color leaf = new Color(0.20f, 0.45f, 0.20f);
            Color leafDark = new Color(0.14f, 0.35f, 0.15f);
            Color wall = new Color(0.88f, 0.80f, 0.62f);
            Color wallShade = Darken(wall, 0.8f);
            Color roofColor = new Color(0.78f, 0.20f, 0.14f);
            Color flagColor = new Color(0.85f, 0.20f, 0.20f);

            float halfW = width / 2f;
            float halfH = height / 2f;
            float horizonY = -halfH * 0.18f;

            (float cx, float rx, float ry)[] farHills =
            {
                (-halfW * 0.55f, width * 0.34f, height * 0.10f),
                (halfW * 0.10f, width * 0.30f, height * 0.085f),
                (halfW * 0.65f, width * 0.28f, height * 0.09f),
            };
            (float cx, float rx, float ry)[] nearHills =
            {
                (-halfW * 0.15f, width * 0.40f, height * 0.075f),
                (halfW * 0.55f, width * 0.36f, height * 0.07f),
            };
            (float x, float baseY)[] trees =
            {
                (-halfW * 0.62f, horizonY - height * 0.02f),
            };

            bool InEllipse(float px, float py, float cx, float cy, float rx, float ry)
            {
                float dx = (px - cx) / rx;
                float dy = (py - cy) / ry;
                return dx * dx + dy * dy <= 1f;
            }

            float cottageOffsetX = halfW * 0.53f;
            float wallHalfW = height * 0.095f;
            float wallHeight = height * 0.10f;
            float roofHeight = height * 0.13f;
            float roofOverhang = height * 0.03f;
            float cottageHalfW = wallHalfW + roofOverhang + 2f;
            float cottageTop = wallHeight + roofHeight + 2f;

            bool InCottageBounds(float px, float py)
            {
                float bx = px - cottageOffsetX;
                float by = py - (horizonY - height * 0.01f);
                return Mathf.Abs(bx) <= cottageHalfW && by >= -2f && by <= cottageTop;
            }

            return RasterSpriteRect(name, width, height, (px, py) =>
            {
                bool inNearHill = false;
                foreach (var h in nearHills)
                    if (InEllipse(px, py, h.cx, horizonY, h.rx, h.ry)) inNearHill = true;

                bool inFarHill = false;
                if (!inNearHill)
                    foreach (var h in farHills)
                        if (InEllipse(px, py, h.cx, horizonY, h.rx, h.ry)) inFarHill = true;

                bool isGround = py <= horizonY || inNearHill || inFarHill;

                Color col;
                if (isGround)
                {
                    if (inNearHill) col = hillNear;
                    else if (inFarHill) col = hillFar;
                    else
                    {
                        float t = Mathf.InverseLerp(horizonY, -halfH, py);
                        col = Blend(groundTop, groundBottom, t);
                    }

                    foreach (var tr in trees)
                    {
                        float tx = px - tr.x;
                        float ty = py - tr.baseY;
                        if (Mathf.Abs(tx) < width * 0.012f && ty > 0f && ty < height * 0.05f) col = trunk;
                        float canopyD = SdCircle(tx, ty - height * 0.06f, width * 0.05f);
                        if (canopyD < 0f) col = canopyD < -width * 0.015f ? leaf : leafDark;
                    }
                }
                else
                {
                    float t = Mathf.InverseLerp(horizonY, halfH, py);
                    col = Blend(skyHorizon, skyTop, t);
                    float sunD = SdCircle(px - halfW * 0.32f, py - halfH * 0.60f, width * 0.11f);
                    if (sunD < width * 0.05f)
                        col = Blend(col, sunColor, Mathf.Clamp01(1f - sunD / (width * 0.05f)) * 0.85f);
                }

                float bx = px - cottageOffsetX;
                float by = py - (horizonY - height * 0.01f);
                if (!InCottageBounds(px, py)) return col;

                float wallDist = SdRoundBox(bx, by - wallHeight * 0.5f, wallHalfW, wallHeight * 0.5f, 0f);
                if (wallDist < 1.5f)
                {
                    Color wallCol = wallDist > -height * 0.008f ? wallShade : wall;

                    float doorHalfW = wallHalfW * 0.35f;
                    float doorCx = -wallHalfW * 0.25f;
                    float doorDist = SdRoundBox(bx - doorCx, by - (wallHeight * 0.29f), doorHalfW, wallHeight * 0.29f, height * 0.004f);
                    if (doorDist < 1f) wallCol = Blend(wallCol, trunk, EdgeAlpha(doorDist));

                    float winD = SdCircle(bx - wallHalfW * 0.55f, by - wallHeight * 0.62f, height * 0.010f);
                    if (winD < 1f) wallCol = Blend(wallCol, Blend(sunColor, wall, 0.25f), EdgeAlpha(winD));

                    col = Blend(col, wallCol, EdgeAlpha(wallDist));
                }

                float roofHalfBase = wallHalfW + roofOverhang;
                if (by >= wallHeight - 1f && by <= wallHeight + roofHeight + 1f)
                {
                    float t = Mathf.Clamp01((by - wallHeight) / roofHeight);
                    float roofHalfWAtT = Mathf.Lerp(roofHalfBase, 0f, t);
                    float roofEdgeDist = Mathf.Abs(bx) - roofHalfWAtT;
                    if (by >= wallHeight && by <= wallHeight + roofHeight && roofEdgeDist < 1.5f)
                        col = Blend(col, roofColor, EdgeAlpha(roofEdgeDist));
                }

                return col;
            });
        }

        private static Sprite SaveSprite(Texture2D tex, string name)
        {
            string path = $"{ArtFolder}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 64;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ---------------------------------------------------------------
        // Prefabs
        // ---------------------------------------------------------------

        private class PrefabSet
        {
            public Cube cube;
            public Rocket rocket;
            public Tnt tnt;
            public Vase vase;
            public Stone stone;
            public ChaliceBoxPart chalicePart;
            public ParticleSystem blastBurst;
            public ParticleSystem explosionBurst;
        }

        private static PrefabSet BuildPrefabs(ArtSet art)
        {
            var set = new PrefabSet
            {
                cube = BuildCubePrefab(art),
                rocket = BuildSimplePrefab<Rocket>("Rocket", art.hRocket),
                tnt = BuildSimplePrefab<Tnt>("Tnt", art.tnt),
                vase = BuildSimplePrefab<Vase>("Vase", art.vase),
                stone = BuildSimplePrefab<Stone>("Stone", art.stone),
                chalicePart = BuildSimplePrefab<ChaliceBoxPart>("ChaliceBoxPart", art.chaliceDoor),
                blastBurst = BuildBurstPrefab("BlastBurst", small: true),
                explosionBurst = BuildBurstPrefab("ExplosionBurst", small: false)
            };
            return set;
        }

        /// <summary>A small one-shot particle burst used for blast/explosion VFX.</summary>
        private static ParticleSystem BuildBurstPrefab(string name, bool small)
        {
            var go = new GameObject(name);
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = small ? 0.35f : 0.5f;
            main.startLifetime = small ? 0.35f : 0.55f;
            main.startSpeed = small ? 2.5f : 5.5f;
            main.startSize = small ? 0.16f : 0.26f;
            main.startColor = new ParticleSystem.MinMaxGradient(Color.white, new Color(1f, 0.92f, 0.55f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(small ? 10 : 26)) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = small ? 0.05f : 0.15f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var sizeCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) };
            var colorKeys = new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) };
            var gradient = new Gradient();
            gradient.SetKeys(colorKeys, alphaKeys);
            colorOverLifetime.color = gradient;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            string path = $"{PrefabFolder}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<ParticleSystem>();
        }

        private static T BuildSimplePrefab<T>(string name, Sprite sprite) where T : GridItem
        {
            var go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            go.AddComponent<T>();

            string path = $"{PrefabFolder}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<T>();
        }


        private static Cube BuildCubePrefab(ArtSet art)
        {
            var go = new GameObject("Cube");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = art.red;
            sr.sortingOrder = 1;

            var hintGo = new GameObject("Hint");
            hintGo.transform.SetParent(go.transform, false);
            var hintSr = hintGo.AddComponent<SpriteRenderer>();
            hintSr.sortingOrder = 2;
            hintSr.transform.localScale = Vector3.one * 0.4f;
            hintSr.enabled = false;

            var cube = go.AddComponent<Cube>();
            var so = new SerializedObject(cube);
            so.FindProperty("bodyRenderer").objectReferenceValue = sr;
            so.FindProperty("hintRenderer").objectReferenceValue = hintSr;
            so.ApplyModifiedPropertiesWithoutUndo();

            string path = $"{PrefabFolder}/Cube.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<Cube>();
        }

        // ---------------------------------------------------------------
        // MainScene
        // ---------------------------------------------------------------

        private static string BuildMainScene(ArtSet art)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor = new Color(0.1f, 0.1f, 0.12f);
            camGo.tag = "MainCamera";

            CreateEventSystem();
            Canvas canvas = CreateCanvas("Canvas");

            Image bg = CreateImage(canvas.transform, "Background", art.background);
            StretchFull(bg.rectTransform);
            bg.type = Image.Type.Simple;
            bg.preserveAspect = true;

            Button levelButton = CreateButton(canvas.transform, "LevelButton", "Level 1", art.buttonBg);
            var buttonRect = levelButton.GetComponent<RectTransform>();
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(420, 200);
            buttonRect.anchoredPosition = Vector2.zero;
            Text buttonText = levelButton.GetComponentInChildren<Text>();
            buttonText.fontSize = 48;

            var controllerGo = new GameObject("MainSceneController");
            var controller = controllerGo.AddComponent<MainSceneController>();
            var so = new SerializedObject(controller);
            so.FindProperty("levelButton").objectReferenceValue = levelButton;
            so.FindProperty("levelButtonText").objectReferenceValue = buttonText;
            so.ApplyModifiedPropertiesWithoutUndo();

            string path = $"{ScenesFolder}/MainScene.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        // ---------------------------------------------------------------
        // LevelScene
        // ---------------------------------------------------------------

        private static string BuildLevelScene(PrefabSet prefabs, ArtSet art)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 7f;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.14f);
            camGo.tag = "MainCamera";

            CreateEventSystem();

            // --- Board root the GridManager positions cells relative to ---
            var boardRootGo = new GameObject("BoardRoot");

            // --- Gameplay controllers ---
            var gridGo = new GameObject("GridManager");
            var grid = gridGo.AddComponent<GridManager>();
            var fall = gridGo.AddComponent<FallController>();
            var blast = gridGo.AddComponent<BlastController>();

            var inputGo = new GameObject("InputController");
            var input = inputGo.AddComponent<InputController>();

            // --- Canvas / HUD ---
            Canvas canvas = CreateCanvas("Canvas");

            var hudGo = new GameObject("HUD", typeof(RectTransform));
            hudGo.transform.SetParent(canvas.transform, false);
            StretchFull(hudGo.GetComponent<RectTransform>());
            var hud = hudGo.AddComponent<LevelHudController>();

            Text levelText = CreateText(hudGo.transform, "LevelText", "Level 1", 44, TextAnchor.UpperLeft);
            Anchor(levelText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(240, -60), new Vector2(400, 80));

            Text movesText = CreateText(hudGo.transform, "MovesText", "Moves: 0", 44, TextAnchor.UpperRight);
            Anchor(movesText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-240, -60), new Vector2(400, 80));

            // --- Obstacle goal row: one icon+count per obstacle type ---
            var goalsGo = new GameObject("ObstacleGoals", typeof(RectTransform));
            goalsGo.transform.SetParent(hudGo.transform, false);
            Anchor(goalsGo.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -150), new Vector2(600, 100));
            var goalsLayout = goalsGo.AddComponent<HorizontalLayoutGroup>();
            goalsLayout.childAlignment = TextAnchor.UpperCenter;
            goalsLayout.spacing = 28f;
            goalsLayout.childForceExpandWidth = false;
            goalsLayout.childForceExpandHeight = false;
            goalsLayout.childControlWidth = false;
            goalsLayout.childControlHeight = false;

            GameObject vaseGoalRoot = BuildGoalIcon(goalsGo.transform, "VaseGoal", art.vase, out Text vaseGoalText);
            GameObject stoneGoalRoot = BuildGoalIcon(goalsGo.transform, "StoneGoal", art.stone, out Text stoneGoalText);
            GameObject chaliceGoalRoot = BuildGoalIcon(goalsGo.transform, "ChaliceGoal", art.chaliceDoor, out Text chaliceGoalText);

            var hudSo = new SerializedObject(hud);
            hudSo.FindProperty("levelText").objectReferenceValue = levelText;
            hudSo.FindProperty("movesText").objectReferenceValue = movesText;
            hudSo.FindProperty("vaseGoalRoot").objectReferenceValue = vaseGoalRoot;
            hudSo.FindProperty("vaseGoalText").objectReferenceValue = vaseGoalText;
            hudSo.FindProperty("stoneGoalRoot").objectReferenceValue = stoneGoalRoot;
            hudSo.FindProperty("stoneGoalText").objectReferenceValue = stoneGoalText;
            hudSo.FindProperty("chaliceGoalRoot").objectReferenceValue = chaliceGoalRoot;
            hudSo.FindProperty("chaliceGoalText").objectReferenceValue = chaliceGoalText;
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            // --- Fail popup ---
            var failGo = new GameObject("FailPopup", typeof(RectTransform));
            failGo.transform.SetParent(canvas.transform, false);
            StretchFull(failGo.GetComponent<RectTransform>());
            Image failOverlay = failGo.AddComponent<Image>();
            failOverlay.color = new Color(0, 0, 0, 0.72f);

            Image failPanel = CreateImage(failGo.transform, "Panel", art.panel);
            failPanel.rectTransform.anchorMin = failPanel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            failPanel.rectTransform.sizeDelta = new Vector2(700, 500);
            failPanel.rectTransform.anchoredPosition = Vector2.zero;

            Text failTitle = CreateText(failPanel.transform, "Title", "Level Failed", 48, TextAnchor.MiddleCenter);
            failTitle.rectTransform.sizeDelta = new Vector2(600, 100);
            failTitle.rectTransform.anchoredPosition = new Vector2(0, 150);

            Button closeButton = CreateButton(failPanel.transform, "CloseButton", "Close", art.buttonBg);
            closeButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-160, -120);
            closeButton.GetComponent<RectTransform>().sizeDelta = new Vector2(260, 120);

            Button tryAgainButton = CreateButton(failPanel.transform, "TryAgainButton", "Try Again", art.buttonBg);
            tryAgainButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(160, -120);
            tryAgainButton.GetComponent<RectTransform>().sizeDelta = new Vector2(260, 120);

            var failPopup = failGo.AddComponent<FailPopupController>();
            var failSo = new SerializedObject(failPopup);
            failSo.FindProperty("root").objectReferenceValue = failGo;
            failSo.FindProperty("panel").objectReferenceValue = failPanel.rectTransform;
            failSo.FindProperty("closeButton").objectReferenceValue = closeButton;
            failSo.FindProperty("tryAgainButton").objectReferenceValue = tryAgainButton;
            failSo.ApplyModifiedPropertiesWithoutUndo();

            // --- Win celebration ---
            var winGo = new GameObject("WinCelebration", typeof(RectTransform));
            winGo.transform.SetParent(canvas.transform, false);
            StretchFull(winGo.GetComponent<RectTransform>());
            Image winOverlay = winGo.AddComponent<Image>();
            winOverlay.color = new Color(0, 0, 0, 0.65f);

            var bannerGo = new GameObject("Banner", typeof(RectTransform));
            bannerGo.transform.SetParent(winGo.transform, false);
            bannerGo.GetComponent<RectTransform>().anchorMin = bannerGo.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
            bannerGo.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 200);
            Image bannerBg = bannerGo.AddComponent<Image>();
            bannerBg.sprite = art.panel;
            bannerBg.type = Image.Type.Sliced;
            Text bannerText = CreateText(bannerGo.transform, "Text", "Level Complete!", 64, TextAnchor.MiddleCenter);
            StretchFull(bannerText.rectTransform);

            // World-space, not parented under the overlay canvas, so the camera renders it.
            var particlesGo = new GameObject("CelebrationParticles");
            particlesGo.transform.SetParent(boardRootGo.transform, false);
            particlesGo.transform.localPosition = new Vector3(0f, 4f, 0f);
            var particles = particlesGo.AddComponent<ParticleSystem>();

            var main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.0f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 0.6f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 300;

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 45f;
            shape.radius = 3.5f;
            shape.rotation = new Vector3(90f, 0f, 0f);

            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 80) });

            var rotationOverLifetime = particles.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-180f, 180f);

            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var confettiAlpha = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) };
            var confettiColors = new[]
            {
                new GradientColorKey(new Color(0.95f, 0.30f, 0.35f), 0f),
                new GradientColorKey(new Color(0.30f, 0.65f, 0.95f), 0.33f),
                new GradientColorKey(new Color(0.95f, 0.80f, 0.20f), 0.66f),
                new GradientColorKey(new Color(0.40f, 0.85f, 0.40f), 1f),
            };
            var confettiGradient = new Gradient();
            confettiGradient.SetKeys(confettiColors, confettiAlpha);
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(confettiGradient);

            var particleRenderer = particlesGo.GetComponent<ParticleSystemRenderer>();
            particleRenderer.material = new Material(Shader.Find("Sprites/Default"));

            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var winCelebration = winGo.AddComponent<WinCelebrationController>();
            var winSo = new SerializedObject(winCelebration);
            winSo.FindProperty("root").objectReferenceValue = winGo;
            winSo.FindProperty("overlayImage").objectReferenceValue = winOverlay;
            winSo.FindProperty("celebrationParticles").objectReferenceValue = particles;
            winSo.FindProperty("popupBanner").objectReferenceValue = bannerGo.transform;
            winSo.ApplyModifiedPropertiesWithoutUndo();

            // --- LevelManager ---
            var levelManagerGo = new GameObject("LevelManager");
            var levelManager = levelManagerGo.AddComponent<LevelManager>();
            var lmSo = new SerializedObject(levelManager);
            lmSo.FindProperty("grid").objectReferenceValue = grid;
            lmSo.FindProperty("hud").objectReferenceValue = hud;
            lmSo.FindProperty("failPopup").objectReferenceValue = failPopup;
            lmSo.FindProperty("winCelebration").objectReferenceValue = winCelebration;
            lmSo.ApplyModifiedPropertiesWithoutUndo();

            // --- Wire GridManager / BlastController / FallController / InputController ---
            var gridSo = new SerializedObject(grid);
            gridSo.FindProperty("boardRoot").objectReferenceValue = boardRootGo.transform;
            gridSo.FindProperty("blastController").objectReferenceValue = blast;
            gridSo.FindProperty("fallController").objectReferenceValue = fall;

            gridSo.ApplyModifiedPropertiesWithoutUndo();

            // Set directly on grid.Prefabs (a plain [Serializable] class, not via SerializedProperty).
            grid.Prefabs.cubePrefab = prefabs.cube;
            grid.Prefabs.redSprite = art.red;
            grid.Prefabs.greenSprite = art.green;
            grid.Prefabs.blueSprite = art.blue;
            grid.Prefabs.yellowSprite = art.yellow;
            grid.Prefabs.hRocketHintSprite = art.hRocket;
            grid.Prefabs.vRocketHintSprite = art.vRocket;
            grid.Prefabs.tntHintSprite = art.tnt;
            grid.Prefabs.rocketPrefab = prefabs.rocket;
            grid.Prefabs.horizontalRocketSprite = art.hRocket;
            grid.Prefabs.verticalRocketSprite = art.vRocket;
            grid.Prefabs.tntPrefab = prefabs.tnt;
            grid.Prefabs.tntSprite = art.tnt;
            grid.Prefabs.vasePrefab = prefabs.vase;
            grid.Prefabs.vaseSprite = art.vase;
            grid.Prefabs.stonePrefab = prefabs.stone;
            grid.Prefabs.stoneSprite = art.stone;
            grid.Prefabs.chaliceBoxPartPrefab = prefabs.chalicePart;
            grid.Prefabs.chaliceDoorSprite = art.chaliceDoor;
            grid.Prefabs.chaliceTopLeftSprites = art.chaliceTopLeft;
            grid.Prefabs.chaliceTopRightSprites = art.chaliceTopRight;
            grid.Prefabs.chaliceBottomLeftSprites = art.chaliceBottomLeft;
            grid.Prefabs.chaliceBottomRightSprites = art.chaliceBottomRight;
            grid.Prefabs.blastBurstPrefab = prefabs.blastBurst;
            grid.Prefabs.explosionBurstPrefab = prefabs.explosionBurst;
            EditorUtility.SetDirty(grid);

            var blastSo = new SerializedObject(blast);
            blastSo.FindProperty("grid").objectReferenceValue = grid;
            blastSo.FindProperty("fallController").objectReferenceValue = fall;
            blastSo.FindProperty("cameraTransform").objectReferenceValue = camGo.transform;
            blastSo.ApplyModifiedPropertiesWithoutUndo();

            var inputSo = new SerializedObject(input);
            inputSo.FindProperty("grid").objectReferenceValue = grid;
            inputSo.FindProperty("worldCamera").objectReferenceValue = cam;
            inputSo.ApplyModifiedPropertiesWithoutUndo();

            string path = $"{ScenesFolder}/LevelScene.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        // ---------------------------------------------------------------
        // Small UI helpers
        // ---------------------------------------------------------------

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }
            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        private static Canvas CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); // portrait 9:16
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void Anchor(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            return img;
        }

        private static Text CreateText(Transform parent, string name, string text, int fontSize, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize;
            t.alignment = anchor;
            t.color = Color.white;
            t.resizeTextForBestFit = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        /// <summary>Builds one icon+count cell for the HUD's obstacle-goal row.</summary>
        private static GameObject BuildGoalIcon(Transform parent, string name, Sprite sprite, out Text countText)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rootRt = root.GetComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(90, 100);

            Image icon = CreateImage(root.transform, "Icon", sprite);
            icon.type = Image.Type.Simple;
            icon.preserveAspect = true;
            icon.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            icon.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            icon.rectTransform.pivot = new Vector2(0.5f, 1f);
            icon.rectTransform.sizeDelta = new Vector2(64, 64);
            icon.rectTransform.anchoredPosition = new Vector2(0, 0);

            countText = CreateText(root.transform, "Count", "x0", 30, TextAnchor.MiddleCenter);
            countText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            countText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            countText.rectTransform.pivot = new Vector2(0.5f, 0f);
            countText.rectTransform.sizeDelta = new Vector2(90, 32);
            countText.rectTransform.anchoredPosition = new Vector2(0, 0);

            return root;
        }

        private static Button CreateButton(Transform parent, string name, string label, Sprite bg)
        {
            Image img = CreateImage(parent, name, bg);
            var button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            var rt = img.rectTransform;
            rt.sizeDelta = new Vector2(300, 120);

            Text text = CreateText(img.transform, "Text", label, 40, TextAnchor.MiddleCenter);
            StretchFull(text.rectTransform);
            return button;
        }
    }
}
