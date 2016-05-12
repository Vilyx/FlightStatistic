using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


class Utils
{
	public static string userName;
	public static string CorrectNumber(string num, int cAD = 2)
	{
		string strToRet = num;
		int commaIndex = strToRet.IndexOf(".");
		if (commaIndex != -1 && strToRet.Length > commaIndex + cAD + 1)
		{
			strToRet = strToRet.Substring(0, commaIndex + cAD + 1);
		}
		else if (commaIndex == -1)
		{
			commaIndex = strToRet.Length;
		}
		for (int i = commaIndex - 3; i > 0; i -= 3)
			strToRet = strToRet.Insert(i, " ");
		return strToRet;
	}
	public static string TicksToDate(object p)
	{
		if (p == null) return "-";
		/*60 Kerbin seconds per Kerbin minute
		60 Kerbin minutes per Kerbin hour
		6 Kerbin hours per Kerbin day
		6.43 Kerbin days per Kerbin month
		426.08 Kerbin days per Kerbin year*/
		double seconds = long.Parse(p.ToString()) / TimeSpan.TicksPerSecond;
		long minutes = (long)(seconds / 60);
		long hours = (long)(minutes / 60);
		long days = (long)(hours / 6);
		long years = (long)(days / 426.08);

		string yearsStr = EnlargeByZeroes((years + 1).ToString(), 2);
		string daysStr = EnlargeByZeroes(((long)(days - years * 426.08) + 1).ToString(), 2);
		string hoursStr = EnlargeByZeroes((hours - days * 6).ToString(), 2);
		string minutesStr = EnlargeByZeroes((minutes - hours * 60).ToString(), 2);
		string secondsStr = EnlargeByZeroes((seconds - minutes * 60).ToString(), 2);
		return yearsStr + "." + daysStr + "/" + hoursStr + ":" + minutesStr + ":" + secondsStr;
	}
	public static string TicksToTotalTime(object p)
	{
		if (p == null) return "-";
		long ticks = long.Parse(p.ToString());
		long days = ticks / (TimeSpan.TicksPerDay / 4);
		long hours = ticks % (TimeSpan.TicksPerDay / 4) / TimeSpan.TicksPerHour;
		long minutes = ticks % TimeSpan.TicksPerHour / TimeSpan.TicksPerMinute;
		long seconds = ticks % TimeSpan.TicksPerMinute / TimeSpan.TicksPerSecond;
		return "D" + days.ToString() + ", " + hours.ToString() + ":" + EnlargeByZeroes(minutes.ToString(), 2) + ":" + EnlargeByZeroes(seconds.ToString(), 2);
	}
	public static string EnlargeByZeroes(string source, int minLen)
	{
		while (source.Length < minLen)
			source = "0" + source;
		return source;
	}
	public static String GetRootPath()
	{
		String path = KSPUtil.ApplicationRootPath;
		path = path.Replace("\\", "/");
		if (path.EndsWith("/")) path = path.Substring(0, path.Length - 1);
		//
		return path;
	}
}
