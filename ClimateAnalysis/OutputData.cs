using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
//using Reclamation.Core;
//using Reclamation.TimeSeries;

namespace ClimateAnalysis {
    public class OutputData {
        private string outputFolderName = "";
        private ProcessData processor;
        private ProcessData.Ensemble[] ensembles;
        private List<KeyValuePair<DateTime, double[]>> forcingData;//data from the forcing file
        private List<KeyValuePair<DateTime, Dictionary<string, double[]>>> forcingDataGSFLOW;
        private List<string> headerGSFLOW;
        private Dictionary<string, int> varsGSFLOW;
        private HashSet<string> varsOrderedGSFLOW;
        private List<MonthlyData> monthlyData; //data for each month in the historical time period
        private List<ProcessData.DateRange> dates;
        private int namePadding;

        #region public methods

        /// <summary>
        /// Constructs an OutputData object
        /// </summary>
        /// <param name="proc">The ProcessData object containing the data.</param>
        /// <param name="outFolderName">The path of the folder where the files will be written to.</param>
        public OutputData(ProcessData proc, string outFolderName) {
            processor = proc;
            outputFolderName = outFolderName;
            ensembles = processor.getEnsembles();
            dates = processor.getDates();
        }

        /// <summary>
        /// Writes the hybrid delta ensemble data to file
        /// </summary>
        public void writeHybridDeltaEnsemble() {//ensemble.hybriddeltaensemble is null for some reason
            List<string> lines;
            
            if(outputFolderName == "")
                return;

            setNamePadding();

            for (int range = 0; range < dates.Count - 1; range++) {
                lines = new List<string>();
                lines.Add(string.Format("{0,-" + namePadding + "}", "ensemble") + "\tqtl \tJan \tFeb \tMar \tApr \tMay \tJun \tJul \tAug \tSep \tOct \tNov \tDec");

                //go through each percentile from .02 to .98
                for (int i = 0; i < 49; i++) {
                    double percentile = (int) i * .02 + .02;

                    for (int j = 0; j < ensembles.Count(); j++) {
                        //hybridDeltaEnsemble = 12 months * 49 quantiles .02 - .98 * precip, temp * future periods
                        ProcessData.Ensemble ensemble = ensembles[j];

                        for (int temp = 0; temp < 2; temp++) {
                            string name;
                            if (temp == 0)
                                name = ensemble.ensembleName + "_dP";
                            else
                                name = ensemble.ensembleName + "_dT";
                            lines.Add(string.Format("{0,-" + namePadding + "}", name) + "\t" + percentile.ToString("F2") + "\t" + ensemble.hybridDeltaEnsemble[0, i, temp, range].ToString("F2") + "\t" +
                                ensemble.hybridDeltaEnsemble[1, i, temp, range].ToString("F2") + "\t" + ensemble.hybridDeltaEnsemble[2, i, temp, range].ToString("F2") + "\t" +
                                ensemble.hybridDeltaEnsemble[3, i, temp, range].ToString("F2") + "\t" + ensemble.hybridDeltaEnsemble[4, i, temp, range].ToString("F2") + "\t" +
                                ensemble.hybridDeltaEnsemble[5, i, temp, range].ToString("F2") + "\t" + ensemble.hybridDeltaEnsemble[6, i, temp, range].ToString("F2") + "\t" +
                                ensemble.hybridDeltaEnsemble[7, i, temp, range].ToString("F2") + "\t" + ensemble.hybridDeltaEnsemble[8, i, temp, range].ToString("F2") + "\t" +
                                ensemble.hybridDeltaEnsemble[9, i, temp, range].ToString("F2") + "\t" + ensemble.hybridDeltaEnsemble[10, i, temp, range].ToString("F2") + "\t" +
                                ensemble.hybridDeltaEnsemble[11, i, temp, range].ToString("F2"));
                        }
                    }
                }
                File.WriteAllLines(outputFolderName + @"\Monthly_HDePT_Factors_" + dates[range + 1].ToStringWithUnderscores() + ".txt", lines);
            }
        }

