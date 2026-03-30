using UnityEngine;
using BlastGame.Items;

namespace BlastGame.Board
{
    public class Cell : MonoBehaviour
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public BoardItem CurrentItem { get; private set; }
        public bool IsEmpty => CurrentItem == null;

        public void Initialize(int x, int y)
        {
            X = x;
            Y = y;
            gameObject.name = $"Cell_{x}_{y}";
        }

        public void SetItem(BoardItem item)
        {
            CurrentItem = item;
            if (item != null)
            {
                item.SetGridPosition(X, Y);
            }
        }

        public void ClearItem()
        {
            CurrentItem = null;
        }
    }
}
