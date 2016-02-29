using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace ClimateAnalysis
{
    public partial class ImportFromFile : Form
    {
        private string precipFileName = "";
        private string tempFileName = "";
        private string modelFileName = "";
        private ProcessData processor;
        private bool dataLoaded = false;
        private FolderBrowserDialog fbd = new FolderBrowserDialog();
        private ClimateAnalysis parent;
        private int progress = 0;
        private List<double> latList = null, longList = null;//latitude and longitude lists, this info available only if imported files are NetCDF

        public ImportFromFile(ProcessData proc, ClimateAnalysis p1p)
        {
            InitializeComponent();
            processor = proc;
            parent = p1p;
        }

        //check to see if data is loaded
        public Boolean isDataLoaded()
        {
            return dataLoaded;
        }
        public void setDataLoaded(bool b) {
            dataLoaded = b;
        }
        
        public void updateProgressBar(double d) {
            if (d * 100 > progress + 1) {
                progress++;
                progressBar1.Value = progress;
            }

        }

        //browse for precip data
        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                precipFileName = openFileDialog.FileName;
                textBox1.Text = precipFileName;
            }
        }

        //browse for temp data
        private void button2_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                tempFileName = openFileDialog.FileName;
                textBox2.Text = tempFileName;
            }
        }

        //read data into memory
        private void button3_Click(object sender, EventArgs e)
        {
            //read in and check parameters
            if (tempFileName == "" || precipFileName == "")
            {
                MessageBox.Show("Please select a precipitation data file AND a temperature data file to open.");
                return;
            }
            if (tempFileName == precipFileName) {
                MessageBox.Show("The precipitation data file and the temperature data file cannot be the same file.");
                return;
            }

            //if data files are NetCDF, convert them to csv
            string precipExtension = Path.GetExtension(precipFileName);
            string tempExtension = Path.GetExtension(tempFileName);
            try {
                if (precipExtension == ".nc") {
                    precipFileName = convertToCSV(precipFileName);
                    precipFileName = cleanUpCSV(precipFileName);
                }
                if (tempExtension == ".nc") {
                    tempFileName = convertToCSV(tempFileName);
                    tempFileName = cleanUpCSV(tempFileName);
                }
            }
            catch (Exception) {
                MessageBox.Show("There was a problem reading the NetCDF files.  Please make sure the files contain time series data, not statistical analysis.");
                return;
            }

            //import data
            try
            {
                processor.Import(precipFileName, tempFileName);
                if (modelFileName != "")
                    processor.importNames(modelFileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }

            if (checkBox1.Checked) {
                processor.addDataToDatabase(this, longList, latList);
                progress = 0;
                progressBar1.Value = 0;
            }

            if (longList != null)
                processor.addLineLayer(longList, latList);

            this.Hide();

            dataLoaded = true;
            processor.generateChangeFactors();
            
        }

        //cancel button
        private void button4_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

        //stop the red x close button from disposing form
        protected override void OnFormClosing(FormClosingEventArgs e) {
            this.Hide();
            e.Cancel = true;
        }

        //browse for model names
        private void button5_Click(object sender, EventArgs e) {
            if (openFileDialog1.ShowDialog() == DialogResult.OK) {
                modelFileName = openFileDialog1.FileName;
                textBox3.Text = modelFileName;
            }
            else {
                modelFileName = "";
                textBox3.Text = "";
            }
        }

        //convert NetCDF to csv
        private string convertToCSV(string path) {
            //return if file has already been converted
            if (File.Exists(path + ".csv"))
                return path + ".csv";

            //register providers with scientific dataset
            string currentPath = Application.StartupPath + "\\Plugins\\ClimateAnalysis\\Scientific DataSet 1.3\\Microsoft.Research.Science.Data.NetCDF4.dll";
            Microsoft.Research.Science.Data.Factory.DataSetFactory.RegisterAssembly(currentPath);
            currentPath = Application.StartupPath + "\\Plugins\\ClimateAnalysis\\Scientific DataSet 1.3\\Microsoft.Research.Science.Data.CSV.dll";
            Microsoft.Research.Science.Data.Factory.DataSetFactory.RegisterAssembly(currentPath);
            
            Microsoft.Research.Science.Data.DataSet dataSet = Microsoft.Research.Science.Data.DataSet.Open(path);
            dataSet.Clone(path + ".csv");
            return path + ".csv";
        }

        private string cleanUpCSV(string path) {
            string[] lines = System.IO.File.ReadAllLines(path);
            string[] line = lines[0].Split(',');
            int dateColumn = line.Length - 1;
            int colsPerMonth = 0;//the width of the data section
            int rowsPerMonth = 0;//the number of rows for each month
            int numMonths = 0;//the number of months in each projection
            int numProjections = 0;//the number of projections

            //find the number of rows and columns that belong to each month, as well as the number of months and projections
            for (int row = lines.Length - 1; row > 0; row--) {
                line = lines[row].Split(',');
                if (line.Length > 6 && line[6].Contains("projection")) {
                    line = line[6].Split(' ', ':');
                    numProjections = int.Parse(line[1]);
                    numMonths = int.Parse(line[3]);
                    rowsPerMonth = int.Parse(line[5]);
                    colsPerMonth = int.Parse(line[7]);
                    break;
                }
            }

            //find latitude and longitude
            findLatAndLong(lines);

            //find time_origin
            DateTime startDate = new DateTime();
            for (int row = lines.Length - 1; row > 0; row--) {
                line = lines[row].Split(',');
                if (line[1] != "time_origin")
                    continue;

                //the correct line has been found
                startDate = DateTime.Parse(line[3]);
                break;
            }

            //get dates from data
            List<DateTime> months = new List<DateTime>();
            for (int row = 1; row < lines.Length; row++) {
                line = lines[row].Split(',');
                string date = line[dateColumn];
                if (date == "")
                    break;
                months.Add(startDate.AddDays(Double.Parse(date)));
            }

            //create new table of data
            List<double>[] data = new List<double>[numProjections];
            for (int projection = 0; projection < numProjections; projection++) {
                data[projection] = new List<double>();
                for (int row = projection * numMonths; row < months.Count * (projection + 1); row++) {
                    //get the total for each column and row for the current month
                    double monthlyTotal = 0;
                    for (int i = 1; i <= rowsPerMonth; i++) {
                        line = lines[row * rowsPerMonth + i].Split(',');
                        for (int j = 0; j < colsPerMonth; j++) {//need to look at both dimensions and average both rows and columns
                            monthlyTotal += Double.Parse(line[j]);
                        }
                    }
                    monthlyTotal /= ((double)rowsPerMonth * (double)colsPerMonth);//average
                    data[projection].Add(monthlyTotal);
                }
            }

            //create string
            List<string> toWrite = new List<string>();
            for (int month = 0; month < months.Count; month++) {
                string currentRow = "";
                currentRow += months[month].Year;
                currentRow += "," + months[month].Month;
                for (int projection = 0; projection < numProjections; projection++) {
                    currentRow += "," + data[projection][month];
                }
                toWrite.Add(currentRow);
            }

            //replace current file with new file containing cleaned up data
            File.Delete(path);
            path = path.Substring(0, path.Length - 7) + ".csv";
            if (File.Exists(path))
                File.Delete(path);
            File.WriteAllLines(path, toWrite);
            return path;
        }

        private void findLatAndLong(string[] lines) {
            string[] line = lines[0].Split(',');
            int longIndex = 0;
            
            while (line[longIndex] != "longitude") {
                longIndex++;
            }

            if (longIndex == 0)
                return;

            longList = new List<double>();
            latList = new List<double>();

            for (int row = 1; row < lines.Length; row++) {
                line = lines[row].Split(',');
                if (line[longIndex] == "" && line[longIndex + 1] == "")
                    break;
                if (line[longIndex] != "")
                    longList.Add(double.Parse(line[longIndex]));
                if (line[longIndex + 1] != "")
                    latList.Add(double.Parse(line[longIndex + 1]));
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

    }
}
