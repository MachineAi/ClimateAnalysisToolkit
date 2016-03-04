using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace ClimateAnalysis
{
    public partial class Output : Form
    {
        private FolderBrowserDialog fbd = new FolderBrowserDialog();
        private string saveToFolderName = "";
        private OutputData dataOut = null;
        private ProcessData processor;
        private ScatterGraph graph;

        public Output(ProcessData proc, ScatterGraph graph)
        {
            InitializeComponent();
            processor = proc;
            this.graph = graph;
        }

        public String getSaveToFolderName() {
            return saveToFolderName;
        }

        public OutputData getOutputData() {
            if (dataOut == null)
                dataOut = new OutputData(processor, saveToFolderName);
            return dataOut;
        }

        //browse for output folder name button
        private void button1_Click(object sender, EventArgs e) {
            if (fbd.ShowDialog() == DialogResult.OK) {
                saveToFolderName = fbd.SelectedPath;
                textBox1.Text = saveToFolderName;
            }
        }

        //create files button
        private void button2_Click(object sender, EventArgs e) {

            //check to make sure inputs are correct
            if (textBox1.Text != saveToFolderName || textBox1.Text == "") {
                MessageBox.Show("Please specify an output folder before creating files.");
                return;
            }

            //check to see if any outputs have been selected
            if (!(checkBox1.Checked || checkBox2.Checked || checkBox3.Checked || checkBox4.Checked || checkBox5.Checked)) {
                MessageBox.Show("Please specify a file to create.");
                return;
            }

            dataOut = new OutputData(processor, saveToFolderName);

            processor.generateDeltas();

            if (checkBox1.Checked) {
                dataOut.writeHybridDeltaEnsemble();
            }
            if (checkBox2.Checked) {
                dataOut.writeHybridEnsemble();
            }
            if (checkBox3.Checked) {
                dataOut.writeDeltaEnsemble();
            }
            if (checkBox4.Checked) {
                dataOut.writeProjectionSummaries();
            }
            if (checkBox5.Checked) {
                graph.drawGraph(null, 0);
                graph.saveGraph(saveToFolderName);
            }

            this.Hide();

            //open output folder
            Process.Start(saveToFolderName);
        }

        //cancel button
        private void button3_Click(object sender, EventArgs e) {
            this.Hide();
        }

        //stop the red x close button from disposing form
        protected override void OnFormClosing(FormClosingEventArgs e) {
            this.Hide();
            e.Cancel = true;
        }
    }
}
