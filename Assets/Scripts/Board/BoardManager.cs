using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using BlastGame.Core;
using BlastGame.Data;
using BlastGame.Items;
using BlastGame.UI;

namespace BlastGame.Board
{
    /// <summary>
    /// Central coordinator for the game board. Owns the grid, orchestrates the
    /// turn pipeline (blast → damage → gravity → spawn → win/lose check), and
    /// drives scene transition animations.
    ///
    /// Design note: This class is intentionally a single coordinator rather than
    /// split across multiple MonoBehaviours. The turn pipeline is a sequential
    /// coroutine that must access grid state, VFX, UI, and animations in order.
    /// Splitting it would scatter a single logical flow across files and require
    /// passing the same references everywhere. Pure-logic helpers (MatchFinder,
    /// GravitySystem, CubeSpawner) are already extracted as stateless utilities.
    /// </summary>
    public class BoardManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ItemFactory itemFactory;
        [SerializeField] private InputHandler inputHandler;
        [SerializeField] private Sprite gridBackgroundSprite;

        [Header("Board Settings")]
        [SerializeField] private float horizontalPaddingPx = 75f;
        [SerializeField] private float verticalPaddingPx = 75f;
        [SerializeField] private float itemScaleFactor = 1.0f;

        [Header("UI")]
        [SerializeField] private RectTransform topBarRect;
        [SerializeField] private GameplayHUD gameplayHUD;
        [SerializeField] private FailPopup failPopup;
        [SerializeField] private CelebrationManager celebrationManager;

        [Header("VFX")]
        [SerializeField] private VFXManager vfxManager;

        [Header("Animation")]
        [SerializeField] private float fallDuration = 0.25f;

        public Cell[,] Grid { get; private set; }
        public LevelData CurrentLevelData { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        private float cellSize;
        private Vector3 boardOrigin;
        private Transform boardParent;
        private Transform itemParent;
        private CubeSpawner cubeSpawner;
        private int movesRemaining;
        private int activeExplosionLines;
        private bool gameOver;
        private List<(int x, int y, RocketDirection dir)> pendingChainRockets
            = new List<(int, int, RocketDirection)>();
        private GameObjectPool rocketPartPool;

        private void Start()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("BoardManager: GameManager not found. " +
                    "Start from MainScene, or use Tools > Set Level. Defaulting to level 1.");
            }

            int levelNumber = GameManager.Instance != null
                ? GameManager.Instance.CurrentLevel
                : PlayerPrefsManager.GetCurrentLevel();

            CurrentLevelData = LevelLoader.LoadLevel(levelNumber);

            if (CurrentLevelData == null)
            {
                Debug.LogError("BoardManager: Failed to load level data!");
                return;
            }

            Width = CurrentLevelData.grid_width;
            Height = CurrentLevelData.grid_height;

            CreateParentObjects();
            SetupCameraAndBoard();
            CreateCells();
            SpawnItems();

            cubeSpawner = new CubeSpawner(itemFactory, this);
            movesRemaining = CurrentLevelData.move_count;

            Debug.Log($"BoardManager: Board created ({Width}x{Height}), " +
                      $"cellSize={cellSize:F2}, moves={movesRemaining}");

            if (vfxManager != null)
                vfxManager.Initialize(cellSize, itemParent);

            rocketPartPool = new GameObjectPool(() =>
            {
                GameObject go = new GameObject("RocketPart");
                go.AddComponent<SpriteRenderer>();
                return go;
            }, transform, "RocketPartPool", 2);

            RefreshRocketHints(immediate: true);

            if (gameplayHUD != null)
            {
                gameplayHUD.Initialize(movesRemaining, CountRemainingObstacles());
            }

