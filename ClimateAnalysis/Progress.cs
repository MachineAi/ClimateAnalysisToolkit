using System;
using System.Timers;
using System.Windows.Forms;
using DotSpatial.Data;

namespace ClimateAnalysis {
	public partial class Progress : Form {
		private System.Timers.Timer timer;
		private string[] messages = new string[] {
		"USGS Geo Data Portal: Waiting for server to process request.",
		"USGS Geo Data Portal: Waiting for server to process request..",
		"USGS Geo Data Portal: Waiting for server to process request...",
		"USGS Geo Data Portal: Done"};
		private int i = 0;
		private IProgressHandler pHandler;

		public Progress(IProgressHandler p) {
			InitializeComponent();
			pHandler = p;
			timer = new System.Timers.Timer(1000);
			timer.Elapsed += new ElapsedEventHandler(timerElapsed);
		}

		public void start() {
			timer.Enabled = true;
			changeText(messages[i]);
		}

		public void stop() {
			timer.Enabled = false;
			changeText(messages[3]);
		}

		private void timerElapsed(object sender, ElapsedEventArgs e) {
			i++;
			if (i >= 3)
				i = 0;
			changeText(messages[i]);
		}

		delegate void SetTextCallback(string text);

		private void changeText(string text) {
			/*if (this.pHandler.Progress.InvokeRequired) {
				SetTextCallback d = new SetTextCallback(changeText);
				this.Invoke(d, new object[] { text });
			}
			else {*/
				pHandler.Progress("", 0, text);
			//}
		}

		//close button
		private void button1_Click(object sender, EventArgs e) {
			this.Hide();
		}

		//stop the red x close button from disposing form
		protected override void OnFormClosing(FormClosingEventArgs e) {
			this.Hide();
			e.Cancel = true;
		}
	}
}
