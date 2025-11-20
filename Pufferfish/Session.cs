using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using log4net;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace PufferFish;

internal class Session : BaseSession
{
	public new const int maxNumberOfGraphs = 9;

	public new const int maxNumberOfSensors = 7;

	public new static int gyro_index = 7;

	public new static int speed_index = 8;

	public Address sessionAddress;

	public PFSize sessionSize;

	private uint dataCount = 0u;

	private int deviceID;

	private SQLiteCommand gpsDataCmd;

	private static readonly ILog log = LogManager.GetLogger("PufferFish");

	private SQLiteCommand selectDataCommand;

	private SQLiteCommand selectGPSDataCommand;

	private SQLiteCommand sensorDataCmd;

	private uint sessionID;

	private SQLiteCommand tagCmd;

	private SQLiteTransaction transaction;

	private bool endReached;

	private bool newCriticalFallFormat;

	internal DateTime startDate;

	public ulong firstGPSindex { get; private set; }

	public uint getSensorsDataCount => dataCount;

	public int getSensorsGraphCount => graphData.getDataCount();

	public Session(int deviceID, uint sessionID, PFSize sessionSize, Address sessionAddress)
		: base(9, 7)
	{
		log.Info($"Initializing session {sessionID} for {deviceID} with size of {sessionSize}");
		this.sessionID = sessionID;
		this.sessionSize = sessionSize;
		this.sessionAddress = sessionAddress;
		this.deviceID = deviceID;
		newCriticalFallFormat = false;
		graphData.reset();
	}

