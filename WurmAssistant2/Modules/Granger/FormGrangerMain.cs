﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Aldurcraft.Utility;

namespace Aldurcraft.WurmOnline.WurmAssistant2.ModuleNS.Granger
{
    public partial class FormGrangerMain : Form
    {
        public class UserViewChangedEventArgs : EventArgs
        {
            public readonly bool HerdViewVisible;
            public readonly bool TraitViewVisible;
            public UserViewChangedEventArgs(bool herdViewVisible, bool traitViewVisible)
            {
                this.HerdViewVisible = herdViewVisible;
                this.TraitViewVisible = traitViewVisible;
            }
        }
        public event EventHandler<UserViewChangedEventArgs> Granger_UserViewChanged;

        public event EventHandler Granger_ValuatorChanged;
        public event EventHandler Granger_AdvisorChanged;

        public event EventHandler Granger_PlayerListChanged;
        public event EventHandler Granger_SelectedSingleHorseChanged;
        public event EventHandler Granger_TraitViewDisplayModeChanged;

        public PersistentObject<GrangerSettings> Settings;

        private ModuleGranger ParentModule;
        private GrangerContext Context;

        public TraitValuator CurrentValuator { get; private set; }
        public BreedingAdvisor CurrentAdvisor { get; private set; }

        /// <summary>
        /// null if none or more horses selected, else ref to horse
        /// </summary>
        public Horse SelectedSingleHorse { get { return ucGrangerHorseList1.SelectedSingleHorse; } }

        bool _WindowInitCompleted = false;
        public FormGrangerMain(ModuleGranger moduleGranger, PersistentObject<GrangerSettings> settings, GrangerContext context)
        {
            this.ParentModule = moduleGranger;
            this.Settings = settings;
            this.Context = context;

            InitializeComponent();

            RebuildValuePresets();
            RefreshValuator();
            RebuildAdvisors();
            RefreshAdvisor();

            ucGrangerHerdList1.Init(this, context);
            ucGrangerHorseList1.Init(this, context);
            ucGrangerTraitView1.Init(this, context);

            Context.OnTraitValuesModified += Context_OnTraitValuesModified;

            this.Size = Settings.Value.MainWindowSize;

            this.checkBoxCapturingEnabled.Checked = Settings.Value.LogCaptureEnabled;
            this.UpdateViewsVisibility();
            this.Update_textBoxCaptureForPlayers();

            _WindowInitCompleted = true;
        }

        /// <summary>
        /// triggers Granger_SelectedSingleHorseChanged event
        /// </summary>
        internal void TriggerSelectedSingleHorseChanged()
        {
            if (Granger_SelectedSingleHorseChanged != null) 
                Granger_SelectedSingleHorseChanged(this, EventArgs.Empty);
        }

        #region TRAIT VALUES

        internal void InvalidateTraitValuator()
        {
            RebuildValuePresets();
            RefreshValuator();
        }

        void Context_OnTraitValuesModified(object sender, EventArgs e)
        {
            RebuildValuePresets();
            RefreshValuator();
        }

        bool _rebuildingValuePresets = false;
        private void comboBoxValuePreset_TextChanged(object sender, EventArgs e)
        {
            if (!_rebuildingValuePresets)
            {
                RefreshValuator();
            }
        }

        void RebuildValuePresets()
        {
            _rebuildingValuePresets = true;
            comboBoxValuePreset.Items.Clear();
            var valuemaps = Context.TraitValues.AsEnumerable().Select(x => x.ValueMapID).Distinct().ToArray();
            comboBoxValuePreset.Items.Add(TraitValuator.DEFAULT_id);
            comboBoxValuePreset.Items.AddRange(valuemaps);
            if (valuemaps.Contains(Settings.Value.ValuePresetID))
                comboBoxValuePreset.Text = Settings.Value.ValuePresetID;
            else comboBoxValuePreset.Text = TraitValuator.DEFAULT_id;
            _rebuildingValuePresets = false;
        }

