namespace DreamGames.Match.Core
{
    /// <summary>Describes where a point of damage against an obstacle/special item came from.</summary>
    public enum DamageSourceType
    {
        AdjacentBlast,
        SpecialExplosion
    }

    public readonly struct DamageContext
    {
        public readonly DamageSourceType SourceType;

        /// <summary>For AdjacentBlast: size of the blasted cube group. For SpecialExplosion: always 1.</summary>
        public readonly int Amount;

        /// <summary>Id of the player action that caused this damage; lets multi-cell obstacles avoid double-counting one source hitting several of their cells.</summary>
        public readonly int SourceId;

        public DamageContext(DamageSourceType sourceType, int amount, int sourceId)
        {
            SourceType = sourceType;
            Amount = amount;
            SourceId = sourceId;
        }

        public static DamageContext AdjacentBlast(int groupSize, int sourceId) =>
            new DamageContext(DamageSourceType.AdjacentBlast, groupSize, sourceId);

        public static DamageContext SpecialExplosionCell(int sourceId) =>
            new DamageContext(DamageSourceType.SpecialExplosion, 1, sourceId);
    }
}
