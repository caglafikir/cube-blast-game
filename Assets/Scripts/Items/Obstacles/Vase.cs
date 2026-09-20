namespace DreamGames.Match.Items.Obstacles
{
    /// <summary>
    /// Takes damage from adjacent blasts or special explosions, falls like a cube,
    /// cleared after 2 damage instances.
    /// </summary>
    public class Vase : Obstacle
    {
        public override bool CanFall => true;
        protected override bool AcceptsAdjacentBlastDamage => true;
        protected override bool AcceptsSpecialExplosionDamage => true;

        private void Reset() => health = 2;
    }
}
