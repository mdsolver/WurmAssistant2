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
    public abstract class BreedingEvaluator
    {
        /// <summary>
        /// get evaluation results for these horse,
        /// null if evaluation does not return any valid results
        /// </summary>
        /// <param name="horse1">first evaluated horse</param>
        /// <param name="horse2">other evaluated horse</param>
        /// <param name="valuator">current trait valuator used by granger</param>
        /// <returns></returns>
        public abstract BreedingEvalResults? Evaluate(Horse horse1, Horse horse2, TraitValuator valuator);
        /// <summary>
        /// set options from outside source
        /// </summary>
        /// <param name="options"></param>
        public abstract void SetOptions(object options);
        /// <summary>
        /// get current options
        /// </summary>
        /// <returns></returns>
        public abstract object GetOptions();
        /// <summary>
        /// let user modify the options, true if any modifications made
        /// </summary>
        /// <returns></returns>
        public abstract bool EditOptions();
        /// <summary>
        /// Determines color for color-cued highlights
        /// </summary>
        /// <param name="horse">evaluated horse</param>
        /// <param name="minBreedValue">minimum breed value for the batch</param>
        /// <param name="maxBreedValue">maximum breed value for the batch</param>
        /// <returns></returns>
        public abstract System.Drawing.Color? GetHintColor(Horse horse, double minBreedValue, double maxBreedValue);
    }

    public struct BreedingEvalResults
    {
        public bool Ignored;
        public bool Discarded;
        public double Value;
    }
}
