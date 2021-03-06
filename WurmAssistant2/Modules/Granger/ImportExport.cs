﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Aldurcraft.WurmOnline.WurmAssistant2.ModuleNS.Granger
{
    public class HerdImporter
    {
        public void ImportHerd(GrangerContext context, string newHerdName, string xmlFilePath)
        {
            if (newHerdName == null || newHerdName.Trim() == string.Empty)
            {
                throw new GrangerException("new herd name cannot be empty");
            }

            // check if this herd already exists in database
            if (context.Herds.Any(x => x.HerdID == newHerdName))
            {
                throw new GrangerException(string.Format("there is already a herd with named {0} in database", newHerdName));
            }

            XDocument doc = XDocument.Load(xmlFilePath);
            var horseEntities = new List<HorseEntity>();       
            var elements = doc.Root.Elements("Horse");
            foreach (var x in elements)
            {
                var entity = new HorseEntity();
                entity.Herd = newHerdName;
                entity.Name = x.Element("Name").Value;

                // verify this name is not present in current list
                if (horseEntities.Any(y => y.Name.Equals(entity.Name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    throw new GrangerException(string.Format("Creature named {0} was already added from this XML file. Review the file for any errors.", entity.Name));
                }

                entity.FatherName = x.Element("Father").Value;
                entity.MotherName = x.Element("Mother").Value;
                entity.Traits = GetTraitsFromXML(x.Element("Traits"));

                var notInMood = x.Element("NotInMoodUntil").Value;
                if (string.IsNullOrEmpty(notInMood))
                {
                    entity.NotInMood = null;
                }
                else
                {
                    entity.NotInMood = DateTime.Parse(notInMood, CultureInfo.InvariantCulture);
                }

                var pregnantUntil = x.Element("PregnantUntil").Value;
                if (string.IsNullOrEmpty(pregnantUntil))
                {
                    entity.PregnantUntil = null;
                }
                else
                {
                    entity.PregnantUntil = DateTime.Parse(pregnantUntil, CultureInfo.InvariantCulture);
                }

                var groomedOn = x.Element("GroomedOn").Value;
                if (string.IsNullOrEmpty(groomedOn)) entity.GroomedOn = null;
                else entity.GroomedOn = DateTime.Parse(groomedOn, CultureInfo.InvariantCulture);

                var gender = x.Element("Gender").Value;
                if (string.IsNullOrEmpty(gender)) entity.IsMale = null;
                else entity.IsMale = gender.Equals("male", StringComparison.InvariantCultureIgnoreCase);

                entity.TakenCareOfBy = x.Element("CaredBy").Value;

                var xInspect = x.Element("InspectSkill");
                if (string.IsNullOrEmpty(xInspect.Value)) entity.TraitsInspectedAtSkill = null;
                else entity.TraitsInspectedAtSkill = float.Parse(xInspect.Value, CultureInfo.InvariantCulture);
                var xInspectAttr = xInspect.Attribute("IsEpic");
                if (string.IsNullOrEmpty(xInspectAttr.Value)) entity.EpicCurve = null;
                else entity.EpicCurve = bool.Parse(xInspectAttr.Value);

                entity.Age = HorseAge.CreateAgeFromEnumString(x.Element("Age").Value);
                entity.Color = HorseColor.CreateColorFromEnumString(x.Element("Color").Value);
                entity.Comments = x.Element("Comments").Value;
                entity.SpecialTagsRaw = x.Element("Tags").Value;
                entity.BrandedFor = x.Element("BrandedFor").Value;

                horseEntities.Add(entity);
            }

            context.InsertHerd(newHerdName);
            foreach (var horseEntity in horseEntities)
            {
                horseEntity.ID = HorseEntity.GenerateNewHorseID(context);
                context.InsertHorse(horseEntity);
            }
        }

        List<HorseTrait> GetTraitsFromXML(XElement xTraits)
        {
            var result = new List<HorseTrait>();
            foreach (var xTrait in xTraits.Elements("Trait"))
            {
                result.Add(HorseTrait.FromEnumINTStr(xTrait.Attribute("TraitId").Value));
            }
            return result;
        }
    }

    public class HerdExporter
    {
        public XDocument CreateXML(GrangerContext context, string herdName)
        {
            if (herdName == null) throw new GrangerException("No herd specified");

            var horses = context.Horses.Where(x => x.Herd == herdName).ToArray();

            if (horses.Length == 0)
            {
                throw new GrangerException(string.Format("No creatures found in {0} herd or herd did not exist", herdName));
            }

            XElement root = new XElement("Herd", new XAttribute("OriginalHerdName", herdName));

            foreach (HorseEntity horseEntity in horses)
            {
                var horse =
                    new XElement("Horse",
                                 new XElement("Name", horseEntity.Name),
                                 new XElement("Father", horseEntity.FatherName),
                                 new XElement("Mother", horseEntity.MotherName),
                                 new XElement("Traits", GetTraitListXML(horseEntity)),
                                 new XElement("NotInMoodUntil",
                                              horseEntity.NotInMood.HasValue
                                                  ? horseEntity.NotInMood.Value.ToString(CultureInfo.InvariantCulture)
                                                  : string.Empty),
                                 new XElement("PregnantUntil",
                                              horseEntity.PregnantUntil.HasValue
                                                  ? horseEntity.PregnantUntil.Value.ToString(
                                                      CultureInfo.InvariantCulture)
                                                  : string.Empty),
                                 new XElement("GroomedOn",
                                              horseEntity.GroomedOn.HasValue
                                                  ? horseEntity.GroomedOn.Value.ToString(CultureInfo.InvariantCulture)
                                                  : string.Empty),
                                 new XElement("Gender", GetGender(horseEntity)),
                                 new XElement("CaredBy", horseEntity.TakenCareOfBy),
                                 new XElement("InspectSkill", horseEntity.TraitsInspectedAtSkill,
                                              new XAttribute("IsEpic", horseEntity.EpicCurve.HasValue ? horseEntity.EpicCurve.ToString() : bool.FalseString)),
                                 new XElement("Age", horseEntity.Age),
                                 new XElement("Color", horseEntity.Color),
                                 new XElement("Comments", horseEntity.Comments),
                                 new XElement("Tags", horseEntity.SpecialTagsRaw),
                                 new XElement("BrandedFor", horseEntity.BrandedFor));
                root.Add(horse);
            }

            return new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                root);
        }

        IEnumerable<XElement> GetTraitListXML(HorseEntity horseEntity)
        {
            var result = new List<XElement>();
            foreach (var trait in horseEntity.Traits)
            {
                result.Add(new XElement("Trait", new XAttribute("TraitId", (int)trait.Trait), trait.ToString()));
            }
            return result;
        }

        string GetGender(HorseEntity horseEntity)
        {
            if (horseEntity.IsMale == null) return string.Empty;
            else return horseEntity.IsMale.Value ? "male" : "female";
        }
    }


}
