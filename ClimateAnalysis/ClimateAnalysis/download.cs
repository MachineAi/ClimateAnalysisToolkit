using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DotSpatial.Controls;
using DotSpatial.Data;
using HydroDesktop;
using IronPython.Hosting;
using DotSpatial.Symbology;
using System.Net;

namespace ClimateAnalysis
{
    public partial class download : Form
    {
        private FolderBrowserDialog fbd = new FolderBrowserDialog();
        private string saveToFolderName = "";
        private AreaSelectMode CurrentAreaSelectMode;
        private Map map;
        private RectangleDrawing _rectangleDrawing;
        public readonly SearchSettings _searchSettings = SearchSettings.Instance;
        private bool _useCurrentView;
        private string selectedModel1;
        private string selectedModel2;
        private string selectedModel3;
        Boolean currentlyRemoving = false;
        private string futOne;
        private string futTwo;
        private string futThree;
        private string futFour;
        private string futFive;
        private string futSix;
        private string histOne;
        private string histTwo;
        private string filePath;
		private ProcessData processor;
		private ImportFromFile import;
		private Progress prog;
		private AppManager app;

        private enum AreaSelectMode
        {
            None,
            DrawBox,
            SelectPolygons,
            SelectAttribute
        }

        public download(AppManager app, ProcessData proc, ImportFromFile imp)
        {
            InitializeComponent();
            map = (app.Map as Map);
			this.app = app;
			processor = proc;
			import = imp;

            //add models
            comboBox1.Items.Add("ACCESS1-0");
            comboBox1.Items.Add("ACCESS1-3");
            //comboBox1.Items.Add("bcc-csm1-1");
            comboBox1.Items.Add("bcc-csm1-1-m");
            comboBox1.Items.Add("BNU-ESM");
            comboBox1.Items.Add("CanESM2");
            comboBox1.Items.Add("CCSM4");
            comboBox1.Items.Add("CESM1-BGC");

            comboBox1.Items.Add("CESM1-CAM5");
            comboBox1.Items.Add("CMCC-CM");
            comboBox1.Items.Add("CNRM-CM5");
            comboBox1.Items.Add("CSIRO-Mk3-6-0");
            comboBox1.Items.Add("EC-EARTH");
            comboBox1.Items.Add("FGOALS-g2");
            comboBox1.Items.Add("FGOALS-s2");

            comboBox1.Items.Add("FIO-ESM");
            comboBox1.Items.Add("GFDL-CM3");
            comboBox1.Items.Add("GFDL-ESM2G");
            comboBox1.Items.Add("GFDL-ESM2M");
            comboBox1.Items.Add("GISS-E2-H-CC");
            comboBox1.Items.Add("GISS-E2-R");
            comboBox1.Items.Add("GISS-E2-R-CC");

            //comboBox1.Items.Add("HadGEM2-AO");
            //comboBox1.Items.Add("HadGEM2-CC");
            //comboBox1.Items.Add("HadGEM2-ES");
            comboBox1.Items.Add("inmcm4");
            comboBox1.Items.Add("IPSL-CM5A-LR");
            comboBox1.Items.Add("IPSL-CM5A-MR");
            comboBox1.Items.Add("ISPL-CM5B-LR");
            comboBox1.Items.Add("MIROC-ESM");

            comboBox1.Items.Add("MIROC-ESM-CHEM");
            comboBox1.Items.Add("MIROC5");
            comboBox1.Items.Add("MPI-ESM-LR");
            comboBox1.Items.Add("MPI-ESM-LR");

        }

		private void callPythonCode() {

            var options = new Dictionary<string, object>();
            options["Frames"] = true;
            options["FullFrames"] = true;

			//create ironpython engine and scope
			Microsoft.Scripting.Hosting.ScriptEngine py = Python.CreateEngine(options);
			Microsoft.Scripting.Hosting.ScriptScope scope = py.CreateScope();

			//add python folder to pythonpath
			string pythonCodePath = Application.StartupPath + "/Plugins/ClimateAnalysis/IronPython/";
            string pyGDPPath = Application.StartupPath + "/Plugins/ClimateAnalysis/IronPython/pyGDP-master/";
            string OWSPath = Application.StartupPath + "/Plugins/ClimateAnalysis/IronPython/owslib/";
            string owslibPath = Application.StartupPath + "/Plugins/ClimateAnalysis/IronPython/OWS/OWSLib-0.8.6/owslib/";
            string libPath = Application.StartupPath + "/Plugins/ClimateAnalysis/IronPython/Lib/";
			ICollection<string> paths = py.GetSearchPaths();
			paths.Add(pythonCodePath);
            paths.Add(pyGDPPath);
            paths.Add(OWSPath);
            paths.Add(owslibPath);
            paths.Add(libPath);
			py.SetSearchPaths(paths);

			//store variables in scope
            scope.SetVariable("futOne",futOne);
            scope.SetVariable("futTwo",futTwo);
            scope.SetVariable("futThree", futThree);
            scope.SetVariable("futFour", futFour);
            scope.SetVariable("futFive", futFive);
            scope.SetVariable("futSix", futSix);
            scope.SetVariable("histOne", histOne);
            scope.SetVariable("histTwo", histTwo);
            scope.SetVariable("filePath", filePath);

			//run code
			py.ExecuteFile(Application.StartupPath + "/Plugins/ClimateAnalysis/IronPython/module1.py", scope);

            string outputPathF2 = scope.GetVariable("outputPathF2").ToString();
            string outputPathH1 = scope.GetVariable("outputPathH1").ToString();

            //retrieve climate data
            filePath = filePath.Remove(filePath.Length - 9);

            string urlF2 = "http://cida.usgs.gov:80/gdp/process/RetrieveResultServlet?id=" + outputPathF2;
            WebClient webClient = new WebClient();
            webClient.DownloadFile(urlF2, @filePath + "FutureClimateData.csv");

            string urlH1 = "http://cida.usgs.gov:80/gdp/process/RetrieveResultServlet?id=" + outputPathH1;
            WebClient webClient2 = new WebClient();
            webClient2.DownloadFile(urlH1, @filePath + "HistoricalClimateData.csv");

			return;
        }

