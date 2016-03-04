using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace ClimateAnalysis.Tests
{
    public static class Utils {

        public static string getMD5Hash(string filename) {
            var rval = new StringBuilder();
            using (var md5 = MD5.Create()) {
                var lines = File.ReadAllLines(filename);
                var bytes = new List<byte[]>();
                for (int i = 0; i < lines.Length; i++) {
                    bytes.Add(Encoding.ASCII.GetBytes(lines[i]));
                }
                var hash = md5.ComputeHash(bytes.SelectMany(i => i).ToArray());
                for (int i = 0; i < hash.Length; i++) {
                    rval.Append(hash[i].ToString("x2"));
                }
                return rval.ToString();
            }
        }

        public static ProcessData.Ensemble[] createStandardEnsembles(float low,
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
