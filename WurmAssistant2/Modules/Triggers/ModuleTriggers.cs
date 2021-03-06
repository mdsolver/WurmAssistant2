﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.IO;
using System.Windows.Forms;
using Aldurcraft.Utility.SoundEngine;
using Aldurcraft.Utility;
using Aldurcraft.WurmOnline.WurmState;

namespace Aldurcraft.WurmOnline.WurmAssistant2.ModuleNS.Triggers
{
    public class ModuleTriggers : AssistantModule
    {
        [DataContract]
        public class TriggersSettings
        {
            [DataMember]
            public HashSet<string> ActiveCharacterNames = new HashSet<string>();
            [DataMember]
            public bool GlobalMute = false;
            [DataMember]
            float globalVolume = 1.0F;
            public float GlobalVolume
            {
                get { return globalVolume; }
                set
                {
                    globalVolume = GeneralHelper.ConstrainValue<float>(value, 0.0F, 1.0F);
                    SoundBank.ChangeGlobalVolume(globalVolume);
                }
            }

            [DataMember]
            public bool SoundNotifyImportCompleted;
        }

        FormTriggersMain MainUI;
        Dictionary<string, TriggerManager> _triggerManagers = new Dictionary<string, TriggerManager>();
        public PersistentObject<TriggersSettings> Settings;

        public override void Initialize()
        {
            base.Initialize();
            Settings = new PersistentObject<TriggersSettings>(new TriggersSettings());
            Settings.SetFilePathAndLoad(Path.Combine(this.ModuleDataDir, "settings.xml"));

            const string queueSoundModFileName = "QueueSoundMod.txt";
            LogQueueParseHelper.Build(
                Path.Combine(this.ModuleDataDir, queueSoundModFileName),
                Path.Combine(this.ModuleAssetDir, queueSoundModFileName));

            SoundBank.ChangeGlobalVolume(Settings.Value.GlobalVolume);
            MainUI = new FormTriggersMain(this);
            foreach (var name in Settings.Value.ActiveCharacterNames.ToArray())
            {
                AddManager(name);
            }

            if (!Settings.Value.SoundNotifyImportCompleted)
            {
                try
                {
                    var importer = new SoundTriggersImporter(this);
                    var executed = importer.Execute();
                    if (executed)
                    {
                        importer.RenameDir();
                        Settings.Value.SoundNotifyImportCompleted = true;
                        Settings.Save();
                        MainUI.Shown += (sender, args) =>
                                       {
                                           MessageBox.Show(
                                               "Existing Sound Triggers have been imported into new Triggers feature. " +
                                               "If there were any errors or imported triggers are incorrect, please post a bug report in forum thread. " +
                                               "Importing can be repeated if needed, nothing is lost.",
                                               "Wurm Assistant Sound Triggers Importer",
                                               MessageBoxButtons.OK,
                                               MessageBoxIcon.Asterisk);
                                           Application.Restart();
                                       };

                    }
                }
                catch (Exception exception)
                {
                    Logger.LogError("Unknown error while importing Sound Triggers", this, exception);
                    MessageBox.Show(
                        "There was an unforseen error while trying to import Sound Triggers settings to new Triggers. Please report this as soon as possible. Process can be repeated, nothing is lost!", 
                        "OH NOES!", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Error);
                }
            }
        }

        public override void Update(bool engineSleeping)
        {
            Settings.Update();
            foreach (var notifier in _triggerManagers.Values.ToArray())
            {
                notifier.Update();
            }
        }

        public override void OpenUI(object sender, EventArgs e)
        {
            MainUI.ShowThisDarnWindowDammitEx();
        }

        public override void Stop()
        {
            Settings.Save();
            foreach (var notifier in _triggerManagers.Values.ToArray())
            {
                notifier.Stop(new object(), new EventArgs());
            }
        }

        public void AddNewNotifier()
        {
            string[] allPlayers = WurmClient.WurmPaths.GetAllPlayersNames();
            var ui = new FormChoosePlayer(allPlayers.Where(player => !_triggerManagers.ContainsKey(player)).ToArray());
            if (ui.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string[] results = ui.result;
                foreach (var name in results)
                {
                    AddManager(name);
                }
            }
        }

        public void AddManager(string charName)
        {
            var notifier = new TriggerManager(this, charName, this.ModuleDataDir);
            AddManager(notifier);
        }

        public void AddManager(TriggerManager triggerManager)
        {
            MainUI.AddNotifierController(triggerManager.GetUIHandle());
            _triggerManagers.Add(triggerManager.Player, triggerManager);
            Settings.Value.ActiveCharacterNames.Add(triggerManager.Player);
            Settings.DelayedSave();
        }

        public void RemoveManager(TriggerManager notifier)
        {
            _triggerManagers.Remove(notifier.Player);
            Settings.Value.ActiveCharacterNames.Remove(notifier.Player);
            Settings.DelayedSave();
            MainUI.RemoveNotifierController(notifier.GetUIHandle());
        }
    }
}
