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
                int scaleWidth = (int)(originImage.Width * 0.01 * Int32.Parse(comboBox_ImageSize.Text));
                int scaleHeight = (int)(originImage.Height * 0.01 * Int32.Parse(comboBox_ImageSize.Text));
                originImage = ImageProcessing.EasyResizeImage(originImage, new Size(scaleWidth, scaleHeight));
                pictureBox_originImage.Image = originImage;
            }
        }

        private void button_doSegmentation_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Stopwatch swatch = new System.Diagnostics.Stopwatch();
            swatch.Start();

            SegmentedImage segmentedImage = RegionBasedSegmentation.PerformSegmentation(originImage);

            swatch.Stop();
            toolStripStatusLabel1.Text = swatch.Elapsed.ToString();

            segmentedImage.AverageRegionPixelsColor();

            pictureBox_segmentedImage.Image = segmentedImage.GetBitmapFromSegments();

            //toolStripStatusLabel1.Text = segmentedImage.Dispersion.ToString();
        }

        private void button_saveResult_Click(object sender, EventArgs e)
        {
            saveFileDialog.Filter = "Bitmap files (*.bmp)|*.bmp";
            saveFileDialog.FilterIndex = 2;
            saveFileDialog.RestoreDirectory = true;
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                pictureBox_segmentedImage.Image.Save(saveFileDialog.FileName);
            }
        }
    }
}
