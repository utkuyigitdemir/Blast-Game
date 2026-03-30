using UnityEngine;

namespace BlastGame.UI
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(SpriteRenderer))]
    public class BackgroundScaler : MonoBehaviour
    {
        private void Start()
        {
            ScaleToFitCamera();
        }

        private void OnValidate()
        {
            ScaleToFitCamera();
        }

        private void ScaleToFitCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr.sprite == null) return;

            float orthoHeight = cam.orthographicSize * 2f;
            float orthoWidth = orthoHeight * cam.aspect;

            float spriteW = sr.sprite.bounds.size.x;
            float spriteH = sr.sprite.bounds.size.y;

            float scale = Mathf.Max(orthoWidth / spriteW, orthoHeight / spriteH);
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
