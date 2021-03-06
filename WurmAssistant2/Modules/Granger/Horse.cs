﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CustomColors;

namespace Aldurcraft.WurmOnline.WurmAssistant2.ModuleNS.Granger
{
    /// <summary>
    /// equal and operator overloaded to compare indexes
    /// </summary>
    public class Horse
    {
        public HorseEntity Entity { get; private set; }
        GrangerContext Context;
        public int Value { get { return MainForm.CurrentValuator.GetValueForHorse(this); } }
        public int PotentialPositiveValue { get { return MainForm.CurrentValuator.GetPotentialPositiveValueForHorse(this); } }
        public int PotentialNegativeValue { get { return MainForm.CurrentValuator.GetPotentialNegativeValueForHorse(this); } }
        //this is no longer public because we use cached breed value!
        double? BreedValue 
        { 
            get 
            {
                var breedResults = MainForm.CurrentAdvisor.GetBreedingValue(this);
                if (breedResults.HasValue)
                {
                    if (breedResults.Value.Ignored) return null;
                    else if (breedResults.Value.Discarded) return double.NegativeInfinity;
                    else return breedResults.Value.Value;
                }
                else return null;
            } 
        }

        //added to support better color hints
        double? _CachedBreedValue = null;
        public double? CachedBreedValue
        {
            get
            {
                return _CachedBreedValue;
            }
        }
        public void RebuildCachedBreedValue()
        {
            _CachedBreedValue = BreedValue;
        }

        System.Drawing.Color? _BreedHintColor = null;
        /// <summary>
        /// Use to color entire row, null if no color set
        /// </summary>
        public System.Drawing.Color? BreedHintColor
        {
            get
            {
                return _BreedHintColor;
            }

            private set
            {
                _BreedHintColor = value;
            }
        }

        private System.Drawing.Color? _HorseBestCandidateColor;
        /// <summary>
        /// null if candidate is not best, else contains default best candidate color
        /// </summary>
        public System.Drawing.Color? HorseBestCandidateColor
        {
            get { return _HorseBestCandidateColor; }
            set { _HorseBestCandidateColor = value; }
        }

        static System.Drawing.Color? DefaultBestBreedHintColor = (System.Drawing.Color)(new HSLColor(120D, 240D, 180D));

        public void RefreshBreedHintColor(double minBreedValue, double maxBreedValue)
        {
            if (CachedBreedValue == maxBreedValue)
            {
                HorseBestCandidateColor = DefaultBestBreedHintColor;
            }

            if (MainForm.SelectedSingleHorse != null)
            {
                BreedHintColor = MainForm.CurrentAdvisor.GetHintColor(this, minBreedValue, maxBreedValue);
            }
        }

        public void ClearColorHints()
        {
            BreedHintColor = null;
            HorseBestCandidateColor = null;
        }

        public System.Drawing.Color? HorseColorBkColor
        {
            get
            {
                HorseColor hcolor = Color;
                if (hcolor == HorseColor.GetDefaultColor()) return null;
                else return hcolor.ToSystemDrawingColor();
            }
        }

        private FormGrangerMain MainForm;
        //other data?

        public Horse(FormGrangerMain mainForm, HorseEntity entity, GrangerContext context)
        {
            MainForm = mainForm;
            Entity = entity;
            Context = context;
        }

        //data accessors

        public HorseTrait[] Traits
        {
            get { return Entity.Traits.ToArray(); }
            set
            {
                Entity.Traits = value.ToList();
            }
        }

        public override string ToString()
        {
            return Entity.ToString();
        }

        public HorseColor Color
        {
            get { return Entity.Color; }
            set
            {
                Entity.Color = value;
            }
        }

        public Horse GetMate()
        {
            if (!HasMate()) return null;

            var mate = Context.Horses.Where(x => x.ID == this.Entity.PairedWith).Select(x => new Horse(MainForm, x, Context)).ToArray();
            if (mate.Length == 1) 
                return mate.First();
            else if (mate.Length == 0) 
                return null;
            else throw new Exception("duplicate creatures found?");
        }

        public bool HasMate()
        {
            if (this.Entity.PairedWith == null) return false;
            else return true;
        }

        public void SetMate(Horse value)
        {
            if (value == null)
            {
                var mate = GetMate();
                if (mate != null) mate.Entity.PairedWith = null;
                this.Entity.PairedWith = null;
            }
            else
            {
                this.Entity.PairedWith = value.Entity.ID;
                value.Entity.PairedWith = this.Entity.ID;
            }
        }

