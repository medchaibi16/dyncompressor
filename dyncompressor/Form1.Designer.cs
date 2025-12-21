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
            SuspendLayout();

            // Form styling - modern theme
            BackColor = Color.FromArgb(35, 40, 75);  // Dark modern background
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            AutoScaleMode = AutoScaleMode.Font;

            // Main title - Centered at top
            label1 = new Label();
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 18F, FontStyle.Bold, GraphicsUnit.Point);
            label1.ForeColor = Color.FromArgb(0, 200, 255);  // Electric blue
            label1.Text = "Dynamic Compressor";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            label1.Anchor = AnchorStyles.Top;

            // ListView with better styling
            lvDropBox = new ListView();
            lvDropBox.AllowDrop = true;
            lvDropBox.FullRowSelect = true;
            lvDropBox.GridLines = true;
            lvDropBox.BorderStyle = BorderStyle.FixedSingle;
            lvDropBox.UseCompatibleStateImageBehavior = false;
            lvDropBox.View = View.Details;
            lvDropBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            // Add columns to ListView
            lvDropBox.Columns.Add("File Name", 300);
            lvDropBox.Columns.Add("Size", 150);
            lvDropBox.Columns.Add("Status", 150);

            // Buttons with modern styling
            btnLoadPath = CreateStyledButton("📁 Load Folder", Color.FromArgb(0, 150, 255));
            btnLoadPath.Click += btnLoadPath_Click;
            btnLoadPath.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            button2 = CreateStyledButton("🗑️ Clear List", Color.FromArgb(255, 87, 87));
            button2.Click += button2_Click;
            button2.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            // Action buttons - centered at bottom
            btnSmartFullCompress = CreateStyledButton("⚡ Smart Compress", Color.FromArgb(0, 200, 83));
            btnSmartFullCompress.Click += btnSmartFullCompress_Click;
            btnSmartFullCompress.Anchor = AnchorStyles.Bottom;

            button1 = CreateStyledButton("📤 Decompress", Color.FromArgb(255, 193, 7));
            button1.Click += button1_Click;
            button1.Anchor = AnchorStyles.Bottom;

            // Time label with better styling
            lblTime = new Label();
            lblTime.AutoSize = true;
            lblTime.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            lblTime.ForeColor = Color.FromArgb(180, 180, 180);
            lblTime.Text = "Elapsed: 00:00 | Estimated: --:--";
            lblTime.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            // Progress bar
            progressBar = new ProgressBar();
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.ForeColor = Color.FromArgb(0, 200, 255);
            progressBar.BackColor = Color.FromArgb(60, 60, 70);
            progressBar.Visible = false;
            progressBar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            // Status label
            lblStatus = new Label();
            lblStatus.AutoSize = true;
            lblStatus.ForeColor = Color.FromArgb(0, 200, 255);
            lblStatus.Text = "Ready";
            lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            // Set control positions dynamically in Form1_Load
            ClientSize = new Size(1000, 600);
            MinimumSize = new Size(800, 500);

            Controls.AddRange(new Control[] {
        label1, lvDropBox, btnLoadPath, button2,
        btnSmartFullCompress, button1, lblTime,
        progressBar, lblStatus
    });

            Name = "Form1";
            Text = "Dynamic Compressor";

            // Handle resize to reposition controls
            Load += Form1_Load;
            ResumeLayout(false);
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
