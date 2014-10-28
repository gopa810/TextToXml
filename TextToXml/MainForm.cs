using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Xml;

namespace TextToXml
{
    public partial class MainForm : Form, ITextParserMachineDelegate
    {
        public TextParserMachine Data = new TextParserMachine();
        public TokenParserMachine DataToken = new TokenParserMachine();

        private Transition selTrans = null;
        private Transition selTrans2 = null;
        public ParserNode Root = null;
        protected Font NodeTypeFont = null;

        public Transition SelectedTransition
        {
            get
            {
                return selTrans;
            }
            set
            {
                if (selTrans != null)
                {
                    selTrans.actions = richTextBox2.Text;
                    selTrans.characters = textBox1.Text;
                }
                selTrans = value;
                if (selTrans != null)
                {
                    richTextBox2.Text = selTrans.actions;
                    textBox1.Text = selTrans.characters;
                }
                else
                {
                    richTextBox2.Text = "";
                    textBox1.Text = "";
                }
            }
        }
        public Transition SelectedTokenTransition
        {
            get
            {
                return selTrans2;
            }
            set
            {
                if (selTrans2 != null)
                {
                    selTrans2.actions = richTextBox5.Text;
                    selTrans2.characters = textBox3.Text;
                }
                selTrans2 = value;
                if (selTrans2 != null)
                {
                    richTextBox5.Text = selTrans2.actions;
                    textBox3.Text = selTrans2.characters;
                }
                else
                {
                    richTextBox5.Text = "";
                    textBox3.Text = "";
                }
            }
        }

        public MainForm()
        {
            NodeTypeFont = SystemFonts.MenuFont;
            InitializeComponent();
            NodeTypeFont = new Font(treeView1.Font, FontStyle.Italic);

            /*XmlDocument doc = new XmlDocument();
            doc.Load("c:\\Users\\peter.kollath\\Documents\\csharp.txml");
            Data.Load(doc);*/
            richTextBox4.Lines = Data.FileExtensions;
            textBox2.Text = Data.ParserName;
            RefreshLists();

            richTextBox1.Text = File.ReadAllText("c:\\Users\\peter.kollath\\Documents\\test_tokens.txt");
        }

        public void RefreshLists()
        {
            SelectedTransition = null;
            listView1.Items.Clear();
            richTextBox3.Text = "";
            foreach (Transition trans in Data.Transitions)
            {
                ListViewItem lvi = new ListViewItem(trans.FromState);
                lvi.SubItems.Add(trans.ToState);
                lvi.SubItems.Add(trans.characters);
                lvi.Tag = trans;
                listView1.Items.Add(lvi);
            }
            
            if (Root != null)
            {
                listView2.Items.Clear();
                foreach (ParserNode pn in Root.Nodes)
                {
                    ListViewItem lvi = new ListViewItem(pn.Name);
                    lvi.SubItems.Add(pn.Type);
                    listView2.Items.Add(lvi);
                }
            }
            listView3.Items.Clear();
            richTextBox5.Text = "";
            foreach (Transition trans in DataToken.Transitions)
            {
                if (trans != null)
                {
                    ListViewItem lvi = new ListViewItem(trans.FromState);
                    lvi.SubItems.Add(trans.ToState);
                    lvi.SubItems.Add(trans.characters);
                    lvi.Tag = trans;
                    listView3.Items.Add(lvi);
                }
            }

            listBox1.Items.Clear();
            foreach (string state in Data.StateID.Keys)
            {
                listBox1.Items.Add(state);
            }

            listBox2.Items.Clear();
            foreach (string action in Data.Actions.Keys)
            {
                listBox2.Items.Add(action);
            }

        }

        public void RefreshLists2()
        {
            treeView1.Nodes.Clear();
            if (Root != null)
                InitTreeOut(treeView1.Nodes, Root);


        }

