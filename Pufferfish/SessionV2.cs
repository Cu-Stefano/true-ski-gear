using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using GeoTimeZone;
using log4net;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using TimeZoneConverter;

namespace PufferFish;

internal class SessionV2 : BaseSession
{
	public delegate void ReadSessionProgress(int perc, long bps, long eta);

	public const byte MSG_TYPE_SESS = 4;

	public const byte MSG_TYPE_1KHZ = 13;

	public const byte MSG_TYPE_100HZ = 14;

	private static readonly ILog log = LogManager.GetLogger("PufferFish");

	private int deviceID;

	private uint sessionID;

	private SQLiteCommand slowSensorDataCmd;

	private SQLiteCommand fastSensorDataCmd;

	private SQLiteCommand gpsDataCmd;

	private SQLiteCommand selectDataCommand;

	private SQLiteCommand selectGPSDataCommand;

	public new const int maxNumberOfGraphs = 9;

	public new const int maxNumberOfSensors = 5;

	public new static int gyro_index = 5;

	public new static int speed_index = 6;

	public string sessionFileName;

	private SessionHeader sessionHeader { get; set; }

	public string SessionFileName => sessionFileName;

	public SessionV2(int deviceID, uint sessionid, string fileName)
		: base(9, 4)
	{
		sessionVersion = 2;
		log.Info($"Initializing session {sessionid} for {deviceID} device from {fileName}");
		this.deviceID = deviceID;
		sessionID = sessionid;
		graphData.reset();
		sessionFileName = fileName;
		FileInfo fileInfo = new FileInfo(fileName);
		string dbFileName = fileName;
		if (!fileInfo.Exists)
		{
			throw new Exception("File does not exist");
		}
		if (fileInfo.Extension == ".dat")
		{
			dbFileName = BaseSession.getDBFolder() + fileInfo.Name + ".sqlite";
			SQLiteConnection.CreateFile(dbFileName);
			sessionDBConn = new SQLiteConnection($"Data Source={dbFileName};Version=3;");
			sessionDBConn.Open();
			selectDataCommand = new SQLiteCommand(sessionDBConn);
			selectGPSDataCommand = new SQLiteCommand(sessionDBConn);
			SQLiteCommand command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS gpssensors (Timestamp DATETIME DEFAULT null, dataIndex INTEGER UNIQUE ON CONFLICT REPLACE, latitude, longitude, speed);", sessionDBConn);
			command.ExecuteNonQuery();
			command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS slowSensors (Timestamp DATETIME DEFAULT null, dataIndex INTEGER UNIQUE ON CONFLICT REPLACE, acc_0_x, acc_0_y, acc_0_z, acc_1_x, acc_1_y, acc_1_z, acc_2_x, acc_2_y, acc_2_z, acc_3_x, acc_3_y, acc_3_z, mag_x, mag_y, mag_z, rot_x, rot_y, rot_z, grav_x, grav_y, grav_z, activation);", sessionDBConn);
			command.ExecuteNonQuery();
			command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS fastSensors (Timestamp DATETIME DEFAULT null, dataIndex INTEGER UNIQUE ON CONFLICT REPLACE, acc_x, acc_y, acc_z, gyro_x, gyro_y, gyro_z, activation);", sessionDBConn);
			command.ExecuteNonQuery();
			command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS tags (id INTEGER PRIMARY KEY AUTOINCREMENT, Timestamp DATETIME DEFAULT null, type TEXT, description TEXT);", sessionDBConn);
			command.ExecuteNonQuery();
			command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS notes (type TEXT, description TEXT);", sessionDBConn);
			command.ExecuteNonQuery();
			command = new SQLiteCommand(" CREATE INDEX IF NOT EXISTS slowTimestampIdx ON slowSensors(timestamp);", sessionDBConn);
			command.ExecuteNonQuery();
			command = new SQLiteCommand(" CREATE INDEX IF NOT EXISTS slowDataIdx ON slowSensors(dataIndex);", sessionDBConn);
			command.ExecuteNonQuery();
			command = new SQLiteCommand(" CREATE INDEX IF NOT EXISTS fastTimestampIdx ON fastSensors(timestamp);", sessionDBConn);
			command.ExecuteNonQuery();
			command = new SQLiteCommand(" CREATE INDEX IF NOT EXISTS fastDataIdx ON fastSensors(dataIndex);", sessionDBConn);
			command.ExecuteNonQuery();
			slowSensorDataCmd = new SQLiteCommand(sessionDBConn);
			slowSensorDataCmd.CommandText = "INSERT INTO slowSensors (dataIndex, acc_0_x, acc_0_y, acc_0_z, acc_1_x, acc_1_y, acc_1_z, acc_2_x, acc_2_y, acc_2_z, acc_3_x, acc_3_y, acc_3_z, mag_x, mag_y, mag_z, rot_x, rot_y, rot_z, grav_x, grav_y, grav_z, activation, Timestamp) VALUES (@dataIndex, @acc_0_x, @acc_0_y, @acc_0_z, @acc_1_x, @acc_1_y, @acc_1_z, @acc_2_x, @acc_2_y, @acc_2_z, @acc_3_x, @acc_3_y, @acc_3_z, @mag_x, @mag_y, @mag_z, @rot_x, @rot_y, @rot_z, @grav_x, @grav_y, @grav_z, @activation, @time);";
			fastSensorDataCmd = new SQLiteCommand(sessionDBConn);
			fastSensorDataCmd.CommandText = "INSERT INTO fastSensors (dataIndex, acc_x, acc_y, acc_z, gyro_x, gyro_y, gyro_z, activation, Timestamp) VALUES (@dataIndex, @acc_x, @acc_y, @acc_z, @gyro_x, @gyro_y, @gyro_z, @activation, @time);";
			gpsDataCmd = new SQLiteCommand(sessionDBConn);
			gpsDataCmd.CommandText = "INSERT INTO gpssensors (dataIndex, latitude, longitude, speed, Timestamp) VALUES (@dataIndex, @latitude, @longitude, @speed, @time);";
			return;
		}
		throw new Exception("Wrong file format");
	}

