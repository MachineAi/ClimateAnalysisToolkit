using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DotSpatial.Controls;
using DotSpatial.Controls.Docking;
using DotSpatial.Controls.Header;
using ClimateAnalysis.Properties;
using HydroDesktop.Common;

namespace ClimateAnalysis{
    public class ClimateAnalysis : Extension{
        #region Fields
		private SimpleActionItem newButton;
		private ImportFromFile importFromFileForm;
		private ImportFromDB importFromDBForm;
        private download downloadForm;
        private Dates datesForm;
        private Ensembles ensembleForm;
        private Output outputForm;
        private ProcessData processor;
		private ScatterGraph graph;
		private ForcingFile forcingFileForm = null;
		#endregion Fields


		#region Plugin operations

		//Activates the plugin
		public override void Activate() {
			datesForm = new Dates(this);
			processor = new ProcessData(datesForm, (Map)App.Map);
			importFromFileForm = new ImportFromFile(processor, this);
			importFromDBForm = new ImportFromDB(processor, importFromFileForm);
			
			graph = new ScatterGraph(processor, datesForm);
			ensembleForm = new Ensembles(processor, graph, datesForm, this);
            outputForm = new Output(processor, graph);
			datesForm.setProcessData(processor);

            // Initialize the Ribbon controls in the "Climate" ribbon tab
            const string ClimateTabKey = "kClimate";
            App.HeaderControl.Add(new RootItem(ClimateTabKey, "Climate Analysis") { SortOrder = 200 });

            // Create and add Download button
            newButton = new SimpleActionItem("Download", downloadClick);
            newButton.RootKey = ClimateTabKey;
            newButton.ToolTipText = "Download climate data";
            newButton.GroupCaption = "Input";
            newButton.LargeImage = Resources.web;
            App.HeaderControl.Add(newButton);

            // Create and add Import button
            newButton = new SimpleActionItem("Import From File", importFromFileClick);
            newButton.RootKey = ClimateTabKey;
            newButton.ToolTipText = "Import climate data from file";
            newButton.GroupCaption = "Input";
            newButton.LargeImage = Resources.fileIcon;
            App.HeaderControl.Add(newButton);

			// Create and add Import button
			newButton = new SimpleActionItem("Import From Database", importFromDBClick);
			newButton.RootKey = ClimateTabKey;
			newButton.ToolTipText = "Import climate data from the database";
			newButton.GroupCaption = "Input";
			newButton.LargeImage = Resources.Database;
			App.HeaderControl.Add(newButton);

			// Create and add delete button
			newButton = new SimpleActionItem("Clear Imported Data", clearImportedDataClick);
			newButton.RootKey = ClimateTabKey;
			newButton.ToolTipText = "Clear all data the has been imported";
			newButton.GroupCaption = "Input";
			newButton.LargeImage = Resources.Actions_window_close_icon;
			App.HeaderControl.Add(newButton);

			// Create and add Date Range button
			SimpleActionItem dateButton = new SimpleActionItem("Select Dates", dateClick);
			dateButton.RootKey = ClimateTabKey;
			dateButton.ToolTipText = "Select date ranges";
			dateButton.GroupCaption = "Analysis";
			dateButton.LargeImage = Resources.calendar;
			App.HeaderControl.Add(dateButton);

			// Create and add Statistical button
			SimpleActionItem statButton = new SimpleActionItem("Select Scenario", statClick);
			statButton.RootKey = ClimateTabKey;
			statButton.ToolTipText = "Select ensembles";
			statButton.GroupCaption = "Analysis";
			statButton.LargeImage = Resources.bar;
			App.HeaderControl.Add(statButton);

            // Create and add Output button
            SimpleActionItem outputButton = new SimpleActionItem("Save Results", outputClick);
            outputButton.RootKey = ClimateTabKey;
            outputButton.ToolTipText = "Outputs Data Files";
            outputButton.GroupCaption = "Output";
            outputButton.LargeImage = Resources.download;
            App.HeaderControl.Add(outputButton);

            // Create and add Cluster Graph button
            SimpleActionItem clusterButton = new SimpleActionItem("Cluster Graph", clusterClick);
            clusterButton.RootKey = ClimateTabKey;
            clusterButton.ToolTipText = "Shows cluster graph";
            clusterButton.GroupCaption = "Output";
            clusterButton.LargeImage = Resources.chart;
            App.HeaderControl.Add(clusterButton);

			// Create and add Adjust Forcing File button
			SimpleActionItem forcingButton = new SimpleActionItem("Adjust Forcing File", forcingClick);
			forcingButton.RootKey = ClimateTabKey;
			forcingButton.ToolTipText = "Adjusts forcing file";
			forcingButton.GroupCaption = "Output";
			forcingButton.LargeImage = Resources.adjust;
			App.HeaderControl.Add(forcingButton);

            // Create and add About button
            SimpleActionItem AboutButton = new SimpleActionItem("About", AboutClick);
            AboutButton.RootKey = ClimateTabKey;
            AboutButton.ToolTipText = "About Toolkit";
            AboutButton.GroupCaption = "Help";
            AboutButton.LargeImage = Resources.readme1;
            App.HeaderControl.Add(AboutButton);

            // Create and add Help button
            SimpleActionItem howToButton = new SimpleActionItem("Help", howToClick);
            howToButton.RootKey = ClimateTabKey;
            howToButton.ToolTipText = "Read Me";
            howToButton.GroupCaption = "Help";
            howToButton.LargeImage = Resources.help;
            App.HeaderControl.Add(howToButton);

			base.Activate();
		}

