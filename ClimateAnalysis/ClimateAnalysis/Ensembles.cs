using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ZedGraph;

namespace ClimateAnalysis {
	public partial class Ensembles : Form {

		private ProcessData.Ensemble[] ensembles = null;
		private List<ProcessData.Ensemble> customEnsembles;
		private List<KeyValuePair<int, PointF>> modelList;//GCMs currently selected to form the ensemble
		private float low = -1f, middle, high;
		private int numPerEnsemble = 10;
		private ProcessData processor;
		private ScatterGraph graph;
		private bool thisFormIsVisible = false;
		private Dates datesForm;
		private PointPairList[] pointList = null;
		private int futurePeriod = 0;
		private PointPairList list = null;
		private ZedGraph.LineItem currentSelection;
		private ClimateAnalysis parent;

		#region Public Methods

		public Ensembles(ProcessData proc, ScatterGraph graph, Dates date, ClimateAnalysis p1p) {
			InitializeComponent();

			parent = p1p;

			zedGraphControl1.MouseMoveEvent += new ZedGraphControl.ZedMouseEventHandler(graph.MouseMove1);
			zedGraphControl1.MouseDownEvent += new ZedGraphControl.ZedMouseEventHandler(MouseClick1);

			processor = proc;
			this.graph = graph;
			datesForm = date;
			customEnsembles = new List<ProcessData.Ensemble>();
			modelList = new List<KeyValuePair<int,PointF>>();

			//default scenario
			textBox3.Text = "10";//numPerEnsemble
			textBox4.Text = "10";//low
			textBox5.Text = "50";//middle
			textBox6.Text = "90";//high

			groupBox2.Enabled = false;
			groupBox3.Enabled = false;
			listBox2.Enabled = false;
		}

		public ProcessData.Ensemble[] getEnsembles() {
			if (ensembles == null)
				createStandardEnsembles();
			return ensembles;
		}

		public void fillInDates() {
			//fill future date range combo box
			comboBox1.Items.Clear();
			List<ProcessData.DateRange> dates = datesForm.getDates();
			for (int i = 1; i < dates.Count; i++)
				comboBox1.Items.Add(dates[i].ToString());
			if (futurePeriod >= comboBox1.Items.Count)
				futurePeriod = 0;
			comboBox1.SelectedIndex = futurePeriod;
			thisFormIsVisible = true;
		}

		public void update() {
			futurePeriod = 0;
			updateGraph();
		}

		#endregion Public Methods

		#region Button Handlers

		private void addButton_Click(object sender, EventArgs e) {
			String ensembleName;

			//get name
			try {
				ensembleName = textBox1.Text;
			} catch (Exception) {
				MessageBox.Show("Please provide a name for this ensemble.");
				return;
			}

			if (radioButton3.Checked) {// statistical ensemble
				float precip, temp;
				int numModels;

				//get statistical data from form
				try {
					temp = float.Parse(textBox7.Text) / 100;
					precip = float.Parse(textBox8.Text) / 100;

					if (temp < 0 || temp > 1 || precip < 0 || precip > 1)
						throw new Exception();
				} catch (Exception) {
					MessageBox.Show("Something is wrong with the percentiles you entered.");
					return;
				}
				try {
					numModels = int.Parse(textBox2.Text);
					if (numModels < 1)
						throw new Exception();
				} catch (Exception) {
						MessageBox.Show("Something is wrong with the number of models per ensemble you entered.");
						return;
				}

				//create new ensemble
				ProcessData.Ensemble ensemble = new ProcessData.Ensemble(ensembleName);
				ensemble.statistical = true;
				ensemble.tempPercent = temp;
				ensemble.precipPercent = precip;
				ensemble.numberOfModels = numModels;

				//add ensemble to custom list
				customEnsembles.Add(ensemble);

			} else {//custom ensemble
				if (modelList.Count == 0) {
					MessageBox.Show("At least one GCM is required to make an ensemble.");
					return;
				}

				//create new ensemble
				ProcessData.Ensemble ensemble = new ProcessData.Ensemble(ensembleName);
				ensemble.statistical = false;
				ensemble.numberOfModels = modelList.Count;
				int[,] columnNumbers = new int[1, modelList.Count];
				for (int i = 0; i < modelList.Count; i++) {
					columnNumbers[0, i] = modelList[i].Key + 2;
				}
				ensemble.columnNumbers = columnNumbers;

				// clear list
				clearCustomEnsemble();

				//add ensemble to custom list
				customEnsembles.Add(ensemble);
			}

			//update gui
			textBox1.Text = "";
			updateEnsembleListBox();
			updateGraph();
		}

