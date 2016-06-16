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
            pictureBox_originImage.Image = null;
            pictureBox_segmentedImage.Image = null;
            toolStripStatusLabel1.Text = " ";

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
            if (pictureBox_originImage.Image == null)
            {
                MessageBox.Show("Выберите изображение для сегментации", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (numericUpDown_defaultSegmentsNumber.Value <= numericUpDown_requiredSegmentsNumber.Value)
            {
                MessageBox.Show("Начальное количество сегментов должно быть больше желаемого количества сегментов",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (pictureBox_segmentedImage.Image != null)
                pictureBox_segmentedImage.Image = null; 

            System.Diagnostics.Stopwatch swatch = new System.Diagnostics.Stopwatch();
            swatch.Start();

            SegmentedImage segmentedImage = RegionBasedSegmentation.PerformSegmentation(originImage,
                (int)numericUpDown_defaultSegmentsNumber.Value, (int)numericUpDown_requiredSegmentsNumber.Value);

            swatch.Stop();
            toolStripStatusLabel1.Text = "Вермя сегментации: " + swatch.Elapsed.ToString();

            segmentedImage.AverageRegionPixelsColor();

            pictureBox_segmentedImage.Image = segmentedImage.GetBitmapFromSegments();
        }

        private void button_saveResult_Click(object sender, EventArgs e)
        {
            if (pictureBox_segmentedImage.Image == null)
            {
                MessageBox.Show("Нечего сохранять", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

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