        public bool IsMale
        {
            get { return (Entity.IsMale ?? false); }
            set { Entity.IsMale = value; }
        }

        public DateTime NotInMoodUntil
        {
            get { return (Entity.NotInMood ?? DateTime.MinValue); }
            set { Entity.NotInMood = value; }
        }

        public DateTime GroomedOn
        {
            get { return (Entity.GroomedOn ?? DateTime.MinValue); }
            set { Entity.GroomedOn = value; }
        }

        public DateTime PregnantUntil
        {
            get { return (Entity.PregnantUntil ?? DateTime.MinValue); }
            set { Entity.PregnantUntil = value; }
        }

        public DateTime BirthDate
        {
            get { return (Entity.BirthDate ?? DateTime.MinValue); }
            set { Entity.BirthDate = value; }
        }

        public bool CheckTag(string name)
        {
            return Entity.CheckTag(name);
            //return Entity.SpecialTags.Contains(name);
        }

        public void SetTag(string name, bool state)
        {
            Entity.SetTag(name, state);
            //var tags = Entity.SpecialTags;
            //if (state) tags.Add(name); else tags.Remove(name);
            //Entity.SpecialTags = tags;
        }

        public float TraitsInspectSkill
        {
            get { return Entity.TraitsInspectedAtSkill ?? 0; }
            set { Entity.TraitsInspectedAtSkill = value; }
        }

        public bool EpicCurve
        {
            get { return Entity.EpicCurve ?? false; }
            set { Entity.EpicCurve = value; }
        }

        public HorseAge Age
        {
            get { return Entity.Age; }
            set { Entity.Age = value; }
        }

        public string Name { get { return Entity.Name; } set { Entity.Name = value; } }
        public string Father { get { return Entity.FatherName; } set { Entity.FatherName = value; } }
        public string Mother { get { return Entity.MotherName; } set { Entity.MotherName = value; } }
        public string TakenCareOfBy { get { return Entity.TakenCareOfBy; } set { Entity.TakenCareOfBy = value; } }
        public string BrandedFor { get { return Entity.BrandedFor; } set { Entity.BrandedFor = value; } }
        public string Comments { get { return Entity.Comments; } set { Entity.Comments = value; } }

        public string Herd
        {
            get { return Entity.Herd; }
            set
            {
                if (this.Herd == value) return;

                var targetHerd = Context.Horses.Where(x => x.Herd == value).Select(x => new Horse(MainForm, x, Context));

                List<Horse> nonuniqueHorses = new List<Horse>();
                foreach (var horse in targetHerd)
                {
                    if (horse.IsIdenticalIdentity(this))
                    {
                        throw new Exception("can not change herd because nonunique creatures already exists in target herd");
                    }
                }

                Entity.Herd = value;
            }
        }

        /// <summary>
        /// Updated: this now only compares horse names due to issues with name+gender setup
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsIdenticalIdentity(Horse other)
        {
            return !this.Entity.IsDifferentIdentityThan(other.Entity);
        }

        //display accessors

        public string HerdAspect { get { return Entity.Herd; } }
        public string NameAspect { get { return Entity.Name; } }
        public string FatherAspect { get { return Entity.FatherName; } }
        public string MotherAspect { get { return Entity.MotherName; } }
        public string TraitsAspect { get { return HorseTrait.GetShortString(Entity.Traits.ToArray(), MainForm.CurrentValuator); } }

        public TimeSpan NotInMoodForAspect
        {
            get
            {
                if (!Entity.NotInMood.HasValue) return TimeSpan.MinValue;
                else
                {
                    return Entity.NotInMood.Value - DateTime.Now;
                }
            }
        }

        public TimeSpan PregnantForAspect
        {
            get
            {
                if (!Entity.PregnantUntil.HasValue) return TimeSpan.MinValue;
                else
                {
                    return Entity.PregnantUntil.Value - DateTime.Now;
                }
            }
        }

        public TimeSpan GroomedAgoAspect
        {
            get
            {
                if (!Entity.GroomedOn.HasValue) return TimeSpan.MaxValue;
                else
                {
                    return DateTime.Now - Entity.GroomedOn.Value;
                }
            }
        }

        public DateTime BirthDateAspect
        {
            get { return Entity.BirthDate ?? DateTime.MinValue; }
        }

