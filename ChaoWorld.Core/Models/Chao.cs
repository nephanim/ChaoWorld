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
        public Colors PrimaryColor { get; set; }
        public Colors? SecondaryColor { get; set; }
        public bool IsShiny { get; set; }
        public bool IsTwoTone { get; set; }
        public int Age { get; set; }
        public int Reincarnations { get; set; }
        public EvolutionStates EvolutionState { get; private set; }
        public Alignments Alignment { get; private set; }
        public int AlignmentValue { get; set; }
        public int EvolutionProgress { get; private set; }
        public AbilityTypes? FirstEvolutionType { get; private set; }
        public AbilityTypes? SecondEvolutionType { get; private set; }
        public int FlySwimAffinity { get; set; }
        public int RunPowerAffinity { get; set; }
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

        public string Appearance
        {
            get
            {
                var typeDescription = "";
                if (IsShiny)
                    typeDescription += $"Shiny ";
                if (IsTwoTone)
                    typeDescription += $"Two-Tone ";
                typeDescription += $"{PrimaryColor}";
                if (SecondaryColor.HasValue)
                    typeDescription += $"/{SecondaryColor} Mix";
                //if (EvolutionState == Core.Chao.EvolutionStates.Egg)
                    //typeDescription += $" Egg";
                //else
                if (EvolutionState == EvolutionStates.Child)
                    typeDescription += $" Child";
                else if (EvolutionState == EvolutionStates.First)
                    typeDescription += $" {Alignment} {FirstEvolutionType}";
                else if (EvolutionState == EvolutionStates.Second && SecondEvolutionType.HasValue)
                    typeDescription += $" {Alignment} {FirstEvolutionType}/{SecondEvolutionType}";
                return typeDescription;
            }
        }

        public enum Colors
        {
            Normal, Black, Blue, Brown, Green, Grey, LimeGreen, Orange, Pink, Purple, Red, SkyBlue, White, Yellow,
            Amethyst, Aquamarine, Emerald, Garnet, Gold, Onyx, Peridot, Ruby, Sapphire, Silver, Topaz,
            Bronze, Moon, Pearl, Glass, Metal,
            Invisible
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
            Normal, Swim, Fly, Run, Power,
            SwimRun, SwimPower, FlyRun, FlyPower,
            Chaos
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