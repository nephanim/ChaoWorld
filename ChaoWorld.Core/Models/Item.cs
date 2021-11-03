using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NodaTime;
using NodaTime.Text;

namespace ChaoWorld.Core
{
    public class MarketItem : ItemBase
    {
        public int Price { get; set; }
    }

    public class Item : ItemBase
    {
        public long Id { get; set; }
        public int GardenId { get; set; }
        public Instant CreatedOn { get; set; }
    }

    public class ItemBase
    {
        public int CategoryId { get; set; }
        public ItemCategories ItemCategory
        {
            get
            {
                return (ItemCategories)CategoryId;
            }
            set
            {
                CategoryId = (int)value;
            }
        }
        public int TypeId { get; set; }
        public ItemTypes ItemType
        {
            get
            {
                return (ItemTypes)TypeId;
            }
            set
            {
                TypeId = (int)value;
            }
        }
        public int Quantity { get; set; }

        public enum ItemCategories
        {
            [Description("Eggs")] Egg,
            [Description("Fruits")] Fruit,
            [Description("Seeds")] Seed,
            [Description("Special")] Special,
            [Description("Hats")] Hat,
            [Description("Accessories")] Accessory,
            [Description("Clothing")] Clothing,
            [Description("Lenses")] Lens
        }
        
        public enum ItemTypes
        {
            // Eggs
            [Description("Normal Egg")] NormalEgg, [Description("White Egg")] WhiteEgg, [Description("Red Egg")] RedEgg, [Description("Yellow Egg")] YellowEgg,
            [Description("Blue Egg")] BlueEgg, [Description("Sky Blue Egg")] SkyBlueEgg, [Description("Pink Egg")] PinkEgg, [Description("Orange Egg")] OrangeEgg,
            [Description("Brown Egg")] BrownEgg, [Description("Purple Egg")] PurpleEgg, [Description("Green Egg")] GreenEgg, [Description("Grey Egg")] GreyEgg,
            [Description("Lime Green Egg")] LimeGreenEgg, [Description("Black Egg")] BlackEgg,
            [Description("Shiny Normal Egg")] ShinyNormalEgg, [Description("Shiny White Egg")] ShinyWhiteEgg, [Description("Shiny Red Egg")] ShinyRedEgg,
            [Description("Shiny Yellow Egg")] ShinyYellowEgg, [Description("Shiny Blue Egg")] ShinyBlueEgg, [Description("Shiny Sky Blue Egg")] ShinySkyBlueEgg,
            [Description("Shiny Pink Egg")] ShinyPinkEgg, [Description("Shiny Orange Egg")] ShinyOrangeEgg, [Description("Shiny Brown Egg")] ShinyBrownEgg,
            [Description("Shiny Purple Egg")] ShinyPurpleEgg, [Description("Shiny Green Egg")] ShinyGreenEgg, [Description("Shiny Grey Egg")] ShinyGreyEgg,
            [Description("Shiny Lime Green Egg")] ShinyLimeGreenEgg, [Description("Shiny Black Egg")] ShinyBlackEgg,
            [Description("Amethyst Egg")] AmethystEgg, [Description("Aquamarine Egg")] AquamarineEgg, [Description("Emerald Egg")] EmeraldEgg,
            [Description("Garnet Egg")] GarnetEgg, [Description("Gold Egg")] GoldEgg, [Description("Onyx Egg")] OnyxEgg, [Description("Peridot Egg")] PeridotEgg,
            [Description("Ruby Egg")] RubyEgg, [Description("Sapphire Egg")] SapphireEgg, [Description("Silver Egg")] SilverEgg, [Description("Topaz Egg")] TopazEgg,
            [Description("Bronze Egg")] BronzeEgg, [Description("Moon Egg")] MoonEgg,

            // Fruits
            [Description("Garden Fruit")] GardenFruit = 1000, [Description("Dark Fruit")] DarkFruit, [Description("Hero Fruit")] HeroFruit,
            [Description("Round Fruit")] RoundFruit, [Description("Square Fruit")] SquareFruit, [Description("Triangle Fruit")] TriangleFruit,
            [Description("Chao Fruit")] ChaoFruit, [Description("Heart Fruit")] HeartFruit, [Description("Mushroom")] Mushroom,
            [Description("Tasty Fruit")] TastyFruit, [Description("Strong Fruit")] StrongFruit,
            [Description("Swim Fruit")] SwimFruit, [Description("Fly Fruit")] FlyFruit, [Description("Run Fruit")] RunFruit,
            [Description("Power Fruit")] PowerFruit, [Description("Smart Fruit")] SmartFruit, [Description("Lucky Mushroom")] LuckyMushroom,
            [Description("Hyper Swim Fruit")] HyperSwimFruit, [Description("Hyper Fly Fruit")] HyperFlyFruit, [Description("Hyper Run Fruit")] HyperRunFruit,
            [Description("Hyper Power Fruit")] HyperPowerFruit, [Description("Hyper Stamina Fruit")] HyperStaminaFruit,
            [Description("Hyper Smart Fruit")] HyperSmartFruit, [Description("Hyper Lucky Mushroom")] HyperLuckyMushroom,

            // Seeds
            [Description("Round Seed")] RoundSeed = 2000, [Description("Square Seed")] SquareSeed, [Description("Triangle Seed")] TriangleSeed,
            [Description("Hero Seed")] HeroSeed, [Description("Dark Seed")] DarkSeed, [Description("Tasty Seed")] TastySeed, [Description("Strong Seed")] StrongSeed,

