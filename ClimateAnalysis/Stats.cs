using System;
using System.Windows.Forms;

namespace ClimateAnalysis
{
    public partial class Stats : Form
    {
        private float[] percentiles = new float[3];
        private int numPerEnsemble = 10;

        public Stats()
        {
            InitializeComponent();
            percentiles[0] = .1f;
            percentiles[1] = .5f;
            percentiles[2] = .9f;
        }

        public float[] getPercentiles() {
            return percentiles;
        }

        public int getNumPerEnsemble() {
            return numPerEnsemble;
        }

        // OK button
        private void button3_Click(object sender, EventArgs e)
        {
            float low, middle, high;

            try {
                numPerEnsemble = int.Parse(textBox3.Text);
                if (numPerEnsemble < 1) 
                    throw new Exception();
            }
            catch (Exception) {
                MessageBox.Show("Please select the number of models to include in each cluster.");
                return;
            }
            try {
                low = float.Parse(textBox4.Text) / 100;
                middle = float.Parse(textBox5.Text) / 100;
                high = float.Parse(textBox6.Text) / 100;

                if (low < 0 || low > 1 || middle < 0 || middle > 1 || high < 0 || high > 1 || middle < low || high < middle)
                    throw new Exception();
            }
            catch (Exception) {
                MessageBox.Show("Please select the percentiles of the clusters.");
                return;
            }

            percentiles = new float[3];//low, middle, high
            percentiles[0] = low;
            percentiles[1] = middle;
            percentiles[2] = high;

            this.Hide();
        }

        // Cancel button
        private void button4_Click(object sender, EventArgs e)
        {
            this.Hide();
        }
    }
}
