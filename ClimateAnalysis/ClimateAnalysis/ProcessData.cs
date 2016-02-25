using Accord.Math;
using Accord.Statistics;
using DotSpatial.Controls;
using HydroDesktop.Configuration;
using HydroDesktop.Database;
using HydroDesktop.ObjectModel;
using HydroDesktop.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using ZedGraph;
using DotSpatial.Data;
using DotSpatial.Topology;
using System.Data;
using DotSpatial.Symbology;

namespace ClimateAnalysis {
	public class ProcessData {
		private double[,] precipData;//all precip data, row * column
		private List<double>[,] precipSubsets;//subsets of precip data, historical first, then one or more future, each list holds the values for a time range and column
		private double[,] tempData;//all temp data
		private List<double>[,] tempSubsets;//subsets of temperature data, historical first, then one or more future, the list holds the values for the range and column
		private double[,] precipChangeFactors;//the percentage change in precipitation from the historical to future periods for each period and model
		private double[,] tempChangeFactors;//the change in temperature from the historical to the future periods for each period and model
		private List<double>[] sortedPrecipChangeFactors;//precipChangeFactors where each period is sorted
		private List<double>[] sortedTempChangeFactors;//tempChangeFactors where each period is sorted
		private PointF[,] points;//points corresponding to each of the future periods and to each of the models, PointF(precip change factor, temp change factor)
		private Ensemble[] ensembles = null;//the ensembles
		private int numberOfColumns;//number of columns in the input files
		private int numberOfRows;//number of rows in the input files
		private List<DateRange> dates = null;//DateRanges containing the past first, then one or more future ranges
		private double[] avgDaysPerYear;//average number of days per year for all periods
		private bool summerOnly = false;//if true, only consider may - sep
		private int futurePeriods;//the number of future time periods being used
		private String[] modelNames = null;//The names of the GCMs;
		private List<double[,]> covarianceMatrices;//the covariance matrix of each future period to use when finding distances
		private Dates datesForm;
		private MapLineLayer lineLayer = null;
		private readonly Map mainMap;

		#region Public Methods

		/// <summary>
		/// Creates a new ProcessData object.
		/// </summary>
		public ProcessData(Dates dates, Map map) {
			datesForm = dates;
			mainMap = map;
		}
		
		/// <summary>
		/// Read in the .csv data from the precipitation and temperature files
		/// </summary>
		/// <param name="precipFileName">The name of the precipitation data file</param>
		/// <param name="tempFileName">The name of the temperature data file</param>
		public void Import(string precipFileName, string tempFileName) {
			string[] precipLines = System.IO.File.ReadAllLines(precipFileName);
			string[] tempLines = System.IO.File.ReadAllLines(tempFileName);
			string[] precipLine = precipLines[0].Split(',');
			string[] tempLine = tempLines[0].Split(',');

			//set the number of columns and rows and check that the input files match.
			numberOfColumns = precipLine.Length;
			if (numberOfColumns != tempLine.Length) {
				throw new Exception("The number of columns in the input files does not match");
			}
			numberOfRows = precipLines.Length;
			if (numberOfRows != tempLines.Length) {
				throw new Exception("The number of rows in the input files does not match");
			}

			//initialize arrays
			precipData = new double[precipLines.Length, precipLine.Length];
			tempData = new double[tempLines.Length, tempLine.Length];

			//fill precipitation array
			for (int row = 0; row < precipLines.Length; row++) {
				precipLine = precipLines[row].Split(',');
				for (int col = 0; col < precipLine.Length; col++) {
					try {
						precipData[row, col] = double.Parse(precipLine[col]);
					}
					catch (Exception) {
						precipData[row, col] = precipData[row - 1, col];
					}
				}
			}

			//fill temperature array
			for (int row = 0; row < tempLines.Length; row++) {
				tempLine = tempLines[row].Split(',');
				for (int col = 0; col < tempLine.Length; col++) {
					try {
						tempData[row, col] = double.Parse(tempLine[col]);
					}
					catch (Exception) {
						tempData[row, col] = tempData[row - 1, col];
					}
				}
			}
		}