            // Special
            [Description("Suspicious Potion")] SuspiciousPotion = 3000, [Description("Chaos Juice")] ChaosJuice, [Description("Negative Mirror")] NegativeMirror,
            [Description("Toy Parts")] ToyParts,

            // Hats
            [Description("Normal Eggshell")] NormalEggshell = 4000, [Description("White Eggshell")] WhiteEggshell, [Description("Red Eggshell")] RedEggshell,
            [Description("Yellow Eggshell")] YellowEggshell, [Description("Blue Eggshell")] BlueEggshell, [Description("Sky Blue Eggshell")] SkyBlueEggshell,
            [Description("Pink Eggshell")] PinkEggshell, [Description("Orange Eggshell")] OrangeEggshell, [Description("Brown Eggshell")] BrownEggshell,
            [Description("Purple Eggshell")] PurpleEggshell, [Description("Green Eggshell")] GreenEggshell, [Description("Grey Eggshell")] GreyEggshell,
            [Description("Lime Green Eggshell")] LimeGreenEggshell, [Description("Black Eggshell")] BlackEggshell,
            [Description("Shiny Normal Eggshell")] ShinyNormalEggshell, [Description("Shiny White Eggshell")] ShinyWhiteEggshell,
            [Description("Shiny Red Eggshell")] ShinyRedEggshell, [Description("Shiny Yellow Eggshell")] ShinyYellowEggshell,
            [Description("Shiny Blue Eggshell")] ShinyBlueEggshell, [Description("Shiny Sky Blue Eggshell")] ShinySkyBlueEggshell,
            [Description("Shiny Pink Eggshell")] ShinyPinkEggshell, [Description("Shiny Orange Eggshell")] ShinyOrangeEggshell,
            [Description("Shiny Brown Eggshell")] ShinyBrownEggshell, [Description("Shiny Purple Eggshell")] ShinyPurpleEggshell,
            [Description("Shiny Green Eggshell")] ShinyGreenEggshell, [Description("Shiny Grey Eggshell")] ShinyGreyEggshell,
            [Description("Shiny Lime Green Eggshell")] ShinyLimeGreenEggshell, [Description("Shiny Black Eggshell")] ShinyBlackEggshell,
            [Description("Shiny Amethyst Eggshell")] ShinyAmethystEggshell, [Description("Shiny Aquamarine Eggshell")] ShinyAquamarineEggshell,
            [Description("Shiny Emerald Eggshell")] ShinyEmeraldEggshell, [Description("Shiny Garnet Eggshell")] ShinyGarnetEggshell,
            [Description("Shiny Gold Eggshell")] ShinyGoldEggshell, [Description("Shiny Onyx Eggshell")] ShinyOnyxEggshell,
            [Description("Shiny Peridot Eggshell")] ShinyPeridotEggshell, [Description("Shiny Ruby Eggshell")] ShinyRubyEggshell,
            [Description("Shiny Sapphire Eggshell")] ShinySapphireEggshell, [Description("Shiny Silver Eggshell")] ShinySilverEggshell,
            [Description("Shiny Topaz Eggshell")] ShinyTopazEggshell, [Description("Shiny Bronze Eggshell")] ShinyBronzeEggshell,
            [Description("Shiny Moon Eggshell")] ShinyMoonEggshell,

            [Description("Pumpkin")] Pumpkin = 5000, [Description("Skull")] Skull, [Description("Apple")] Apple, [Description("Cardboard Box")] CardboardBox,
            [Description("Pan")] Pan, [Description("Paper Bag")] PaperBag, [Description("Empty Can")] EmptyCan, [Description("Stump")] Stump,
            [Description("Flower Pot")] FlowerPot, [Description("Bucket")] Bucket, [Description("Watermelon")] Watermelon,
            [Description("Red Wool Beanie")] RedWoolBeanie, [Description("Blue Wool Beanie")] BlueWoolBeanie, [Description("Black Wool Beanie")] BlackWoolBeanie,

            // Lenses
            [Description("Normal Lens")] NormalLens = 10000, [Description("White Lens")] WhiteLens, [Description("Red Lens")] RedLens,
            [Description("Yellow Lens")] YellowLens, [Description("Blue Lens")] BlueLens, [Description("Sky Blue Lens")] SkyBlueLens,
            [Description("Pink Lens")] PinkLens, [Description("Orange Lens")] OrangeLens, [Description("Brown Lens")] BrownLens,
            [Description("Purple Lens")] PurpleLens, [Description("Green Lens")] GreenLens, [Description("Grey Lens")] GreyLens,
            [Description("Lime Green Lens")] LimeGreenLens, [Description("Black Lens")] BlackLens,
            [Description("Devil Lens")] DevilLens, [Description("Angel Lens")] AngelLens, [Description("Chaos Lens")] ChaosLens, [Description("Robot Lens")] RobotLens
        }
    }

    public static class BlackMarket
    {
        public static Instant GetNewRefreshTime()
        {
            var now = SystemClock.Instance.GetCurrentInstant();
            return now.Plus(Duration.FromHours(1));
        }

        // TODO: Actually figure out what to list
        public static List<MarketItem> MakeListings()
        {
            var items = new List<MarketItem>();
            items.Add(new MarketItem
            {
                ItemCategory = ItemBase.ItemCategories.Egg,
                ItemType = ItemBase.ItemTypes.NormalEgg,
                Quantity = 1,
                Price = 0
            });
            return items;
        }
    }
}