using System;
using UnityEngine;
using UnityEngine.UI;

namespace DreamGames.Match.UI
{
    /// <summary>
    /// "You lose" popup: close button returns to MainScene, try again replays the level.
    /// Root GameObject is expected to start inactive and only be shown via Show().
    /// </summary>
    public class FailPopupController : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Transform panel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button tryAgainButton;

        private Action onClose;
        private Action onTryAgain;

        private void Awake()
        {
            closeButton?.onClick.AddListener(() => onClose?.Invoke());
            tryAgainButton?.onClick.AddListener(() => onTryAgain?.Invoke());
            if (root != null) root.SetActive(false);
        }

        public void Show(Action onClose, Action onTryAgain)
        {
            this.onClose = onClose;
            this.onTryAgain = onTryAgain;
            if (root != null) root.SetActive(true);

            if (panel != null)
            {
                panel.localScale = Vector3.zero;
                Utils.TweenUtil.ScaleInBouncy(this, panel, Vector3.one, 0.4f);
            }
        }
    }
}