		public void addData(double[,] precip, double[,] temp, string[] names) {
			double[,] newPrecip;
			double[,] newTemp;
			int col = 0, inputCol = 0;
			if (numberOfColumns == 0) {
				newPrecip = new double[precip.GetLength(0), precip.GetLength(1) + 2];
				newTemp = new double[temp.GetLength(0), temp.GetLength(1) + 2];

				//set year and month in first 2 columns
				int year = 1950;
				int month = 0;
				for (int row = 0; row < precip.GetLength(0); row ++) {
					month++;
					if (month == 13) {
						month = 1;
						year++;
					}
					newPrecip[row, 0] = year;
					newPrecip[row, 1] = month;
					newTemp[row, 0] = year;
					newTemp[row, 1] = month;
				}

				col = 2;
			}
			else {
				newPrecip = new double[Math.Max(precipData.GetLength(0), precip.GetLength(0)), precipData.GetLength(1) + precip.GetLength(1)];
				newTemp = new double[Math.Max(tempData.GetLength(0), temp.GetLength(0)), tempData.GetLength(1) + temp.GetLength(1)];
				col = precipData.GetLength(1);
			}

			for ( ; col < newPrecip.GetLength(1); col++) {
				for (int row = 0; row < newPrecip.GetLength(0); row++) {
					if (col < precipData.GetLength(1)) {
						if (row < precipData.GetLength(0)) {
							newPrecip[row, col] = precipData[row, col];
							newTemp[row, col] = tempData[row, col];
						} else {
							newPrecip[row, col] = precipData[precipData.GetLength(0) - 1, col];
							newTemp[row, col] = tempData[tempData.GetLength(0) - 1, col];
						}
					}
					else {
						if (row < precip.GetLength(0)) {
							newPrecip[row, col] = precip[row, inputCol];
							newTemp[row, col] = temp[row, inputCol];
						} else {
							newPrecip[row, col] = precip[precip.GetLength(0) - 1, inputCol];
							newTemp[row, col] = temp[temp.GetLength(0) - 1, inputCol];
						}
					}
				}
				inputCol++;
			}

			precipData = newPrecip;
			tempData = newTemp;
			numberOfColumns = precipData.GetLength(1);
			numberOfRows = precipData.GetLength(0);

			//add names to modelNames
			string[] newNames = new string[modelNames.Length + names.Length];
			for (int i = 0; i < newNames.Length; i++) {
				if (i < modelNames.Length) {
					newNames[i] = modelNames[i];
				}
				else {
					newNames[i] = names[i - modelNames.Length];
				}
			}
			modelNames = newNames;
		}

		/// <summary>
		/// Clears all data that has been imported
		/// </summary>
		public void clearData() {
			precipData = new double[0, 0];
			tempData = new double[0, 0];
			numberOfColumns = 0;
			numberOfRows = 0;
			modelNames = new String[0];
		}

		/// <summary>
		/// Reads the names of the GCMs into memory.
		/// </summary>
		/// <param name="fileName">The name of the file containing the GCM names.</param>
		public void importNames(string fileName) {
			string[] names = System.IO.File.ReadAllLines(fileName);
			int index = 0;

			//check if there are the right number of names in the file
			if (numberOfColumns - 2 != names.Length)
				throw new Exception("The number of names in the GCM names file doesn't match the number of GCMs in the data.");
			
			//initialize and fill modelNames
			modelNames = new String[names.Length];
			foreach (String str in names) {
				modelNames[index++] = str;
			}
		}

