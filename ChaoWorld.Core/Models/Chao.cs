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
    public readonly struct ChaoId: INumericId<ChaoId, long>
    {
        public long Value { get; }

        public ChaoId(long value)
        {
            Value = value;
        }

        public bool Equals(ChaoId other) => Value == other.Value;

        public override bool Equals(object obj) => obj is ChaoId other && Equals(other);

        public static bool operator ==(ChaoId left, ChaoId right) => left.Equals(right);

        public static bool operator !=(ChaoId left, ChaoId right) => !left.Equals(right);

        public int CompareTo(ChaoId other) => Value.CompareTo(other.Value);

        public override string ToString() => $"#{Value:D5}";
    }

    public class Chao
    {
        // Dapper *can* figure out mapping to getter-only properties, but this doesn't work
        // when trying to map to *subclasses* (eg. ListedChao). Adding private setters makes it work anyway.
        public ChaoId Id { get; private set; }
        public long DatabaseId { get { return Id.Value; } }
        public GardenId GardenId { get; private set; }
        public string Name { get; private set; }
        public Instant CreatedOn { get; private set; }
        public Instant RebirthOn { get; set; }

        //Appearance stats
        public Colors PrimaryColor { get; set; }
        public Colors? SecondaryColor { get; set; }
        public bool IsShiny { get; set; }
        public bool IsTwoTone { get; set; }
        public bool IsReversed { get; set; }

        //Development stats
        public int CurrentAge
        {
            get
            {
                var now = SystemClock.Instance.GetCurrentInstant();
                var elapsed = now - RebirthOn;
                return (elapsed.Days / 7);
            }
        }
        public int TotalAge
        {
            get
            {
                var now = SystemClock.Instance.GetCurrentInstant();
                var elapsed = now - CreatedOn;
                return (elapsed.Days / 7);
            }
        }
        public int Reincarnations { get; set; }
        public EvolutionStates EvolutionState { get; set; }
        public Alignments Alignment { get; set; }
        public int AlignmentValue { get; set; }
        public AbilityTypes? FirstEvolutionType { get; set; }
        public AbilityTypes? SecondEvolutionType { get; set; }
        public int FlySwimAffinity { get; set; }
        public int RunPowerAffinity { get; set; }
        
        // Some helpers for affinity calculations
        public int FlyAffinity
        {
            get
            {
                return FlySwimAffinity <= -50
                    ? Math.Abs(FlySwimAffinity)
                    : 0;
            }
        }
        public int SwimAffinity
        {
            get
            {
                return FlySwimAffinity >= 50
                    ? FlySwimAffinity
                    : 0;
            }
        }
        public int RunAffinity
        {
            get
            {
                return RunPowerAffinity <= -50
                    ? Math.Abs(RunPowerAffinity)
                    : 0;
            }
        }
        public int PowerAffinity
        {
            get
            {
                return RunPowerAffinity >= 50
                    ? RunPowerAffinity
                    : 0;
            }
        }

        // Abilities
        public StatGrades SwimGrade { get; set; }
        public int SwimLevel { get; private set; }
        public int SwimProgress { get; private set; }
        public int SwimValue { get; private set; }
        public StatGrades FlyGrade { get; set; }
        public int FlyLevel { get; private set; }
        public int FlyProgress { get; private set; }
        public int FlyValue { get; private set; }
        public StatGrades RunGrade { get; set; }
        public int RunLevel { get; private set; }
        public int RunProgress { get; private set; }
        public int RunValue { get; private set; }
        public StatGrades PowerGrade { get; set; }
        public int PowerLevel { get; private set; }
        public int PowerProgress { get; private set; }
        public int PowerValue { get; private set; }
        public StatGrades StaminaGrade { get; set; }
        public int StaminaLevel { get; private set; }
        public int StaminaProgress { get; private set; }
        public int StaminaValue { get; private set; }
        public StatGrades IntelligenceGrade { get; set; }
        public int IntelligenceLevel { get; private set; }
        public int IntelligenceProgress { get; private set; }
        public int IntelligenceValue { get; private set; }
        public StatGrades LuckGrade { get; set; }
        public int LuckLevel { get; private set; }
        public int LuckProgress { get; private set; }
        public int LuckValue { get; private set; }
        public double ReincarnationStatFactor { get; set; }

        public string Appearance
        {
            get
            {
                var typeDescription = "";
                if (IsReversed)
                    typeDescription += $"Reverse ";
                if (IsShiny)
                    typeDescription += $"Shiny ";
                if (IsTwoTone)
                    typeDescription += $"Two-Tone ";
                typeDescription += $"{PrimaryColor.GetDescription()}";
                if (SecondaryColor.HasValue)
                    typeDescription += $"/{SecondaryColor.GetDescription()} Mix";
                //if (EvolutionState == Core.Chao.EvolutionStates.Egg)
                    //typeDescription += $" Egg";
                //else
                if (EvolutionState == EvolutionStates.Child)
                    typeDescription += $" Child";
                else if (EvolutionState == EvolutionStates.First)
                    typeDescription += $" {Alignment} {FirstEvolutionType.GetDescription()}";
                else if (EvolutionState == EvolutionStates.Second && SecondEvolutionType.HasValue)
                    typeDescription += $" {Alignment} {FirstEvolutionType.GetDescription()}/{SecondEvolutionType.GetDescription()}";
                return typeDescription;
            }
        }

        public enum Colors
        {
            [Description("Normal")] Normal,
            [Description("Black")] Black,
            [Description("Blue")] Blue,
            [Description("Brown")] Brown,
            [Description("Green")] Green,
            [Description("Grey")] Grey,
            [Description("Lime Green")] LimeGreen,
            [Description("Orange")] Orange,
            [Description("Pink")] Pink,
            [Description("Purple")] Purple,
            [Description("Red")] Red,
            [Description("Sky Blue")] SkyBlue,
            [Description("White")] White,
            [Description("Yellow")] Yellow,
            [Description("Amethyst")] Amethyst,
            [Description("Aquamarine")] Aquamarine,
            [Description("Emerald")] Emerald,
            [Description("Garnet")] Garnet,
            [Description("Gold")] Gold,
            [Description("Onyx")] Onyx,
            [Description("Peridot")] Peridot,
            [Description("Ruby")] Ruby,
            [Description("Sapphire")] Sapphire,
            [Description("Silver")] Silver,
            [Description("Topaz")] Topaz,
            [Description("Bronze")] Bronze,
            [Description("Moon")] Moon,
            [Description("Pearl")] Pearl,
            [Description("Glass")] Glass,
            [Description("Metal")] Metal,
            [Description("Invisible")] Invisible,
            [Description("Dark Blue")] DeepDarkBlue,
            [Description("Dark Grey")] DarkerGrey
        }

        public enum EvolutionStates
        {
            //Egg,
            Child, First, Second
        }

        public enum Alignments
        {
            Neutral, Hero, Dark
        }

        public enum AbilityTypes
        {
            [Description("Normal")] Normal,
            [Description("Swim")] Swim,
            [Description("Fly")] Fly,
            [Description("Run")] Run,
            [Description("Power")] Power,
            [Description("Swim-Run")] SwimRun,
            [Description("Swim-Power")] SwimPower,
            [Description("Fly-Run")] FlyRun,
            [Description("Fly-Power")] FlyPower,
            [Description("Chaos")] Chaos
        }

        public enum StatGrades
        {
            E, D, C, B, A, S, X
        }

        public void Initialize(ItemBase.ItemTypes egg = ItemBase.ItemTypes.NormalTwoToneEgg)
        {
            Name = "Unnamed";
            PrimaryColor = ItemBase.GetPrimaryColor(egg);
            SecondaryColor = ItemBase.GetSecondaryColor(egg);
            IsShiny = ItemBase.GetShininess(egg);
            IsTwoTone = ItemBase.GetTwoToneness(egg);

            SwimGrade = MiscUtils.GenerateStatGrade();
            FlyGrade = MiscUtils.GenerateStatGrade();
            RunGrade = MiscUtils.GenerateStatGrade();
            PowerGrade = MiscUtils.GenerateStatGrade();
            StaminaGrade = MiscUtils.GenerateStatGrade();
            IntelligenceGrade = MiscUtils.GenerateStatGrade();
            LuckGrade = MiscUtils.GenerateStatGrade();
        }

        public void RaiseStatGrade(AbilityTypes type)
        {
            switch (type)
            {
                case AbilityTypes.Swim:
                    if (SwimGrade < StatGrades.S)
                        SwimGrade++;
                    break;
                case AbilityTypes.Fly:
                    if (FlyGrade < StatGrades.S)
                        FlyGrade++;
                    break;
                case AbilityTypes.Run:
                    if (RunGrade < StatGrades.S)
                        RunGrade++;
                    break;
                case AbilityTypes.Power:
                    if (PowerGrade < StatGrades.S)
                        PowerGrade++;
                    break;
                case AbilityTypes.Normal:
                default:
                    if (StaminaGrade < StatGrades.S)
                        StaminaGrade++;
                    break;
            }
        }

        public void RaiseSwim(int amount)
        {
            if (SwimLevel < 99)
            {
                SwimProgress += amount;
                if (SwimProgress >= 100)
                {
                    SwimProgress = SwimProgress % 100;
                    SwimLevel += 1;
                    if (SwimLevel >= 99)
                        SwimProgress = 0;
                    SwimValue += GetLevelUpIncrease(SwimGrade);
                }
            }
        }

        public void RaiseFly(int amount)
        {
            if (FlyLevel < 99)
            {
                FlyProgress += amount;
                if (FlyProgress >= 100)
                {
                    FlyProgress = FlyProgress % 100;
                    FlyLevel += 1;
                    if (FlyLevel >= 99)
                        FlyProgress = 0;
                    FlyValue += GetLevelUpIncrease(FlyGrade);
                }
            }
        }

        public void RaiseRun(int amount)
        {
            if (RunLevel < 99)
            {
                RunProgress += amount;
                if (RunProgress >= 100)
                {
                    RunProgress = RunProgress % 100;
                    RunLevel += 1;
                    if (RunLevel >= 99)
                        RunProgress = 0;
                    RunValue += GetLevelUpIncrease(RunGrade);
                }
            }
        }

        public void RaisePower(int amount)
        {
            if (PowerLevel < 99)
            {
                PowerProgress += amount;
                if (PowerProgress >= 100)
                {
                    PowerProgress = PowerProgress % 100;
                    PowerLevel += 1;
                    if (PowerLevel >= 99)
                        PowerProgress = 0;
                    PowerValue += GetLevelUpIncrease(PowerGrade);
                }
            }
        }

        public void RaiseStamina(int amount)
        {
            if (StaminaLevel < 99)
            {
                StaminaProgress += amount;
                if (StaminaProgress >= 100)
                {
                    StaminaProgress = StaminaProgress % 100;
                    StaminaLevel += 1;
                    if (StaminaLevel >= 99)
                        StaminaProgress = 0;
                    StaminaValue += GetLevelUpIncrease(StaminaGrade);
                }
            }
        }

        public void RaiseIntelligence(int amount)
        {
            if (IntelligenceLevel < 99)
            {
                IntelligenceProgress += amount;
                if (IntelligenceProgress >= 100)
                {
                    IntelligenceProgress = IntelligenceProgress % 100;
                    IntelligenceLevel += 1;
                    if (IntelligenceLevel >= 99)
                        IntelligenceProgress = 0;
                    IntelligenceValue += GetLevelUpIncrease(IntelligenceGrade);
                }
            }
        }

        public void RaiseLuck(int amount)
        {
            if (LuckLevel < 99)
            {
                LuckProgress += amount;
                if (LuckProgress >= 100)
                {
                    LuckProgress = LuckProgress % 100;
                    LuckLevel += 1;
                    if (LuckLevel >= 99)
                        LuckProgress = 0;
                    LuckValue += GetLevelUpIncrease(LuckGrade);
                }
            }
        }

        public int GetLevelUpIncrease(StatGrades grade)
        {
            var baseModifier = ((int)grade) + 1;
            var r = new Random().Next(
                11 + baseModifier*3,
                15 + baseModifier*3
            );
            return r;
        }

        public string GetEmojiGrade(StatGrades grade)
        {
            switch (grade)
            {
                case StatGrades.X:
                    return ":regional_indicator_x:";
                case StatGrades.S:
                    return ":regional_indicator_s:";
                case StatGrades.A:
                    return ":regional_indicator_a:";
                case StatGrades.B:
                    return ":regional_indicator_b:";
                case StatGrades.C:
                    return ":regional_indicator_c:";
                case StatGrades.D:
                    return ":regional_indicator_d:";
                case StatGrades.E:
                default:
                    return ":regional_indicator_e:";
            }
        }

        public Alignments GetEffectiveAlignment()
        {
            if (EvolutionState != EvolutionStates.Child)
                return Alignment;
            else if (AlignmentValue >= 50)
                return Alignments.Hero;
            else if (AlignmentValue <= -50)
                return Alignments.Dark;
            else
                return Alignments.Neutral;
        }

        public AbilityTypes GetEffectiveAbilityType()
        {
            if (SecondEvolutionType.HasValue)
                // This chao isn't going to change anymore regardless of affinity
                return SecondEvolutionType.Value;

            // Not at second evolution yet, so what will our next evolution be?
            if (Math.Abs(FlySwimAffinity) < 50 && Math.Abs(RunPowerAffinity) < 50)
                return AbilityTypes.Normal; // This is the only way we should ever get the Normal ability type
            // These evolution types are only available for second evolution, but we'll check them first to rule them out
            else if (FirstEvolutionType.HasValue && FlyAffinity >= 50 && RunAffinity >= 50)
                return AbilityTypes.FlyRun;
            else if (FirstEvolutionType.HasValue && FlyAffinity >= 50 && PowerAffinity >= 50)
                return AbilityTypes.FlyPower;
            else if (FirstEvolutionType.HasValue && SwimAffinity >= 50 && RunAffinity >= 50)
                return AbilityTypes.SwimRun;
            else if (FirstEvolutionType.HasValue && SwimAffinity >= 50 && PowerAffinity >= 50)
                return AbilityTypes.SwimPower;
            // For ties beyond this point, swim > run/power > fly
            else if (SwimAffinity >= 50 && RunAffinity <= SwimAffinity && PowerAffinity <= SwimAffinity)
                return AbilityTypes.Swim;
            else if (RunAffinity >= 50 && FlyAffinity <= RunAffinity)
                return AbilityTypes.Run;
            else if (PowerAffinity >= 50 && FlyAffinity <= PowerAffinity)
                return AbilityTypes.Power;
            else
                return AbilityTypes.Fly;
        }

        public void Reincarnate()
        {
            Reincarnations++;
            RebirthOn = SystemClock.Instance.GetCurrentInstant();
            EvolutionState = EvolutionStates.Child;
            Alignment = Alignments.Neutral;
            AlignmentValue = 0;
            FirstEvolutionType = null;
            SecondEvolutionType = null;
            FlySwimAffinity = 0;
            RunPowerAffinity = 0;
            SwimValue = (int)(SwimValue * ReincarnationStatFactor);
            SwimLevel = 1;
            SwimProgress = 0;
            FlyValue = (int)(FlyValue * ReincarnationStatFactor);
            FlyLevel = 1;
            FlyProgress = 0;
            RunValue = (int)(RunValue * ReincarnationStatFactor);
            RunLevel = 1;
            RunProgress = 0;
            PowerValue = (int)(PowerValue * ReincarnationStatFactor);
            PowerLevel = 1;
            PowerProgress = 0;
            StaminaValue = (int)(StaminaValue * ReincarnationStatFactor);
            StaminaLevel = 1;
            StaminaProgress = 0;
            IntelligenceValue = (int)(IntelligenceValue * ReincarnationStatFactor);
            IntelligenceLevel = 1;
            IntelligenceProgress = 0;
            LuckValue = (int)(LuckValue * ReincarnationStatFactor);
            LuckLevel = 1;
            LuckProgress = 0;
        }
    }

    public class Affection
    {
        public long ChaoId { get; set; }
        public long AccountUid { get; set; }
        public int FriendshipValue { get; set; }

        public AffectionLevel GetAffectionLevel
        {
            get
            {
                if (FriendshipValue >= 1000)
                    return AffectionLevel.Love;
                if (FriendshipValue >= 500)
                    return AffectionLevel.Attached;
                if (FriendshipValue >= 250)
                    return AffectionLevel.Friendly;
                if (FriendshipValue >= 0)
                    return AffectionLevel.Indifferent;
                if (FriendshipValue <= -250)
                    return AffectionLevel.Unfriendly;
                if (FriendshipValue <= -500)
                    return AffectionLevel.Avoidant;
                if (FriendshipValue <= -1000)
                    return AffectionLevel.Hate;
                else
                    return AffectionLevel.Indifferent;
            }
        }

        public enum AffectionLevel
        {
            Hate = -3,
            Avoidant = -2,
            Unfriendly = -1,
            Indifferent, Friendly, Attached, Love
        }
    }
}