        void RefreshValuator()
        {
            try
            {
                CurrentValuator = new TraitValuator(this, comboBoxValuePreset.Text, Context);
            }
            catch (Exception _e)
            {
                CurrentValuator = new TraitValuator(this);
                Logger.LogError("failed to create TraitValuator for valuemapid: " + comboBoxValuePreset.Text + "; using defaults", this, _e);
            }
            Settings.Value.ValuePresetID = CurrentValuator.ValueMapID;
            Settings.DelayedSave();
            if (Granger_ValuatorChanged != null) Granger_ValuatorChanged(this, new EventArgs());
        }

        private void buttonEditValuePreset_Click(object sender, EventArgs e)
        {
            //dialog to edit value presets
            FormEditValuePresets ui = new FormEditValuePresets(Context);
            ui.ShowDialog();
        }

        #endregion

        #region ADVISORS

        bool _rebuildingAdvisors = false;
        void RebuildAdvisors()
        {
            _rebuildingAdvisors = true;
            comboBoxAdvisor.Items.Clear();
            var advisors = BreedingAdvisor.DefaultAdvisorIDs;
            comboBoxAdvisor.Items.AddRange(advisors);
            if (advisors.Contains(Settings.Value.AdvisorID))
                comboBoxAdvisor.Text = Settings.Value.AdvisorID;
            else comboBoxAdvisor.Text = BreedingAdvisor.DEFAULT_id;
            _rebuildingAdvisors = false;
        }

        void RefreshAdvisor()
        {
            try
            {
                CurrentAdvisor = new BreedingAdvisor(this, comboBoxAdvisor.Text, Context);
            }
            catch (Exception _e)
            {
                CurrentAdvisor = new BreedingAdvisor(this, BreedingAdvisor.DEFAULT_id, Context);
                Logger.LogError("failed to create BreedingAdvisor for advisorid: " + comboBoxAdvisor.Text + "; using defaults", this, _e);
            }
            Settings.Value.AdvisorID = CurrentAdvisor.AdvisorID;
            Settings.DelayedSave();
            if (Granger_AdvisorChanged != null) Granger_AdvisorChanged(this, new EventArgs());
        }

        private void comboBoxAdvisor_TextChanged(object sender, EventArgs e)
        {
            if (!_rebuildingAdvisors) RefreshAdvisor();
        }

        private void buttonAdvisorOptions_Click(object sender, EventArgs e)
        {
            if (CurrentAdvisor != null)
            {
                if (CurrentAdvisor.ShowOptions())
                {
                    //do not call refresh advisor, rebuilds are not needed if options are changed!
                    if (Granger_AdvisorChanged != null) Granger_AdvisorChanged(this, new EventArgs());
                }
            }
        }

        #endregion

        #region MISC

