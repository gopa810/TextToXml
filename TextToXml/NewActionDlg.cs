using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TextToXml
{
    public partial class NewActionDlg : Form
    {
        public NewActionDlg()
        {
            InitializeComponent();
        }

        public string ActionName
        {
            get { return textBox1.Text; }
            set { textBox1.Text = value; }
        }

        public string ActionScript
        {
            get { return richTextBox1.Text; }
            set { richTextBox1.Text = value; }
        }
    }
}