	public Session(int deviceID, uint sessionid, string filename)
		: base(9, 7)
	{
		log.Info($"Initializing session {sessionid} for {deviceID} device from {filename}");
		this.deviceID = deviceID;
		sessionID = sessionid;
		newCriticalFallFormat = false;
		graphData.reset();
		if (!File.Exists(filename))
		{
			SQLiteConnection.CreateFile(filename);
		}
		sessionDBConn = new SQLiteConnection($"Data Source={filename};Version=3;");
		sessionDBConn.Open();
		selectDataCommand = new SQLiteCommand(sessionDBConn);
		selectGPSDataCommand = new SQLiteCommand(sessionDBConn);
		SQLiteCommand command = new SQLiteCommand("SELECT count(*) - coalesce(max(dataIndex), 0) FROM gpssensors;", sessionDBConn);
		if ((long)command.ExecuteScalar() > 0)
		{
			command = new SQLiteCommand("BEGIN; \r\n                delete from sensors where timestamp < date('2016-01-01');\r\n                CREATE INDEX IF NOT EXISTS timestampIdx ON sensors(timestamp);\r\n                CREATE INDEX IF NOT EXISTS dataIdx ON sensors(dataIndex);\r\n                CREATE TABLE gpssensors_temp(Timestamp DATETIME DEFAULT null, dataIndex INTEGER, latitude, longitude, speed, angle);\r\n                INSERT INTO gpssensors_temp SELECT * FROM gpssensors;\r\n                UPDATE gpssensors_temp set dataIndex = -1, Timestamp = timestamp || '.001';\r\n                UPDATE gpssensors_temp set dataIndex = (SELECT dataindex from sensors where sensors.timestamp = gpssensors_temp.timestamp);\r\n                DROP TABLE gpssensors;\r\n                ALTER TABLE gpssensors_temp RENAME TO gpssensors;\r\n                COMMIT;", sessionDBConn);
			command.ExecuteNonQuery();
		}
		command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS gpssensors (Timestamp DATETIME DEFAULT null, dataIndex INTEGER UNIQUE ON CONFLICT REPLACE, latitude, longitude, speed, angle);", sessionDBConn);
		command.ExecuteNonQuery();
		command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS sensors (Timestamp DATETIME DEFAULT null, dataIndex INTEGER UNIQUE ON CONFLICT REPLACE, acc_0_x, acc_0_y, acc_0_z, acc_1_x, acc_1_y, acc_1_z, acc_2_x, acc_2_y, acc_2_z, acc_3_x, acc_3_y, acc_3_z, acc_4_x, acc_4_y, acc_4_z, acc_5_x, acc_5_y, acc_5_z, acc_6_x, acc_6_y, acc_6_z, gyro_x, gyro_y, gyro_z, fall);", sessionDBConn);
		command.ExecuteNonQuery();
		command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS tags (id INTEGER PRIMARY KEY AUTOINCREMENT, Timestamp DATETIME DEFAULT null, type TEXT, description TEXT);", sessionDBConn);
		command.ExecuteNonQuery();
		command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS notes (type TEXT, description TEXT);", sessionDBConn);
		command.ExecuteNonQuery();
		command = new SQLiteCommand(" CREATE INDEX IF NOT EXISTS timestampIdx ON sensors(timestamp);", sessionDBConn);
		command.ExecuteNonQuery();
		command = new SQLiteCommand(" CREATE INDEX IF NOT EXISTS dataIdx ON sensors(dataIndex);", sessionDBConn);
		command.ExecuteNonQuery();
		sensorDataCmd = new SQLiteCommand(sessionDBConn);
		sensorDataCmd.CommandText = "INSERT INTO sensors (dataIndex, acc_0_x, acc_0_y, acc_0_z, acc_1_x, acc_1_y, acc_1_z, acc_2_x, acc_2_y, acc_2_z, acc_3_x, acc_3_y, acc_3_z, acc_4_x, acc_4_y, acc_4_z, acc_5_x, acc_5_y, acc_5_z, acc_6_x, acc_6_y, acc_6_z, gyro_x, gyro_y, gyro_z, fall, Timestamp) VALUES (@dataIndex, @acc_0_x, @acc_0_y, @acc_0_z, @acc_1_x, @acc_1_y, @acc_1_z, @acc_2_x, @acc_2_y, @acc_2_z, @acc_3_x, @acc_3_y, @acc_3_z, @acc_4_x, @acc_4_y, @acc_4_z, @acc_5_x, @acc_5_y, @acc_5_z, @acc_6_x, @acc_6_y, @acc_6_z, @gyro_x, @gyro_y, @gyro_z, @fall, @time);";
		gpsDataCmd = new SQLiteCommand(sessionDBConn);
		gpsDataCmd.CommandText = "INSERT INTO gpssensors (dataIndex, latitude, longitude, speed, angle, Timestamp) VALUES (@dataIndex, @latitude, @longitude, @speed, @angle, @time);";
		tagCmd = new SQLiteCommand(sessionDBConn);
		tagCmd.CommandText = "INSERT INTO tags (type, description, Timestamp) VALUES (@type, @description, @time);";
		if (!isFieldExist("sensors", "fall"))
		{
			command = new SQLiteCommand("alter table sensors add column fall", sessionDBConn);
			command.ExecuteNonQuery();
		}
		command.CommandText = "Select min(Timestamp), max(Timestamp), count(*), min(DataIndex), max(DataIndex) from sensors;";
		SQLiteDataReader reader = command.ExecuteReader();
		if (reader.Read())
		{
			minTime = reader.GetDateTime(0);
			maxTime = reader.GetDateTime(1);
			dataCount = (uint)reader.GetInt64(2);
			sessionSize = new PFSize(dataCount);
			base.MinIndex = (uint)reader.GetInt64(3);
			base.MaxIndex = (uint)reader.GetInt64(4);
		}
		reader.Close();
		command.CommandText = "SELECT dataIndex, latitude, longitude, speed, angle, Timestamp FROM gpssensors order by dataIndex";
		reader = command.ExecuteReader();
		while (reader.Read())
		{
			GPSData gd = new GPSData((uint)reader.GetInt32(0), reader.GetDateTime(5), reader.GetFloat(3))
			{
				coords = 
				{
					Lat = reader.GetDouble(1),
					Lng = reader.GetDouble(2)
				},
				speed = reader.GetFloat(3),
				angle = reader.GetFloat(4),
				time = reader.GetDateTime(5)
			};
			gps_data.Add(gd);
		}
		reader.Close();
		command.CommandText = "SELECT dataIndex, fall FROM sensors where fall != 0 order by dataIndex";
		reader = command.ExecuteReader();
		while (reader.Read())
		{
			if ((reader.GetInt32(1) & 0xF000000) == 167772160)
			{
				newCriticalFallFormat = true;
			}
			if ((reader.GetInt32(1) & 0xF0FFFFFFu) != 0)
			{
				falls.Add(new Fall((uint)reader.GetInt32(0), reader.GetInt32(1)));
			}
		}
		reader.Close();
		endReached = true;
	}

