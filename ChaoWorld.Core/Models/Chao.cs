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

        public override string ToString() => $"{Value}";
    }

    public class Chao
    {
        // Dapper *can* figure out mapping to getter-only properties, but this doesn't work
        // when trying to map to *subclasses* (eg. ListedChao). Adding private setters makes it work anyway.
        public ChaoId Id { get; private set; }
        public long DatabaseId { get { return Id.Value; } }
        public GardenId GardenId { get; private set; }
        public string Name { get; private set; }
        public string Tag { get; set; }
        public string DisplayName
        {
            get
            {
                return string.IsNullOrEmpty(Tag)
                    ? Name
                    : $"{Name} {Tag}";
            }
        }
        public Instant CreatedOn { get; private set; }
        public Instant RebirthOn { get; set; }

        //Appearance stats
        public Colors PrimaryColor { get; set; }
        public Colors? SecondaryColor { get; set; }
        public bool IsShiny { get; set; }
        public bool IsTwoTone { get; set; }
        public bool IsReversed { get; set; }
        public bool IsFertile { get; set; }

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

        // Chaos Factor
        public double SwimFactor { get; set; }
        public double FlyFactor { get; set; }
        public double RunFactor { get; set; }
        public double PowerFactor { get; set; }
        public double StaminaFactor { get; set; }
        public double IntelligenceFactor { get; set; }
        public double LuckFactor { get; set; }

        // Time Gates / Energy
        public int Energy { get; set; }
        public int Hunger { get; set; }

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

        public int AverageStatValue
        {
            get
            {
                return (SwimValue + FlyValue + RunValue + PowerValue + StaminaValue + IntelligenceValue + LuckValue) / 7;
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
            [Description("Powder Blue")] PowderBlue,
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
            [Description("Chaos")] Chaos,
            [Description("Intelligence")] Intelligence,
            [Description("Luck")] Luck
        }

        public enum StatGrades
        {
            E, D, C, B, A, S, X
        }

        public void Initialize(Colors primaryColor = Colors.Normal, Colors? secondaryColor = null, bool isShiny = false, bool isTwoTone = true)
        {
            Name = "Unnamed";
            PrimaryColor = primaryColor;
            SecondaryColor = secondaryColor;
            IsShiny = isShiny;
            IsTwoTone = isTwoTone;
            RebirthOn = SystemClock.Instance.GetCurrentInstant();

            SwimGrade = MiscUtils.GenerateStatGrade();
            FlyGrade = MiscUtils.GenerateStatGrade();
            RunGrade = MiscUtils.GenerateStatGrade();
            PowerGrade = MiscUtils.GenerateStatGrade();
            StaminaGrade = MiscUtils.GenerateStatGrade();
            IntelligenceGrade = MiscUtils.GenerateStatGrade();
            LuckGrade = MiscUtils.GenerateStatGrade();
        }

        public bool RaiseStatGrade(AbilityTypes type)
        {
            switch (type)
            {
                case AbilityTypes.Swim:
                    if (SwimGrade < StatGrades.S)
                    {
                        SwimGrade++;
                        return true;
                    }
                    else
                        return false;
                case AbilityTypes.Fly:
                    if (FlyGrade < StatGrades.S)
                    {
                        FlyGrade++;
                        return true;
                    }
                    else
                        return false;
                case AbilityTypes.Run:
                    if (RunGrade < StatGrades.S)
                    {
                        RunGrade++;
                        return true;
                    }
                    else
                        return false;
                case AbilityTypes.Power:
                    if (PowerGrade < StatGrades.S)
                    {
                        PowerGrade++;
                        return true;
                    }
                    else
                        return false;
                case AbilityTypes.Intelligence:
                    if (IntelligenceGrade < StatGrades.S)
                    {
                        IntelligenceGrade++;
                        return true;
                    }
                    else
                        return false;
                case AbilityTypes.Luck:
                    if (LuckGrade < StatGrades.S)
                    {
                        LuckGrade++;
                        return true;
                    }
                    else
                        return false;
                case AbilityTypes.Normal:
                default:
                    if (StaminaGrade < StatGrades.S)
                    {
                        StaminaGrade++;
                        return true;
                    }
                    else
                        return false;
            }
        }

        public void RaiseSwim(int amount)
        {
            if (SwimLevel < 99)
            {
                SwimProgress += amount;
                if (SwimProgress >= 100)
                {
                    var levels = Math.Min(99 - SwimLevel, SwimProgress / 100);
                    SwimLevel += levels;
                    SwimProgress = SwimProgress % 100;
                    if (SwimLevel >= 99)
                    {
                        SwimLevel = 99;
                        SwimProgress = 0;
                    }
                    while (levels > 0)
                    {
                        SwimValue += GetLevelUpIncrease(SwimGrade);
                        levels--;
                    }
                    if (SwimValue > 9999)
                        SwimValue = 9999;
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
                    var levels = Math.Min(99 - FlyLevel, FlyProgress / 100);
                    FlyLevel += levels;
                    FlyProgress = FlyProgress % 100;
                    if (FlyLevel >= 99)
                    {
                        FlyLevel = 99;
                        FlyProgress = 0;
                    }
                    while (levels > 0)
                    {
                        FlyValue += GetLevelUpIncrease(FlyGrade);
                        levels--;
                    }
                    if (FlyValue > 9999)
                        FlyValue = 9999;
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
                    var levels = Math.Min(99 - RunLevel, RunProgress / 100);
                    RunLevel += levels;
                    RunProgress = RunProgress % 100;
                    if (RunLevel >= 99)
                    {
                        RunLevel = 99;
                        RunProgress = 0;
                    }
                    while (levels > 0)
                    {
                        RunValue += GetLevelUpIncrease(RunGrade);
                        levels--;
                    }
                    if (RunValue > 9999)
                        RunValue = 9999;
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
                    var levels = Math.Min(99 - PowerLevel, PowerProgress / 100);
                    PowerLevel += levels;
                    PowerProgress = PowerProgress % 100;
                    if (PowerLevel >= 99)
                    {
                        PowerLevel = 99;
                        PowerProgress = 0;
                    }
                    while (levels > 0)
                    {
                        PowerValue += GetLevelUpIncrease(PowerGrade);
                        levels--;
                    }
                    if (PowerValue > 9999)
                        PowerValue = 9999;
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
                    var levels = Math.Min(99 - StaminaLevel, StaminaProgress / 100);
                    StaminaLevel += levels;
                    StaminaProgress = StaminaProgress % 100;
                    if (StaminaLevel >= 99)
                    {
                        StaminaLevel = 99;
                        StaminaProgress = 0;
                    }
                    while (levels > 0)
                    {
                        StaminaValue += GetLevelUpIncrease(StaminaGrade);
                        levels--;
                    }
                    if (StaminaValue > 9999)
                        StaminaValue = 9999;
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
                    var levels = Math.Min(99 - IntelligenceLevel, IntelligenceProgress / 100);
                    IntelligenceLevel += levels;
                    IntelligenceProgress = IntelligenceProgress % 100;
                    if (IntelligenceLevel >= 99)
                    {
                        IntelligenceLevel = 99;
                        IntelligenceProgress = 0;
                    }
                    while (levels > 0)
                    {
                        IntelligenceValue += GetLevelUpIncrease(IntelligenceGrade);
                        levels--;
                    }
                    if (IntelligenceValue > 9999)
                        IntelligenceValue = 9999;
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
                    var levels = Math.Min(99 - LuckLevel, LuckProgress / 100);
                    LuckLevel += levels;
                    LuckProgress = LuckProgress % 100;
                    if (LuckLevel >= 99)
                    {
                        LuckLevel = 99;
                        LuckProgress = 0;
                    }
                    while (levels > 0)
                    {
                        LuckValue += GetLevelUpIncrease(LuckGrade);
                        levels--;
                    }
                    if (LuckValue > 9999)
                        LuckValue = 9999;
                }
            }
        }

        public int GetLevelUpIncrease(StatGrades grade)
        {
            var baseModifier = (int)grade;
            // This has possible values from 11-16 at grade E (0) - upper bound is not included in Random.Next return value
            // At grade X (6), possible values are from 29-34
            var r = new Random().Next(
                11 + baseModifier*3,
                17 + baseModifier*3
            );
            return r;
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
            IsFertile = false;
            Alignment = Alignments.Neutral;
            AlignmentValue = 0;
            FirstEvolutionType = null;
            SecondEvolutionType = null;
            FlySwimAffinity = 0;
            RunPowerAffinity = 0;
            SwimValue = (int)(SwimValue * SwimFactor);
            SwimLevel = 1;
            SwimProgress = 0;
            FlyValue = (int)(FlyValue * FlyFactor);
            FlyLevel = 1;
            FlyProgress = 0;
            RunValue = (int)(RunValue * RunFactor);
            RunLevel = 1;
            RunProgress = 0;
            PowerValue = (int)(PowerValue * PowerFactor);
            PowerLevel = 1;
            PowerProgress = 0;
            StaminaValue = (int)(StaminaValue * StaminaFactor);
            StaminaLevel = 1;
            StaminaProgress = 0;
            IntelligenceValue = (int)(IntelligenceValue * IntelligenceFactor);
            IntelligenceLevel = 1;
            IntelligenceProgress = 0;
            LuckValue = (int)(LuckValue * LuckFactor);
            LuckLevel = 1;
            LuckProgress = 0;
        }
    }

    public class ChaoGenes
    {
        public long ChaoId { get; set; }
        public long? FirstParentId { get; set; }
        public long? SecondParentId { get; set; }
        public int FirstColorId { get; set; }
        public Chao.Colors FirstColor
        {
            get
            {
                return (Chao.Colors)FirstColorId;
            }
            set
            {
                FirstColorId = (int)value;
            }
        }
        public int SecondColorId { get; set; }
        public Chao.Colors SecondColor
        {
            get
            {
                return (Chao.Colors)SecondColorId;
            }
            set
            {
                SecondColorId = (int)value;
            }
        }
        public bool FirstShiny { get; set; }
        public bool SecondShiny { get; set; }
        public bool FirstTwoTone { get; set; }
        public bool SecondTwoTone { get; set; }
        public Chao.StatGrades FirstSwimGrade { get; set; }
        public Chao.StatGrades SecondSwimGrade { get; set; }
        public Chao.StatGrades FirstFlyGrade { get; set; }
        public Chao.StatGrades SecondFlyGrade { get; set; }
        public Chao.StatGrades FirstRunGrade { get; set; }
        public Chao.StatGrades SecondRunGrade { get; set; }
        public Chao.StatGrades FirstPowerGrade { get; set; }
        public Chao.StatGrades SecondPowerGrade { get; set; }
        public Chao.StatGrades FirstStaminaGrade { get; set; }
        public Chao.StatGrades SecondStaminaGrade { get; set; }
        public Chao.StatGrades FirstIntelligenceGrade { get; set; }
        public Chao.StatGrades SecondIntelligenceGrade { get; set; }
        public Chao.StatGrades FirstLuckGrade { get; set; }
        public Chao.StatGrades SecondLuckGrade { get; set; }

        public Chao InitializeFromParents(ChaoGenes firstParent, ChaoGenes secondParent)
        {
            // We will modify the genes, but also generate a chao for the caller based on those genes
            var chao = new Chao();
            chao.Initialize();
            if (firstParent == null || secondParent == null)
            {
                // Treat this as a default normal chao since we don't know anything
                FirstColor = SecondColor = chao.PrimaryColor;
                FirstShiny = SecondShiny = chao.IsShiny;
                FirstTwoTone = SecondTwoTone = chao.IsTwoTone;
                FirstSwimGrade = SecondSwimGrade = chao.SwimGrade;
                FirstFlyGrade = SecondFlyGrade = chao.FlyGrade;
                FirstRunGrade = SecondRunGrade = chao.RunGrade;
                FirstPowerGrade = SecondPowerGrade = chao.PowerGrade;
                FirstStaminaGrade = SecondStaminaGrade = chao.StaminaGrade;
                FirstIntelligenceGrade = SecondIntelligenceGrade = chao.IntelligenceGrade;
                FirstLuckGrade = SecondLuckGrade = chao.LuckGrade;
            }
            else
            {
                // Copy over the parents' IDs
                FirstParentId = firstParent.ChaoId;
                SecondParentId = secondParent.ChaoId;

                // TODO: When we start allowing color mixes, implement that here (chance to set a SecondaryColor)
                // Colors except normal have equal dominance. The normal color gene is recessive.
                if (firstParent.FirstColor == Chao.Colors.Normal && firstParent.SecondColor == Chao.Colors.Normal)
                    FirstColor = Chao.Colors.Normal;
                else
                    FirstColor = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(),0)).Next(0, 2) == 0 ? firstParent.FirstColor : firstParent.SecondColor;
                if (secondParent.FirstColor == Chao.Colors.Normal && secondParent.SecondColor == Chao.Colors.Normal)
                    SecondColor = Chao.Colors.Normal;
                else
                    SecondColor = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0)).Next(0, 2) == 0 ? secondParent.FirstColor : secondParent.SecondColor;

                if (FirstColor == Chao.Colors.Normal && SecondColor == Chao.Colors.Normal)
                    chao.PrimaryColor = Chao.Colors.Normal;
                else
                    chao.PrimaryColor = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0)).Next(0, 2) == 0 ? FirstColor : SecondColor;

                // If either gene is shiny, that's what the parent passes on
                // Likewise, if either inherited gene is shiny, the chao will be too
                FirstShiny = firstParent.FirstShiny || firstParent.SecondShiny;
                SecondShiny = secondParent.FirstShiny || secondParent.SecondShiny;

                chao.IsShiny = FirstShiny || SecondShiny;

                // Tone alleles are equally dominant, so we can take either
                FirstTwoTone = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0)).Next(0, 2) == 0 ? firstParent.FirstTwoTone : firstParent.SecondTwoTone;
                SecondTwoTone = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0)).Next(0, 2) == 0 ? secondParent.FirstTwoTone : secondParent.SecondTwoTone;

                chao.IsTwoTone = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0)).Next(0, 2) == 0 ? FirstTwoTone : SecondTwoTone;

                // Stat grades are also equally dominant
                FirstSwimGrade = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0)).Next(0, 2) == 0 ? firstParent.FirstSwimGrade : firstParent.SecondSwimGrade;
                SecondSwimGrade = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0)).Next(0, 2) == 0 ? secondParent.FirstSwimGrade : secondParent.SecondSwimGrade;
                FirstFlyGrade = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0)).Next(0, 2) == 0 ? firstParent.FirstFlyGrade : firstParent.SecondFlyGrade;
                SecondFlyGrade = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0)).Next(0, 2) == 0 ? secondParent.FirstFlyGrade : secondParent.SecondFlyGrade;
                FirstRunGrade = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0)).Next(0, 2) == 0 ? firstParent.FirstRunGrade : firstParent.SecondRunGrade;
                SecondRunGrade = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0)).Next(0, 2) == 0 ? secondParent.FirstRunGrade : secondParent.SecondRunGrade;
                FirstPowerGrade = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0)).Next(0, 2) == 0 ? firstParent.FirstPowerGrade : firstParent.SecondPowerGrade;
                SecondPowerGrade = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0)).Next(0, 2) == 0 ? secondParent.FirstPowerGrade : secondParent.SecondPowerGrade;
                FirstStaminaGrade = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0)).Next(0, 2) == 0 ? firstParent.FirstStaminaGrade : firstParent.SecondStaminaGrade;
                SecondStaminaGrade = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0)).Next(0, 2) == 0 ? secondParent.FirstStaminaGrade : secondParent.SecondStaminaGrade;
                FirstIntelligenceGrade = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0)).Next(0, 2) == 0 ? firstParent.FirstIntelligenceGrade : firstParent.SecondIntelligenceGrade;
                SecondIntelligenceGrade = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0)).Next(0, 2) == 0 ? secondParent.FirstIntelligenceGrade : secondParent.SecondIntelligenceGrade;
                FirstLuckGrade = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0)).Next(0, 2) == 0 ? firstParent.FirstLuckGrade : firstParent.SecondLuckGrade;
                SecondLuckGrade = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0)).Next(0, 2) == 0 ? secondParent.FirstLuckGrade : secondParent.SecondLuckGrade;

                chao.SwimGrade = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0)).Next(0, 2) == 0 ? FirstSwimGrade : SecondSwimGrade;
                chao.FlyGrade = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0)).Next(0, 2) == 0 ? FirstFlyGrade : SecondFlyGrade;
                chao.RunGrade = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0)).Next(0, 2) == 0 ? FirstRunGrade : SecondRunGrade;
                chao.PowerGrade = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0)).Next(0, 2) == 0 ? FirstPowerGrade : SecondPowerGrade;
                chao.StaminaGrade = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0)).Next(0, 2) == 0 ? FirstStaminaGrade : SecondStaminaGrade;
                chao.IntelligenceGrade = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0)).Next(0, 2) == 0 ? FirstIntelligenceGrade : SecondIntelligenceGrade;
                chao.LuckGrade = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0)).Next(0, 2) == 0 ? FirstLuckGrade : SecondLuckGrade;
            }
            return chao;
        }

        public void InitializeFromChao(Chao chao)
        {
            // In cases where a chao doesn't have parents (e.g. default, hatched from a market egg) we can just set their genes this way
            ChaoId = chao.Id.Value;
            FirstColor = chao.PrimaryColor;
            SecondColor = chao.SecondaryColor.GetValueOrDefault(chao.PrimaryColor);
            FirstShiny = SecondShiny = chao.IsShiny;
            FirstTwoTone = SecondTwoTone = chao.IsTwoTone;
            FirstSwimGrade = SecondSwimGrade = chao.SwimGrade;
            FirstFlyGrade = SecondFlyGrade = chao.FlyGrade;
            FirstRunGrade = SecondRunGrade = chao.RunGrade;
            FirstPowerGrade = SecondPowerGrade = chao.PowerGrade;
            FirstStaminaGrade = SecondStaminaGrade = chao.StaminaGrade;
            FirstIntelligenceGrade = SecondIntelligenceGrade = chao.IntelligenceGrade;
            FirstLuckGrade = SecondLuckGrade = chao.LuckGrade;
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