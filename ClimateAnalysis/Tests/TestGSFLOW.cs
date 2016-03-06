using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace ClimateAnalysis.Tests
{
    [TestFixture]
    class TestGSFLOW {

        string buildDirToProjectDir = "..\\..\\..\\";
        string buildDir = AppDomain.CurrentDomain.BaseDirectory;

        [Test]
        public void AdjustForcingGSFLOW() {
            var projDir = Path.GetFullPath(Path.Combine(buildDir, buildDirToProjectDir));
            var gsflowDataDir = Path.Combine(projDir, "Tests", "Data", "GSFLOW");
            var inputs = Path.Combine(gsflowDataDir, "inputs");
            var outputs_expected = Path.Combine(gsflowDataDir, "outputs_expected");
            var outputs = Path.Combine(gsflowDataDir, "outputs");
            Directory.CreateDirectory(outputs);

            var vicP = Path.Combine(inputs, "CRBIA_Deschutes_Prcp_SpatialStat_mean.CMIP5.csv");
            var vicT = Path.Combine(inputs, "CRBIA_Deschutes_Tavg_SpatialStat_mean.CMIP5.csv");
            var proj = Path.Combine(inputs, "Projections.CMIP5.txt");
            var forc = Path.Combine(inputs, "deschutes.1980-2013.small.data");

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
            processor.findEnsembles(Utils.createStandardEnsembles(0.20f, 0.50f, 0.80f, 10));
            processor.generateDeltas();

            var output = new OutputData(processor, outputs);
            output.writeHybridDeltaEnsemble();
            output.writeProjectionSummaries();
            output.adjustForcingFile(forc, ForcingFormat.GSFLOW, false);

            //generate expected md5s
            //using (var sw = new StreamWriter(Path.Combine(outputs_expected, "md5s.txt"))) {
            //    foreach (var f in Directory.GetFiles(outputs_expected)) {
            //        var fname = Path.GetFileName(f);
            //        if (fname != "md5s.txt")
            //            sw.WriteLine(fname + "," + Utils.getMD5Hash(f));
            //    }
            //}

            //compare md5s
            string md5s = Path.Combine(outputs_expected, "md5s.txt");
            using (StreamReader sr = new StreamReader(md5s)) {
                string line;
                while (!sr.EndOfStream) {
                    line = sr.ReadLine();
                    string[] info = line.Split(',');
                    string fname = info[0];
                    string md5_prev = info[1];
                    string md5_new = Utils.getMD5Hash(Path.Combine(outputs, fname));
                    Assert.AreEqual(md5_prev, md5_new);
                }
            }
        }

    }
}
