namespace ImageSegmentation
{
    partial class MainForm
    {
        /// <summary>
        /// Требуется переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Обязательный метод для поддержки конструктора - не изменяйте
        /// содержимое данного метода при помощи редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.textBox_imagePath = new System.Windows.Forms.TextBox();
            this.button_browse = new System.Windows.Forms.Button();
            this.pictureBox_originImage = new System.Windows.Forms.PictureBox();
            this.pictureBox_segmentedImage = new System.Windows.Forms.PictureBox();
            this.button_doSegmentation = new System.Windows.Forms.Button();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_originImage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_segmentedImage)).BeginInit();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // textBox_imagePath
            // 
            this.textBox_imagePath.Location = new System.Drawing.Point(12, 12);
            this.textBox_imagePath.Name = "textBox_imagePath";
            this.textBox_imagePath.Size = new System.Drawing.Size(912, 22);
            this.textBox_imagePath.TabIndex = 0;
            // 
            // button_browse
            // 
            this.button_browse.Location = new System.Drawing.Point(930, 11);
            this.button_browse.Name = "button_browse";
            this.button_browse.Size = new System.Drawing.Size(112, 27);
            this.button_browse.TabIndex = 1;
            this.button_browse.Text = "browse";
            this.button_browse.UseVisualStyleBackColor = true;
            this.button_browse.Click += new System.EventHandler(this.button_browse_Click);
            // 
            // pictureBox_originImage
            // 
            this.pictureBox_originImage.Location = new System.Drawing.Point(12, 40);
            this.pictureBox_originImage.Name = "pictureBox_originImage";
            this.pictureBox_originImage.Size = new System.Drawing.Size(512, 512);
            this.pictureBox_originImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox_originImage.TabIndex = 2;
            this.pictureBox_originImage.TabStop = false;
            // 
            // pictureBox_segmentedImage
            // 
            this.pictureBox_segmentedImage.Location = new System.Drawing.Point(530, 40);
            this.pictureBox_segmentedImage.Name = "pictureBox_segmentedImage";
            this.pictureBox_segmentedImage.Size = new System.Drawing.Size(512, 512);
            this.pictureBox_segmentedImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox_segmentedImage.TabIndex = 3;
            this.pictureBox_segmentedImage.TabStop = false;
            // 
            // button_doSegmentation
            // 
            this.button_doSegmentation.Location = new System.Drawing.Point(446, 558);
            this.button_doSegmentation.Name = "button_doSegmentation";
            this.button_doSegmentation.Size = new System.Drawing.Size(165, 33);
            this.button_doSegmentation.TabIndex = 4;
            this.button_doSegmentation.Text = "Perform segmentation";
            this.button_doSegmentation.UseVisualStyleBackColor = true;
            this.button_doSegmentation.Click += new System.EventHandler(this.button_doSegmentation_Click);
            // 
            // statusStrip
            // 
            this.statusStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip.Location = new System.Drawing.Point(0, 595);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(1056, 24);
            this.statusStrip.TabIndex = 5;
            this.statusStrip.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(0, 19);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1056, 619);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.button_doSegmentation);
            this.Controls.Add(this.pictureBox_segmentedImage);
            this.Controls.Add(this.pictureBox_originImage);
            this.Controls.Add(this.button_browse);
            this.Controls.Add(this.textBox_imagePath);
            this.Name = "MainForm";
            this.Text = "Segmentation";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_originImage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_segmentedImage)).EndInit();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox_imagePath;
        private System.Windows.Forms.Button button_browse;
        private System.Windows.Forms.PictureBox pictureBox_originImage;
        private System.Windows.Forms.PictureBox pictureBox_segmentedImage;
        private System.Windows.Forms.Button button_doSegmentation;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
    }
}

