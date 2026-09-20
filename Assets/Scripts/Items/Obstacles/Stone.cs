namespace DreamGames.Match.Items.Obstacles
{
    /// <summary>
    /// Only damaged by special item explosions, never falls, cleared after 1 damage.
    /// </summary>
    public class Stone : Obstacle
    {
        public override bool CanFall => false;
        protected override bool AcceptsAdjacentBlastDamage => false;
        protected override bool AcceptsSpecialExplosionDamage => true;

        private void Reset() => health = 1;
    }
}
