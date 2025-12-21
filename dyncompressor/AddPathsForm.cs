using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace dyncompressor
{
    public class AddPathsForm : Form
    {
        private ListBox lstPaths;
        private Button btnAddFiles;
        private Button btnAddFolder;
        private Button btnRemove;
        private Button btnClear;
        private Button btnOk;
        private Button btnCancel;

        public string[] SelectedPaths => lstPaths.Items.Cast<string>().ToArray();

        public AddPathsForm()
        {
            Text = "Add files or folders";
            Width = 700;
            Height = 400;
            StartPosition = FormStartPosition.CenterParent;

            lstPaths = new ListBox() { Left = 10, Top = 10, Width = 660, Height = 260 };
            Controls.Add(lstPaths);

            btnAddFiles = new Button() { Text = "Add Files...", Left = 10, Top = 280, Width = 100 };
            btnAddFolder = new Button() { Text = "Add Folder...", Left = 120, Top = 280, Width = 100 };
            btnRemove = new Button() { Text = "Remove", Left = 230, Top = 280, Width = 80 };
            btnClear = new Button() { Text = "Clear", Left = 320, Top = 280, Width = 80 };
            btnOk = new Button() { Text = "OK", Left = 520, Top = 320, Width = 70 };
            btnCancel = new Button() { Text = "Cancel", Left = 600, Top = 320, Width = 70 };

            Controls.AddRange(new Control[] { btnAddFiles, btnAddFolder, btnRemove, btnClear, btnOk, btnCancel });

            btnAddFiles.Click += BtnAddFiles_Click;
            btnAddFolder.Click += BtnAddFolder_Click;
            btnRemove.Click += (s, e) => { if (lstPaths.SelectedItem != null) lstPaths.Items.Remove(lstPaths.SelectedItem); };
            btnClear.Click += (s, e) => lstPaths.Items.Clear();
            btnOk.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        }

        private void BtnAddFiles_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Multiselect = true;
                ofd.Filter = "All files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    foreach (var f in ofd.FileNames)
                        if (!lstPaths.Items.Contains(f))
                            lstPaths.Items.Add(f);
                }
            }
        }

        private void BtnAddFolder_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select folder to add (you can repeat to add multiple folders)";
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    string folder = fbd.SelectedPath;
                    if (!lstPaths.Items.Contains(folder))
                        lstPaths.Items.Add(folder);
                }
            }
        }
    }
}
