using System.Collections.Generic;
using BlastGame.Items;

namespace BlastGame.Board
{
    public struct FallMove
    {
        public BoardItem Item;
        public int X;
        public int FromY;
        public int ToY;
    }

    public static class GravitySystem
    {
        /// <summary>
        /// Processes gravity for the entire board. Non-falling obstacles split
        /// columns into independent segments. Returns moves for animation.
        /// </summary>
        public static List<FallMove> ProcessGravity(BoardManager board)
        {
            var moves = new List<FallMove>();

            for (int x = 0; x < board.Width; x++)
            {
                ProcessColumn(board, x, moves);
            }

            return moves;
        }

        private static void ProcessColumn(BoardManager board, int x, List<FallMove> moves)
        {
            // Find segment boundaries: non-falling obstacles split the column
            int segStart = 0;

            for (int y = 0; y <= board.Height; y++)
            {
                bool isBlocker = false;

                if (y < board.Height)
                {
                    Cell cell = board.GetCell(x, y);
                    if (!cell.IsEmpty && !cell.CurrentItem.CanFall())
                    {
                        isBlocker = true;
                    }
                }

                // End of column or hit a blocker → collapse the segment below
                if (y == board.Height || isBlocker)
                {
                    if (y > segStart)
                    {
                        CollapseSegment(board, x, segStart, y - 1, moves);
                    }
                    segStart = y + 1;
                }
            }
        }

        /// <summary>
        /// Collapses a single segment (segBottom..segTop inclusive).
        /// Falling items shift down to fill empty cells.
        /// </summary>
        private static void CollapseSegment(BoardManager board, int x,
            int segBottom, int segTop, List<FallMove> moves)
        {
            int writeY = segBottom;

            for (int readY = segBottom; readY <= segTop; readY++)
            {
                Cell readCell = board.GetCell(x, readY);
                if (readCell.IsEmpty) continue;

                BoardItem item = readCell.CurrentItem;

                if (!item.CanFall())
                {
                    // Shouldn't happen inside a segment, but safety guard
                    writeY = readY + 1;
                    continue;
                }

                if (readY != writeY)
                {
                    // Move item from readY to writeY
                    Cell writeCell = board.GetCell(x, writeY);
                    readCell.ClearItem();
                    writeCell.SetItem(item);

                    moves.Add(new FallMove
                    {
                        Item = item,
                        X = x,
                        FromY = readY,
                        ToY = writeY
                    });
                }

                writeY++;
            }
        }

        /// <summary>
        /// Returns the top of the uppermost segment in a column (the one connected
        /// to the top of the grid). Used by CubeSpawner to know where to fill.
        /// Also returns the bottom of that segment.
        /// </summary>
        public static void GetTopSegmentRange(BoardManager board, int x,
            out int segBottom, out int segTop)
        {
            segTop = board.Height - 1;
            segBottom = 0;

            // Scan from top down: the first non-falling obstacle marks the bottom
            for (int y = board.Height - 1; y >= 0; y--)
            {
                Cell cell = board.GetCell(x, y);
                if (!cell.IsEmpty && !cell.CurrentItem.CanFall())
                {
                    segBottom = y + 1;
                    break;
                }
            }
        }
    }
}