		/// <summary>
		/// Adds the precip, temp, and names data to the hydrodesktop database
		/// </summary>
		public void addDataToDatabase(ImportFromFile import, List<double> longList, List<double> latList) {
			string[] gcmNames = getModelNames();
			int IdNum = 0;
			List<HydroDesktop.Interfaces.ObjectModel.Series> series = new List<HydroDesktop.Interfaces.ObjectModel.Series>();

			double lon = 0, lat = 0;

			if (longList != null) {
				lon = longList.Average();
				lat = latList.Average();
			}

			//create precip and temp series for each gcm
			for (int col = 2; col < numberOfColumns; col++) {
				//precip
				HydroDesktop.Interfaces.ObjectModel.Series precip = new HydroDesktop.Interfaces.ObjectModel.Series();
				precip.Id = IdNum++;
				precip.Site = new HydroDesktop.Interfaces.ObjectModel.Site();
				precip.Site.Name = gcmNames[col - 2];
				precip.Site.Code = gcmNames[col - 2];
				if (longList != null) {
					precip.Site.Longitude = lon;
					precip.Site.Latitude = lat;
					precip.Site.SpatialReference = new HydroDesktop.Interfaces.ObjectModel.SpatialReference("WGS 84");
				}
				precip.Variable = new HydroDesktop.Interfaces.ObjectModel.Variable();
				precip.Variable.Name = "Precipitation";
				precip.Variable.Code = "305";
				precip.Variable.Speciation = "Unknown";
				precip.Variable.SampleMedium = "Unknown";
				precip.Variable.ValueType = "Unknown";
				precip.Variable.DataType = "Unknown";
				precip.Variable.GeneralCategory = "Amount";
				precip.Variable.VariableUnit = new HydroDesktop.Interfaces.ObjectModel.Unit();
				precip.Variable.VariableUnit.UnitsType = "Amount";
				precip.Variable.VariableUnit.Name = "mm/day";
				precip.Variable.VariableUnit.Abbreviation = "mm/day";
				precip.Variable.TimeUnit = new HydroDesktop.Interfaces.ObjectModel.Unit();
				precip.Variable.TimeUnit.Name = "Month";
				precip.Variable.TimeUnit.UnitsType = "Duration";
				precip.Variable.TimeUnit.Abbreviation = "Month";
				precip.Method = new HydroDesktop.Interfaces.ObjectModel.Method();
				precip.Method.Description = "Unknown";
				precip.QualityControlLevel = new HydroDesktop.Interfaces.ObjectModel.QualityControlLevel();
				precip.QualityControlLevel.Code = "Unknown";
				precip.QualityControlLevel.Definition = "Unknown";
				precip.QualityControlLevel.Explanation = "Unknown";
				precip.Source = new HydroDesktop.Interfaces.ObjectModel.Source();

				//temp
				HydroDesktop.Interfaces.ObjectModel.Series temp = new HydroDesktop.Interfaces.ObjectModel.Series();
				temp.Id = IdNum++;
				temp.Site = new HydroDesktop.Interfaces.ObjectModel.Site();
				temp.Site.Name = gcmNames[col - 2];
				temp.Site.Code = gcmNames[col - 2];
				if (longList != null) {
					temp.Site.Longitude = lon;
					temp.Site.Latitude = lat;
					precip.Site.SpatialReference = new HydroDesktop.Interfaces.ObjectModel.SpatialReference("WGS 84");
				}
				temp.Variable = new HydroDesktop.Interfaces.ObjectModel.Variable();
				temp.Variable.Name = "Temperature";
				temp.Variable.Code = "96";
				temp.Variable.Speciation = "Unknown";
				temp.Variable.SampleMedium = "Unknown";
				temp.Variable.ValueType = "Unknown";
				temp.Variable.DataType = "degC";
				temp.Variable.GeneralCategory = "Temperature";
				temp.Variable.VariableUnit = new HydroDesktop.Interfaces.ObjectModel.Unit();
				temp.Variable.VariableUnit.UnitsType = "Temperature";
				temp.Variable.VariableUnit.Name = "Degrees Celsius";
				temp.Variable.VariableUnit.Abbreviation = "degC";
				temp.Variable.TimeUnit = new HydroDesktop.Interfaces.ObjectModel.Unit();
				temp.Variable.TimeUnit.Name = "Month";
				temp.Variable.TimeUnit.UnitsType = "Duration";
				temp.Variable.TimeUnit.Abbreviation = "Month";
				temp.Method = new HydroDesktop.Interfaces.ObjectModel.Method();
				temp.Method.Description = "Unknown";
				temp.QualityControlLevel = new HydroDesktop.Interfaces.ObjectModel.QualityControlLevel();
				temp.QualityControlLevel.Code = "Unknown";
				temp.QualityControlLevel.Definition = "Unknown";
				temp.QualityControlLevel.Explanation = "Unknown";
				temp.Source = new HydroDesktop.Interfaces.ObjectModel.Source();

				//add values to series
				for (int row = 0; row < numberOfRows; row++) {
					precip.AddDataValue(new DateTime((int)precipData[row, 0], (int)precipData[row, 1], 1), precipData[row, col]);
					temp.AddDataValue(new DateTime((int)tempData[row, 0], (int)tempData[row, 1], 1), tempData[row, col]);
				}

				//ISeriesSelector selector = new SeriesSelector();
				precip.UpdateSeriesInfoFromDataValues();
				temp.UpdateSeriesInfoFromDataValues();
				series.Add(precip);
				series.Add(temp);
			}

			HydroDesktop.Interfaces.ObjectModel.Theme theme = new HydroDesktop.Interfaces.ObjectModel.Theme();
			theme.Id = 0;
			theme.Name = "CMIP";

			//save series to database
			string connString = Settings.Instance.DataRepositoryConnectionString;

			///////////////////////////////////////////////
			//use custom database manager
			DbManager dbm = new DbManager(connString);
			dbm.SaveSeriesAsCopy(series, theme, import);

			// for use when updated RepositoryManagerSQL is available, allowing a list of series to be passed in
			//IRepositoryManager rm = connString != null ? RepositoryFactory.Instance.Get<IRepositoryManager>(DatabaseTypes.SQLite, connString)
					//: RepositoryFactory.Instance.Get<IRepositoryManager>();
			//rm.SaveSeries(series, theme, OverwriteOptions.Append);
			///////////////////////////////////////////////

			mainMap.Refresh();
			//figure out how to update series selector
			
		}