        public TimeSpan ExactAgeAspect
        {
            get { return !Entity.BirthDate.HasValue ? TimeSpan.MaxValue : DateTime.Now - Entity.BirthDate.Value; }
        }

        public string GenderAspect
        {
            get
            {
                return Entity.GenderAspect;
            }
        }

        public string TakenCareOfByAspect { get { return Entity.TakenCareOfBy; } }

        public struct TraitsInspectedContainer : IComparable
        {
            public float Skill;
            public bool EpicCurve;

            public override string ToString()
            {
                return EpicCurve == true ? Skill.ToString() + " (epic)" : Skill.ToString();
            }

            public int CompareTo(TraitsInspectedContainer other)
            {
                return Skill.CompareTo(other.Skill);
            }

            public int CompareTo(object obj)
            {
                if (obj == null) return 1;

                if (obj is TraitsInspectedContainer)
                {
                    return CompareTo((TraitsInspectedContainer)obj);
                }
                else throw new ArgumentException("Object is not a TraitsInspectedContained");
            }
        }
        public TraitsInspectedContainer TraitsInspectedAtSkillAspect
        {
            get
            {
                float val = Entity.TraitsInspectedAtSkill.HasValue ? Entity.TraitsInspectedAtSkill.Value : 0;
                return new TraitsInspectedContainer() 
                { 
                    Skill = val, 
                    EpicCurve = Entity.EpicCurve.HasValue ? Entity.EpicCurve.Value : false 
                };
            }
        }


        public HorseAge AgeAspect { get { return this.Age; } }
        public string ColorAspect
        {
            get
            {
                return Entity.Color.EnumVal == HorseColor.Color.Unknown ? string.Empty : Entity.Color.ToString();
            }
        }
        public string TagsAspect { get { return string.Join(", ", Entity.SpecialTags.OrderBy(x => x)); } }
        public string CommentsAspect { get { return Entity.Comments; } }

        public int ValueAspect
        {
            get
            {
                return this.Value;
            }
        }

        public string PotentialValueAspect
        {
            get
            {
                int potPositive = PotentialPositiveValue;
                int potNegative = PotentialNegativeValue;
                return string.Format("{0}, {1}",
                    potPositive > 0 ? "+" + potPositive.ToString() : potPositive.ToString(),
                    potNegative.ToString());
            }
        }

        public double? BreedValueAspect
        {
            get
            {
                return this.CachedBreedValue;
            }
        }

        public string PairedWithAspect
        {
            get
            {
                Horse mate = GetMate();
                if (mate == null) return string.Empty;
                return mate.ToString();
            }
        }

        public string BrandedForAspect
        {
            get
            {
                return Entity.BrandedFor ?? string.Empty;
            }
        }

        public override bool Equals(System.Object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Point return false.
            Horse p = obj as Horse;
            if ((System.Object)p == null)
            {
                return false;
            }

            // Return true if the fields match:
            return this.Entity.ID == p.Entity.ID;
        }

        public bool Equals(Horse p)
        {
            // If parameter is null return false:
            if ((object)p == null)
            {
                return false;
            }

            // Return true if the fields match:
            return this.Entity.ID == p.Entity.ID;
        }

        public override int GetHashCode()
        {
            return this.Entity.ID;
        }

        public static bool operator ==(Horse a, Horse b)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            // Return true if the fields match:
            return a.Equals(b);
        }

        public static bool operator !=(Horse a, Horse b)
        {
            return !(a == b);
        }

        // utility

        public bool NotInMood { get { return this.NotInMoodUntil > DateTime.Now; } }

        public bool Pregnant { get { return this.PregnantUntil > DateTime.Now; } }

        public bool PregnantInLast24h { get { return this.PregnantUntil > DateTime.Now - TimeSpan.FromHours(24); } }

        internal bool IsInbreedWith(Horse otherHorse)
        {
            if (   Name == otherHorse.Mother
                || Name == otherHorse.Father
                || Mother == otherHorse.Name
                || Father == otherHorse.Name
                || (!string.IsNullOrEmpty(Mother) && Mother == otherHorse.Mother)
                || (!string.IsNullOrEmpty(Father) && Father == otherHorse.Father))
                return true;
            else return false;
        }

        internal bool IsFoal()
        {
            return Age.EnumVal == HorseAge.Age.YoungFoal || Age.EnumVal == HorseAge.Age.AdolescentFoal;
        }
    }
}
