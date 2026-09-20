using DreamGames.Match.Items;
using DreamGames.Match.Items.Obstacles;
using DreamGames.Match.Items.SpecialItems;
using UnityEngine;

namespace DreamGames.Match.Grid
{
    /// <summary>Holds every prefab/sprite reference GridManager needs to spawn items.</summary>
    [System.Serializable]
    public class ItemPrefabRegistry
    {
        [Header("Cubes")]
        public Cube cubePrefab;
        public Sprite redSprite, greenSprite, blueSprite, yellowSprite;
        public Sprite hRocketHintSprite, vRocketHintSprite, tntHintSprite;

        [Header("Special Items")]
        public Rocket rocketPrefab;
        public Sprite horizontalRocketSprite, verticalRocketSprite;
        public Tnt tntPrefab;
        public Sprite tntSprite;

        [Header("Obstacles")]
        public Vase vasePrefab;
        public Sprite vaseSprite;
        public Stone stonePrefab;
        public Sprite stoneSprite;
        public ChaliceBoxPart chaliceBoxPartPrefab;
        public Sprite chaliceDoorSprite;

        /// <summary>Sprites per corner of the shared 2x2 chalice cabinet; index = cups still owed in that slice.</summary>
        public Sprite[] chaliceTopLeftSprites;
        public Sprite[] chaliceTopRightSprites;
        public Sprite[] chaliceBottomLeftSprites;
        public Sprite[] chaliceBottomRightSprites;

        [Header("VFX")]
        public ParticleSystem blastBurstPrefab;
        public ParticleSystem explosionBurstPrefab;

        public Sprite SpriteFor(Core.CubeColor color)
        {
            switch (color)
            {
                case Core.CubeColor.Red: return redSprite;
                case Core.CubeColor.Green: return greenSprite;
                case Core.CubeColor.Blue: return blueSprite;
                case Core.CubeColor.Yellow: return yellowSprite;
                default: return redSprite;
            }
        }
    }
}