		//add linelayer to map if latitude and longitude data are available
		public void addLineLayer(List<double> longList, List<double> latList) {
			double halfOfGridSize = 0;
			FeatureSet rectangleFs = null;

			if (longList.Count == 0 || latList.Count == 0)
				return;

			if (lineLayer != null)
				mainMap.Layers.Remove(lineLayer);

			//create featureSet
			rectangleFs = new FeatureSet(FeatureType.Line);
			rectangleFs.Projection = DotSpatial.Projections.KnownCoordinateSystems.Geographic.World.WGS1984;

			//find the size in degrees of half of a grid cell
			if (longList.Count > 1) {
				halfOfGridSize = Math.Abs(longList[0] - longList[1]) / 2;
			}
			else if (latList.Count > 1) {
				halfOfGridSize = Math.Abs(latList[0] - latList[1]) / 2;
			}
			else {
				halfOfGridSize = 0.0625;//assume 1/8th degree grid
			}

			//find coordinates of the lines
			List<double> longPoints = new List<double>(), latPoints = new List<double>();
			for (int i = 0; i <= longList.Count; i++) {
				if (i == longList.Count)
					longPoints.Add(longList[i - 1] + halfOfGridSize);
				else
					longPoints.Add(longList[i] - halfOfGridSize);
			}
			for (int i = 0; i <= latList.Count; i++) {
				if (i == latList.Count)
					latPoints.Add(latList[i - 1] + halfOfGridSize);
				else
					latPoints.Add(latList[i] - halfOfGridSize);
			}

			//find bounds
			double top = latPoints[latPoints.Count - 1];
			double bottom = latPoints[0];
			double left = longPoints[0];
			double right = longPoints[longPoints.Count - 1];

			//add vertical lines to featureSet
			for (int i = 0; i < longPoints.Count; i++) {
				List<Coordinate> line = new List<Coordinate>();
				Coordinate a = new Coordinate(longPoints[i], top);
				line.Add(a);
				Coordinate b = new Coordinate(longPoints[i], bottom);
				line.Add(b);
				LineString lineString = new LineString(line);
				rectangleFs.AddFeature(lineString);
			}

			//add horizontal lines to featureSet
			for (int i = 0; i < latPoints.Count; i++) {
				List<Coordinate> line = new List<Coordinate>();
				Coordinate a = new Coordinate(left, latPoints[i]);
				line.Add(a);
				Coordinate b = new Coordinate(right, latPoints[i]);
				line.Add(b);
				LineString lineString = new LineString(line);
				rectangleFs.AddFeature(lineString);
			}

			//reproject featureset to map's projection
			DotSpatial.Projections.ProjectionInfo dest = mainMap.Projection;
			rectangleFs.Reproject(dest);
			lineLayer = new MapLineLayer(rectangleFs) { LegendText = "Global Circulation Model Grids" };
			lineLayer.Symbolizer = new LineSymbolizer(Color.Red, 3);
			lineLayer.SelectionSymbolizer = lineLayer.Symbolizer;
			mainMap.Layers.Add(lineLayer);

			//zoom in on lineLayer
			IExtent ext = lineLayer.Extent;
			double Y = (ext.MaxY - ext.MinY) * .1;
			ext.MinY = ext.MinY - Y;
			ext.MaxY = ext.MaxY + Y;
			double X = (ext.MaxX - ext.MinX) * .1;
			ext.MinX = ext.MinX - X;
			ext.MaxX = ext.MaxX + X;
			mainMap.ViewExtents = (Extent)ext;
			mainMap.Refresh();
		}

		/// <summary>
		/// Compares the average temperature and precipitation for the historical and future periods for each GCM.  Reports the change factor as
		/// absolute change for temperature and percent change for precipitation.
		/// </summary>
		/// <param name="dates">An array containing the start and end dates of the future and historical periods in the order: 
		/// histStartMonth, histStartYear, histEndMonth, histEndYear, futStartMonth, futStartYear, futEndMonth, futEndYear</param>
		/// <param name="summerOnly">A boolean instructing the code whether to consider only May through Sep when calculating change factors</param>
		public void generateChangeFactors() {
			double historicAvg, futureAvg;

			if (precipData == null)
				return;
			
			this.summerOnly = datesForm.isSummerOnly();

			//save dates
			this.dates = datesForm.getDates();
			futurePeriods = dates.Count - 1;
			precipChangeFactors = new double[futurePeriods, numberOfColumns - 2];
			tempChangeFactors = new double[futurePeriods, numberOfColumns - 2];

			//get historical and future subsets from the precipData and tempData tables
			getSubsets();
			
			//Get the average days per year for each period to use when calculating precip change factors
			getAvgDaysPerYear();

			//calculate the change factors
			for (int dateRange = 1; dateRange < dates.Count; dateRange++) {
				//calculate precipitation change factors
				for (int col = 2; col < numberOfColumns; col++) {
					historicAvg = precipSubsets[0, col].Average() * avgDaysPerYear[0];
					futureAvg = precipSubsets[dateRange, col].Average() * avgDaysPerYear[dateRange];
					precipChangeFactors[dateRange - 1, col - 2] = (futureAvg / historicAvg) * 100 - 100;
				}

				//calculate temperature change factors
				for (int col = 2; col < numberOfColumns; col++) {
					historicAvg = tempSubsets[0, col].Average();
					futureAvg = tempSubsets[dateRange, col].Average();
					tempChangeFactors[dateRange - 1, col - 2] = futureAvg - historicAvg;
				}
			}

			//create sorted lists of change factors for use in finding percentiles
			sortedPrecipChangeFactors = new List<double>[precipChangeFactors.GetLength(0)];
			sortedTempChangeFactors = new List<double>[precipChangeFactors.GetLength(0)];
			for (int range = 0; range < precipChangeFactors.GetLength(0); range++) {
				sortedPrecipChangeFactors[range] = new List<double>();
				sortedTempChangeFactors[range] = new List<double>();

				//add data to lists
				for (int i = 0; i < precipChangeFactors.GetLength(1); i++) {
					sortedPrecipChangeFactors[range].Add(precipChangeFactors[range, i]);
					sortedTempChangeFactors[range].Add(tempChangeFactors[range, i]);
				}

				//sort lists
				sortedPrecipChangeFactors[range].Sort();
				sortedTempChangeFactors[range].Sort();
			}
		}

		/// <summary>
		/// Finds the missing information in the ensembles
		/// </summary>
		/// <param name="ens">The ensembles</param>
		public void findEnsembles(Ensemble[] ens) {
			if (precipData == null)
				return;

			dates = datesForm.getDates();
			futurePeriods = dates.Count - 1;
			ensembles = ens;
			//create array of PointF from model change factors
			points = new PointF[futurePeriods, numberOfColumns - 2];
			for (int dateRange = 0; dateRange < futurePeriods; dateRange++)
				for (int col = 0; col < numberOfColumns - 2; col++)
					points[dateRange, col] = new PointF((float) precipChangeFactors[dateRange, col], (float) tempChangeFactors[dateRange, col]);

			//get the covariance matrix for each period to use when finding mahalonobis distances
			getCovarianceMatrices();

			//go through each ensemble and find the missing information
			for (int ensemble = 0; ensemble < ensembles.Length; ensemble++) {
				fillEnsemble(ensembles[ensemble]);
			}
		}

