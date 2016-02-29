using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ClimateAnalysis
{
    public partial class Dates : Form
    {
        Boolean summerOnly = false;
        List<ProcessData.DateRange> dates = new List<ProcessData.DateRange>();
        ProcessData processor;
        ClimateAnalysis parent;

        public Dates(ClimateAnalysis p1p)
        {
            InitializeComponent();

            parent = p1p;

            this.comboBox1.SelectedIndex = 9;
            this.comboBox2.SelectedIndex = 8;
            this.comboBox3.SelectedIndex = 9;
            this.comboBox4.SelectedIndex = 8;
            this.comboBox5.SelectedIndex = 0;
            this.comboBox6.SelectedIndex = 50;
            this.comboBox7.SelectedIndex = 90;
            this.comboBox8.SelectedIndex = 120;
            this.comboBox9.SelectedIndex = 1;
            //default historical
            dates.Add(new ProcessData.DateRange(10, 1950, 9, 2000));
        }

        public List<ProcessData.DateRange> DateRange
        {
            get {
                //add default future if no future in list
                if (dates.Count == 1)
                    dates.Add(new ProcessData.DateRange(10, 2030, 9, 2060));
                return dates;
            }
            internal set {
                dates = value;
            }
        }

        public bool isSummerOnly() {
            return summerOnly;
        }

        public void updateList() {
            List<string> list = new List<string>();

            for (int i = 1; i < dates.Count(); i++) {
                ProcessData.DateRange range = dates[i];
                list.Add(range.ToString());
            }

            listBox1.DataSource = null;
            listBox1.DataSource = list;
        }

        public void setProcessData(ProcessData proc) {
            processor = proc;
        }

        //add button
        private void button1_Click(object sender, EventArgs e) {
            int startMonth, startYear, endMonth, endYear;
            startMonth = comboBox3.SelectedIndex + 1;
            startYear = int.Parse(comboBox7.SelectedItem.ToString());
            endMonth = comboBox4.SelectedIndex + 1;
            endYear = int.Parse(comboBox8.SelectedItem.ToString());
            dates.Add(new ProcessData.DateRange(startMonth, startYear, endMonth, endYear));
            updateList();
        }

        //delete button
        private void button2_Click(object sender, EventArgs e) {
            int index = listBox1.SelectedIndex + 1;
            if (index >= dates.Count)
                return;
            dates.RemoveAt(index);
            updateList();
        }

        //ok button
        private void button3_Click(object sender, EventArgs e) {
            int startMonth, startYear, endMonth, endYear;

            //historical period
            startMonth = comboBox1.SelectedIndex + 1;
            startYear = int.Parse(comboBox5.SelectedItem.ToString());
            endMonth = comboBox2.SelectedIndex + 1;
            endYear = int.Parse(comboBox6.SelectedItem.ToString());
            dates[0] = new ProcessData.DateRange(startMonth, startYear, endMonth, endYear);

            if (dates.Count == 1) {
                startMonth = comboBox3.SelectedIndex + 1;
                startYear = int.Parse(comboBox7.SelectedItem.ToString());
                endMonth = comboBox4.SelectedIndex + 1;
                endYear = int.Parse(comboBox8.SelectedItem.ToString());
                dates.Add(new ProcessData.DateRange(startMonth, startYear, endMonth, endYear));
            }

            if (comboBox9.SelectedIndex == 0)
                summerOnly = true;
            else
                summerOnly = false;

            this.Hide();

            processor.generateChangeFactors();
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

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void comboBox5_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void comboBox9_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
