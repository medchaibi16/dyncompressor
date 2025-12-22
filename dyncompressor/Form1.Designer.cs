using System.Diagnostics;
using System.Resources;

namespace dyncompressor
{
    partial class Form1
    {
        private Stopwatch stopwatch;
        private Label lblTime;   // clock label
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            // --- Buttons ---
            btnLoadPath = CreateStyledButton("Load Path", Color.FromArgb(0, 120, 215));
            btnSmartFullCompress = CreateStyledButton("Smart Compress", Color.FromArgb(0, 200, 255));
            button1 = CreateStyledButton("Decompress", Color.FromArgb(60, 180, 75));
            button2 = CreateStyledButton("Clear", Color.FromArgb(200, 80, 80));

            // Names (important)
            btnLoadPath.Name = "btnLoadPath";
            btnSmartFullCompress.Name = "btnSmartFullCompress";
            button1.Name = "button1";
            button2.Name = "button2";

            // Event handlers
            btnLoadPath.Click += btnLoadPath_Click;
            btnSmartFullCompress.Click += btnSmartFullCompress_Click;
            button1.Click += button1_Click;
            button2.Click += button2_Click;

            // Add to form
            Controls.Add(btnLoadPath);
            Controls.Add(btnSmartFullCompress);
            Controls.Add(button1);
            Controls.Add(button2);

            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            label1 = new Label();
            lvDropBox = new ListView();
            lblTime = new Label();
            progressBar = new ProgressBar();
            lblStatus = new Label();
            SuspendLayout();
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Top;
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            label1.ForeColor = Color.FromArgb(0, 200, 255);
            label1.Location = new Point(0, 0);
            label1.Name = "label1";
            label1.Size = new Size(315, 41);
            label1.TabIndex = 0;
            label1.Text = "Dynamic Compressor";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lvDropBox
            // 
            lvDropBox.AllowDrop = true;
            lvDropBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lvDropBox.BorderStyle = BorderStyle.FixedSingle;
            lvDropBox.FullRowSelect = true;
            lvDropBox.GridLines = true;
            lvDropBox.Location = new Point(0, 0);
            lvDropBox.Name = "lvDropBox";
            lvDropBox.Size = new Size(121, 97);
            lvDropBox.TabIndex = 1;
            lvDropBox.UseCompatibleStateImageBehavior = false;
            lvDropBox.View = View.Details;
            // 
            // lblTime
            // 
            lblTime.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblTime.AutoSize = true;
            lblTime.Font = new Font("Segoe UI", 9F);
            lblTime.ForeColor = Color.FromArgb(180, 180, 180);
            lblTime.Location = new Point(0, 0);
            lblTime.Name = "lblTime";
            lblTime.Size = new Size(215, 20);
            lblTime.TabIndex = 2;
            lblTime.Text = "Elapsed: 00:00 | Estimated: --:--";
            // 
            // progressBar
            // 
            progressBar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            progressBar.BackColor = Color.FromArgb(60, 60, 70);
            progressBar.ForeColor = Color.FromArgb(0, 200, 255);
            progressBar.Location = new Point(0, 0);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(100, 23);
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.TabIndex = 3;
            progressBar.Visible = false;
            // 
            // lblStatus
            // 
            lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblStatus.AutoSize = true;
            lblStatus.ForeColor = Color.FromArgb(0, 200, 255);
            lblStatus.Location = new Point(0, 0);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(50, 20);
            lblStatus.TabIndex = 4;
            lblStatus.Text = "Ready";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(35, 40, 75);
            ClientSize = new Size(1000, 600);
            Controls.Add(label1);
            Controls.Add(lvDropBox);
            Controls.Add(lblTime);
            Controls.Add(progressBar);
            Controls.Add(lblStatus);
            Font = new Font("Segoe UI", 9F);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(800, 500);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Dynamic Compressor";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();

        }

        // Improved button creation method
        private Button CreateStyledButton(string text, Color bgColor)
        {
            return new Button()
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                BackColor = bgColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point),
                Cursor = Cursors.Hand,
                FlatAppearance = {
            BorderColor = bgColor,
            BorderSize = 0
        },
                Height = 40
            };
        }

        // Add this method to handle dynamic positioning
        private void Form1_Load(object sender, EventArgs e)
        {
            PositionControls();
            this.Resize += (s, e) => PositionControls();
        }

        // Method to dynamically position all controls based on window size
        private void PositionControls()
        {
            int padding = 20;
            int clientWidth = ClientSize.Width;
            int clientHeight = ClientSize.Height;

            // Title - Centered at top with padding
            label1.Location = new Point((clientWidth - label1.Width) / 2, padding);

            // ListView - Main content area
            lvDropBox.Location = new Point(padding, label1.Bottom + padding);
            lvDropBox.Size = new Size(clientWidth - 180 - (2 * padding), clientHeight - 200 - (2 * padding));

            // Right side buttons
            btnLoadPath.Location = new Point(clientWidth - 160, lvDropBox.Top);
            btnLoadPath.Size = new Size(140, 35);

            button2.Location = new Point(clientWidth - 160, btnLoadPath.Bottom + 10);
            button2.Size = new Size(140, 35);

            // Bottom action buttons - centered
            int bottomSectionTop = clientHeight - 80;

            btnSmartFullCompress.Location = new Point((clientWidth / 2) - 160, bottomSectionTop);
            btnSmartFullCompress.Size = new Size(150, 40);

            button1.Location = new Point((clientWidth / 2) + 10, bottomSectionTop);
            button1.Size = new Size(150, 40);

            // Status elements
            progressBar.Location = new Point(padding, clientHeight - 60);
            progressBar.Size = new Size(clientWidth - (2 * padding), 20);

            lblStatus.Location = new Point(padding, progressBar.Top - 25);
            lblTime.Location = new Point(padding, clientHeight - 35);
        }

        #endregion
        private ProgressBar progressBar;
        private Label lblStatus;
        private Label label1;
        private Button btnLoadPath;
        private Button btnSmartFullCompress;
        private Button button1;
        private ListView lvDropBox;
        private Button button2;
    }
}
