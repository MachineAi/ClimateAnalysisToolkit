using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using HydroDesktop.Database;
using HydroDesktop.Interfaces;
using HydroDesktop.Interfaces.ObjectModel;

namespace ClimateAnalysis {
	public partial class ImportFromDB : Form {
		private ProcessData processor;
		private ImportFromFile import;

		public ImportFromDB(ProcessData proc, ImportFromFile iff) {
			processor = proc;
			import = iff;
			InitializeComponent();
		}

		public void updateCheckedListBox() {
			var repository = RepositoryFactory.Instance.Get<IDataSeriesRepository>();
			DataTable table = repository.GetDetailedSeriesTable();
			DataRowCollection rows = table.Rows;
			List<string> newModelNames = new List<string>();
			string[] currentModelNames = processor.getModelNames();
			string name;
			bool nameAlreadyLoaded = false;

			foreach (DataRow row in rows) {
				name = row.ItemArray[7].ToString();
				nameAlreadyLoaded = false;
				for (int i = 0; i < currentModelNames.Length; i++) {
					if (name.Equals(currentModelNames[i]))
						nameAlreadyLoaded = true;
				}
				for (int i = 0; i < newModelNames.Count; i++) {
					if (name.Equals(newModelNames[i]))
						nameAlreadyLoaded = true;
				}
				if (nameAlreadyLoaded)
					continue;
				else
					newModelNames.Add(name);
			}

			newModelNames.Sort();
			checkedListBox1.Items.Clear();

			foreach (string str in newModelNames) {
				checkedListBox1.Items.Add(str);
			}
		}

		//stop the red x close button from disposing form
		protected override void OnFormClosing(FormClosingEventArgs e) {
			this.Hide();
			e.Cancel = true;
		}

		//select all button click
		private void button1_Click(object sender, EventArgs e) {
			for (int i = 0; i < checkedListBox1.Items.Count; i++ ) {
				checkedListBox1.SetItemChecked(i, true);
			}
		}

		//deselect all button click
		private void button2_Click(object sender, EventArgs e) {
			for (int i = 0; i < checkedListBox1.Items.Count; i++) {
				checkedListBox1.SetItemChecked(i, false);
			}
		}

		//OK button click
		private void button3_Click(object sender, EventArgs e) {
			double[,] precip, temp;
			string[] names;
			CheckedListBox.CheckedItemCollection collection = checkedListBox1.CheckedItems;

			if (collection.Count == 0) {
				this.Hide();
				return;
			}

			var repository1 = RepositoryFactory.Instance.Get<IDataSeriesRepository>();
			var repository2 = RepositoryFactory.Instance.Get<IDataValuesRepository>();
			IList<Series> seriesList = repository1.GetAllSeries();
			IList<double> precipList;
			IList<double> tempList;
			int precipID = 0, tempID = 0;

			precip = new double[1800, collection.Count];
			temp = new double[1800, collection.Count];
			names = new string[collection.Count];

			for (int i = 0; i < collection.Count; i++) {
				names[i] = collection[i].ToString();
				
				for (int j = 0; j < seriesList.Count; j++) {
					if (seriesList[j].Site.ToString().Equals(names[i])) {
						if (seriesList[j].Variable.ToString() == "Precipitation") {
							precipID = (int)seriesList[j].Id;
						}
						if (seriesList[j].Variable.ToString() == "Temperature") {
							tempID = (int)seriesList[j].Id;
						}
					}
				}

				precipList = repository2.GetValues(precipID);
				tempList = repository2.GetValues(tempID);
				for (int row = 0; row < 1800; row++) {
					if (row < precipList.Count) {
						precip[row, i] = precipList[row];
						temp[row, i] = tempList[row];
					}
					else {
						precip[row, i] = Double.NaN;
						temp[row, i] = Double.NaN;
					}
					
				}
			}
	
			processor.addData(precip, temp, names);
			processor.generateChangeFactors();
			import.setDataLoaded(true);
			this.Hide();
		}

		//Cancel button click
		private void button4_Click(object sender, EventArgs e) {
			this.Hide();
		}

		
	}
}
