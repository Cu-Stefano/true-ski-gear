using System;
using System.Text.RegularExpressions;

namespace PufferFish;

public class SessionV2Tag
{
	public int deviceID;

	public uint sessionID;

	public string fileName;

	public DateTime? startDate;

	public SessionV2Tag(int deviceID, uint sessionID, string fileName)
	{
		this.deviceID = deviceID;
		this.sessionID = sessionID;
		this.fileName = fileName;
		string sess_file_pattern = "([0-9]{4})([0-9]{2})([0-9]{2})_[0-9]*_[0-9]*.dat";
		Match m = Regex.Match(fileName, sess_file_pattern);
		if (m != null && m.Success)
		{
			startDate = new DateTime(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value));
		}
		else
		{
			startDate = null;
		}
	}
}
