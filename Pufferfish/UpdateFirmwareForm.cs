using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using CyUSB;
using PufferFish.Properties;
using USBBootloaderHost;

namespace PufferFish;

public class UpdateFirmwareForm : Form
{
	public CyHidDevice BootloaderHIDDevice;

	public USBDeviceList usbHIDDevices = null;

	internal const int ERR_CLOSE = 2;

	internal const int ERR_OPEN = 1;

	internal const int ERR_READ = 3;

	internal const int ERR_SUCCESS = 0;

	internal const int ERR_WRITE = 4;

	private Bootloader_Utils.CyBtldr_CommunicationsData comm_data = default(Bootloader_Utils.CyBtldr_CommunicationsData);

	private string firmwareTempFile;

	private int PID = 46877;

	private int VID = 1204;

	private Thread thread;

	public const int DEBUG = 0;

	public const int RELEASE = 1;

	public const int AIRBAG = 2;

	public const int FILE = 3;

	private string filename = null;

	private int tipo_firmware;

	private IContainer components = null;

	private Button button1;

	private CheckBox firmwareRead;

	private CheckBox deviceDetected;

	private CheckBox scritturaFirmware;

	private ProgressBar progressBar1;

	private CheckBox riavvioCheckbox;

	private TableLayoutPanel tableLayoutPanel1;

	public UpdateFirmwareForm(int tipo_firmware, string filename)
	{
		this.tipo_firmware = tipo_firmware;
		this.filename = filename;
		InitializeComponent();
	}

	public int CloseConnection()
	{
		int status = 0;
		BootloaderHIDDevice = null;
		return status;
	}

	public bool GetHidDevice()
	{
		bool Status = false;
		try
		{
			BootloaderHIDDevice = usbHIDDevices[VID, PID] as CyHidDevice;
			Status = BootloaderHIDDevice != null;
			return Status;
		}
		catch
		{
			return Status;
		}
	}

	public int OpenConnection()
	{
		int status = 0;
		return (!GetHidDevice()) ? 1 : 0;
	}

	public void ProgressUpdate(byte arrayID, ushort rowNum)
	{
		BeginInvoke((MethodInvoker)delegate
		{
			progressBar1.Increment(1);
		});
	}

	public int ReadData(IntPtr buffer, int size)
	{
		bool local_status = false;
		byte[] data = new byte[size];
		if (GetHidDevice())
		{
			local_status = BootloaderHIDDevice.ReadInput();
			data = BootloaderHIDDevice.Inputs.DataBuf;
			Marshal.Copy(data, 1, buffer, Math.Min(size, data.Length));
			return (!local_status) ? 3 : 0;
		}
		return 3;
	}

	public int WriteData(IntPtr buffer, int size)
	{
		byte[] data = new byte[64];
		bool status = true;
		Marshal.Copy(buffer, data, 0, size);
		if (GetHidDevice())
		{
			BootloaderHIDDevice.Outputs.DataBuf[0] = BootloaderHIDDevice.Outputs.ID;
			for (int i = 1; i <= size; i++)
			{
				BootloaderHIDDevice.Outputs.DataBuf[i] = data[i - 1];
			}
			if (BootloaderHIDDevice.WriteOutput())
			{
				return 0;
			}
			return 4;
		}
		return 4;
	}

	private void button1_Click(object sender, EventArgs e)
	{
		if (thread.IsAlive)
		{
			thread.Abort();
		}
		Close();
	}

	private void doWork()
	{
		try
		{
			if (!LeggiFile())
			{
				MessageBox.Show("Error reading the file");
				button1.Enabled = true;
				return;
			}
			BeginInvoke((MethodInvoker)delegate
			{
				firmwareRead.Checked = true;
			});
			WaitForHid();
			BeginInvoke((MethodInvoker)delegate
			{
				deviceDetected.Checked = true;
				button1.Enabled = false;
			});
			if (WriteFirmware())
			{
				BeginInvoke((MethodInvoker)delegate
				{
					scritturaFirmware.Checked = true;
				});
			}
			BeginInvoke((MethodInvoker)delegate
			{
				riavvioCheckbox.Text = "Reboot the device (wait 15 seconds)";
			});
			Thread.Sleep(15000);
			BeginInvoke((MethodInvoker)delegate
			{
				riavvioCheckbox.Text = "Reboot the device (completed)";
				riavvioCheckbox.Checked = true;
			});
			BeginInvoke((MethodInvoker)delegate
			{
				button1.Text = "Firmware update completed";
				button1.Enabled = true;
			});
		}
		catch
		{
		}
	}