        /// <summary>
        /// Writes the hybrid ensemble data to file (mean quantiles from historical and future used to compute hybridDeltaEnsemble)
        /// </summary>
        public void writeHybridEnsemble() {
            List<string> lines;

            if (outputFolderName == "")
                return;

            setNamePadding();

            for (int range = 0; range < dates.Count - 1; range++) {
                lines = new List<string>();
                lines.Add(string.Format("{0,-" + (namePadding + 7) + "}", "ensemble") + "\tqtl \tJan \tFeb \tMar \tApr \tMay \tJun \tJul \tAug \tSep \tOct \tNov \tDec");

                for (int i = 0; i < 49; i++) {
                    double percentile = (int) i * .02 + .02;

                    for (int j = 0; j < ensembles.Count(); j++) {
                        ProcessData.Ensemble ensemble = ensembles[j];

                        for (int temp = 0; temp < 2; temp++) {
                            for (int hist = 0; hist < 2; hist++) {
                                string name;
                                if (temp == 0) {
                                    if (hist == 0)
                                        name = ensemble.ensembleName + "_mean_hisP";
                                    else
                                        name = ensemble.ensembleName + "_mean_futP";
                                } else {
                                    if (hist == 0)
                                        name = ensemble.ensembleName + "_mean_hisT";
                                    else
                                        name = ensemble.ensembleName + "_mean_futT";
                                }
                                //12 months * 49 quantiles .02 - .98 * precip, temp * historical, future * future periods
                                lines.Add(string.Format("{0,-" + (namePadding + 7) + "}", name) + "\t" + percentile.ToString("F2") + "\t" + ensemble.hybridEnsemble[0, i, temp, hist, range].ToString("F2") + "\t" +
                                    ensemble.hybridEnsemble[1, i, temp, hist, range].ToString("F2") + "\t" + ensemble.hybridEnsemble[2, i, temp, hist, range].ToString("F2") + "\t" +
                                    ensemble.hybridEnsemble[3, i, temp, hist, range].ToString("F2") + "\t" + ensemble.hybridEnsemble[4, i, temp, hist, range].ToString("F2") + "\t" +
                                    ensemble.hybridEnsemble[5, i, temp, hist, range].ToString("F2") + "\t" + ensemble.hybridEnsemble[6, i, temp, hist, range].ToString("F2") + "\t" +
                                    ensemble.hybridEnsemble[7, i, temp, hist, range].ToString("F2") + "\t" + ensemble.hybridEnsemble[8, i, temp, hist, range].ToString("F2") + "\t" +
                                    ensemble.hybridEnsemble[9, i, temp, hist, range].ToString("F2") + "\t" + ensemble.hybridEnsemble[10, i, temp, hist, range].ToString("F2") + "\t" +
                                    ensemble.hybridEnsemble[11, i, temp, hist, range].ToString("F2"));
                            }
                        }
                    }
                }
                File.WriteAllLines(outputFolderName + @"\Monthly_HePT_Factors_" + dates[range + 1].ToStringWithUnderscores() + ".txt", lines);
            }
        }

        /// <summary>
        /// Writes the names of the models that are included in each cluster
        /// </summary>
        public void writeProjectionSummaries() {
            List<string> toWrite;
            
            if(outputFolderName == "")
                return;

            setNamePadding();

            string[] names = processor.getModelNames();

            for (int range = 0; range < dates.Count - 1; range++) {
                toWrite = new List<string>();

                for (int i = 0; i < ensembles.Length; i++) {
                    string currentLine = "";
                    ProcessData.Ensemble ensemble = ensembles[i];
                    currentLine += ensemble.ensembleName + "\r\n";
                    if (names == null) {//print out column numbers
                        int rank = 1;
                        currentLine += string.Format("{0,-25}", "Column") + "\tChange in Precip(%)\tChange in Temp(degC)\tRank\r\n";
                        for (int index = 0; index < ensemble.columnNumbers.GetLength(1); index++) {
                            currentLine += "\t" + (ensemble.columnNumbers[range, index] + 1) + "\t\t\t" + ensemble.points[range, index].X.ToString("F6") + 
                                "\t\t\t" + ensemble.points[range, index].Y.ToString("F6") + "\t\t\t" + (rank++) + "\r\n";
                        }
                    }
                    else {//print out GCM names;
                        int rank = 1;
                        currentLine += string.Format("{0,-25}", "GCM") + "\tChange in Precip(%)\tChange in Temp(degC)\tRank\r\n";
                        for (int index = 0; index < ensemble.columnNumbers.GetLength(1); index++) {
                            int adjustedRange = range;
                            if (!ensemble.statistical)//if the ensemble is custom, it will only have set of column numbers
                                adjustedRange = 0;
                            currentLine += string.Format("{0,-25}", names[ensemble.columnNumbers[adjustedRange, index] - 2]) + "\t\t" + ensemble.points[range, index].X.ToString("F6") + 
                                "\t\t\t" + ensemble.points[range, index].Y.ToString("F6") + "\t\t\t" + (rank++) + "\r\n";
                        }
                    }
                    toWrite.Add(currentLine);
                }
                File.WriteAllLines(outputFolderName + @"\Projections_Summary_" + dates[range + 1].ToStringWithUnderscores() + ".txt", toWrite);
            }
        }
        
