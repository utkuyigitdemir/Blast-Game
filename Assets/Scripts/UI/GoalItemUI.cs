using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BlastGame.UI
{
    public class GoalItemUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private Image checkImage;

        public Image IconImage => iconImage;
        public TextMeshProUGUI CountText => countText;
        public Image CheckImage => checkImage;

        public void SetIcon(Sprite sprite)
        {
            iconImage.sprite = sprite;
        }

        public void SetCount(int count)
        {
            countText.text = count.ToString();
        }

        public void ShowCompleted()
        {
            countText.gameObject.SetActive(false);
            checkImage.gameObject.SetActive(true);
        }

        public void ShowCount(int count)
        {
            countText.text = count.ToString();
            countText.gameObject.SetActive(true);
            checkImage.gameObject.SetActive(false);
        }
    }
}