		/// <summary>
		/// Generates the deltas for each ensemble
		/// </summary>
		public void generateDeltas() {
			dates = datesForm.getDates();
			futurePeriods = dates.Count - 1;
			collectMonthlyClusterData();
			calculateDeltaEnsemble();
			calculateHybridDeltaEnsemble();
		}

		#endregion Public Methods

		#region Accessors

		public PointPairList[] getPoints() {
			if (futurePeriods != points.GetLength(0))
				findEnsembles(ensembles);

			PointPairList[] result = new PointPairList[futurePeriods];
			for (int period = 0; period < futurePeriods; period++) {
				PointPairList list = new PointPairList();
				for (int col = 0; col < numberOfColumns - 2; col++) {
					PointF p = points[period, col];
					list.Add(p.X, p.Y);
				}
				result[period] = list;
			}
			return result;
		}

		public List<DateRange> getDates() {
			return dates;
		}

		public String[] getModelNames() {
			if (modelNames == null) {
				if (numberOfColumns == 0)
					return new String[1];
				String[] names = new String[numberOfColumns - 2];
				for (int i = 0; i < numberOfColumns - 2; i++)
					names[i] = (i + 1).ToString();
				return names;
			}
			return modelNames;
		}

		public Ensemble[] getEnsembles() {
			return ensembles;
		}
		
		#endregion Accessors

		#region Private Methods

		//calculates the hybrid ensemble data, i.e. the change factors for the quantiles .02 - .98
		private void calculateHybridDeltaEnsemble() {
			
			for (int i = 0; i < ensembles.Length; i++) {
				Ensemble ensemble = ensembles[i];
				double[, , ,] hybridDeltaEnsemble = new double[12, 49, 2, futurePeriods];//12 months * 49 quantiles .02 - .98 * precip, temp * future periods
				double[, , , ,] hybridEnsemble = new double[12, 49, 2, 2, futurePeriods];//12 months * 49 quantiles .02 - .98 * precip, temp * historical, future * future periods
				List<double>[, , , ,] monthlyClusterData = ensemble.monthlyClusterData;//12 months * numModels * precip, temp * historical, future * future periods
				double futurePrecipAvg, histPrecipAvg = 0, futureTempAvg, histTempAvg = 0;

				for (int month = 0; month < 12; month++) {
					for (int quantile = 0; quantile < 49; quantile++) {
						double percentile = (double)quantile * .02 + .02;
						for (int period = 0; period < futurePeriods; period++) {
							//historical
							histPrecipAvg = histTempAvg = 0;
							for (int model = 0; model < ensemble.numberOfModels; model++) {
								histPrecipAvg += Percentile(monthlyClusterData[month, model, 0, 0, period], percentile);
								histTempAvg += Percentile(monthlyClusterData[month, model, 1, 0, period], percentile);
							}
							histPrecipAvg /= ensemble.numberOfModels;
							histTempAvg /= ensemble.numberOfModels;
							hybridEnsemble[month, quantile, 0, 0, period] = histPrecipAvg;
							hybridEnsemble[month, quantile, 1, 0, period] = histTempAvg;
							
							//future
							futurePrecipAvg = futureTempAvg = 0;
							for (int model = 0; model < ensemble.numberOfModels; model++) {
								futurePrecipAvg += Percentile(monthlyClusterData[month, model, 0, 1, period], percentile);
								futureTempAvg += Percentile(monthlyClusterData[month, model, 1, 1, period], percentile);
							}
							futurePrecipAvg /= ensemble.numberOfModels;
							futureTempAvg /= ensemble.numberOfModels;
							hybridEnsemble[month, quantile, 0, 1, period] = futurePrecipAvg;
							hybridEnsemble[month, quantile, 1, 1, period] = futureTempAvg;
							//precip
							if (histPrecipAvg == 0)
								hybridDeltaEnsemble[month, quantile, 0, period] = 0;
							else
								hybridDeltaEnsemble[month, quantile, 0, period] = futurePrecipAvg / histPrecipAvg;
							//temp
							hybridDeltaEnsemble[month, quantile, 1, period] = futureTempAvg - histTempAvg;
							
						}
					}
				}
				ensemble.hybridEnsemble = hybridEnsemble;
				ensemble.hybridDeltaEnsemble = hybridDeltaEnsemble;
			}
		}