	public IEnumerable<DataPoint> getGravityData(int axis)
	{
		return graphData.getGravityData(axis);
	}

	public IEnumerable<DataPoint> getOrientationData(int axis)
	{
		return graphData.getOrientationData(axis);
	}

	public static PlotModel InitSessionPlotModel(List<LineSeries[]> series, List<LineAnnotation> lineAnnotations, Func<double, string> getTimeLabel)
	{
		PlotModel myModel = new PlotModel();
		int nofsensors = 5;
		int nofgraphs = 9;
		for (int i = 0; i < nofgraphs; i++)
		{
			lineAnnotations.Add(new LineAnnotation());
			series.Add(null);
		}
		string Title = "";
		myModel.Axes.Add(new LinearAxis
		{
			Position = AxisPosition.Bottom,
			MaximumRange = 600000.0,
			MinimumRange = 500.0,
			Key = "Time",
			AbsoluteMinimum = 0.0,
			TextColor = OxyColors.White,
			LabelFormatter = getTimeLabel
		});
		OxyColor b1 = OxyColor.FromRgb(20, 20, 20);
		OxyColor b2 = OxyColors.Black;
		for (int j = 0; j < nofsensors; j++)
		{
			int pos = 0;
			switch (j)
			{
			case 4:
				Title = "Acc 4";
				pos = 5;
				break;
			case 3:
				Title = "Acc 3";
				pos = 4;
				break;
			case 2:
				Title = "Acc 2";
				pos = 3;
				break;
			case 1:
				Title = "Acc 1";
				pos = 2;
				break;
			case 0:
				Title = "Main";
				pos = 6;
				break;
			}
			myModel.Axes.Add(new LinearAxis
			{
				Position = AxisPosition.Left,
				Key = Title,
				TitleColor = OxyColors.White,
				TextColor = OxyColors.White,
				FontSize = 10.0,
				StartPosition = (float)pos / (float)nofgraphs,
				EndPosition = (float)(pos + 1) / (float)nofgraphs,
				Minimum = -8.0,
				Maximum = 8.0,
				MajorStep = 8.0,
				Title = Title,
				IsZoomEnabled = false,
				IsPanEnabled = false
			});
			series[j] = new LineSeries[3];
			for (int k = 0; k < 3; k++)
			{
				series[j][k] = new LineSeries();
				series[j][k].YAxisKey = Title;
				series[j][k].StrokeThickness = 2.0;
				series[j][k].Background = ((pos % 2 == 0) ? b1 : b2);
				series[j][k].Smooth = false;
				series[j][k].TrackerFormatString = "{x} {Y}";
				myModel.Series.Add(series[j][k]);
			}
		}
		myModel.Axes.Add(new LinearAxis
		{
			Position = AxisPosition.Left,
			Key = "Gyro",
			TextColor = OxyColors.White,
			TitleColor = OxyColors.White,
			StartPosition = (float)(nofsensors + 2) / (float)nofgraphs,
			EndPosition = (float)(nofsensors + 3) / (float)nofgraphs,
			Title = "Gyro",
			IsZoomEnabled = false,
			IsPanEnabled = false,
			FontSize = 10.0
		});
		series[gyro_index] = new LineSeries[3];
		for (int l = 0; l < 3; l++)
		{
			series[gyro_index][l] = new LineSeries();
			series[gyro_index][l].YAxisKey = "Gyro";
			series[gyro_index][l].StrokeThickness = 2.0;
			series[gyro_index][l].Background = b1;
			series[gyro_index][l].Smooth = false;
			myModel.Series.Add(series[gyro_index][l]);
		}
		myModel.Axes.Add(new LinearAxis
		{
			Position = AxisPosition.Left,
			Key = "Speed",
			TextColor = OxyColors.White,
			TitleColor = OxyColors.White,
			StartPosition = (float)(nofsensors + 3) / (float)nofgraphs,
			EndPosition = (float)(nofsensors + 4) / (float)nofgraphs,
			Title = "Speed",
			IsZoomEnabled = false,
			IsPanEnabled = false,
			FontSize = 10.0,
			MajorStep = 100.0,
			Minimum = 0.0,
			Maximum = 300.0
		});
		series[speed_index] = new LineSeries[1];
		series[speed_index][0] = new LineSeries();
		series[speed_index][0].YAxisKey = "Speed";
		series[speed_index][0].StrokeThickness = 2.0;
		series[speed_index][0].Background = b1;
		series[speed_index][0].Smooth = false;
		myModel.Series.Add(series[speed_index][0]);
		myModel.Axes.Add(new LinearAxis
		{
			Position = AxisPosition.Left,
			Key = "Pose",
			TextColor = OxyColors.White,
			TitleColor = OxyColors.White,
			StartPosition = 0.0,
			EndPosition = 1f / (float)nofgraphs,
			Title = "Pose",
			IsZoomEnabled = false,
			IsPanEnabled = false,
			FontSize = 10.0
		});
		series[speed_index + 1] = new LineSeries[3];
		for (int m = 0; m < 3; m++)
		{
			series[speed_index + 1][m] = new LineSeries();
			series[speed_index + 1][m].YAxisKey = "Pose";
			series[speed_index + 1][m].StrokeThickness = 2.0;
			series[speed_index + 1][m].Background = b1;
			series[speed_index + 1][m].Smooth = false;
			myModel.Series.Add(series[speed_index + 1][m]);
		}
		myModel.Axes.Add(new LinearAxis
		{
			Position = AxisPosition.Left,
			Key = "Gravity",
			TextColor = OxyColors.White,
			TitleColor = OxyColors.White,
			StartPosition = 1f / (float)nofgraphs,
			EndPosition = 2f / (float)nofgraphs,
			Title = "Gravity",
			IsZoomEnabled = false,
			IsPanEnabled = false,
			FontSize = 10.0
		});
		series[speed_index + 2] = new LineSeries[3];
		for (int n = 0; n < 3; n++)
		{
			series[speed_index + 2][n] = new LineSeries();
			series[speed_index + 2][n].YAxisKey = "Gravity";
			series[speed_index + 2][n].StrokeThickness = 2.0;
			series[speed_index + 2][n].Background = b1;
			series[speed_index + 2][n].Smooth = false;
			myModel.Series.Add(series[speed_index + 2][n]);
		}
		for (int num = 0; num < nofsensors; num++)
		{
			series[num][0].Color = OxyColors.Red;
			series[num][1].Color = OxyColors.Green;
			series[num][2].Color = OxyColors.Blue;
			lineAnnotations[num] = new LineAnnotation
			{
				Type = LineAnnotationType.Vertical,
				X = 10.0,
				ClipByYAxis = true,
				Color = OxyColors.White,
				LineStyle = LineStyle.Solid,
				StrokeThickness = 2.0,
				YAxisKey = series[num][0].YAxisKey
			};
			myModel.Annotations.Add(lineAnnotations[num]);
		}
		series[nofsensors][0].Color = OxyColors.Red;
		series[nofsensors][1].Color = OxyColors.Green;
		series[nofsensors][2].Color = OxyColors.Blue;
		lineAnnotations[nofsensors] = new LineAnnotation
		{
			Type = LineAnnotationType.Vertical,
			X = 10.0,
			ClipByYAxis = true,
			Color = OxyColors.White,
			LineStyle = LineStyle.Solid,
			StrokeThickness = 2.0,
			YAxisKey = series[nofsensors][0].YAxisKey
		};
		myModel.Annotations.Add(lineAnnotations[nofsensors]);
		series[speed_index][0].Color = OxyColors.Red;
		lineAnnotations[speed_index] = new LineAnnotation
		{
			Type = LineAnnotationType.Vertical,
			X = 10.0,
			ClipByYAxis = true,
			Color = OxyColors.White,
			LineStyle = LineStyle.Solid,
			StrokeThickness = 2.0,
			YAxisKey = series[speed_index][0].YAxisKey
		};
		myModel.Annotations.Add(lineAnnotations[speed_index]);
		series[speed_index + 1][0].Color = OxyColors.Red;
		series[speed_index + 1][1].Color = OxyColors.Green;
		series[speed_index + 1][2].Color = OxyColors.Blue;
		lineAnnotations[speed_index + 1] = new LineAnnotation
		{
			Type = LineAnnotationType.Vertical,
			X = 10.0,
			ClipByYAxis = true,
			Color = OxyColors.White,
			LineStyle = LineStyle.Solid,
			StrokeThickness = 2.0,
			YAxisKey = series[speed_index + 1][0].YAxisKey
		};
		myModel.Annotations.Add(lineAnnotations[speed_index + 1]);
		series[speed_index + 2][0].Color = OxyColors.Red;
		series[speed_index + 2][1].Color = OxyColors.Green;
		series[speed_index + 2][2].Color = OxyColors.Blue;
		lineAnnotations[speed_index + 2] = new LineAnnotation
		{
			Type = LineAnnotationType.Vertical,
			X = 10.0,
			ClipByYAxis = true,
			Color = OxyColors.White,
			LineStyle = LineStyle.Solid,
			StrokeThickness = 2.0,
			YAxisKey = series[speed_index + 2][0].YAxisKey
		};
		myModel.Annotations.Add(lineAnnotations[speed_index + 2]);
		return myModel;
	}