        /// <summary>
        /// Writes the delta ensemble file which contains the average change factors for each month and ensemble
        /// </summary>
        public void writeDeltaEnsemble() {
            List<string> lines;

            if(outputFolderName == "")
                return;

            setNamePadding();

            //write deltaEnsembles to file
            for (int range = 0; range < dates.Count - 1; range++) {
                lines = new List<string>();

                lines.Add(string.Format("{0,-" + namePadding + "}", " ") + "\tJan \tFeb \tMar \tApr \tMay \tJun \tJul \tAug \tSep \tOct \tNov \tDec");

                for (int i = 0; i < ensembles.Length; i++) {
                    ProcessData.Ensemble ensemble = ensembles[i];
                    //deltaEnsemble: 12 months * precip, temp * future periods
                    for (int temp = 0; temp < 2; temp++) {
                        string name;
                        if (temp == 0)
                            name = ensemble.ensembleName + "_dP";
                        else
                            name = ensemble.ensembleName + "_dT";
                        lines.Add(string.Format("{0,-" + namePadding + "}", name) + "\t" + ensemble.deltaEnsemble[0, temp, range].ToString("F2") + "\t" +
                            ensemble.deltaEnsemble[1, temp, range].ToString("F2") + "\t" + ensemble.deltaEnsemble[2, temp, range].ToString("F2") + "\t" +
                            ensemble.deltaEnsemble[3, temp, range].ToString("F2") + "\t" + ensemble.deltaEnsemble[4, temp, range].ToString("F2") + "\t" +
                            ensemble.deltaEnsemble[5, temp, range].ToString("F2") + "\t" + ensemble.deltaEnsemble[6, temp, range].ToString("F2") + "\t" +
                            ensemble.deltaEnsemble[7, temp, range].ToString("F2") + "\t" + ensemble.deltaEnsemble[8, temp, range].ToString("F2") + "\t" +
                            ensemble.deltaEnsemble[9, temp, range].ToString("F2") + "\t" + ensemble.deltaEnsemble[10, temp, range].ToString("F2") + "\t" +
                            ensemble.deltaEnsemble[11, temp, range].ToString("F2"));
                    }
                }
                File.WriteAllLines(outputFolderName + @"\Monthly_DePT_Factors_" + dates[range + 1].ToStringWithUnderscores() + ".txt", lines);
            }
        }

        /// <summary>
        /// Reads in a forcing file and writes out a copy for each ensemble 
        /// with the precipitation and temperatures adjusted according to the 
        /// ensemble results.
        /// </summary>
        /// <param name="forcingFile">The path to the forcing file.</param>
        /// <param name="VIC">True if VIC, false if DHSVM</param>
        public void adjustForcingFile(string forcingFile, ForcingFormat format, 
            bool generateDatabase, DateTime forcingFileStartDate = default(DateTime)) {
            
            if (outputFolderName == "")
                return;
            if (format == ForcingFormat.VIC && forcingFileStartDate == default(DateTime))
                return;

            //read in forcing file
            readForcingFile(forcingFile, format, forcingFileStartDate);

            //write adjusted data to output folder
            writeAdjustedForcingFiles(forcingFile, format);

            //generate pisces database if requested
            //if (generateDatabase)
            //    generatePiscesDatabase();
        }

        public void setSaveToFolderName(string path) {
            outputFolderName = path;
        }

        #endregion public methods

        #region private methods

        //Reads in the forcing file.  forcingFileStartDate is used if a VIC 
        //file is being imported to allow the date of each row to be determined
        //and stored.
        private void readForcingFile(string forcingFile, ForcingFormat format, 
            DateTime forcingFileStartDate) {
            
            DateTime startDate = new DateTime(dates[0].startYear, dates[0].startMonth, 1);//period to be adjusted starts on this date
            DateTime endDate = new DateTime(dates[0].endYear, dates[0].endMonth, 1);
            endDate = endDate.AddMonths(1);//period to be adjusted ends on the day before this date

            forcingData = new List<KeyValuePair<DateTime, double[]>>();

            switch (format) {
                case ForcingFormat.VIC:
                    forcingData = readForcingFileVIC(forcingFile, startDate, endDate, forcingFileStartDate);
                    break;
                case ForcingFormat.DHSVM:
                    forcingData = readForcingFileDHSVM(forcingFile, startDate, endDate);
                    break;
                case ForcingFormat.GSFLOW:
                    forcingDataGSFLOW = readForcingFileGSFLOW(forcingFile, startDate, endDate);
                    break;
                default:
                    throw new InvalidEnumArgumentException();
            }
        }

        private List<KeyValuePair<DateTime, double[]>> readForcingFileVIC(string forcingFile, 
            DateTime startDate, DateTime endDate, DateTime forcingFileStartDate) {

            var rval = new List<KeyValuePair<DateTime, double[]>>();

            string[] lines = File.ReadAllLines(forcingFile);
            string[] line = SplitOnSpaceOrTab(lines[0]);
            double[] values;
            int numRows = lines.Length;
            int numCols = line.Length + 1;

            for (int row = 0; row < numRows; row++) {
                values = new double[numCols];//Precip, MaxTemp, MinTemp, Wind, AvgTemp
                line = SplitOnSpaceOrTab(lines[row]);
                DateTime date = forcingFileStartDate.AddDays(Convert.ToDouble(row));

                //skip row if outside of time period
                if (date < startDate || date >= endDate)
                    continue;

                for (int col = 0; col < numCols; col++) {
                    if (col == numCols - 1) {
                        values[numCols - 1] = (values[1] + values[2]) / 2;
                        continue;
                    }
                    try {
                        values[col] = double.Parse(line[col]);
                    } catch (Exception) {
                        values[col] = double.NaN;
                    }
                }

                rval.Add(new KeyValuePair<DateTime, double[]>(date, values));
            }

            return rval;
        }

