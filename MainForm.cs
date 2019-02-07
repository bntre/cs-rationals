using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Rationals.Forms {

    public partial class MainForm : Form {
        public MainForm() {
            InitializeComponent();
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            //
            var viewport = new Torec.Drawing.Viewport(600,600, 0,20, 0,20, false);
            var image = new Torec.Drawing.Gdi.GdiImage(viewport);
            Torec.Drawing.Tests.DrawTest3(image);
            image.Draw(e.Graphics);
        }

    }

    public static class Utils {
        public static void RunForm() {
            Application.EnableVisualStyles();

            var form = new MainForm();
            Application.Run(form);
        }
    }
}
