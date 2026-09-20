using System.Collections.Generic;
using DreamGames.Match.Core;
using UnityEngine;

namespace DreamGames.Match.Items.Obstacles
{
    /// <summary>
    /// One of the four cells occupied by a Chalice Box (a 2x2 obstacle). See ChaliceBoxState
    /// for the shared damage/phase/goal logic; this class draws its own slice of the shared
    /// 10-cup shelf (2 rows of 5, split 3+2 across each row's two cells).
    /// </summary>
    public class ChaliceBoxPart : GridItem, IDamageable
    {
        private static readonly Dictionary<ChaliceBoxCorner, (int segmentTotal, int offset)> Segments =
            new Dictionary<ChaliceBoxCorner, (int, int)>
            {
                { ChaliceBoxCorner.TopLeft, (3, 0) },
                { ChaliceBoxCorner.TopRight, (2, 3) },
                { ChaliceBoxCorner.BottomLeft, (3, 5) },
                { ChaliceBoxCorner.BottomRight, (2, 8) },
            };

        public ChaliceBoxCorner Corner { get; private set; }
        public ChaliceBoxState State { get; private set; }

        [SerializeField] private SpriteRenderer spriteRenderer;

        public override bool CanFall => false;
        public override bool BlocksFall => true;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Setup(ChaliceBoxCorner corner, ChaliceBoxState state)
        {
            Corner = corner;
            State = state;
            state.RegisterPart(this);
            state.OnPhaseChanged += HandlePhaseChanged;
            state.OnDamaged += HandleDamaged;
        }

        public override void OnTap() { }

        public bool TakeDamage(DamageContext context)
        {
            State.ApplyDamage(context);

            if (State.Phase != ChaliceBoxPhase.Cleared)
            {
                Utils.TweenUtil.Punch(this, transform, strength: 1.15f, duration: 0.14f);
            }
            return State.Phase == ChaliceBoxPhase.Cleared;
        }

        private void HandlePhaseChanged(ChaliceBoxPhase phase)
        {
            if (phase == ChaliceBoxPhase.Chalice)
            {
                ApplyShelfSprite();
                Utils.TweenUtil.Punch(this, transform, strength: 1.2f);
            }
            else if (phase == ChaliceBoxPhase.Cleared)
            {
                RemoveFromGrid();
            }
        }

        private void HandleDamaged()
        {
            if (State.Phase == ChaliceBoxPhase.Chalice) ApplyShelfSprite();
        }

        private void ApplyShelfSprite()
        {
            if (spriteRenderer == null || Grid == null) return;
            if (!Segments.TryGetValue(Corner, out var segment)) return;

            int collectedInSegment = Mathf.Clamp(State.ChaliceCollected - segment.offset, 0, segment.segmentTotal);
            int remaining = segment.segmentTotal - collectedInSegment;

            Sprite[] set = GetSpriteSet();
            if (set != null && remaining >= 0 && remaining < set.Length && set[remaining] != null)
            {
                spriteRenderer.sprite = set[remaining];
            }
        }

        private Sprite[] GetSpriteSet()
        {
            switch (Corner)
            {
                case ChaliceBoxCorner.TopLeft: return Grid.Prefabs.chaliceTopLeftSprites;
                case ChaliceBoxCorner.TopRight: return Grid.Prefabs.chaliceTopRightSprites;
                case ChaliceBoxCorner.BottomLeft: return Grid.Prefabs.chaliceBottomLeftSprites;
                case ChaliceBoxCorner.BottomRight: return Grid.Prefabs.chaliceBottomRightSprites;
                default: return null;
            }
        }

        private void OnDestroy()
        {
            if (State == null) return;
            State.OnPhaseChanged -= HandlePhaseChanged;
            State.OnDamaged -= HandleDamaged;
        }
    }
}
