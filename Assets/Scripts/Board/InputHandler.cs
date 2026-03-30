using UnityEngine;

namespace BlastGame.Board
{
    public class InputHandler : MonoBehaviour
    {
        [SerializeField] private BoardManager boardManager;

        public bool IsInputLocked { get; set; }

        private Camera mainCamera;

        private void Start()
        {
            mainCamera = Camera.main;
        }

        private void Update()
        {
            if (IsInputLocked) return;
            if (!Input.GetMouseButtonDown(0)) return;

            Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            worldPos.z = 0f;

            var gridPos = boardManager.WorldToGrid(worldPos);
            if (gridPos == null) return;

            var (x, y) = gridPos.Value;
            boardManager.OnCellTapped(x, y);
        }
    }
}