	private bool LeggiFile()
	{
		try
		{
			firmwareTempFile = Path.GetTempFileName();
			switch (tipo_firmware)
			{
			default:
				File.WriteAllBytes(firmwareTempFile, Resources.Pesce_Palla_D);
				break;
			case 1:
				File.WriteAllBytes(firmwareTempFile, Resources.Pesce_Palla_R);
				break;
			case 2:
				File.WriteAllBytes(firmwareTempFile, Resources.Pesce_Palla_Airbag);
				break;
			case 3:
				firmwareTempFile = filename;
				break;
			}
			int lines = File.ReadAllLines(firmwareTempFile).Length - 1;
			BeginInvoke((MethodInvoker)delegate
			{
				progressBar1.Maximum = lines;
			});
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	private void UpdateFirmwareForm_Load(object sender, EventArgs e)
	{
		usbHIDDevices = new USBDeviceList(4);
		thread = new Thread(doWork);
		thread.Start();
	}

	private void WaitForHid()
	{
		while (!GetHidDevice())
		{
			Thread.Sleep(10);
		}
	}

	private bool WriteFirmware()
	{
		comm_data.OpenConnection = OpenConnection;
		comm_data.CloseConnection = CloseConnection;
		comm_data.ReadData = ReadData;
		comm_data.WriteData = WriteData;
		comm_data.MaxTransferSize = 64u;
		ReturnCodes local_status = ReturnCodes.CYRET_SUCCESS;
		progressBar1.Value = 0;
		Bootloader_Utils.CyBtldr_ProgressUpdate update = ProgressUpdate;
		local_status = (ReturnCodes)Bootloader_Utils.CyBtldr_Program(firmwareTempFile, null, 1, ref comm_data, update);
		if (local_status == ReturnCodes.CYRET_SUCCESS)
		{
			return true;
		}
		if (ReturnCodes.CYRET_ERR_COMM_MASK == (local_status & ReturnCodes.CYRET_ERR_COMM_MASK))
		{
			MessageBox.Show(" Program failed: Communication Error");
		}
		else if (ReturnCodes.CYRET_ERR_DATA == (local_status & ReturnCodes.CYRET_ERR_DATA))
		{
			MessageBox.Show(" Program failed: Check Security Key");
		}
		else
		{
			MessageBox.Show(" Program failed  " + local_status);
		}
		return false;
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PufferFish.UpdateFirmwareForm));
		this.button1 = new System.Windows.Forms.Button();
		this.firmwareRead = new System.Windows.Forms.CheckBox();
		this.deviceDetected = new System.Windows.Forms.CheckBox();
		this.scritturaFirmware = new System.Windows.Forms.CheckBox();
		this.progressBar1 = new System.Windows.Forms.ProgressBar();
		this.riavvioCheckbox = new System.Windows.Forms.CheckBox();
		this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
		this.tableLayoutPanel1.SuspendLayout();
		base.SuspendLayout();
		this.button1.Anchor = System.Windows.Forms.AnchorStyles.None;
		this.button1.Location = new System.Drawing.Point(29, 314);
		this.button1.Margin = new System.Windows.Forms.Padding(6);
		this.button1.Name = "button1";
		this.button1.Size = new System.Drawing.Size(446, 44);
		this.button1.TabIndex = 0;
		this.button1.Text = "Close";
		this.button1.UseVisualStyleBackColor = true;
		this.button1.Click += new System.EventHandler(button1_Click);
		this.firmwareRead.Anchor = System.Windows.Forms.AnchorStyles.Left;
		this.firmwareRead.AutoCheck = false;
		this.firmwareRead.AutoSize = true;
		this.firmwareRead.Location = new System.Drawing.Point(6, 14);
		this.firmwareRead.Margin = new System.Windows.Forms.Padding(6);
		this.firmwareRead.Name = "firmwareRead";
		this.firmwareRead.Size = new System.Drawing.Size(261, 29);
		this.firmwareRead.TabIndex = 1;
		this.firmwareRead.Text = "Read new firmware file";
		this.firmwareRead.UseVisualStyleBackColor = true;
		this.deviceDetected.Anchor = System.Windows.Forms.AnchorStyles.Left;
		this.deviceDetected.AutoCheck = false;
		this.deviceDetected.AutoSize = true;
		this.deviceDetected.Location = new System.Drawing.Point(6, 72);
		this.deviceDetected.Margin = new System.Windows.Forms.Padding(6);
		this.deviceDetected.Name = "deviceDetected";
		this.deviceDetected.Size = new System.Drawing.Size(167, 29);
		this.deviceDetected.TabIndex = 2;
		this.deviceDetected.Text = "Detect board";
		this.deviceDetected.UseVisualStyleBackColor = true;
		this.scritturaFirmware.Anchor = System.Windows.Forms.AnchorStyles.Left;
		this.scritturaFirmware.AutoCheck = false;
		this.scritturaFirmware.AutoSize = true;
		this.scritturaFirmware.Location = new System.Drawing.Point(6, 130);
		this.scritturaFirmware.Margin = new System.Windows.Forms.Padding(6);
		this.scritturaFirmware.Name = "scritturaFirmware";
		this.scritturaFirmware.Size = new System.Drawing.Size(181, 29);
		this.scritturaFirmware.TabIndex = 3;
		this.scritturaFirmware.Text = "Write firmware";
		this.scritturaFirmware.UseVisualStyleBackColor = true;
		this.progressBar1.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.progressBar1.Location = new System.Drawing.Point(6, 181);
		this.progressBar1.Margin = new System.Windows.Forms.Padding(6);
		this.progressBar1.Name = "progressBar1";
		this.progressBar1.Size = new System.Drawing.Size(492, 44);
		this.progressBar1.TabIndex = 4;
		this.riavvioCheckbox.Anchor = System.Windows.Forms.AnchorStyles.Left;
		this.riavvioCheckbox.AutoCheck = false;
		this.riavvioCheckbox.AutoSize = true;
		this.riavvioCheckbox.Location = new System.Drawing.Point(6, 246);
		this.riavvioCheckbox.Margin = new System.Windows.Forms.Padding(6);
		this.riavvioCheckbox.Name = "riavvioCheckbox";
		this.riavvioCheckbox.Size = new System.Drawing.Size(182, 29);
		this.riavvioCheckbox.TabIndex = 5;
		this.riavvioCheckbox.Text = "Reboot device";
		this.riavvioCheckbox.UseVisualStyleBackColor = true;
		this.tableLayoutPanel1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.tableLayoutPanel1.AutoSize = true;
		this.tableLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
		this.tableLayoutPanel1.ColumnCount = 1;
		this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this.tableLayoutPanel1.Controls.Add(this.firmwareRead, 0, 0);
		this.tableLayoutPanel1.Controls.Add(this.button1, 0, 5);
		this.tableLayoutPanel1.Controls.Add(this.riavvioCheckbox, 0, 4);
		this.tableLayoutPanel1.Controls.Add(this.deviceDetected, 0, 1);
		this.tableLayoutPanel1.Controls.Add(this.progressBar1, 0, 3);
		this.tableLayoutPanel1.Controls.Add(this.scritturaFirmware, 0, 2);
		this.tableLayoutPanel1.Location = new System.Drawing.Point(12, 12);
		this.tableLayoutPanel1.Name = "tableLayoutPanel1";
		this.tableLayoutPanel1.RowCount = 6;
		this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20f));
		this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20f));
		this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20f));
		this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20f));
		this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20f));
		this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 92f));
		this.tableLayoutPanel1.Size = new System.Drawing.Size(504, 382);
		this.tableLayoutPanel1.TabIndex = 6;
		base.AutoScaleDimensions = new System.Drawing.SizeF(12f, 25f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(518, 402);
		base.ControlBox = false;
		base.Controls.Add(this.tableLayoutPanel1);
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.Margin = new System.Windows.Forms.Padding(6);
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "UpdateFirmwareForm";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		this.Text = "Update firmware";
		base.Load += new System.EventHandler(UpdateFirmwareForm_Load);
		this.tableLayoutPanel1.ResumeLayout(false);
		this.tableLayoutPanel1.PerformLayout();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
