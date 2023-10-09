namespace CGA_FIRST
{
    partial class Form1
    {
        /// <summary>
        /// Обязательная переменная конструктора.
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
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.objectPB = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.objectPB)).BeginInit();
            this.SuspendLayout();
            // 
            // objectPB
            // 
            this.objectPB.Location = new System.Drawing.Point(0, -2);
            this.objectPB.Name = "objectPB";
            this.objectPB.Size = new System.Drawing.Size(1530, 963);
            this.objectPB.TabIndex = 0;
            this.objectPB.TabStop = false;
            this.objectPB.MouseDown += new System.Windows.Forms.MouseEventHandler(this.objectPB_MouseDown);
            this.objectPB.MouseMove += new System.Windows.Forms.MouseEventHandler(this.objectPB_MouseMove);
            this.objectPB.MouseUp += new System.Windows.Forms.MouseEventHandler(this.objectPB_MouseUp);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1529, 960);
            this.Controls.Add(this.objectPB);
            this.Name = "Form1";
            this.Text = "Form1";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyDown);
            ((System.ComponentModel.ISupportInitialize)(this.objectPB)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox objectPB;
    }
}

