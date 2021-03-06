﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Aldurcraft.Utility;
using System.Windows.Forms;
using CustomColors;

namespace Aldurcraft.WurmOnline.WurmAssistant2.ModuleNS.Granger
{
    public class DefaultBreedingEvaluator : BreedingEvaluator
    {
        [DataContract]
        public class Options
        {
            //ignore settings
            [DataMember]
            public bool IgnoreNotInMood;
            [DataMember]
            public bool IgnorePregnant;
            [DataMember]
            public bool IgnoreRecentlyPregnant;
            [DataMember]
            public bool IgnoreOtherHerds;
            [DataMember]
            public bool IgnorePairedHorses;

            [DataMember]
            public bool IgnoreSold;
            [DataMember]
            public bool IgnoreDead;

            //age ignores
            [DataMember]
            public bool IgnoreFoals;
            [DataMember]
            public bool IgnoreYoung;
            [DataMember]
            public bool IgnoreAdolescent;

            [DataMember]
            public bool AgeIgnoreOnlyOtherHorses;

            //potential value
            [DataMember]
            public bool IncludePotentialValue;

            [DataMember]
            double _PotentialValuePositiveWeight;
            public double PotentialValuePositiveWeight
            {
                get { return _PotentialValuePositiveWeight; }
                set
                {
                    if (_PotentialValuePositiveWeight < 0) _PotentialValuePositiveWeight = 0;
                    else _PotentialValuePositiveWeight = value;
                }
            }

            [DataMember]
            double _PotentialValueNegativeWeight;
            public double PotentialValueNegativeWeight
            {
                get { return _PotentialValueNegativeWeight; }
                set
                {
                    if (_PotentialValueNegativeWeight < 0) _PotentialValueNegativeWeight = 0;
                    else _PotentialValueNegativeWeight = value;
                }
            }

            //prefer missing traits
            [DataMember]
            public bool PreferUniqueTraits;
            [DataMember]
            double _UniqueTraitWeight;
            public double UniqueTraitWeight
            {
                get { return _UniqueTraitWeight; }
                set
                {
                    if (_UniqueTraitWeight < 0) _UniqueTraitWeight = 0;
                    else _UniqueTraitWeight = value;
                }
            }

            //discard on negatives
            [DataMember]
            public bool DiscardOnAnyNegativeTraits;
            [DataMember]
            double _NegativeTraitPenaltyWeight;
            public double BadTraitWeight
            {
                get { return _NegativeTraitPenaltyWeight; }
                set
                {
                    if (_NegativeTraitPenaltyWeight < 0) _NegativeTraitPenaltyWeight = 0;
                    else _NegativeTraitPenaltyWeight = value;
                }
            }

            //discard on inbreeding
            [DataMember]
            public bool DiscardOnInbreeding;
            [DataMember]
            double _InbreedingPenaltyWeight;
            [DataMember]
            public int NumPotentialTraitsToConsider;

            public double InbreedingPenaltyWeight
            {
                get { return _InbreedingPenaltyWeight; }
                set
                {
                    if (_InbreedingPenaltyWeight < 0) _InbreedingPenaltyWeight = 0;
                    else _InbreedingPenaltyWeight = value;
                }
            }

            [DataMember]
            private Dictionary<HorseColor, float> _horseColorValues;

            public IEnumerable<ColorWeight> HorseColorValues
            {
                get { return _horseColorValues.Select(x => new ColorWeight(x.Key, x.Value)).ToArray(); }
                set
                {
                    foreach (var colorWeight in value)
                    {
                        _horseColorValues[colorWeight.Color] = colorWeight.Weight;
                    }
                }
            }

            [DataMember]
            public bool ExcludeExactAgeEnabled;
            [DataMember]
            public TimeSpan ExcludeExactAgeValue;

            public Options()
            {
                InitMe();
            }

            [OnDeserializing]
            public void OnDes(StreamingContext context)
            {
                InitMe();
            }

