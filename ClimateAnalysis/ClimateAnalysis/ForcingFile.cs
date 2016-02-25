using System;
using System.IO;
using System.Windows.Forms;

namespace ClimateAnalysis {
    public partial class ForcingFile : Form {
        private FolderBrowserDialog fbd = new FolderBrowserDialog();
        private String forcingFileName = "";
        private String saveToFolderName;
        private Output output;
        private bool VIC;

        public ForcingFile(Output output) {
            InitializeComponent();
            this.output = output;
            textBox3.Text = output.getSaveToFolderName();
            saveToFolderName = output.getSaveToFolderName();
        }

        //browse for forcing file
        private void button1_Click(object sender, EventArgs e) {
            if (openFileDialog1.ShowDialog() == DialogResult.OK) {
                forcingFileName = openFileDialog1.FileName;
                textBox1.Text = forcingFileName;
            }
        }

        //browse for output folder
        private void button3_Click(object sender, EventArgs e) {
            if (fbd.ShowDialog() == DialogResult.OK) {
                saveToFolderName = fbd.SelectedPath;
                textBox3.Text = saveToFolderName;
            }
        }

        //adjust button
        private void button2_Click(object sender, EventArgs e) {
            OutputData dataOut = output.getOutputData();
            DateTime date = new DateTime();

            if (saveToFolderName == "") {
                MessageBox.Show("Please specify an output folder.");
                return;
            }

            if (forcingFileName == "") {
                MessageBox.Show("Please specify an forcing file to adjust.");
                return;
            }

            dataOut.setSaveToFolderName(saveToFolderName);

            if (radioButton1.Checked)
                VIC = true;
            else
                VIC = false;

            if (VIC) {
                try {
                    date = DateTime.Parse(textBox2.Text);
                }
                catch (Exception) {
                    MessageBox.Show("Date is not in the correct format.");
                    return;
                }
            }

            if (checkBox2.Checked) {
                string folder = Path.GetDirectoryName(forcingFileName);
                string[] files = Directory.GetFiles(folder);
                foreach (string s in files) {
                    try {
                        dataOut.adjustForcingFile(s, VIC, checkBox1.Checked, date);
                    }
                    catch (Exception) {
                        MessageBox.Show("Error parsing: " + s);
                    }
                }
            }
            else {
                dataOut.adjustForcingFile(forcingFileName, VIC, checkBox1.Checked, date);
            }

            this.Hide();

            //open output folder
            System.Diagnostics.Process.Start(saveToFolderName);
        }

        //cancel button
        private void button4_Click(object sender, EventArgs e) {
            this.Hide();
        }

        //stop the red x close button from disposing form
        protected override void OnFormClosing(FormClosingEventArgs e) {
            this.Hide();
            e.Cancel = true;
        }
    }
}