		private void createPrecipTempAndProjectionsFiles() {
			string[] futureLines = System.IO.File.ReadAllLines(filePath + "/FutureClimateData.csv");
			string[] historicalLines = System.IO.File.ReadAllLines(filePath + "/HistoricalClimateData.csv");
			string[] currentLine = null;
			DateTime dt;
			double value;
			bool precip = false;
			List<List<KeyValuePair<DateTime, double>>> historicalPrecipData = new List<List<KeyValuePair<DateTime, double>>>();
			List<List<KeyValuePair<DateTime, double>>> futurePrecipData = new List<List<KeyValuePair<DateTime, double>>>();
			List<List<KeyValuePair<DateTime, double>>> historicalTempData = new List<List<KeyValuePair<DateTime, double>>>();
			List<List<KeyValuePair<DateTime, double>>> futureTempData = new List<List<KeyValuePair<DateTime, double>>>();
			List<KeyValuePair<DateTime, double>> currentList = null;
			List<String> projectionNames = new List<string>();
			//process historical data
			for (int row = 0; row < historicalLines.Length; row++) {
				if (historicalLines[row].Contains('#')) {
					//save currentList if it has data
					if (row != 0)
						if (precip)
							historicalPrecipData.Add(currentList);
						else
							historicalTempData.Add(currentList);
					currentList = new List<KeyValuePair<DateTime, double>>();
					precip = !precip;
					row += 2;
				} else {
					currentLine = historicalLines[row].Split(',');
					dt = DateTime.Parse(currentLine[0]);
					value = Double.Parse(currentLine[1]);
					currentList.Add(new KeyValuePair<DateTime,double>(dt, value));
				}
			}
			historicalTempData.Add(currentList);

			//process future data
			for (int row = 0; row < futureLines.Length; row++) {
				if (futureLines[row].Contains('#')) {
					//save projection name
					if (precip) {
						string[] info = futureLines[row].Split('_');
						projectionNames.Add(info[4] + "." + info[6].ElementAt(1) + "." + info[5]);
					}
					//save currentList if it has data
					if (row != 0)
						if (precip)
							futurePrecipData.Add(currentList);
						else
							futureTempData.Add(currentList);
					//create new list
					currentList = new List<KeyValuePair<DateTime, double>>();
					precip = !precip;
					row += 2;
				}
				else {
					currentLine = futureLines[row].Split(',');
					dt = DateTime.Parse(currentLine[0]);
					value = Double.Parse(currentLine[1]);
					currentList.Add(new KeyValuePair<DateTime, double>(dt, value));
				}
			}
			futureTempData.Add(currentList);

			//write precip file
			string precipFileName = filePath + "/precip_data.csv";
			List<String> lines = new List<string>();
			for (int row = 0; row < historicalPrecipData[0].Count; row++) {
				String str = historicalPrecipData[0][row].Key.Year + ", " + historicalPrecipData[0][row].Key.Month;
				for (int col = 0; col < historicalPrecipData.Count; col++) {
					str += ", " + historicalPrecipData[col][row].Value;
				}
				lines.Add(str);
			}
			for (int row = 0; row < futurePrecipData[0].Count; row++) {
				String str = futurePrecipData[0][row].Key.Year + ", " + futurePrecipData[0][row].Key.Month;
				for (int col = 0; col < futurePrecipData.Count; col++) {
					str += ", " + futurePrecipData[col][row].Value;
				}
				lines.Add(str);
			}
			System.IO.File.WriteAllLines(precipFileName, lines);

			//write temp file
			string tempFileName = filePath + "/temp_data.csv";
			lines = new List<string>();
			for (int row = 0; row < historicalTempData[0].Count; row++) {
				String str = historicalTempData[0][row].Key.Year + ", " + historicalTempData[0][row].Key.Month;
				for (int col = 0; col < historicalTempData.Count; col++) {
					str += ", " + historicalTempData[col][row].Value;
				}
				lines.Add(str);
			}
			for (int row = 0; row < futureTempData[0].Count; row++) {
				String str = futureTempData[0][row].Key.Year + ", " + futureTempData[0][row].Key.Month;
				for (int col = 0; col < futureTempData.Count; col++) {
					str += ", " + futureTempData[col][row].Value;
				}
				lines.Add(str);
			}
			System.IO.File.WriteAllLines(tempFileName, lines);

			//write projections file
			string modelFileName = filePath + "/Projections.txt";
			lines = new List<string>();
			foreach (String str in projectionNames) {
				lines.Add(str);
			}
			System.IO.File.WriteAllLines(modelFileName, lines);

			processor.Import(precipFileName, tempFileName);
			processor.importNames(modelFileName);
			import.setDataLoaded(true);
			processor.generateChangeFactors();
			processor.addDataToDatabase(import, null, null);
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        //download button
        private async void button5_Click(object sender, EventArgs e)
        {
            filePath = "";
            filePath = textBox1.Text;

			if (filePath == "") {
				MessageBox.Show("Please select a file path");
				return;
			}
			
            //export rectangle as shapefile
            Boolean overwrite = true;
            FeatureSet copy=null;
            FeatureSet fs = (FeatureSet)map.Layers[map.Layers.Count - 1].DataSet;

            //identify selected feature and export as shapefile
            foreach (IMapGroup item in map.GetAllGroups())
            {
                foreach (DotSpatial.Symbology.ILayer item2 in item)
                {
                    try
                    {
                        IFeatureLayer ifea = item2 as IFeatureLayer;
                        if (ifea.Selection.Count > 0)
                        {
                            copy = new FeatureSet(DotSpatial.Topology.FeatureType.Polygon);
                            copy.Projection = map.Projection;
                            copy.DataTable.Columns.Add("id");

                            for (int i = 0; i < ifea.Selection.Count; i++)
			                  {
                                IFeature fea = copy.AddFeature(ifea.Selection.ToFeatureList()[i]);
                                fea.DataRow["id"] = i;
                            }
                        }
                    }
                    catch
                    {


			                   }

                        }


                    }



                    
            //identify selected watershed and export as shapefile
            foreach (IFeatureLayer item2 in map.GetFeatureLayers())
            {
                try
                {
                    IFeatureLayer ifea = item2 as IFeatureLayer;
                    if (ifea.Selection.Count > 0)
                    {
                        copy = new FeatureSet(DotSpatial.Topology.FeatureType.Polygon);
                        copy.Projection = map.Projection;
                        copy.DataTable.Columns.Add("id");
                     
                        for (int i = 0; i < ifea.Selection.Count; i++)
                        {
                            IFeature fea = copy.AddFeature(ifea.Selection.ToFeatureList()[i]);
                            fea.DataRow["id"] = i;
                        }
                    }
                    }
                catch
                {


                }
                
              
            }

            if (fs == null && copy == null)
            {
                MessageBox.Show("Please select a feature on the map or draw a region");
                return;
            }

            if (copy != null)
            {
            filePath = filePath + "\\shape.shp";
            copy.SaveAs(filePath, overwrite); 
            }


            else if (fs != null)
            {
            filePath = filePath + "\\shape.shp";
            fs.SaveAs(filePath, overwrite);
            }

            // Compose a string consisting of ESPG WKT for web mercator
            string lines = "PROJCS[\"WGS 84 / Pseudo-Mercator\",\r\n  GEOGCS[\"WGS 84\",\r\n    DATUM[\"World Geodetic System 1984\",\r\n      SPHEROID[\"WGS 84\", 6378137.0, 298.257223563, AUTHORITY[\"EPSG\",\"7030\"]],\r\n      AUTHORITY[\"EPSG\",\"6326\"]],\r\n    PRIMEM[\"Greenwich\", 0.0, AUTHORITY[\"EPSG\",\"8901\"]],\r\n    UNIT[\"degree\", 0.017453292519943295],\r\n    AXIS[\"Geodetic longitude\", EAST],\r\n    AXIS[\"Geodetic latitude\", NORTH],\r\n    AUTHORITY[\"EPSG\",\"4326\"]],\r\n  PROJECTION[\"Popular Visualisation Pseudo Mercator\", AUTHORITY[\"EPSG\",\"1024\"]],\r\n  PARAMETER[\"semi_minor\", 6378137.0],\r\n  PARAMETER[\"latitude_of_origin\", 0.0],\r\n  PARAMETER[\"central_meridian\", 0.0],\r\n  PARAMETER[\"scale_factor\", 1.0],\r\n  PARAMETER[\"false_easting\", 0.0],\r\n  PARAMETER[\"false_northing\", 0.0],\r\n  UNIT[\"m\", 1.0],\r\n  AXIS[\"Easting\", EAST],\r\n  AXIS[\"Northing\", NORTH],\r\n  AUTHORITY[\"EPSG\",\"3857\"]]";

            // Write the string to the .prj file
            filePath = filePath.Remove(filePath.Length - 3) + "prj";
            System.IO.StreamWriter file = new System.IO.StreamWriter(filePath);
            file.WriteLine(lines);
            file.Close();
            filePath = filePath.Remove(filePath.Length - 3) + "shp";

            //create 6 strings of models to be passed to python

            futOne = "";
            for (int models = 0; models < listBox1.Items.Count; models++)
            {
                if ((listBox1.Items[models].Equals("HadGEM2-ES") && listBox2.Items[models].Equals("26") && listBox3.Items[models].Equals("1")) || (listBox1.Items[models].Equals("HadGEM2-ES") && listBox2.Items[models].Equals("45") && listBox3.Items[models].Equals("1")) || (listBox1.Items[models].Equals("HadGEM2-ES") && listBox2.Items[models].Equals("60") && listBox3.Items[models].Equals("1"))) 
                {
                    futOne = futOne + "BCSD_0-125deg_pr_Amon_" + listBox1.Items[models].ToString() + "_" + listBox2.Items[models].ToString() + "_r" + listBox3.Items[models].ToString() + "i1p1_200512-209911," + "BCSD_0-125deg_tas_Amon_" + listBox1.Items[models].ToString() + "_" + listBox2.Items[models].ToString() + "_r" + listBox3.Items[models].ToString() + "i1p1_200512-209911,";
                }
            }
            futOne = futOne.TrimEnd(',');

            futTwo = "";
            for (int models = 0; models < listBox1.Items.Count; models++)
            {
                if (listBox1.Items[models].Equals("ACCESS1-0") || listBox1.Items[models].Equals("ACCESS1-3") || listBox1.Items[models].Equals("bcc-csm1-1-m") || listBox1.Items[models].Equals("ACCESS1-0") || listBox1.Items[models].Equals("BNU-ESM") || listBox1.Items[models].Equals("CanESM2") || listBox1.Items[models].Equals("CCSM4") || listBox1.Items[models].Equals("CESM1-BGC") || listBox1.Items[models].Equals("CESM1-CAM5") || listBox1.Items[models].Equals("CMCC-CM") || listBox1.Items[models].Equals("CNRM-CM5") || listBox1.Items[models].Equals("CSIRO-Mk3-6-0") || listBox1.Items[models].Equals("EC-EARTH") || listBox1.Items[models].Equals("FGOALS-g2") || listBox1.Items[models].Equals("FGOALS-s2") || listBox1.Items[models].Equals("FIO-ESM") || listBox1.Items[models].Equals("GFDL-CM3") || listBox1.Items[models].Equals("GFDL-ESM2G") || listBox1.Items[models].Equals("GFDL-ESM2M") || listBox1.Items[models].Equals("GISS-E2-H-CC") || listBox1.Items[models].Equals("GISS-E2-R") || (listBox1.Items[models].Equals("HadGEM2-ES") && listBox3.Items[models].Equals("3")) || (listBox1.Items[models].Equals("HadGEM2-ES") && listBox2.Items[models].Equals("26") && listBox3.Items[models].Equals("2")) || (listBox1.Items[models].Equals("HadGEM2-ES") && listBox2.Items[models].Equals("45") && listBox3.Items[models].Equals("2")) || (listBox1.Items[models].Equals("HadGEM2-ES") && listBox2.Items[models].Equals("60") && listBox3.Items[models].Equals("2")) || listBox1.Items[models].Equals("inmcm4") || listBox1.Items[models].Equals("IPSL-CM5A-LR") || listBox1.Items[models].Equals("IPSL-CM5A-MR") || listBox1.Items[models].Equals("IPSL-CM5B-LR") || listBox1.Items[models].Equals("MIROC5") || listBox1.Items[models].Equals("MIROC-ESM") || listBox1.Items[models].Equals("MPI-ESM")) 
                {
                    futTwo = futTwo + "BCSD_0-125deg_pr_Amon_" + listBox1.Items[models].ToString() + "_" + listBox2.Items[models].ToString() + "_r" + listBox3.Items[models].ToString() + "i1p1_200601-210012," + "BCSD_0-125deg_tas_Amon_" + listBox1.Items[models].ToString() + "_" + listBox2.Items[models].ToString() + "_r" + listBox3.Items[models].ToString() + "i1p1_200601-210012,";
                }
            }
            futTwo = futTwo.TrimEnd(',');

            futThree = "";
            for (int models = 0; models < listBox1.Items.Count; models++)
            {
                if (listBox1.Items[models].Equals("bcc-csm1-1") || listBox1.Items[models].Equals("HadGEM2-AO"))
                {
                    futThree = futThree + "BCSD_0-125deg_pr_Amon_" + listBox1.Items[models].ToString() + "_" + listBox2.Items[models].ToString() + "_r" + listBox3.Items[models].ToString() + "i1p1_200601-209912," + "BCSD_0-125deg_tas_Amon_" + listBox1.Items[models].ToString() + "_" + listBox2.Items[models].ToString() + "_r" + listBox3.Items[models].ToString() + "i1p1_200601-209912,";
                }
            }
            futThree = futThree.TrimEnd(',');

            futFour = "";
            for (int models = 0; models < listBox1.Items.Count; models++)
            {
                if (listBox1.Items[models].Equals("HadCM3") || listBox1.Items[models].Equals("MIROC4h"))
                {
                    futFour = futFour + "BCSD_0-125deg_pr_Amon_" + listBox1.Items[models].ToString() + "_" + listBox2.Items[models].ToString() + "_r" + listBox3.Items[models].ToString() + "i1p1_200601-203512," + "BCSD_0-125deg_tas_Amon_" + listBox1.Items[models].ToString() + "_" + listBox2.Items[models].ToString() + "_r" + listBox3.Items[models].ToString() + "i1p1_200601-203512,";
                }
            }
            futFour = futFour.TrimEnd(',');

            futFive = "";
            for (int models = 0; models < listBox1.Items.Count; models++)
            {
                if ((listBox1.Items[models].Equals("HadGEM2-CC") && listBox2.Items[models].Equals("45")) || (listBox1.Items[models].Equals("HadGEM2-ES") && listBox2.Items[models].Equals("85") && listBox3.Items[models].Equals("1")))
                {
                    futFive = futFive + "BCSD_0-125deg_pr_Amon_" + listBox1.Items[models].ToString() + "_" + listBox2.Items[models].ToString() + "_r" + listBox3.Items[models].ToString() + "i1p1_200512-209912," + "BCSD_0-125deg_tas_Amon_" + listBox1.Items[models].ToString() + "_" + listBox2.Items[models].ToString() + "_r" + listBox3.Items[models].ToString() + "i1p1_200512-209912,";
                }
            }
            futFive = futFive.TrimEnd(',');

            futSix = "";
            for (int models = 0; models < listBox1.Items.Count; models++)
            {
                if ((listBox1.Items[models].Equals("HadGEM2-CC") && listBox2.Items[models].Equals("85")) || (listBox1.Items[models].Equals("HadGEM2-ES") && listBox2.Items[models].Equals("26") && listBox3.Items[models].Equals("4")) || (listBox1.Items[models].Equals("HadGEM2-ES") && listBox2.Items[models].Equals("45") && listBox3.Items[models].Equals("4")) || (listBox1.Items[models].Equals("HadGEM2-ES") && listBox2.Items[models].Equals("60") && listBox3.Items[models].Equals("4")) || (listBox1.Items[models].Equals("HadGEM2-ES") && listBox2.Items[models].Equals("85") && listBox3.Items[models].Equals("2")) || (listBox1.Items[models].Equals("HadGEM2-ES") && listBox2.Items[models].Equals("85") && listBox3.Items[models].Equals("4")))
                {
                    futSix = futSix + "BCSD_0-125deg_pr_Amon_" + listBox1.Items[models].ToString() + "_" + listBox2.Items[models].ToString() + "_r" + listBox3.Items[models].ToString() + "i1p1_200512-210012," + "BCSD_0-125deg_tas_Amon_" + listBox1.Items[models].ToString() + "_" + listBox2.Items[models].ToString() + "_r" + listBox3.Items[models].ToString() + "i1p1_200512-210012,";
                }
            }
            futSix = futSix.TrimEnd(',');

            histOne = "";
            for (int models = 0; models < listBox1.Items.Count; models++)
            {
                if (listBox1.Items[models].Equals("ACCESS1-0") || listBox1.Items[models].Equals("ACCESS1-3") || listBox1.Items[models].Equals("bcc-csm1-1") || listBox1.Items[models].Equals("bcc-csm1-1-m") || listBox1.Items[models].Equals("BNU-ESM") || listBox1.Items[models].Equals("CanESM2") || listBox1.Items[models].Equals("CCSM4") || listBox1.Items[models].Equals("CESM1-BGC") || listBox1.Items[models].Equals("CESM1-CAM5") || listBox1.Items[models].Equals("CMCC-CM") || listBox1.Items[models].Equals("CNRM-CM5") || listBox1.Items[models].Equals("CSIRO-Mk3-6-0") || listBox1.Items[models].Equals("EC-EARTH") || listBox1.Items[models].Equals("FGOALS-g2") || listBox1.Items[models].Equals("FGOALS-s2") || listBox1.Items[models].Equals("FIO-ESM") || listBox1.Items[models].Equals("GFDL-CM3") || listBox1.Items[models].Equals("GFDL-ESM2G") || listBox1.Items[models].Equals("GFDL-ESM2M") || listBox1.Items[models].Equals("GISS-E2-H-CC") || listBox1.Items[models].Equals("GISS-E2-R") || listBox1.Items[models].Equals("GISS-E2-R-CC") || listBox1.Items[models].Equals("HadCM3") || listBox1.Items[models].Equals("HadGEM2-AO") || listBox1.Items[models].Equals("ACCESS1-3") || (listBox1.Items[models].Equals("HadGEM2-ES") && (listBox3.Items[models].Equals("2") || listBox3.Items[models].Equals("3"))) || listBox1.Items[models].Equals("inmcm4") || listBox1.Items[models].Equals("IPSL-CM5A-LR") || listBox1.Items[models].Equals("IPSL-CM5A-MR") || listBox1.Items[models].Equals("IPSL-CM5B-LR") || listBox1.Items[models].Equals("MIROC4h") || listBox1.Items[models].Equals("MIROC5") || listBox1.Items[models].Equals("MIROC-ESM") || listBox1.Items[models].Equals("MIROC-ESM-CHEM") || listBox1.Items[models].Equals("MPI-ESM-LR"))
                {
                    histOne = histOne + "BCSD_0-125deg_pr_Amon_" + listBox1.Items[models].ToString() + "_historical_r" + listBox3.Items[models].ToString() + "i1p1_195001-200512," + "BCSD_0-125deg_tas_Amon_" + listBox1.Items[models].ToString() + "_historical_r" + listBox3.Items[models].ToString() + "i1p1_195001-200512,";
                }
            }
            histOne = histOne.TrimEnd(',');

            histTwo = "";
            for (int models = 0; models < listBox1.Items.Count; models++)
            {
                if (listBox1.Items[models].Equals("HadGEM2-CC") || (listBox1.Items[models].Equals("HadGEM2-ES") && (listBox3.Items[models].Equals("1") || listBox3.Items[models].Equals("4"))))
                {
                    histTwo = histTwo + "BCSD_0-125deg_pr_Amon_" + listBox1.Items[models].ToString() + "_historical_r" + listBox3.Items[models].ToString() + "i1p1_195001-200511," + "BCSD_0-125deg_tas_Amon_" + listBox1.Items[models].ToString() + "_historical_r" + listBox3.Items[models].ToString() + "i1p1_195001-200511,";
                }
            }
            histTwo = histTwo.TrimEnd(',');

            this.Hide();

			IProgressHandler pHandler = app.ProgressHandler;
			pHandler.Progress("", 0, "blah");
			prog = new Progress(pHandler);

			//start new thread to handle python code
			prog.start();
            await Task.Factory.StartNew(() => callPythonCode());
			prog.stop();
			createPrecipTempAndProjectionsFiles();

			//reset form
            this.Dispose();
        }

        //cancel button
        private void button4_Click(object sender, EventArgs e)
        {

            this.Dispose();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            
        }

        //Button to select features on map
        private void button1_Click(object sender, EventArgs e)
        {
            DeactivateDrawBox();
            DeactivateCurrentView();

            if (map.FunctionMode != FunctionMode.Select)
                map.FunctionMode = FunctionMode.Select;
            CurrentAreaSelectMode = AreaSelectMode.SelectPolygons;


            

        }

        //browse button to select file path to save to
        private void button3_Click(object sender, EventArgs e)
        {
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                saveToFolderName = fbd.SelectedPath;
                textBox1.Text = saveToFolderName;
            }
        }