            void InitMe()
            {
                IgnoreNotInMood = true;
                IgnorePregnant = true;
                IgnoreRecentlyPregnant = true;
                IgnoreOtherHerds = false;
                IgnorePairedHorses = false;
                IgnoreSold = false;

                IgnoreFoals = true;
                IgnoreYoung = true;
                IgnoreAdolescent = false;

                AgeIgnoreOnlyOtherHorses = false;

                IncludePotentialValue = false;
                PotentialValuePositiveWeight = 1.0;
                PotentialValueNegativeWeight = 1.0;

                PreferUniqueTraits = false;
                UniqueTraitWeight = 3.0;

                DiscardOnAnyNegativeTraits = false;
                BadTraitWeight = 1.0;

                DiscardOnInbreeding = true;
                InbreedingPenaltyWeight = 1.0;

                _horseColorValues = new Dictionary<HorseColor, float>();
                BuildInitialColorValuesDict();
            }

            [OnDeserialized]
            void AfterDes(StreamingContext context)
            {
                BuildInitialColorValuesDict();
            }

            void BuildInitialColorValuesDict()
            {
                foreach (var currentColor in HorseColor.GetAll())
                {
                    if (!_horseColorValues.ContainsKey(currentColor))
                    {
                        _horseColorValues.Add(currentColor, 1.0f);
                    }
                }
            }

            public float GetValueForColor(HorseColor color)
            {
                float result = 1.0f;
                _horseColorValues.TryGetValue(color, out result);
                return result;
            }
        }

        public class ColorWeight
        {
            public readonly HorseColor Color;
            public float Weight;

            public ColorWeight(HorseColor color, float weight)
            {
                Color = color;
                Weight = weight;
            }
        }

        Options _options;

        public DefaultBreedingEvaluator()
        {
            _options = new Options();
        }

        public override void SetOptions(object options)
        {
            if (options == null)
            {
                Logger.LogInfo("no options available for evaluator", this);
                return;
            }

            try
            {
                _options = (Options)options;
            }
            catch (Exception _e)
            {
                Logger.LogError("provided options were invalid, using default", this, _e);
            }
        }

        public override object GetOptions()
        {
            return (object)_options;
        }

        public override bool EditOptions()
        {
            BreedingEvaluatorDefaultConfig ui = new BreedingEvaluatorDefaultConfig(_options);
            if (ui.ShowDialog() == DialogResult.OK)
            {
                return true;
            }
            else return false;
        }

        HorseTrait[] GetUniqueTraits(HorseTrait[] allPresentTraits, HorseTrait[] traitGroup1, HorseTrait[] traitGroup2)
        {
            List<HorseTrait> uniqueTraits = new List<HorseTrait>();
            foreach (var trait in allPresentTraits)
            {
                if (!(traitGroup1.Contains(trait) && traitGroup2.Contains(trait)))
                    uniqueTraits.Add(trait);
            }
            return uniqueTraits.ToArray();
        }

