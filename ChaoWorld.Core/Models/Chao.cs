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
        public bool IsActive { get; private set; }
        public bool IsDeleted { get; private set; }
        public Instant CreatedOn { get; private set; }
        public Instant? DeletedOn { get; private set; }
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

        public enum Colors
        {
            Normal, Black, Blue, Brown, Green, Grey, LimeGreen, Orange, Pink, Purple, Red, SkyBlue, White, Yellow,
            Amethyst, Aquamarine, Emerald, Garnet, Gold, Onyx, Peridot, Ruby, Sapphire, Silver, Topaz
        }

        public enum EvolutionStates
        {
            Egg, Child, First, Second
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

        public void Initialize(bool isStarterChao)
        {
            IsActive = isStarterChao;
            Name = "Unnamed";
            SwimGrade = MiscUtils.GenerateStatGrade();
            FlyGrade = MiscUtils.GenerateStatGrade();
            RunGrade = MiscUtils.GenerateStatGrade();
            PowerGrade = MiscUtils.GenerateStatGrade();
            StaminaGrade = MiscUtils.GenerateStatGrade();
            IntelligenceGrade = MiscUtils.GenerateStatGrade();
            LuckGrade = MiscUtils.GenerateStatGrade();
        }
    }
}