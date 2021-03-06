﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Aldurcraft.Utility;

namespace Aldurcraft.WurmOnline.WurmAssistant2.ModuleNS.Granger
{
    public partial class FormHerdName : Form
    {
        GrangerContext Context;
        string[] allHerdNames;
        //public string Result;
        Form MainForm;

        string RenamingHerd;

        public FormHerdName(GrangerContext context, Form mainForm, string renamingHerd = null)
        {
            MainForm = mainForm;
            RenamingHerd = renamingHerd;
            Context = context;
            InitializeComponent();
            allHerdNames = context.Herds.Select(x => x.HerdID).ToArray();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            if (RenamingHerd != null)
            {
                try
                {
                    Context.RenameHerd(RenamingHerd, textBox1.Text);
                }
                catch (Exception _e)
                {
                    Logger.LogError("RenameHerd error", this, _e);
                    MessageBox.Show("renaming herd failed, check log for details");
                }
            }
            else
            {
                try
                {
                    Context.InsertHerd(textBox1.Text);
                }
                catch (Exception _e)
                {
                    Logger.LogError("AddHerd error", this, _e);
                    MessageBox.Show("adding herd failed, check log for details");
                }
            }
        }

        bool NameIsValid
        {
            get
            {
                if (textBox1.Text.Length < 1)
                {
                    labelInfo.Text = "name can't be empty";
                    return false;
                }
                else if (textBox1.Text.Length > 20)
                {
                    labelInfo.Text = "name too long";
                    return false;
                }
                else if (!Regex.IsMatch(textBox1.Text, @"^\w+$"))
                {
                    labelInfo.Text = "must be one word made of letters and/or numbers";
                    return false;
                }
                else if (Context.Herds.Where(x => x.HerdID == textBox1.Text).Count() > 0)
                {
                    labelInfo.Text = "herd with this name already exists";
                    return false;
                }
                else
                {
                    labelInfo.Text = "";
                    return true;
                }
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            buttonOK.Enabled = false;
            if (NameIsValid) buttonOK.Enabled = true;
        }

        private void FormHerdName_Load(object sender, EventArgs e)
        {
            this.Location = FormHelper.GetCenteredChildPositionRelativeToParentWorkAreaBound(this, MainForm);
            textBox1.Select();
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                buttonOK.PerformClick();
            }
        }
    }
}