	private static T FromBinaryReader<T>(BinaryReader reader)
	{
		byte[] bytes = reader.ReadBytes(Marshal.SizeOf(typeof(T)));
		GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
		T theStructure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
		handle.Free();
		return theStructure;
	}

	private DateTime GenerateSessionTimestampFromUSec(double uSec)
	{
		return ((sessionHeader.year == 0) ? new DateTime(0L, DateTimeKind.Utc) : new DateTime(2000 + sessionHeader.year, sessionHeader.month, sessionHeader.day, 0, 0, 0, DateTimeKind.Utc)).AddMilliseconds((int)(uSec / 1000.0));
	}

	public void ReadSessionFromFileV2(string fileName, ReadSessionProgress prog = null)
	{
		if (!File.Exists(fileName))
		{
			log.Warn("File " + fileName + " does not exist");
			return;
		}
		BinaryReader reader = new BinaryReader(new FileStream(fileName, FileMode.Open, FileAccess.Read));
		uint dataIndex = 0u;
		using SQLiteTransaction transaction = sessionDBConn.BeginTransaction();
		try
		{
			DateTime starttime = DateTime.Now;
			long prev_pos = 0L;
			long bps = 0L;
			long eta = 0L;
			Queue<(double, double, double)> myQ = new Queue<(double, double, double)>();
			FFTMotDetect mot_det = new FFTMotDetect();
			int db_num = 0;
			while (reader.BaseStream.Position != reader.BaseStream.Length)
			{
				byte type;
				try
				{
					type = (byte)reader.PeekChar();
				}
				catch (Exception)
				{
					reader.BaseStream.Seek(reader.BaseStream.Length, SeekOrigin.Begin);
					continue;
				}
				switch (type)
				{
				case 4:
					Marshal.SizeOf(typeof(SessionHeader));
					try
					{
						sessionHeader = FromBinaryReader<SessionHeader>(reader);
					}
					catch (Exception)
					{
						reader.BaseStream.Seek(reader.BaseStream.Length, SeekOrigin.Begin);
						continue;
					}
					break;
				case 13:
				{
					SensorsData1KHZStruct sensorsData1KHZStruct;
					try
					{
						sensorsData1KHZStruct = FromBinaryReader<SensorsData1KHZStruct>(reader);
					}
					catch (Exception)
					{
						reader.BaseStream.Seek(reader.BaseStream.Length, SeekOrigin.Begin);
						continue;
					}
					double accScale2 = (double)(int)sessionHeader.acc_full_scale / (Math.Pow(2.0, 16.0) / 2.0);
					double gyroScale = (double)(int)sessionHeader.gyro_full_scale / (Math.Pow(2.0, 16.0) / 2.0);
					gyroScale = ((sessionHeader.gyro_full_scale == 2000) ? 0.07 : ((sessionHeader.gyro_full_scale == 1000) ? 0.035 : ((sessionHeader.gyro_full_scale != 500) ? 0.004375 : (7.0 / 800.0))));
					try
					{
						fastSensorDataCmd.Parameters.AddWithValue("@dataIndex", dataIndex);
						int db_num_ret = mot_det.AddSample((float)sensorsData1KHZStruct.data.acc[2] * 0.244f * 0.001f);
						if (db_num_ret != 0)
						{
							db_num = db_num_ret;
						}
						fastSensorDataCmd.Parameters.AddWithValue("@acc_x", (double)sensorsData1KHZStruct.data.acc[0] * accScale2);
						fastSensorDataCmd.Parameters.AddWithValue("@acc_y", (double)sensorsData1KHZStruct.data.acc[1] * accScale2);
						fastSensorDataCmd.Parameters.AddWithValue("@acc_z", (double)sensorsData1KHZStruct.data.acc[2] * accScale2);
						fastSensorDataCmd.Parameters.AddWithValue("@gyro_x", (double)sensorsData1KHZStruct.data.gyro[0] * gyroScale);
						fastSensorDataCmd.Parameters.AddWithValue("@gyro_y", (double)sensorsData1KHZStruct.data.gyro[1] * gyroScale);
						fastSensorDataCmd.Parameters.AddWithValue("@gyro_z", (double)sensorsData1KHZStruct.data.gyro[2] * gyroScale);
						fastSensorDataCmd.Parameters.AddWithValue("@activation", sensorsData1KHZStruct.data.activation);
						fastSensorDataCmd.Parameters.AddWithValue("@time", GenerateSessionTimestampFromUSec((double)sensorsData1KHZStruct.t.msec * 100.0));
						var res = fastSensorDataCmd.ExecuteNonQuery();
					}
					catch (Exception message2)
					{
						log.Error(message2);
						log.Error($"Unable to store sensor data with index {dataIndex}");
					}
					break;
				}
				case 14:
				{
					SensorsData100HZStruct sensorsData100HZStruct;
					try
					{
						sensorsData100HZStruct = FromBinaryReader<SensorsData100HZStruct>(reader);
					}
					catch (Exception)
					{
						reader.BaseStream.Seek(reader.BaseStream.Length, SeekOrigin.Begin);
						continue;
					}
					try
					{
						if (sensorsData100HZStruct.header.size >= 74)
						{
							slowSensorDataCmd.Parameters.AddWithValue("@dataIndex", dataIndex);
							double accScale = (double)(int)sessionHeader.acc_full_scale / (Math.Pow(2.0, 16.0) / 2.0);
							double magScale = (double)(int)sessionHeader.mag_full_scale / (Math.Pow(2.0, 16.0) / 2.0);
							for (int i = 0; i < 4; i++)
							{
								slowSensorDataCmd.Parameters.AddWithValue($"@acc_{i}_x", (double)sensorsData100HZStruct.data.acc[i * 3] * accScale);
								slowSensorDataCmd.Parameters.AddWithValue($"@acc_{i}_y", (double)sensorsData100HZStruct.data.acc[i * 3 + 1] * accScale);
								slowSensorDataCmd.Parameters.AddWithValue($"@acc_{i}_z", (double)sensorsData100HZStruct.data.acc[i * 3 + 2] * accScale);
							}
							slowSensorDataCmd.Parameters.AddWithValue("@mag_x", (double)sensorsData100HZStruct.data.mag[0] * magScale);
							slowSensorDataCmd.Parameters.AddWithValue("@mag_y", (double)sensorsData100HZStruct.data.mag[1] * magScale);
							slowSensorDataCmd.Parameters.AddWithValue("@mag_z", (double)sensorsData100HZStruct.data.mag[2] * magScale);
							double g = (double)sensorsData100HZStruct.data.rotation[2] / 360.0 * 2.0 * Math.PI;
							double b = (double)sensorsData100HZStruct.data.rotation[1] / 360.0 * 2.0 * Math.PI;
							double a = (double)sensorsData100HZStruct.data.rotation[0] / 360.0 * 2.0 * Math.PI;
							double x = Math.Cos(a) * Math.Sin(b) * Math.Cos(g) + Math.Sin(a) * Math.Sin(g);
							double y = Math.Sin(a) * Math.Sin(b) * Math.Cos(g) - Math.Cos(a) * Math.Sin(g);
							double z = Math.Cos(b) * Math.Cos(g);
							myQ.Enqueue((x, y, z));
							slowSensorDataCmd.Parameters.AddWithValue("@rot_x", x);
							slowSensorDataCmd.Parameters.AddWithValue("@rot_y", y);
							slowSensorDataCmd.Parameters.AddWithValue("@rot_z", z);
							slowSensorDataCmd.Parameters.AddWithValue("@grav_x", sensorsData100HZStruct.data.gravity[0]);
							slowSensorDataCmd.Parameters.AddWithValue("@grav_y", sensorsData100HZStruct.data.gravity[1]);
							slowSensorDataCmd.Parameters.AddWithValue("@grav_z", sensorsData100HZStruct.data.gravity[2]);
							slowSensorDataCmd.Parameters.AddWithValue("@activation", sensorsData100HZStruct.data.activation);
							slowSensorDataCmd.Parameters.AddWithValue("@time", GenerateSessionTimestampFromUSec((double)sensorsData100HZStruct.t.msec * 100.0));
							slowSensorDataCmd.ExecuteNonQuery();
							if (sensorsData100HZStruct.data.latitude != 0f)
							{
								DateTime time = GenerateSessionTimestampFromUSec((double)sensorsData100HZStruct.t.msec * 100.0);
								gpsDataCmd.Parameters.AddWithValue("@dataIndex", dataIndex);
								gpsDataCmd.Parameters.AddWithValue("@latitude", sensorsData100HZStruct.data.latitude);
								gpsDataCmd.Parameters.AddWithValue("@longitude", sensorsData100HZStruct.data.longitude);
								gpsDataCmd.Parameters.AddWithValue("@speed", sensorsData100HZStruct.data.speed);
								gpsDataCmd.Parameters.AddWithValue("@time", time);
								GPSData gd = new GPSData(dataIndex, time, sensorsData100HZStruct.data.speed);
								gd.coords.Lat = sensorsData100HZStruct.data.latitude;
								gd.coords.Lng = sensorsData100HZStruct.data.longitude;
								gd.speed = sensorsData100HZStruct.data.speed;
								gd.time = time;
								gps_data.Add(gd);
								gpsDataCmd.ExecuteNonQuery();
							}
							else
							{
								log.Warn("No longitude data");
							}
						}
						else
						{
							DateTime time2 = GenerateSessionTimestampFromUSec((double)sensorsData100HZStruct.t.msec * 100.0);
							gpsDataCmd.Parameters.AddWithValue("@dataIndex", dataIndex);
							gpsDataCmd.Parameters.AddWithValue("@latitude", sensorsData100HZStruct.data.latitude);
							gpsDataCmd.Parameters.AddWithValue("@longitude", sensorsData100HZStruct.data.longitude);
							gpsDataCmd.Parameters.AddWithValue("@speed", sensorsData100HZStruct.data.speed);
							gpsDataCmd.Parameters.AddWithValue("@time", time2);
							GPSData gd2 = new GPSData(dataIndex, time2, sensorsData100HZStruct.data.speed);
							gd2.coords.Lat = sensorsData100HZStruct.data.latitude;
							gd2.coords.Lng = sensorsData100HZStruct.data.longitude;
							gd2.speed = sensorsData100HZStruct.data.speed;
							gd2.time = time2;
							gps_data.Add(gd2);
							gpsDataCmd.ExecuteNonQuery();
						}
					}
					catch (Exception message)
					{
						log.Error(message);
						log.Error($"Unable to store sensor data with index {dataIndex}");
					}
					break;
				}
				default:
					log.Warn($"Unsupported data type {type}");
					reader.ReadChar();
					break;
				}
				if ((DateTime.Now - starttime).TotalSeconds > 1.0)
				{
					bps = (long)Math.Floor((double)(reader.BaseStream.Position - prev_pos) / (DateTime.Now - starttime).TotalSeconds);
					prev_pos = reader.BaseStream.Position;
					starttime = DateTime.Now;
				}
				if (bps != 0)
				{
					eta = (reader.BaseStream.Length - reader.BaseStream.Position) / bps;
				}
				prog?.Invoke((int)(reader.BaseStream.Position * 100 / reader.BaseStream.Length), bps, eta);
				dataIndex++;
			}
			if (dataIndex % 10000 == 0)
			{
				transaction.Commit();
			}
			base.MaxIndex = dataIndex;
		}
		catch (Exception ex5)
		{
			log.Warn(ex5.Message);
			transaction.Rollback();
		}
		finally
		{
			transaction.Commit();
		}
		try
		{
			SQLiteCommand fallCommand = new SQLiteCommand(sessionDBConn);
			fallCommand.CommandText = "SELECT dataIndex, activation FROM fastSensors where activation != 0 order by dataIndex";
			SQLiteDataReader reader3 = fallCommand.ExecuteReader();
			while (reader3.Read())
			{
				falls.Add(new Fall((uint)reader3.GetInt32(0), reader3.GetInt32(1)));
			}
			reader.Close();
			fallCommand.Reset();
			fallCommand.CommandText = "SELECT dataIndex, activation FROM slowSensors where activation != 0 order by dataIndex";
			reader3 = fallCommand.ExecuteReader();
			while (reader3.Read())
			{
				falls.Add(new Fall((uint)reader3.GetInt32(0), reader3.GetInt32(1)));
			}
			reader.Close();
		}
		catch (Exception)
		{
		}
		try
		{
			SQLiteCommand gpsReader = new SQLiteCommand(sessionDBConn);
			gpsReader.CommandText = "SELECT min(latitude), max(latitude), min(longitude), max(longitude) FROM gpssensors;";
			SQLiteDataReader reader4 = gpsReader.ExecuteReader();
			if (reader4.Read())
			{
				float lat = (reader4.GetFloat(1) + reader4.GetFloat(0)) / 2f;
				float lon = (reader4.GetFloat(3) + reader4.GetFloat(2)) / 2f;
				string tzIana = TimeZoneLookup.GetTimeZone(lat, lon).Result;
				TimeZoneInfo tzInfo = TZConvert.GetTimeZoneInfo(tzIana);
				DateTimeOffset convertedTime = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tzInfo);
				SQLiteCommand timezoneCommand = new SQLiteCommand(sessionDBConn);
				timezoneCommand.CommandText = "UPDATE gpssensors SET Timestamp = strftime('%Y-%m-%d %H:%M:%f', Timestamp, @tzoffset)";
				timezoneCommand.Parameters.AddWithValue("@tzoffset", convertedTime.Offset.TotalHours.ToString(CultureInfo.CreateSpecificCulture("en-GB")) + " hours");
				timezoneCommand.ExecuteNonQuery();
				timezoneCommand.CommandText = "UPDATE slowSensors SET Timestamp = strftime('%Y-%m-%d %H:%M:%f', Timestamp, @tzoffset)";
				timezoneCommand.ExecuteNonQuery();
				timezoneCommand.CommandText = "UPDATE fastSensors SET Timestamp = strftime('%Y-%m-%d %H:%M:%f', Timestamp, @tzoffset)";
				timezoneCommand.ExecuteNonQuery();
			}
			reader.Close();
			SQLiteCommand timeCommand = new SQLiteCommand(sessionDBConn);
			timeCommand.CommandText = "SELECT min(Timestamp), max(Timestamp) FROM gpssensors;";
			reader4 = timeCommand.ExecuteReader();
			if (reader4.Read())
			{
				minTime = reader4.GetDateTime(0);
				maxTime = reader4.GetDateTime(1);
			}
			reader4.Close();
		}
		catch (Exception)
		{
		}
	}

	public override void CloseDB()
	{
		try
		{
			if (slowSensorDataCmd != null)
			{
				slowSensorDataCmd.Dispose();
			}
			if (fastSensorDataCmd != null)
			{
				fastSensorDataCmd.Dispose();
			}
			if (sessionDBConn != null)
			{
				sessionDBConn.Dispose();
				sessionDBConn = null;
			}
		}
		catch (Exception)
		{
		}
	}

	public override bool isNewCriticalFallFormat()
	{
		return false;
	}

	public override void commit()
	{
		throw new NotImplementedException();
	}

	internal void InitDB()
	{
		throw new NotImplementedException();
	}

	internal void ClearData()
	{
		throw new NotImplementedException();
	}

	public void storeSensorDataToDB()
	{
		throw new NotImplementedException();
	}

	internal override void loadData(int minTime, int maxTime)
	{
		loadFromTo(sessionDBConn, minTime, maxTime);
	}

	private void loadFromTo(SQLiteConnection sessionDBConn, int minIndex, int maxIndex)
	{
		graphData.reset();
		int sampling = (maxIndex - minIndex) / 1000;
		if (sampling > 100)
		{
			selectDataCommand.CommandText = "SELECT dataIndex, acc_x, acc_y, acc_z, gyro_x, gyro_y, gyro_z, Timestamp FROM fastSensors WHERE DataIndex between @minIndex AND @maxIndex and (rowid % @sampling = 0) order by DataIndex";
			selectDataCommand.Parameters.AddWithValue("@sampling", sampling);
		}
		else if (sampling > 2)
		{
			selectDataCommand.CommandText = "SELECT dataIndex, avg(acc_x) as acc_x, avg(acc_y) as acc_y, avg(acc_z ) as acc_z, avg(gyro_x) as gyro_x, avg(gyro_y) as gyro_y, avg(gyro_z) as gyro_z, Timestamp FROM fastSensors WHERE DataIndex between @minIndex AND @maxIndex group by round(DataIndex / @sampling) order by DataIndex;";
			selectDataCommand.Parameters.AddWithValue("@sampling", sampling);
		}
		else
		{
			selectDataCommand.CommandText = "SELECT dataIndex, acc_x, acc_y, acc_z, gyro_x, gyro_y, gyro_z, Timestamp FROM fastSensors WHERE DataIndex between @minIndex AND @maxIndex order by DataIndex";
		}
		selectDataCommand.Parameters.AddWithValue("@minIndex", minIndex);
		selectDataCommand.Parameters.AddWithValue("@maxIndex", maxIndex);
		SQLiteDataReader reader = selectDataCommand.ExecuteReader();
		try
		{
			while (reader.Read())
			{
				SensorData sd = new SensorData((uint)reader.GetInt32(0), 2);
				sd.accelerometer[0][0] = reader.GetDouble(1);
				sd.accelerometer[0][1] = reader.GetDouble(2);
				sd.accelerometer[0][2] = reader.GetDouble(3);
				sd.gyro[0] = reader.GetDouble(4);
				sd.gyro[1] = reader.GetDouble(5);
				sd.gyro[2] = reader.GetDouble(6);
				sd.time = reader.GetDateTime(7);
				graphData.add(sd);
			}
		}
		catch (Exception)
		{
		}
		finally
		{
			reader.Close();
		}
		if (sampling > 100)
		{
			selectDataCommand.CommandText = "SELECT dataIndex, acc_0_x, acc_0_y, acc_0_z, acc_1_x, acc_1_y, acc_1_z, acc_2_x, acc_2_y, acc_2_z, acc_3_x, acc_3_y, acc_3_z, rot_x, rot_y, rot_z, grav_x, grav_y, grav_z, Timestamp FROM slowSensors WHERE DataIndex between @minIndex AND @maxIndex and (rowid % @sampling = 0) order by DataIndex";
			selectDataCommand.Parameters.AddWithValue("@sampling", sampling / 10);
			selectGPSDataCommand.CommandText = "SELECT dataIndex, latitude, longitude, speed, Timestamp FROM gpssensors WHERE (DataIndex between @minIndex AND @maxIndex) and (rowid % @sampling = 0) order by dataIndex";
			selectGPSDataCommand.Parameters.AddWithValue("@sampling", sampling);
		}
		else if (sampling > 2)
		{
			selectDataCommand.CommandText = "SELECT dataIndex, avg(acc_0_x ) as acc_0_x, avg(acc_0_y ) as acc_0_y, avg(acc_0_z ) as acc_0_z, avg(acc_1_x ) as acc_1_x, avg(acc_1_y ) as acc_1_y, avg(acc_1_z ) as acc_1_z, avg(acc_2_x ) as acc_2_x, avg(acc_2_y ) as acc_2_y,avg(acc_2_z ) as acc_2_z, avg(acc_3_x ) as acc_3_x, avg(acc_3_y ) as acc_3_y,avg(acc_3_z ) as acc_3_z, avg(rot_x) as rot_x, avg(rot_y) as rot_y, avg(rot_z) as rot_z, avg(grav_x) as grav_x, avg(grav_y) as grav_y, avg(grav_z) as grav_z, Timestamp FROM slowSensors WHERE DataIndex between @minIndex AND @maxIndex group by round(DataIndex / @sampling) order by DataIndex;";
			selectDataCommand.Parameters.AddWithValue("@sampling", sampling);
			selectGPSDataCommand.CommandText = "SELECT dataIndex, avg(latitude) as latitude, avg(longitude) as longitude, avg(speed) as speed, Timestamp FROM gpssensors WHERE DataIndex between @minIndex AND @maxIndex group by round(DataIndex / @sampling) order by dataIndex";
			selectGPSDataCommand.Parameters.AddWithValue("@sampling", sampling);
		}
		else
		{
			selectDataCommand.CommandText = "SELECT dataIndex, acc_0_x, acc_0_y, acc_0_z, acc_1_x, acc_1_y, acc_1_z, acc_2_x, acc_2_y, acc_2_z, acc_3_x, acc_3_y, acc_3_z, rot_x, rot_y, rot_z, grav_x, grav_y, grav_z, Timestamp FROM slowSensors WHERE DataIndex between @minIndex AND @maxIndex order by DataIndex";
			selectGPSDataCommand.CommandText = "SELECT dataIndex, latitude, longitude, speed, Timestamp FROM gpssensors WHERE DataIndex between @minIndex AND @maxIndex order by dataIndex";
		}
		selectDataCommand.Parameters.AddWithValue("@minIndex", minIndex);
		selectDataCommand.Parameters.AddWithValue("@maxIndex", maxIndex);
		selectGPSDataCommand.Parameters.AddWithValue("@minIndex", minIndex);
		selectGPSDataCommand.Parameters.AddWithValue("@maxIndex", maxIndex);
		reader = selectDataCommand.ExecuteReader();
		try
		{
			int i = 0;
			int j = 0;
			while (reader.Read())
			{
				SensorData sd2 = new SensorData((uint)reader.GetInt32(0), nofsensors);
				for (i = 0; i < nofsensors; i++)
				{
					for (j = 0; j < 3; j++)
					{
						sd2.accelerometer[i][j] = reader.GetDouble(1 + i * 3 + j);
					}
				}
				sd2.orientation = new double[3];
				sd2.gravity = new double[3];
				sd2.orientation[0] = reader.GetDouble(1 + i * 3);
				sd2.orientation[1] = reader.GetDouble(1 + i * 3 + 1);
				sd2.orientation[2] = reader.GetDouble(1 + i * 3 + 2);
				sd2.gravity[0] = reader.GetDouble(1 + i * 3 + 3);
				sd2.gravity[1] = reader.GetDouble(1 + i * 3 + 4);
				sd2.gravity[2] = reader.GetDouble(1 + i * 3 + 5);
				sd2.time = reader.GetDateTime(19);
				graphData.add(sd2);
			}
		}
		catch (Exception)
		{
		}
		finally
		{
			reader.Close();
		}
		try
		{
			reader = selectGPSDataCommand.ExecuteReader();
			while (reader.Read())
			{
				GPSData gd = new GPSData((uint)reader.GetInt32(0), reader.GetDateTime(4), reader.GetFloat(3));
				gd.coords.Lat = reader.GetDouble(1);
				gd.coords.Lng = reader.GetDouble(2);
				gd.speed = reader.GetFloat(3);
				gd.time = reader.GetDateTime(4);
				gps_data.Add(gd);
			}
		}
		catch (Exception)
		{
			log.Error("Unable to read GPS data");
		}
		finally
		{
			reader.Close();
		}
		GPSDataComparer gpsComparer = new GPSDataComparer();
		gps_data.Sort(gpsComparer);
		for (int k = 1; k < gps_data.Count - 1; k++)
		{
			if (((GPSData)gps_data[k + 1]).index >= minIndex && ((GPSData)gps_data[k - 1]).index <= maxIndex)
			{
				graphData.add((GPSData)gps_data[k]);
			}
		}
		DataPointComparer comparer = new DataPointComparer();
		ArrayList[] data = graphData.data;
		foreach (ArrayList dataList in data)
		{
			dataList.Sort(comparer);
		}
	}

	public void exportFromToAll(string FileName, int minIndex, int maxIndex)
	{
		try
		{
			SQLiteCommand selectFastDataCommand = new SQLiteCommand(sessionDBConn);
			SQLiteCommand selecSlowDataCommand = new SQLiteCommand(sessionDBConn);
			SQLiteCommand selectGPSDataCommand = new SQLiteCommand(sessionDBConn);
			selectFastDataCommand.CommandText = "SELECT Timestamp, dataIndex, acc_x, acc_y, acc_z, gyro_x, gyro_y, gyro_z FROM fastSensors WHERE DataIndex between @minIndex AND @maxIndex order by DataIndex";
			selectFastDataCommand.Parameters.AddWithValue("@minIndex", minIndex);
			selectFastDataCommand.Parameters.AddWithValue("@maxIndex", maxIndex);
			SQLiteDataReader fastSensorDataReader = selectFastDataCommand.ExecuteReader();
			selecSlowDataCommand.CommandText = "SELECT Timestamp, dataIndex, acc_0_x, acc_0_y, acc_0_z, acc_1_x, acc_1_y, acc_1_z, acc_2_x, acc_2_y, acc_2_z, acc_3_x, acc_3_y, acc_3_z, mag_x, mag_y, mag_z, rot_x, rot_y, rot_z, grav_x, grav_y, grav_z FROM slowSensors WHERE DataIndex between @minIndex AND @maxIndex order by DataIndex";
			selecSlowDataCommand.Parameters.AddWithValue("@minIndex", minIndex);
			selecSlowDataCommand.Parameters.AddWithValue("@maxIndex", maxIndex);
			SQLiteDataReader slowSensorDataReader = selecSlowDataCommand.ExecuteReader();
			selectGPSDataCommand.CommandText = "SELECT Timestamp, dataIndex, speed FROM gpssensors WHERE dataIndex between @minIndex AND @maxIndex order by DataIndex";
			selectGPSDataCommand.Parameters.AddWithValue("@minIndex", minIndex);
			selectGPSDataCommand.Parameters.AddWithValue("@maxIndex", maxIndex);
			SQLiteDataReader gpsReader = selectGPSDataCommand.ExecuteReader();
			try
			{
				using StreamWriter sw = new StreamWriter(FileName);
				gpsReader.Read();
				bool hasFastData = fastSensorDataReader.Read();
				bool hasSlowData = slowSensorDataReader.Read();
				while (hasFastData || hasSlowData)
				{
					string line = "";
					bool readerHasGpsData = gpsReader.HasRows;
					if (readerHasGpsData)
					{
						int firstGpsData = gpsReader.GetInt32(1);
						int firstSensorDataReader = fastSensorDataReader.GetInt32(1);
						if (firstGpsData < firstSensorDataReader && readerHasGpsData)
						{
							gpsReader.Read();
						}
					}
					if (fastSensorDataReader.HasRows)
					{
						if (slowSensorDataReader.HasRows && slowSensorDataReader.GetInt32(1) < fastSensorDataReader.GetInt32(1))
						{
							for (int i = 0; i < (nofgraphs - nofsensors) * 3; i++)
							{
								line += ",";
							}
							for (int i = 0; i < nofsensors; i++)
							{
								for (int j = 0; j < 3; j++)
								{
									line = line + slowSensorDataReader.GetDouble(2 + i * 3 + j).ToString("G", CultureInfo.CreateSpecificCulture("en-UK")) + ",";
								}
							}
							hasSlowData = slowSensorDataReader.Read();
						}
						else
						{
							line = line + fastSensorDataReader.GetDouble(1).ToString("G", CultureInfo.CreateSpecificCulture("en-UK")) + ",";
							line = line + fastSensorDataReader.GetDouble(2).ToString("G", CultureInfo.CreateSpecificCulture("en-UK")) + ",";
							line = line + fastSensorDataReader.GetDouble(3).ToString("G", CultureInfo.CreateSpecificCulture("en-UK")) + ",";
							line = line + fastSensorDataReader.GetDouble(4).ToString("G", CultureInfo.CreateSpecificCulture("en-UK")) + ",";
							line = line + fastSensorDataReader.GetDouble(5).ToString("G", CultureInfo.CreateSpecificCulture("en-UK")) + ",";
							line = line + fastSensorDataReader.GetDouble(6).ToString("G", CultureInfo.CreateSpecificCulture("en-UK")) + ",";
							for (int i = 0; i < nofsensors * 3; i++)
							{
								line += ",";
							}
							hasFastData = fastSensorDataReader.Read();
						}
					}
					else
					{
						if (!slowSensorDataReader.HasRows)
						{
							break;
						}
						if (fastSensorDataReader.HasRows && slowSensorDataReader.GetInt32(1) < fastSensorDataReader.GetInt32(1))
						{
							for (int i = 0; i < (nofgraphs - nofsensors) * 3; i++)
							{
								line += ",";
							}
							for (int i = 0; i < nofsensors; i++)
							{
								for (int j = 0; j < 3; j++)
								{
									line = line + slowSensorDataReader.GetDouble(2 + i * 3 + j).ToString("G", CultureInfo.CreateSpecificCulture("en-UK")) + ",";
								}
							}
							hasSlowData = slowSensorDataReader.Read();
						}
						else
						{
							line = line + fastSensorDataReader.GetDouble(1).ToString("G", CultureInfo.CreateSpecificCulture("en-UK")) + ",";
							line = line + fastSensorDataReader.GetDouble(2).ToString("G", CultureInfo.CreateSpecificCulture("en-UK")) + ",";
							line = line + fastSensorDataReader.GetDouble(3).ToString("G", CultureInfo.CreateSpecificCulture("en-UK")) + ",";
							line = line + fastSensorDataReader.GetDouble(4).ToString("G", CultureInfo.CreateSpecificCulture("en-UK")) + ",";
							line = line + fastSensorDataReader.GetDouble(5).ToString("G", CultureInfo.CreateSpecificCulture("en-UK")) + ",";
							line = line + fastSensorDataReader.GetDouble(6).ToString("G", CultureInfo.CreateSpecificCulture("en-UK")) + ",";
							for (int i = 0; i < nofsensors * 3; i++)
							{
								line += ",";
							}
							hasFastData = fastSensorDataReader.Read();
						}
					}
					sw.WriteLine(line);
				}
			}
			catch (Exception)
			{
			}
			finally
			{
				slowSensorDataReader.Close();
				fastSensorDataReader.Close();
				gpsReader.Close();
			}
		}
		catch (Exception)
		{
		}
	}

	public override void exportFromTo(string FileName, int minIndex, int maxIndex)
	{
		try
		{
			SQLiteCommand selectFastDataCommand = new SQLiteCommand(sessionDBConn);
			SQLiteCommand selecSlowDataCommand = new SQLiteCommand(sessionDBConn);
			SQLiteCommand selectGPSDataCommand = new SQLiteCommand(sessionDBConn);
			selectFastDataCommand.CommandText = "SELECT Timestamp, dataIndex, acc_x, acc_y, acc_z, gyro_x, gyro_y, gyro_z FROM fastSensors WHERE DataIndex between @minIndex AND @maxIndex order by DataIndex";
			selectFastDataCommand.Parameters.AddWithValue("@minIndex", minIndex);
			selectFastDataCommand.Parameters.AddWithValue("@maxIndex", maxIndex);
			SQLiteDataReader fastSensorDataReader = selectFastDataCommand.ExecuteReader();
			try
			{
				using StreamWriter sw = new StreamWriter(FileName);
				bool hasFastData = fastSensorDataReader.Read();
				while (hasFastData)
				{
					string line = "";
					line = line + fastSensorDataReader.GetDouble(1).ToString("G", CultureInfo.CreateSpecificCulture("en-UK")) + ",";
					line = line + fastSensorDataReader.GetDouble(2).ToString("G", CultureInfo.CreateSpecificCulture("en-UK")) + ",";
					line = line + fastSensorDataReader.GetDouble(3).ToString("G", CultureInfo.CreateSpecificCulture("en-UK")) + ",";
					line = line + fastSensorDataReader.GetDouble(4).ToString("G", CultureInfo.CreateSpecificCulture("en-UK")) + ",";
					line = line + fastSensorDataReader.GetDouble(5).ToString("G", CultureInfo.CreateSpecificCulture("en-UK")) + ",";
					line = line + fastSensorDataReader.GetDouble(6).ToString("G", CultureInfo.CreateSpecificCulture("en-UK")) + ",";
					hasFastData = fastSensorDataReader.Read();
					sw.WriteLine(line);
				}
			}
			catch (Exception)
			{
			}
			finally
			{
				fastSensorDataReader.Close();
			}
		}
		catch (Exception)
		{
		}
	}
}