        public override BreedingEvalResults? Evaluate(Horse horse1, Horse horse2, TraitValuator valuator)
        {
            if (horse1 == horse2) return null;

            BreedingEvalResults results = new BreedingEvalResults();

            var allPossibleTraits = HorseTrait.GetAllPossibleTraits();
            var traits1 = horse1.Traits;
            var traits2 = horse2.Traits;
            var concatTraits = traits1.Concat<HorseTrait>(traits2).ToArray(); //horse1 + horse2
            var presentTraits = concatTraits.Distinct().ToArray(); //horse1 + horse2 but without duplicates
            // not using these for now:
            //var uniqueTraits = GetUniqueTraits(presentTraits, traits1, traits2);  //traits which only one horse have
            //var missingTraits = HorseTrait.GetAllTraits().Where(x => !presentTraits.Contains(x)).ToArray(); //all traits that horses dont have
            double value2 = horse2.Value;

            if (horse1.IsMale == horse2.IsMale) results.Ignored = true;

            if (_options.IgnoreNotInMood)
                if (horse1.NotInMood || horse2.NotInMood) results.Ignored = true;

            if (_options.IgnorePregnant)
                if (horse1.Pregnant || horse2.Pregnant) results.Ignored = true;

            if (_options.IgnoreRecentlyPregnant)
                if (horse1.PregnantInLast24h || horse2.PregnantInLast24h) results.Ignored = true;

            if (_options.IgnoreOtherHerds)
                if (horse1.Herd != horse2.Herd) results.Ignored = true;

            if (_options.IgnorePairedHorses)
                if (horse1.HasMate() || horse2.HasMate())
                    results.Ignored = true;

            if (_options.IgnoreSold)
                if (horse1.CheckTag("sold") || horse2.CheckTag("sold"))
                    results.Ignored = true;

            if (_options.IgnoreDead)
                if (horse1.CheckTag("dead") || horse2.CheckTag("dead"))
                    results.Ignored = true;

            if (_options.IgnoreFoals)
                if ((horse1.IsFoal() && !_options.AgeIgnoreOnlyOtherHorses) ||
                    horse2.IsFoal()) results.Ignored = true;

            if (_options.IgnoreYoung)
                if (((horse1.Age.EnumVal == HorseAge.Age.Young) && !_options.AgeIgnoreOnlyOtherHorses) ||
                    horse2.Age.EnumVal == HorseAge.Age.Young)
                    results.Ignored = true;

            if (_options.IgnoreAdolescent)
                if (((horse1.Age.EnumVal == HorseAge.Age.Adolescent) && !_options.AgeIgnoreOnlyOtherHorses) ||
                    horse2.Age.EnumVal == HorseAge.Age.Adolescent)
                    results.Ignored = true;

            if (_options.ExcludeExactAgeEnabled)
            {

                if (DateTime.Now - horse1.BirthDate < _options.ExcludeExactAgeValue ||
                    DateTime.Now - horse2.BirthDate < _options.ExcludeExactAgeValue)
                {
                    results.Ignored = true;
                }
            }

            if (horse1.IsInbreedWith(horse2))
            {
                if (_options.DiscardOnInbreeding) results.Discarded = true;
                else
                {
                    // get all potential inbreeding-specific bad traits this horse doesnt yet have,
                    // average a value out of these traits,
                    // multiply by 2 (because this is like both horses having one bad trait)
                    // multiply by inbreed weight (NOT bad trait weight)
                    // we add this to results
                    var potentialBadTraits = HorseTrait.GetInbreedBadTraits().Where(x => !presentTraits.Contains(x)).ToArray();
                    double sum = 0;
                    foreach (var trait in potentialBadTraits)
                    {
                        sum += trait.GetTraitValue(valuator);
                    }
                    sum /= potentialBadTraits.Length;
                    sum *= _options.InbreedingPenaltyWeight * 2;
                    results.Value += sum;
                }
            }

            if (_options.DiscardOnAnyNegativeTraits)
            {
                if (horse2.Traits.Where(x => x.GetTraitValue(valuator) < 0).Count() > 0)
                    results.Discarded = true;
            }

            // continue only if horse is still evaluated
            if (results.Discarded != true && results.Ignored != true)
            {
                // calculate value for each trait:
                // use 1.0, bad trait or unique trait weights if appropriate
                // use dict to check, which traits were already handled, 
                // the value of keys is meaningless, only key presence check is needed
                Dictionary<HorseTrait, int> uniqueTraitCounter = new Dictionary<HorseTrait, int>();
                foreach (var trait in concatTraits)
                {
                    //add this trait to counter for PreferUniqueTraits
                    if (uniqueTraitCounter.ContainsKey(trait))
                    {
                        uniqueTraitCounter[trait] += 1;
                    }
                    else
                    {
                        uniqueTraitCounter[trait] = 1;
                    }
                    var traitval = trait.GetTraitValue(valuator);
                    double result = 0;
                    if (traitval < 0) result += traitval * _options.BadTraitWeight;
                    else if (traitval > 0) result += traitval;

                    results.Value += result;
                }

                //apply bonus for unique traits
                if (_options.PreferUniqueTraits)
                {
                    foreach (var keyval in uniqueTraitCounter)
                    {
                        if (keyval.Value == 1) //this trait was unique in this evaluation
                        {
                            var traitval = keyval.Key.GetTraitValue(valuator);
                            if (traitval > 0) //apply bonus if the trait is positive value
                            {
                                results.Value += (traitval * _options.UniqueTraitWeight) - traitval;
                                //subtracting initial traitval because it was already applied
                                //this works for any weight, a 0.5 weight causes unique trait to have half value for result
                                //0.0 weight causes trait to have 0 value for result (effectively nullifying this trait value)

                                // NOTE: if in future good trait value has any other weights applied,
                                // this WILL break. This class is not expected to be improved,
                                // please write your own, new evaluator by subclassing BreedingEvaluator class
                                // and writing your own complete logic!
                            }
                        }
                    }

                }

                if (_options.IncludePotentialValue)
                {
                    // here we need to take care of potential trait values
                    // this is hard to figure, because horse can contain many different hidden traits,
                    // that all can participate in breeding

                    // we handle this naively, asume horses have all of their potential traits
                    // we regulate how much this affects result with the weight

                    // we need to loop over all possible traits twice, for each horse
                    // pick traits that req AH above their inspect skill and do eval for these
                    // we do this explicitly rather than in methods to improve readability
                    foreach (var trait in allPossibleTraits)
                    {
                        double result = 0;
                        result += EvaluatePotentialValue(horse1, valuator, trait);
                        result += EvaluatePotentialValue(horse2, valuator, trait);
                        results.Value += result;
                    }
                }

                // boost or lower value based on potential color of offspring
                if (results.Value > 0)
                {
                    var h1colVal = _options.GetValueForColor(horse1.Color);
                    var h2colVal = _options.GetValueForColor(horse2.Color);
                    var colValAdj = (h1colVal + h2colVal)*0.5f;
                    results.Value *= colValAdj;
                }
            }
            return results;
        }

