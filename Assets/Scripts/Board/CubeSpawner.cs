using System.Collections.Generic;
using BlastGame.Items;

namespace BlastGame.Board
{
    public struct SpawnMove
    {
        public BoardItem Item;
        public int X;
        public int TargetY;
        public int SpawnOffsetFromTop; // how many cells above the grid top to start
    }

    public class CubeSpawner
    {
        private readonly ItemFactory itemFactory;
        private readonly BoardManager board;

        public CubeSpawner(ItemFactory itemFactory, BoardManager board)
        {
            this.itemFactory = itemFactory;
            this.board = board;
        }

        /// <summary>
        /// Fills empty cells in the top segment of each column with random cubes.
        /// Returns spawn info for animation (items start above the grid and fall in).
        /// </summary>
        public List<SpawnMove> SpawnNewCubes()
        {
            var moves = new List<SpawnMove>();

            for (int x = 0; x < board.Width; x++)
            {
                SpawnColumn(x, moves);
            }

            return moves;
        }

        private void SpawnColumn(int x, List<SpawnMove> moves)
        {
            GravitySystem.GetTopSegmentRange(board, x, out int segBottom, out int segTop);

            int spawnIndex = 0;

            for (int y = segBottom; y <= segTop; y++)
            {
                Cell cell = board.GetCell(x, y);
                if (!cell.IsEmpty) continue;

                Cube cube = itemFactory.CreateRandomCube();
                board.PlaceItem(cube, x, y);

                moves.Add(new SpawnMove
                {
                    Item = cube,
                    X = x,
                    TargetY = y,
                    SpawnOffsetFromTop = spawnIndex + 1
                });

                spawnIndex++;
            }
        }
    }
}
