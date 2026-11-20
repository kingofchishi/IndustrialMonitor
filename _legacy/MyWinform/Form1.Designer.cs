namespace MyWinform
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            label1 = new Label();
            btn_test = new Button();
            btn_test2 = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(63, 35);
            label1.Name = "label1";
            label1.Size = new Size(69, 20);
            label1.TabIndex = 0;
            label1.Text = "文本内容";
            label1.Click += label1_Click;
            // 
            // btn_test
            // 
            btn_test.Location = new Point(330, 194);
            btn_test.Name = "btn_test";
            btn_test.Size = new Size(276, 111);
            btn_test.TabIndex = 1;
            btn_test.Text = "神奇小按钮";
            btn_test.UseVisualStyleBackColor = true;
            btn_test.Click += btn_test_Click;
            // 
            // btn_test2
            // 
            btn_test2.Enabled = false;
            btn_test2.Location = new Point(387, 90);
            btn_test2.Name = "btn_test2";
            btn_test2.Size = new Size(183, 64);
            btn_test2.TabIndex = 2;
            btn_test2.Text = "神奇小按钮2";
            btn_test2.UseVisualStyleBackColor = true;
            btn_test2.Click += btn_test2_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(btn_test2);
            Controls.Add(btn_test);
            Controls.Add(label1);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private Button btn_test;
        private Button btn_test2;
    }
}
