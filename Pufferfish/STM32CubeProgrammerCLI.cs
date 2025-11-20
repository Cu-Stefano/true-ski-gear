using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Pufferfish;

public class STM32CubeProgrammerCLI
{
	private string cli_path;

	private bool operation_result;

	private static string start_address = "0x08000000";

	private static string prog_address = "0x08007000";

	private STM32CubeProgrammerCLI_Callback curr_callback;

	private bool update_in_progress;

	public STM32CubeProgrammerCLI(string path)
	{
		cli_path = path;
		operation_result = false;
		curr_callback = null;
		update_in_progress = false;
	}

	private async Task ConsumeOutput(TextReader reader)
	{
		string line = "";
		char[] currentChar = new char[1];
		while (update_in_progress)
		{
			if (await reader.ReadAsync(currentChar, 0, 1) <= 0)
			{
				continue;
			}
			if (currentChar[0] == '\n' || currentChar[0] == '\r' || currentChar[0] == '%')
			{
				line += currentChar[0];
				if (curr_callback != null)
				{
					if (line.Contains("erasing sector"))
					{
						curr_callback("Firmware Update: " + line.Substring(0, 19) + "\n");
					}
					else if (currentChar[0] == '%')
					{
						curr_callback("Firmware Update: " + line.Substring(line.LastIndexOf(' '), line.Length - line.LastIndexOf(' ')) + "\n");
					}
				}
				line = "";
			}
			else
			{
				line += currentChar[0];
			}
		}
	}

	private void StopConsumingOutput(object sender, EventArgs e)
	{
		update_in_progress = false;
		curr_callback?.Invoke("Firmware Update: Done\n");
	}

	public async Task<bool> IsRETMicrocontroller()
	{
		string line = "";
		char[] currentChar = new char[1];
		bool continue_processing = true;
		bool serialFound = false;
		Process process = new Process();
		process.StartInfo = new ProcessStartInfo
		{
			WindowStyle = ProcessWindowStyle.Hidden,
			FileName = "cmd.exe",
			Arguments = $"/C \"{cli_path}bin\\STM32_Programmer_CLI.exe\"  -c port=usb1",
			UseShellExecute = false,
			RedirectStandardOutput = true,
			CreateNoWindow = true,
			StandardOutputEncoding = Encoding.GetEncoding(850)
		};
		process.EnableRaisingEvents = true;
		process.Start();
		process.WaitForExit(20000);
		if (process.ExitCode != 0)
		{
			throw new InvalidOperationException("Failed to execute STM32_Programmer_CLI command to get serial number.");
		}
		while (continue_processing)
		{
			int size = process.StandardOutput.Read(currentChar, 0, 1);
			if (size > 0)
			{
				if (currentChar[0] == '\n' || currentChar[0] == '\r')
				{
					line += currentChar[0];
					if (line.StartsWith("SN          :"))
					{
						int colonIndex = line.IndexOf(':');
						if (colonIndex != -1)
						{
							string serialNumber = line.Substring(colonIndex + 1).Trim();
							if (!uint.TryParse(serialNumber, out var serialNumberDig))
							{
								throw new FormatException("Serial number format is invalid.");
							}
							return serialNumberDig >= 4026531840u;
						}
						throw new InvalidOperationException("Serial number line is malformed.");
					}
					line = "";
				}
				else
				{
					line += currentChar[0];
				}
			}
			continue_processing = !process.WaitForExit(10) || !process.StandardOutput.EndOfStream;
		}
		if (!serialFound)
		{
			throw new InvalidOperationException("Serial number not found in STM32_Programmer_CLI output.");
		}
		return false;
	}