		//collects the data from the clusters and organizes it.
		private void collectMonthlyClusterData() {
			//go through each ensemble/cluster and fill the monthlyClusterData variable
			for (int i = 0; i < ensembles.Length; i++) {
				Ensemble ensemble = ensembles[i];
				List<double>[,,,,] monthlyClusterData = new List<double>[12, ensemble.numberOfModels, 2, 2, futurePeriods];
				int column, rowMonth;

				//monthlyClusterData = 12 months * numModels * precip, temp * historical, future * future periods
				//initialize monthlyClusterData array
				
				for (int month = 0; month < 12; month++) {
					for (int model = 0; model < ensemble.numberOfModels; model++) {
						for (int period = 0; period < futurePeriods; period++) {
							monthlyClusterData[month, model, 0, 0, period] = new List<double>();
							monthlyClusterData[month, model, 1, 0, period] = new List<double>();
							monthlyClusterData[month, model, 0, 1, period] = new List<double>();
							monthlyClusterData[month, model, 1, 1, period] = new List<double>();
						}
					}
				}

				//goes through rows in the precip and temp subsets and adds values to lists
				for (int period = 0; period < futurePeriods; period++) {
					//historical
					for (int row = 0; row < precipSubsets[0, 0].Count; row++) {
						rowMonth = (int)precipSubsets[0, 1][row];
						for (int model = 0; model < ensemble.numberOfModels; model++) {
							if (ensemble.statistical)
								column = ensemble.columnNumbers[period, model];
							else//there is only one set of column numbers for a non-statistical ensemble
								column = ensemble.columnNumbers[0, model];
							monthlyClusterData[rowMonth - 1, model, 0, 0, period].Add(precipSubsets[0, column][row]);
							monthlyClusterData[rowMonth - 1, model, 1, 0, period].Add(tempSubsets[0, column][row]);
						}
					}
					//future
					for (int row = 0; row < precipSubsets[period + 1, 0].Count; row++) {
						rowMonth = (int)precipSubsets[period + 1, 1][row];
						for (int model = 0; model < ensemble.numberOfModels; model++) {
							if (ensemble.statistical)
								column = ensemble.columnNumbers[period, model];
							else//there is only one set of column numbers for a non-statistical ensemble
								column = ensemble.columnNumbers[0, model];
							monthlyClusterData[rowMonth - 1, model, 0, 1, period].Add(precipSubsets[period + 1, column][row]);
							monthlyClusterData[rowMonth - 1, model, 1, 1, period].Add(tempSubsets[period + 1, column][row]);
						}
					}
				}

				//sort lists
				for (int month = 0; month < 12; month++) {
					for (int model = 0; model < ensemble.numberOfModels; model++) {
						for (int period = 0; period < futurePeriods; period++) {
							monthlyClusterData[month, model, 0, 0, period].Sort();
							monthlyClusterData[month, model, 1, 0, period].Sort();
							monthlyClusterData[month, model, 0, 1, period].Sort();
							monthlyClusterData[month, model, 1, 1, period].Sort();
						}
					}
				}

				ensemble.monthlyClusterData = monthlyClusterData;
			}
		}

		//calculates the delta ensemble file which contains the average change factors for each month and ensemble
		private void calculateDeltaEnsemble() {

			for (int i = 0; i < ensembles.Length; i++) {
				Ensemble ensemble = ensembles[i];
				double[,,] deltaEnsemble = new double[12, 2, futurePeriods];//12 months * precip, temp * future periods
				List<double>[, , , ,] monthlyClusterData = ensemble.monthlyClusterData;//12 months * numModels * precip, temp * historical, future * future periods
				double pastPrecipAverage = 0;
				double pastTempAverage = 0;

				//conpare each future period to the past
				for (int period = 0; period < futurePeriods; period++) {
					//add averages to delta
					for (int month = 0; month < 12; month++) {
						double[] pastPrecip = new double[ensemble.numberOfModels];
						double[] pastTemp = new double[ensemble.numberOfModels];
						double[] futurePrecip = new double[ensemble.numberOfModels];
						double[] futureTemp = new double[ensemble.numberOfModels];

						//get the median for each past and future column
						for (int model = 0; model < ensemble.numberOfModels; model++) {
							pastPrecip[model] = getMedian(monthlyClusterData[month, model, 0, 0, period]);
							pastTemp[model] = getMedian(monthlyClusterData[month, model, 1, 0, period]);
							futurePrecip[model] = getMedian(monthlyClusterData[month, model, 0, 1, period]);
							futureTemp[model] = getMedian(monthlyClusterData[month, model, 1, 1, period]);
						}
						
						pastPrecipAverage = pastPrecip.Average();
						pastTempAverage = pastTemp.Average();
						
						if (pastPrecipAverage == 0)
							deltaEnsemble[month, 0, period] = 0;
						else
							deltaEnsemble[month, 0, period] = futurePrecip.Average() / pastPrecipAverage;

						if (pastTempAverage == 0)
							deltaEnsemble[month, 1, period] = 0;
						else
							deltaEnsemble[month, 1, period] = futureTemp.Average() - pastTempAverage;
					}
				}
				ensemble.deltaEnsemble = deltaEnsemble;
			}
		}

		//gets median of a list of numbers
		private double getMedian(List<double> sourceNumbers) {     
			if (sourceNumbers == null || sourceNumbers.Count == 0)
				return 0d;

			//get the median
			int size = sourceNumbers.Count;
			int mid = size / 2;
			double median = (size % 2 != 0) ? (double)sourceNumbers[mid] : ((double)sourceNumbers[mid] + (double)sourceNumbers[mid - 1]) / 2;
			return median;
		}

