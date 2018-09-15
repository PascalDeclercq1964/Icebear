using IceBear;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Icebear_Showcase
{
    public partial class ShowCaseForm : Form
    {
        public ShowCaseForm()
        {
            InitializeComponent();
        }

        NorthwindEntities model = new NorthwindEntities();

        private void Form1_Load(object sender, EventArgs e)
        {

        }



    }
}
