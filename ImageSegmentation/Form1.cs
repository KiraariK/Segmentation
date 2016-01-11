using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ImageSegmentation.Segmentation;

namespace ImageSegmentation
{
    public partial class MainForm : Form
    {
        Bitmap originImage;
        Bitmap segmentedImage;

        public MainForm()
        {
            InitializeComponent();
        }

        private void button_browse_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Multiselect = false;
            openFile.RestoreDirectory = true;
            openFile.Filter = "Bitmap|*.bmp";

            if (openFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox_imagePath.Text = openFile.FileName;
                originImage = new Bitmap(openFile.FileName);
                pictureBox_originImage.Image = originImage;
            }
        }

        private void button_doSegmentation_Click(object sender, EventArgs e)
        {
            SegmentedImage segmentedImage = RegionBasedSegmentation.PerformSegmentation(originImage);

            segmentedImage.AverageRegionPixelsColor();

            pictureBox_segmentedImage.Image = segmentedImage.GetBitmapFromSegments();

            toolStripStatusLabel1.Text = segmentedImage.Dispersion.ToString();
        }
    }
}