        private void download_Load(object sender, EventArgs e)
        {

        }

        //Button to draw region on map
        private void button2_Click(object sender, EventArgs e)
        {
            do_rbDrawBox_Click();
        }

        void do_rbDrawBox_Click()
        {
            CurrentAreaSelectMode = AreaSelectMode.DrawBox;

            DeactivateSelectAreaByPolygon();
            DeactivateCurrentView();

            var layers = map.MapFrame.GetAllLayers();
            map.MapFrame.IsSelected = false;
            foreach (var layer in layers)
            {
                layer.IsSelected = false;
                layer.GetParentItem().IsSelected = false;
            }

            if (_rectangleDrawing == null)
            {
                _rectangleDrawing = new RectangleDrawing((Map)map);
                _rectangleDrawing.RectangleCreated += rectangleDrawing_RectangleCreated;
                _rectangleDrawing.Deactivated += _rectangleDrawing_Deactivated;
            }

            _rectangleDrawing.Activate();
            map.Legend.RefreshNodes();
        }

        private bool _isDeactivatingDrawBox;

        void _rectangleDrawing_Deactivated(object sender, EventArgs e)
        {
            if (_isDeactivatingDrawBox) return;
            rbSelect_Click(this, EventArgs.Empty);
        }

        void rectangleDrawing_RectangleCreated(object sender, EventArgs e)
        {
            if (_rectangleDrawing == null) return;
            double xMin = _rectangleDrawing.RectangleExtent.MinX;
            double xMax = _rectangleDrawing.RectangleExtent.MaxX;
            double yMin = _rectangleDrawing.RectangleExtent.MinY;
            double yMax = _rectangleDrawing.RectangleExtent.MaxY;
            Box bx = new Box(xMin, xMax, yMin, yMax);
            _searchSettings.AreaSettings.SetAreaRectangle(bx, map.Projection);

        }

