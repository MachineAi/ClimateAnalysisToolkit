using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;

namespace ClimateAnalysis.Tests
{
    [TestFixture]
    public class TestVIC {

        string buildDirToProjectDir = "..\\..\\";
        string buildDir = AppDomain.CurrentDomain.BaseDirectory;

        [Test]
        public void AdjustForcingVIC() {
            var projDir = Path.GetFullPath(Path.Combine(buildDir, buildDirToProjectDir));
            var vicDataDir = Path.Combine(projDir, "Tests", "Data", "VIC");
            var inputs = Path.Combine(vicDataDir, "inputs");
            var outputs_expected = Path.Combine(vicDataDir, "outputs_expected");
            var outputs = Path.Combine(vicDataDir, "outputs");
            Directory.CreateDirectory(outputs);

            var vicP = Path.Combine(inputs, "Yakima_Prcp_SpatialStat_mean.CMIP5.csv");
            var vicT = Path.Combine(inputs, "Yakima_Tavg_SpatialStat_mean.CMIP5.csv");
            var proj = Path.Combine(inputs, "Projections.CMIP5.txt");
            var forc = Path.Combine(inputs, "Yakima_Baseline_forcing_46.03125_-120.46875");
            
            var ca = new ClimateAnalysis();
            var dates = new Dates(ca);
            dates.DateRange = new List<ProcessData.DateRange>() {
                new ProcessData.DateRange(1, 1980, 12, 2009),
                new ProcessData.DateRange(1, 2070, 12, 2099) 
            };
            
            var processor = new ProcessData(dates);
            processor.Import(vicP, vicT);
            processor.importNames(proj);
            processor.generateChangeFactors();
            processor.findEnsembles(createStandardEnsembles(0.20f, 0.50f, 0.80f, 10));
            processor.generateDeltas();

            var output = new OutputData(processor, outputs);
            output.writeHybridDeltaEnsemble();
            output.writeProjectionSummaries();
            output.adjustForcingFile(forc, true, false, new DateTime(1915, 1, 1));

            string md5s = Path.Combine(outputs_expected, "md5s.txt");
            using (StreamReader sw = new StreamReader(md5s)) {
                string line;
                while (!sw.EndOfStream) {
                    line = sw.ReadLine();
                    string[] info = line.Split(',');
                    string fname = info[0];
                    string md5_prev = info[1];
                    string md5_new = getMD5(Path.Combine(outputs, fname));
                    Assert.AreEqual(md5_prev, md5_new);
                }
            }
        }

        public string getMD5(string filename) {
            using (var md5 = MD5.Create()) {
                using (var stream = File.OpenRead(filename)) {
                    return Encoding.Default.GetString(md5.ComputeHash(stream));
                }
            }
        }

        private ProcessData.Ensemble[] createStandardEnsembles(float low,
            float mid, float high, int ensembleNumber)
        {
            var rval = new ProcessData.Ensemble[5];

            ProcessData.Ensemble hd = new ProcessData.Ensemble("More Warming/Dry");
            hd.statistical = true;
            hd.precipPercent = low;
            hd.tempPercent = high;
            hd.numberOfModels = ensembleNumber;
            rval[0] = hd;

            ProcessData.Ensemble hw = new ProcessData.Ensemble("More Warming/Wet");
            hw.statistical = true;
            hw.precipPercent = high;
            hw.tempPercent = high;
            hw.numberOfModels = ensembleNumber;
            rval[1] = hw;

            ProcessData.Ensemble mi = new ProcessData.Ensemble("Median");
            mi.statistical = true;
            mi.precipPercent = mid;
            mi.tempPercent = mid;
            mi.numberOfModels = ensembleNumber;
            rval[2] = mi;

            ProcessData.Ensemble wd = new ProcessData.Ensemble("Less Warming/Dry");
            wd.statistical = true;
            wd.precipPercent = low;
            wd.tempPercent = low;
            wd.numberOfModels = ensembleNumber;
            rval[3] = wd;

            ProcessData.Ensemble ww = new ProcessData.Ensemble("Less Warming/Wet");
            ww.statistical = true;
            ww.precipPercent = high;
            ww.tempPercent = low;
            ww.numberOfModels = ensembleNumber;
            rval[4] = ww;

            return rval;
        }
    }
}