		private void removeButton_Click(object sender, EventArgs e) {
			if (customEnsembles.Count == 0)
				return;

			if (listBox1.SelectedIndex == -1)
				return;

			//remove ensemble from list
			int index = listBox1.SelectedIndex;
			customEnsembles.RemoveAt(index);

			//update gui
			updateEnsembleListBox();
			updateGraph();
		}

		private void okButton_Click(object sender, EventArgs e) {
			if (radioButton1.Checked)//use standard ensembles
				createStandardEnsembles();
			else {//use custom ensembles
				ensembles = new ProcessData.Ensemble[customEnsembles.Count];
				for (int i = 0; i < customEnsembles.Count; i++) {
					ensembles[i] = customEnsembles[i];
				}
			}
			thisFormIsVisible = false;
			this.Hide();
		}

		private void cancelButton_Click(object sender, EventArgs e) {
			thisFormIsVisible = false;
			this.Hide();
		}

		#endregion Button Handlers

		#region Private Methods

		private bool createStandardEnsembles() {
			ensembles = new ProcessData.Ensemble[5];

			//get data from form
			try {
				low = float.Parse(textBox4.Text) / 100;
				middle = float.Parse(textBox5.Text) / 100;
				high = float.Parse(textBox6.Text) / 100;

				if (low < 0 || low > 1 || middle < 0 || middle > 1 || high < 0 || high > 1)
					throw new Exception();
			} catch (Exception) {
				MessageBox.Show("Something is wrong with the percentiles you entered.");
				return false;
			}
			try {
				numPerEnsemble = int.Parse(textBox3.Text);
				if (numPerEnsemble < 1 || numPerEnsemble > 100)
					throw new Exception();
			} catch (Exception) {
				MessageBox.Show("The number of models per ensemble needs to be 1 and 100 inclusive.");
				return false;
			}

			ProcessData.Ensemble hd = new ProcessData.Ensemble("More Warming/Dry");
			hd.statistical = true;
			hd.precipPercent = low;
			hd.tempPercent = high;
			hd.numberOfModels = numPerEnsemble;
			ensembles[0] = hd;

			ProcessData.Ensemble hw = new ProcessData.Ensemble("More Warming/Wet");
			hw.statistical = true;
			hw.precipPercent = high;
			hw.tempPercent = high;
			hw.numberOfModels = numPerEnsemble;
			ensembles[1] = hw;

			ProcessData.Ensemble mi = new ProcessData.Ensemble("Median");
			mi.statistical = true;
			mi.precipPercent = middle;
			mi.tempPercent = middle;
			mi.numberOfModels = numPerEnsemble;
			ensembles[2] = mi;

			ProcessData.Ensemble wd = new ProcessData.Ensemble("Less Warming/Dry");
			wd.statistical = true;
			wd.precipPercent = low;
			wd.tempPercent = low;
			wd.numberOfModels = numPerEnsemble;
			ensembles[3] = wd;

			ProcessData.Ensemble ww = new ProcessData.Ensemble("Less Warming/Wet");
			ww.statistical = true;
			ww.precipPercent = high;
			ww.tempPercent = low;
			ww.numberOfModels = numPerEnsemble;
			ensembles[4] = ww;

			return true;
		}

		private void updateModelListBox() {
			List<string> list = new List<string>();
			String[] modelNames = processor.getModelNames();
			bool namesAreAvailable = true;
			if (modelNames == null)
				namesAreAvailable = false;

			for (int i = 0; i < modelList.Count(); i++) {
				int colNum = modelList[i].Key;
				if (namesAreAvailable)
					list.Add(modelNames[colNum]);
				else
					list.Add(colNum.ToString());
			}

			listBox2.DataSource = null;
			listBox2.DataSource = list;
		}

		private void updateEnsembleListBox() {
			List<string> list = new List<string>();

			for (int i = 0; i < customEnsembles.Count(); i++) {
				list.Add(customEnsembles[i].ensembleName);
			}

			listBox1.DataSource = null;
			listBox1.DataSource = list;
		}

		private void updateCheckedListBox() {
			modelList.Clear();
			pointList = processor.getPoints();

			foreach (int indexChecked in checkedListBox1.CheckedIndices)
				modelList.Add(new KeyValuePair<int, PointF>(indexChecked, new PointF((float)pointList[futurePeriod][indexChecked].X, (float)pointList[futurePeriod][indexChecked].Y)));

			list = new PointPairList();
			foreach (KeyValuePair<int, PointF> pair in modelList)
				list.Add(new PointPair(pair.Value.X, pair.Value.Y));
			
			updateModelListBox();
			updateGraph();
		}

