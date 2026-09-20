using System;
using System.Collections.Generic;
using DreamGames.Match.Core;
using UnityEngine;

namespace DreamGames.Match.Items.Obstacles
{
    public enum ChaliceBoxPhase
    {
        Door,
        Chalice,
        Cleared
    }

    /// <summary>
    /// Shared logic for one 2x2 Chalice Box. The four ChaliceBoxPart instances (one per grid
    /// cell) forward damage here so the box behaves as a single entity across its 4 cells.
    /// Door phase: any source deals 1 damage. Chalice phase: special explosions deal damage
    /// equal to affected cells, adjacent blasts deal damage equal to group size; each point
    /// collects 1 chalice, goal is 10.
    /// </summary>
    public class ChaliceBoxState
    {
        public ChaliceBoxPhase Phase { get; private set; } = ChaliceBoxPhase.Door;

        private int doorHealth;
        private readonly int chaliceGoal;
        private int chaliceCollected;

        // A single source only counts once, even if it touches multiple corners.
        private int lastDoorSourceId = int.MinValue;
        private int lastAdjacentBlastSourceId = int.MinValue;

        // The source that broke the door shouldn't also collect chalices in the same move.
        private int doorBrokenBySourceId = int.MinValue;

        public readonly List<ChaliceBoxPart> Parts = new List<ChaliceBoxPart>(4);

        public event Action OnCleared;
        public event Action OnDamaged;
        public event Action<ChaliceBoxPhase> OnPhaseChanged;

        public ChaliceBoxState(int doorHealth = 1, int chaliceGoal = 10)
        {
            this.doorHealth = Mathf.Max(1, doorHealth);
            this.chaliceGoal = Mathf.Max(1, chaliceGoal);
        }

        public void RegisterPart(ChaliceBoxPart part) => Parts.Add(part);

        public void ApplyDamage(DamageContext context)
        {
            if (Phase == ChaliceBoxPhase.Cleared) return;

            if (Phase == ChaliceBoxPhase.Door)
            {
                if (context.SourceId == lastDoorSourceId) return;
                lastDoorSourceId = context.SourceId;

                doorHealth -= 1;
                OnDamaged?.Invoke();

                if (doorHealth <= 0)
                {
                    doorBrokenBySourceId = context.SourceId;
                    Phase = ChaliceBoxPhase.Chalice;
                    OnPhaseChanged?.Invoke(Phase);
                }
                return;
            }

            if (context.SourceId == doorBrokenBySourceId) return;

            int amount = context.Amount;
            if (context.SourceType == DamageSourceType.AdjacentBlast)
            {
                if (context.SourceId == lastAdjacentBlastSourceId) return;
                lastAdjacentBlastSourceId = context.SourceId;
            }

            chaliceCollected = Mathf.Min(chaliceGoal, chaliceCollected + amount);
            OnDamaged?.Invoke();

            if (chaliceCollected >= chaliceGoal)
            {
                Phase = ChaliceBoxPhase.Cleared;
                OnPhaseChanged?.Invoke(Phase);
                OnCleared?.Invoke();
            }
        }

        public float ChaliceProgress01 => chaliceGoal <= 0 ? 0f : (float)chaliceCollected / chaliceGoal;
        public int ChaliceGoal => chaliceGoal;
        public int ChaliceCollected => chaliceCollected;
    }
}
