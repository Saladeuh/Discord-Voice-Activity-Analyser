using System.Globalization;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;

internal class Program
{
  public static List<string> DictToList(Dictionary<string, int> d)
  {
    List<string> list = new List<string>();
    foreach (string key in d.Keys)
    {
      list.Add($"{key}:{d[key]}");
    }
    return list;
  }
  public static List<T> JsonConverter<T>(string fileName)
  {
    string jsonString = File.ReadAllText(fileName);
    String[] subs = jsonString.Split('\n');
    List<T> listt = new List<T>();
    foreach (string s in subs)
    {
      if (s != "")
        listt.Add(JsonSerializer.Deserialize<T>(s));
    }
    Console.WriteLine("fini");
    return listt;
  }
  public static void WriteEveontsTypesFile(List<Analytics> listAnalytics)
  {
    Dictionary<string, int> e_types = new Dictionary<string, int>(); // associe un event avec son nombre d'occurences
    foreach (var a in listAnalytics)
    {
      try
      {
        e_types.Add(a.event_type, 0);
      }
      catch (ArgumentException)
      {
        e_types[a.event_type] += 1;
      }
    }
    File.WriteAllLines("Event_types.txt", DictToList(e_types));
    Console.WriteLine(e_types.LongCount());
  }
  public static (List<DateTime>, List<DateTime>) WriteDatesFile(List<Analytics> listAnalytics)
  {
    listAnalytics = listAnalytics.OrderBy(a => a.timestamp).ToList();
    List<DateTime> joinDates = new List<DateTime>();
    List<DateTime> leaveDates = new List<DateTime>();
    var joinDateString = new List<string>();
    var leaveDateString = new List<string>();
    foreach (var a in listAnalytics)
    {
      if (a.timestamp is not null && a.timestamp != "")
      {
        var originalTimeStamp = ParseIso8601(a.timestamp.Replace("\"", "")).UtcDateTime;
        var timestamp = new DateTime(originalTimeStamp.Year, originalTimeStamp.Month, originalTimeStamp.Day, originalTimeStamp.Hour, originalTimeStamp.Minute, 0, originalTimeStamp.Kind);
        if ((a.event_type == "join_voice_channel" || a.event_type == "join_call") && !joinDates.Contains(timestamp))
        {
          joinDates.Add(timestamp);
          joinDateString.Add($"j {a.timestamp}: {a.voice_state_count}");
        }
        if ((a.event_type == "leave_voice_channel" || a.event_type=="session_end" ||  a.event_type == "voice_disconnect" ) && !leaveDates.Contains(timestamp))
        {
          leaveDates.Add(timestamp);
          leaveDateString.Add($"l {a.timestamp}: {a.voice_state_count}");
        }
      }
    }
    var count = 0;
    for (int j = 0; j < joinDates.Count; j++)
    {
      DateTime jd = joinDates[j];
      if (leaveDates.IndexOf(jd) != -1)
      {
        leaveDates.RemoveAt(leaveDates.IndexOf(jd));
        //oinDates.RemoveAt(j);
        count++;
      }
    }
    Console.WriteLine(count.ToString());
    var allDateString = joinDateString.Concat(leaveDateString);
    allDateString = allDateString.OrderBy(item => item.Replace("j", "").Replace("l", "")).ToList();
    File.WriteAllLines("dates.txt", allDateString);
    return (joinDates, leaveDates);
  }
  public static DateTimeOffset ParseIso8601(string iso8601String)
  {
    return DateTimeOffset.ParseExact(
        iso8601String,
        new string[] { "yyyy-MM-d'T'HH:mm:ssZ", "yyyy-MM-d'T'HH:mm:ss.fffZ" }, // 2018 - 01 - 05T15:18:00.137Z 
        CultureInfo.InvariantCulture,
        DateTimeStyles.AdjustToUniversal);
  }
  static void Main(string[] args)
  {
    List<Analytics> analyticss = JsonConverter<Analytics>(@"C:/Users/emili/package/activity/analytics/events-2023-00000-of-00001.json");
    //WriteEveontsTypesFile(analyticss);
    var (joinDates, leaveDate) = WriteDatesFile(analyticss);
    Console.Write($"{joinDates.Count} {leaveDate.Count}");
    List<(double, DateTime, DateTime)> dateInterval = new List<(double, DateTime, DateTime)>();
    var current = DateTime.MinValue;
    foreach (var join in joinDates)
    {
      foreach (var leave in leaveDate)
      {
        if (join > current)
        {
          if (join < leave)
          {
            var inter = leave.Subtract(join);
            if (inter.TotalMinutes > 1 && inter.TotalDays <= 2)
            {
              dateInterval.Add((inter.TotalMinutes, join, leave));
              current = leave;
              break;
            }
          }
        }
      }
    }
    var sortedList = dateInterval.OrderBy(item => item.Item1).ToList();
    Console.WriteLine($"{sortedList[sortedList.Count()-1]}");
    List<string> dateIntervalStrings = new List<string>();
    foreach (var interval in dateInterval)
      dateIntervalStrings.Add(interval.Item1.ToString() + " " + interval.Item2.ToString() + " " + interval.Item3.ToString());
    File.WriteAllLines("intevals.txt", dateIntervalStrings);
    Console.WriteLine((getTotalMinutes(dateInterval)/60/(7.5*365)).ToString());
    Console.ReadLine();
  }
  public static double getTotalMinutes(List<(double, DateTime, DateTime)> dateInterval)
  {
    double total = 0;
    foreach(var inter in dateInterval)
    {
      total += inter.Item1;
    }
    return total;
  }
}