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

    }

    public class Item : ItemBase
    {
        public long Id { get; set; }
        public int GardenId { get; set; }
        public Instant CreatedOn { get; set; }
    }

    public class ItemBase
    {
        public int TypeId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int CategoryId { get; set; }
        public ItemCategories Category
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
        public int EffectTypeId { get; set; }
        public ItemEffects EffectType
        {
            get
            {
                return (ItemEffects)EffectTypeId;
            }
            set
            {
                EffectTypeId = (int)value;
            }
        }
        public bool IsMarketEnabled { get; set; }
        public int? MarketPrice { get; set; }
        public bool? IsShiny { get; set; }
        public bool? IsTwoTone { get; set; }
        public int? PrimaryColorId { get; set; }
        public Chao.Colors? PrimaryColor
        {
            get
            {
                return PrimaryColorId.HasValue
                    ? (Chao.Colors)PrimaryColorId
                    : null;
            }
            set
            {
                PrimaryColorId = (int)value;
            }
        }
        public int? SecondaryColorId { get; set; }
        public Chao.Colors? SecondaryColor
        {
            get
            {
                return SecondaryColorId.HasValue
                    ? (Chao.Colors)SecondaryColorId
                    : null;
            }
            set
            {
                SecondaryColorId = (int)value;
            }
        }
        public int? GrowsFruitId { get; set; }
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

        public enum ItemEffects
        {
            None,
            NewChao,
            NewOmochao,
            SwimProgressIncrease,
            SwimGradeIncrease,
            FlyProgressIncrease,
            FlyGradeIncrease,
            RunProgressIncrease,
            RunGradeIncrease,
            PowerProgressIncrease,
            PowerGradeIncrease,
            StaminaProgressIncrease,
            StaminaGradeIncrease,
            IntelligenceProgressIncrease,
            IntelligenceGradeIncrease,
            LuckProgressIncrease,
            LuckGradeIncrease,
            AllStatsProgressIncrease,
            Mushroom,
            HeroAlignment,
            DarkAlignment,
            AllowMating,
            Reincarnation,
            ReincarnationFactorIncrease, // No longer used - replaced by individual factor increases
            Negativity,
            NewTree,
            Equipment,
            ChangeEyeColor,
            SwimFactorIncrease,
            FlyFactorIncrease,
            RunFactorIncrease,
            PowerFactorIncrease,
            StaminaFactorIncrease,
            IntelligenceFactorIncrease,
            LuckFactorIncrease
        }
        
        public enum ItemTypes
        {
            // Common Eggs
            [ItemCategory(ItemCategories.Egg)] [Description("White Egg")] [Price(4000)] [PrimaryColor(Chao.Colors.White)] WhiteEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Red Egg")] [Price(5000)] [PrimaryColor(Chao.Colors.Red)] RedEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Yellow Egg")] [Price(5000)] [PrimaryColor(Chao.Colors.Yellow)] YellowEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Blue Egg")] [Price(5000)] [PrimaryColor(Chao.Colors.Blue)] BlueEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Sky Blue Egg")] [Price(5000)] [PrimaryColor(Chao.Colors.SkyBlue)] SkyBlueEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Pink Egg")] [Price(6000)] [PrimaryColor(Chao.Colors.Pink)] PinkEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Orange Egg")] [Price(6000)] [PrimaryColor(Chao.Colors.Orange)] OrangeEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Brown Egg")] [Price(8000)] [PrimaryColor(Chao.Colors.Brown)] BrownEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Purple Egg")] [Price(8000)] [PrimaryColor(Chao.Colors.Purple)] PurpleEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Green Egg")] [Price(8000)] [PrimaryColor(Chao.Colors.Green)] GreenEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Grey Egg")] [Price(10000)] [PrimaryColor(Chao.Colors.Grey)] GreyEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Lime Green Egg")] [Price(15000)] [PrimaryColor(Chao.Colors.LimeGreen)] LimeGreenEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Black Egg")] [Price(20000)] [PrimaryColor(Chao.Colors.Black)] BlackEgg,

            // Uncommon Eggs
            [ItemCategory(ItemCategories.Egg)] [Description("Shiny White Egg")] [Price(40000)] [PrimaryColor(Chao.Colors.White)] [Shiny] ShinyWhiteEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Shiny Red Egg")] [Price(50000)] [PrimaryColor(Chao.Colors.Red)] [Shiny] ShinyRedEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Shiny Yellow Egg")] [Price(50000)] [PrimaryColor(Chao.Colors.Yellow)] [Shiny] ShinyYellowEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Shiny Blue Egg")] [Price(50000)] [PrimaryColor(Chao.Colors.Blue)] [Shiny] ShinyBlueEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Shiny Sky Blue Egg")] [Price(50000)] [PrimaryColor(Chao.Colors.SkyBlue)] [Shiny] ShinySkyBlueEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Shiny Pink Egg")] [Price(60000)] [PrimaryColor(Chao.Colors.Pink)] [Shiny] ShinyPinkEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Shiny Orange Egg")] [Price(60000)] [PrimaryColor(Chao.Colors.Orange)] [Shiny] ShinyOrangeEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Shiny Brown Egg")] [Price(80000)] [PrimaryColor(Chao.Colors.Brown)] [Shiny] ShinyBrownEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Shiny Purple Egg")] [Price(80000)] [PrimaryColor(Chao.Colors.Purple)] [Shiny] ShinyPurpleEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Shiny Green Egg")] [Price(80000)] [PrimaryColor(Chao.Colors.Green)] [Shiny] ShinyGreenEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Shiny Grey Egg")] [Price(100000)] [PrimaryColor(Chao.Colors.Grey)] [Shiny] ShinyGreyEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Shiny Lime Green Egg")] [Price(150000)] [PrimaryColor(Chao.Colors.LimeGreen)] [Shiny] ShinyLimeGreenEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Shiny Black Egg")] [Price(200000)] [PrimaryColor(Chao.Colors.Black)] [Shiny] ShinyBlackEgg,

            // Rare Eggs
            [ItemCategory(ItemCategories.Egg)] [Description("Amethyst Egg")] [PrimaryColor(Chao.Colors.Amethyst)] AmethystEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Aquamarine Egg")] [PrimaryColor(Chao.Colors.Aquamarine)] AquamarineEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Emerald Egg")] [PrimaryColor(Chao.Colors.Emerald)] EmeraldEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Garnet Egg")] [PrimaryColor(Chao.Colors.Garnet)] GarnetEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Gold Egg")] [PrimaryColor(Chao.Colors.Gold)] GoldEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Onyx Egg")] [PrimaryColor(Chao.Colors.Onyx)] OnyxEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Peridot Egg")] [PrimaryColor(Chao.Colors.Peridot)] PeridotEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Ruby Egg")] [PrimaryColor(Chao.Colors.Ruby)] RubyEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Sapphire Egg")] [PrimaryColor(Chao.Colors.Sapphire)] SapphireEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Silver Egg")] [PrimaryColor(Chao.Colors.Silver)] SilverEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Topaz Egg")] [PrimaryColor(Chao.Colors.Topaz)] TopazEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Bronze Egg")] [PrimaryColor(Chao.Colors.Bronze)] BronzeEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Moon Egg")] [PrimaryColor(Chao.Colors.Moon)] MoonEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Toy Parts")] [Price(100000)] ToyParts, // TODO: Decide how to handle Omochao

            // Non-Market Eggs
            [ItemCategory(ItemCategories.Egg)] [Description("Normal Egg")] [PrimaryColor(Chao.Colors.Normal)] NormalEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Shiny Normal Egg")] [PrimaryColor(Chao.Colors.Normal)] [Shiny] ShinyNormalEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Pearl Egg")] [PrimaryColor(Chao.Colors.Pearl)] PearlEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Glass Egg")] [PrimaryColor(Chao.Colors.Glass)] GlassEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Metal Egg")] [PrimaryColor(Chao.Colors.Metal)] MetalEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Weird Egg")] [PrimaryColor(Chao.Colors.Normal)] TextureEgg, // TODO: Decide how to handle texture chao - just make each their own eggs?
            [ItemCategory(ItemCategories.Egg)] [Description("Normal Two-Tone Egg")] [PrimaryColor(Chao.Colors.Normal)] [TwoTone] NormalTwoToneEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Strange White Egg")] [PrimaryColor(Chao.Colors.White)] [TwoTone] WhiteTwoToneEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Strange Red Egg")] [PrimaryColor(Chao.Colors.Red)] [TwoTone] RedTwoToneEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Strange Yellow Egg")] [PrimaryColor(Chao.Colors.Yellow)] [TwoTone] YellowTwoToneEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Strange Blue Egg")] [PrimaryColor(Chao.Colors.Blue)] [TwoTone] BlueTwoToneEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Strange Sky Blue Egg")] [PrimaryColor(Chao.Colors.SkyBlue)] [TwoTone] SkyBlueTwoToneEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Strange Pink Egg")] [PrimaryColor(Chao.Colors.Pink)] [TwoTone] PinkTwoToneEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Strange Orange Egg")] [PrimaryColor(Chao.Colors.Orange)] [TwoTone] OrangeTwoToneEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Strange Brown Egg")] [PrimaryColor(Chao.Colors.Brown)] [TwoTone] BrownTwoToneEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Strange Purple Egg")] [PrimaryColor(Chao.Colors.Purple)] [TwoTone] PurpleTwoToneEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Strange Green Egg")] [PrimaryColor(Chao.Colors.Green)] [TwoTone] GreenTwoToneEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Strange Grey Egg")] [PrimaryColor(Chao.Colors.Grey)] [TwoTone] GreyTwoToneEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Strange Lime Green Egg")] [PrimaryColor(Chao.Colors.LimeGreen)] [TwoTone] LimeGreenTwoToneEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Strange Black Egg")] [PrimaryColor(Chao.Colors.Black)] [TwoTone] BlackTwoToneEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Strange Shiny White Egg")] [PrimaryColor(Chao.Colors.White)] [TwoTone] ShinyWhiteTwoToneEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Strange Shiny Red Egg")] [PrimaryColor(Chao.Colors.Red)] [TwoTone] ShinyRedTwoToneEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Strange Shiny Yellow Egg")] [PrimaryColor(Chao.Colors.Yellow)] [TwoTone] ShinyYellowTwoToneEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Strange Shiny Blue Egg")] [PrimaryColor(Chao.Colors.Blue)] [TwoTone] ShinyBlueTwoToneEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Strange Shiny Sky Blue Egg")] [PrimaryColor(Chao.Colors.SkyBlue)] [TwoTone] ShinySkyBlueTwoToneEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Strange Shiny Pink Egg")] [PrimaryColor(Chao.Colors.Pink)] [TwoTone] ShinyPinkTwoToneEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Strange Shiny Orange Egg")] [PrimaryColor(Chao.Colors.Orange)] [TwoTone] ShinyOrangeTwoToneEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Strange Shiny Brown Egg")] [PrimaryColor(Chao.Colors.Brown)] [TwoTone] ShinyBrownTwoToneEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Strange Shiny Purple Egg")] [PrimaryColor(Chao.Colors.Purple)] [TwoTone] ShinyPurpleTwoToneEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Strange Shiny Green Egg")] [PrimaryColor(Chao.Colors.Green)] [TwoTone] ShinyGreenTwoToneEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Strange Shiny Grey Egg")] [PrimaryColor(Chao.Colors.Grey)] [TwoTone] ShinyGreyTwoToneEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Strange Shiny Lime Green Egg")] [PrimaryColor(Chao.Colors.LimeGreen)] [TwoTone] ShinyLimeGreenTwoToneEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Strange Shiny Black Egg")] [PrimaryColor(Chao.Colors.Black)] [TwoTone] ShinyBlackTwoToneEgg,
            [ItemCategory(ItemCategories.Egg)] [Description("Colorful Egg")] [TwoTone] MultiColorEgg, // TODO: Decide how to handle color mix chao... specifically how the eggs are going to work

            // Common Fruits
            [ItemCategory(ItemCategories.Fruit)] [Description("Tasty Fruit")] [Price(80)] TastyFruit = 1000,
            [ItemCategory(ItemCategories.Fruit)] [Description("Dark Fruit")] [Price(120)] DarkFruit,
            [ItemCategory(ItemCategories.Fruit)] [Description("Hero Fruit")] [Price(120)] HeroFruit,
            [ItemCategory(ItemCategories.Fruit)] [Description("Round Fruit")] [Price(80)] RoundFruit,
            [ItemCategory(ItemCategories.Fruit)] [Description("Square Fruit")] [Price(80)] SquareFruit,
            [ItemCategory(ItemCategories.Fruit)] [Description("Triangle Fruit")] [Price(80)] TriangleFruit,
            [ItemCategory(ItemCategories.Fruit)] [Description("Chao Fruit")] [Price(5000)] ChaoFruit,
            [ItemCategory(ItemCategories.Fruit)] [Description("Heart Fruit")] [Price(300)] HeartFruit,
            [ItemCategory(ItemCategories.Fruit)] [Description("Mushroom")] [Price(300)] Mushroom,
            [ItemCategory(ItemCategories.Fruit)] [Description("Strong Fruit")] [Price(500)] StrongFruit,
            [ItemCategory(ItemCategories.Fruit)] [Description("Swim Fruit")] [Price(1000)] SwimFruit,
            [ItemCategory(ItemCategories.Fruit)] [Description("Fly Fruit")] [Price(1000)] FlyFruit,
            [ItemCategory(ItemCategories.Fruit)] [Description("Run Fruit")] [Price(1000)] RunFruit,
            [ItemCategory(ItemCategories.Fruit)] [Description("Power Fruit")] [Price(1000)] PowerFruit,
            [ItemCategory(ItemCategories.Fruit)] [Description("Smart Fruit")] [Price(1000)] SmartFruit,
            [ItemCategory(ItemCategories.Fruit)] [Description("Lucky Mushroom")] [Price(1000)] LuckyMushroom,
            [ItemCategory(ItemCategories.Fruit)] [Description("Mint Candy")] [Price(10000)] MintCandy,

            // Rare Fruits
            [ItemCategory(ItemCategories.Fruit)] [Description("Hyper Swim Fruit")] [Price(100000)] HyperSwimFruit,
            [ItemCategory(ItemCategories.Fruit)] [Description("Hyper Fly Fruit")] [Price(100000)] HyperFlyFruit,
            [ItemCategory(ItemCategories.Fruit)] [Description("Hyper Run Fruit")] [Price(100000)] HyperRunFruit,
            [ItemCategory(ItemCategories.Fruit)] [Description("Hyper Power Fruit")] [Price(100000)] HyperPowerFruit,
            [ItemCategory(ItemCategories.Fruit)] [Description("Hyper Stamina Fruit")] [Price(100000)] HyperStaminaFruit,
            [ItemCategory(ItemCategories.Fruit)] [Description("Hyper Smart Fruit")] [Price(100000)] HyperSmartFruit,
            [ItemCategory(ItemCategories.Fruit)] [Description("Hyper Lucky Mushroom")] [Price(100000)] HyperLuckyMushroom,

            // Seeds
            [ItemCategory(ItemCategories.Seed)] [Description("Round Seed")] [Price(30000)] RoundSeed = 2000,
            [ItemCategory(ItemCategories.Seed)] [Description("Square Seed")] [Price(30000)] SquareSeed,
            [ItemCategory(ItemCategories.Seed)] [Description("Triangle Seed")] [Price(30000)] TriangleSeed,
            [ItemCategory(ItemCategories.Seed)] [Description("Hero Seed")] [Price(40000)] HeroSeed,
            [ItemCategory(ItemCategories.Seed)] [Description("Dark Seed")] [Price(40000)] DarkSeed,
            [ItemCategory(ItemCategories.Seed)] [Description("Tasty Seed")] [Price(20000)] TastySeed,
            [ItemCategory(ItemCategories.Seed)] [Description("Strong Seed")] [Price(50000)] StrongSeed,
            [ItemCategory(ItemCategories.Seed)] [Description("Swim Seed")] [Price(100000)] SwimSeed,
            [ItemCategory(ItemCategories.Seed)] [Description("Fly Seed")] [Price(100000)] FlySeed,
            [ItemCategory(ItemCategories.Seed)] [Description("Run Seed")] [Price(100000)] RunSeed,
            [ItemCategory(ItemCategories.Seed)] [Description("Power Seed")] [Price(100000)] PowerSeed,
            [ItemCategory(ItemCategories.Seed)] [Description("Smart Seed")] [Price(100000)] SmartSeed,
            [ItemCategory(ItemCategories.Seed)] [Description("Lucky Mushroom Spores")] [Price(100000)] LuckyMushroomSpores,

            // Special
            [ItemCategory(ItemCategories.Special)] [Description("Suspicious Potion")] [Price(30000)] SuspiciousPotion = 3000,
            [ItemCategory(ItemCategories.Special)] [Description("Chaos Juice")] [Price(100000)] ChaosJuice,
            [ItemCategory(ItemCategories.Special)] [Description("Negative Mirror")] [Price(10000)] NegativeMirror,

            // Non-Market Hats
            [ItemCategory(ItemCategories.Hat)] [Description("Normal Eggshell")] NormalEggshell = 4000,
            [ItemCategory(ItemCategories.Hat)] [Description("White Eggshell")] WhiteEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Red Eggshell")] RedEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Yellow Eggshell")] YellowEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Blue Eggshell")] BlueEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Sky Blue Eggshell")] SkyBlueEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Pink Eggshell")] PinkEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Orange Eggshell")] OrangeEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Brown Eggshell")] BrownEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Purple Eggshell")] PurpleEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Green Eggshell")] GreenEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Grey Eggshell")] GreyEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Lime Green Eggshell")] LimeGreenEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Black Eggshell")] BlackEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Shiny Normal Eggshell")] ShinyNormalEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Shiny White Eggshell")] ShinyWhiteEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Shiny Red Eggshell")] ShinyRedEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Shiny Yellow Eggshell")] ShinyYellowEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Shiny Blue Eggshell")] ShinyBlueEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Shiny Sky Blue Eggshell")] ShinySkyBlueEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Shiny Pink Eggshell")] ShinyPinkEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Shiny Orange Eggshell")] ShinyOrangeEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Shiny Brown Eggshell")] ShinyBrownEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Shiny Purple Eggshell")] ShinyPurpleEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Shiny Green Eggshell")] ShinyGreenEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Shiny Grey Eggshell")] ShinyGreyEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Shiny Lime Green Eggshell")] ShinyLimeGreenEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Shiny Black Eggshell")] ShinyBlackEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Shiny Amethyst Eggshell")] ShinyAmethystEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Shiny Aquamarine Eggshell")] ShinyAquamarineEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Shiny Emerald Eggshell")] ShinyEmeraldEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Shiny Garnet Eggshell")] ShinyGarnetEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Shiny Gold Eggshell")] ShinyGoldEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Shiny Onyx Eggshell")] ShinyOnyxEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Shiny Peridot Eggshell")] ShinyPeridotEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Shiny Ruby Eggshell")] ShinyRubyEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Shiny Sapphire Eggshell")] ShinySapphireEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Shiny Silver Eggshell")] ShinySilverEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Shiny Topaz Eggshell")] ShinyTopazEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Shiny Bronze Eggshell")] ShinyBronzeEggshell,
            [ItemCategory(ItemCategories.Hat)] [Description("Shiny Moon Eggshell")] ShinyMoonEggshell,

            // Market Hats
            [ItemCategory(ItemCategories.Hat)] [Description("Pumpkin")] Pumpkin = 5000,
            [ItemCategory(ItemCategories.Hat)] [Description("Skull")] Skull,
            [ItemCategory(ItemCategories.Hat)] [Description("Apple")] Apple,
            [ItemCategory(ItemCategories.Hat)] [Description("Cardboard Box")] CardboardBox,
            [ItemCategory(ItemCategories.Hat)] [Description("Pan")] Pan,
            [ItemCategory(ItemCategories.Hat)] [Description("Paper Bag")] PaperBag,
            [ItemCategory(ItemCategories.Hat)] [Description("Empty Can")] EmptyCan,
            [ItemCategory(ItemCategories.Hat)] [Description("Stump")] Stump,
            [ItemCategory(ItemCategories.Hat)] [Description("Flower Pot")] FlowerPot,
            [ItemCategory(ItemCategories.Hat)] [Description("Bucket")] Bucket,
            [ItemCategory(ItemCategories.Hat)] [Description("Watermelon")] Watermelon,
            [ItemCategory(ItemCategories.Hat)] [Description("Red Wool Beanie")] RedWoolBeanie,
            [ItemCategory(ItemCategories.Hat)] [Description("Blue Wool Beanie")] BlueWoolBeanie,
            [ItemCategory(ItemCategories.Hat)] [Description("Black Wool Beanie")] BlackWoolBeanie,

            // Lenses
            [ItemCategory(ItemCategories.Lens)] [Description("Normal Lens")] NormalLens = 10000,
            [ItemCategory(ItemCategories.Lens)] [Description("White Lens")] WhiteLens,
            [ItemCategory(ItemCategories.Lens)] [Description("Red Lens")] RedLens,
            [ItemCategory(ItemCategories.Lens)] [Description("Yellow Lens")] YellowLens,
            [ItemCategory(ItemCategories.Lens)] [Description("Blue Lens")] BlueLens,
            [ItemCategory(ItemCategories.Lens)] [Description("Sky Blue Lens")] SkyBlueLens,
            [ItemCategory(ItemCategories.Lens)] [Description("Pink Lens")] PinkLens,
            [ItemCategory(ItemCategories.Lens)] [Description("Orange Lens")] OrangeLens,
            [ItemCategory(ItemCategories.Lens)] [Description("Brown Lens")] BrownLens,
            [ItemCategory(ItemCategories.Lens)] [Description("Purple Lens")] PurpleLens,
            [ItemCategory(ItemCategories.Lens)] [Description("Green Lens")] GreenLens,
            [ItemCategory(ItemCategories.Lens)] [Description("Grey Lens")] GreyLens,
            [ItemCategory(ItemCategories.Lens)] [Description("Lime Green Lens")] LimeGreenLens,
            [ItemCategory(ItemCategories.Lens)] [Description("Black Lens")] BlackLens,
            [ItemCategory(ItemCategories.Lens)] [Description("Devil Lens")] DevilLens,
            [ItemCategory(ItemCategories.Lens)] [Description("Angel Lens")] AngelLens,
            [ItemCategory(ItemCategories.Lens)] [Description("Chaos Lens")] ChaosLens,
            [ItemCategory(ItemCategories.Lens)] [Description("Robot Lens")] RobotLens,

            // Placeholder
            [Description("???")] Placeholder = 1000000 // This is just for use as a boundary, nobody should have this in their inventory :^)
        }
    }
}