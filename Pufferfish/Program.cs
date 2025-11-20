using System;
using System.Windows.Forms;
using log4net.Config;

namespace PufferFish;

internal static class Program
{
	[STAThread]
	private static void Main()
	{
		XmlConfigurator.Configure();
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(defaultValue: false);
		Application.Run(new MainForm());
	}
}