        private List<KeyValuePair<DateTime, double[]>> readForcingFileDHSVM(string forcingFile, 
            DateTime startDate, DateTime endDate) {

            var rval = new List<KeyValuePair<DateTime, double[]>>();

            string[] lines = File.ReadAllLines(forcingFile);
            string[] line = SplitOnSpaceOrTab(lines[0]);
            double[] values;
            int numRows = lines.Length;
            int numCols = line.Length;

            for (int row = 0; row < numRows; row++) {
                values = new double[numCols - 1];
                line = SplitOnSpaceOrTab(lines[row]);
                var dateHour = line[0].Split('-');
                DateTime date = DateTime.Parse(dateHour[0]).AddHours(Convert.ToInt16(dateHour[1].Split(':')[0]));

                //skip row if outside of time period
                if (date < startDate || date >= endDate)
                    continue;

                for (int col = 1; col < numCols; col++) {
                    try {
                        values[col - 1] = double.Parse(line[col]);
                    } catch (Exception) {
                        values[col - 1] = double.NaN;
                    }
                }

                rval.Add(new KeyValuePair<DateTime, double[]>(date, values));
            }

            return rval;
        }

        private List<KeyValuePair<DateTime, Dictionary<string, double[]>>> readForcingFileGSFLOW(string forcingFile, 
            DateTime startDate, DateTime endDate) {

            var rval = new List<KeyValuePair<DateTime, Dictionary<string, double[]>>>();
            
            string[] lines = File.ReadAllLines(forcingFile);

            //store header lines to write out later
            int lineIdx = 0;
            headerGSFLOW = new List<string>();
            do
            {
                headerGSFLOW.Add(lines[lineIdx++]);
            } while (!lines[lineIdx].Contains("####"));
            headerGSFLOW.Add(lines[lineIdx++]);

            //store variables and lengths
            varsGSFLOW = new Dictionary<string, int>();
            varsOrderedGSFLOW = new HashSet<string>();
            var knownVars = new List<string>() { "pan_evap", "runoff", 
                "precip", "solrad", "tmax", "tmin", "form_data", "rain_day" };
            foreach (var item in headerGSFLOW) {
                var line = SplitOnSpaceOrTab(item);
                if (knownVars.Contains(line[0])) {
                    varsGSFLOW.Add(line[0], Convert.ToInt32(line[1]));
                    varsOrderedGSFLOW.Add(line[0]);
                }
            }
            
            //get actual data
            for (int i = lineIdx; i < lines.Length; i++) {
                var line = SplitOnSpaceOrTab(lines[i]);
                var iLine = Array.ConvertAll(line.Take(6).ToArray(), Convert.ToInt32);
                var date = new DateTime(iLine[0], iLine[1], iLine[2], iLine[3], iLine[4], iLine[5]);

                //skip row if outside of time period
                if (date < startDate || date >= endDate)
                    continue;

                var dLine = Array.ConvertAll(line.Skip(6).Take(line.Length).ToArray(), Convert.ToDouble);

                int lastIdx = 0;
                var valuesList = new Dictionary<string,double[]>();
                foreach (var pair in varsGSFLOW) {
                    var values = dLine.Skip(lastIdx).Take(pair.Value).ToArray();
                    valuesList.Add(pair.Key, values);
                    lastIdx += pair.Value;
                }
                rval.Add(new KeyValuePair<DateTime, Dictionary<string, double[]>>(date, valuesList));
            }

            return rval;
        }

        static char[] delimiter = new char[] { ' ', '\t' };
        private static string[] SplitOnSpaceOrTab(string line) {
            return line.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
        }