        private void DeactivateSelectAreaByPolygon()
        {
            _searchSettings.AreaSettings.Polygons = null;
        }

        public void DeactivateCurrentView()
        {
            if (_useCurrentView)
            {
                _useCurrentView = false;
            }
            _searchSettings.AreaSettings.SetAreaRectangle((Box)null, null);
        }

        void rbSelect_Click(object sender, EventArgs e)
        {
            DeactivateDrawBox();
            DeactivateCurrentView();

            if (map.FunctionMode != FunctionMode.Select)
                map.FunctionMode = FunctionMode.Select;
            CurrentAreaSelectMode = AreaSelectMode.SelectPolygons;

            
        }

        public void DeactivateDrawBox()
        {
            if (_rectangleDrawing == null) return;

            _isDeactivatingDrawBox = true;
            _rectangleDrawing.Deactivate();
            _searchSettings.AreaSettings.SetAreaRectangle((Box)null, null);
            _isDeactivatingDrawBox = false;
        }

        //add model to list button
        private void button7_Click(object sender, EventArgs e)
        {
            Boolean alreadyAdded = false;
            selectedModel1 = null;
            selectedModel2 = null;
            selectedModel3 = null;
            selectedModel1 = comboBox1.Text;
            selectedModel2 = comboBox2.Text;
            selectedModel3 = comboBox3.Text;

            if (selectedModel1 != "" && selectedModel2 != "" && selectedModel3 != "")
            {
                for (int i = 0; i < listBox1.Items.Count; i++)
                {
                    if (listBox1.Items[i].Equals(selectedModel1))
                    {
                        if (listBox2.Items[i].Equals(selectedModel2) && listBox3.Items[i].Equals(selectedModel3))
                        {
                            alreadyAdded = true;
                            break;
                        }
                    }
                }
               
                if (!alreadyAdded) {
                    listBox1.Items.Add(selectedModel1);
                    listBox2.Items.Add(selectedModel2);
                    listBox3.Items.Add(selectedModel3);
                }
            }
            else
            {
                MessageBox.Show("Please select a model, pathway, and run");
                return;
            }


        }

