using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IcebearDemo
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            List<ReportDemo> reportDemos = new List<ReportDemo>() {
                new ReportDemo(){Name="Customer list"}
            };

            listBox1.DataSource = reportDemos;
            listBox1.DisplayMember = "Name";
        }

        class ReportDemo
        {
            public string Name { get; set; }
            public string ReportName { get; set; }
        }
    }
}
