using System.Collections.Generic;
using BlastGame.Data;
using BlastGame.Items;

namespace BlastGame.Board
{
    public static class MatchFinder
    {
        private static readonly int[] DX = { 0, 0, -1, 1 };
        private static readonly int[] DY = { -1, 1, 0, 0 };

        /// <summary>
        /// BFS flood-fill: returns all connected cells containing same-color Cubes.
        /// </summary>
        public static List<Cell> FindConnectedGroup(BoardManager board, int startX, int startY)
        {
            var result = new List<Cell>();

            Cell startCell = board.GetCell(startX, startY);
            if (startCell == null || startCell.IsEmpty) return result;
            if (startCell.CurrentItem.Type != ItemType.Cube) return result;

            CubeColor targetColor = ((Cube)startCell.CurrentItem).Color;

            var visited = new HashSet<Cell>();
            var queue = new Queue<Cell>();

            visited.Add(startCell);
            queue.Enqueue(startCell);

            while (queue.Count > 0)
            {
                Cell current = queue.Dequeue();
                result.Add(current);

                for (int d = 0; d < 4; d++)
                {
                    int nx = current.X + DX[d];
                    int ny = current.Y + DY[d];

                    Cell neighbor = board.GetCell(nx, ny);
                    if (neighbor == null || visited.Contains(neighbor)) continue;
                    if (neighbor.IsEmpty) continue;
                    if (neighbor.CurrentItem.Type != ItemType.Cube) continue;

                    Cube cube = (Cube)neighbor.CurrentItem;
                    if (cube.Color != targetColor) continue;

                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }

            return result;
        }

        /// <summary>
        /// Returns cells adjacent to the group but NOT in the group itself.
        /// Used for obstacle blast-adjacency damage.
        /// </summary>
        public static HashSet<Cell> GetAdjacentCells(BoardManager board, List<Cell> group)
        {
            var groupSet = new HashSet<Cell>(group);
            var adjacents = new HashSet<Cell>();

            foreach (Cell cell in group)
            {
                for (int d = 0; d < 4; d++)
                {
                    int nx = cell.X + DX[d];
                    int ny = cell.Y + DY[d];

                    Cell neighbor = board.GetCell(nx, ny);
                    if (neighbor == null) continue;
                    if (groupSet.Contains(neighbor)) continue;

                    adjacents.Add(neighbor);
                }
            }

            return adjacents;
        }
    }
}
