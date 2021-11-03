using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using ChaoWorld.Core.Attributes;
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
            // Common Eggs
            [Description("White Egg")] [Price(4000)] WhiteEgg,
            [Description("Red Egg")] [Price(5000)] RedEgg,
            [Description("Yellow Egg")] [Price(5000)] YellowEgg,
            [Description("Blue Egg")] [Price(5000)] BlueEgg,
            [Description("Sky Blue Egg")] [Price(5000)] SkyBlueEgg,
            [Description("Pink Egg")] [Price(6000)] PinkEgg,
            [Description("Orange Egg")] [Price(6000)] OrangeEgg,
            [Description("Brown Egg")] [Price(8000)] BrownEgg,
            [Description("Purple Egg")] [Price(8000)] PurpleEgg,
            [Description("Green Egg")] [Price(8000)] GreenEgg,
            [Description("Grey Egg")] [Price(10000)] GreyEgg,
            [Description("Lime Green Egg")] [Price(15000)] LimeGreenEgg,
            [Description("Black Egg")] [Price(20000)] BlackEgg,

            // Uncommon Eggs
            [Description("Shiny White Egg")] [Price(40000)] ShinyWhiteEgg,
            [Description("Shiny Red Egg")] [Price(50000)] ShinyRedEgg,
            [Description("Shiny Yellow Egg")] [Price(50000)] ShinyYellowEgg,
            [Description("Shiny Blue Egg")] [Price(50000)] ShinyBlueEgg,
            [Description("Shiny Sky Blue Egg")] [Price(50000)] ShinySkyBlueEgg,
            [Description("Shiny Pink Egg")] [Price(60000)] ShinyPinkEgg,
            [Description("Shiny Orange Egg")] [Price(60000)] ShinyOrangeEgg,
            [Description("Shiny Brown Egg")] [Price(80000)] ShinyBrownEgg,
            [Description("Shiny Purple Egg")] [Price(80000)] ShinyPurpleEgg,
            [Description("Shiny Green Egg")] [Price(80000)] ShinyGreenEgg,
            [Description("Shiny Grey Egg")] [Price(100000)] ShinyGreyEgg,
            [Description("Shiny Lime Green Egg")] [Price(150000)] ShinyLimeGreenEgg,
            [Description("Shiny Black Egg")] [Price(200000)] ShinyBlackEgg,

            // Rare Eggs
            [Description("Amethyst Egg")] AmethystEgg,
            [Description("Aquamarine Egg")] AquamarineEgg,
            [Description("Emerald Egg")] EmeraldEgg,
            [Description("Garnet Egg")] GarnetEgg,
            [Description("Gold Egg")] GoldEgg,
            [Description("Onyx Egg")] OnyxEgg,
            [Description("Peridot Egg")] PeridotEgg,
            [Description("Ruby Egg")] RubyEgg,
            [Description("Sapphire Egg")] SapphireEgg,
            [Description("Silver Egg")] SilverEgg,
            [Description("Topaz Egg")] TopazEgg,
            [Description("Bronze Egg")] BronzeEgg,
            [Description("Moon Egg")] MoonEgg,

            // Non-Market Eggs
            [Description("Normal Egg")] NormalEgg,
            [Description("Shiny Normal Egg")] ShinyNormalEgg,
            [Description("Pearl Egg")] PearlEgg,
            [Description("Glass Egg")] GlassEgg,
            [Description("Metal Egg")] MetalEgg,
            [Description("Weird Egg")] TextureEgg,
            [Description("Strange White Egg")] WhiteTwoToneEgg,
            [Description("Strange Red Egg")] RedTwoToneEgg,
            [Description("Strange Yellow Egg")] YellowTwoToneEgg,
            [Description("Strange Blue Egg")] BlueTwoToneEgg,
            [Description("Strange Sky Blue Egg")] SkyBlueTwoToneEgg,
            [Description("Strange Pink Egg")] PinkTwoToneEgg,
            [Description("Strange Orange Egg")] OrangeTwoToneEgg,
            [Description("Strange Brown Egg")] BrownTwoToneEgg,
            [Description("Strange Purple Egg")] PurpleTwoToneEgg,
            [Description("Strange Green Egg")] GreenTwoToneEgg,
            [Description("Strange Grey Egg")] GreyTwoToneEgg,
            [Description("Strange Lime Green Egg")] LimeGreenTwoToneEgg,
            [Description("Strange Black Egg")] BlackTwoToneEgg,
            [Description("Colorful Egg")] MultiColorEgg,

            // Common Fruits
            [Description("Tasty Fruit")] [Price(80)] TastyFruit = 1000,
            [Description("Dark Fruit")] [Price(120)] DarkFruit,
            [Description("Hero Fruit")] [Price(120)] HeroFruit,
            [Description("Round Fruit")] [Price(80)] RoundFruit,
            [Description("Square Fruit")] [Price(80)] SquareFruit,
            [Description("Triangle Fruit")] [Price(80)] TriangleFruit,
            [Description("Chao Fruit")] [Price(5000)] ChaoFruit,
            [Description("Heart Fruit")] [Price(300)] HeartFruit,
            [Description("Mushroom")] [Price(300)] Mushroom,
            [Description("Strong Fruit")] [Price(500)] StrongFruit,
            [Description("Swim Fruit")] [Price(1000)] SwimFruit,
            [Description("Fly Fruit")] [Price(1000)] FlyFruit,
            [Description("Run Fruit")] [Price(1000)] RunFruit,
            [Description("Power Fruit")] [Price(1000)] PowerFruit,
            [Description("Smart Fruit")] [Price(1000)] SmartFruit,
            [Description("Lucky Mushroom")] [Price(1000)] LuckyMushroom,
            [Description("Mint Candy")] [Price(10000)] MintCandy,

            // Rare Fruits
            [Description("Hyper Swim Fruit")] [Price(100000)] HyperSwimFruit,
            [Description("Hyper Fly Fruit")] [Price(100000)] HyperFlyFruit,
            [Description("Hyper Run Fruit")] [Price(100000)] HyperRunFruit,
            [Description("Hyper Power Fruit")] [Price(100000)] HyperPowerFruit,
            [Description("Hyper Stamina Fruit")] [Price(100000)] HyperStaminaFruit,
            [Description("Hyper Smart Fruit")] [Price(100000)] HyperSmartFruit,
            [Description("Hyper Lucky Mushroom")] [Price(100000)] HyperLuckyMushroom,

            // Seeds
            [Description("Round Seed")] [Price(30000)] RoundSeed = 2000,
            [Description("Square Seed")] [Price(30000)] SquareSeed,
            [Description("Triangle Seed")] [Price(30000)] TriangleSeed,
            [Description("Hero Seed")] [Price(40000)] HeroSeed,
            [Description("Dark Seed")] [Price(40000)] DarkSeed,
            [Description("Tasty Seed")] [Price(20000)] TastySeed,
            [Description("Strong Seed")] [Price(50000)] StrongSeed,

            // Special
            [Description("Suspicious Potion")] [Price(30000)] SuspiciousPotion = 3000,
            [Description("Chaos Juice")] [Price(100000)] ChaosJuice,
            [Description("Negative Mirror")] [Price(10000)] NegativeMirror,
            [Description("Toy Parts")] [Price(100000)] ToyParts,

            // Non-Market Hats
            [Description("Normal Eggshell")] NormalEggshell = 4000,
            [Description("White Eggshell")] WhiteEggshell,
            [Description("Red Eggshell")] RedEggshell,
            [Description("Yellow Eggshell")] YellowEggshell,
            [Description("Blue Eggshell")] BlueEggshell,
            [Description("Sky Blue Eggshell")] SkyBlueEggshell,
            [Description("Pink Eggshell")] PinkEggshell,
            [Description("Orange Eggshell")] OrangeEggshell,
            [Description("Brown Eggshell")] BrownEggshell,
            [Description("Purple Eggshell")] PurpleEggshell,
            [Description("Green Eggshell")] GreenEggshell,
            [Description("Grey Eggshell")] GreyEggshell,
            [Description("Lime Green Eggshell")] LimeGreenEggshell,
            [Description("Black Eggshell")] BlackEggshell,
            [Description("Shiny Normal Eggshell")] ShinyNormalEggshell,
            [Description("Shiny White Eggshell")] ShinyWhiteEggshell,
            [Description("Shiny Red Eggshell")] ShinyRedEggshell,
            [Description("Shiny Yellow Eggshell")] ShinyYellowEggshell,
            [Description("Shiny Blue Eggshell")] ShinyBlueEggshell,
            [Description("Shiny Sky Blue Eggshell")] ShinySkyBlueEggshell,
            [Description("Shiny Pink Eggshell")] ShinyPinkEggshell,
            [Description("Shiny Orange Eggshell")] ShinyOrangeEggshell,
            [Description("Shiny Brown Eggshell")] ShinyBrownEggshell,
            [Description("Shiny Purple Eggshell")] ShinyPurpleEggshell,
            [Description("Shiny Green Eggshell")] ShinyGreenEggshell,
            [Description("Shiny Grey Eggshell")] ShinyGreyEggshell,
            [Description("Shiny Lime Green Eggshell")] ShinyLimeGreenEggshell,
            [Description("Shiny Black Eggshell")] ShinyBlackEggshell,
            [Description("Shiny Amethyst Eggshell")] ShinyAmethystEggshell,
            [Description("Shiny Aquamarine Eggshell")] ShinyAquamarineEggshell,
            [Description("Shiny Emerald Eggshell")] ShinyEmeraldEggshell,
            [Description("Shiny Garnet Eggshell")] ShinyGarnetEggshell,
            [Description("Shiny Gold Eggshell")] ShinyGoldEggshell,
            [Description("Shiny Onyx Eggshell")] ShinyOnyxEggshell,
            [Description("Shiny Peridot Eggshell")] ShinyPeridotEggshell,
            [Description("Shiny Ruby Eggshell")] ShinyRubyEggshell,
            [Description("Shiny Sapphire Eggshell")] ShinySapphireEggshell,
            [Description("Shiny Silver Eggshell")] ShinySilverEggshell,
            [Description("Shiny Topaz Eggshell")] ShinyTopazEggshell,
            [Description("Shiny Bronze Eggshell")] ShinyBronzeEggshell,
            [Description("Shiny Moon Eggshell")] ShinyMoonEggshell,

            // Market Hats
            [Description("Pumpkin")] Pumpkin = 5000,
            [Description("Skull")] Skull,
            [Description("Apple")] Apple,
            [Description("Cardboard Box")] CardboardBox,
            [Description("Pan")] Pan,
            [Description("Paper Bag")] PaperBag,
            [Description("Empty Can")] EmptyCan,
            [Description("Stump")] Stump,
            [Description("Flower Pot")] FlowerPot,
            [Description("Bucket")] Bucket,
            [Description("Watermelon")] Watermelon,
            [Description("Red Wool Beanie")] RedWoolBeanie,
            [Description("Blue Wool Beanie")] BlueWoolBeanie,
            [Description("Black Wool Beanie")] BlackWoolBeanie,

            // Lenses
            [Description("Normal Lens")] NormalLens = 10000,
            [Description("White Lens")] WhiteLens,
            [Description("Red Lens")] RedLens,
            [Description("Yellow Lens")] YellowLens,
            [Description("Blue Lens")] BlueLens,
            [Description("Sky Blue Lens")] SkyBlueLens,
            [Description("Pink Lens")] PinkLens,
            [Description("Orange Lens")] OrangeLens,
            [Description("Brown Lens")] BrownLens,
            [Description("Purple Lens")] PurpleLens,
            [Description("Green Lens")] GreenLens,
            [Description("Grey Lens")] GreyLens,
            [Description("Lime Green Lens")] LimeGreenLens,
            [Description("Black Lens")] BlackLens,
            [Description("Devil Lens")] DevilLens,
            [Description("Angel Lens")] AngelLens,
            [Description("Chaos Lens")] ChaosLens,
            [Description("Robot Lens")] RobotLens,

            // Placeholder
            [Description("???")] Placeholder = 1000000 // This is just for use as a boundary, nobody should have this in their inventory :^)
        }

        public static int GetPrice(ItemTypes value)
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null)
            {
                FieldInfo field = type.GetField(name);
                if (field != null)
                {
                    PriceAttribute attr =
                           Attribute.GetCustomAttribute(field,
                             typeof(PriceAttribute)) as PriceAttribute;
                    if (attr != null)
                    {
                        return attr.Price;
                    }
                }
            }
            return 5000000; // In case we mess up, make it stupid expensive so it's obvious and gets fixed
        }
    }

    public static class BlackMarket
    {
        public static Instant GetNewRefreshTime()
        {
            var now = SystemClock.Instance.GetCurrentInstant();
            return now.Plus(Duration.FromHours(1));
        }

        public static List<MarketItem> MakeListings()
        {
            var items = new List<MarketItem>();
            var commonEggLimit = new Random().Next(1, 5); // Always have at least one egg in the market
            var uncommonEggLimit = new Random().Next(1, 8) == 1 ? 1 : 0; // Only have shiny eggs available a few times a day
            var commonFruitLimit = new Random().Next(3, 5); // Always have some fruit on the market
            var rareFruitLimit = new Random().Next(1, 24) == 1 ? 1 : 0; // Only have hyper fruit available roughly once per day
            var specialLimit = new Random().Next(1, 24) == 1 ? 1 : 0; // Only have special items available roughly once per day
            
            // Disabled for now (until properly implemented)
            var seedLimit = 0;
            var hatLimit = 0;
            var lensLimit = 0;

            ListItemsForType(ItemBase.ItemCategories.Egg, GetAllCommonEggTypes(), items, commonEggLimit, 1);
            ListItemsForType(ItemBase.ItemCategories.Egg, GetAllUncommonEggTypes(), items, uncommonEggLimit, 1);
            ListItemsForType(ItemBase.ItemCategories.Fruit, GetAllCommonFruitTypes(), items, commonFruitLimit, 5);
            ListItemsForType(ItemBase.ItemCategories.Fruit, GetAllRareFruitTypes(), items, rareFruitLimit, 1);
            ListItemsForType(ItemBase.ItemCategories.Seed, GetAllSeedTypes(), items, seedLimit, 1);
            ListItemsForType(ItemBase.ItemCategories.Special, GetAllSpecialTypes(), items, specialLimit, 1);
            ListItemsForType(ItemBase.ItemCategories.Hat, GetAllHatTypes(), items, hatLimit, 1);
            ListItemsForType(ItemBase.ItemCategories.Lens, GetAllLensTypes(), items, lensLimit, 1);

            return items;
        }

        private static void ListItemsForType(ItemBase.ItemCategories category, List<ItemBase.ItemTypes> availableItems, List<MarketItem> listedItems, int limit, int quantity)
        {
            // Keep adding until we reach the limit
            while (limit > 0)
            {
                // Pick a random item from the pool
                var random = new Random();
                var index = random.Next(availableItems.Count);
                var item = availableItems[index];

                // Add it to the listings
                listedItems.Add(new MarketItem()
                {
                    ItemCategory = category,
                    ItemType = item,
                    Price = ItemBase.GetPrice(item),
                    Quantity = quantity
                });

                // Remove it from the pool and keep going
                availableItems.RemoveAt(index);
                limit--;
            }
        }

        private static List<ItemBase.ItemTypes> GetAllCommonEggTypes()
        {
            return GetItemTypesBetween(ItemBase.ItemTypes.WhiteEgg, ItemBase.ItemTypes.ShinyWhiteEgg);
        }

        private static List<ItemBase.ItemTypes> GetAllUncommonEggTypes()
        {
            return GetItemTypesBetween(ItemBase.ItemTypes.ShinyWhiteEgg, ItemBase.ItemTypes.AmethystEgg);
        }

        private static List<ItemBase.ItemTypes> GetAllRareEggTypes()
        {
            return GetItemTypesBetween(ItemBase.ItemTypes.AmethystEgg, ItemBase.ItemTypes.NormalEgg);
        }

        private static List<ItemBase.ItemTypes> GetAllCommonFruitTypes()
        {
            return GetItemTypesBetween(ItemBase.ItemTypes.TastyFruit, ItemBase.ItemTypes.HyperSwimFruit);
        }

        private static List<ItemBase.ItemTypes> GetAllRareFruitTypes()
        {
            return GetItemTypesBetween(ItemBase.ItemTypes.HyperSwimFruit, ItemBase.ItemTypes.RoundSeed);
        }

        private static List<ItemBase.ItemTypes> GetAllSeedTypes()
        {
            return GetItemTypesBetween(ItemBase.ItemTypes.RoundSeed, ItemBase.ItemTypes.SuspiciousPotion);
        }

        private static List<ItemBase.ItemTypes> GetAllSpecialTypes()
        {
            return GetItemTypesBetween(ItemBase.ItemTypes.SuspiciousPotion, ItemBase.ItemTypes.NormalEggshell);
        }

        private static List<ItemBase.ItemTypes> GetAllHatTypes()
        {
            return GetItemTypesBetween(ItemBase.ItemTypes.Pumpkin, ItemBase.ItemTypes.NormalLens);
        }

        private static List<ItemBase.ItemTypes> GetAllLensTypes()
        {
            return GetItemTypesBetween(ItemBase.ItemTypes.NormalLens, ItemBase.ItemTypes.Placeholder);
        }

        private static List<ItemBase.ItemTypes> GetItemTypesBetween(ItemBase.ItemTypes itemStart, ItemBase.ItemTypes itemEnd)
        {
            return Enum.GetValues(typeof(ItemBase.ItemTypes)).Cast<ItemBase.ItemTypes>().ToList()
                .Where(x => ((int)x) >= (int)itemStart)
                .Where(x => ((int)x) < (int)itemEnd)
                .ToList();
        }
    }
}