using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZedGraph;

namespace ClimateAnalysis {
	public partial class ScatterGraph : Form {
		ProcessData processor;
		PointPairList[] pointList = null;
		PointPairList currentList = new PointPairList();
		ToolTip tt;
		PointF lastPoint;
		String[] modelNames = null;
		ZedGraph.LineItem circle;
		DateTime lastUpdate = DateTime.Now;
		Color[] colors = new Color[] { Color.Orange, Color.LightGreen, Color.Red, Color.Blue, Color.Magenta, Color.Yellow, Color.DarkGray, Color.PeachPuff, Color.LightCyan};
		SymbolType[] symbols = new SymbolType[] { SymbolType.Diamond, SymbolType.TriangleDown, SymbolType.Star, SymbolType.Triangle, 
			SymbolType.Square, SymbolType.Plus, SymbolType.XCross, SymbolType.Circle };
		int colorIndex = 0;
		int symbolIndex = 0;
		ZedGraph.LineItem allPointsCurve;
		Dates datesForm;
		int periodToShow;
		List<ProcessData.DateRange> dates;

		public ScatterGraph(ProcessData proc, Dates dates) {
			InitializeComponent();
			processor = proc;
			datesForm = dates;
			zedGraphControl1.MouseMoveEvent += new ZedGraphControl.ZedMouseEventHandler(MouseMove1);
			tt = new ToolTip();
		}

		public void drawGraph(ZedGraphControl zgc, int period) {
			if (zgc == null)
				zgc = zedGraphControl1;
			periodToShow = period;
			colorIndex = 0;
			symbolIndex = 0;
			RectangleF rect = new RectangleF(0, 0, zgc.Size.Width, zgc.Size.Height);
			String xAxisLabel = "Precipitation Change, %";
			String yAxisLabel = "Temperature Change, degC";
			dates = datesForm.getDates();
			String title = "Changes in Mean Annual Temp & Precip\nComparing " + dates[0].ToString() + " to " + dates[periodToShow + 1].ToString();
			zgc.GraphPane = new GraphPane(rect, title, xAxisLabel, yAxisLabel);
			zgc.GraphPane.Legend.Position = LegendPos.InsideBotRight;
			zgc.GraphPane.Legend.FontSpec.Size = 8f;

			// add ensemble points
			ProcessData.Ensemble[] ensembles = processor.getEnsembles();

			for (int i = 0; i < ensembles.Length; i++) {
				ProcessData.Ensemble ensemble = ensembles[i];
				ZedGraph.LineItem curve = zgc.GraphPane.AddCurve(ensemble.ensembleName, ensemble.getEnsemblePoints(periodToShow), getColor(), getSymbol());
				curve.Line.IsVisible = false;
				curve.Symbol.Border.IsVisible = true;
				curve.Symbol.Fill.IsVisible = false;
				curve.Symbol.Size = 6;
			}
			
			// add all points
			pointList = processor.getPoints();
			allPointsCurve = zgc.GraphPane.AddCurve(pointList[periodToShow].Count + " GCMs", pointList[periodToShow], Color.Black, SymbolType.Circle);
			allPointsCurve.Line.IsVisible = false;
			allPointsCurve.Symbol.Border.IsVisible = false;
			allPointsCurve.Symbol.Fill.IsVisible = true;
			allPointsCurve.Symbol.Size = 2;
			allPointsCurve.Symbol.Fill.Type = FillType.Solid;

			//change axes to get new mins and maxs for drawing lines
			zgc.AxisChange();

			for (int i = 0; i < ensembles.Length; i++) {
				ProcessData.Ensemble ensemble = ensembles[i];
				if (ensemble.statistical) {
					//add precip line
					LineObj line1 = new LineObj(Color.Black, ensemble.center[periodToShow].X, zgc.GraphPane.YAxis.Scale.Min, ensemble.center[periodToShow].X, zgc.GraphPane.YAxis.Scale.Max);
					line1.Line.Width = 1f;
					zgc.GraphPane.GraphObjList.Add(line1);

					//add temp line
					LineObj line2 = new LineObj(Color.Black, zgc.GraphPane.XAxis.Scale.Min, ensemble.center[periodToShow].Y, zgc.GraphPane.XAxis.Scale.Max, ensemble.center[periodToShow].Y);
					line2.Line.Width = 1f;
					zgc.GraphPane.GraphObjList.Add(line2);
				}
			}

			modelNames = processor.getModelNames();

			zgc.Refresh();
		}

		public void saveGraph(String outputFolderName) {
			dates = datesForm.getDates();

			for (int period = 0; period < dates.Count - 1; period++) {
				drawGraph(zedGraphControl1, period);
				zedGraphControl1.GraphPane.GetImage().Save(outputFolderName + "\\" + dates[period + 1].ToStringWithUnderscores() + "_scattergraph.png");
			}
		}

		public bool findClosestPoint(ZedGraphControl zgc, PointF mouseLocation, out int column) {
			CurveItem curveItem;
			return zgc.GraphPane.FindNearestPoint(mouseLocation, allPointsCurve, out curveItem, out column);
		}

		public bool MouseMove1(object sender, MouseEventArgs e) {
			if (pointList == null || modelNames == null)
				return false;

			//only update every quarter of a second
			DateTime now = DateTime.Now;
			if ((now - lastUpdate).Milliseconds < 250)
				return false;

			ZedGraphControl zgc = (ZedGraphControl) sender;

			PointF currentPoint = (PointF)e.Location;
			CurveItem curveItem;
			int closestPoint;
			if (zgc.GraphPane.FindNearestPoint(currentPoint, allPointsCurve, out curveItem, out closestPoint)) {
				//add tooltip showing the model name
				tt.Show(modelNames[closestPoint], zgc.ParentForm, new Point((int)e.X + 15, (int)e.Y - 15), 2000);

				//put a circle around the current point
				currentList.Clear();
				currentList.Add(new PointPair(pointList[periodToShow][closestPoint]));
				circle = zgc.GraphPane.AddCurve("", currentList, Color.Black, SymbolType.Circle);
				circle.Line.IsVisible = false;
				circle.Symbol.Border.IsVisible = true;
				circle.Symbol.Fill.IsVisible = false;
				circle.Symbol.Size = 8;
				zgc.Refresh();

				lastPoint = e.Location;
				lastUpdate = DateTime.Now;
			}
			else {
				currentList.Clear();
				circle = zgc.GraphPane.AddCurve("", currentList, Color.Black, SymbolType.Circle);
				zgc.Refresh();
			}

			return false;
		}

		public void fillInDates() {
			//fill future date range combo box
			comboBox1.Items.Clear();
			dates = datesForm.getDates();
			for (int i = 1; i < dates.Count; i++)
				comboBox1.Items.Add(dates[i].ToString());
			comboBox1.SelectedIndex = periodToShow;
		}

		//change of future period
		private void comboBox1_SelectedIndexChanged(object sender, EventArgs e) {
			drawGraph(zedGraphControl1, comboBox1.SelectedIndex);
		}

		private Color getColor() {
			if (colorIndex == colors.Length)
				colorIndex = 0;
			return colors[colorIndex++];
		}

		private SymbolType getSymbol() {
			if (symbolIndex == symbols.Length)
				symbolIndex = 0;
			return symbols[symbolIndex++];
		}

		//stop the red x close button from disposing form
		protected override void OnFormClosing(FormClosingEventArgs e) {
			this.Hide();
			e.Cancel = true;
		}

		//close button
		private void button1_Click(object sender, EventArgs e) {
			this.Hide();
		}
	}
}