        //delete model from list button
        private void button6_Click(object sender, EventArgs e)
        {
            currentlyRemoving = true;
            listBox1.Items.Remove(listBox1.SelectedItem);
            listBox2.Items.Remove(listBox2.SelectedItem);
            listBox3.Items.Remove(listBox3.SelectedItem);
            currentlyRemoving = false;
            listBox1.SelectedIndex = -1;
            listBox2.SelectedIndex = -1;
            listBox3.SelectedIndex = -1;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (currentlyRemoving)
                return;
            int ind = listBox1.SelectedIndex;
            listBox2.SelectedIndex = ind;
            listBox3.SelectedIndex = ind;
        }

        //populates pathway and run number combo boxes
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBox3.Items.Clear();
            comboBox2.Items.Clear();

            comboBox2.ResetText();
            comboBox3.ResetText();



            if (comboBox1.SelectedItem == "ACCESS1-0" || comboBox1.SelectedItem == "ACCESS1-3" || comboBox1.SelectedItem == "bcc-csm1-1-m" || comboBox1.SelectedItem == "CESM1-BGC" || comboBox1.SelectedItem == "CMCC-CM" || comboBox1.SelectedItem == "HadGEM2-CC" || comboBox1.SelectedItem == "inmcm4" || comboBox1.SelectedItem == "ISPL-CM5B-LR")
            {
                comboBox2.Items.Add("rcp45");
                comboBox2.Items.Add("rcp85");
                comboBox3.Items.Add("1");
            }

