using System;
using System.Collections.Generic;
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
        public Colors PrimaryColor { get; private set; }
        public Colors? SecondaryColor { get; private set; }
        public bool IsShiny { get; private set; }
        public bool IsTwoTone { get; private set; }
        public int Age { get; private set; }
        public int Reincarnations { get; private set; }
        public EvolutionStates EvolutionState { get; private set; }
        public Alignments Alignment { get; private set; }
        public int AlignmentValue { get; private set; }
        public int EvolutionProgress { get; private set; }
        public AbilityTypes? FirstEvolutionType { get; private set; }
        public AbilityTypes? SecondEvolutionType { get; private set; }
        public int FlySwimAffinity { get; private set; }
        public int RunPowerAffinity { get; private set; }
        public StatGrades SwimGrade { get; private set; }
        public int SwimLevel { get; private set; }
        public int SwimProgress { get; private set; }
        public int SwimValue { get; private set; }
        public StatGrades FlyGrade { get; private set; }
        public int FlyLevel { get; private set; }
        public int FlyProgress { get; private set; }
        public int FlyValue { get; private set; }
        public StatGrades RunGrade { get; private set; }
        public int RunLevel { get; private set; }
        public int RunProgress { get; private set; }
        public int RunValue { get; private set; }
        public StatGrades PowerGrade { get; private set; }
        public int PowerLevel { get; private set; }
        public int PowerProgress { get; private set; }
        public int PowerValue { get; private set; }
        public StatGrades StaminaGrade { get; private set; }
        public int StaminaLevel { get; private set; }
        public int StaminaProgress { get; private set; }
        public int StaminaValue { get; private set; }
        public StatGrades IntelligenceGrade { get; private set; }
        public int IntelligenceLevel { get; private set; }
        public int IntelligenceProgress { get; private set; }
        public int IntelligenceValue { get; private set; }
        public StatGrades LuckGrade { get; private set; }
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
                if (IsTwoTone && SecondaryColor.HasValue)
                    typeDescription += $"Two-Tone {PrimaryColor}/{SecondaryColor} ";
                typeDescription += $"{PrimaryColor}";
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
            Amethyst, Aquamarine, Emerald, Garnet, Gold, Onyx, Peridot, Ruby, Sapphire, Silver, Topaz
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

        public void Initialize()
        {
            Name = "Unnamed";
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
                if (SwimProgress >= 1000)
                {
                    SwimProgress = SwimProgress % 1000;
                    SwimLevel += 1;
                    if (SwimLevel >= 99)
                        SwimProgress = 0;
                    SwimValue = GetLevelUpIncrease(SwimGrade);
                }
            }
        }

        public void RaiseFly(int amount)
        {
            if (FlyLevel < 99)
            {
                FlyProgress += amount;
                if (FlyProgress >= 1000)
                {
                    FlyProgress = FlyProgress % 1000;
                    FlyLevel += 1;
                    if (FlyLevel >= 99)
                        FlyProgress = 0;
                    FlyValue = GetLevelUpIncrease(FlyGrade);
                }
            }
        }

        public void RaiseRun(int amount)
        {
            if (RunLevel < 99)
            {
                RunProgress += amount;
                if (RunProgress >= 1000)
                {
                    RunProgress = RunProgress % 1000;
                    RunLevel += 1;
                    if (RunLevel >= 99)
                        RunProgress = 0;
                    RunValue = GetLevelUpIncrease(RunGrade);
                }
            }
        }

        public void RaisePower(int amount)
        {
            if (PowerLevel < 99)
            {
                PowerProgress += amount;
                if (PowerProgress >= 1000)
                {
                    PowerProgress = PowerProgress % 1000;
                    PowerLevel += 1;
                    if (PowerLevel >= 99)
                        PowerProgress = 0;
                    PowerValue = GetLevelUpIncrease(PowerGrade);
                }
            }
        }

        public void RaiseStamina(int amount)
        {
            if (StaminaLevel < 99)
            {
                StaminaProgress += amount;
                if (StaminaProgress >= 1000)
                {
                    StaminaProgress = StaminaProgress % 1000;
                    StaminaLevel += 1;
                    if (StaminaLevel >= 99)
                        StaminaProgress = 0;
                    StaminaValue = GetLevelUpIncrease(StaminaGrade);
                }
            }
        }

        public void RaiseIntelligence(int amount)
        {
            if (IntelligenceLevel < 99)
            {
                IntelligenceProgress += amount;
                if (IntelligenceProgress >= 1000)
                {
                    IntelligenceProgress = IntelligenceProgress % 1000;
                    IntelligenceLevel += 1;
                    if (IntelligenceLevel >= 99)
                        IntelligenceProgress = 0;
                    IntelligenceValue = GetLevelUpIncrease(IntelligenceGrade);
                }
            }
        }

        public void RaiseLuck(int amount)
        {
            if (LuckLevel < 99)
            {
                LuckProgress += amount;
                if (LuckProgress >= 1000)
                {
                    LuckProgress = LuckProgress % 1000;
                    LuckLevel += 1;
                    if (LuckLevel >= 99)
                        LuckProgress = 0;
                    LuckValue = GetLevelUpIncrease(LuckGrade);
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
    }
}