		//gets the sections of the precip and temp data belonging to the historical and future ranges
		private void getSubsets() {
			precipSubsets = new List<double>[dates.Count, numberOfColumns];//subsets of precip data, historical first, then one or more future, the list holds the values for the range and column
			tempSubsets = new List<double>[dates.Count, numberOfColumns];//subsets of temperature data, historical first, then one or more future, the list holds the values for the range and column

			//initialize arrays
			for (int x = 0; x < dates.Count; x++) {
				for (int y = 0; y < numberOfColumns; y++) {
					precipSubsets[x, y] = new List<double>();
					tempSubsets[x, y] = new List<double>();
				}
			}

			//get the subset of data for each date range
			for (int dateRange = 0; dateRange < dates.Count; dateRange++) {
				//find the start and end rows of the date range
				DateRange range = dates[dateRange];
				int startRow = getRow(range.startMonth, range.startYear, 0, numberOfRows);
				int endRow = getRow(range.endMonth, range.endYear, 0, numberOfRows);

				for (int row = startRow; row <= endRow; row++) {
					if (summerOnly && (precipData[row, 1] < 5 || precipData[row, 1] > 9))
						continue;
					for (int col = 0; col < numberOfColumns; col++) {
						double d = precipData[row, col];
						if (Double.IsNaN(d))
							precipSubsets[dateRange, col].Add(0);
						else 
							precipSubsets[dateRange, col].Add(d);
						d = tempData[row, col];
						if (Double.IsNaN(d))
							tempSubsets[dateRange, col].Add(0);
						else
							tempSubsets[dateRange, col].Add(d);
					}
				}
			}
		}

		//recursive binary search to find a row given the month and year
		private int getRow(int month, int year, int beginning, int end) {
			int middle = (end - beginning) / 2 + beginning;
			if (beginning == middle)//this prevents stack overflow if date doesn't exist in precipData
				return middle;
			if (year == precipData[middle, 0] && month == precipData[middle, 1])
				return middle;
			if (year < precipData[middle, 0] || (year == precipData[middle, 0] && month < precipData[middle, 1]))
				return getRow(month, year, beginning, middle);//search first half
			return getRow(month, year, middle, end);//search second half
		}

		//calculate average days per year for historic and future periods
		private void getAvgDaysPerYear() {
			avgDaysPerYear = new double[dates.Count];
			//if summerOnly, average days per year is the number of days May - Sep inclusive
			if (summerOnly) {
				int daysInSummer = 31 + 30 + 31 + 31 + 30;
				for (int i = 0; i < dates.Count; i++) {
					avgDaysPerYear[i] = daysInSummer;
				}
				return;
			}
			//calculate average days per year for each period
			for (int dateRange = 0; dateRange < dates.Count; dateRange++) {
				DateRange range = dates[dateRange];
				DateTime start = new DateTime(range.startYear, range.startMonth, 1);
				int endMonth = range.endMonth;
				int endYear = range.endYear;
				if (endMonth == 12) {
					endMonth = 1;
					endYear++;
				}
				else
					endMonth++;
				DateTime end = new DateTime(endYear, endMonth, 1);
				TimeSpan span = end - start;
				double years = endYear + endMonth / 12 - start.Year + start.Month / 12;
				avgDaysPerYear[dateRange] = span.Days / years;
			}
		}

		//gets the covariance matrix of each period to use when finding the mahalonobis distance
		private void getCovarianceMatrices() {
			covarianceMatrices = new List<double[,]>();

			for (int period = 0; period < futurePeriods; period++) {
				
				//find covariance matrix
				double[,] matrix = new double[numberOfColumns - 2, 2];
				for (int col = 0; col < numberOfColumns - 2; col++) {
					matrix[col, 0] = precipChangeFactors[period, col];
					matrix[col, 1] = tempChangeFactors[period, col];
				}
				double[,] covariance = matrix.Covariance();

				//invert matrix
				covariance = covariance.Inverse();

				//add matrix to covarianceMatrices
				covarianceMatrices.Add(covariance);
			}
		}

