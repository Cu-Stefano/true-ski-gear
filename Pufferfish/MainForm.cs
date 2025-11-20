using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Net.Cache;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using log4net;
using Microsoft.Win32;
using MsgBox;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;
using OxyPlot.WindowsForms;
using Pufferfish;
using PufferFish.Properties;
using RTSerialCom;
using SevenZip.SDK;
using SevenZip.SDK.Compress.LZMA;

namespace PufferFish;

public class MainForm : Form
{
	public enum SIGDN : uint
	{
		NORMALDISPLAY = 0u,
		PARENTRELATIVEPARSING = 2147581953u,
		DESKTOPABSOLUTEPARSING = 2147647488u,
		PARENTRELATIVEEDITING = 2147684353u,
		DESKTOPABSOLUTEEDITING = 2147794944u,
		FILESYSPATH = 2147844096u,
		URL = 2147909632u,
		PARENTRELATIVEFORADDRESSBAR = 2147991553u,
		PARENTRELATIVE = 2148007937u
	}

	private static readonly ILog log = LogManager.GetLogger("PufferFish");

	private static int gyro_index = 7;

	private static int speed_index = 8;

	private readonly ushort LAST_FIRMWARE = 50949;

	private int badPackageFound = 0;

	private ulong bytesRead;

	private uint currentSessionID = uint.MaxValue;

	private int deviceID;

	private uint firmwareVersion = 0u;

	private SQLiteConnection historyDBConn;

	private bool justconnected;

	private DateTime? lastTime = null;

	private GMarkerGoogle marker;

	private GMapOverlay polyOverlay;

	private bool readingSession;

	private MyGMapRoute route;

	private List<LineSeries[]> series = new List<LineSeries[]>();

	private uint sess_count;

	private DateTime sesseionRequestTime;

	private Hashtable sessions = new Hashtable();

	private bool stopRequested;

	private List<LineAnnotation> verticalAnnotations = new List<LineAnnotation>();

	private ArrayList sessionFallPoints = new ArrayList();

	private WaitFormDelete waitForm = new WaitFormDelete();

	private WaitForm waitFormLoad = new WaitForm();

	private SerialClient seriale = new SerialClient("COM1");

	private int falls_index = -1;

	private ArrayList critical_falls = new ArrayList();

	private bool pufferfishv2 = false;

	private string pufferfishv2_firmware_path = null;

	private ByteQueue serial_buffer = new ByteQueue();

	private const string keyBase = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths";

	public const string SHELL = "shell32.dll";

	private int autoLoadIndex = -1;

	private List<uint> autoSessionsID;

	private IEnumerator scanSessionsEnum;

	private bool scanninSessions;

	private IContainer components = null;

	private Panel panel1;

	private Label label2;

	private ProgressBar progressBarDownload;

	private ListView tagsListView;

	private ColumnHeader columnHeader1;

	private ColumnHeader columnHeader2;

	private ColumnHeader columnHeader3;

	private MenuStrip menuStrip1;

	private TrackBar trackTimer;

	private GroupBox panelTags;

	private DateTimePicker newEventTime;

	private ContextMenuStrip listContextMenu;

	private ToolStripMenuItem eliminaToolStripMenuItem;

	private ToolStripMenuItem modificaToolStripMenuItem;

	private Button buttonInsertTag;

	private Label label4;

	private GMapControl gMapControl;

	private Button buttonTerminateEditing;

	private ComboBox comboPorts;

	private Button btnConnect;

	private ToolStripMenuItem azioniToolStripMenuItem;

	private ToolStripMenuItem richiediStatoToolStripMenuItem;

	private ToolStripMenuItem richiediListaSessioniToolStripMenuItem;

	private ToolStripMenuItem richiediSessioneToolStripMenuItem;

	private ToolStripMenuItem richiediStopToolStripMenuItem;

	private Button btnDisconnect;

	private StatusStrip statusStrip;

	private ToolStripStatusLabel toolStripStatusLabel;

	private ToolStripStatusLabel toolStripStatusLabelSpeed;

	private ToolStripMenuItem cancellaMemoriaToolStripMenuItem;

	private ToolStripStatusLabel toolStripVersionStatus;

	private ToolStripStatusLabel toolStripCurrentSession;

	private ToolStripStatusLabel toolStripStatusLabel2;

	private ToolStripStatusLabel toolStripTotSession;

	private ToolStripStatusLabel toolStripMemory;

	private ToolStripStatusLabel toolStripStatusUpload;

	private ToolStripProgressBar toolStripProgressBarUpload;

	private Button buttonSaveToFile;

	private ToolStripSeparator toolStripMenuItem1;

	private ToolStripMenuItem avanzatoToolStripMenuItem;

	private ToolStripMenuItem aggiornaFirmwareToolStripMenuItem;

	private Button updatePortsButton;

	private Button button2;

	private Button button3;

	private Button stopReadingButton;

	private PictureBox pictureBox1;

	private ToolStripMenuItem fileToolStripMenuItem;

	private ToolStripMenuItem caricaSessioneDaFileToolStripMenuItem;

	private BackgroundWorker backgroundWorker1;

	private ToolStripMenuItem installaFirmwareDiTestToolStripMenuItem;

	private ToolStripMenuItem richiediTutteLeSessioniToolStripMenuItem;

	private ToolStripMenuItem resetModuloToolStripMenuItem;

	private ToolStripSeparator toolStripMenuItem2;

	private ToolStripMenuItem mostraCartellaFileLocaliToolStripMenuItem;

	private ToolStripMenuItem eliminaFileLocaliToolStripMenuItem1;

	private Button buttonContatto;

	private Button buttonScivolamento;

	private TextBox eventNote;

	private ToolStripMenuItem verificaStatoSensoriToolStripMenuItem;

	private ToolStripSeparator toolStripMenuItem3;

	private ToolStripMenuItem toolStripMenuItem4;

	private ToolStripMenuItem toolStripMenuItem5;

	private Button PrevActivationButton;

	private Button NextActivationButton;

	private TableLayoutPanel tableLayoutPanel1;

	private TableLayoutPanel tableLayoutPanel3;

	private ToolStripStatusLabel toolStripCriticalFall;

	private ToolStripMenuItem esportaGraficoSuFileToolStripMenuItem;

	private TableLayoutPanel tableLayoutPanel2;

	private Label trackLabelCurrent;

	private Label trackLabelEnd;

	private Label trackLabelStart;

	private PlotView plotView;

	private TableLayoutPanel tableLayoutPanel4;

	private TableLayoutPanel tableLayoutPanel5;

	private TableLayoutPanel tableLayoutPanel6;

	private TableLayoutPanel tableLayoutPanel9;

	private TableLayoutPanel tableLayoutPanel7;

	private TableLayoutPanel tableLayoutPanel8;

	private TableLayoutPanel tableLayoutPanel10;

	private TableLayoutPanel tableLayoutPanel11;

	private ToolStripMenuItem toolStripMenuItem6;

	private ToolStripMenuItem enterShippingModeToolStripMenuItem;

	private ToolStripMenuItem magnetometerCalibrationToolStripMenuItem;

	public MainForm()
	{
		InitializeComponent();
		readingSession = false;
		base.WindowState = FormWindowState.Maximized;
		seriale.OnReceiving += Seriale_OnReceiving;
	}

	private void Seriale_OnReceiving(object sender, DataStreamEventArgs e)
	{
		if (stopRequested)
		{
			seriale.DiscardInBuffer();
			return;
		}
		bool forcedStop = false;
		byte[] package = null;
		serial_buffer.Enqueue(e.Response, 0, e.Response.Length);
		while (Thereisenoughdata() && !stopRequested)
		{
			if (PackageSeemsGood())
			{
				package = GetPackage();
				bytesRead += (uint)package.Length;
				badPackageFound = 3;
				ParsePackage(package);
				continue;
			}
			if (scanninSessions)
			{
				justconnected = false;
				SendStopRequest(isdefinitive: false);
				ScanNextSession();
				return;
			}
			if (badPackageFound < 3)
			{
				stopRequested = true;
				SendStopRequest(isdefinitive: false);
				SendSessionRequest(currentSessionID, bytesRead, tryingtorecover: true);
				badPackageFound++;
				return;
			}
			byte header_type = serial_buffer.At(0);
			byte header_len = serial_buffer.At(1);
			log.WarnFormat("Unexpected message of type 0x{0:X} and size 0x{1:x} while reading a session with base address {2} - 0x{3:X} bytes read ({4} tries)", header_type, header_len, (getCurrentBaseSession() as Session).sessionAddress.ToString(), bytesRead, badPackageFound);
			log.Warn("I'll look for a good packet");
			bytesRead++;
			serial_buffer.Dequeue(0, 1);
			bool foundgoodpack = false;
			while (Thereisenoughdata())
			{
				if (!SeekGoodMessageInBuffer())
				{
					bytesRead++;
					serial_buffer.Dequeue(0, 1);
					continue;
				}
				log.Warn("Good package found!");
				badPackageFound = 0;
				foundgoodpack = true;
				break;
			}
			if (!foundgoodpack)
			{
				bytesRead += 64000uL;
				SendStopRequest(isdefinitive: false);
				SendSessionRequest(currentSessionID, bytesRead, tryingtorecover: true);
			}
		}
		if (forcedStop)
		{
			SendSessionRequest(currentSessionID, bytesRead, tryingtorecover: true);
		}
	}