        //Creates a list of MonthlyData objects, one for each month, containing data about each month
        private void findMonthlyValues(ForcingFormat format) {

            double totalTemp = 0, avgTemp = 0, totalPrecip = 0;
            int numOfTimePeriodsThisMonth = 0, month = 0, year = 0, index = 0;
            double[,,] changeFactors;//temporary array to pass to MonthlyData object
            Dictionary<int, int>[] precipPercentiles, tempPercentiles;
            int precipPercentile, tempPercentile;
            List<KeyValuePair<int, double>>[] precipValues = new List<KeyValuePair<int, double>>[12];//a list of totalPrecip values by month
            List<KeyValuePair<int, double>>[] tempValues = new List<KeyValuePair<int, double>>[12];//a list of average temp values by month

            //initialize lists
            monthlyData = new List<MonthlyData>();
            for (int i = 0; i < 12; i++) {
                precipValues[i] = new List<KeyValuePair<int, double>>();
                tempValues[i] = new List<KeyValuePair<int, double>>();
            }

            //creates a MonthlyData object for each month in forcingData
            foreach (KeyValuePair<DateTime, double[]> pair in forcingData) {
                if ((pair.Key.Day == 1 && (format == ForcingFormat.VIC || format == ForcingFormat.GSFLOW)) || 
                    (format == ForcingFormat.DHSVM && pair.Key.Day == 1 && pair.Key.Hour == 0)) {//new month
                    
                    if (numOfTimePeriodsThisMonth != 0) {
                        //add new MonthlyData object to monthlyData
                        avgTemp = totalTemp / numOfTimePeriodsThisMonth;
                        monthlyData.Add(new MonthlyData(index, year, month, totalPrecip, avgTemp));
                        precipValues[month].Add(new KeyValuePair<int, double>(index, totalPrecip));
                        tempValues[month].Add(new KeyValuePair<int, double>(index, avgTemp));
                        index++;
                    }

                    //reset variables					
                    month = pair.Key.Month - 1;
                    year = pair.Key.Year;
                    numOfTimePeriodsThisMonth = 0;
                    totalTemp = totalPrecip = 0;
                }

                switch (format) {
                    case ForcingFormat.VIC:
                        //value = Precip, MaxTemp, MinTemp, Wind, AvgTemp
                        totalPrecip += pair.Value[0];
                        totalTemp += pair.Value[4];
                        break;
                    case ForcingFormat.DHSVM:
                        //value = temp, ?, ?, ?, ?, precip, ?, ?
                        totalPrecip += pair.Value[5];
                        totalTemp += pair.Value[0];
                        break;
                    case ForcingFormat.GSFLOW:
                        //values = precip, tmax, tmin, tavg
                        totalPrecip += pair.Value[0];
                        totalTemp += pair.Value[3];
                        break;
                    default:
                        throw new InvalidEnumArgumentException();
                }
                
                numOfTimePeriodsThisMonth++;
            }

            //add last MonthlyData object to monthlyData
            avgTemp = totalTemp / numOfTimePeriodsThisMonth;
            monthlyData.Add(new MonthlyData(index, year, month, totalPrecip, avgTemp));
            precipValues[month].Add(new KeyValuePair<int, double>(index, totalPrecip));
            tempValues[month].Add(new KeyValuePair<int, double>(index, avgTemp));

            //find percentiles
            precipPercentiles = getPercentiles(precipValues);
            tempPercentiles = getPercentiles(tempValues);

            //get data from ProcessData instance
            ensembles = processor.getEnsembles();
            setNamePadding();

            //add percentiles change factors to each monthlyData object
            for (int range = 0; range < dates.Count - 1; range++) {
                foreach (MonthlyData md in monthlyData) {
                    if (md.changeFactors == null)
                        changeFactors = new double[dates.Count - 1, ensembles.Length, 2];//ensembles * precip, temp
                    else
                        changeFactors = md.changeFactors;
                    precipPercentile = precipPercentiles[md.month][md.index];
                    tempPercentile = tempPercentiles[md.month][md.index];
                    //hDE = 12 months * 49 quantiles .02 - .98 * precip, temp * future periods
                    //fill changeFactors array
                    for (int i = 0; i < ensembles.Length; i++) {
                        ProcessData.Ensemble ensemble = ensembles[i];
                        //precip
                        changeFactors[range, i, 0] = ensemble.hybridDeltaEnsemble[md.month, precipPercentile, 0, range];

                        //temp
                        changeFactors[range, i, 1] = ensemble.hybridDeltaEnsemble[md.month, tempPercentile, 1, range];
                    }

                    //fill in remaining fields in md
                    md.precipPct = precipPercentile;
                    md.tempPct = tempPercentile;
                    md.changeFactors = changeFactors;
                }
            }
        }

        //Creates 12 Dictionarys, one for each month, with the precentile rank of each Jan among all Januaries, etc.
        private Dictionary<int, int>[] getPercentiles(List<KeyValuePair<int, double>>[] list) {
            Dictionary<int, int>[] result = new Dictionary<int,int>[12];//index, percentile 0 - 48
            int numInList;
            double pct;
            int adjPct;
            int frequency, before, after;

            //initialize result array
            for (int i = 0; i < 12; i++) {
                result[i] = new Dictionary<int, int>();
            }

            for (int month = 0; month < 12; month++) {
                //sort by value
                list[month].Sort(compareValues);

                numInList = list[month].Count;

                //write percentiles to new list, percentile rank = (count of all scores less than the current value
                // + 0.5 * frequency of current value) / total number of values
                for (int i = 0; i < numInList; i++) {

                    //find frequency of current entry
                    frequency = 1;
                    before = i - 1;
                    after = i + 1;
                    while (before > 0 && list[month][before].Value == list[month][i].Value) {
                        frequency++;
                        before--;
                    }
                    while (after < numInList && list[month][after].Value == list[month][i].Value) {
                        frequency++;
                        after++;
                    }

                    pct = (Convert.ToDouble(i) + 0.5 * frequency) / Convert.ToDouble(numInList);
                    
                    adjPct = Convert.ToInt32(pct * 48);
                    result[month].Add(list[month][i].Key, adjPct);
                }
            }

            return result;
        }