            if (comboBox1.SelectedItem == "bcc-csm1-1" || comboBox1.SelectedItem == "GFDL-CM3" || comboBox1.SelectedItem == "GFDL-ESM2G" || comboBox1.SelectedItem == "GFDL-ESM2M" || comboBox1.SelectedItem == "HadGEM2-AO" || comboBox1.SelectedItem == "IPSL-CM5A-MR" || comboBox1.SelectedItem == "MIROC-ESM" || comboBox1.SelectedItem == "MIROC-ESM-CHEM" || comboBox1.SelectedItem == "MIROC5")
            {
                comboBox2.Items.Add("rcp26");
                comboBox2.Items.Add("rcp45");
                comboBox2.Items.Add("rcp60");
                comboBox2.Items.Add("rcp85");
                comboBox3.Items.Add("1");
            }

            if (comboBox1.SelectedItem == "CanESM2")
            {
                comboBox2.Items.Add("rcp26");
                comboBox2.Items.Add("rcp45");
                comboBox2.Items.Add("rcp85");
                comboBox3.Items.Add("1");
                comboBox3.Items.Add("2");
                comboBox3.Items.Add("3");
                comboBox3.Items.Add("4");
                comboBox3.Items.Add("5");
            }

            if (comboBox1.SelectedItem == "CCSM4")
            {
                comboBox2.Items.Add("rcp26");
                comboBox2.Items.Add("rcp45");
                comboBox2.Items.Add("rcp60");
                comboBox2.Items.Add("rcp85");
                comboBox3.Items.Add("1");
                comboBox3.Items.Add("2");
                comboBox3.Items.Add("3");
                comboBox3.Items.Add("4");
                comboBox3.Items.Add("5");
            }

            if (comboBox1.SelectedItem == "CESM1-CAM5")
            {
                comboBox2.Items.Add("rcp26");
                comboBox2.Items.Add("rcp45");
                comboBox2.Items.Add("rcp60");
                comboBox2.Items.Add("rcp85");
            }

            if (comboBox1.SelectedItem == "CNRM-CM5")
            {
                comboBox2.Items.Add("rcp45");
                comboBox2.Items.Add("rcp85");
                
            }

            if (comboBox1.SelectedItem == "CSIRO-Mk3-6-0")
            {
                comboBox2.Items.Add("rcp26");
                comboBox2.Items.Add("rcp45");
                comboBox2.Items.Add("rcp60");
                comboBox2.Items.Add("rcp85");
                comboBox3.Items.Add("1");
                comboBox3.Items.Add("2");
                comboBox3.Items.Add("3");
                comboBox3.Items.Add("4");
                comboBox3.Items.Add("5");
                comboBox3.Items.Add("6");
                comboBox3.Items.Add("7");
                comboBox3.Items.Add("8");
                comboBox3.Items.Add("9");
                comboBox3.Items.Add("10");
            }

            if (comboBox1.SelectedItem == "EC-EARTH")
            {
                comboBox2.Items.Add("rcp26");
                comboBox2.Items.Add("rcp45");
                comboBox2.Items.Add("rcp85");
                
            }

