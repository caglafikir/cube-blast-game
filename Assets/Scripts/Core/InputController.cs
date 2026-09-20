using DreamGames.Match.Grid;
using DreamGames.Match.Items;
using UnityEngine;

namespace DreamGames.Match.Core
{
    /// <summary>
    /// Translates mouse/touch taps in LevelScene into GridItem.OnTap() calls.
    /// Uses grid-math (GridManager.TryWorldToCell) instead of physics raycasts/colliders,
    /// keeping input resolution independent of prefab collider setup.
    /// </summary>
    public class InputController : MonoBehaviour
    {
        [SerializeField] private GridManager grid;
        [SerializeField] private Camera worldCamera;

        private void Reset()
        {
            worldCamera = Camera.main;
        }

        private void Update()
        {
            if (grid == null || grid.Blast == null || grid.Blast.IsBusy) return;

            bool tapped = Input.GetMouseButtonDown(0);
#if UNITY_ANDROID || UNITY_IOS
            tapped = Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
            Vector3 screenPos = tapped ? (Vector3)Input.GetTouch(0).position : Vector3.zero;
#else
            Vector3 screenPos = Input.mousePosition;
#endif
            if (!tapped) return;

            Camera cam = worldCamera != null ? worldCamera : Camera.main;
            if (cam == null) return;

            Vector3 worldPos = cam.ScreenToWorldPoint(screenPos);
            worldPos.z = 0f;

            if (grid.TryWorldToCell(worldPos, out int row, out int col))
            {
                GridItem item = grid.GetItem(row, col);
                item?.OnTap();
            }
        }
    }
}