        //used to sort KeyValuePairs by the values.
        static private int compareValues(KeyValuePair<int, double> a, KeyValuePair<int, double> b) {
            return a.Value.CompareTo(b.Value);
        }

        //write out copies of the forcing file, one for each ensemble, adjusting the precip and temp columns in the process
        private void writeAdjustedForcingFiles(string forcingFile, ForcingFormat format) {

            switch (format)
            {
                case ForcingFormat.VIC:
                case ForcingFormat.DHSVM:
                    writeAdjustedForcingFilesVICorDHSVM(forcingFile, format);
                    break;
                case ForcingFormat.GSFLOW:
                    writeAdjustedForcingFilesGSFLOW(forcingFile, format);
                    break;
                default:
                    throw new InvalidEnumArgumentException();
            }
        }

        private void writeAdjustedForcingFilesGSFLOW(string forcingFile,
            ForcingFormat format) {

                for (int range = 0; range < dates.Count - 1; range++) {
                    var output = new List<KeyValuePair<DateTime, Dictionary<string, double[]>>>[ensembles.Length];
                    for (int i = 0; i < output.Length; i++)  {
                        output[i] = new List<KeyValuePair<DateTime, Dictionary<string, double[]>>>(forcingDataGSFLOW);
                    }

                    //use precip as surrogate for number of grid points to adjust
                    for (int i = 0; i < varsGSFLOW["precip"]; i++) {
                        forcingData = getVIClikeGSFLOWforcingData(i);
                        findMonthlyValues(format);

                        int monthIdx = 0;
                        MonthlyData monthData = monthlyData[0];

                        int dateIdx = 0;
                        foreach (KeyValuePair<DateTime, double[]> pair in forcingData) {
                            if (pair.Key.Day == 1)
                                monthData = monthlyData[monthIdx++];

                            //values = precip, tmax, tmin, tavg     monthData.changeFactors = future date ranges * ensembles * precip, temp
                            for (int ensemble = 0; ensemble < ensembles.Length; ensemble++) {
                                double precip = pair.Value[0];
                                double precipFactor = monthData.changeFactors[range, ensemble, 0];
                                double tempFactor = monthData.changeFactors[range, ensemble, 1];
                                if (precipFactor != 0)//multiply precip value by change factor if factor is not 0, change factor will be 0 sometimes with summer only
                                    precip *= precipFactor;
                                output[ensemble][dateIdx].Value["precip"][i] = precip;
                                if (pair.Value[1] != -901.0)
                                    output[ensemble][dateIdx].Value["tmax"][i] = pair.Value[1] + tempFactor;
                                if (pair.Value[2] != -901.0)
                                    output[ensemble][dateIdx].Value["tmin"][i] = pair.Value[2] + tempFactor;
                            }
                            dateIdx++;
                        }
                    }

                    //write output files to GSFLOW format
                    for (int ensemble = 0; ensemble < output.Length; ensemble++) {
                        var fname = outputFolderName + "/" + makeValidFileName(ensembles[ensemble].ensembleName) + "_" + dates[range + 1].ToStringWithUnderscores() + "_" + Path.GetFileName(forcingFile);
                        using (TextWriter fileTW = new StreamWriter(fname)) {
                            fileTW.NewLine = "\n";
                            //write header
                            foreach (var line in headerGSFLOW) {
                                fileTW.WriteLine(line);
                            }
                            for (int i = 0; i < output[ensemble].Count; i++) {
                                //write date
                                fileTW.Write(output[ensemble][i].Key.ToString("yyyy M d H m s") + " ");
                                //write each var
                                foreach (string var in varsOrderedGSFLOW) {
                                    var line = string.Join(" ", output[ensemble][i].Value[var].Select(x => x.ToString("F02")).ToArray());
                                    fileTW.Write(line + " ");
                                }
                                fileTW.WriteLine("");
                            }
                        }
                    }

                }
        }

        private List<KeyValuePair<DateTime, double[]>> getVIClikeGSFLOWforcingData(int i) {

            var rval = new List<KeyValuePair<DateTime, double[]>>();
            
            foreach (var item in forcingDataGSFLOW) {
                //values = precip, tmax, tmin, tavg
                var values = new double[4];
                values[0] = item.Value["precip"][i];
                values[1] = item.Value["tmax"][i];
                values[2] = item.Value["tmin"][i];
                values[3] = (values[1] + values[2]) / 2;
                rval.Add(new KeyValuePair<DateTime, double[]>(item.Key, values));
            }

            return rval;
        }

