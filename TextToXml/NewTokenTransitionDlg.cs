using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace TextToXml
{
    public partial class NewTokenTransitionDlg : Form
    {
        private Transition p_token_trans = null;
        private DataContext p_ctx = null;
        public ParserMachine Parser = null;
        public Transition UpdatedTransition = null;

        public DataContext Ctx
        {
            get { return p_ctx; }
            set
            {
                p_ctx = value;
                if (p_ctx != null)
                {
                    comboBox1.Items.Clear();
                    foreach (string state in p_ctx.Parser.StateID.Keys)
                    {
                        comboBox1.Items.Add(state);
                    }
                    comboBox2.SelectedIndex = 0;
                    comboBox3.SelectedIndex = 0;
                    listView4.BeginUpdate();
                    int idx = 0;
                    foreach (ParserNode pn in p_ctx.InputNodes)
                    {
                        ListViewItem lvi = new ListViewItem(pn.Name);
                        lvi.SubItems.Add(pn.Type);
                        lvi.Tag = pn;
                        listView4.Items.Add(lvi);
                        if (p_ctx.CurrentNodeIndex == idx)
                            lvi.Selected = true;
                        idx++;
                    }
                    listView4.EndUpdate();
                    try
                    {
                        listView4.EnsureVisible(p_ctx.CurrentNodeIndex);
                    }
                    catch
                    {
                    }

                    listView1.Items.Clear();
                    foreach (Transition t in p_ctx.Parser.Transitions)
                    {
                        if (t.StateA == p_ctx.CurrentState)
                        {
                            ListViewItem lvi = listView1.Items.Add(t.ToState);
                            lvi.SubItems.Add(t.characters);
                            lvi.Tag = t;
                        }
                    }

                    listView2.Items.Clear();
                    foreach (KeyValuePair<string, StringBuilder> pair in p_ctx.Scalars)
                    {
                        listView2.Items.Add(pair.Key).SubItems.Add(pair.Value.ToString());
                    }

                    listView3.Items.Clear();
                    foreach (Transition t in p_ctx.Parser.Transitions)
                    {
                        if (t.StateB == p_ctx.CurrentState)
                        {
                            ListViewItem lvi = listView3.Items.Add(t.ToState);
                            lvi.SubItems.Add(t.characters);
                            lvi.Tag = t;
                        }
                    }

                    Parser = p_ctx.Parser;
                }
            }
        }

        public Transition Transition
        {
            get
            {
                p_token_trans = new Transition(Parser);
                p_token_trans.actions = Actions;
                p_token_trans.characters = Characters;
                p_token_trans.FromState = FromState;
                p_token_trans.ToState = ToState;
                p_token_trans.AttributeName = comboBox2.SelectedItem.ToString();
                p_token_trans.CompareOperatorIndex = comboBox3.SelectedIndex;

                return p_token_trans;
            }
            set
            {
                p_token_trans = value;
                if (p_token_trans != null)
                {
                    FromState = p_token_trans.FromState;
                    ToState = p_token_trans.ToState;
                    Characters = p_token_trans.characters;
                    Actions = p_token_trans.actions;
                    comboBox2.SelectedIndex = comboBox2.FindStringExact(p_token_trans.AttributeName);
                    comboBox3.SelectedIndex = p_token_trans.CompareOperatorIndex;
                }
            }
        }

        public NewTokenTransitionDlg()
        {
            InitializeComponent();
        }

        public string FromState
        {
            get { return textBox1.Text; }
            set { textBox1.Text = value; }
        }

        public string ToState
        {
            get { return comboBox1.Text; }
            set { comboBox1.Text = value; }
        }

        public string Characters
        {
            get { return textBox3.Text; }
            set { textBox3.Text = DataContext.RawStringToRegular(value); }
        }

        public string Actions
        {
            get { return richTextBox1.Text; }
            set { richTextBox1.Text = value; }
        }

        public bool FromStateEnabled
        {
            get { return textBox1.Enabled; }
            set { textBox1.Enabled = value; }
        }
        public bool ToStateEnabled
        {
            get { return comboBox1.Enabled; }
            set { comboBox1.Enabled = value; }
        }
        public bool CharactersEnabled
        {
            get { return textBox3.Enabled; }
            set { textBox3.Enabled = value; }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                Transition tr = listView1.SelectedItems[0].Tag as Transition;
                Actions = tr.actions;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (listView3.SelectedItems.Count > 0)
            {
                Transition tr = listView3.SelectedItems[0].Tag as Transition;
                Actions = tr.actions;
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                Transition tr = listView1.SelectedItems[0].Tag as Transition;
                tr.characters = tr.characters + textBox3.Text;
                UpdatedTransition = tr;
                DialogResult = DialogResult.Yes;
                Close();
            }
        }

        private void textBox3_Enter(object sender, EventArgs e)
        {
            richTextBox3.Text = "Characters:\n\n\\w - all letters\n\\d - all digits\n\\s - space (ASCII = 32)\n\\\\ - slash character\nall other characters" 
                + " write as they are.\n\nExamples:\n\\w\\d_   - this accepts all alphanumeric characters plus underscore character";
        }

        private void richTextBox1_Enter(object sender, EventArgs e)
        {
            richTextBox3.Text = @"Available commands:
append <variable> <value>    - appends value at the end of variable
back <value>      - puts <value> back to input stream
cd <dir>   - changes current directory, cd /, cd .., cd ./name
clr <variable>   - clears variable
mkdir <dir>     - creates directory. 
                  Notation is like in unix, 
                  / for root directory
nodestate <state>   - assigns NodeState for current node
                      this will be used when current dir is changed by cd
set <variable> <value>   - sets new value to variable
transition_if <value1> <value2> <new_state>  - conditionaly 
                 changes to new state if value1 equals value2


*** Names of variables ***
$stat, $buff, $prod

*** Names of arguments ***
$1, $2, $3

by default in main actions for transition, received character is in $1 variable

";

        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                ListViewItem lvi = listView1.SelectedItems[0];
                if (listView1.Tag != null && listView1.Tag is Transition)
                {
                    richTextBox4.Text = (listView1.Tag as Transition).actions;
                }
            }
        }
    }
}
