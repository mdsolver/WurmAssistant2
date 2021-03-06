﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Aldurcraft.Utility;

namespace Aldurcraft.WurmOnline.WurmAssistant2.ModuleNS.Granger
{
    public partial class UCGrangerHerdList : UserControl
    {
        FormGrangerMain MainForm;
        GrangerContext Context;

        public UCGrangerHerdList()
        {
            InitializeComponent();
        }

        public void Init(FormGrangerMain mainForm, GrangerContext context)
        {
            MainForm = mainForm;
            Context = context;

            objectListView1.BooleanCheckStateGetter = new BrightIdeasSoftware.BooleanCheckStateGetterDelegate(x =>
                {
                    HerdEntity entity = (HerdEntity)x;
                    return entity.Selected;
                });
            objectListView1.BooleanCheckStatePutter = new BrightIdeasSoftware.BooleanCheckStatePutterDelegate((x, y) =>
                {
                    HerdEntity entity = (HerdEntity)x;
                    Context.UpdateHerdSelectedState(entity.HerdID, y);
                    return entity.Selected;
                });

            Context.OnHerdsModified += RefreshHerdList;
            RefreshHerdList(this, new EventArgs());
        }

        public void RefreshHerdList(object sender, EventArgs e)
        {
            HerdEntity[] Herds = Context.Herds.ToArray();
            objectListView1.SetObjects(Herds, true);
        }

        private void addNewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormHerdName ui = new FormHerdName(Context, MainForm);
            ui.ShowDialog();
        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selHerd = selectedHerd;
            if (selHerd == null) MessageBox.Show("select a herd first");
            else
            {
                FormHerdName ui = new FormHerdName(Context, MainForm, selHerd.HerdID);
                ui.ShowDialog();
            }
        }

        private void combineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selHerd = selectedHerd;
            if (selHerd == null) MessageBox.Show("select a herd first");
            else
            {
                FormHerdMerge ui = new FormHerdMerge(Context, MainForm, selHerd.HerdID);
                ui.ShowDialog();
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selHerd = selectedHerd;
            if (selHerd == null) MessageBox.Show("select a herd first");
            else
            {
                HorseEntity[] horses = Context.Horses.Where(x => x.Herd == selHerd.HerdID).ToArray();
                if (MessageBox.Show("Following herd will be deleted: " + selHerd + "\r\n" + "all creatures in this herd will also be deleted:" + "\r\n"
                    + (horses.Length == 0 ? "no creatures in this herd" : string.Join(", ", (IEnumerable<HorseEntity>)horses)) + "\r\n\r\n" +
                    "Continue?", "confirm", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) == DialogResult.OK)
                {
                    Context.DeleteHerd(selHerd.HerdID);
                }
            }
        }

        HerdEntity selectedHerd { get { return (HerdEntity)objectListView1.SelectedObject; } }

        private void addHorseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selHerd = selectedHerd;
            if (selHerd == null) MessageBox.Show("select a herd first");
            else
            {
                FormHorseViewEdit ui = new FormHorseViewEdit(MainForm, null, Context, HorseViewEditOpType.New, selHerd.HerdID);
                ui.ShowDialog();
            }
        }

        //olv
        private void objectListView1_ItemChecked(object sender, ItemCheckedEventArgs e)
        {

        }

        private void objectListView1_CellRightClick(object sender, BrightIdeasSoftware.CellRightClickEventArgs e)
        {
            if (e.Model == null)
            {
                addHorseToolStripMenuItem.Enabled = false;
                deleteToolStripMenuItem.Enabled = false;
                combineToolStripMenuItem.Enabled = false;
                renameToolStripMenuItem.Enabled = false;
            }
            else
            {
                addHorseToolStripMenuItem.Enabled = true;
                deleteToolStripMenuItem.Enabled = true;
                combineToolStripMenuItem.Enabled = true;
                renameToolStripMenuItem.Enabled = true;
            }
        }
    }
}