            StartCoroutine(PlayEnterAnimation());
        }

        // ───────────────────────── Board Setup ─────────────────────────

        private void CreateParentObjects()
        {
            boardParent = new GameObject("Board").transform;
            boardParent.SetParent(transform);

            itemParent = new GameObject("Items").transform;
            itemParent.SetParent(transform);
        }

        private void SetupCameraAndBoard()
        {
            Camera cam = Camera.main;

            // Fixed camera
            float orthoSize = 5f;
            cam.orthographicSize = orthoSize;
            cam.transform.position = new Vector3(0f, 0f, -10f);

            float screenW = Screen.width;
            float screenH = Screen.height;

            // Force canvas layout so RectTransform positions are up to date
            Canvas.ForceUpdateCanvases();

            // TopBar visual bottom in screen pixels
            Vector3[] corners = new Vector3[4];
            topBarRect.GetWorldCorners(corners);
            float topBarScreenHeight = corners[1].y - corners[0].y;
            float transparentOffset = (75f / 650f) * topBarScreenHeight;
            float topBarVisualBottomPx = corners[0].y + transparentOffset;

            // Available area in pixels (between topBar visual bottom and screen bottom)
            float availableTopPx = topBarVisualBottomPx - verticalPaddingPx;
            float availableBottomPx = verticalPaddingPx;
            float availableLeftPx = horizontalPaddingPx;
            float availableRightPx = screenW - horizontalPaddingPx;

            float availableWidthPx = availableRightPx - availableLeftPx;
            float availableHeightPx = availableTopPx - availableBottomPx;

            // Cell size in pixels, then convert to world units
            float cellSizePx = Mathf.Min(availableWidthPx / Width, availableHeightPx / Height);
            float totalWorldHeight = orthoSize * 2f;
            cellSize = cellSizePx / screenH * totalWorldHeight;

            // Grid center in pixels, then convert to world
            float gridCenterPxX = screenW / 2f;
            float gridCenterPxY = (availableTopPx + availableBottomPx) / 2f;

            Vector3 gridCenterWorld = cam.ScreenToWorldPoint(new Vector3(gridCenterPxX, gridCenterPxY, 0f));

            float boardWidth = Width * cellSize;
            float boardHeight = Height * cellSize;

            boardOrigin = new Vector3(
                gridCenterWorld.x - boardWidth / 2f,
                gridCenterWorld.y - boardHeight / 2f,
                0f
            );
        }

        private void CreateCells()
        {
            Grid = new Cell[Width, Height];

            CreateGridBackground();

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Vector3 worldPos = GetWorldPosition(x, y);
                    GameObject cellGo = new GameObject($"Cell_{x}_{y}");
                    cellGo.transform.position = worldPos;
                    cellGo.transform.SetParent(boardParent);

                    Cell cell = cellGo.AddComponent<Cell>();
                    cell.Initialize(x, y);
                    Grid[x, y] = cell;
                }
            }
        }

        private void SpawnItems()
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    string token = CurrentLevelData.GetCell(x, y);
                    BoardItem item = itemFactory.CreateFromToken(token);

                    if (item == null) continue;

                    PlaceItem(item, x, y);
                }
            }
        }

        private void CreateGridBackground()
        {
            if (gridBackgroundSprite == null)
            {
                Debug.LogError("BoardManager: gridBackgroundSprite is not assigned!");
                return;
            }

            float boardWidth = Width * cellSize;
            float boardHeight = Height * cellSize;
            float horizontalPadding = cellSize * 0.13f;
            float verticalPadding = cellSize * 0.2f;

            Vector3 center = new Vector3(
                boardOrigin.x + boardWidth / 2f,
                boardOrigin.y + boardHeight / 2f,
                0f
            );

            GameObject bgGo = new GameObject("GridBackground");
            bgGo.transform.position = center;
            bgGo.transform.SetParent(boardParent);

            SpriteRenderer sr = bgGo.AddComponent<SpriteRenderer>();
            sr.sprite = gridBackgroundSprite;
            sr.sortingOrder = 0;

            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(boardWidth + horizontalPadding * 2f, boardHeight + verticalPadding * 2f);
        }

        // ───────────────────────── Public Board API ─────────────────────────

        public void PlaceItem(BoardItem item, int x, int y)
        {
            Cell cell = Grid[x, y];
            cell.SetItem(item);

            Vector3 worldPos = GetWorldPosition(x, y);
            item.transform.position = worldPos;
            item.transform.SetParent(itemParent);

            ScaleItemToCell(item);

            SpriteRenderer sr = item.SpriteRenderer;
            if (sr != null)
                sr.sortingOrder = 10 + y;
        }

        public Vector3 GetWorldPosition(int x, int y)
        {
            float worldX = boardOrigin.x + x * cellSize + cellSize / 2f;
            float worldY = boardOrigin.y + y * cellSize + cellSize / 2f;
            return new Vector3(worldX, worldY, 0f);
        }

        public bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public Cell GetCell(int x, int y)
        {
            if (!IsValidPosition(x, y)) return null;
            return Grid[x, y];
        }

        /// <summary>
        /// Converts world position to grid coordinates. Returns null if outside the grid.
        /// </summary>
        public (int x, int y)? WorldToGrid(Vector3 worldPos)
        {
            float localX = worldPos.x - boardOrigin.x;
            float localY = worldPos.y - boardOrigin.y;

            int gx = Mathf.FloorToInt(localX / cellSize);
            int gy = Mathf.FloorToInt(localY / cellSize);

            if (!IsValidPosition(gx, gy)) return null;
            return (gx, gy);
        }

        // ───────────────────────── Turn Pipeline ─────────────────────────

        public void OnCellTapped(int x, int y)
        {
            if (gameOver) return;

            Cell cell = GetCell(x, y);
            if (cell == null || cell.IsEmpty) return;

            if (cell.CurrentItem is Rocket)
                StartCoroutine(ProcessRocketTurn(x, y));
            else
                StartCoroutine(ProcessTurn(x, y));
        }

        private IEnumerator ProcessTurn(int x, int y)
        {
            inputHandler.IsInputLocked = true;

            // 1. Find connected group
            List<Cell> group = MatchFinder.FindConnectedGroup(this, x, y);

            if (group.Count < 2)
            {
                // Dead tap — shake the single cube
                Cell tappedCell = GetCell(x, y);
                if (tappedCell != null && !tappedCell.IsEmpty)
                {
                    tappedCell.CurrentItem.transform.DOKill(true);
                    tappedCell.CurrentItem.transform.DOPunchScale(
                        new Vector3(0.15f, -0.15f, 0f), 0.2f, 6, 0.5f);
                }
                inputHandler.IsInputLocked = false;
                yield break;
            }

            // Decrement moves immediately on valid tap
            movesRemaining--;
            if (gameplayHUD != null)
                gameplayHUD.UpdateMoves(movesRemaining);

            // Tap feedback: quick scale punch on group cubes
            if (vfxManager != null)
            {
                foreach (Cell c in group)
                {
                    if (c.CurrentItem != null)
                        vfxManager.PlayTapPunch(c.CurrentItem.transform);
                }
                yield return new WaitForSeconds(0.12f);
            }

            // 2. Find cells adjacent to the group (for obstacle damage)
            HashSet<Cell> adjacents = MatchFinder.GetAdjacentCells(this, group);

            // 3. Destroy cubes (with rocket creation if group >= 4)
            bool createRocket = group.Count >= 4;

            if (createRocket)
            {
                // Capture cube color before they are destroyed
                CubeColor groupColor = (group[0].CurrentItem is Cube firstCube)
                    ? firstCube.Color : CubeColor.Red;

                // Animate cubes toward tapped cell, then destroy
                yield return AnimateCubesTowardAndDestroy(group, x, y);

                // Big burst at converge point
                if (vfxManager != null)
                    vfxManager.PlayCubeBlast(GetWorldPosition(x, y), groupColor, true);

                // Create rocket with random direction
                RocketDirection dir = Random.value < 0.5f
                    ? RocketDirection.Horizontal
                    : RocketDirection.Vertical;
                Rocket rocket = itemFactory.CreateRocket(dir);
                PlaceItem(rocket, x, y);

                // Rocket creation pop effect
                if (vfxManager != null)
                    vfxManager.PlayRocketCreationPop(rocket.transform);

                Debug.Log($"Rocket created at ({x},{y}), direction={dir}, group size={group.Count}");
            }
            else
            {
                // Small group: destroy with individual particle bursts
                foreach (Cell cell in group)
                {
                    BoardItem item = cell.CurrentItem;
                    Vector3 pos = item.transform.position;
                    CubeColor color = (item is Cube cube) ? cube.Color : CubeColor.Red;
                    cell.ClearItem();
                    item.OnDestroyed();

                    if (vfxManager != null)
                        vfxManager.PlayCubeBlast(pos, color, false);
                }
            }

            // 4. Damage adjacent obstacles (with VFX)
            foreach (Cell adjCell in adjacents)
            {
                if (adjCell.IsEmpty) continue;
                BoardItem obstacle = adjCell.CurrentItem;
                Vector3 pos = obstacle.transform.position;
                ItemType type = obstacle.Type;
                bool destroyed = obstacle.TakeDamage(DamageSource.Blast);
                if (destroyed)
                    adjCell.ClearItem();

                if (vfxManager != null)
                {
                    if (destroyed)
                        vfxManager.PlayObstacleBreak(pos, type);
                    else if (type == ItemType.Box || type == ItemType.Stone || type == ItemType.Vase)
                        vfxManager.PlayDamageShake(obstacle.transform);
                }
            }

            // 5. Wait one frame for Destroy() to process
            yield return null;

            // 6. Clean up destroyed obstacle references
            CleanUpDestroyedItems();

            // 7. Gravity + refill loop
            yield return GravityAndRefillLoop();

            // 8. Reset vase turn flags
            ResetVaseTurnFlags();

            // 9. Update HUD (goals only — moves already decremented)
            UpdateHUD();

            // 10. Check win/lose
            if (CheckWinLoseConditions())
                yield break;

            // 12. Refresh rocket hint icons
            RefreshRocketHints();

            // 13. Unlock input
            inputHandler.IsInputLocked = false;
        }

        private IEnumerator ProcessRocketTurn(int x, int y)
        {
            inputHandler.IsInputLocked = true;

            Cell startCell = GetCell(x, y);
            Rocket startRocket = startCell.CurrentItem as Rocket;
            if (startRocket == null)
            {
                inputHandler.IsInputLocked = false;
                yield break;
            }

            // Decrement moves immediately on valid tap
            movesRemaining--;
            if (gameplayHUD != null)
                gameplayHUD.UpdateMoves(movesRemaining);

            // Check for Rocket-Rocket combo: any adjacent rocket?
            var adjacentRockets = FindAdjacentRockets(x, y);
            bool isCombo = adjacentRockets.Count > 0;

            var alreadyExploded = new HashSet<Cell>();
            pendingChainRockets.Clear();
            activeExplosionLines = 0;

            // Consume tapped rocket
            RocketDirection startDir = startRocket.Direction;
            Vector3 rocketPos = GetWorldPosition(x, y);
            alreadyExploded.Add(startCell);
            startCell.ClearItem();
            startRocket.OnDestroyed();

            // Rocket explosion burst at origin
            if (vfxManager != null)
                vfxManager.PlayRocketExplosion(rocketPos);

            if (isCombo)
            {
                Debug.Log($"Rocket-Rocket COMBO at ({x},{y})! Adjacent rockets: {adjacentRockets.Count}");

                // Consume all adjacent rockets (with explosion at each)
                foreach (var (adjCell, adjRocket) in adjacentRockets)
                {
                    if (vfxManager != null)
                        vfxManager.PlayRocketExplosion(GetWorldPosition(adjCell.X, adjCell.Y));

                    alreadyExploded.Add(adjCell);
                    adjCell.ClearItem();
                    adjRocket.OnDestroyed();
                }

                yield return null;

                // Fire 3 horizontal lines (rows y-1, y, y+1) + 3 vertical lines (cols x-1, x, x+1)
                // = 12 rocket parts, ALL simultaneously
                var hSprites = itemFactory.GetRocketPartSprites(RocketDirection.Horizontal);
                var vSprites = itemFactory.GetRocketPartSprites(RocketDirection.Vertical);

                for (int row = y - 1; row <= y + 1; row++)
                {
                    if (row < 0 || row >= Height) continue;
                    StartCoroutine(ExplosionLineCoroutine(x, row, -1, 0, hSprites.partA, alreadyExploded));
                    StartCoroutine(ExplosionLineCoroutine(x, row, 1, 0, hSprites.partB, alreadyExploded));
                }

                for (int col = x - 1; col <= x + 1; col++)
                {
                    if (col < 0 || col >= Width) continue;
                    StartCoroutine(ExplosionLineCoroutine(col, y, 0, -1, vSprites.partA, alreadyExploded));
                    StartCoroutine(ExplosionLineCoroutine(col, y, 0, 1, vSprites.partB, alreadyExploded));
                }
            }
            else
            {
                yield return null;

                // Single rocket: fire 2 parts simultaneously
                var (spriteA, spriteB) = itemFactory.GetRocketPartSprites(startDir);

                if (startDir == RocketDirection.Horizontal)
                {
                    StartCoroutine(ExplosionLineCoroutine(x, y, -1, 0, spriteA, alreadyExploded));
                    StartCoroutine(ExplosionLineCoroutine(x, y, 1, 0, spriteB, alreadyExploded));
                }
                else
                {
                    StartCoroutine(ExplosionLineCoroutine(x, y, 0, -1, spriteA, alreadyExploded));
                    StartCoroutine(ExplosionLineCoroutine(x, y, 0, 1, spriteB, alreadyExploded));
                }
            }

            // Wait for all explosion lines to finish
            while (activeExplosionLines > 0)
                yield return null;

            // Process chain rockets (they fire normally, no combo)
            while (pendingChainRockets.Count > 0)
            {
                var chains = new List<(int, int, RocketDirection)>(pendingChainRockets);
                pendingChainRockets.Clear();

                foreach (var (cx, cy, cdir) in chains)
                {
                    var (sprA, sprB) = itemFactory.GetRocketPartSprites(cdir);
                    if (cdir == RocketDirection.Horizontal)
                    {
                        StartCoroutine(ExplosionLineCoroutine(cx, cy, -1, 0, sprA, alreadyExploded));
                        StartCoroutine(ExplosionLineCoroutine(cx, cy, 1, 0, sprB, alreadyExploded));
                    }
                    else
                    {
                        StartCoroutine(ExplosionLineCoroutine(cx, cy, 0, -1, sprA, alreadyExploded));
                        StartCoroutine(ExplosionLineCoroutine(cx, cy, 0, 1, sprB, alreadyExploded));
                    }
                }

                while (activeExplosionLines > 0)
                    yield return null;
            }

            yield return null;

            // Clean up destroyed items
            CleanUpDestroyedItems();

            // Gravity + refill
            yield return GravityAndRefillLoop();

            // Reset vase flags
            ResetVaseTurnFlags();

            // Update HUD (goals only — moves already decremented)
            UpdateHUD();

            // Check win/lose
            if (CheckWinLoseConditions())
                yield break;

            // Refresh hints
            RefreshRocketHints();

            // Unlock input
            inputHandler.IsInputLocked = false;
        }

        /// <summary>
        /// Coroutine for one explosion line (one direction). Steps through cells one by one,
        /// damages each item as the rocket part reaches it. Multiple lines run simultaneously.
        /// </summary>
        private IEnumerator ExplosionLineCoroutine(
            int originX, int originY, int dx, int dy,
            Sprite partSprite, HashSet<Cell> alreadyExploded)
        {
            activeExplosionLines++;

            Vector3 startPos = GetWorldPosition(originX, originY);
            GameObject partGo = CreateRocketPartVisual(partSprite, startPos);

            float stepDuration = 0.025f;
            int cx = originX + dx;
            int cy = originY + dy;

            while (IsValidPosition(cx, cy))
            {
                Vector3 targetPos = GetWorldPosition(cx, cy);
                partGo.transform.DOMove(targetPos, stepDuration).SetEase(Ease.Linear);
                yield return new WaitForSeconds(stepDuration);

                // Rocket trail particles
                if (vfxManager != null)
                    vfxManager.PlayRocketTrail(targetPos);

                Cell cell = GetCell(cx, cy);
                if (cell != null && !cell.IsEmpty && !alreadyExploded.Contains(cell))
                {
                    BoardItem item = cell.CurrentItem;

                    if (item is Rocket chainRocket)
                    {
                        // Collect chain rocket — don't damage, will fire separately
                        alreadyExploded.Add(cell);
                        RocketDirection chainDir = chainRocket.Direction;
                        int chainX = cell.X;
                        int chainY = cell.Y;
                        cell.ClearItem();
                        chainRocket.OnDestroyed();
                        pendingChainRockets.Add((chainX, chainY, chainDir));
                    }
                    else
                    {
                        // Capture item info before potential destruction
                        Vector3 itemPos = item.transform.position;
                        ItemType itemType = item.Type;
                        CubeColor itemColor = (item is Cube cubeItem)
                            ? cubeItem.Color : CubeColor.Red;

                        // Damage the item (cube, obstacle, etc.)
                        bool destroyed = item.TakeDamage(DamageSource.Rocket);
                        if (destroyed)
                            cell.ClearItem();

                        if (vfxManager != null)
                        {
                            if (destroyed)
                            {
                                if (itemType == ItemType.Cube)
                                    vfxManager.PlayCubeBlast(itemPos, itemColor, false);
                                else
                                    vfxManager.PlayObstacleBreak(itemPos, itemType);
                            }
                            else if (itemType == ItemType.Box || itemType == ItemType.Stone
                                     || itemType == ItemType.Vase)
                            {
                                vfxManager.PlayDamageShake(item.transform);
                            }
                        }
                    }
                }

                cx += dx;
                cy += dy;
            }

            ReturnRocketPart(partGo);
            activeExplosionLines--;
        }

        private void ReturnRocketPart(GameObject go)
        {
            if (go == null || rocketPartPool == null) return;
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = null;
            rocketPartPool.Return(go);
        }

        private GameObject CreateRocketPartVisual(Sprite sprite, Vector3 position)
        {
            GameObject partGo = rocketPartPool.Get();
            partGo.transform.position = position;
            partGo.transform.SetParent(itemParent);

            SpriteRenderer sr = partGo.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 100;

            if (sprite != null)
            {
                float spriteWidth = sr.sprite.bounds.size.x;
                float scale = cellSize / spriteWidth * itemScaleFactor;
                partGo.transform.localScale = new Vector3(scale, scale, 1f);
            }

            return partGo;
        }

        private List<(Cell cell, Rocket rocket)> FindAdjacentRockets(int x, int y)
        {
            var result = new List<(Cell, Rocket)>();
            int[] dx = { 0, 0, -1, 1 };
            int[] dy = { -1, 1, 0, 0 };

            for (int d = 0; d < 4; d++)
            {
                Cell neighbor = GetCell(x + dx[d], y + dy[d]);
                if (neighbor == null || neighbor.IsEmpty) continue;
                if (neighbor.CurrentItem is Rocket rocket)
                    result.Add((neighbor, rocket));
            }

            return result;
        }

        private IEnumerator GravityAndRefillLoop()
        {
            while (true)
            {
                // Logic: sequential (spawn reads gravity result)
                List<FallMove> fallMoves = GravitySystem.ProcessGravity(this);
                List<SpawnMove> spawnMoves = cubeSpawner.SpawnNewCubes();

                if (fallMoves.Count == 0 && spawnMoves.Count == 0)
                    break;

                // Animation: parallel (falls + spawns run simultaneously)
                float maxDuration = 0f;

                // Animate existing items falling
                foreach (FallMove move in fallMoves)
                {
                    if (move.Item == null) continue;

                    Vector3 targetPos = GetWorldPosition(move.X, move.ToY);
                    float duration = fallDuration;
                    if (duration > maxDuration) maxDuration = duration;

                    move.Item.transform.DOKill(true);

                    BoardItem item = move.Item;
                    move.Item.transform.DOMove(targetPos, duration)
                        .SetEase(Ease.OutQuad)
                        .OnComplete(() =>
                        {
                            if (item != null)
                                item.transform.DOPunchScale(
                                    new Vector3(0.1f, -0.1f, 0f), 0.15f, 4, 0.5f);
                        });

                    SpriteRenderer sr = move.Item.SpriteRenderer;
                    if (sr != null)
                        sr.sortingOrder = 10 + move.ToY;
                }

                // Animate new cubes spawning from top (runs in parallel with falls)
                foreach (SpawnMove move in spawnMoves)
                {
                    if (move.Item == null) continue;

                    Vector3 startPos = GetWorldPosition(move.X, Height - 1 + move.SpawnOffsetFromTop);
                    Vector3 targetPos = GetWorldPosition(move.X, move.TargetY);
                    move.Item.transform.position = startPos;

                    float duration = fallDuration;
                    if (duration > maxDuration) maxDuration = duration;

                    move.Item.transform.DOKill(true);

                    BoardItem item = move.Item;
                    move.Item.transform.DOMove(targetPos, duration)
                        .SetEase(Ease.OutQuad)
                        .OnComplete(() =>
                        {
                            if (item != null)
                                item.transform.DOPunchScale(
                                    new Vector3(0.1f, -0.1f, 0f), 0.15f, 4, 0.5f);
                        });

                    SpriteRenderer sr = move.Item.SpriteRenderer;
                    if (sr != null)
                        sr.sortingOrder = 10 + move.TargetY;
                }

                yield return new WaitForSeconds(maxDuration);
            }
        }

        // ───────────────────────── Helpers ─────────────────────────

        private IEnumerator AnimateCubesTowardAndDestroy(List<Cell> group, int targetX, int targetY)
        {
            Vector3 targetPos = GetWorldPosition(targetX, targetY);
            float duration = 0.15f;

            foreach (Cell cell in group)
            {
                BoardItem item = cell.CurrentItem;
                if (item == null) continue;
                item.transform.DOMove(targetPos, duration).SetEase(Ease.InBack);
            }

            yield return new WaitForSeconds(duration);

            foreach (Cell cell in group)
            {
                BoardItem item = cell.CurrentItem;
                if (item == null) continue;
                cell.ClearItem();
                item.OnDestroyed();
            }

            yield return null;
        }

        private void CleanUpDestroyedItems()
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Cell cell = Grid[x, y];
                    if (cell.IsEmpty) continue;

                    // If the GameObject was destroyed or returned to pool (inactive)
                    if (cell.CurrentItem == null || cell.CurrentItem.gameObject == null
                        || !cell.CurrentItem.gameObject.activeSelf)
                    {
                        cell.ClearItem();
                    }
                }
            }
        }

        private void ResetVaseTurnFlags()
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Cell cell = Grid[x, y];
                    if (cell.IsEmpty) continue;
                    if (cell.CurrentItem is Vase vase)
                    {
                        vase.ResetTurnFlag();
                    }
                }
            }
        }

        private void RefreshRocketHints(bool immediate = false)
        {
            // First, clear all hints
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Cell cell = Grid[x, y];
                    if (cell.IsEmpty) continue;
                    if (cell.CurrentItem is Cube cube)
                        cube.SetRocketHint(false, immediate);
                }
            }

            // Then scan every cube, find its group, mark hints for groups >= 4
            var visited = new HashSet<Cell>();

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Cell cell = Grid[x, y];
                    if (cell.IsEmpty || visited.Contains(cell)) continue;
                    if (cell.CurrentItem.Type != ItemType.Cube) continue;

                    List<Cell> group = MatchFinder.FindConnectedGroup(this, x, y);

                    foreach (Cell g in group)
                        visited.Add(g);

                    if (group.Count >= 4)
                    {
                        foreach (Cell g in group)
                        {
                            if (g.CurrentItem is Cube cube)
                                cube.SetRocketHint(true, immediate);
                        }
                    }
                }
            }
        }

        // ───────────────────────── Win/Lose ─────────────────────────

        private Dictionary<ItemType, int> CountRemainingObstacles()
        {
            var counts = new Dictionary<ItemType, int>();

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Cell cell = Grid[x, y];
                    if (cell.IsEmpty) continue;

                    ItemType type = cell.CurrentItem.Type;
                    if (type == ItemType.Box || type == ItemType.Stone || type == ItemType.Vase)
                    {
                        if (counts.ContainsKey(type))
                            counts[type]++;
                        else
                            counts[type] = 1;
                    }
                }
            }

            return counts;
        }

        private bool CheckWinLoseConditions()
        {
            var obstacles = CountRemainingObstacles();
            int totalObstacles = 0;
            foreach (var kvp in obstacles)
                totalObstacles += kvp.Value;

            if (totalObstacles == 0)
            {
                gameOver = true;
                if (GameManager.Instance != null)
                    GameManager.Instance.OnLevelWon();
                StartCoroutine(WinSequence());
                return true;
            }

            if (movesRemaining <= 0)
            {
                gameOver = true;
                if (GameManager.Instance != null)
                    GameManager.Instance.OnLevelLost();
                StartCoroutine(LoseSequence());
                return true;
            }

            return false;
        }

        private IEnumerator WinSequence()
        {
            // Dark overlay fades in (world-space, confetti stays above it)
            if (vfxManager != null)
                vfxManager.ShowDarkOverlay(0.9f, 0.3f);

            // Show celebration UI (confetti is now UI-based inside CelebrationManager)
            if (celebrationManager != null)
                celebrationManager.Show();

            if (celebrationManager != null)
            {
                yield return new WaitForSeconds(celebrationManager.GetDuration());

                // Celebration UI animates out first
                float hideDuration = celebrationManager.Hide();
                yield return new WaitForSeconds(hideDuration);

                // Overlay fades out
                if (vfxManager != null)
                    vfxManager.HideDarkOverlay(0.3f);
                yield return new WaitForSeconds(0.3f);
            }
            else
            {
                yield return new WaitForSeconds(2f);
            }

            // Then exit animation
            yield return PlayExitAnimation();

            if (GameManager.Instance != null)
                GameManager.Instance.LoadMainScene();
        }

        private IEnumerator LoseSequence()
        {
            yield return new WaitForSeconds(0.5f);

            // Dark overlay behind popup
            if (vfxManager != null)
                vfxManager.ShowDarkOverlay(0.9f, 0.3f);

            if (failPopup != null)
            {
                failPopup.Show();
            }
            else
            {
                Debug.Log("Level lost — no FailPopup assigned.");
            }
        }

        // ───────────────────────── Scene Transitions ─────────────────────────

        private Vector2 originalTopBarAnchor;
        private Vector3 originalBoardPos;
        private Vector3 originalItemPos;

        private IEnumerator PlayEnterAnimation()
        {
            if (inputHandler != null)
                inputHandler.IsInputLocked = true;

            // Save original positions
            originalTopBarAnchor = topBarRect.anchoredPosition;
            originalBoardPos = boardParent.position;
            originalItemPos = itemParent.position;

            float offScreenY = -(Camera.main.orthographicSize + Height * cellSize);

            // Start off screen
            topBarRect.anchoredPosition = new Vector2(
                originalTopBarAnchor.x, originalTopBarAnchor.y + 800f);
            boardParent.position = new Vector3(
                originalBoardPos.x, offScreenY, originalBoardPos.z);
            itemParent.position = new Vector3(
                originalItemPos.x, offScreenY, originalItemPos.z);

            // Slide in simultaneously
            topBarRect.DOAnchorPosY(originalTopBarAnchor.y, 0.5f)
                .SetEase(Ease.InOutCubic);
            boardParent.DOMove(originalBoardPos, 0.5f)
                .SetEase(Ease.InOutCubic);
            itemParent.DOMove(originalItemPos, 0.5f)
                .SetEase(Ease.InOutCubic);

            yield return new WaitForSeconds(0.5f);

            // Landing squash — staggered row by row (bottom to top)
            for (int y = 0; y < Height; y++)
            {
                float delay = y * 0.02f;
                for (int x = 0; x < Width; x++)
                {
                    Cell cell = Grid[x, y];
                    if (cell.IsEmpty) continue;
                    cell.CurrentItem.transform.DOPunchScale(
                        new Vector3(0.1f, -0.1f, 0f), 0.15f, 4, 0.5f)
                        .SetDelay(delay);
                }
            }

            if (inputHandler != null)
                inputHandler.IsInputLocked = false;
        }

        private IEnumerator PlayExitAnimation()
        {
            float offScreenY = -(Camera.main.orthographicSize + Height * cellSize);

            // Slide out simultaneously
            topBarRect.DOAnchorPosY(originalTopBarAnchor.y + 800f, 0.4f)
                .SetEase(Ease.InOutCubic);
            boardParent.DOMoveY(offScreenY, 0.4f)
                .SetEase(Ease.InOutCubic);
            itemParent.DOMoveY(offScreenY, 0.4f)
                .SetEase(Ease.InOutCubic);

            yield return new WaitForSeconds(0.4f);
        }

        /// <summary>Called by FailPopup Close button.</summary>
        public void PlayExitAndLoadMainScene()
        {
            StartCoroutine(ExitThenLoad(loadMainScene: true));
        }

        /// <summary>Called by FailPopup Try Again button.</summary>
        public void PlayExitAndReloadLevel()
        {
            StartCoroutine(ExitThenLoad(loadMainScene: false));
        }

        private IEnumerator ExitThenLoad(bool loadMainScene)
        {
            // Hide dark overlay before exit animation
            if (vfxManager != null)
                vfxManager.HideDarkOverlay(0.3f);

            yield return PlayExitAnimation();

            if (GameManager.Instance != null)
            {
                if (loadMainScene)
                    GameManager.Instance.LoadMainScene();
                else
                    GameManager.Instance.LoadLevel(GameManager.Instance.CurrentLevel);
            }
        }

        // ───────────────────────── HUD ─────────────────────────

        private void UpdateHUD()
        {
            if (gameplayHUD == null) return;
            gameplayHUD.UpdateMoves(movesRemaining);
            gameplayHUD.UpdateGoals(CountRemainingObstacles());
        }

        private void ScaleItemToCell(BoardItem item)
        {
            SpriteRenderer sr = item.SpriteRenderer;
            if (sr == null || sr.sprite == null) return;

            float spriteWidth = sr.sprite.bounds.size.x;
            float scale = cellSize / spriteWidth * itemScaleFactor;
            item.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