	public static bool CompressFileLZMA(string inFile, string outFile)
	{
		try
		{
			int dictionary = 8388608;
			int posStateBits = 2;
			int litContextBits = 3;
			int litPosBits = 0;
			int algorithm = 2;
			int numFastBytes = 128;
			string mf = "bt4";
			bool eos = true;
			bool stdInMode = false;
			CoderPropID[] propIDs = new CoderPropID[8]
			{
				CoderPropID.DictionarySize,
				CoderPropID.PosStateBits,
				CoderPropID.LitContextBits,
				CoderPropID.LitPosBits,
				CoderPropID.Algorithm,
				CoderPropID.NumFastBytes,
				CoderPropID.MatchFinder,
				CoderPropID.EndMarker
			};
			object[] properties = new object[8] { dictionary, posStateBits, litContextBits, litPosBits, algorithm, numFastBytes, mf, eos };
			using (FileStream inStream = new FileStream(inFile, FileMode.Open))
			{
				using FileStream outStream = new FileStream(outFile, FileMode.Create);
				SevenZip.SDK.Compress.LZMA.Encoder encoder = new SevenZip.SDK.Compress.LZMA.Encoder();
				encoder.SetCoderProperties(propIDs, properties);
				encoder.WriteCoderProperties(outStream);
				long fileSize = ((!(eos || stdInMode)) ? inStream.Length : (-1));
				for (int i = 0; i < 8; i++)
				{
					outStream.WriteByte((byte)(fileSize >> 8 * i));
				}
				encoder.Code(inStream, outStream, -1L, -1L, null);
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static int SplitFile(string inputFile, int chunkSize, string path)
	{
		byte[] buffer = new byte[20480];
		using Stream input = File.OpenRead(inputFile);
		int index = 0;
		while (input.Position < input.Length)
		{
			using (Stream output = File.Create(path + "." + index))
			{
				int remaining = chunkSize;
				int bytesRead;
				while (remaining > 0 && (bytesRead = input.Read(buffer, 0, Math.Min(remaining, 20480))) > 0)
				{
					output.Write(buffer, 0, bytesRead);
					remaining -= bytesRead;
				}
			}
			index++;
		}
		return index;
	}

	protected string GetFileMD5(string fileName)
	{
		try
		{
			FileStream file = new FileStream(fileName, FileMode.Open);
			MD5 md5 = new MD5CryptoServiceProvider();
			byte[] retVal = md5.ComputeHash(file);
			file.Close();
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < retVal.Length; i++)
			{
				sb.Append(retVal[i].ToString("x2"));
			}
			return sb.ToString();
		}
		catch (Exception)
		{
			return "???";
		}
	}

	private static List<string> ComPortNames(string VID, string PID)
	{
		string pattern = $"^VID_{VID}.PID_{PID}";
		Regex _rx = new Regex(pattern, RegexOptions.IgnoreCase);
		List<string> comports = new List<string>();
		try
		{
			RegistryKey rk1 = Registry.LocalMachine;
			RegistryKey rk2 = rk1.OpenSubKey("SYSTEM\\CurrentControlSet\\Enum");
			string[] subKeyNames = rk2.GetSubKeyNames();
			foreach (string s3 in subKeyNames)
			{
				RegistryKey rk3 = rk2.OpenSubKey(s3);
				string[] subKeyNames2 = rk3.GetSubKeyNames();
				foreach (string s4 in subKeyNames2)
				{
					if (!_rx.Match(s4).Success)
					{
						continue;
					}
					RegistryKey rk4 = rk3.OpenSubKey(s4);
					string[] subKeyNames3 = rk4.GetSubKeyNames();
					foreach (string s5 in subKeyNames3)
					{
						RegistryKey rk5 = rk4.OpenSubKey(s5);
						string location = (string)rk5.GetValue("LocationInformation");
						RegistryKey rk6 = rk5.OpenSubKey("Device Parameters");
						string portName = (string)rk6.GetValue("PortName");
						if (!string.IsNullOrEmpty(portName) && Array.Exists(SerialPort.GetPortNames(), (string port) => port.Equals(portName)))
						{
							comports.Add((string)rk6.GetValue("PortName"));
						}
					}
				}
			}
		}
		catch
		{
		}
		return comports;
	}

	private static double GpsEncodingToDegrees(string gpsencoding, bool isPositive)
	{
		double a = float.Parse(gpsencoding, CultureInfo.InvariantCulture);
		double d = (int)a / 100;
		a -= d * 100.0;
		if (isPositive)
		{
			return d + a / 60.0;
		}
		return 0.0 - (d + a / 60.0);
	}

	private void addPointToRoute(PointLatLng point)
	{
		BeginInvoke((MethodInvoker)delegate
		{
			if (polyOverlay.Routes.Count == 0)
			{
				route = new MyGMapRoute("session");
				route.Stroke.Width = 2f;
				polyOverlay.Routes.Add(route);
				GPSData lastGPSdata = getCurrentBaseSession().getLastGPSdata();
				if (lastGPSdata != null)
				{
					gMapControl.Position = lastGPSdata.coords;
				}
			}
			polyOverlay.Routes[0].Points.Add(point);
			gMapControl.Refresh();
			if (getCurrentBaseSession().getGPSDataCount == 1)
			{
				trackLabelStart.Text = getStartTime().ToLongTimeString() + " (" + getStartTime().ToShortDateString() + ")";
			}
			trackLabelEnd.Text = getEndTimeLabel();
		});
	}

	private void firmwareUpdateProcessEnd(object sender, EventArgs e)
	{
		BeginInvoke((MethodInvoker)delegate
		{
			label2.Text = "Download from the device";
			progressBarDownload.Style = ProgressBarStyle.Blocks;
			if (pufferfishv2_firmware_path != null)
			{
				try
				{
					File.Delete(pufferfishv2_firmware_path);
					pufferfishv2_firmware_path = null;
				}
				catch
				{
				}
			}
			aggiornaPorte();
		});
	}

	private string GetPathForExe(string fileName)
	{
		RegistryKey localMachine = Registry.LocalMachine;
		RegistryKey fileKey = localMachine.OpenSubKey(string.Format("{0}\\{1}", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths", fileName));
		object result = null;
		if (fileKey != null)
		{
			result = fileKey.GetValue("Path");
			fileKey.Close();
		}
		return (string)result;
	}

	private void aggiornaFirmware(string pufferfishv2_firmware_path)
	{
		string pathToSTM32CubeProgrammer = GetPathForExe("STM32CubeProgrammer.exe");
		if (pathToSTM32CubeProgrammer != null)
		{
			STM32CubeProgrammerCLI prog = new STM32CubeProgrammerCLI(pathToSTM32CubeProgrammer);
			UpdateStatusBar("Updating firmware...");
			if (seriale.IsOpen())
			{
				SendEnterBootloader();
				doEarlyDisconnect();
			}
			label2.Text = "Firmware update: Please Wait...";
			progressBarDownload.Style = ProgressBarStyle.Marquee;
			toolStripVersionStatus.Text = "";
			firmwareVersion = 0u;
			Refresh();
			Thread.Sleep(1000);
			if (!prog.ProgramApp(pufferfishv2_firmware_path, null, firmwareUpdateProcessEnd))
			{
				MessageBox.Show("Cannot complete firmware update. The update file is wrong. Please unplug the board to terminate the procedure.");
			}
		}
		else
		{
			MessageBox.Show("Cannot find STM32CubeProgrammer");
		}
	}

	private void aggiornaFirmwareToolStripMenuItem_Click(object sender, EventArgs e)
	{
		if (MessageBox.Show("Are you sure you want to update to the latest firmware?\r\nThis operation is dangerous and you should contact support before proceeding", "Device firmware update", MessageBoxButtons.YesNo) != DialogResult.Yes)
		{
			return;
		}
		if (!pufferfishv2)
		{
			UpdateFirmware(2, null);
			return;
		}
		byte[] embedded_fwr = null;
		pufferfishv2_firmware_path = null;
		string pathToSTM32CubeProgrammer = GetPathForExe("STM32CubeProgrammer.exe");
		if (pathToSTM32CubeProgrammer != null)
		{
			if (seriale.IsOpen())
			{
				SendEnterBootloader();
				doEarlyDisconnect();
			}
			STM32CubeProgrammerCLI prog = new STM32CubeProgrammerCLI(pathToSTM32CubeProgrammer);
			bool isRetMicro = false;
			int maxAttempts = 3;
			for (int attempt = 0; attempt < maxAttempts; attempt++)
			{
				try
				{
					isRetMicro = prog.IsRETMicrocontroller().GetAwaiter().GetResult();
				}
				catch
				{
					if (attempt == maxAttempts - 1)
					{
						MessageBox.Show("Cannot complete firmware update. Please unplug the board to terminate the procedure and retry.");
						return;
					}
					continue;
				}
				break;
			}
			if (isRetMicro)
			{
				embedded_fwr = Resources.PufferfishV2_0_1_54_A_RET;
				pufferfishv2_firmware_path = Path.GetTempFileName();
				File.Move(pufferfishv2_firmware_path, Path.ChangeExtension(pufferfishv2_firmware_path, ".RET.bin"));
				pufferfishv2_firmware_path = Path.ChangeExtension(pufferfishv2_firmware_path, ".RET.bin");
			}
			else
			{
				embedded_fwr = Resources.PufferfishV2_0_1_54_A;
				pufferfishv2_firmware_path = Path.GetTempFileName();
				File.Move(pufferfishv2_firmware_path, Path.ChangeExtension(pufferfishv2_firmware_path, ".bin"));
				pufferfishv2_firmware_path = Path.ChangeExtension(pufferfishv2_firmware_path, ".bin");
			}
		}
		if (embedded_fwr == null)
		{
			return;
		}
		using Stream input = new MemoryStream(embedded_fwr);
		using Stream output = File.Create(pufferfishv2_firmware_path);
		input.CopyTo(output);
		output.Close();
		aggiornaFirmware(pufferfishv2_firmware_path);
	}

	private void P_OutputDataReceived(object sender, DataReceivedEventArgs e)
	{
	}

	private void aggiornaPorte()
	{
		string[] names = SerialPort.GetPortNames();
		comboPorts.Text = "";
		comboPorts.Items.Clear();
		List<string> res = ComPortNames("04B4", "F232");
		res.AddRange(ComPortNames("0483", "5740"));
		foreach (string name in res)
		{
			comboPorts.Items.Add(name);
		}
		if (comboPorts.Items.Count != 0)
		{
			comboPorts.Text = comboPorts.Items[0].ToString();
		}
	}

	private void AskForSessionToRequest()
	{
		if (pufferfishv2)
		{
			return;
		}
		if (sessions.Count == 0)
		{
			MessageBox.Show("No session found on the device");
			return;
		}
		InputBox.SetLanguage(InputBox.Language.Italian);
		string[] sessionsName = new string[sessions.Count];
		List<uint> sessionsID = new List<uint>();
		foreach (uint s in sessions.Keys)
		{
			sessionsID.Add(s);
		}
		sessionsID.Sort();
		sessionsID.Reverse();
		byte i = 0;
		foreach (uint s2 in sessionsID)
		{
			_ = ((Session)sessions[s2]).startDate;
			if (((Session)sessions[s2]).startDate == DateTime.MinValue)
			{
				sessionsName[i++] = $"{s2} ({((Session)sessions[s2]).sessionSize.getSize() / 1000} kB - about {((Session)sessions[s2]).sessionSize.getSize() / 50084 / 60} mins)";
				continue;
			}
			sessionsName[i++] = $"{s2} ({((Session)sessions[s2]).sessionSize.getSize() / 1000} kB - about {((Session)sessions[s2]).sessionSize.getSize() / 50084 / 60} mins - {((Session)sessions[s2]).startDate.ToShortDateString()})";
		}
		if (InputBox.ShowDialog("Select a session:", "PufferFish", InputBox.Icon.Nothing, InputBox.Buttons.OkCancel, InputBox.Type.ComboBox, sessionsName) == DialogResult.OK)
		{
			string r = InputBox.ResultValue.Remove(InputBox.ResultValue.IndexOf(' '));
			uint sessionID = uint.Parse(r);
			SendSessionRequest(sessionID, 0uL, tryingtorecover: false);
		}
	}

	private void ReadSessionV2ProgressReport(int perc, long bps, long eta)
	{
		BeginInvoke((MethodInvoker)delegate
		{
			progressBarDownload.Value = perc * 10;
			UpdateStatusBarSpeed("{0:n0} kB/s - {1:00}:{2:00} mins before completing", bps, eta / 60, eta % 60);
		});
	}

	private void AskForSessionToRequestV2()
	{
		if (!pufferfishv2)
		{
			return;
		}
		if (sessions.Count == 0)
		{
			MessageBox.Show("No session found on the device");
			return;
		}
		InputBox.SetLanguage(InputBox.Language.Italian);
		string[] sessionsName = new string[sessions.Count];
		List<uint> sessionsID = new List<uint>();
		foreach (uint s in sessions.Keys)
		{
			sessionsID.Add(s);
		}
		sessionsID.Sort();
		sessionsID.Reverse();
		byte i = 0;
		foreach (uint s2 in sessionsID)
		{
			string[] sizes = new string[5] { "B", "KB", "MB", "GB", "TB" };
			double len = 0.0;
			DateTime? date = null;
			if (!(sessions[s2].GetType() == typeof(SessionV2Tag)))
			{
				len = ((!(sessions[s2].GetType() == typeof(SessionV2))) ? 0.0 : ((double)new FileInfo(((SessionV2)sessions[s2]).SessionFileName).Length));
			}
			else
			{
				len = new FileInfo(((SessionV2Tag)sessions[s2]).fileName).Length;
				date = ((SessionV2Tag)sessions[s2]).startDate;
			}
			int order = 0;
			while (len >= 1024.0 && order < sizes.Length - 1)
			{
				order++;
				len /= 1024.0;
			}
			if (date.HasValue)
			{
				sessionsName[i++] = string.Format("{3}/{4}/{5} {0} ({1:0.##}{2})", s2, len, sizes[order], date.Value.Day, date.Value.Month, date.Value.Year);
			}
			else
			{
				sessionsName[i++] = $"- {s2} {len:0.##}{sizes[order]}";
			}
		}
		if (InputBox.ShowDialog("Select a session:", "PufferFish", InputBox.Icon.Nothing, InputBox.Buttons.OkCancel, InputBox.Type.ComboBox, sessionsName) != DialogResult.OK)
		{
			return;
		}
		currentSessionID = uint.Parse(InputBox.ResultValue.Split(' ')[1]);
		try
		{
			backgroundWorker1.RunWorkerAsync(((SessionV2Tag)sessions[currentSessionID]).fileName);
			base.Enabled = false;
			waitFormLoad.Show(this);
		}
		catch
		{
			RunWorkerCompletedEventArgs e = new RunWorkerCompletedEventArgs(sessions[currentSessionID], null, cancelled: false);
			loadFile_Completed(null, e);
		}
	}

	private void StartScanSessions()
	{
		scanSessionsEnum = sessions.Keys.GetEnumerator();
		scanninSessions = true;
		ScanNextSession();
	}

	private void ScanNextSession()
	{
		if (!scanSessionsEnum.MoveNext())
		{
			EndScanSessions();
		}
		else if (((Session)sessions[(uint)scanSessionsEnum.Current]).sessionSize.getSize() > 100)
		{
			log.WarnFormat("Autoreading session at index {0}, id {1}", autoLoadIndex, (uint)scanSessionsEnum.Current);
			SendSessionRequest((uint)scanSessionsEnum.Current, 0uL, tryingtorecover: false);
		}
		else
		{
			log.WarnFormat("Skipping session at index {0}, id {1}", autoLoadIndex, (uint)scanSessionsEnum.Current);
			ScanNextSession();
		}
	}

	private void EndScanSessions()
	{
		scanSessionsEnum = null;
		scanninSessions = false;
		AskForSessionToRequest();
	}

	private void BtnConnect_Click(object sender, EventArgs e)
	{
		if (comboPorts.Text.Length == 0)
		{
			MessageBox.Show("Choose a port", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			return;
		}
		try
		{
			SendStopRequest();
			seriale.Dispose();
			seriale.OpenConn(comboPorts.Text, Convert.ToInt32(ConfigurationManager.AppSettings["DefaultSpeed"]));
			seriale.DiscardInBuffer();
			setConnectedButtons(connected: true);
			btnConnect.Enabled = false;
			justconnected = true;
			SendSendStatusRequest();
		}
		catch (Exception)
		{
			MessageBox.Show("Couldn't open the specified port", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private void btnDisconnect_Click(object sender, EventArgs e)
	{
		doDisconnect();
		aggiornaPorte();
		toolStripVersionStatus.Text = "";
		firmwareVersion = 0u;
	}

	private void button1_Click(object sender, EventArgs e)
	{
		disconnect();
		aggiornaPorte();
	}

	private void CheckInternetConnectivity(object state)
	{
		if (NetworkInterface.GetIsNetworkAvailable())
		{
			using (WebClient webClient = new WebClient())
			{
				webClient.CachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);
				webClient.Proxy = null;
				webClient.OpenReadCompleted += WebClient_OpenReadCompleted;
				webClient.OpenReadAsync(new Uri("http://www.google.com"));
			}
		}
	}

	private void closeDB()
	{
		try
		{
			if (SessionIsValid())
			{
				getCurrentBaseSession().CloseDB();
			}
			SQLiteConnection.ClearAllPools();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		catch (Exception)
		{
		}
	}

	private string ComPortName(string v1, string v2)
	{
		List<string> res = ComPortNames(v1, v2);
		if (res.Count > 0)
		{
			return res[res.Count - 1];
		}
		return "";
	}

	private void disconnect()
	{
		try
		{
			if (SessionIsValid())
			{
				getCurrentBaseSession().commit();
			}
		}
		catch
		{
		}
		try
		{
			if (seriale.IsOpen())
			{
				SendStopRequest();
				seriale.Dispose();
			}
		}
		catch
		{
		}
	}

	private void doEarlyDisconnect()
	{
		try
		{
			if (seriale.IsOpen())
			{
				seriale.Dispose();
			}
			UpdateStatusBar("Disconnected port");
			UpdateStatusBarSpeed("");
		}
		catch (Exception)
		{
			UpdateStatusBarSpeed("");
		}
		setConnectedButtons(connected: false);
	}

	private void doDisconnect()
	{
		try
		{
			disconnect();
			UpdateStatusBar("Disconnected port");
			UpdateStatusBarSpeed("");
		}
		catch (Exception)
		{
			UpdateStatusBarSpeed("");
		}
		setConnectedButtons(connected: false);
	}

	private void setConnectedButtons(bool connected)
	{
		BeginInvoke((MethodInvoker)delegate
		{
			btnConnect.Enabled = !connected;
			btnDisconnect.Enabled = connected;
			updatePortsButton.Enabled = !connected;
			comboPorts.Enabled = !connected;
			azioniToolStripMenuItem.Enabled = connected;
			enterShippingModeToolStripMenuItem.Enabled = connected;
			magnetometerCalibrationToolStripMenuItem.Enabled = connected;
			resetModuloToolStripMenuItem.Enabled = connected;
			verificaStatoSensoriToolStripMenuItem.Enabled = connected;
		});
	}

	private void endEditing_Click(object sender, EventArgs e)
	{
		panelTags.Enabled = false;
		buttonTerminateEditing.Enabled = false;
		buttonSaveToFile.Enabled = true;
		getCurrentBaseSession().commit();
	}

	private void Form1_FormClosing(object sender, FormClosingEventArgs e)
	{
		disconnect();
		closeDB();
		historyDBConn.Dispose();
	}

	private void Form1_Load(object sender, EventArgs e)
	{
		Directory.CreateDirectory(BaseSession.getDBFolder());
		gMapControl.MapProvider = GMapProviders.EmptyProvider;
		gMapControl.Position = new PointLatLng((getMaxLat() + getMinLat()) / 2.0, (getMaxLng() + getMinLng()) / 2.0);
		gMapControl.MinZoom = 10;
		gMapControl.MaxZoom = 20;
		gMapControl.Zoom = 16.0;
		gMapControl.CanDragMap = true;
		polyOverlay = new GMapOverlay("polygons");
		gMapControl.Overlays.Add(polyOverlay);
		GMapOverlay markersOverlay = new GMapOverlay("markers");
		marker = new GMarkerGoogle(new PointLatLng(0.0, 0.0), GMarkerGoogleType.blue_dot);
		markersOverlay.Markers.Add(marker);
		gMapControl.Overlays.Add(markersOverlay);
		aggiornaPorte();
		plotView.Model = Session.InitSessionPlotModel(series, verticalAnnotations, getTimeLabel);
		plotView.Model.IsLegendVisible = false;
		plotView.Model.Axes[0].AxisChanged += delegate
		{
			ReloadData();
		};
		historyDBConn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", BaseSession.getDBFolder() + "history.sqlite"));
		historyDBConn.Open();
		SQLiteCommand command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS sessions (Timestamp DATETIME DEFAULT null, deviceID INTEGER, sessionID INTEGER, status TEXT, files INTEGER, sent INTEGER, basename TEXT);", historyDBConn);
		command.ExecuteNonQuery();
		command.Dispose();
		ThreadPool.QueueUserWorkItem(CheckInternetConnectivity);
		Text = "Pufferfish Data Tool v" + typeof(MainForm).Assembly.GetName().Version.ToString();
	}

	private BaseSession getCurrentBaseSession()
	{
		if (!sessions.Contains(currentSessionID))
		{
			throw new Exception("No current session!");
		}
		try
		{
			return (BaseSession)sessions[currentSessionID];
		}
		catch
		{
			return null;
		}
	}

	private Session getCurrentSession()
	{
		if (!sessions.Contains(currentSessionID))
		{
			MessageBox.Show("The selected session id was not found on the device", "An error occurred", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return new Session(0, 0u, 0uL, new Address());
		}
		return (Session)sessions[currentSessionID];
	}

	private string getEndTimeLabel()
	{
		BaseSession currentSession = getCurrentBaseSession();
		if (currentSession.sessionVersion == 1)
		{
			if ((currentSession as Session).isCompleted() || (currentSession as Session).sessionSize.getSize() < bytesRead)
			{
				return currentSession.MaxTime.ToLongTimeString();
			}
			if (currentSession.getGPSDataCount > 0)
			{
				return currentSession.getLastGPSdata().time.AddSeconds(((currentSession as Session).sessionSize.getSize() - bytesRead) / 50084).ToLongTimeString() + " (estimated)";
			}
		}
		else if (currentSession.getGPSDataCount > 0)
		{
			return currentSession.maxTime.ToLongTimeString();
		}
		return DateTime.Now.ToLongTimeString();
	}

	private double getMaxLat()
	{
		if (!SessionIsValid())
		{
			return 0.0;
		}
		return getCurrentBaseSession().getMaxLat();
	}

	private double getMaxLng()
	{
		if (!SessionIsValid())
		{
			return 0.0;
		}
		return getCurrentBaseSession().getMaxLng();
	}

	private double getMinLat()
	{
		if (!SessionIsValid())
		{
			return 0.0;
		}
		return getCurrentBaseSession().getMinLat();
	}

	private double getMinLng()
	{
		if (!SessionIsValid())
		{
			return 0.0;
		}
		return getCurrentBaseSession().getMinLng();
	}

	private string getSendFolder()
	{
		return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Pufferfish\\tosend\\";
	}

	private string getSessionStatus(ushort deviceID, uint sessionID)
	{
		SQLiteCommand sql = new SQLiteCommand($"SELECT status FROM sessions where deviceID={deviceID} and sessionID={sessionID}", historyDBConn);
		return (string)sql.ExecuteScalar();
	}

	private DateTime getStartTime()
	{
		return getCurrentBaseSession().MinTime;
	}

	private string getTempFolder()
	{
		return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Pufferfish\\temp\\";
	}

	private string getTimeLabel(double arg)
	{
		if (SessionIsValid())
		{
			BaseSession currentBaseSession = getCurrentBaseSession();
			if (currentBaseSession != null)
			{
				_ = currentBaseSession.MinTime;
				if (true)
				{
					return getCurrentBaseSession().MinTime.AddMilliseconds(arg - (double)getCurrentBaseSession().MinIndex).ToString("hh:mm:ss");
				}
			}
		}
		return "--:--:--";
	}

	private ulong getUsedMemory()
	{
		ulong usedMemory = 0uL;
		foreach (Session s in sessions.Values)
		{
			usedMemory += s.sessionSize.getSize();
		}
		return usedMemory;
	}

	private void gMapControl_Resize(object sender, EventArgs e)
	{
		double minLat = getMinLat();
		double maxLat = getMaxLat();
		double minLng = getMinLng();
		double maxLng = getMaxLng();
	}

	private void loadFile_Completed(object sender, RunWorkerCompletedEventArgs e)
	{
		if (!e.Cancelled)
		{
			if (e.Error != null)
			{
				MessageBox.Show("Error while loading the file");
			}
			else
			{
				sessions[currentSessionID] = e.Result;
				BaseSession session = e.Result as BaseSession;
				if (session != null && session.sessionVersion == 1)
				{
					Session currentSession = getCurrentBaseSession() as Session;
					bytesRead = currentSession.sessionSize.getSize();
					UpdateDownloadBar();
					for (int k = 0; k < session.nofsensors; k++)
					{
						for (int j = 0; j < 3; j++)
						{
							series[k][j].ItemsSource = currentSession.getSensorData(k, j);
						}
					}
					for (int i = 0; i < 3; i++)
					{
						series[gyro_index][i].ItemsSource = currentSession.getGyroData(i);
					}
					plotView.Model.InvalidatePlot(updateData: true);
					if (polyOverlay.Routes.Count == 0)
					{
						route = new MyGMapRoute("session");
						route.Stroke.Width = 2f;
						polyOverlay.Routes.Add(route);
						GPSData lastGPSData = currentSession.getLastGPSdata();
						if (lastGPSData != null)
						{
							gMapControl.Position = lastGPSData.coords;
						}
					}
					else
					{
						route.Clear();
					}
					tagsListView.Items.Clear();
					for (int l = 0; l < currentSession.getGPSDataCount; l++)
					{
						polyOverlay.Routes[0].Points.Add(currentSession.getGPSdata(l).coords);
					}
					trackLabelStart.Text = getStartTime().ToLongTimeString();
					trackLabelEnd.Text = getEndTimeLabel();
					series[speed_index][0].ItemsSource = currentSession.getSpeedData();
					UpdateCriticalFalls();
					UpdateTagsAndNotes();
					BeginInvoke((MethodInvoker)delegate
					{
						stopReadingButton.Enabled = false;
						panelTags.Enabled = true;
						buttonTerminateEditing.Enabled = false;
						buttonSaveToFile.Enabled = false;
						toolStripCurrentSession.Text = "Session reading complete";
					});
					ReloadData(currentSession.GetSessionRange());
					updateFilename();
				}
				else
				{
					SessionV2 currentSession2 = getCurrentBaseSession() as SessionV2;
					for (int j2 = 0; j2 < 3; j2++)
					{
						series[0][j2].ItemsSource = currentSession2.getMainData(0, j2);
						series[currentSession2.nofsensors + 1][j2].ItemsSource = currentSession2.getMainData(1, j2);
					}
					for (int k2 = 0; k2 < currentSession2.nofsensors; k2++)
					{
						for (int j3 = 0; j3 < 3; j3++)
						{
							series[k2 + 1][j3].ItemsSource = currentSession2.getSensorData(k2, j3);
						}
					}
					series[currentSession2.getSpeedIndex() + 1][0].ItemsSource = currentSession2.getOrientationData(0);
					series[currentSession2.getSpeedIndex() + 1][1].ItemsSource = currentSession2.getOrientationData(1);
					series[currentSession2.getSpeedIndex() + 1][2].ItemsSource = currentSession2.getOrientationData(2);
					series[currentSession2.getSpeedIndex() + 2][0].ItemsSource = currentSession2.getGravityData(0);
					series[currentSession2.getSpeedIndex() + 2][1].ItemsSource = currentSession2.getGravityData(1);
					series[currentSession2.getSpeedIndex() + 2][2].ItemsSource = currentSession2.getGravityData(2);
					plotView.Model.InvalidatePlot(updateData: true);
					if (polyOverlay.Routes.Count == 0)
					{
						route = new MyGMapRoute("session");
						route.Stroke.Width = 2f;
						polyOverlay.Routes.Add(route);
						GPSData lastGPSData2 = getCurrentBaseSession().getLastGPSdata();
						if (lastGPSData2 != null)
						{
							gMapControl.Position = lastGPSData2.coords;
						}
					}
					else
					{
						route.Clear();
					}
					tagsListView.Items.Clear();
					for (int i2 = 0; i2 < currentSession2.getGPSDataCount; i2++)
					{
						if (polyOverlay.Routes.Count == 0)
						{
							route = new MyGMapRoute("session");
							route.Stroke.Width = 2f;
							polyOverlay.Routes.Add(route);
							GPSData lastGPSData3 = getCurrentBaseSession().getLastGPSdata();
							if (lastGPSData3 != null)
							{
								gMapControl.Position = lastGPSData3.coords;
							}
						}
						polyOverlay.Routes[0].Points.Add(currentSession2.getGPSdata(i2).coords);
					}
					BeginInvoke((MethodInvoker)delegate
					{
						gMapControl.Refresh();
						if (getCurrentBaseSession().getGPSDataCount == 1)
						{
							trackLabelStart.Text = getStartTime().ToLongTimeString() + " (" + getStartTime().ToShortDateString() + ")";
						}
						trackLabelEnd.Text = getEndTimeLabel();
					});
					trackLabelStart.Text = getStartTime().ToLongTimeString();
					trackLabelEnd.Text = getEndTimeLabel();
					series[currentSession2.getSpeedIndex()][0].ItemsSource = currentSession2.getSpeedData();
					UpdateCriticalFalls();
					UpdateTagsAndNotes();
					BeginInvoke((MethodInvoker)delegate
					{
						stopReadingButton.Enabled = false;
						panelTags.Enabled = true;
						buttonTerminateEditing.Enabled = false;
						buttonSaveToFile.Enabled = false;
						UpdateStatusBarSession();
					});
					ReloadData(session.GetSessionRange());
					updateFilename();
				}
			}
		}
		progressBarDownload.Value = progressBarDownload.Maximum;
		waitFormLoad.Hide();
		Activate();
		base.Enabled = true;
	}

	private void updateFilename()
	{
		Text = "Loading file - " + getCurrentBaseSession().filename();
	}

	private void updateVisibleAnnotations(uint minIndex, uint maxIndex, int width)
	{
		plotView.Model.Annotations.Clear();
		foreach (LineAnnotation a in verticalAnnotations)
		{
			if (a != null)
			{
				plotView.Model.Annotations.Add(a);
			}
		}
		if (!currentSessionValid())
		{
			return;
		}
		uint lastindex = 0u;
		double delta = (maxIndex - minIndex) / width * 3;
		foreach (Fall f in getCurrentBaseSession().falls)
		{
			if (f.index >= minIndex && f.index <= maxIndex && (double)(f.index - lastindex) > delta)
			{
				lastindex = f.index;
				AddFallPointAnnotation(f.index, (uint)f.fall);
			}
		}
	}

	private void UpdateTagsAndNotes()
	{
		LinkedList<Tag> tags = getCurrentBaseSession().getTags();
		foreach (Tag t in tags)
		{
			ListViewItem listViewItem1 = new ListViewItem(new string[3]
			{
				t.timestamp.ToLongTimeString(),
				t.type,
				t.description
			}, -1);
			listViewItem1.Tag = t.id;
			tagsListView.Items.Add(listViewItem1);
		}
	}

	private void LoadFile_DoWork(object sender, DoWorkEventArgs e)
	{
		BackgroundWorker worker = sender as BackgroundWorker;
		if (worker.CancellationPending)
		{
			e.Cancel = true;
			return;
		}
		FileInfo fileInfo = new FileInfo((string)e.Argument);
		series.Clear();
		verticalAnnotations.Clear();
		if (fileInfo.Extension == ".dat")
		{
			if (!sessions.ContainsKey(currentSessionID) || sessions[currentSessionID].GetType() == typeof(SessionV2Tag))
			{
				SessionV2 sV2 = new SessionV2(deviceID, currentSessionID, (string)e.Argument);
				sessions[currentSessionID] = sV2;
				plotView.Model = SessionV2.InitSessionPlotModel(series, verticalAnnotations, getTimeLabel);
				sV2.ReadSessionFromFileV2((string)e.Argument, ReadSessionV2ProgressReport);
				e.Result = sV2;
			}
			else
			{
				plotView.Model = SessionV2.InitSessionPlotModel(series, verticalAnnotations, getTimeLabel);
				e.Result = sessions[currentSessionID];
			}
		}
		else
		{
			plotView.Model = Session.InitSessionPlotModel(series, verticalAnnotations, getTimeLabel);
			e.Result = new Session(deviceID, currentSessionID, (string)e.Argument);
		}
		plotView.Model.Axes[0].AxisChanged += delegate
		{
			ReloadData();
		};
	}

	private bool Thereisenoughdata()
	{
		if (serial_buffer.Length < 2)
		{
			return false;
		}
		if (serial_buffer.Length == 49 && serial_buffer.At(1) == 48)
		{
			return true;
		}
		if (serial_buffer.Length < serial_buffer.At(1) + 2)
		{
			return false;
		}
		return true;
	}

	private bool SeekGoodMessageInBuffer()
	{
		if (serial_buffer.Length < 2)
		{
			return false;
		}
		byte header_type = serial_buffer.At(0);
		byte header_len = serial_buffer.At(1);
		if (header_type != 2 && header_type != 3 && header_type != 11)
		{
			return false;
		}
		switch (header_type)
		{
		case 2:
			if (header_len == 48)
			{
				return true;
			}
			return false;
		case 11:
			if (header_len == 52)
			{
				return true;
			}
			return false;
		case 3:
			if (header_len == 82)
			{
				return true;
			}
			return false;
		default:
			return false;
		}
	}

	private bool PackageSeemsGood()
	{
		if (serial_buffer.Length < 2)
		{
			return false;
		}
		byte header_type = serial_buffer.At(0);
		byte header_len = serial_buffer.At(1);
		if (serial_buffer.Length < header_len + 2 && header_len != 48 && serial_buffer.Length != 49)
		{
			log.WarnFormat("Message correction");
			return false;
		}
		if (readingSession && header_type != 2 && header_type != 3 && header_type != 11)
		{
			log.WarnFormat("Unexpected message of type 0x{0:X} while reading a session with base address {1} - 0x{2:X} bytes read ({3} tries)", header_type, (getCurrentBaseSession() as Session).sessionAddress.ToString(), bytesRead, badPackageFound);
			return false;
		}
		switch (header_type)
		{
		case 1:
			if (header_len == 21)
			{
				return true;
			}
			return false;
		case 2:
			if (header_len == 48)
			{
				return true;
			}
			return false;
		case 11:
			if (header_len == 52)
			{
				return true;
			}
			return false;
		case 3:
			if (header_len == 82)
			{
				return true;
			}
			return false;
		case 4:
			return header_len == 21;
		case 5:
			return header_len == 0;
		case 6:
			return header_len == 1;
		case 7:
			return header_len == 0;
		case 8:
			return header_len == 0;
		case 9:
			if (pufferfishv2)
			{
				return header_len == 8;
			}
			return header_len == 6;
		default:
			return true;
		}
	}

	private byte[] GetPackage()
	{
		byte size = serial_buffer.At(1);
		byte[] ret = new byte[size + 2];
		serial_buffer.Dequeue(ret, 0, size + 2);
		return ret;
	}

	private void OnDeleteMemoryClick(object sender, EventArgs e)
	{
		if (MessageBox.Show("Are you sure?", "Device memory erasing", MessageBoxButtons.YesNo) == DialogResult.Yes)
		{
			SendMemoryDelete();
			base.Enabled = false;
			waitForm.Show(this);
			waitForm.FormClosed += WaitForm_FormClosed;
		}
	}

	private void WaitForm_FormClosed(object sender, FormClosedEventArgs e)
	{
		base.Enabled = true;
	}

	private void OnLoadSessionFromFileClick(object sender, EventArgs e)
	{
		OpenFileDialog openFileDialog1 = new OpenFileDialog();
		openFileDialog1.Filter = "Track file (*.session, *.track, *.dat)|*.session;*.track;*.dat";
		openFileDialog1.FilterIndex = 1;
		openFileDialog1.CheckFileExists = true;
		openFileDialog1.Multiselect = false;
		if (openFileDialog1.ShowDialog() == DialogResult.OK)
		{
			string sPattern = "(\\d*)_(\\d*)_*X*\\.(?:session|track|dat)";
			Match m = Regex.Match(openFileDialog1.SafeFileName, sPattern);
			if (m != null && m.Success)
			{
				uint sessionid = uint.Parse(m.Groups[2].Value);
				deviceID = int.Parse(m.Groups[1].Value);
				currentSessionID = sessionid;
				backgroundWorker1.RunWorkerAsync(openFileDialog1.FileName);
				base.Enabled = false;
				waitFormLoad.Show(this);
			}
		}
	}

	private void OnSaveToFileClick(object sender, EventArgs e)
	{
		if (SessionIsValid())
		{
			SaveFileDialog savefile = new SaveFileDialog();
			savefile.OverwritePrompt = true;
			savefile.FileName = getCurrentSession().getDbName() + ".track";
			savefile.Filter = "Track file|*.track";
			if (savefile.ShowDialog() == DialogResult.OK && savefile.FileName.Length > 0)
			{
				try
				{
					new FileIOPermission(FileIOPermissionAccess.Read, BaseSession.getDBFolder() + getCurrentSession().getDbName()).Demand();
					new FileIOPermission(FileIOPermissionAccess.Write, savefile.FileName).Demand();
					File.Copy(BaseSession.getDBFolder() + getCurrentSession().getDbName(), savefile.FileName, overwrite: true);
				}
				catch (UnauthorizedAccessException)
				{
					MessageBox.Show("Waring, you don't have the permission to save the file to the specified position", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				}
				catch (Exception ex2)
				{
					MessageBox.Show(string.Format("An error occurred while saving the file {0}\nThe error is {1}\nThe local file is {2} ({3:n} kB)\n{4}\n{5}", savefile.FileName, ex2.Message, BaseSession.getDBFolder() + getCurrentSession().getDbName(), (int)(new FileInfo(BaseSession.getDBFolder() + getCurrentSession().getDbName()).Length / 1000), string.Join(";", ex2.Data), ex2.StackTrace), "Error while saving the file", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				}
			}
		}
		else
		{
			MessageBox.Show("Session not valid", "Error while saving the file", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private void OnStopDataClick(object sender, EventArgs e)
	{
		justconnected = false;
		SendStopRequest();
		UpdateStatusBarSpeed("");
		SetSessionRead();
	}

	private void MoveGraphToIndex(uint selecteDataIndex)
	{
		try
		{
			int position = trackTimer.Value;
			int maxposition = trackTimer.Maximum * progressBarDownload.Value / progressBarDownload.Maximum;
			if (position > maxposition)
			{
				trackTimer.Value = maxposition;
				return;
			}
			BaseSession session = getCurrentBaseSession();
			DateTime selectedTime = session.getTimeForDataIndex(selecteDataIndex);
			trackLabelCurrent.Text = selectedTime.ToLongTimeString();
			if (session.getGPSDataCount > 0)
			{
				int gpsIndex = session.getGPSIndex(selecteDataIndex);
				route.middlepoint = gpsIndex;
				marker.Position = session.getGPSdata(gpsIndex).coords;
				gMapControl.Position = session.getGPSdata(gpsIndex).coords;
				gMapControl.Refresh();
			}
			double delta = (plotView.Model.Axes[0].ActualMaximum - plotView.Model.Axes[0].ActualMinimum) / 2.0;
			double deltamin = delta;
			double deltamax = delta;
			double center = selecteDataIndex;
			if (center + deltamax > plotView.Model.Axes[0].AbsoluteMaximum)
			{
				deltamax += plotView.Model.Axes[0].AbsoluteMaximum - (center + delta);
				deltamin += plotView.Model.Axes[0].AbsoluteMaximum - (center + delta);
			}
			plotView.Model.Axes[0].Zoom(center - deltamin, center + deltamax);
			foreach (LineAnnotation verticalAnnotation in verticalAnnotations)
			{
				verticalAnnotation.X = center;
			}
		}
		catch (Exception ex)
		{
			log.Error(ex.Message);
		}
	}

	private void OnTrackScroll(object sender, EventArgs e)
	{
		try
		{
			int position = trackTimer.Value;
			int maxposition = trackTimer.Maximum * progressBarDownload.Value / progressBarDownload.Maximum;
			if (position > maxposition)
			{
				trackTimer.Value = maxposition;
			}
			else
			{
				MoveGraphToIndex((uint)(getCurrentBaseSession().MinIndex + (getCurrentBaseSession().MaxIndex - getCurrentBaseSession().MinIndex) * position / maxposition));
			}
		}
		catch (Exception ex)
		{
			log.Error(ex.Message);
		}
	}

	private void OnZoomInClick(object sender, EventArgs e)
	{
		gMapControl.Zoom += 1.0;
	}

	private void OnZoomOutClick(object sender, EventArgs e)
	{
		gMapControl.Zoom -= 1.0;
	}

	private void ParseDeviceID(byte[] buffer)
	{
		if (pufferfishv2)
		{
			deviceID = BitConverter.ToInt32(buffer, 2);
			firmwareVersion = BitConverter.ToUInt32(buffer, 6);
		}
		else
		{
			deviceID = BitConverter.ToInt32(buffer, 2);
			firmwareVersion = BitConverter.ToUInt16(buffer, 6);
		}
		UpdateStatusBarVersion();
	}

	private GPSData ParseGPSData(byte[] buffer)
	{
		string stringa = Encoding.ASCII.GetString(buffer, 2, 82);
		if (stringa.StartsWith("$PSRF") || stringa.StartsWith("PSRF"))
		{
			return null;
		}
		string[] stringhe = stringa.Split(',');
		if (stringhe.Length != 13)
		{
			log.DebugFormat("Wrong number of fields in GPS data at 0x{0:X2}", bytesRead);
			return null;
		}
		if (!"A".Equals(stringhe[2]))
		{
			log.Info("GPS reports invalid status");
			return null;
		}
		string time_string = stringhe[9] + stringhe[1];
		DateTime time;
		if (time_string.Length == 16)
		{
			time = DateTime.ParseExact(time_string, "ddMMyyHHmmss.fff", null, DateTimeStyles.AssumeUniversal);
		}
		else
		{
			if (time_string.Length != 15)
			{
				log.Info("Wrong data format detected");
				return null;
			}
			time = DateTime.ParseExact(time_string, "ddMMyyHHmmss.ff", null, DateTimeStyles.AssumeUniversal);
		}
		GPSData gd = new GPSData(getCurrentSession().getSensorsDataCount, time, float.Parse(stringhe[7], CultureInfo.InvariantCulture) * 1.852f);
		lastTime = time;
		gd.coords = new PointLatLng(GpsEncodingToDegrees(stringhe[3], !"S".Equals(stringhe[4])), GpsEncodingToDegrees(stringhe[5], !"W".Equals(stringhe[6])));
		if (stringhe[8] != "")
		{
			gd.angle = float.Parse(stringhe[8], CultureInfo.InvariantCulture);
		}
		else
		{
			gd.angle = 0f;
		}
		getCurrentSession().storeGPSData(gd);
		return gd;
	}

	private void ParsePackage(byte[] package)
	{
		switch (package[0])
		{
		case 1:
			ParseStatus(package);
			SendDeviceIDRequest();
			break;
		case 2:
		case 11:
			if (SessionIsValid())
			{
				if (getCurrentSession().getSensorsDataCount % 100 == 0)
				{
					UpdateDownloadBar();
				}
				ParseSensorsData(package);
				if (bytesRead == getCurrentSession().sessionSize.getSize())
				{
					SetSessionRead();
				}
				else if (bytesRead > getCurrentSession().sessionSize.getSize())
				{
					log.WarnFormat("Session was 0x{1:X} and I read 0x{0:X} bytes!", bytesRead, getCurrentSession().sessionSize.getSize());
					getCurrentSession().commit();
					UpdateDownloadBar();
				}
				else if (getCurrentSession().getSensorsDataCount % 100 == 0)
				{
					UpdateDownloadBar();
				}
			}
			break;
		case 3:
		{
			if (!SessionIsValid())
			{
				break;
			}
			UpdateDownloadBar();
			GPSData newData = ParseGPSData(package);
			if (newData != null)
			{
				addPointToRoute(newData.coords);
				if (scanninSessions)
				{
					getCurrentSession().startDate = newData.time;
					justconnected = false;
					SendStopRequest(isdefinitive: false);
					Thread.Sleep(500);
					ScanNextSession();
				}
			}
			if (bytesRead == getCurrentSession().sessionSize.getSize())
			{
				SetSessionRead();
			}
			else if (bytesRead > getCurrentSession().sessionSize.getSize())
			{
				log.WarnFormat("Session was 0x{1:X} and I read 0x{0:X} bytes!", bytesRead, getCurrentSession().sessionSize.getSize());
			}
			break;
		}
		case 4:
			ParseSession(package);
			UpdateMemoryBar();
			if (sessions.Count == sess_count && justconnected)
			{
				BeginInvoke((MethodInvoker)delegate
				{
					AskForSessionToRequest();
				});
			}
			break;
		case 9:
			ParseDeviceID(package);
			if (!pufferfishv2 && (firmwareVersion & 0xFFF) < (LAST_FIRMWARE & 0xFFF) && MessageBox.Show("The connected device has an old firmware version. Do you want to update it?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
			{
				UpdateFirmware(2, null);
				justconnected = false;
			}
			else
			{
				if (!justconnected)
				{
					break;
				}
				if (!pufferfishv2)
				{
					if (sess_count == 0)
					{
						MessageBox.Show("No session was found on the device", "No session found", MessageBoxButtons.OK);
					}
					else
					{
						SendSessionListRequest();
					}
				}
				else
				{
					GetSessionsFileList();
				}
			}
			break;
		case 7:
			log.Info("Got USB message");
			break;
		case 6:
			if (package[2] == 4)
			{
				UpdateStatusBar("Memory erased");
				log.Info("Got Clean done");
				currentSessionID = uint.MaxValue;
				SendSendStatusRequest();
				BeginInvoke((MethodInvoker)delegate
				{
					waitForm.Hide();
					base.Enabled = true;
				});
			}
			else
			{
				log.WarnFormat("Got unexpected simple command {0}", package);
			}
			break;
		case 10:
			if (package[1] == 1)
			{
				MessageBox.Show(string.Format("Hip Right {0}\nElbow Right {1}\nShoulder Right {2}\nShoulder Left {3}\nElbow Left {4}\nHip Left {5}", ((package[2] & 1) != 0) ? "yes" : "no", ((package[2] & 2) != 0) ? "yes" : "no", ((package[2] & 4) != 0) ? "yes" : "no", ((package[2] & 8) != 0) ? "yes" : "no", ((package[2] & 0x10) != 0) ? "yes" : "no", ((package[2] & 0x20) != 0) ? "yes" : "no"), "Found sensors");
			}
			else
			{
				log.WarnFormat("Got unexpected simple command {0}", package);
			}
			break;
		default:
			log.WarnFormat("Got unknown packet data  at 0x{1:X2}: header 0x{0:X2} lenght:{2}", package[0], bytesRead, package[1]);
			break;
		}
	}

	private void ParseSensorsData(byte[] buffer)
	{
		SensorData sd = new SensorData(getCurrentSession().getSensorsDataCount);
		bool hasFallDetection = buffer[0] == 11;
		int i = 2;
		for (int j = 0; j < 3; j++)
		{
			sd.gyro[j] = (double)BitConverter.ToInt16(buffer, i) * 0.0152587890625;
			i += 2;
		}
		for (int s = 0; s < getCurrentSession().nofsensors; s++)
		{
			for (int k = 0; k < 3; k++)
			{
				sd.accelerometer[s][k] = (double)BitConverter.ToInt16(buffer, i) / 16384.0 * 2.0;
				i += 2;
			}
		}
		if (hasFallDetection)
		{
			sd.fall = BitConverter.ToInt32(buffer, i);
			i += 4;
		}
		if (!lastTime.HasValue)
		{
			lastTime = DateTime.Now;
		}
		if (lastTime.HasValue)
		{
			lastTime = lastTime.Value.AddMilliseconds(1.0);
			sd.time = lastTime.Value;
			getCurrentSession().storeSensorDataToDB(sd);
			getCurrentSession().addSensorDataToSession(sd);
			if (sd.fall == 0)
			{
			}
		}
		bool endreached = bytesRead == getCurrentSession().sessionSize.getSize();
		if (getCurrentSession().getSensorsDataCount % 1000 == 0 || endreached)
		{
			plotView.Model.InvalidatePlot(updateData: true);
			if (!endreached)
			{
				int sec = (int)((double)(getCurrentSession().sessionSize.getSize() - bytesRead) * (DateTime.Now - sesseionRequestTime).TotalSeconds / (double)bytesRead);
				UpdateStatusBarSpeed("{0:n0} kB/s - {1:00}:{2:00} mins before completing", (double)bytesRead / (DateTime.Now - sesseionRequestTime).TotalSeconds / 1000.0, sec / 60, sec % 60);
			}
		}
		if (endreached)
		{
			getCurrentSession().setEndreached();
		}
	}

	private void AddFallPointAnnotation(uint index, uint fall)
	{
		uint color = 2291846450u;
		if (IsCriticalFall(fall, getCurrentBaseSession()))
		{
			color = 2296911639u;
		}
		if ((fall & 1) != 0)
		{
			AddFallPointAnnotation(index, "Main", color);
		}
		if ((fall & 2) != 0)
		{
			AddFallPointAnnotation(index, "Hip R", color);
		}
		if ((fall & 4) != 0)
		{
			AddFallPointAnnotation(index, "Elbow R", color);
		}
		if ((fall & 8) != 0)
		{
			AddFallPointAnnotation(index, "Shoul R", color);
		}
		if ((fall & 0x10) != 0)
		{
			AddFallPointAnnotation(index, "Shoul L", color);
		}
		if ((fall & 0x20) != 0)
		{
			AddFallPointAnnotation(index, "Elbow L", color);
		}
		if ((fall & 0x40) != 0)
		{
			AddFallPointAnnotation(index, "Hip L", color);
		}
		if ((fall & 0x100) != 0)
		{
			AddFallPointAnnotation(index, "Gyro", color);
		}
		if ((fall & 0x1000) != 0)
		{
			AddFallPointAnnotation(index, "Gravity", color);
		}
		if ((fall & 0x10000) != 0)
		{
			if ((fall & 0x20000) != 0)
			{
				color = 2296911639u;
			}
			AddFallPointAnnotation(index, "Pose", color);
		}
		if ((fall & 0x100000) != 0)
		{
			color = OxyColor.FromRgb(100, 100, 100).ToUint();
			AddFallPointAnnotation(index, "Pose", color);
		}
	}

	private void AddFallPointAnnotation(uint index, string title, uint color)
	{
		Annotation annotation = new LineAnnotation
		{
			Type = LineAnnotationType.Vertical,
			X = index,
			ClipByYAxis = true,
			Color = OxyColor.FromUInt32(color),
			LineStyle = LineStyle.Solid,
			StrokeThickness = 3.0,
			YAxisKey = title
		};
		sessionFallPoints.Add(annotation);
		plotView.Model.Annotations.Add(annotation);
	}

	private void ParseSession(byte[] buffer)
	{
		int offset = 2;
		uint sessionID = BitConverter.ToUInt32(buffer, offset);
		Address sessionAddress = new Address(buffer, offset + 4);
		PFSize sessionSize = new PFSize(buffer, offset + 10);
		Address nextAddress = new Address(buffer, offset + 15);
		log.Info("Got session data");
		log.InfoFormat("\tID {0}, size 0x{1:X}, sessAddress ox{2:X}, nextAddress 0x{3:X}", sessionID, sessionSize.getSize(), sessionAddress, nextAddress);
		if (sessions.ContainsKey(sessionID))
		{
			sessions.Remove(sessionID);
		}
		BeginInvoke((MethodInvoker)delegate
		{
			richiediSessioneToolStripMenuItem.Enabled = true;
			richiediTutteLeSessioniToolStripMenuItem.Enabled = true;
		});
		Session s = new Session(deviceID, sessionID, sessionSize, sessionAddress);
		sessions[sessionID] = s;
		UpdateStatusBar("Read {0} sessions", sessions.Count);
		UpdateStatusBarSession();
	}

	private void SetPufferfishV2(bool ver)
	{
		pufferfishv2 = ver;
		if (pufferfishv2)
		{
			richiediStopToolStripMenuItem.Enabled = false;
			cancellaMemoriaToolStripMenuItem.Enabled = false;
			installaFirmwareDiTestToolStripMenuItem.Enabled = false;
			aggiornaFirmwareToolStripMenuItem.Enabled = true;
			toolStripMenuItem4.Enabled = false;
			toolStripMenuItem5.Enabled = true;
			enterShippingModeToolStripMenuItem.Enabled = true;
			magnetometerCalibrationToolStripMenuItem.Enabled = true;
		}
		else
		{
			richiediStopToolStripMenuItem.Enabled = true;
			cancellaMemoriaToolStripMenuItem.Enabled = true;
			installaFirmwareDiTestToolStripMenuItem.Enabled = true;
			aggiornaFirmwareToolStripMenuItem.Enabled = true;
			toolStripMenuItem4.Enabled = true;
			toolStripMenuItem5.Enabled = false;
			enterShippingModeToolStripMenuItem.Enabled = false;
			magnetometerCalibrationToolStripMenuItem.Enabled = false;
		}
	}

	private void ParseStatus(byte[] buffer)
	{
		int err = BitConverter.ToInt32(buffer, 2);
		int err_type = BitConverter.ToInt32(buffer, 6);
		int err_value = BitConverter.ToInt32(buffer, 10);
		int bad_blocks = BitConverter.ToInt32(buffer, 14);
		int app_status = BitConverter.ToInt32(buffer, 18);
		sess_count = buffer[22];
		log.Info("Got status data");
		if (app_status == 255)
		{
			SetPufferfishV2(ver: true);
		}
		else
		{
			SetPufferfishV2(ver: false);
		}
		if (err != 0)
		{
			string error = "";
			if ((err & 1) != 0)
			{
				error = "Error in gyroscope data";
			}
			if ((err & 2) != 0)
			{
				error += "Error in accellerometer data";
			}
			if ((err & 4) != 0)
			{
				error += "Error in GPS data";
			}
			if ((err & 8) != 0)
			{
				error = err_type switch
				{
					1 => error + "ERROR: Cannot detect memory (" + err_value + ")", 
					2 => error + "ERROR: Impossible to initialize memory", 
					_ => error + "Memory Error", 
				};
			}
			if ((err & 0x10) != 0)
			{
				error = "Errore off-board accelerometer";
			}
			UpdateStatusBar("Errore 0x{0:X} - {1} ( ERR_TYPE {2}  ERR_VALUE {3}  {4} {5} {6})", err, error, err_type, err_value, bad_blocks, app_status, sess_count);
			log.WarnFormat("err 0x{0:X}, err_type {1}, err_value {2}, bad_blocks {3}, app_status {4}, sess_count {5}", err, err_type, err_value, bad_blocks, app_status, sess_count);
		}
		else
		{
			UpdateStatusBar("Stato: OK");
			log.DebugFormat("err 0x{0:X}, err_type {1}, err_value {2}, bad_blocks {3}, app_status {4}, sess_count {5}", err, err_type, err_value, bad_blocks, app_status, sess_count);
		}
		UpdateStatusBarSession();
	}

	private void ReloadData(Range<uint> range = null)
	{
		if (sessions.Count != 0)
		{
			uint minimum = range?.start ?? ((uint)plotView.Model.Axes[0].ActualMinimum);
			uint maximum = range?.end ?? ((uint)plotView.Model.Axes[0].ActualMaximum);
			plotView.Model.Axes[0].Minimum = minimum;
			plotView.Model.Axes[0].Maximum = maximum;
			getCurrentBaseSession().loadData((int)minimum, (int)maximum);
			updateVisibleAnnotations(minimum, maximum, plotView.Width);
			plotView.Model.InvalidatePlot(updateData: true);
			plotView.InvalidatePlot(updateData: true);
		}
	}

	private void RichiediListaSessioniToolStripMenuItem_Click(object sender, EventArgs e)
	{
		justconnected = false;
		if (!pufferfishv2)
		{
			if (sess_count == 0)
			{
				MessageBox.Show("No sessions are present on the device", "No session", MessageBoxButtons.OK);
			}
			else
			{
				SendSessionListRequest();
			}
		}
		else
		{
			GetSessionsFileList();
		}
	}

	private void RichiediSessioneToolStripMenuItem_Click(object sender, EventArgs e)
	{
		justconnected = false;
		if (!pufferfishv2)
		{
			AskForSessionToRequest();
		}
		else
		{
			AskForSessionToRequestV2();
		}
	}

	private void RichiediStatoToolStripMenuItem_Click(object sender, EventArgs e)
	{
		justconnected = false;
		SendSendStatusRequest();
	}

	private void RichiediStopToolStripMenuItem_Click(object sender, EventArgs e)
	{
		justconnected = false;
		SendStopRequest();
		UpdateStatusBarSpeed("");
	}

	private void Safelog(string stringa)
	{
		log.Debug(stringa);
	}

	private void SendData(byte[] dati)
	{
		int tries = 9;
		if (!seriale.IsOpen())
		{
			Safelog("Open port before sending commands!\n");
			return;
		}
		do
		{
			try
			{
				seriale.Transmit(dati);
				tries = 0;
			}
			catch
			{
				tries--;
				if (tries > 0)
				{
					Thread.Sleep(500);
					continue;
				}
				MessageBox.Show("It was not possible to communicate with the device, please retry.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				if (justconnected)
				{
					doEarlyDisconnect();
				}
			}
		}
		while (tries != 0);
	}

	private void SendDeviceIDRequest()
	{
		byte[] buffer = new byte[3];
		int i = 0;
		buffer[i++] = 6;
		buffer[i++] = 1;
		buffer[i++] = 5;
		SendData(buffer);
		stopRequested = false;
	}

	private void SendMemoryDelete()
	{
		byte[] buffer = new byte[3];
		int i = 0;
		buffer[i++] = 6;
		buffer[i++] = 1;
		buffer[i++] = 3;
		SendData(buffer);
		UpdateStatusBar("Memory deletion in progress");
	}

	private void SendMicroReset()
	{
		byte[] buffer = new byte[3];
		int i = 0;
		buffer[i++] = 6;
		buffer[i++] = 1;
		buffer[i++] = 8;
		SendData(buffer);
		UpdateStatusBar("Device reset in progress");
	}

	private void SendShippingMode()
	{
		byte[] buffer = new byte[3];
		int i = 0;
		buffer[i++] = 6;
		buffer[i++] = 1;
		buffer[i++] = 11;
		SendData(buffer);
		UpdateStatusBar("Device reset in progress");
	}

	private void SendMagnetometerCalibration()
	{
		byte[] buffer = new byte[3];
		int i = 0;
		buffer[i++] = 6;
		buffer[i++] = 1;
		buffer[i++] = 15;
		SendData(buffer);
	}

	private void SendEnterBootloader()
	{
		byte[] buffer = new byte[3];
		int i = 0;
		buffer[i++] = 6;
		buffer[i++] = 1;
		buffer[i++] = 12;
		SendData(buffer);
	}

	private void SendSendStatusRequest()
	{
		byte[] buffer = new byte[3];
		int i = 0;
		buffer[i++] = 6;
		buffer[i++] = 1;
		buffer[i++] = 1;
		SendData(buffer);
		stopRequested = false;
	}

	private void SendSessionListRequest()
	{
		sessions.Clear();
		byte[] buffer = new byte[3];
		int i = 0;
		buffer[i++] = 6;
		buffer[i++] = 1;
		buffer[i++] = 2;
		SendData(buffer);
		stopRequested = false;
	}

	private void SendSessionRequest(uint sessionID, PFSize offset, bool tryingtorecover)
	{
		if (pufferfishv2)
		{
			return;
		}
		try
		{
			byte[] buffer = new byte[11];
			int i = 0;
			buffer[i++] = 5;
			buffer[i++] = 9;
			BitConverter.GetBytes(sessionID).CopyTo(buffer, i);
			i += 4;
			i += offset.WriteToBuffer(buffer, i);
			stopRequested = false;
			if (!tryingtorecover)
			{
				closeDB();
				currentSessionID = sessionID;
				lastTime = null;
				bytesRead = 0uL;
				sesseionRequestTime = DateTime.Now;
				BeginInvoke((MethodInvoker)delegate
				{
					progressBarDownload.Value = 0;
				});
				getCurrentSession().clearData();
				BeginInvoke((MethodInvoker)delegate
				{
					updateFilename();
				});
				tagsListView.Items.Clear();
				if (route != null)
				{
					route.Clear();
				}
				for (int k = 0; k < getCurrentSession().nofsensors; k++)
				{
					for (int j = 0; j < 3; j++)
					{
						series[k][j].ItemsSource = getCurrentSession().getSensorData(k, j);
					}
				}
				for (int j2 = 0; j2 < 3; j2++)
				{
					series[gyro_index][j2].ItemsSource = getCurrentSession().getGyroData(j2);
				}
				series[speed_index][0].ItemsSource = getCurrentSession().getSpeedData();
			}
			else
			{
				log.WarnFormat("Trying to continue from {0:X}...", offset.getSize());
			}
			seriale.DiscardInBuffer();
			serial_buffer.Clear();
			readingSession = true;
			SendData(buffer);
			UpdateStatusBar("Session {0} requested - {1:n0} kB", sessionID, getCurrentSession().sessionSize.getSize() / 1000);
			UpdateStatusBarSession();
			BeginInvoke((MethodInvoker)delegate
			{
				stopReadingButton.Enabled = true;
				panelTags.Enabled = true;
				buttonTerminateEditing.Enabled = false;
				buttonSaveToFile.Enabled = false;
			});
		}
		catch (Exception ex)
		{
			log.Error(ex.Message);
		}
	}

	private void SendStopRequest(bool isdefinitive = true)
	{
		byte[] buffer = new byte[3];
		int i = 0;
		buffer[i++] = 6;
		buffer[i++] = 1;
		buffer[i++] = 128;
		readingSession = false;
		SendData(buffer);
		stopRequested = true;
		if (isdefinitive)
		{
			BeginInvoke((MethodInvoker)delegate
			{
				stopReadingButton.Enabled = false;
			});
		}
	}

	private bool SessionIsValid()
	{
		return currentSessionID != uint.MaxValue && sessions.Contains(currentSessionID);
	}

	private void SetSessionRead()
	{
		readingSession = false;
		BeginInvoke((MethodInvoker)delegate
		{
			stopReadingButton.Enabled = false;
			UpdateStatusBar("Session {0} read completed", currentSessionID);
			UpdateStatusBarSpeed("{0:n0} kB/s", (double)bytesRead / (DateTime.Now - sesseionRequestTime).TotalSeconds / 1000.0);
			UpdateCriticalFalls();
			progressBarDownload.Value = progressBarDownload.Maximum;
			buttonTerminateEditing.Enabled = true;
			buttonSaveToFile.Enabled = false;
		});
		if (autoLoadIndex != -1)
		{
			LoadNextSession();
		}
		else
		{
			ReloadData(getCurrentSession().GetSessionRange());
		}
	}

	[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
	public static extern uint SHParseDisplayName(string pszName, IntPtr zero, out IntPtr ppidl, uint sfgaoIn, out uint psfgaoOut);

	[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
	public static extern uint SHGetNameFromIDList(IntPtr pidl, SIGDN sigdnName, out string ppszName);

	public string GetDriveLabel(string driveNameAsLetterColonBackslash)
	{
		if (SHParseDisplayName(driveNameAsLetterColonBackslash, IntPtr.Zero, out var pidl, 0u, out var _) == 0 && SHGetNameFromIDList(pidl, SIGDN.PARENTRELATIVEEDITING, out var name) == 0 && name != null)
		{
			return name;
		}
		return null;
	}

	private string GetDriveLabelFromAutorunInf(string drivename)
	{
		try
		{
			string filepathAutorunInf = Path.Combine(drivename, "autorun.Inf");
			string stringInputLine = "";
			if (File.Exists(filepathAutorunInf))
			{
				StreamReader streamReader = new StreamReader(filepathAutorunInf);
				while ((stringInputLine = streamReader.ReadLine()) != null)
				{
					if (stringInputLine.StartsWith("label="))
					{
						return stringInputLine.Substring(6);
					}
				}
				return "";
			}
			return "";
		}
		catch (Exception)
		{
			return "";
		}
	}

	private void GetSessionsFileList()
	{
		if (!pufferfishv2)
		{
			return;
		}
		DriveInfo[] drives = DriveInfo.GetDrives();
		foreach (DriveInfo drive in drives)
		{
			if (drive.DriveType != DriveType.Removable)
			{
				continue;
			}
			string sPattern = "PF_([0-9]*)";
			Match m = Regex.Match(GetDriveLabel(drive.Name), sPattern);
			if (m == null || !m.Success)
			{
				continue;
			}
			int id = int.Parse(m.Groups[1].Value);
			if (id != deviceID)
			{
				continue;
			}
			string[] list = Directory.GetFiles(drive.Name, "*.dat");
			string[] array = list;
			foreach (string sess_file in array)
			{
				string sess_file_pattern = "([0-9]*)_([0-9]*)_([0-9]*).dat";
				m = Regex.Match(sess_file, sess_file_pattern);
				if (m != null && m.Success)
				{
					uint sessionID = uint.Parse(m.Groups[3].Value);
					if (sessions.ContainsKey(sessionID))
					{
						sessions.Remove(sessionID);
					}
					sessions[sessionID] = new SessionV2Tag(deviceID, sessionID, sess_file);
				}
			}
			sess_count = (uint)sessions.Count;
			if (sess_count == 0)
			{
				richiediSessioneToolStripMenuItem.Enabled = false;
			}
			else
			{
				richiediSessioneToolStripMenuItem.Enabled = true;
			}
			UpdateStatusBarSession();
		}
	}

	private void UpdateDownloadBar()
	{
		BeginInvoke((MethodInvoker)delegate
		{
			progressBarDownload.Value = Math.Min(1000, (int)(bytesRead * (uint)progressBarDownload.Maximum / getCurrentSession().sessionSize.getSize()));
		});
	}

	private void UpdateFirmware(int test_firmware, string filetoupload)
	{
		if (seriale.IsOpen())
		{
			if (SessionIsValid())
			{
				getCurrentSession().commit();
			}
			setConnectedButtons(connected: false);
			SendMicroReset();
			try
			{
				seriale.CloseConn();
			}
			catch
			{
			}
		}
		else if (MessageBox.Show("The serial port is not connected. You must connect to it or do the reset.\n\nDo you want to proceed anyway?", "Attention", MessageBoxButtons.YesNo) != DialogResult.Yes)
		{
			return;
		}
		UpdateStatusBar("Updating firmware...");
		try
		{
			UpdateFirmwareForm uff = new UpdateFirmwareForm(test_firmware, filetoupload);
			uff.ShowDialog();
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message);
		}
		UpdateStatusBar("Firmware update completed");
	}

	private void UpdateMemoryBar()
	{
		BeginInvoke((MethodInvoker)delegate
		{
			ulong usedMemory = getUsedMemory();
			int num = (int)(usedMemory * 100 / 2147483648u);
			toolStripMemory.Text = $"Used memory: {num}%";
		});
	}

	private void UpdateStatusBar(string v, params object[] obs)
	{
		BeginInvoke((MethodInvoker)delegate
		{
			toolStripStatusLabel.Text = string.Format(v, obs);
		});
	}

	private void UpdateStatusBarSession()
	{
		BeginInvoke((MethodInvoker)delegate
		{
			if (currentSessionValid())
			{
				toolStripCurrentSession.Text = $"Session ID: {currentSessionID}";
			}
			else
			{
				toolStripCurrentSession.Text = "No Session selected";
			}
			if (sess_count != 0)
			{
				toolStripTotSession.Text = $"Total Sessions: {sess_count}";
			}
			else
			{
				toolStripTotSession.Text = "Empty session list";
			}
		});
	}

	private bool currentSessionValid()
	{
		return currentSessionID != uint.MaxValue && sessions.Contains(currentSessionID);
	}

	private void UpdateStatusBarSpeed(string v, params object[] obs)
	{
		BeginInvoke((MethodInvoker)delegate
		{
			toolStripStatusLabelSpeed.Text = string.Format(v, obs);
		});
	}

	private void UpdateStatusBarVersion()
	{
		if (!pufferfishv2)
		{
			BeginInvoke((MethodInvoker)delegate
			{
				string text = (((firmwareVersion & 0xC000) == 49152) ? "A" : (((firmwareVersion & 0x8000) != 32768) ? "D" : "R"));
				toolStripVersionStatus.Text = $"Board SN: {deviceID} Firmware {(firmwareVersion & 0x3FFF) / 256}.{(firmwareVersion & 0x3FFF) % 256} {text}";
			});
			return;
		}
		BeginInvoke((MethodInvoker)delegate
		{
			string text = (((firmwareVersion & 0xC0000000u) == 3221225472u) ? "A" : (((firmwareVersion & 0x80000000u) != 2147483648u) ? "T" : "R"));
			toolStripVersionStatus.Text = $"Board SN: {deviceID} Firmware {(firmwareVersion & 0xFF0000) >> 16}.{(firmwareVersion & 0xFF00) >> 8}.{firmwareVersion & 0xFF} {text}";
		});
	}

	private void WebClient_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
	{
		if (e.Error == null)
		{
			BeginInvoke((MethodInvoker)delegate
			{
				gMapControl.MapProvider = GMapProviders.BingSatelliteMap;
			});
		}
	}

	private void RichiediTutteLeSessioniToolStripMenuItem_Click(object sender, EventArgs e)
	{
		justconnected = false;
		if (sessions.Count == 0)
		{
			MessageBox.Show("No session found in the device");
			return;
		}
		autoSessionsID = new List<uint>();
		foreach (uint s in sessions.Keys)
		{
			autoSessionsID.Add(s);
		}
		autoSessionsID.Sort();
		autoLoadIndex = -1;
		LoadNextSession();
	}

	private void LoadNextSession()
	{
		autoLoadIndex++;
		if (autoLoadIndex >= autoSessionsID.Count)
		{
			MessageBox.Show("Caricamento terminato");
			autoLoadIndex = -1;
		}
		else if (((Session)sessions[autoSessionsID[autoLoadIndex]]).sessionSize.getSize() > 100)
		{
			log.WarnFormat("Autoreading session at index {0}, id {1}", autoLoadIndex, autoSessionsID[autoLoadIndex]);
			SendSessionRequest(autoSessionsID[autoLoadIndex], 0uL, tryingtorecover: false);
		}
		else
		{
			log.WarnFormat("Skipping session at index {0}, id {1}", autoLoadIndex, autoSessionsID[autoLoadIndex]);
			LoadNextSession();
		}
	}

	private void ResetModuloToolStripMenuItem_Click_1(object sender, EventArgs e)
	{
		if (MessageBox.Show("Are you sure?", "Device reboot", MessageBoxButtons.YesNo) == DialogResult.Yes)
		{
			SendMicroReset();
			setConnectedButtons(connected: false);
			aggiornaPorte();
		}
	}

	private void MostraCartellaFileLocaliToolStripMenuItem_Click(object sender, EventArgs e)
	{
		OpenFolder(BaseSession.getDBFolder());
	}

	private void OpenFolder(string folderPath)
	{
		if (Directory.Exists(folderPath))
		{
			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				Arguments = folderPath,
				FileName = "explorer.exe"
			};
			Process.Start(startInfo);
		}
		else
		{
			MessageBox.Show($"{folderPath} The folder doesn't exist!");
		}
	}

	private void EliminaFileLocaliToolStripMenuItem1_Click(object sender, EventArgs e)
	{
		ClearFolder(BaseSession.getDBFolder());
	}

	private void ClearFolder(string FolderName)
	{
		DirectoryInfo dir = new DirectoryInfo(FolderName);
		int good = 0;
		int bad = 0;
		FileInfo[] files = dir.GetFiles("*.session");
		foreach (FileInfo fi in files)
		{
			try
			{
				fi.Delete();
				good++;
			}
			catch
			{
				bad++;
			}
		}
		string testo = ((bad != 0) ? $"{good} file(s) have been deleted with success. An errorr occurred while deleteing {bad} file(s)" : $"{good} file(s) have been deleted");
		MessageBox.Show(testo);
	}

	private void ButtonHighside_Click(object sender, EventArgs e)
	{
		long tagid = getCurrentSession().addTag("highside", eventNote.Text, newEventTime.Value);
		ListViewItem listViewItem1 = new ListViewItem(new string[3]
		{
			newEventTime.Value.ToLongTimeString(),
			"highside",
			eventNote.Text
		}, -1);
		listViewItem1.Tag = tagid;
		tagsListView.Items.Add(listViewItem1);
	}

	private void ButtonScivolamento_Click(object sender, EventArgs e)
	{
		long tagid = getCurrentSession().addTag("lowside", eventNote.Text, newEventTime.Value);
		ListViewItem listViewItem1 = new ListViewItem(new string[3]
		{
			newEventTime.Value.ToLongTimeString(),
			"lowside",
			eventNote.Text
		}, -1);
		listViewItem1.Tag = tagid;
		tagsListView.Items.Add(listViewItem1);
	}

	private void ButtonContatto_Click(object sender, EventArgs e)
	{
		long tagid = getCurrentSession().addTag("contact", eventNote.Text, newEventTime.Value);
		ListViewItem listViewItem1 = new ListViewItem(new string[3]
		{
			newEventTime.Value.ToLongTimeString(),
			"contact",
			eventNote.Text
		}, -1);
		listViewItem1.Tag = tagid;
		tagsListView.Items.Add(listViewItem1);
	}

	private void VerificaStatoSensoriToolStripMenuItem_Click(object sender, EventArgs e)
	{
		byte[] buffer = new byte[3];
		int i = 0;
		buffer[i++] = 6;
		buffer[i++] = 1;
		buffer[i++] = 7;
		SendData(buffer);
	}

	private void installaFirmwareDiTestToolStripMenuItem_Click(object sender, EventArgs e)
	{
		if (MessageBox.Show("Are you sure you want to update the test firmware?\r\nThis operation is dangerous and you should contact support before proceeding", "Device firmware update", MessageBoxButtons.YesNo) == DialogResult.Yes)
		{
			UpdateFirmware(0, null);
		}
	}

	private void installaFirmwareAirbag(object sender, EventArgs e)
	{
		if (MessageBox.Show("Are you sure you want to update the airbag test firmware?\r\nThis operation is dangerous and you should contact support before proceeding", "Device firmware update", MessageBoxButtons.YesNo) == DialogResult.Yes)
		{
			UpdateFirmware(1, null);
		}
	}

	private void installaFirmwareLoad(object sender, EventArgs e)
	{
		if (MessageBox.Show("Are you sure you want to update the firmware with an external file?\r\nThis operation is dangerous and you should contact support before proceeding", "Device firmware update", MessageBoxButtons.YesNo) != DialogResult.No)
		{
			OpenFileDialog openFileDialog1 = new OpenFileDialog();
			openFileDialog1.Filter = "Firmware file (*.bin)|*.bin";
			openFileDialog1.FilterIndex = 1;
			openFileDialog1.CheckFileExists = true;
			openFileDialog1.Multiselect = false;
			if (openFileDialog1.ShowDialog() == DialogResult.OK)
			{
				aggiornaFirmware(openFileDialog1.FileName);
			}
		}
	}

	private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
	{
	}

	private void statusStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
	{
	}

	private void NextActivationButton_Click(object sender, EventArgs e)
	{
		int maxposition = trackTimer.Maximum * progressBarDownload.Value / progressBarDownload.Maximum;
		falls_index++;
		if (falls_index >= critical_falls.Count)
		{
			falls_index = critical_falls.Count - 1;
		}
		if (falls_index == critical_falls.Count - 1 && critical_falls.Count != 1)
		{
			NextActivationButton.Enabled = false;
		}
		if (critical_falls.Count > 1)
		{
			PrevActivationButton.Enabled = true;
		}
		MoveGraphToIndex(((Fall)critical_falls[falls_index]).index);
		trackTimer.Value = Convert.ToInt32((((Fall)critical_falls[falls_index]).index - getCurrentBaseSession().MinIndex) * maxposition / (getCurrentBaseSession().MaxIndex - getCurrentBaseSession().MinIndex));
		toolStripCriticalFall.Text = $"{falls_index + 1}/{critical_falls.Count.ToString()} activations";
	}

	private void PrevActivationButton_Click(object sender, EventArgs e)
	{
		int maxposition = trackTimer.Maximum * progressBarDownload.Value / progressBarDownload.Maximum;
		falls_index--;
		if (falls_index < 0)
		{
			falls_index = 0;
		}
		if (falls_index == 0 && critical_falls.Count != 1)
		{
			PrevActivationButton.Enabled = false;
		}
		if (critical_falls.Count > 1)
		{
			NextActivationButton.Enabled = true;
		}
		MoveGraphToIndex(((Fall)critical_falls[falls_index]).index);
		trackTimer.Value = Convert.ToInt32((((Fall)critical_falls[falls_index]).index - getCurrentBaseSession().MinIndex) * maxposition / (getCurrentBaseSession().MaxIndex - getCurrentBaseSession().MinIndex));
		toolStripCriticalFall.Text = $"{falls_index + 1}/{critical_falls.Count.ToString()} activations";
	}

	private bool IsValidFall(int fall)
	{
		if ((fall & 0xA000000) == 0 && (fall & 0x80000000u) != 0)
		{
			return true;
		}
		if ((fall & 0xA000000) == 167772160 && (fall & 0xF0000000u) == 64424509440L)
		{
			return true;
		}
		return false;
	}

	private bool IsCriticalFall(uint fall, BaseSession s)
	{
		if ((fall & 0xFF000000u) == 2147483648u && !s.isNewCriticalFallFormat())
		{
			return true;
		}
		if ((fall & 0xFF000000u) == 4026531840u && !s.isNewCriticalFallFormat())
		{
			return true;
		}
		if ((fall & 0xFF000000u) == 4194304000u && s.isNewCriticalFallFormat())
		{
			return true;
		}
		return false;
	}

	private void UpdateCriticalFalls()
	{
		critical_falls.Clear();
		falls_index = -1;
		foreach (Fall f in getCurrentBaseSession().falls)
		{
			if (IsCriticalFall((uint)f.fall, getCurrentBaseSession()))
			{
				critical_falls.Add(f);
			}
		}
		if (critical_falls.Count > 0)
		{
			PrevActivationButton.Enabled = false;
			NextActivationButton.Enabled = true;
			toolStripCriticalFall.Text = $"0/{critical_falls.Count.ToString()} activations";
		}
		else
		{
			PrevActivationButton.Enabled = false;
			NextActivationButton.Enabled = false;
			toolStripCriticalFall.Text = "No activation";
		}
	}

	private void trackTimer_QueryAccessibilityHelp(object sender, QueryAccessibilityHelpEventArgs e)
	{
	}

	private void esportaGraficoSuFileToolStripMenuItem_Click(object sender, EventArgs e)
	{
		SaveFileDialog savefile = new SaveFileDialog();
		savefile.FileName = "unknown.csv";
		savefile.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
		BaseSession currentSession;
		try
		{
			currentSession = getCurrentBaseSession();
		}
		catch
		{
			MessageBox.Show("Please, load a session first", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			return;
		}
		MessageBox.Show(currentSession.getTimeForDataIndex((uint)plotView.Model.Axes[0].ActualMinimum).ToString() + " " + currentSession.getTimeForDataIndex((uint)plotView.Model.Axes[0].ActualMaximum).ToString());
		if (savefile.ShowDialog() == DialogResult.OK)
		{
			currentSession.exportFromTo(savefile.FileName, (int)plotView.Model.Axes[0].ActualMinimum, (int)plotView.Model.Axes[0].ActualMaximum);
		}
	}

	private void LeggiITempi_click(object sender, EventArgs e)
	{
		StartScanSessions();
	}

	private void tableLayoutPanel11_Paint(object sender, PaintEventArgs e)
	{
	}

	private void tableLayoutPanel4_Paint(object sender, PaintEventArgs e)
	{
	}

	private void tableLayoutPanel1_Paint_1(object sender, PaintEventArgs e)
	{
	}

	private void toolStripMenuItem3_Click(object sender, EventArgs e)
	{
	}

	private void enterShippingModeToolStripMenuItem_Click(object sender, EventArgs e)
	{
		if (MessageBox.Show("Are you sure?", "Shipping Mode", MessageBoxButtons.YesNo) == DialogResult.Yes)
		{
			SendShippingMode();
			setConnectedButtons(connected: false);
			aggiornaPorte();
		}
	}

	private void trackLabelEnd_Click(object sender, EventArgs e)
	{
	}

	private void magnetometerCalibrationToolStripMenuItem_Click(object sender, EventArgs e)
	{
		MagCalBox mgc = new MagCalBox();
		SendMagnetometerCalibration();
		setConnectedButtons(connected: false);
		aggiornaPorte();
		mgc.ShowDialog();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		this.components = new System.ComponentModel.Container();
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PufferFish.MainForm));
		this.panel1 = new System.Windows.Forms.Panel();
		this.label2 = new System.Windows.Forms.Label();
		this.progressBarDownload = new System.Windows.Forms.ProgressBar();
		this.tagsListView = new System.Windows.Forms.ListView();
		this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
		this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
		this.columnHeader3 = new System.Windows.Forms.ColumnHeader();
		this.listContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
		this.eliminaToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.modificaToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.menuStrip1 = new System.Windows.Forms.MenuStrip();
		this.azioniToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.richiediStatoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.richiediListaSessioniToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
		this.richiediSessioneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripMenuItem6 = new System.Windows.Forms.ToolStripMenuItem();
		this.richiediTutteLeSessioniToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.richiediStopToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.cancellaMemoriaToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.avanzatoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.aggiornaFirmwareToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripMenuItem();
		this.installaFirmwareDiTestToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripMenuItem5 = new System.Windows.Forms.ToolStripMenuItem();
		this.resetModuloToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.magnetometerCalibrationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.enterShippingModeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
		this.verificaStatoSensoriToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
		this.mostraCartellaFileLocaliToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.eliminaFileLocaliToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
		this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.caricaSessioneDaFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.esportaGraficoSuFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.trackTimer = new System.Windows.Forms.TrackBar();
		this.panelTags = new System.Windows.Forms.GroupBox();
		this.tableLayoutPanel9 = new System.Windows.Forms.TableLayoutPanel();
		this.tableLayoutPanel7 = new System.Windows.Forms.TableLayoutPanel();
		this.buttonInsertTag = new System.Windows.Forms.Button();
		this.buttonScivolamento = new System.Windows.Forms.Button();
		this.buttonContatto = new System.Windows.Forms.Button();
		this.tableLayoutPanel8 = new System.Windows.Forms.TableLayoutPanel();
		this.eventNote = new System.Windows.Forms.TextBox();
		this.label4 = new System.Windows.Forms.Label();
		this.newEventTime = new System.Windows.Forms.DateTimePicker();
		this.gMapControl = new GMap.NET.WindowsForms.GMapControl();
		this.buttonTerminateEditing = new System.Windows.Forms.Button();
		this.comboPorts = new System.Windows.Forms.ComboBox();
		this.btnConnect = new System.Windows.Forms.Button();
		this.btnDisconnect = new System.Windows.Forms.Button();
		this.statusStrip = new System.Windows.Forms.StatusStrip();
		this.toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
		this.toolStripStatusLabelSpeed = new System.Windows.Forms.ToolStripStatusLabel();
		this.toolStripTotSession = new System.Windows.Forms.ToolStripStatusLabel();
		this.toolStripCurrentSession = new System.Windows.Forms.ToolStripStatusLabel();
		this.toolStripCriticalFall = new System.Windows.Forms.ToolStripStatusLabel();
		this.toolStripMemory = new System.Windows.Forms.ToolStripStatusLabel();
		this.toolStripVersionStatus = new System.Windows.Forms.ToolStripStatusLabel();
		this.toolStripStatusUpload = new System.Windows.Forms.ToolStripStatusLabel();
		this.toolStripProgressBarUpload = new System.Windows.Forms.ToolStripProgressBar();
		this.buttonSaveToFile = new System.Windows.Forms.Button();
		this.updatePortsButton = new System.Windows.Forms.Button();
		this.button2 = new System.Windows.Forms.Button();
		this.button3 = new System.Windows.Forms.Button();
		this.stopReadingButton = new System.Windows.Forms.Button();
		this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
		this.PrevActivationButton = new System.Windows.Forms.Button();
		this.NextActivationButton = new System.Windows.Forms.Button();
		this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
		this.pictureBox1 = new System.Windows.Forms.PictureBox();
		this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
		this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
		this.trackLabelCurrent = new System.Windows.Forms.Label();
		this.trackLabelEnd = new System.Windows.Forms.Label();
		this.trackLabelStart = new System.Windows.Forms.Label();
		this.plotView = new OxyPlot.WindowsForms.PlotView();
		this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
		this.tableLayoutPanel5 = new System.Windows.Forms.TableLayoutPanel();
		this.tableLayoutPanel6 = new System.Windows.Forms.TableLayoutPanel();
		this.tableLayoutPanel10 = new System.Windows.Forms.TableLayoutPanel();
		this.tableLayoutPanel11 = new System.Windows.Forms.TableLayoutPanel();
		this.panel1.SuspendLayout();
		this.listContextMenu.SuspendLayout();
		this.menuStrip1.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.trackTimer).BeginInit();
		this.panelTags.SuspendLayout();
		this.tableLayoutPanel9.SuspendLayout();
		this.tableLayoutPanel7.SuspendLayout();
		this.tableLayoutPanel8.SuspendLayout();
		this.statusStrip.SuspendLayout();
		this.tableLayoutPanel1.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.pictureBox1).BeginInit();
		this.tableLayoutPanel3.SuspendLayout();
		this.tableLayoutPanel2.SuspendLayout();
		this.tableLayoutPanel4.SuspendLayout();
		this.tableLayoutPanel5.SuspendLayout();
		this.tableLayoutPanel6.SuspendLayout();
		this.tableLayoutPanel10.SuspendLayout();
		this.tableLayoutPanel11.SuspendLayout();
		base.SuspendLayout();
		this.panel1.Controls.Add(this.label2);
		this.panel1.Controls.Add(this.progressBarDownload);
		this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panel1.Location = new System.Drawing.Point(9, 858);
		this.panel1.Margin = new System.Windows.Forms.Padding(9, 8, 9, 8);
		this.panel1.Name = "panel1";
		this.panel1.Size = new System.Drawing.Size(1858, 119);
		this.panel1.TabIndex = 0;
		this.label2.Location = new System.Drawing.Point(9, 21);
		this.label2.Margin = new System.Windows.Forms.Padding(9, 0, 9, 0);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(352, 72);
		this.label2.TabIndex = 3;
		this.label2.Text = "Download from the device";
		this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.progressBarDownload.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.progressBarDownload.Location = new System.Drawing.Point(362, 36);
		this.progressBarDownload.Margin = new System.Windows.Forms.Padding(9, 8, 9, 8);
		this.progressBarDownload.Maximum = 1000;
		this.progressBarDownload.Name = "progressBarDownload";
		this.progressBarDownload.Size = new System.Drawing.Size(1490, 41);
		this.progressBarDownload.TabIndex = 2;
		this.tagsListView.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.tagsListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[3] { this.columnHeader1, this.columnHeader2, this.columnHeader3 });
		this.tagsListView.ContextMenuStrip = this.listContextMenu;
		this.tagsListView.FullRowSelect = true;
		this.tagsListView.HideSelection = false;
		this.tagsListView.Location = new System.Drawing.Point(9, 830);
		this.tagsListView.Margin = new System.Windows.Forms.Padding(9, 8, 9, 8);
		this.tagsListView.Name = "tagsListView";
		this.tagsListView.Size = new System.Drawing.Size(779, 147);
		this.tagsListView.TabIndex = 1;
		this.tagsListView.UseCompatibleStateImageBehavior = false;
		this.tagsListView.View = System.Windows.Forms.View.Details;
		this.columnHeader1.Text = "Time";
		this.columnHeader1.Width = 90;
		this.columnHeader2.Text = "Type";
		this.columnHeader2.Width = 114;
		this.columnHeader3.Text = "Note";
		this.columnHeader3.Width = 286;
		this.listContextMenu.ImageScalingSize = new System.Drawing.Size(20, 20);
		this.listContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[2] { this.eliminaToolStripMenuItem, this.modificaToolStripMenuItem });
		this.listContextMenu.Name = "listContextMenu";
		this.listContextMenu.Size = new System.Drawing.Size(212, 100);
		this.eliminaToolStripMenuItem.Name = "eliminaToolStripMenuItem";
		this.eliminaToolStripMenuItem.Size = new System.Drawing.Size(211, 48);
		this.eliminaToolStripMenuItem.Text = "Elimina";
		this.modificaToolStripMenuItem.Name = "modificaToolStripMenuItem";
		this.modificaToolStripMenuItem.Size = new System.Drawing.Size(211, 48);
		this.modificaToolStripMenuItem.Text = "Modifica";
		this.menuStrip1.GripMargin = new System.Windows.Forms.Padding(2, 2, 0, 2);
		this.menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
		this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[3] { this.azioniToolStripMenuItem, this.avanzatoToolStripMenuItem, this.fileToolStripMenuItem });
		this.menuStrip1.Location = new System.Drawing.Point(0, 0);
		this.menuStrip1.Name = "menuStrip1";
		this.menuStrip1.Padding = new System.Windows.Forms.Padding(10, 5, 0, 5);
		this.menuStrip1.Size = new System.Drawing.Size(2697, 55);
		this.menuStrip1.TabIndex = 3;
		this.menuStrip1.Text = "menuStrip1";
		this.azioniToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[8] { this.richiediStatoToolStripMenuItem, this.richiediListaSessioniToolStripMenuItem, this.toolStripMenuItem1, this.richiediSessioneToolStripMenuItem, this.toolStripMenuItem6, this.richiediTutteLeSessioniToolStripMenuItem, this.richiediStopToolStripMenuItem, this.cancellaMemoriaToolStripMenuItem });
		this.azioniToolStripMenuItem.Enabled = false;
		this.azioniToolStripMenuItem.Name = "azioniToolStripMenuItem";
		this.azioniToolStripMenuItem.Size = new System.Drawing.Size(140, 45);
		this.azioniToolStripMenuItem.Text = "Actions";
		this.richiediStatoToolStripMenuItem.Name = "richiediStatoToolStripMenuItem";
		this.richiediStatoToolStripMenuItem.Size = new System.Drawing.Size(481, 54);
		this.richiediStatoToolStripMenuItem.Text = "Read device status";
		this.richiediStatoToolStripMenuItem.Click += new System.EventHandler(RichiediStatoToolStripMenuItem_Click);
		this.richiediListaSessioniToolStripMenuItem.Name = "richiediListaSessioniToolStripMenuItem";
		this.richiediListaSessioniToolStripMenuItem.Size = new System.Drawing.Size(481, 54);
		this.richiediListaSessioniToolStripMenuItem.Text = "Read sessions list";
		this.richiediListaSessioniToolStripMenuItem.Click += new System.EventHandler(RichiediListaSessioniToolStripMenuItem_Click);
		this.toolStripMenuItem1.Name = "toolStripMenuItem1";
		this.toolStripMenuItem1.Size = new System.Drawing.Size(478, 6);
		this.richiediSessioneToolStripMenuItem.Enabled = false;
		this.richiediSessioneToolStripMenuItem.Name = "richiediSessioneToolStripMenuItem";
		this.richiediSessioneToolStripMenuItem.Size = new System.Drawing.Size(481, 54);
		this.richiediSessioneToolStripMenuItem.Text = "Read a session";
		this.richiediSessioneToolStripMenuItem.Click += new System.EventHandler(RichiediSessioneToolStripMenuItem_Click);
		this.toolStripMenuItem6.Enabled = false;
		this.toolStripMenuItem6.Name = "toolStripMenuItem6";
		this.toolStripMenuItem6.Size = new System.Drawing.Size(481, 54);
		this.toolStripMenuItem6.Text = "Read sessions headers";
		this.toolStripMenuItem6.Visible = false;
		this.toolStripMenuItem6.Click += new System.EventHandler(LeggiITempi_click);
		this.richiediTutteLeSessioniToolStripMenuItem.Enabled = false;
		this.richiediTutteLeSessioniToolStripMenuItem.Name = "richiediTutteLeSessioniToolStripMenuItem";
		this.richiediTutteLeSessioniToolStripMenuItem.Size = new System.Drawing.Size(481, 54);
		this.richiediTutteLeSessioniToolStripMenuItem.Text = "Read all sessions";
		this.richiediTutteLeSessioniToolStripMenuItem.Click += new System.EventHandler(RichiediTutteLeSessioniToolStripMenuItem_Click);
		this.richiediStopToolStripMenuItem.Name = "richiediStopToolStripMenuItem";
		this.richiediStopToolStripMenuItem.Size = new System.Drawing.Size(481, 54);
		this.richiediStopToolStripMenuItem.Text = "Stop data reading";
		this.richiediStopToolStripMenuItem.Click += new System.EventHandler(RichiediStopToolStripMenuItem_Click);
		this.cancellaMemoriaToolStripMenuItem.Name = "cancellaMemoriaToolStripMenuItem";
		this.cancellaMemoriaToolStripMenuItem.Size = new System.Drawing.Size(481, 54);
		this.cancellaMemoriaToolStripMenuItem.Text = "Erase device memory";
		this.cancellaMemoriaToolStripMenuItem.Click += new System.EventHandler(OnDeleteMemoryClick);
		this.avanzatoToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[12]
		{
			this.aggiornaFirmwareToolStripMenuItem, this.toolStripMenuItem4, this.installaFirmwareDiTestToolStripMenuItem, this.toolStripMenuItem5, this.resetModuloToolStripMenuItem, this.magnetometerCalibrationToolStripMenuItem, this.enterShippingModeToolStripMenuItem, this.toolStripMenuItem3, this.verificaStatoSensoriToolStripMenuItem, this.toolStripMenuItem2,
			this.mostraCartellaFileLocaliToolStripMenuItem, this.eliminaFileLocaliToolStripMenuItem1
		});
		this.avanzatoToolStripMenuItem.Name = "avanzatoToolStripMenuItem";
		this.avanzatoToolStripMenuItem.Size = new System.Drawing.Size(173, 45);
		this.avanzatoToolStripMenuItem.Text = "Advanced";
		this.aggiornaFirmwareToolStripMenuItem.Name = "aggiornaFirmwareToolStripMenuItem";
		this.aggiornaFirmwareToolStripMenuItem.Size = new System.Drawing.Size(750, 54);
		this.aggiornaFirmwareToolStripMenuItem.Text = "Install latest firmware";
		this.aggiornaFirmwareToolStripMenuItem.Click += new System.EventHandler(aggiornaFirmwareToolStripMenuItem_Click);
		this.toolStripMenuItem4.Name = "toolStripMenuItem4";
		this.toolStripMenuItem4.Size = new System.Drawing.Size(750, 54);
		this.toolStripMenuItem4.Text = "Install latest firmware NO Airbag Activation";
		this.toolStripMenuItem4.Click += new System.EventHandler(installaFirmwareAirbag);
		this.installaFirmwareDiTestToolStripMenuItem.Name = "installaFirmwareDiTestToolStripMenuItem";
		this.installaFirmwareDiTestToolStripMenuItem.Size = new System.Drawing.Size(750, 54);
		this.installaFirmwareDiTestToolStripMenuItem.Text = "Install latest Debug firmware";
		this.installaFirmwareDiTestToolStripMenuItem.Click += new System.EventHandler(installaFirmwareDiTestToolStripMenuItem_Click);
		this.toolStripMenuItem5.Name = "toolStripMenuItem5";
		this.toolStripMenuItem5.Size = new System.Drawing.Size(750, 54);
		this.toolStripMenuItem5.Text = "Install firmware from file";
		this.toolStripMenuItem5.Click += new System.EventHandler(installaFirmwareLoad);
		this.resetModuloToolStripMenuItem.Enabled = false;
		this.resetModuloToolStripMenuItem.Name = "resetModuloToolStripMenuItem";
		this.resetModuloToolStripMenuItem.Size = new System.Drawing.Size(750, 54);
		this.resetModuloToolStripMenuItem.Text = "Reboot device";
		this.resetModuloToolStripMenuItem.Click += new System.EventHandler(ResetModuloToolStripMenuItem_Click_1);
		this.magnetometerCalibrationToolStripMenuItem.Enabled = false;
		this.magnetometerCalibrationToolStripMenuItem.Name = "magnetometerCalibrationToolStripMenuItem";
		this.magnetometerCalibrationToolStripMenuItem.Size = new System.Drawing.Size(750, 54);
		this.magnetometerCalibrationToolStripMenuItem.Text = "Magnetometer calibration";
		this.magnetometerCalibrationToolStripMenuItem.Click += new System.EventHandler(magnetometerCalibrationToolStripMenuItem_Click);
		this.enterShippingModeToolStripMenuItem.Enabled = false;
		this.enterShippingModeToolStripMenuItem.Name = "enterShippingModeToolStripMenuItem";
		this.enterShippingModeToolStripMenuItem.Size = new System.Drawing.Size(750, 54);
		this.enterShippingModeToolStripMenuItem.Text = "Enter Shipping Mode";
		this.enterShippingModeToolStripMenuItem.Click += new System.EventHandler(enterShippingModeToolStripMenuItem_Click);
		this.toolStripMenuItem3.Name = "toolStripMenuItem3";
		this.toolStripMenuItem3.Size = new System.Drawing.Size(747, 6);
		this.toolStripMenuItem3.Click += new System.EventHandler(toolStripMenuItem3_Click);
		this.verificaStatoSensoriToolStripMenuItem.Name = "verificaStatoSensoriToolStripMenuItem";
		this.verificaStatoSensoriToolStripMenuItem.Size = new System.Drawing.Size(750, 54);
		this.verificaStatoSensoriToolStripMenuItem.Text = "Check sensors";
		this.verificaStatoSensoriToolStripMenuItem.Click += new System.EventHandler(VerificaStatoSensoriToolStripMenuItem_Click);
		this.toolStripMenuItem2.Name = "toolStripMenuItem2";
		this.toolStripMenuItem2.Size = new System.Drawing.Size(747, 6);
		this.mostraCartellaFileLocaliToolStripMenuItem.Name = "mostraCartellaFileLocaliToolStripMenuItem";
		this.mostraCartellaFileLocaliToolStripMenuItem.Size = new System.Drawing.Size(750, 54);
		this.mostraCartellaFileLocaliToolStripMenuItem.Text = "Open local folder";
		this.mostraCartellaFileLocaliToolStripMenuItem.Click += new System.EventHandler(MostraCartellaFileLocaliToolStripMenuItem_Click);
		this.eliminaFileLocaliToolStripMenuItem1.Name = "eliminaFileLocaliToolStripMenuItem1";
		this.eliminaFileLocaliToolStripMenuItem1.Size = new System.Drawing.Size(750, 54);
		this.eliminaFileLocaliToolStripMenuItem1.Text = "Delete local folder";
		this.eliminaFileLocaliToolStripMenuItem1.Click += new System.EventHandler(EliminaFileLocaliToolStripMenuItem1_Click);
		this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[2] { this.caricaSessioneDaFileToolStripMenuItem, this.esportaGraficoSuFileToolStripMenuItem });
		this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
		this.fileToolStripMenuItem.Size = new System.Drawing.Size(297, 45);
		this.fileToolStripMenuItem.Text = "Load track from file";
		this.caricaSessioneDaFileToolStripMenuItem.Name = "caricaSessioneDaFileToolStripMenuItem";
		this.caricaSessioneDaFileToolStripMenuItem.Size = new System.Drawing.Size(462, 54);
		this.caricaSessioneDaFileToolStripMenuItem.Text = "Load track from a file";
		this.caricaSessioneDaFileToolStripMenuItem.Click += new System.EventHandler(OnLoadSessionFromFileClick);
		this.esportaGraficoSuFileToolStripMenuItem.Name = "esportaGraficoSuFileToolStripMenuItem";
		this.esportaGraficoSuFileToolStripMenuItem.Size = new System.Drawing.Size(462, 54);
		this.esportaGraficoSuFileToolStripMenuItem.Text = "Export graph to file";
		this.esportaGraficoSuFileToolStripMenuItem.Click += new System.EventHandler(esportaGraficoSuFileToolStripMenuItem_Click);
		this.trackTimer.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.trackTimer.LargeChange = 50;
		this.trackTimer.Location = new System.Drawing.Point(130, 8);
		this.trackTimer.Margin = new System.Windows.Forms.Padding(9, 8, 9, 8);
		this.trackTimer.Maximum = 30000;
		this.trackTimer.Name = "trackTimer";
		this.trackTimer.Size = new System.Drawing.Size(1604, 62);
		this.trackTimer.TabIndex = 4;
		this.trackTimer.TickFrequency = 100;
		this.trackTimer.Scroll += new System.EventHandler(OnTrackScroll);
		this.trackTimer.QueryAccessibilityHelp += new System.Windows.Forms.QueryAccessibilityHelpEventHandler(trackTimer_QueryAccessibilityHelp);
		this.panelTags.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.panelTags.Controls.Add(this.tableLayoutPanel9);
		this.panelTags.Enabled = false;
		this.panelTags.Location = new System.Drawing.Point(9, 592);
		this.panelTags.Margin = new System.Windows.Forms.Padding(9, 8, 9, 8);
		this.panelTags.Name = "panelTags";
		this.panelTags.Padding = new System.Windows.Forms.Padding(9, 8, 9, 8);
		this.panelTags.Size = new System.Drawing.Size(779, 222);
		this.panelTags.TabIndex = 6;
		this.panelTags.TabStop = false;
		this.panelTags.Text = " ";
		this.tableLayoutPanel9.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.tableLayoutPanel9.ColumnCount = 1;
		this.tableLayoutPanel9.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50f));
		this.tableLayoutPanel9.Controls.Add(this.tableLayoutPanel7, 0, 1);
		this.tableLayoutPanel9.Controls.Add(this.tableLayoutPanel8, 0, 0);
		this.tableLayoutPanel9.Location = new System.Drawing.Point(10, 23);
		this.tableLayoutPanel9.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
		this.tableLayoutPanel9.Name = "tableLayoutPanel9";
		this.tableLayoutPanel9.RowCount = 2;
		this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50f));
		this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50f));
		this.tableLayoutPanel9.Size = new System.Drawing.Size(758, 191);
		this.tableLayoutPanel9.TabIndex = 31;
		this.tableLayoutPanel7.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.tableLayoutPanel7.ColumnCount = 3;
		this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333f));
		this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333f));
		this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333f));
		this.tableLayoutPanel7.Controls.Add(this.buttonInsertTag, 0, 0);
		this.tableLayoutPanel7.Controls.Add(this.buttonScivolamento, 1, 0);
		this.tableLayoutPanel7.Controls.Add(this.buttonContatto, 2, 0);
		this.tableLayoutPanel7.Location = new System.Drawing.Point(6, 100);
		this.tableLayoutPanel7.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
		this.tableLayoutPanel7.MaximumSize = new System.Drawing.Size(0, 372);
		this.tableLayoutPanel7.Name = "tableLayoutPanel7";
		this.tableLayoutPanel7.RowCount = 1;
		this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this.tableLayoutPanel7.Size = new System.Drawing.Size(746, 85);
		this.tableLayoutPanel7.TabIndex = 31;
		this.buttonInsertTag.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.buttonInsertTag.Location = new System.Drawing.Point(9, 8);
		this.buttonInsertTag.Margin = new System.Windows.Forms.Padding(9, 8, 9, 8);
		this.buttonInsertTag.Name = "buttonInsertTag";
		this.buttonInsertTag.Size = new System.Drawing.Size(230, 69);
		this.buttonInsertTag.TabIndex = 6;
		this.buttonInsertTag.Text = "High Side";
		this.buttonInsertTag.UseVisualStyleBackColor = true;
		this.buttonInsertTag.Click += new System.EventHandler(ButtonHighside_Click);
		this.buttonScivolamento.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.buttonScivolamento.Location = new System.Drawing.Point(257, 8);
		this.buttonScivolamento.Margin = new System.Windows.Forms.Padding(9, 8, 9, 8);
		this.buttonScivolamento.Name = "buttonScivolamento";
		this.buttonScivolamento.Size = new System.Drawing.Size(230, 69);
		this.buttonScivolamento.TabIndex = 6;
		this.buttonScivolamento.Text = "Low Side";
		this.buttonScivolamento.UseVisualStyleBackColor = true;
		this.buttonScivolamento.Click += new System.EventHandler(ButtonScivolamento_Click);
		this.buttonContatto.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.buttonContatto.Location = new System.Drawing.Point(505, 8);
		this.buttonContatto.Margin = new System.Windows.Forms.Padding(9, 8, 9, 8);
		this.buttonContatto.Name = "buttonContatto";
		this.buttonContatto.Size = new System.Drawing.Size(232, 69);
		this.buttonContatto.TabIndex = 6;
		this.buttonContatto.Text = "Other";
		this.buttonContatto.UseVisualStyleBackColor = true;
		this.buttonContatto.Click += new System.EventHandler(ButtonContatto_Click);
		this.tableLayoutPanel8.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.tableLayoutPanel8.ColumnCount = 3;
		this.tableLayoutPanel8.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333f));
		this.tableLayoutPanel8.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333f));
		this.tableLayoutPanel8.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333f));
		this.tableLayoutPanel8.Controls.Add(this.eventNote, 2, 0);
		this.tableLayoutPanel8.Controls.Add(this.label4, 0, 0);
		this.tableLayoutPanel8.Controls.Add(this.newEventTime, 1, 0);
		this.tableLayoutPanel8.Location = new System.Drawing.Point(6, 5);
		this.tableLayoutPanel8.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
		this.tableLayoutPanel8.MinimumSize = new System.Drawing.Size(0, 85);
		this.tableLayoutPanel8.Name = "tableLayoutPanel8";
		this.tableLayoutPanel8.RowCount = 1;
		this.tableLayoutPanel8.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this.tableLayoutPanel8.Size = new System.Drawing.Size(746, 85);
		this.tableLayoutPanel8.TabIndex = 31;
		this.eventNote.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.eventNote.Location = new System.Drawing.Point(505, 23);
		this.eventNote.Margin = new System.Windows.Forms.Padding(9, 8, 9, 8);
		this.eventNote.Name = "eventNote";
		this.eventNote.Size = new System.Drawing.Size(232, 38);
		this.eventNote.TabIndex = 7;
		this.label4.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.label4.AutoSize = true;
		this.label4.Location = new System.Drawing.Point(9, 0);
		this.label4.Margin = new System.Windows.Forms.Padding(9, 0, 9, 0);
		this.label4.Name = "label4";
		this.label4.Size = new System.Drawing.Size(230, 85);
		this.label4.TabIndex = 1;
		this.label4.Text = "Orario:";
		this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
		this.newEventTime.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.newEventTime.Format = System.Windows.Forms.DateTimePickerFormat.Time;
		this.newEventTime.Location = new System.Drawing.Point(257, 23);
		this.newEventTime.Margin = new System.Windows.Forms.Padding(9, 8, 9, 8);
		this.newEventTime.MaxDate = new System.DateTime(2100, 12, 31, 0, 0, 0, 0);
		this.newEventTime.MinDate = new System.DateTime(2016, 1, 1, 0, 0, 0, 0);
		this.newEventTime.Name = "newEventTime";
		this.newEventTime.Size = new System.Drawing.Size(230, 38);
		this.newEventTime.TabIndex = 0;
		this.newEventTime.Value = new System.DateTime(2016, 1, 1, 0, 0, 0, 0);
		this.gMapControl.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.gMapControl.Bearing = 0f;
		this.gMapControl.CanDragMap = true;
		this.gMapControl.EmptyTileColor = System.Drawing.Color.Navy;
		this.gMapControl.GrayScaleMode = false;
		this.gMapControl.HelperLineOption = GMap.NET.WindowsForms.HelperLineOptions.DontShow;
		this.gMapControl.LevelsKeepInMemmory = 5;
		this.gMapControl.Location = new System.Drawing.Point(86, 8);
		this.gMapControl.Margin = new System.Windows.Forms.Padding(9, 8, 9, 8);
		this.gMapControl.MarkersEnabled = true;
		this.gMapControl.MaxZoom = 2;
		this.gMapControl.MinZoom = 2;
		this.gMapControl.MouseWheelZoomEnabled = true;
		this.gMapControl.MouseWheelZoomType = GMap.NET.MouseWheelZoomType.MousePositionWithoutCenter;
		this.gMapControl.Name = "gMapControl";
		this.gMapControl.NegativeMode = false;
		this.gMapControl.PolygonsEnabled = true;
		this.gMapControl.RetryLoadTile = 0;
		this.gMapControl.RoutesEnabled = true;
		this.gMapControl.ScaleMode = GMap.NET.WindowsForms.ScaleModes.Fractional;
		this.gMapControl.SelectedAreaFillColor = System.Drawing.Color.FromArgb(33, 65, 105, 225);
		this.gMapControl.ShowTileGridLines = false;
		this.gMapControl.Size = new System.Drawing.Size(600, 326);
		this.gMapControl.TabIndex = 7;
		this.gMapControl.Zoom = 0.0;
		this.gMapControl.Resize += new System.EventHandler(gMapControl_Resize);
		this.buttonTerminateEditing.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.buttonTerminateEditing.Enabled = false;
		this.buttonTerminateEditing.Location = new System.Drawing.Point(9, 8);
		this.buttonTerminateEditing.Margin = new System.Windows.Forms.Padding(9, 8, 9, 8);
		this.buttonTerminateEditing.Name = "buttonTerminateEditing";
		this.buttonTerminateEditing.Size = new System.Drawing.Size(374, 69);
		this.buttonTerminateEditing.TabIndex = 8;
		this.buttonTerminateEditing.Text = "Mark tagging as completed";
		this.buttonTerminateEditing.UseVisualStyleBackColor = true;
		this.buttonTerminateEditing.Click += new System.EventHandler(endEditing_Click);
		this.comboPorts.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.comboPorts.FormattingEnabled = true;
		this.comboPorts.Location = new System.Drawing.Point(205, 17);
		this.comboPorts.Margin = new System.Windows.Forms.Padding(9, 8, 9, 8);
		this.comboPorts.Name = "comboPorts";
		this.comboPorts.Size = new System.Drawing.Size(178, 39);
		this.comboPorts.TabIndex = 11;
		this.comboPorts.Text = "COM11";
		this.btnConnect.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.btnConnect.Location = new System.Drawing.Point(401, 8);
		this.btnConnect.Margin = new System.Windows.Forms.Padding(9, 8, 9, 8);
		this.btnConnect.Name = "btnConnect";
		this.btnConnect.Size = new System.Drawing.Size(178, 59);
		this.btnConnect.TabIndex = 12;
		this.btnConnect.Text = "Connect";
		this.btnConnect.UseVisualStyleBackColor = true;
		this.btnConnect.Click += new System.EventHandler(BtnConnect_Click);
		this.btnDisconnect.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.btnDisconnect.Enabled = false;
		this.btnDisconnect.Location = new System.Drawing.Point(597, 8);
		this.btnDisconnect.Margin = new System.Windows.Forms.Padding(9, 8, 9, 8);
		this.btnDisconnect.Name = "btnDisconnect";
		this.btnDisconnect.Size = new System.Drawing.Size(179, 59);
		this.btnDisconnect.TabIndex = 14;
		this.btnDisconnect.Text = "Disconnect";
		this.btnDisconnect.UseVisualStyleBackColor = true;
		this.btnDisconnect.Click += new System.EventHandler(btnDisconnect_Click);
		this.statusStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
		this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[7] { this.toolStripStatusLabel, this.toolStripStatusLabelSpeed, this.toolStripTotSession, this.toolStripCurrentSession, this.toolStripCriticalFall, this.toolStripMemory, this.toolStripVersionStatus });
		this.statusStrip.Location = new System.Drawing.Point(0, 1050);
		this.statusStrip.Name = "statusStrip";
		this.statusStrip.Padding = new System.Windows.Forms.Padding(6, 0, 38, 0);
		this.statusStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
		this.statusStrip.Size = new System.Drawing.Size(2697, 58);
		this.statusStrip.TabIndex = 15;
		this.statusStrip.Text = "statusStrip1";
		this.statusStrip.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(statusStrip_ItemClicked);
		this.toolStripStatusLabel.Name = "toolStripStatusLabel";
		this.toolStripStatusLabel.Size = new System.Drawing.Size(84, 45);
		this.toolStripStatusLabel.Text = "State";
		this.toolStripStatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
		this.toolStripStatusLabelSpeed.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
		this.toolStripStatusLabelSpeed.Name = "toolStripStatusLabelSpeed";
		this.toolStripStatusLabelSpeed.Size = new System.Drawing.Size(304, 45);
		this.toolStripStatusLabelSpeed.Text = "Downloading: -- kB/s";
		this.toolStripStatusLabelSpeed.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
		this.toolStripTotSession.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
		this.toolStripTotSession.Name = "toolStripTotSession";
		this.toolStripTotSession.Size = new System.Drawing.Size(269, 45);
		this.toolStripTotSession.Text = "Empty sessions list";
		this.toolStripCurrentSession.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
		this.toolStripCurrentSession.Name = "toolStripCurrentSession";
		this.toolStripCurrentSession.Size = new System.Drawing.Size(285, 45);
		this.toolStripCurrentSession.Text = "No session selected";
		this.toolStripCriticalFall.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
		this.toolStripCriticalFall.Name = "toolStripCriticalFall";
		this.toolStripCriticalFall.Size = new System.Drawing.Size(156, 45);
		this.toolStripCriticalFall.Text = "No events";
		this.toolStripMemory.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
		this.toolStripMemory.Name = "toolStripMemory";
		this.toolStripMemory.Size = new System.Drawing.Size(248, 45);
		this.toolStripMemory.Text = "Used memory: --";
		this.toolStripMemory.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
		this.toolStripVersionStatus.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
		this.toolStripVersionStatus.Name = "toolStripVersionStatus";
		this.toolStripVersionStatus.Size = new System.Drawing.Size(158, 45);
		this.toolStripVersionStatus.Text = "ID: 0 v: 0.0";
		this.toolStripStatusUpload.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
		this.toolStripStatusUpload.Margin = new System.Windows.Forms.Padding(0, 3, 0, 2);
		this.toolStripStatusUpload.Name = "toolStripStatusUpload";
		this.toolStripStatusUpload.Size = new System.Drawing.Size(193, 37);
		this.toolStripStatusUpload.Text = "0 file da caricare";
		this.toolStripProgressBarUpload.Margin = new System.Windows.Forms.Padding(1, 2, 1, 1);
		this.toolStripProgressBarUpload.Name = "toolStripProgressBarUpload";
		this.toolStripProgressBarUpload.Size = new System.Drawing.Size(200, 36);
		this.toolStripProgressBarUpload.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
		this.buttonSaveToFile.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.buttonSaveToFile.Enabled = false;
		this.buttonSaveToFile.Location = new System.Drawing.Point(401, 8);
		this.buttonSaveToFile.Margin = new System.Windows.Forms.Padding(9, 8, 9, 8);
		this.buttonSaveToFile.MinimumSize = new System.Drawing.Size(0, 23);
		this.buttonSaveToFile.Name = "buttonSaveToFile";
		this.buttonSaveToFile.Size = new System.Drawing.Size(375, 69);
		this.buttonSaveToFile.TabIndex = 18;
		this.buttonSaveToFile.Text = "Save track on file";
		this.buttonSaveToFile.UseVisualStyleBackColor = true;
		this.buttonSaveToFile.Click += new System.EventHandler(OnSaveToFileClick);
		this.updatePortsButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.updatePortsButton.Location = new System.Drawing.Point(9, 8);
		this.updatePortsButton.Margin = new System.Windows.Forms.Padding(9, 8, 9, 8);
		this.updatePortsButton.Name = "updatePortsButton";
		this.updatePortsButton.Size = new System.Drawing.Size(178, 59);
		this.updatePortsButton.TabIndex = 20;
		this.updatePortsButton.Text = "Update ports";
		this.updatePortsButton.UseVisualStyleBackColor = true;
		this.updatePortsButton.Click += new System.EventHandler(button1_Click);
		this.button2.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom;
		this.button2.AutoSize = true;
		this.button2.Location = new System.Drawing.Point(0, 0);
		this.button2.Margin = new System.Windows.Forms.Padding(0);
		this.button2.Name = "button2";
		this.button2.Size = new System.Drawing.Size(77, 342);
		this.button2.TabIndex = 21;
		this.button2.Text = "-";
		this.button2.UseVisualStyleBackColor = true;
		this.button2.Click += new System.EventHandler(OnZoomOutClick);
		this.button3.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom;
		this.button3.AutoSize = true;
		this.button3.Location = new System.Drawing.Point(695, 0);
		this.button3.Margin = new System.Windows.Forms.Padding(0);
		this.button3.Name = "button3";
		this.button3.Size = new System.Drawing.Size(90, 342);
		this.button3.TabIndex = 22;
		this.button3.Text = "+";
		this.button3.UseVisualStyleBackColor = true;
		this.button3.Click += new System.EventHandler(OnZoomInClick);
		this.stopReadingButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.stopReadingButton.Enabled = false;
		this.stopReadingButton.Location = new System.Drawing.Point(9, 93);
		this.stopReadingButton.Margin = new System.Windows.Forms.Padding(9, 8, 9, 8);
		this.stopReadingButton.Name = "stopReadingButton";
		this.stopReadingButton.Size = new System.Drawing.Size(779, 69);
		this.stopReadingButton.TabIndex = 23;
		this.stopReadingButton.Text = "Stop session reading";
		this.stopReadingButton.UseVisualStyleBackColor = true;
		this.stopReadingButton.Click += new System.EventHandler(OnStopDataClick);
		this.backgroundWorker1.DoWork += new System.ComponentModel.DoWorkEventHandler(LoadFile_DoWork);
		this.backgroundWorker1.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(loadFile_Completed);
		this.PrevActivationButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.PrevActivationButton.Enabled = false;
		this.PrevActivationButton.Location = new System.Drawing.Point(6, 5);
		this.PrevActivationButton.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
		this.PrevActivationButton.Name = "PrevActivationButton";
		this.PrevActivationButton.Size = new System.Drawing.Size(109, 68);
		this.PrevActivationButton.TabIndex = 25;
		this.PrevActivationButton.Text = "<<";
		this.PrevActivationButton.UseVisualStyleBackColor = true;
		this.PrevActivationButton.Click += new System.EventHandler(PrevActivationButton_Click);
		this.NextActivationButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.NextActivationButton.Enabled = false;
		this.NextActivationButton.Location = new System.Drawing.Point(1749, 5);
		this.NextActivationButton.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
		this.NextActivationButton.Name = "NextActivationButton";
		this.NextActivationButton.Size = new System.Drawing.Size(109, 68);
		this.NextActivationButton.TabIndex = 26;
		this.NextActivationButton.Text = ">>";
		this.NextActivationButton.UseVisualStyleBackColor = true;
		this.NextActivationButton.Click += new System.EventHandler(NextActivationButton_Click);
		this.tableLayoutPanel1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.tableLayoutPanel1.ColumnCount = 1;
		this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 4);
		this.tableLayoutPanel1.Controls.Add(this.pictureBox1, 0, 0);
		this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel3, 0, 3);
		this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 2);
		this.tableLayoutPanel1.Controls.Add(this.plotView, 0, 1);
		this.tableLayoutPanel1.Location = new System.Drawing.Point(815, 5);
		this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
		this.tableLayoutPanel1.Name = "tableLayoutPanel1";
		this.tableLayoutPanel1.RowCount = 5;
		this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 85f));
		this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
		this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
		this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
		this.tableLayoutPanel1.Size = new System.Drawing.Size(1876, 985);
		this.tableLayoutPanel1.TabIndex = 27;
		this.tableLayoutPanel1.Paint += new System.Windows.Forms.PaintEventHandler(tableLayoutPanel1_Paint_1);
		this.pictureBox1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
		this.pictureBox1.Image = (System.Drawing.Image)resources.GetObject("pictureBox1.Image");
		this.pictureBox1.Location = new System.Drawing.Point(1680, 8);
		this.pictureBox1.Margin = new System.Windows.Forms.Padding(9, 8, 9, 8);
		this.pictureBox1.Name = "pictureBox1";
		this.pictureBox1.Size = new System.Drawing.Size(187, 34);
		this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
		this.pictureBox1.TabIndex = 24;
		this.pictureBox1.TabStop = false;
		this.tableLayoutPanel3.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.tableLayoutPanel3.ColumnCount = 3;
		this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 121f));
		this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 121f));
		this.tableLayoutPanel3.Controls.Add(this.PrevActivationButton, 0, 0);
		this.tableLayoutPanel3.Controls.Add(this.trackTimer, 1, 0);
		this.tableLayoutPanel3.Controls.Add(this.NextActivationButton, 2, 0);
		this.tableLayoutPanel3.Location = new System.Drawing.Point(6, 767);
		this.tableLayoutPanel3.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
		this.tableLayoutPanel3.MaximumSize = new System.Drawing.Size(0, 85);
		this.tableLayoutPanel3.Name = "tableLayoutPanel3";
		this.tableLayoutPanel3.RowCount = 1;
		this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this.tableLayoutPanel3.Size = new System.Drawing.Size(1864, 78);
		this.tableLayoutPanel3.TabIndex = 28;
		this.tableLayoutPanel2.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.tableLayoutPanel2.ColumnCount = 3;
		this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333f));
		this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33334f));
		this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333f));
		this.tableLayoutPanel2.Controls.Add(this.trackLabelCurrent, 1, 0);
		this.tableLayoutPanel2.Controls.Add(this.trackLabelEnd, 2, 0);
		this.tableLayoutPanel2.Controls.Add(this.trackLabelStart, 0, 0);
		this.tableLayoutPanel2.Location = new System.Drawing.Point(6, 718);
		this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
		this.tableLayoutPanel2.Name = "tableLayoutPanel2";
		this.tableLayoutPanel2.RowCount = 1;
		this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this.tableLayoutPanel2.Size = new System.Drawing.Size(1864, 39);
		this.tableLayoutPanel2.TabIndex = 26;
		this.trackLabelCurrent.Dock = System.Windows.Forms.DockStyle.Fill;
		this.trackLabelCurrent.Location = new System.Drawing.Point(630, 0);
		this.trackLabelCurrent.Margin = new System.Windows.Forms.Padding(9, 0, 9, 0);
		this.trackLabelCurrent.Name = "trackLabelCurrent";
		this.trackLabelCurrent.Size = new System.Drawing.Size(603, 39);
		this.trackLabelCurrent.TabIndex = 5;
		this.trackLabelCurrent.Text = "--";
		this.trackLabelCurrent.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.trackLabelEnd.Dock = System.Windows.Forms.DockStyle.Fill;
		this.trackLabelEnd.Location = new System.Drawing.Point(1251, 0);
		this.trackLabelEnd.Margin = new System.Windows.Forms.Padding(9, 0, 9, 0);
		this.trackLabelEnd.Name = "trackLabelEnd";
		this.trackLabelEnd.Size = new System.Drawing.Size(604, 39);
		this.trackLabelEnd.TabIndex = 17;
		this.trackLabelEnd.Text = "--";
		this.trackLabelEnd.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
		this.trackLabelEnd.Click += new System.EventHandler(trackLabelEnd_Click);
		this.trackLabelStart.Dock = System.Windows.Forms.DockStyle.Fill;
		this.trackLabelStart.Location = new System.Drawing.Point(9, 0);
		this.trackLabelStart.Margin = new System.Windows.Forms.Padding(9, 0, 9, 0);
		this.trackLabelStart.Name = "trackLabelStart";
		this.trackLabelStart.Size = new System.Drawing.Size(603, 39);
		this.trackLabelStart.TabIndex = 16;
		this.trackLabelStart.Text = "--";
		this.trackLabelStart.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
		this.plotView.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.plotView.BackColor = System.Drawing.Color.Black;
		this.plotView.Location = new System.Drawing.Point(9, 93);
		this.plotView.Margin = new System.Windows.Forms.Padding(9, 8, 9, 8);
		this.plotView.Name = "plotView";
		this.plotView.PanCursor = System.Windows.Forms.Cursors.Hand;
		this.plotView.Size = new System.Drawing.Size(1858, 612);
		this.plotView.TabIndex = 9;
		this.plotView.Text = "plotView7";
		this.plotView.ZoomHorizontalCursor = System.Windows.Forms.Cursors.SizeWE;
		this.plotView.ZoomRectangleCursor = System.Windows.Forms.Cursors.SizeNWSE;
		this.plotView.ZoomVerticalCursor = System.Windows.Forms.Cursors.SizeNS;
		this.tableLayoutPanel4.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.tableLayoutPanel4.ColumnCount = 4;
		this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25f));
		this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25f));
		this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25f));
		this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25f));
		this.tableLayoutPanel4.Controls.Add(this.updatePortsButton, 0, 0);
		this.tableLayoutPanel4.Controls.Add(this.btnConnect, 2, 0);
		this.tableLayoutPanel4.Controls.Add(this.btnDisconnect, 3, 0);
		this.tableLayoutPanel4.Controls.Add(this.comboPorts, 1, 0);
		this.tableLayoutPanel4.Location = new System.Drawing.Point(6, 5);
		this.tableLayoutPanel4.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
		this.tableLayoutPanel4.MinimumSize = new System.Drawing.Size(201, 0);
		this.tableLayoutPanel4.Name = "tableLayoutPanel4";
		this.tableLayoutPanel4.RowCount = 1;
		this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this.tableLayoutPanel4.Size = new System.Drawing.Size(785, 75);
		this.tableLayoutPanel4.TabIndex = 28;
		this.tableLayoutPanel4.Paint += new System.Windows.Forms.PaintEventHandler(tableLayoutPanel4_Paint);
		this.tableLayoutPanel5.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.tableLayoutPanel5.ColumnCount = 2;
		this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50f));
		this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50f));
		this.tableLayoutPanel5.Controls.Add(this.buttonTerminateEditing, 0, 0);
		this.tableLayoutPanel5.Controls.Add(this.buttonSaveToFile, 1, 0);
		this.tableLayoutPanel5.Location = new System.Drawing.Point(6, 527);
		this.tableLayoutPanel5.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
		this.tableLayoutPanel5.MinimumSize = new System.Drawing.Size(0, 85);
		this.tableLayoutPanel5.Name = "tableLayoutPanel5";
		this.tableLayoutPanel5.RowCount = 1;
		this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50f));
		this.tableLayoutPanel5.Size = new System.Drawing.Size(785, 85);
		this.tableLayoutPanel5.TabIndex = 29;
		this.tableLayoutPanel6.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.tableLayoutPanel6.ColumnCount = 3;
		this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
		this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
		this.tableLayoutPanel6.Controls.Add(this.button3, 2, 0);
		this.tableLayoutPanel6.Controls.Add(this.gMapControl, 1, 0);
		this.tableLayoutPanel6.Controls.Add(this.button2, 0, 0);
		this.tableLayoutPanel6.Location = new System.Drawing.Point(6, 175);
		this.tableLayoutPanel6.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
		this.tableLayoutPanel6.Name = "tableLayoutPanel6";
		this.tableLayoutPanel6.RowCount = 1;
		this.tableLayoutPanel6.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this.tableLayoutPanel6.Size = new System.Drawing.Size(785, 342);
		this.tableLayoutPanel6.TabIndex = 30;
		this.tableLayoutPanel10.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.tableLayoutPanel10.ColumnCount = 1;
		this.tableLayoutPanel10.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this.tableLayoutPanel10.Controls.Add(this.tableLayoutPanel4, 0, 0);
		this.tableLayoutPanel10.Controls.Add(this.stopReadingButton, 0, 1);
		this.tableLayoutPanel10.Controls.Add(this.tableLayoutPanel5, 0, 3);
		this.tableLayoutPanel10.Controls.Add(this.tableLayoutPanel6, 0, 2);
		this.tableLayoutPanel10.Controls.Add(this.tagsListView, 0, 5);
		this.tableLayoutPanel10.Controls.Add(this.panelTags, 0, 4);
		this.tableLayoutPanel10.Location = new System.Drawing.Point(6, 5);
		this.tableLayoutPanel10.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
		this.tableLayoutPanel10.Name = "tableLayoutPanel10";
		this.tableLayoutPanel10.RowCount = 6;
		this.tableLayoutPanel10.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 85f));
		this.tableLayoutPanel10.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 85f));
		this.tableLayoutPanel10.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 68.42106f));
		this.tableLayoutPanel10.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 62f));
		this.tableLayoutPanel10.RowStyles.Add(new System.Windows.Forms.RowStyle());
		this.tableLayoutPanel10.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 31.57895f));
		this.tableLayoutPanel10.Size = new System.Drawing.Size(797, 985);
		this.tableLayoutPanel10.TabIndex = 31;
		this.tableLayoutPanel11.AllowDrop = true;
		this.tableLayoutPanel11.ColumnCount = 2;
		this.tableLayoutPanel11.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30f));
		this.tableLayoutPanel11.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 70f));
		this.tableLayoutPanel11.Controls.Add(this.tableLayoutPanel1, 1, 0);
		this.tableLayoutPanel11.Controls.Add(this.tableLayoutPanel10, 0, 0);
		this.tableLayoutPanel11.Dock = System.Windows.Forms.DockStyle.Fill;
		this.tableLayoutPanel11.Location = new System.Drawing.Point(0, 55);
		this.tableLayoutPanel11.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
		this.tableLayoutPanel11.Name = "tableLayoutPanel11";
		this.tableLayoutPanel11.RowCount = 1;
		this.tableLayoutPanel11.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this.tableLayoutPanel11.Size = new System.Drawing.Size(2697, 995);
		this.tableLayoutPanel11.TabIndex = 32;
		this.tableLayoutPanel11.Paint += new System.Windows.Forms.PaintEventHandler(tableLayoutPanel11_Paint);
		base.AutoScaleDimensions = new System.Drawing.SizeF(16f, 31f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(2697, 1108);
		base.Controls.Add(this.tableLayoutPanel11);
		base.Controls.Add(this.statusStrip);
		base.Controls.Add(this.menuStrip1);
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.MainMenuStrip = this.menuStrip1;
		base.Margin = new System.Windows.Forms.Padding(9, 8, 9, 8);
		this.MaximumSize = new System.Drawing.Size(1333303, 1239932);
		base.Name = "MainForm";
		this.Text = "Data tool";
		base.FormClosing += new System.Windows.Forms.FormClosingEventHandler(Form1_FormClosing);
		base.Load += new System.EventHandler(Form1_Load);
		this.panel1.ResumeLayout(false);
		this.listContextMenu.ResumeLayout(false);
		this.menuStrip1.ResumeLayout(false);
		this.menuStrip1.PerformLayout();
		((System.ComponentModel.ISupportInitialize)this.trackTimer).EndInit();
		this.panelTags.ResumeLayout(false);
		this.tableLayoutPanel9.ResumeLayout(false);
		this.tableLayoutPanel7.ResumeLayout(false);
		this.tableLayoutPanel8.ResumeLayout(false);
		this.tableLayoutPanel8.PerformLayout();
		this.statusStrip.ResumeLayout(false);
		this.statusStrip.PerformLayout();
		this.tableLayoutPanel1.ResumeLayout(false);
		this.tableLayoutPanel1.PerformLayout();
		((System.ComponentModel.ISupportInitialize)this.pictureBox1).EndInit();
		this.tableLayoutPanel3.ResumeLayout(false);
		this.tableLayoutPanel3.PerformLayout();
		this.tableLayoutPanel2.ResumeLayout(false);
		this.tableLayoutPanel4.ResumeLayout(false);
		this.tableLayoutPanel5.ResumeLayout(false);
		this.tableLayoutPanel6.ResumeLayout(false);
		this.tableLayoutPanel6.PerformLayout();
		this.tableLayoutPanel10.ResumeLayout(false);
		this.tableLayoutPanel11.ResumeLayout(false);
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