            if (comboBox1.SelectedItem == "FGOALS-g2" || comboBox1.SelectedItem == "MPI-ESM-LR" || comboBox1.SelectedItem == "BNU-ESM")
            {
                comboBox2.Items.Add("rcp26");
                comboBox2.Items.Add("rcp45");
                comboBox2.Items.Add("rcp85");
                comboBox3.Items.Add("1");
            }

            if (comboBox1.SelectedItem == "FGOALS-s2")
            {
                comboBox2.Items.Add("rcp60");
                comboBox2.Items.Add("rcp85");
                
            }

            if (comboBox1.SelectedItem == "FIO-ESM")
            {
                comboBox2.Items.Add("rcp26");
                comboBox2.Items.Add("rcp45");
                comboBox2.Items.Add("rcp60");
                comboBox2.Items.Add("rcp85");
                comboBox3.Items.Add("1");
                comboBox3.Items.Add("2");
                comboBox3.Items.Add("3");
            }

            if (comboBox1.SelectedItem == "GISS-E2-H-CC" || comboBox1.SelectedItem == "GISS-E2-R-CC")
            {
                comboBox2.Items.Add("rcp45");
                comboBox3.Items.Add("1");
            }

            if (comboBox1.SelectedItem == "GISS-E2-R")
            {
                comboBox2.Items.Add("rcp26");
                comboBox2.Items.Add("rcp45");
                comboBox2.Items.Add("rcp60");
                comboBox2.Items.Add("rcp85");
                
            }

            if (comboBox1.SelectedItem == "HadGEM2-ES")
            {
                comboBox2.Items.Add("rcp26");
                comboBox2.Items.Add("rcp45");
                comboBox2.Items.Add("rcp60");
                comboBox2.Items.Add("rcp85");
                comboBox3.Items.Add("1");
                comboBox3.Items.Add("2");
                comboBox3.Items.Add("3");
                comboBox3.Items.Add("4");
            }

            if (comboBox1.SelectedItem == "IPSL-CM5A-LR")
            {
                comboBox2.Items.Add("rcp26");
                comboBox2.Items.Add("rcp45");
                comboBox2.Items.Add("rcp60");
                comboBox2.Items.Add("rcp85");
                
            }

            if (comboBox1.SelectedItem == "MPI-ESM-LR")
            {
                comboBox2.Items.Add("rcp26");
                comboBox2.Items.Add("rcp45");
                comboBox2.Items.Add("rcp85");
                
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }


        //stop the red x close button from disposing form
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            this.Hide();

            listBox1.Items.Clear();
            listBox2.Items.Clear();
            listBox3.Items.Clear();
            comboBox1.Text = "";
            comboBox2.Text = "";
            comboBox3.Text = "";
            textBox1.Text = "";
            e.Cancel = true;
        }

        private void comboBox2_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            if (comboBox2.SelectedItem == "access1-0")
            {
                comboBox3.Items.Add("rcp45");

            }
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {

        }


