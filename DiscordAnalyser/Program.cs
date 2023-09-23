using System.Globalization;
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
    foreach (var a in listAnalytics)
    {
      if ((a.event_type == "join_voice_channel" || a.event_type == "join_call") && a.timestamp != "")
      {
        joinDates.Add(ParseIso8601(a.timestamp.Replace("\"", "")).UtcDateTime);
      }
      if (a.event_type == "leave_voice_channel" && a.timestamp != "")
        leaveDates.Add(ParseIso8601(a.timestamp.Replace("\"", "")).UtcDateTime);
    }
    var joinDateString = new List<string>();
    foreach (var a in joinDates)
    {
      joinDateString.Add(a.ToLongDateString());
    }
    File.WriteAllLines("dates.txt", joinDateString);
    return (joinDates, leaveDates);
  }
  public static DateTimeOffset ParseIso8601(string iso8601String)
  {
    return DateTimeOffset.ParseExact(
        iso8601String,
        new string[] { "yyyy-MM-d'T'HH:mm:ssZ", "yyyy-MM-d'T'HH:mm:ss.fffZ" }, // 2018 - 01 - 05T15:18:00.137Z 
                                                                               //new string[] { "yyyy-MM-dd'T'HH:mm:ss.FFFK" },
        CultureInfo.InvariantCulture,
        DateTimeStyles.AdjustToUniversal);
  }
  static void Main(string[] args)
  {
    List<Analytics> analyticss = JsonConverter<Analytics>(@"C:/Users/emili/package/activity/analytics/events-2022-00000-of-00001.json");
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
            dateInterval.Add((leave.Subtract(join).TotalMinutes, join, leave));
            current = leave;
            break;
          }
        }
      }
    }
    List<string> dateIntervalStrings = new List<string>();
    foreach (var interval in dateInterval)
      dateIntervalStrings.Add(interval.Item1.ToString() + " " + interval.Item2.ToString() + " " + interval.Item3.ToString());
    File.WriteAllLines("intevals.txt", dateIntervalStrings);
  }
}