		private void updateGraph() {
			if (radioButton2.Checked) {//fill ensembles from customEnsembles list
				ensembles = new ProcessData.Ensemble[customEnsembles.Count];
				for (int i = 0; i < customEnsembles.Count; i++) {
					ensembles[i] = customEnsembles[i];
				}
			}

			processor.findEnsembles(getEnsembles());
			graph.drawGraph(zedGraphControl1, futurePeriod);

			if (list != null) {
				currentSelection = zedGraphControl1.GraphPane.AddCurve("", list, Color.Black, SymbolType.Circle);
				currentSelection.Line.IsVisible = false;
				currentSelection.Symbol.Border.IsVisible = true;
				currentSelection.Symbol.Fill.IsVisible = false;
				currentSelection.Symbol.Size = 8;
				zedGraphControl1.Refresh();
			}
		}

		private void clearCustomEnsemble() {
			modelList.Clear();
			updateModelListBox();
			foreach (int i in checkedListBox1.CheckedIndices)
				checkedListBox1.SetItemCheckState(i, CheckState.Unchecked);
			updateCheckedListBox();
		}

		#endregion Private Methods

		#region Event Handlers

		protected override void OnFormClosing(FormClosingEventArgs e) {
			thisFormIsVisible = false;
			this.Hide();
			e.Cancel = true;
		}

		protected override void OnShown(EventArgs e) {
			thisFormIsVisible = true;
			
			//fill checkedListBox
			checkedListBox1.Items.AddRange(processor.getModelNames());

			processor.generateChangeFactors();
			updateGraph();
			base.OnShown(e);
		}

		//standard ensembles low percentile
		private void textBox4_TextChanged(object sender, EventArgs e) {
			if (!thisFormIsVisible)
				return;
			if (textBox4.Text == "")
				return;
			if (createStandardEnsembles())
				updateGraph();
		}

		//standard ensembles middle percentile
		private void textBox5_TextChanged(object sender, EventArgs e) {
			if (!thisFormIsVisible)
				return;
			if (textBox5.Text == "")
				return;
			if (createStandardEnsembles())
				updateGraph();
		}

		//standard ensembles high percentile
		private void textBox6_TextChanged(object sender, EventArgs e) {
			if (!thisFormIsVisible)
				return;
			if (textBox6.Text == "")
				return;
			if (createStandardEnsembles())
				updateGraph();
		}

		//standard ensembles number of models
		private void textBox3_TextChanged(object sender, EventArgs e) {
			if (!thisFormIsVisible)
				return;
			if (textBox3.Text == "")
				return;
			if (createStandardEnsembles())
				updateGraph();
		}

		//checklist box on the right side holding the GCMs
		private void checkedListBox1_ItemCheck(object sender, EventArgs e) {
			this.BeginInvoke((MethodInvoker)delegate {
				updateCheckedListBox();
			});
		}

		//set to standard ensembles
		private void radioButton1_CheckedChanged(object sender, EventArgs e) {
			groupBox1.Enabled = true;
			groupBox2.Enabled = false;
			groupBox3.Enabled = false;
			createStandardEnsembles();
			clearCustomEnsemble();
			updateGraph();
		}

		//set to custom ensembles
		private void radioButton2_CheckedChanged(object sender, EventArgs e) {
			groupBox1.Enabled = false;
			groupBox2.Enabled = true;

			if (radioButton4.Checked) {
				listBox2.Enabled = true;
				groupBox3.Enabled = true;
				groupBox5.Enabled = false;
			}

			updateGraph();
		}

		//set custom ensembles to statistical
		private void radioButton3_CheckedChanged(object sender, EventArgs e) {
			listBox2.Enabled = false;
			groupBox3.Enabled = false;
			groupBox5.Enabled = true;
			clearCustomEnsemble();
		}

		//set custom ensembles to custom
		private void radioButton4_CheckedChanged(object sender, EventArgs e) {
			listBox2.Enabled = true;
			groupBox3.Enabled = true;
			groupBox5.Enabled = false;
		}

		//select model when clicked on in graph
		private bool MouseClick1(object sender, MouseEventArgs e) {
			int column;

			if (!groupBox3.Enabled)
				return false;

			//find closest point
			if (!graph.findClosestPoint(zedGraphControl1, (PointF)e.Location, out column))
				return false;

			if (checkedListBox1.GetItemChecked(column))
				checkedListBox1.SetItemChecked(column, false);
			else
				checkedListBox1.SetItemChecked(column, true);
			
			return true;
		}

		//change of future period
		private void comboBox1_SelectedIndexChanged(object sender, EventArgs e) {
			if (!thisFormIsVisible)
				return;
			futurePeriod = comboBox1.SelectedIndex;
			updateCheckedListBox();
			updateGraph();
		}

		#endregion Event Handlers		
	}
}
