using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PreviewDemo.plugin
{
    public partial class Notice : Form
    {
        public Notice(string str)
        {
            InitializeComponent();
            label1.Text = str;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Hide();
        }
    }
}
