using System;

namespace PufferFish;

public class Tag
{
	public long id;

	public string type;

	public string description;

	public DateTime timestamp;

	public Tag(long id, string type, string description, DateTime timestamp)
	{
		this.id = id;
		this.type = type;
		this.description = description;
		this.timestamp = timestamp;
	}
}