        private void listBox2_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            if (currentlyRemoving)
                return;
            int ind = listBox2.SelectedIndex;
            listBox1.SelectedIndex = ind;
            listBox3.SelectedIndex = ind;
        }

        private void listBox3_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            if (currentlyRemoving)
                return;
            int ind = listBox3.SelectedIndex;
            listBox1.SelectedIndex = ind;
            listBox2.SelectedIndex = ind;
        }

        private void comboBox2_SelectedIndexChanged_2(object sender, EventArgs e)
        {
            comboBox3.ResetText();
            
            if (comboBox1.SelectedItem == "CESM1-CAM5" && (comboBox2.SelectedItem == "rcp26" || comboBox2.SelectedItem == "rcp45" || comboBox2.SelectedItem == "rcp85"))
            {
                comboBox3.Items.Clear();
                comboBox3.ResetText();
                comboBox3.Items.Add("1");
                comboBox3.Items.Add("2");
                comboBox3.Items.Add("3");
            }
            if (comboBox1.SelectedItem == "CESM1-CAM5" && (comboBox2.SelectedItem == "rcp60"))
            {
                comboBox3.Items.Clear();
                comboBox3.ResetText();
                comboBox3.Items.Add("1");
                comboBox3.Items.Add("3");
            }

            if (comboBox1.SelectedItem == "CNRM-CM5" && (comboBox2.SelectedItem == "rcp45"))
            {
                comboBox3.Items.Clear();
                comboBox3.ResetText();
                comboBox3.Items.Add("1");
            }
            if (comboBox1.SelectedItem == "CNRM-CM5" && (comboBox2.SelectedItem == "rcp85"))
            {
                comboBox3.Items.Clear();
                comboBox3.ResetText();
                comboBox3.Items.Add("1");
                comboBox3.Items.Add("2");
                comboBox3.Items.Add("4");
                comboBox3.Items.Add("6");
                comboBox3.Items.Add("10");
            }

            if (comboBox1.SelectedItem == "EC-EARTH" && (comboBox2.SelectedItem == "rcp26"))
            {
                comboBox3.Items.Clear();
                comboBox3.ResetText();
                comboBox3.Items.Add("8");
                comboBox3.Items.Add("12");
            }
            if (comboBox1.SelectedItem == "EC-EARTH" && (comboBox2.SelectedItem == "rcp45"))
            {
                comboBox3.Items.Clear();
                comboBox3.ResetText();
                comboBox3.Items.Add("2");
                comboBox3.Items.Add("8");
                comboBox3.Items.Add("12");
            }
            if (comboBox1.SelectedItem == "EC-EARTH" && (comboBox2.SelectedItem == "rcp85"))
            {
                comboBox3.Items.Clear();
                comboBox3.ResetText();
                comboBox3.Items.Add("6");
                comboBox3.Items.Add("8");
                comboBox3.Items.Add("12");
            }

            if (comboBox1.SelectedItem == "FGOALS-s2" && (comboBox2.SelectedItem == "rcp60"))
            {
                comboBox3.Items.Clear();
                comboBox3.ResetText();
                comboBox3.Items.Add("2");
            }
            if (comboBox1.SelectedItem == "FGOALS-s2" && (comboBox2.SelectedItem == "rcp85"))
            {
                comboBox3.Items.Clear();
                comboBox3.ResetText();
                comboBox3.Items.Add("2");
                comboBox3.Items.Add("3");
            }

            if (comboBox1.SelectedItem == "GISS-E2-R" && (comboBox2.SelectedItem == "rcp26" || comboBox2.SelectedItem == "rcp60" || comboBox2.SelectedItem == "rcp85"))
            {
                comboBox3.Items.Clear();
                comboBox3.ResetText();
                comboBox3.Items.Add("1");
            }
            if (comboBox1.SelectedItem == "GISS-E2-R" && (comboBox2.SelectedItem == "rcp60"))
            {
                comboBox3.Items.Clear();
                comboBox3.ResetText();
                comboBox3.Items.Add("1");
                comboBox3.Items.Add("2");
                comboBox3.Items.Add("3");
                comboBox3.Items.Add("4");
                comboBox3.Items.Add("5");
            }

            if (comboBox1.SelectedItem == "IPSL-CM5A-LR" && (comboBox2.SelectedItem == "rcp26"))
            {
                comboBox3.Items.Clear();
                comboBox3.ResetText();
                comboBox3.Items.Add("1");
                comboBox3.Items.Add("2");
                comboBox3.Items.Add("3");
            }
            if (comboBox1.SelectedItem == "IPSL-CM5A-LR" && (comboBox2.SelectedItem == "rcp45" || comboBox2.SelectedItem == "rcp85"))
            {
                comboBox3.Items.Clear();
                comboBox3.ResetText();
                comboBox3.Items.Add("1");
                comboBox3.Items.Add("2");
                comboBox3.Items.Add("3");
                comboBox3.Items.Add("4");
            }
            if (comboBox1.SelectedItem == "IPSL-CM5A-LR" && (comboBox2.SelectedItem == "rcp60"))
            {
                comboBox3.Items.Clear();
                comboBox3.ResetText();
                comboBox3.Items.Add("1");
            }

            if (comboBox1.SelectedItem == "MPI-ESM-LR" & (comboBox2.SelectedItem == "rcp85"))
            {
                comboBox3.Items.Clear();
                comboBox3.ResetText();
                comboBox3.Items.Add("1");
                comboBox3.Items.Add("2");
                comboBox3.Items.Add("3");
            }
            if (comboBox1.SelectedItem == "MPI-ESM-LR" && (comboBox2.SelectedItem == "rcp26" || comboBox2.SelectedItem == "rcp45"))
            {
                comboBox3.Items.Clear();
                comboBox3.ResetText();
                comboBox3.Items.Add("1");
            }
        }

        private void comboBox3_SelectedIndexChanged_1(object sender, EventArgs e)
        {

        }

		//temporary, for use generating a shape file for each huc 8
		private void button8_Click(object sender, EventArgs e) {
			filePath = "";
			filePath = textBox1.Text;

			if (filePath == "") {
				MessageBox.Show("Please select a file path");
				return;
			}

			//export rectangle as shapefile
			Boolean overwrite = true;
			FeatureSet copy = null;
			FeatureSet fs = (FeatureSet)map.Layers[map.Layers.Count - 1].DataSet;
			List<IFeature> featureList = null;

			//identify selected watershed and export as shapefile
			foreach (IFeatureLayer item2 in map.GetFeatureLayers()) {
				try {
					IFeatureLayer ifea = item2 as IFeatureLayer;
					if (ifea.Selection.Count > 0) {
						ifea.SelectAll();
						featureList = ifea.Selection.ToFeatureList();
					}
				}
				catch {
				}
			}

			if (fs == null && copy == null) {
				MessageBox.Show("Please select a feature on the map or draw a region");
				return;
			}

			for (int i = 0; i < featureList.Count; i++) {
				//save shape file
				IFeature ifea = featureList[i];
				IFeature ifea2 = ifea.Centroid();
				IList<DotSpatial.Topology.Coordinate> centroid = ifea2.Coordinates;
				System.IO.Directory.CreateDirectory(filePath + "\\" + i);
				string newFilePath = filePath + "\\" + i + "\\" + "a" + centroid[0].X + "_" + centroid[0].Y + ".shp";
				fs = new FeatureSet();
				fs.AddFeature(ifea);
				fs.SaveAs(newFilePath, overwrite);

				//modify shape file

				// Compose a string consisting of EPSG WKT for web mercator
				string lines = "PROJCS[\"WGS 84 / Pseudo-Mercator\",\r\n  GEOGCS[\"WGS 84\",\r\n    DATUM[\"World Geodetic System 1984\",\r\n      SPHEROID[\"WGS 84\", 6378137.0, 298.257223563, AUTHORITY[\"EPSG\",\"7030\"]],\r\n      AUTHORITY[\"EPSG\",\"6326\"]],\r\n    PRIMEM[\"Greenwich\", 0.0, AUTHORITY[\"EPSG\",\"8901\"]],\r\n    UNIT[\"degree\", 0.017453292519943295],\r\n    AXIS[\"Geodetic longitude\", EAST],\r\n    AXIS[\"Geodetic latitude\", NORTH],\r\n    AUTHORITY[\"EPSG\",\"4326\"]],\r\n  PROJECTION[\"Popular Visualisation Pseudo Mercator\", AUTHORITY[\"EPSG\",\"1024\"]],\r\n  PARAMETER[\"semi_minor\", 6378137.0],\r\n  PARAMETER[\"latitude_of_origin\", 0.0],\r\n  PARAMETER[\"central_meridian\", 0.0],\r\n  PARAMETER[\"scale_factor\", 1.0],\r\n  PARAMETER[\"false_easting\", 0.0],\r\n  PARAMETER[\"false_northing\", 0.0],\r\n  UNIT[\"m\", 1.0],\r\n  AXIS[\"Easting\", EAST],\r\n  AXIS[\"Northing\", NORTH],\r\n  AUTHORITY[\"EPSG\",\"3857\"]]";

				// Write the string to the .prj file
				newFilePath = newFilePath.Remove(newFilePath.Length - 3) + "prj";
				System.IO.StreamWriter file = new System.IO.StreamWriter(newFilePath);
				file.WriteLine(lines);
				file.Close();

			}

			this.Hide();
		}




    }
}