	public bool ProgramFirmwareImgage(string filepath, STM32CubeProgrammerCLI_Callback callback, EventHandler end_callback = null)
	{
		BackgroundWorker worker = new BackgroundWorker();
		worker.DoWork += delegate
		{
			Process process = new Process();
			ProcessStartInfo processStartInfo = new ProcessStartInfo
			{
				WindowStyle = ProcessWindowStyle.Hidden,
				FileName = "cmd.exe",
				Arguments = $"/C \"{cli_path}bin\\STM32_Programmer_CLI.exe\"  -c port=SWD -ob RDP=0xAA nRST_STOP=1 nRST_STDBY=1 nRST_SHDW=0 IWDG_SW=1 IWDG_STOP=0 IWDG_STDBY=0 WWDG_SW=1 BFB2=0 DualBank=0 nBOOT1=1 SRAM2_PE=1 SRAM2_RST=1",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				CreateNoWindow = true,
				StandardOutputEncoding = Encoding.GetEncoding(850)
			};
			process.StartInfo = processStartInfo;
			process.EnableRaisingEvents = true;
			curr_callback = callback;
			update_in_progress = true;
			curr_callback?.Invoke("Flashing Firmware Image: Setting Option Bytes\n");
			process.Start();
			process.WaitForExit();
			if (process.ExitCode == 0)
			{
				process = new Process();
				curr_callback?.Invoke("Flashing Firmware Image: Starting\n");
				curr_callback?.Invoke(string.Format("/C \"{0}bin\\STM32_Programmer_CLI.exe\"  -c port=SWD -e all -d {1} ", cli_path, filepath.Replace("-", "^-")));
				processStartInfo.Arguments = string.Format("/C \"{0}bin\\STM32_Programmer_CLI.exe\"  -c port=SWD -e all -d {1} ", cli_path, filepath.Replace("-", "^-"));
				process.StartInfo = processStartInfo;
				if (end_callback != null)
				{
					process.Exited += end_callback;
				}
				process.EnableRaisingEvents = true;
				process.Exited += StopConsumingOutput;
				process.Start();
				Task task = ConsumeOutput(process.StandardOutput);
				process.WaitForExit();
				if (process.ExitCode != 0)
				{
					curr_callback?.Invoke("Flashing Firmware Image: Failed\n");
				}
				else
				{
					curr_callback?.Invoke("Flashing Firmware Image: Success\n");
				}
			}
			else
			{
				curr_callback?.Invoke("Flashing Firmware Image: Failed setting option bytes\n");
			}
		};
		worker.RunWorkerAsync();
		return true;
	}

	public bool ProgramApp(string filepath, STM32CubeProgrammerCLI_Callback callback, EventHandler end_callback = null)
	{
		bool isRetMicrocontroller = false;
		int attempts = 0;
		Exception lastException = null;
		while (attempts < 3)
		{
			try
			{
				isRetMicrocontroller = IsRETMicrocontroller().GetAwaiter().GetResult();
				lastException = null;
			}
			catch (Exception ex)
			{
				lastException = ex;
				attempts++;
				if (attempts >= 3)
				{
					if (end_callback != null)
					{
						end_callback(this, null);
					}
					return false;
				}
				continue;
			}
			break;
		}
		if (isRetMicrocontroller && !Path.GetFileName(filepath).Contains("RET"))
		{
			if (end_callback != null)
			{
				end_callback(this, null);
			}
			return false;
		}
		if (!isRetMicrocontroller && Path.GetFileName(filepath).Contains("RET"))
		{
			if (end_callback != null)
			{
				end_callback(this, null);
			}
			return false;
		}
		BackgroundWorker worker = new BackgroundWorker();
		worker.DoWork += delegate
		{
			Process process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					WindowStyle = ProcessWindowStyle.Hidden,
					FileName = "cmd.exe",
					Arguments = string.Format("/C \"{0}bin\\STM32_Programmer_CLI.exe\"  -c port=usb1 -w {1} {2} --start {3}", cli_path, filepath.Replace("-", "^-"), prog_address, start_address),
					UseShellExecute = false,
					RedirectStandardOutput = true,
					CreateNoWindow = true,
					StandardOutputEncoding = Encoding.GetEncoding(850)
				}
			};
			if (end_callback != null)
			{
				process.Exited += end_callback;
			}
			process.EnableRaisingEvents = true;
			process.Exited += StopConsumingOutput;
			curr_callback = callback;
			update_in_progress = true;
			curr_callback?.Invoke("Firmware Update: Starting\n");
			process.Start();
			Task task = ConsumeOutput(process.StandardOutput);
			process.WaitForExit();
		};
		worker.RunWorkerAsync();
		return true;
	}
}