        private double EvaluatePotentialValue(Horse horse, TraitValuator valuator, HorseTrait trait)
        {
            double result = 0;
            if (trait.IsUnknownForThisHorse(horse))
            {
                var traitval = trait.GetTraitValue(valuator);
                if (traitval > 0)
                {
                    result += traitval * _options.PotentialValuePositiveWeight;
                }
                else if (traitval < 0)
                {
                    result += traitval * _options.PotentialValueNegativeWeight;
                }
                // we dont care when its 0
            }
            return result;
        }

        static System.Drawing.Color? DefaultIgnoredHintColor = (System.Drawing.Color)(new HSLColor(0D, 0D, 210D));
        static System.Drawing.Color? DefaultExcludedHintColor = (System.Drawing.Color)(new HSLColor(210D, 240D, 0D));

        public override System.Drawing.Color? GetHintColor(Horse horse, double minBreedValue, double maxBreedValue)
        {
            if (horse.CachedBreedValue.HasValue == false) { return DefaultIgnoredHintColor; }
            if (horse.CachedBreedValue == double.PositiveInfinity) { return DefaultExcludedHintColor; }

            CustomColors.HSLColor color = new CustomColors.HSLColor();
            color.Luminosity = 210D;
            color.Saturation = 240D;
            color.Hue = 35D; //0 is red, 35 is yellow, 70 is green
            //for best candidate (maxBreedValue) name-only highlights: HSLColor(120D, 240D, 180D)

            double spectrum = Math.Max(Math.Abs(minBreedValue), Math.Abs(maxBreedValue));
            //normalize breed value to the spectrum
            double normalizedBValue = horse.CachedBreedValue.Value / spectrum;
            color.Hue += normalizedBValue * 35;
            return color;
        }
    }
}