        private void writeAdjustedForcingFilesVICorDHSVM(string forcingFile, 
            ForcingFormat format) {
            
            findMonthlyValues(format);
            for (int range = 0; range < dates.Count - 1; range++) {
                var output = new List<string>[ensembles.Length];
                for (int i = 0; i < output.Length; i++) {
                    output[i] = new List<string>();
                }

                int index = 0;
                MonthlyData monthData = monthlyData[0];

                foreach (KeyValuePair<DateTime, double[]> pair in forcingData) {
                    if (format == ForcingFormat.VIC) {
                        if (pair.Key.Day == 1)
                            monthData = monthlyData[index++];

                        //value in vic = Precip, MaxTemp, MinTemp, Wind, AvgTemp;     monthData.changeFactors = future date ranges * ensembles * precip, temp
                        for (int ensemble = 0; ensemble < ensembles.Length; ensemble++) {
                            double precip = pair.Value[0];
                            if (monthData.changeFactors[range, ensemble, 0] != 0)//multiply precip value by change factor if factor is not 0, change factor will be 0 sometimes with summer only
                                precip *= monthData.changeFactors[range, ensemble, 0];
                            output[ensemble].Add(precip.ToString("F04") + "\t" + (pair.Value[1] + monthData.changeFactors[range, ensemble, 1]).ToString("F04") +
                                "\t" + (pair.Value[2] + monthData.changeFactors[range, ensemble, 1]).ToString("F04") + "\t" + pair.Value[3].ToString("F04"));
                        }
                    } else if (format == ForcingFormat.DHSVM) {
                        if (pair.Key.Day == 1 && pair.Key.Hour == 0)
                            monthData = monthlyData[index++];

                        string date = pair.Key.ToString("MM/dd/yyyy-HH");
                        string glacier = "";//if the DHSVM file has 9 columns, the last column has something to do with glacier lapse rates
                        if (pair.Value.Length == 8)
                            glacier = pair.Value[7].ToString("F08");

                        //value = temp, ?, ?, ?, ?, precip, ?, ?,  monthData.changeFactors = future date ranges * ensembles * precip, temp
                        for (int ensemble = 0; ensemble < ensembles.Length; ensemble++) {
                            double precip = pair.Value[5];
                            if (monthData.changeFactors[range, ensemble, 0] != 0)//multiply precip value by change factor if factor is not 0, change factor will be 0 sometimes with summer only
                                precip *= monthData.changeFactors[range, ensemble, 0];
                            output[ensemble].Add(date + " " + (pair.Value[0] + monthData.changeFactors[range, ensemble, 1]).ToString("F04") + " " + pair.Value[1].ToString("F04") + " " +
                                pair.Value[2].ToString("F04") + " " + pair.Value[3].ToString("F04") + " " + pair.Value[4].ToString("F04") + " " +
                                precip.ToString("F07") + " " + pair.Value[6].ToString("F08") + " " + glacier);
                        }
                    }
                }

                for (int ensemble = 0; ensemble < output.Length; ensemble++) {
                    var fname = outputFolderName + "/" + makeValidFileName(ensembles[ensemble].ensembleName) + "_" + dates[range + 1].ToStringWithUnderscores() + "_" + Path.GetFileName(forcingFile);
                    using (TextWriter fileTW = new StreamWriter(fname)) {
                        fileTW.NewLine = "\n";
                        for (int i = 0; i < output[ensemble].Count; i++)
                            fileTW.WriteLine(output[ensemble][i]);
                    }
                }
            }
        }

        //replaces any forbidden characters in the ensemble name with an underscore
        private string makeValidFileName(string filename) {
            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars) {
                filename = filename.Replace(c, ' ');
            }
            filename = filename.Replace(" ", "");
            filename = filename.Replace("_", "");
            return filename;
        }

        //Creates a Pisces Database from the monthlyData
        //private void generatePiscesDatabase() {
        //    String connectionString = "Data Source=" + outputFolderName + @"\ChangeFactorStats.sdf; Version=3";
        //    Reclamation.Core.SQLiteServer DB = new SQLiteServer(connectionString);
        //    TimeSeriesDatabase pDB = new TimeSeriesDatabase(DB);
        //    TimeSeriesDatabaseDataSet.SeriesCatalogRow serCatRow = pDB.GetSeriesCatalog()[0];
        //    PiscesFolder prntFldr = new PiscesFolder(pDB, serCatRow);