        public void InitTreeOut(TreeNodeCollection nodes, ParserNode item)
        {
            foreach (ParserNode node in item.Nodes)
            {
                TreeNode tn = nodes.Add(node.Name);
                tn.Tag = node;
                if (node.IsAttribute)
                    tn.ForeColor = Color.SeaGreen;
                if (node.Nodes.Count > 0)
                    InitTreeOut(tn.Nodes, node);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string str = File.ReadAllText(ofd.FileName);
                richTextBox1.Text = str;
            }
        }

        /// <summary>
        /// Run conversion of text.
        /// If something is missing, user will be asked for configuring actions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                TokenParserMachineOld tokens = new TokenParserMachineOld();
                DataContext ctx = new DataContext();
                Data.Delegate = this;
                Data.ParseFile(ctx, "untitled", richTextBox1.Text);
                Root = ctx.Root;
                //Root = tokens.ProcessTokens(ctx.Root.Nodes, Data.ParserName);
                RefreshLists();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message + "\n\n" + ex.StackTrace, "Exception");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            NewTransitionDlg dlg = new NewTransitionDlg();
            dlg.Parser = Data;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                Data.Transitions.Add(dlg.Transition);
                RefreshLists();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                Transition trans = listView1.SelectedItems[0].Tag as Transition;
                NewTransitionDlg dlg = new NewTransitionDlg();
                dlg.Parser = Data;
                dlg.Transition = trans;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    trans.actions = richTextBox2.Text = dlg.Actions;
                    trans.characters = textBox1.Text = dlg.Characters;
                    trans.FromState = listView1.SelectedItems[0].SubItems[0].Text = dlg.FromState;
                    trans.ToState = listView1.SelectedItems[0].SubItems[1].Text = dlg.ToState;
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                ListViewItem lvi = listView1.SelectedItems[0];
                Transition trans = lvi.Tag as Transition;
                if (MessageBox.Show("Do you want to remove transition " + trans.FromState + " -> "
                    + trans.ToState) == DialogResult.OK)
                {
                    listView1.Items.RemoveAt(lvi.Index);
                    Data.Transitions.Remove(trans);
                    RefreshLists();
                }
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                ListViewItem lci = listView1.SelectedItems[0];
                SelectedTransition = lci.Tag as Transition;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            NewActionDlg dlg = new NewActionDlg();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                Data.Actions[dlg.ActionName] = dlg.ActionScript;
                RefreshLists();
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (listBox2.SelectedIndex >= 0)
            {
                string origName;
                NewActionDlg dlg = new NewActionDlg();
                origName = dlg.ActionName = listBox2.SelectedItem as string;
                dlg.ActionScript = Data.Actions[dlg.ActionName];
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    if (origName != dlg.ActionName)
                        Data.Actions.Remove(origName);
                    Data.Actions[dlg.ActionName] = dlg.ActionScript;
                }
                RefreshLists();
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (listBox2.SelectedIndex >= 0)
            {
                string origName;
                origName = listBox2.SelectedItem as string;
                if (MessageBox.Show("Do you want to delete action \"" + origName + "\" ?") == DialogResult.OK)
                {
                    Data.Actions.Remove(origName);
                }
                RefreshLists();
            }
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox2.SelectedIndex >= 0)
            {
                richTextBox3.Text = Data.Actions[listBox2.SelectedItem as string];
            }
        }

        /// <summary>
        /// Loading translation settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button9_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Translator TXML (*.txml)|*.txml";
            ofd.FilterIndex = 1;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(ofd.FileName);
                Data.Load(doc);
                richTextBox4.Lines = Data.FileExtensions;
                textBox2.Text = Data.ParserName;
                RefreshLists();
            }
        }

        /// <summary>
        /// Saving translation settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button10_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Translator TXML (*.txml)|*.txml||";

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                XmlDocument doc = new XmlDocument();
                Data.FileExtensions = richTextBox4.Lines;
                Data.ParserName = textBox2.Text;
                Data.Save(doc);
                doc.Save(sfd.FileName);
            }

        }

        public Transition DelegateProvideNewTransition(ParserMachine Parser, DataContext ctx, char rc)
        {
            Transition trans = null;
            NewTransitionDlg dlg = new NewTransitionDlg();
            dlg.Ctx = ctx;
            dlg.FromState = Parser.StateName[ctx.CurrentState];
            dlg.FromStateEnabled = false;
            dlg.Characters = string.Format("{0}", rc);
            DialogResult result = dlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                trans = dlg.Transition;
                Parser.Transitions.Add(trans);
            }
            else if (result == DialogResult.Yes)
            {
                trans = dlg.UpdatedTransition;
            }
            return trans;
        }

        public Transition DelegateProvideNewTokenTransition(ParserMachine Parser, DataContext ctx, string rc)
        {
            Transition trans = null;
            NewTokenTransitionDlg dlg = new NewTokenTransitionDlg();
            dlg.Ctx = ctx;
            dlg.FromState = Parser.StateName[ctx.CurrentState];
            dlg.FromStateEnabled = false;
            dlg.Characters = rc;
            DialogResult result = dlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                trans = dlg.Transition;
                Parser.Transitions.Add(trans);
            }
            else if (result == DialogResult.Yes)
            {
                trans = dlg.UpdatedTransition;
            }
            return trans;
        }

        public string DelegateProvideUpdatedActions(ParserMachine parser, DataContext ctx, Transition trans)
        {
            NewTransitionDlg dlg = new NewTransitionDlg();
            dlg.Ctx = ctx;
            dlg.Transition = trans;
            dlg.FromStateEnabled = false;
            dlg.ToStateEnabled = false;
            dlg.CharactersEnabled = false;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                return dlg.Actions;
            }

            return trans.actions;
        }

        public string DelegateProvideUpdatedTokenActions(ParserMachine parser, DataContext ctx, Transition trans)
        {
            NewTokenTransitionDlg dlg = new NewTokenTransitionDlg();
            dlg.Ctx = ctx;
            dlg.Transition = trans;
            dlg.FromStateEnabled = false;
            dlg.ToStateEnabled = false;
            dlg.CharactersEnabled = false;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                return dlg.Actions;
            }

            return trans.actions;
        }

        private Rectangle NodeBounds(TreeNode node)
        {
            // Set the return value to the normal node bounds.
            Rectangle bounds = node.Bounds;
            if (node.Tag != null)
            {
                // Retrieve a Graphics object from the TreeView handle 
                // and use it to calculate the display width of the tag.
                Graphics g = treeView1.CreateGraphics();
                int tagWidth = (int)g.MeasureString
                    (node.Tag.ToString(), treeView1.Font).Width + 6;

                // Adjust the node bounds using the calculated value.
                bounds.Offset(tagWidth / 2, 0);
                bounds = Rectangle.Inflate(bounds, tagWidth / 2, 0);
                g.Dispose();
            }

            return bounds;

        }

        private void treeView1_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            // Draw the background and node text for a selected node. 
            if ((e.State & TreeNodeStates.Selected) != 0)
            {
                // Draw the background of the selected node. The NodeBounds 
                // method makes the highlight rectangle large enough to 
                // include the text of a node tag, if one is present.
                e.Graphics.FillRectangle(Brushes.LightYellow, NodeBounds(e.Node));

                // Retrieve the node font. If the node font has not been set, 
                // use the TreeView font.
                Font nodeFont = e.Node.NodeFont;
                if (nodeFont == null) nodeFont = ((TreeView)sender).Font;

                // Draw the node text.
                e.Graphics.DrawString(e.Node.Text, nodeFont, Brushes.Black,
                    Rectangle.Inflate(e.Bounds, 2, 0));
            }

            // Use the default background and node text. 
            else
            {
                e.DrawDefault = true;
            }

            // If a node tag is present, draw its string representation  
            // to the right of the label text. 
            if (e.Node.Tag != null && e.Node.Tag is ParserNode)
            {
                ParserNode pn = e.Node.Tag as ParserNode;
                e.Graphics.DrawString(pn.Type, NodeTypeFont,
                    Brushes.Blue, e.Bounds.Right + 2, e.Bounds.Top);
            }

            // If the node has focus, draw the focus rectangle large, making 
            // it large enough to include the text of the node tag, if present. 
            if ((e.State & TreeNodeStates.Focused) != 0)
            {
                using (Pen focusPen = new Pen(Color.Black))
                {
                    focusPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                    Rectangle focusBounds = NodeBounds(e.Node);
                    focusBounds.Size = new Size(focusBounds.Width - 1,
                    focusBounds.Height - 1);
                    e.Graphics.DrawRectangle(focusPen, focusBounds);
                }
            }
        }

        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            this.listView1.ListViewItemSorter = new ListViewItemComparer(e.Column);
        }

        class ListViewItemComparer : IComparer
        {
            private int col;
            public ListViewItemComparer()
            {
                col = 0;
            }
            public ListViewItemComparer(int column)
            {
                col = column;
            }
            public int Compare(object x, object y)
            {
                return String.Compare(((ListViewItem)x).SubItems[col].Text, ((ListViewItem)y).SubItems[col].Text);
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            RefreshLists();
        }

        // handlers for token rules

        private void button14_Click(object sender, EventArgs e)
        {
            NewTokenTransitionDlg dlg = new NewTokenTransitionDlg();
            dlg.Parser = null;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                DataToken.Transitions.Add(dlg.Transition);
                RefreshLists();
            }
        }

        private void button15_Click(object sender, EventArgs e)
        {
            if (listView3.SelectedItems.Count > 0)
            {
                Transition trans = listView3.SelectedItems[0].Tag as Transition;
                NewTokenTransitionDlg dlg = new NewTokenTransitionDlg();
                dlg.Parser = DataToken;
                dlg.Transition = trans;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    trans.actions = richTextBox2.Text = dlg.Actions;
                    trans.characters = textBox1.Text = dlg.Characters;
                    trans.FromState = listView3.SelectedItems[0].SubItems[0].Text = dlg.FromState;
                    trans.ToState = listView3.SelectedItems[0].SubItems[1].Text = dlg.ToState;
                }
            }
        }

        private void button16_Click(object sender, EventArgs e)
        {
            if (listView3.SelectedItems.Count > 0)
            {
                ListViewItem lvi = listView3.SelectedItems[0];
                Transition trans = lvi.Tag as Transition;
                if (MessageBox.Show("Do you want to remove transition " + trans.FromState + " -> "
                    + trans.ToState) == DialogResult.OK)
                {
                    listView3.Items.RemoveAt(lvi.Index);
                    DataToken.Transitions.Remove(trans);
                    RefreshLists();
                }
            }
        }

        private void listView3_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            this.listView3.ListViewItemSorter = new ListViewItemComparer(e.Column);
        }

        private void listView3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView3.SelectedItems.Count > 0)
            {
                ListViewItem lci = listView3.SelectedItems[0];
                SelectedTokenTransition = lci.Tag as Transition;
            }
        }

        private void button15_Click_1(object sender, EventArgs e)
        {
            try
            {
                if (Data.ParserName == "c#")
                {
                    TokenizerCSharp tokenizer = new TokenizerCSharp();
                    Root = tokenizer.Process(Root);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n\n" + ex.StackTrace, "Exception");
            }

            try
            {
                RefreshLists2();
            }
            catch
            {
            }
        }

        private void button16_Click_1(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Tokenizer KXML (*.kxml)|*.kxml";
            ofd.FilterIndex = 1;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(ofd.FileName);
                DataToken.Load(doc);
                richTextBox4.Lines = DataToken.FileExtensions;
                textBox2.Text = DataToken.ParserName;
                RefreshLists();
            }
        }

        /// <summary>
        /// Saving translation settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button17_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Tokenizer KXML (*.kxml)|*.kxml||";

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                XmlDocument doc = new XmlDocument();
                DataToken.FileExtensions = richTextBox4.Lines;
                DataToken.ParserName = textBox2.Text;
                DataToken.Save(doc);
                doc.Save(sfd.FileName);
            }

        }

    }
}