	public static PlotModel InitSessionPlotModel(List<LineSeries[]> series, List<LineAnnotation> lineAnnotations, Func<double, string> getTimeLabel)
	{
		PlotModel myModel = new PlotModel();
		int nofsensors = 7;
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
			case 6:
				Title = "Hip L";
				pos = 1;
				break;
			case 5:
				Title = "Elbow L";
				pos = 3;
				break;
			case 4:
				Title = "Shoul L";
				pos = 5;
				break;
			case 3:
				Title = "Shoul R";
				pos = 4;
				break;
			case 2:
				Title = "Elbow R";
				pos = 2;
				break;
			case 1:
				Title = "Hip R";
				pos = 0;
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
			StartPosition = (float)nofsensors / (float)nofgraphs,
			EndPosition = (float)(nofsensors + 1) / (float)nofgraphs,
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
			StartPosition = (float)(nofsensors + 1) / (float)nofgraphs,
			EndPosition = (float)(nofsensors + 2) / (float)nofgraphs,
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
		for (int m = 0; m < nofsensors; m++)
		{
			series[m][0].Color = OxyColors.Red;
			series[m][1].Color = OxyColors.Green;
			series[m][2].Color = OxyColors.Blue;
			lineAnnotations[m] = new LineAnnotation
			{
				Type = LineAnnotationType.Vertical,
				X = 10.0,
				ClipByYAxis = true,
				Color = OxyColors.White,
				LineStyle = LineStyle.Solid,
				StrokeThickness = 2.0,
				YAxisKey = series[m][0].YAxisKey
			};
			myModel.Annotations.Add(lineAnnotations[m]);
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
		return myModel;
	}

	public bool isFieldExist(string tableName, string fieldName)
	{
		SQLiteCommand command = new SQLiteCommand("PRAGMA table_info(" + tableName + ")", sessionDBConn);
		SQLiteDataReader reader = command.ExecuteReader();
		while (reader.Read())
		{
			if (reader.GetString(1) == fieldName)
			{
				return true;
			}
		}
		return false;
	}

	public override bool isNewCriticalFallFormat()
	{
		return newCriticalFallFormat;
	}

	public override void CloseDB()
	{
		try
		{
			commit();
			if (sensorDataCmd != null)
			{
				sensorDataCmd.Dispose();
			}
			if (gpsDataCmd != null)
			{
				gpsDataCmd.Dispose();
			}
			if (tagCmd != null)
			{
				tagCmd.Dispose();
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

	public override void commit()
	{
		if (transaction != null)
		{
			try
			{
				transaction.Commit();
			}
			catch
			{
			}
			transaction = null;
		}
	}

	public string getDbName()
	{
		return $"{deviceID}_{sessionID}.session";
	}

	public void storeSensorDataToDB(SensorData sd)
	{
		try
		{
			if (transaction == null)
			{
				transaction = sessionDBConn.BeginTransaction();
			}
			sensorDataCmd.Parameters.AddWithValue("@dataIndex", sd.index);
			sensorDataCmd.Parameters.AddWithValue("@acc_0_x", sd.accelerometer[0][0]);
			sensorDataCmd.Parameters.AddWithValue("@acc_0_y", sd.accelerometer[0][1]);
			sensorDataCmd.Parameters.AddWithValue("@acc_0_z", sd.accelerometer[0][2]);
			sensorDataCmd.Parameters.AddWithValue("@acc_1_x", sd.accelerometer[1][0]);
			sensorDataCmd.Parameters.AddWithValue("@acc_1_y", sd.accelerometer[1][1]);
			sensorDataCmd.Parameters.AddWithValue("@acc_1_z", sd.accelerometer[1][2]);
			sensorDataCmd.Parameters.AddWithValue("@acc_2_x", sd.accelerometer[2][0]);
			sensorDataCmd.Parameters.AddWithValue("@acc_2_y", sd.accelerometer[2][1]);
			sensorDataCmd.Parameters.AddWithValue("@acc_2_z", sd.accelerometer[2][2]);
			sensorDataCmd.Parameters.AddWithValue("@acc_3_x", sd.accelerometer[3][0]);
			sensorDataCmd.Parameters.AddWithValue("@acc_3_y", sd.accelerometer[3][1]);
			sensorDataCmd.Parameters.AddWithValue("@acc_3_z", sd.accelerometer[3][2]);
			sensorDataCmd.Parameters.AddWithValue("@acc_4_x", sd.accelerometer[4][0]);
			sensorDataCmd.Parameters.AddWithValue("@acc_4_y", sd.accelerometer[4][1]);
			sensorDataCmd.Parameters.AddWithValue("@acc_4_z", sd.accelerometer[4][2]);
			sensorDataCmd.Parameters.AddWithValue("@acc_5_x", sd.accelerometer[5][0]);
			sensorDataCmd.Parameters.AddWithValue("@acc_5_y", sd.accelerometer[5][1]);
			sensorDataCmd.Parameters.AddWithValue("@acc_5_z", sd.accelerometer[5][2]);
			sensorDataCmd.Parameters.AddWithValue("@acc_6_x", sd.accelerometer[6][0]);
			sensorDataCmd.Parameters.AddWithValue("@acc_6_y", sd.accelerometer[6][1]);
			sensorDataCmd.Parameters.AddWithValue("@acc_6_z", sd.accelerometer[6][2]);
			sensorDataCmd.Parameters.AddWithValue("@gyro_x", sd.gyro[0]);
			sensorDataCmd.Parameters.AddWithValue("@gyro_y", sd.gyro[1]);
			sensorDataCmd.Parameters.AddWithValue("@gyro_z", sd.gyro[2]);
			sensorDataCmd.Parameters.AddWithValue("@fall", sd.fall);
			sensorDataCmd.Parameters.AddWithValue("@time", sd.time);
			sensorDataCmd.ExecuteNonQuery();
		}
		catch (Exception message)
		{
			log.Error(message);
			log.Error($"Unable to store sensor data with index {sd.index}");
		}
		if (getSensorsDataCount % 100000 == 0)
		{
			commit();
		}
	}

	internal void addNote(string umidita, string pista, bool fondoIrregolare, bool pistaSporca, string note)
	{
		SQLiteCommand command = new SQLiteCommand("INSERT INTO notes (type, description) VALUES (@type, @description);", sessionDBConn);
		command.Parameters.AddWithValue("@type", "umidità");
		command.Parameters.AddWithValue("@description", umidita);
		command.ExecuteNonQuery();
		command.Parameters.AddWithValue("@type", "pista");
		command.Parameters.AddWithValue("@description", pista);
		command.ExecuteNonQuery();
		command.Parameters.AddWithValue("@type", "irregolare");
		command.Parameters.AddWithValue("@description", fondoIrregolare ? "si" : "no");
		command.ExecuteNonQuery();
		command.Parameters.AddWithValue("@type", "sporca");
		command.Parameters.AddWithValue("@description", pistaSporca ? "si" : "no");
		command.ExecuteNonQuery();
		command.Parameters.AddWithValue("@type", "note");
		command.Parameters.AddWithValue("@description", note);
		command.ExecuteNonQuery();
		command.Dispose();
	}

	internal long addTag(string type, string description, DateTime time)
	{
		tagCmd.Parameters.AddWithValue("@description", description);
		tagCmd.Parameters.AddWithValue("@type", type);
		tagCmd.Parameters.AddWithValue("@time", time);
		tagCmd.ExecuteNonQuery();
		return sessionDBConn.LastInsertRowId;
	}

	public Hashtable getNotes()
	{
		Hashtable ret = new Hashtable();
		SQLiteCommand command = new SQLiteCommand("SELECT type, description from notes", sessionDBConn);
		SQLiteDataReader reader = command.ExecuteReader();
		try
		{
			while (reader.Read())
			{
				ret.Add(reader.GetString(0), reader.GetString(1));
			}
		}
		catch (Exception)
		{
		}
		finally
		{
			reader.Close();
		}
		return ret;
	}

	internal void clearData()
	{
		graphData.reset();
		gps_data.Clear();
		falls.Clear();
		dataCount = 0u;
		firstGPSindex = 0uL;
		InitDB();
		endReached = false;
	}

	internal DataPoint getSensorDataItem(int i, int indexData)
	{
		return (DataPoint)graphData.data[i][indexData];
	}

	internal new Range<uint> GetSessionRange()
	{
		return new Range<uint>
		{
			start = base.MinIndex,
			end = base.MaxIndex + 50
		};
	}

	internal override void loadData(int minTime, int maxTime)
	{
		loadFromTo(sessionDBConn, minTime, maxTime);
	}

	internal void setEndreached()
	{
		commit();
		endReached = true;
	}

	internal void storeGPSData(GPSData sd)
	{
		if (base.getGPSDataCount == 0)
		{
			firstGPSindex = getSensorsDataCount;
			minTime = sd.time;
		}
		gps_data.Add(sd);
		try
		{
			if (transaction == null)
			{
				transaction = sessionDBConn.BeginTransaction();
			}
			gpsDataCmd.Parameters.AddWithValue("@dataIndex", sd.index);
			gpsDataCmd.Parameters.AddWithValue("@latitude", sd.coords.Lat);
			gpsDataCmd.Parameters.AddWithValue("@longitude", sd.coords.Lng);
			gpsDataCmd.Parameters.AddWithValue("@speed", sd.speed);
			gpsDataCmd.Parameters.AddWithValue("@angle", sd.angle);
			gpsDataCmd.Parameters.AddWithValue("@time", sd.time);
			gpsDataCmd.ExecuteNonQuery();
		}
		catch (Exception)
		{
		}
	}

	internal void addSensorDataToSession(SensorData sd)
	{
		dataCount++;
		if (sd.index < base.MinIndex || dataCount == 1)
		{
			base.MinIndex = sd.index;
		}
		if (sd.index > base.MaxIndex)
		{
			base.MaxIndex = sd.index;
		}
		if ((sd.fall & 0xF000000) == 167772160)
		{
			newCriticalFallFormat = true;
		}
		if ((sd.fall & 0xF0FFFFFFu) != 0)
		{
			falls.Add(new Fall(sd.index, sd.fall));
		}
	}

	private void InitDB()
	{
		CloseDB();
		string filename = getDbName();
		if (!File.Exists(BaseSession.getDBFolder() + filename))
		{
			SQLiteConnection.CreateFile(BaseSession.getDBFolder() + filename);
		}
		sessionDBConn = new SQLiteConnection($"Data Source={BaseSession.getDBFolder()}{filename};Version=3;");
		sessionDBConn.Open();
		selectDataCommand = new SQLiteCommand(sessionDBConn);
		SQLiteCommand command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS gpssensors (Timestamp DATETIME DEFAULT null, dataIndex INTEGER UNIQUE ON CONFLICT REPLACE, latitude, longitude, speed, angle);", sessionDBConn);
		command.ExecuteNonQuery();
		command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS sensors (Timestamp DATETIME DEFAULT null, dataIndex INTEGER UNIQUE ON CONFLICT REPLACE, acc_0_x, acc_0_y, acc_0_z, acc_1_x, acc_1_y, acc_1_z, acc_2_x, acc_2_y, acc_2_z, acc_3_x, acc_3_y, acc_3_z, acc_4_x, acc_4_y, acc_4_z, acc_5_x, acc_5_y, acc_5_z, acc_6_x, acc_6_y, acc_6_z, gyro_x, gyro_y, gyro_z, fall);", sessionDBConn);
		command.ExecuteNonQuery();
		command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS tags (id INTEGER PRIMARY KEY AUTOINCREMENT, Timestamp DATETIME DEFAULT null, type TEXT, description TEXT);", sessionDBConn);
		command.ExecuteNonQuery();
		command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS notes (type TEXT, description TEXT);", sessionDBConn);
		command.ExecuteNonQuery();
		command = new SQLiteCommand("CREATE INDEX IF NOT EXISTS dataIndexIdx ON sensors(dataIndex);", sessionDBConn);
		command.ExecuteNonQuery();
		command.Dispose();
		sensorDataCmd = new SQLiteCommand(sessionDBConn);
		sensorDataCmd.CommandText = "INSERT INTO sensors (dataIndex, acc_0_x, acc_0_y, acc_0_z, acc_1_x, acc_1_y, acc_1_z, acc_2_x, acc_2_y, acc_2_z, acc_3_x, acc_3_y, acc_3_z, acc_4_x, acc_4_y, acc_4_z, acc_5_x, acc_5_y, acc_5_z, acc_6_x, acc_6_y, acc_6_z, gyro_x, gyro_y, gyro_z, fall, Timestamp) VALUES (@dataIndex, @acc_0_x, @acc_0_y, @acc_0_z, @acc_1_x, @acc_1_y, @acc_1_z, @acc_2_x, @acc_2_y, @acc_2_z, @acc_3_x, @acc_3_y, @acc_3_z, @acc_4_x, @acc_4_y, @acc_4_z, @acc_5_x, @acc_5_y, @acc_5_z, @acc_6_x, @acc_6_y, @acc_6_z, @gyro_x, @gyro_y, @gyro_z, @fall, @time);";
		gpsDataCmd = new SQLiteCommand(sessionDBConn);
		gpsDataCmd.CommandText = "INSERT INTO gpssensors (dataIndex, latitude, longitude, speed, angle, Timestamp) VALUES (@dataIndex, @latitude, @longitude, @speed, @angle, @time);";
		tagCmd = new SQLiteCommand(sessionDBConn);
		tagCmd.CommandText = "INSERT INTO tags (type, description, Timestamp) VALUES (@type, @description, @time);";
		if (!isFieldExist("sensors", "fall"))
		{
			command = new SQLiteCommand("alter table sensors add column fall", sessionDBConn);
			command.ExecuteNonQuery();
		}
	}

	public override void exportFromTo(string FileName, int minIndex, int maxIndex)
	{
		selectDataCommand.CommandText = "SELECT Timestamp, dataIndex, acc_0_x, acc_0_y, acc_0_z, acc_1_x, acc_1_y, acc_1_z, acc_2_x, acc_2_y, acc_2_z, acc_3_x, acc_3_y, acc_3_z, acc_4_x, acc_4_y, acc_4_z, acc_5_x, acc_5_y, acc_5_z, acc_6_x, acc_6_y, acc_6_z, gyro_x, gyro_y, gyro_z, fall FROM sensors WHERE dataIndex between @minIndex AND @maxIndex order by DataIndex";
		selectDataCommand.Parameters.AddWithValue("@minIndex", minIndex);
		selectDataCommand.Parameters.AddWithValue("@maxIndex", maxIndex);
		SQLiteDataReader reader = selectDataCommand.ExecuteReader();
		selectGPSDataCommand.CommandText = "SELECT Timestamp, dataIndex, speed FROM gpssensors WHERE dataIndex between @minIndex AND @maxIndex order by DataIndex";
		selectGPSDataCommand.Parameters.AddWithValue("@minIndex", minIndex);
		selectGPSDataCommand.Parameters.AddWithValue("@maxIndex", maxIndex);
		SQLiteDataReader gps_reader = selectGPSDataCommand.ExecuteReader();
		try
		{
			using StreamWriter sw = new StreamWriter(FileName);
			gps_reader.Read();
			while (reader.Read())
			{
				string line = "";
				if (gps_reader.GetInt32(1) < reader.GetInt32(1) && gps_reader.HasRows)
				{
					gps_reader.Read();
				}
				for (int i = 0; i < 7; i++)
				{
					for (int j = 0; j < 3; j++)
					{
						line = ((!(reader.GetFieldType(2 + i * 3 + j) == typeof(double))) ? (line + ((double)reader.GetInt32(2 + i * 3 + j) / 16384.0).ToString("G", CultureInfo.CreateSpecificCulture("en-UK")) + ",") : (line + reader.GetDouble(2 + i * 3 + j).ToString("G", CultureInfo.CreateSpecificCulture("en-UK")) + ","));
					}
				}
				if (reader.GetFieldType(23) == typeof(double))
				{
					line = line + reader.GetDouble(23).ToString("G", CultureInfo.CreateSpecificCulture("en-UK")) + ",";
					line = line + reader.GetDouble(24).ToString("G", CultureInfo.CreateSpecificCulture("en-UK")) + ",";
					line = line + reader.GetDouble(25).ToString("G", CultureInfo.CreateSpecificCulture("en-UK")) + ",";
				}
				else
				{
					line = line + ((double)reader.GetInt32(23) * 0.0152587890625).ToString("G", CultureInfo.CreateSpecificCulture("en-UK")) + ",";
					line = line + ((double)reader.GetInt32(24) * 0.0152587890625).ToString("G", CultureInfo.CreateSpecificCulture("en-UK")) + ",";
					line = line + ((double)reader.GetInt32(25) * 0.0152587890625).ToString("G", CultureInfo.CreateSpecificCulture("en-UK")) + ",";
				}
				line = line + reader.GetInt32(26) + ",";
				line += gps_reader.GetFloat(2).ToString("G", CultureInfo.CreateSpecificCulture("en-UK"));
				sw.WriteLine(line);
			}
		}
		catch (Exception)
		{
		}
		finally
		{
			reader.Close();
			gps_reader.Close();
		}
	}

	private void loadFromTo(SQLiteConnection sessionDBConn, int minIndex, int maxIndex)
	{
		graphData.reset();
		int sampling = (maxIndex - minIndex) / 1000;
		if (sampling > 100)
		{
			selectDataCommand.CommandText = "SELECT dataIndex, acc_0_x, acc_0_y, acc_0_z, acc_1_x, acc_1_y, acc_1_z, acc_2_x, acc_2_y, acc_2_z, acc_3_x, acc_3_y, acc_3_z, acc_4_x, acc_4_y, acc_4_z, acc_5_x, acc_5_y, acc_5_z, acc_6_x, acc_6_y, acc_6_z, gyro_x, gyro_y, gyro_z, Timestamp FROM sensors WHERE DataIndex between @minIndex AND @maxIndex and (rowid % @sampling = 0) order by DataIndex";
			selectDataCommand.Parameters.AddWithValue("@sampling", sampling);
		}
		else if (sampling > 2)
		{
			selectDataCommand.CommandText = "SELECT dataIndex, avg(acc_0_x ) as acc_0_x, avg(acc_0_y ) as acc_0_y, avg(acc_0_z ) as acc_0_z, avg(acc_1_x ) as acc_1_x, avg(acc_1_y ) as acc_1_y, avg(acc_1_z ) as acc_1_z, avg(acc_2_x ) as acc_2_x, avg(acc_2_y ) as acc_2_y,avg(acc_2_z ) as acc_2_z, avg(acc_3_x ) as acc_3_x, avg(acc_3_y ) as acc_3_y,avg(acc_3_z ) as acc_3_z, avg(acc_4_x ) as acc_4_x, avg(acc_4_y ) as acc_4_y,avg(acc_4_z ) as acc_4_z, avg(acc_5_x ) as acc_5_x, avg(acc_5_y ) as acc_5_y,avg(acc_5_z ) as acc_5_z, avg(acc_6_x ) as acc_6_x, avg(acc_6_y ) as acc_6_y,avg(acc_6_z ) as acc_6_z, avg(gyro_x ) as gyro_x, avg(gyro_y ) as gyro_y, avg(gyro_z ) as gyro_z, Timestamp FROM sensors WHERE DataIndex between @minIndex AND @maxIndex group by round(DataIndex / @sampling) order by DataIndex;";
			selectDataCommand.Parameters.AddWithValue("@sampling", sampling);
		}
		else
		{
			selectDataCommand.CommandText = "SELECT dataIndex, acc_0_x, acc_0_y, acc_0_z, acc_1_x, acc_1_y, acc_1_z, acc_2_x, acc_2_y, acc_2_z, acc_3_x, acc_3_y, acc_3_z, acc_4_x, acc_4_y, acc_4_z, acc_5_x, acc_5_y, acc_5_z, acc_6_x, acc_6_y, acc_6_z, gyro_x, gyro_y, gyro_z, Timestamp FROM sensors WHERE DataIndex between @minIndex AND @maxIndex order by DataIndex";
		}
		selectDataCommand.Parameters.AddWithValue("@minIndex", minIndex);
		selectDataCommand.Parameters.AddWithValue("@maxIndex", maxIndex);
		SQLiteDataReader reader = selectDataCommand.ExecuteReader();
		try
		{
			while (reader.Read())
			{
				SensorData sd = new SensorData((uint)reader.GetInt32(0));
				for (int i = 0; i < 7; i++)
				{
					for (int j = 0; j < 3; j++)
					{
						if (reader.GetFieldType(1 + i * 3 + j) == typeof(double))
						{
							sd.accelerometer[i][j] = reader.GetDouble(1 + i * 3 + j) * 2.0;
						}
						else
						{
							sd.accelerometer[i][j] = (double)reader.GetInt32(1 + i * 3 + j) / 16384.0 * 2.0;
						}
					}
				}
				if (reader.GetFieldType(22) == typeof(double))
				{
					sd.gyro[0] = reader.GetDouble(22);
					sd.gyro[1] = reader.GetDouble(23);
					sd.gyro[2] = reader.GetDouble(24);
				}
				else
				{
					sd.gyro[0] = (double)reader.GetInt32(22) * 0.0152587890625;
					sd.gyro[1] = (double)reader.GetInt32(23) * 0.0152587890625;
					sd.gyro[2] = (double)reader.GetInt32(24) * 0.0152587890625;
				}
				sd.time = reader.GetDateTime(25);
				graphData.add(sd);
			}
			for (int i = 1; i < gps_data.Count - 1; i++)
			{
				if (((GPSData)gps_data[i + 1]).index >= minIndex && ((GPSData)gps_data[i - 1]).index <= maxIndex)
				{
					graphData.add((GPSData)gps_data[i]);
				}
			}
		}
		catch (Exception)
		{
		}
		finally
		{
			reader.Close();
		}
	}

	internal bool isCompleted()
	{
		return endReached;
	}
}
