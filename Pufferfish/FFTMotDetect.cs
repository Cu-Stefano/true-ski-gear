using System.Linq;
using Numpy;

namespace PufferFish;

internal class FFTMotDetect
{
	private const int CFG_FFT_DB_SIZE = 64;

	private float[] db1;

	private float[] db2;

	private float[] current_db;

	private int current_db_size;

	private float[] avg;

	private int avg_idx;

	private int avg_init;

	public FFTMotDetect()
	{
		db1 = new float[64];
		db2 = new float[64];
		current_db_size = 0;
		current_db = db1;
		avg = new float[10];
		avg_idx = 0;
		avg_init = 0;
	}

	public int AddSample(float data)
	{
		current_db[current_db_size++] = data;
		if (current_db_size == 64)
		{
			if (current_db == db1)
			{
				current_db_size = 0;
				current_db = db2;
				return 1;
			}
			current_db_size = 0;
			current_db = db1;
			return 2;
		}
		return 0;
	}

	public double Compute(int db_num)
	{
		float[] db;
		switch (db_num)
		{
		case 1:
			db = db1;
			break;
		case 2:
			db = db2;
			break;
		default:
			return 0.0;
		}
		NDarray res = np.fft.rfft(db, null, -1);
		int idx_low = 17;
		int idx_high = 20;
		res = np.absolute(res[$"{idx_low}:{idx_high}"]);
		res = np.float_power(res, np.array<int>(2));
		res = np.multiply(res, np.array<double>(1.0 / 64.0));
		res = np.log10(res);
		res = np.multiply(res, np.array<double>(10.0));
		return np.mean(res);
	}

	public void FilterAddSample(float val)
	{
		avg[avg_idx] = val;
		avg_idx++;
		if (avg_idx >= avg.Count())
		{
			avg_idx = 0;
			avg_init = 1;
		}
	}

	public double FilterCompute()
	{
		if (avg_init == 1)
		{
			return np.mean(avg.Take(avg_idx).ToArray());
		}
		return np.mean(avg);
	}
}