		//finds the numPerEnsemble points closest to the point x,y using mahalonobis distance
		private void fillEnsemble(Ensemble ensemble) {
			List<KeyValuePair<double, KeyValuePair<int, PointF>>> list;
			double distance;
			PointF p, center;

			//check to see if number of models is set too high
			if (ensemble.numberOfModels > numberOfColumns - 2)
				ensemble.numberOfModels = numberOfColumns - 2;

			//initialize ensemble arrays
			ensemble.points = new PointF[futurePeriods, ensemble.numberOfModels];
			ensemble.center = new PointF[futurePeriods];
			if (ensemble.statistical)
				ensemble.columnNumbers = new int[futurePeriods, ensemble.numberOfModels];

			//fill ensemble for each future date range
			for (int range = 0; range < futurePeriods; range++) {
				if (ensemble.statistical) {
					list = new List<KeyValuePair<double, KeyValuePair<int, PointF>>>();
					//find center
					center = findPercentiles(ensemble, range);
					ensemble.center[range] = center;
					double[] centerPoint = new double[2];
					centerPoint[0] = center.X;
					centerPoint[1] = center.Y;

					//fill list with a keyvaluepair representing the distance, the column index from the original data, and the PointF
					for (int i = 0; i < points.GetLength(1); i++) {
						p = points[range, i];
						double[] currentPoint = new double[2];
						currentPoint[0] = p.X;
						currentPoint[1] = p.Y;
						distance = centerPoint.Mahalanobis(currentPoint, covarianceMatrices[range]);
						list.Add(new KeyValuePair<double, KeyValuePair<int, PointF>>(distance, new KeyValuePair<int, PointF>(i + 2, p)));
					}

					list.Sort(compare2);

					//add the first numPerEnsemble points to the ensemble using a keyvaluepair to keep track of original column number
					for (int i = 0; i < ensemble.numberOfModels; i++) {
						ensemble.points[range, i] = list[i].Value.Value;
						ensemble.columnNumbers[range, i] = list[i].Value.Key;
					}
				}
				else {//ensemble is custom, need to create points to match models
					//find points and center
					float precipTotal = 0, tempTotal = 0;
					for (int i = 0; i < ensemble.numberOfModels; i++) {
						int col = ensemble.columnNumbers[0, i] - 2;
						float precip = (float) precipChangeFactors[range, col];
						precipTotal += precip;
						float temp = (float) tempChangeFactors[range, col];
						tempTotal += temp;
						ensemble.points[range, i] = new PointF(precip, temp);
					}
					ensemble.center[range] = new PointF(precipTotal / ensemble.numberOfModels, tempTotal / ensemble.numberOfModels);
				}
			}
		}

		//finds the x and y entries that correspond to the desired percentiles
		private PointF findPercentiles(Ensemble ensemble, int range) {
			float x = (float) Percentile(sortedPrecipChangeFactors[range], ensemble.precipPercent);
			float y = (float) Percentile(sortedTempChangeFactors[range], ensemble.tempPercent);
			return new PointF(x, y);
		}

		//find the given percentile from sequence
		private double Percentile(List<double> sequence, double percentile) {
			int N = sequence.Count;
			if (N == 0)
				return 0;
			double n = (N - 1) * percentile + 1;
			if (n == 1d) return sequence[0];
			else if (n == N) return sequence[N - 1];
			else
			{
				 int k = (int)n;
				 double d = n - k;
				 return sequence[k - 1] + d * (sequence[k] - sequence[k - 1]);
			}
		}

		static private int compare1(KeyValuePair<int, PointF> a, KeyValuePair<int, PointF> b) {
			return a.Value.X.CompareTo(b.Value.X);
		}

		static private int compare2(KeyValuePair<double, KeyValuePair<int, PointF>> a, KeyValuePair<double, KeyValuePair<int, PointF>> b) {
			return a.Key.CompareTo(b.Key);
		}

		static private int compare3(PointF a, PointF b) {
			return a.X.CompareTo(b.X);
		}

		#endregion Private Methods

		public class Ensemble {
			//used by all ensembles:
			public String ensembleName { get; set; }//the ensembles name
			public PointF[,] points { get; set; }//The future time periods * PointF = precip change factor, temp change factor 
			public int numberOfModels { get; set; }//the number of models in this ensemble
			public int[,] columnNumbers { get; set; }//The future time periods * The column numbers of the members of the ensemble
			public List<double>[, , , ,] monthlyClusterData;//12 months * numModels * precip, temp * historical, future * future periods
			public double[, ,] deltaEnsemble = null;//12 months * precip, temp * future periods
			public double[, , ,] hybridDeltaEnsemble = null;//12 months * 49 quantiles .02 - .98 * precip, temp * future periods
			public double[, , , ,] hybridEnsemble = null;//12 months * 49 quantiles .02 - .98 * precip, temp * historical, future * future periods

			//statistical variables:
			public bool statistical { get; set; }//true if ensemble will be found by statistical methods, false if models have already been chosen
			public PointF[] center { get; set; }//the center of the ensemble for each future time period
			public double precipPercent { get; set; }//the precip percentile of this ensemble
			public double tempPercent { get; set; }//the temp percentile of this ensemble
			
			public Ensemble(String name) {
				ensembleName = name;
			}

			public PointPairList getEnsemblePoints(int futurePeriod) {
				int numberOfFuturePeriods = points.GetLength(0);
				int numberOfChangeFactors = points.GetLength(1);
				PointPairList result = new PointPairList();

				for (int col = 0; col < numberOfChangeFactors; col++) {
					PointF p = points[futurePeriod, col];
					result.Add(p.X, p.Y);
				}

				return result;
			}

			static private int compare(PointF a, PointF b) {
				return a.X.CompareTo(b.X);
			}
		}

		public class DateRange {
			public int startMonth { get; set; }
			public int endMonth { get; set; }
			public int startYear { get; set; }
			public int endYear { get; set; }
			private string[] months = new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

			public DateRange(int startMonth, int startYear, int endMonth, int endYear) {
				this.startMonth = startMonth;
				this.startYear = startYear;
				this.endMonth = endMonth;
				this.endYear = endYear;
			}

			public override string ToString() {
					return months[startMonth - 1] + " " + startYear + " - " + months[endMonth - 1] + " " + endYear;
			}

			public string ToStringWithUnderscores() {
				return months[startMonth - 1] + startYear + "_" + months[endMonth - 1] + endYear;
			}
		}
	}
}