        FormGrangerNewInfo NewcomerHelpUI;

        
        private void FormGrangerMain_Load(object sender, EventArgs e)
        {
            try
            {
                splitContainer2.SplitterDistance = Settings.Value.HerdViewSplitterPosition;
                splitContainer2.Panel2Collapsed = Settings.Value.TraitViewVisible;
            }
            catch (Exception _e)
            {
                Logger.LogError("FormGrangerMain_Load", this, _e);
                throw;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (this.Visible && !Settings.Value.DoNotShowReadFirstWindow)
            {
                NewcomerHelpUI = new FormGrangerNewInfo(Settings);
                NewcomerHelpUI.Show(); //show here so its in front
                //TODO remove this when wiki complete
                timer1.Enabled = false;
            }
        }

        internal void BringBackFromAbyss()
        {
            if (this.Visible)
            {
                this.BringToFront();
            }
            else
            {
                this.Show();
                this.RestoreFromMin();
            }
        }

        internal void RestoreFromMin()
        {
            if (this.WindowState == FormWindowState.Minimized) this.WindowState = FormWindowState.Normal;
        }

        private void FormGrangerMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    this.Hide();
                }
                SaveAllState();
            }
            catch (Exception _e)
            {
                Logger.LogError("FormGrangerMain_FormClosing", this, _e);
                throw;
            }
        }

        public void SaveAllState()
        {
            try
            {
                if (!this.IsDisposed)
                {
                    //show panel if hidden, so correct SplitterDistance can be saved
                    ucGrangerTraitView1.SaveStateToSettings();
                    ucGrangerHorseList1.SaveStateToSettings();
                    splitContainer2.Panel2Collapsed = false;
                    Settings.Value.HerdViewSplitterPosition = splitContainer2.SplitterDistance;
                    Settings.Save();
                }
                else Logger.LogError("SaveAllState when already disposed", this);
            }
            catch (Exception _e)
            {
                Logger.LogError("SaveAllState", this, _e);
                throw;
            }
        }

        private void FormGrangerMain_Resize(object sender, EventArgs e)
        {
            try
            {
                if (_WindowInitCompleted)
                {
                    Settings.Value.MainWindowSize = this.Size;
                    Settings.DelayedSave();
                }
            }
            catch (Exception _e)
            {
                Logger.LogError("FormGrangerMain_Resize", this, _e);
                throw;
            }
        }

        #endregion

        #region VIEWS

        private void buttonHerdView_Click(object sender, EventArgs e)
        {
            Settings.Value.HerdViewVisible = !Settings.Value.HerdViewVisible;
            Settings.DelayedSave();
            UpdateViewsVisibility();
        }

        private void buttonTraitView_Click(object sender, EventArgs e)
        {
            splitContainer2.Panel2Collapsed = !splitContainer2.Panel2Collapsed;
            Settings.Value.TraitViewVisible = splitContainer2.Panel2Collapsed;
            Settings.DelayedSave();
            UpdateViewsVisibility();
        }

        void UpdateViewsVisibility()
        {
            if (Settings.Value.HerdViewVisible)
                tableLayoutPanel1.ColumnStyles[0].Width = 150;
            else tableLayoutPanel1.ColumnStyles[0].Width = 0;

            if (Granger_UserViewChanged != null)
                Granger_UserViewChanged(this, new UserViewChangedEventArgs(
                    Settings.Value.HerdViewVisible,
                    Settings.Value.TraitViewVisible));
        }

        public TraitViewManager.TraitDisplayMode TraitViewDisplayMode
        {
            get
            {
                return Settings.Value.TraitViewDisplayMode;
            }
            set
            {
                Settings.Value.TraitViewDisplayMode = value;
                Settings.DelayedSave();
                if (Granger_TraitViewDisplayModeChanged != null) Granger_TraitViewDisplayModeChanged(this, EventArgs.Empty);
            }
        }

        #endregion

        #region LOG TRACKING

        private void checkBoxCapturingEnabled_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Value.LogCaptureEnabled = checkBoxCapturingEnabled.Checked;
            Settings.DelayedSave();
        }

        private void buttonChangePlayers_Click(object sender, EventArgs e)
        {
            FormChoosePlayers dialog = new FormChoosePlayers((Settings.Value.CaptureForPlayers ?? new List<string>()).ToArray());
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Settings.Value.CaptureForPlayers = dialog.Result.ToList();
                Settings.DelayedSave();
                Update_textBoxCaptureForPlayers();
                if (Granger_PlayerListChanged != null) Granger_PlayerListChanged(this, new EventArgs());
            }
        }

        void Update_textBoxCaptureForPlayers()
        {
            textBoxCaptureForPlayers.Text = String.Join(", ", Settings.Value.CaptureForPlayers);
        }

        #endregion

        private void splitContainer2_SplitterMoved(object sender, SplitterEventArgs e)
        {
            
        }

        private void buttonOptions_Click(object sender, EventArgs e)
        {

        }

        private void buttonGrangerGeneralOptions_Click(object sender, EventArgs e)
        {
            var ui = new FormGrangerGeneralOptions(Settings);
            if (ui.ShowDialog() == DialogResult.OK)
            {
                //do something with results?
            }
        }

        private void buttonImportExport_Click(object sender, EventArgs e)
        {
            var ui = new FormGrangerImportExport(Context);
            ui.ShowDialog();
        }
    }
}