        //    SeriesList HWt = new SeriesList();
        //    SeriesList HWp = new SeriesList();
        //    SeriesList HDt = new SeriesList();
        //    SeriesList HDp = new SeriesList();
        //    SeriesList MIt = new SeriesList();
        //    SeriesList MIp = new SeriesList();
        //    SeriesList WWt = new SeriesList();
        //    SeriesList WWp = new SeriesList();
        //    SeriesList WDt = new SeriesList();
        //    SeriesList WDp = new SeriesList();

        //    Series HDdP = new Series();
        //    Series HDdT = new Series();
        //    Series HWdP = new Series();
        //    Series HWdT = new Series();
        //    Series WDdP = new Series();
        //    Series WDdT = new Series();
        //    Series WWdP = new Series();
        //    Series WWdT = new Series();
        //    Series MIdP = new Series();
        //    Series MIdT = new Series();
            
        //    //get the data from each month and put it in the appropriate series
        //    foreach (MonthlyData md in monthlyData) {
        //        DateTime date = new DateTime(md.year, md.month + 1, 1);
        //        double[,,] changeFactors = md.changeFactors;//future date ranges * ensembles * precip, temp
        //        HDdP.Add(date, changeFactors[0, 0, 0]);
        //        HDdT.Add(date, changeFactors[0, 0, 1]);
        //        HWdP.Add(date, changeFactors[0, 1, 0]);
        //        HWdT.Add(date, changeFactors[0, 1, 1]);
        //        WDdP.Add(date, changeFactors[0, 2, 0]);
        //        WDdT.Add(date, changeFactors[0, 2, 1]);
        //        WWdP.Add(date, changeFactors[0, 3, 0]);
        //        WWdT.Add(date, changeFactors[0, 3, 1]);
        //        MIdP.Add(date, changeFactors[0, 4, 0]);
        //        MIdT.Add(date, changeFactors[0, 4, 1]);
        //    }

        //    //add the series to the serieslists
        //    HWt.Add(HWdT);
        //    HWp.Add(HWdP);
        //    HDt.Add(HDdT);
        //    HDp.Add(HDdP);
        //    MIt.Add(MIdT);
        //    MIp.Add(MIdP);
        //    WWt.Add(WWdT);
        //    WWp.Add(WWdP);
        //    WDt.Add(WDdT);
        //    WDp.Add(WDdP);

        //    //set the names and providers of the series
        //    HWdT.Name = "HWdT";
        //    HWdT.Provider = "Series";
        //    HWdP.Name = "HWdP";
        //    HWdP.Provider = "Series";
        //    HDdT.Name = "HDdT";
        //    HDdT.Provider = "Series";
        //    HDdP.Name = "HDdP";
        //    HDdP.Provider = "Series";
        //    MIdT.Name = "MIdT";
        //    MIdT.Provider = "Series";
        //    MIdP.Name = "MIdP";
        //    MIdP.Provider = "Series";
        //    WWdT.Name = "WWdT";
        //    WWdT.Provider = "Series";
        //    WWdP.Name = "WWdP";
        //    WWdP.Provider = "Series";
        //    WDdT.Name = "WDdT";
        //    WDdT.Provider = "Series";
        //    WDdP.Name = "WDdP";

        //    //Add Folder and Series to database
        //    PiscesFolder sFldr = pDB.AddFolder(forcingFileName);
        //    sFldr.Parent = prntFldr;
        //    pDB.AddSeries(HWdT, sFldr);
        //    pDB.AddSeries(HWdP, sFldr);
        //    pDB.AddSeries(HDdT, sFldr);
        //    pDB.AddSeries(HDdP, sFldr);
        //    pDB.AddSeries(MIdT, sFldr);
        //    pDB.AddSeries(MIdP, sFldr);
        //    pDB.AddSeries(WWdT, sFldr);
        //    pDB.AddSeries(WWdP, sFldr);
        //    pDB.AddSeries(WDdT, sFldr);
        //    pDB.AddSeries(WDdP, sFldr);
        //}

        private void setNamePadding() {
            int longest = 0;
            foreach (ProcessData.Ensemble e in ensembles)
                if (e.ensembleName.Length > longest)
                    longest = e.ensembleName.Length;
            namePadding = longest + 6;
        }

        #endregion private methods

        //Objects that hold the data for each month
        private class MonthlyData {
            public int index { get; set; }//the number of the month, from 0 to the total number of months - 1
            public int year { get; set; }
            public int month { get; set; }
            public double precipSum { get; set; }
            public double tempAvg { get; set; }
            public int precipPct { get; set; }//0 - 48 for .02 - .98
            public int tempPct { get; set; }//0 - 48 for .02 - .98
            public double[,,] changeFactors { get; set; }//future date ranges * ensembles * precip, temp

            public MonthlyData(int index, int year, int month, double precipSum, double tempAvg) {
                this.index = index;
                this.year = year;
                this.month = month;
                this.precipSum = precipSum;
                this.tempAvg = tempAvg;
                changeFactors = null;
            }
        }
    }
}
