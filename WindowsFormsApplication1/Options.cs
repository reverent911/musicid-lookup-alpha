using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Configuration;

namespace WindowsFormsApplication1
{
    public partial class Options : Form
    {
        public Options()
        {
            InitializeComponent();
        }

        private void Options_Load(object sender, EventArgs e)
        {
           // ConfigurationManager.
            textBox1.Text = Settings1.Default.movefolder;
            textBox2.Text = Settings1.Default.threshold;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Settings1.Default.movefolder = textBox1.Text;
            Settings1.Default.threshold = textBox2.Text;
            Settings1.Default.Save();
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (this.folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox1.Text = this.folderBrowserDialog1.SelectedPath;
            }

        }
    }
}