		// Deactivates the plugin
		public override void Deactivate() {
			// Remove ribbon tab
			App.HeaderControl.RemoveAll();
			base.Deactivate();
		}

		#endregion Plugin operations

		#region Event Handlers

        //Event handler that responds to Import From File Button being clicked
		void importFromFileClick(object sender, EventArgs e) {
			importFromFileForm.Owner = (App.Map as Map).ParentForm;
			importFromFileForm.Show();
		}

		//Event handler that responds to Import From Database Button being clicked
		void importFromDBClick(object sender, EventArgs e) {
			importFromDBForm.Owner = (App.Map as Map).ParentForm;
			importFromDBForm.updateCheckedListBox();
			importFromDBForm.Show();
		}

		//Event handler that responds to Clear Imported Data button being clicked
		void clearImportedDataClick(object sender, EventArgs e) {
			processor.clearData();
			importFromFileForm.setDataLoaded(false);
		}

        //Event handler that responds to Ouput Button being clicked
        void outputClick(object sender, EventArgs e)
        {
            if (importFromFileForm == null || !importFromFileForm.isDataLoaded())
            {
                MessageBox.Show("Please import climate data before creating files.");
                return;
            }

			processor.generateChangeFactors();
			processor.findEnsembles(ensembleForm.getEnsembles());

            outputForm.Show();
        }

        //Event handler that responds to Date Range button being clicked
        void dateClick(object sender, EventArgs e)
        {
            datesForm.Owner = (App.Map as Map).ParentForm;
            datesForm.Show();
			datesForm.updateList();
        }

        //Event handler that responds to Statistical button being clicked
        void statClick(object sender, EventArgs e)
        {
			//check to see if data has been imported
			if (importFromFileForm == null || !importFromFileForm.isDataLoaded()) {
				MessageBox.Show("Please import climate data before running the analysis.");
				return;
			}

            ensembleForm.Owner = (App.Map as Map).ParentForm;
			ensembleForm.update();
            ensembleForm.Show();
			ensembleForm.fillInDates();
        }

        //Event handler that responds to Cluster Button being clicked
        void clusterClick(object sender, EventArgs e)
        {
			//check to see if data has been loaded
            if (importFromFileForm == null || !importFromFileForm.isDataLoaded()){
                MessageBox.Show("Please import climate data before creating graph.");
                return;
            }

			processor.generateChangeFactors();
			processor.findEnsembles(ensembleForm.getEnsembles());

            //show cluster graph
			graph.drawGraph(null, 0);
			graph.fillInDates();
            graph.Show();
            graph.BringToFront();
        }

		//Event handler that responds to Forcing Button being clicked
		void forcingClick(object sender, EventArgs e) {
			//check to see if data has been loaded
			if (importFromFileForm == null || !importFromFileForm.isDataLoaded()) {
				MessageBox.Show("Please import climate data before adjusting forcing files.");
				return;
			}

			processor.generateChangeFactors();
			processor.findEnsembles(ensembleForm.getEnsembles());

			if (forcingFileForm == null) {
				forcingFileForm = new ForcingFile(outputForm);
				forcingFileForm.Owner = (App.Map as Map).ParentForm;
			}
			processor.generateDeltas();
			forcingFileForm.Show();
		}

        //Event handler that responds to Download Button being clicked
        void downloadClick(object sender, EventArgs e)
        {
            downloadForm = new download(App, processor, importFromFileForm);
            downloadForm.Owner = (App.Map as Map).ParentForm;
            downloadForm.Show();
        }


        //Event handler that responds to Help Button being clicked
        void howToClick(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://climate.codeplex.com/documentation");
        }

        //Event handler that responds to About Button being clicked
        void AboutClick(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://climate.codeplex.com/");
        } 

		#endregion Event Handlers
    }
}
