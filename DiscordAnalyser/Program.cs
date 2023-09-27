using System.Text;
using System.Text.Json;

class Program
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
  public async static Task<List<T>> JsonConverter<T>(string fileName)
  {
    var buffer = new StringBuilder();
    List<T> listt = new List<T>();
    foreach (var line in File.ReadLines(fileName))
    {
      //buffer.Append(line);
      listt.Add(JsonSerializer.Deserialize<T>(line));
    }
    /*
    String[] subs = buffer.ToString().Split('\n');
    List<T> listt = new List<T>();
    foreach (string s in b)
    {
      if (s != "")
        listt.Add(JsonSerializer.Deserialize<T>(s));
    }
    */
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
  }
  public static (List<(DateTime, string)>, List<(DateTime, string)>) WriteDatesFile(List<Analytics> listAnalytics)
  {
    listAnalytics = listAnalytics.OrderBy(a => a.timestamp).ToList();
    var joinDates = new List<(DateTime, string)>();
    var leaveDates = new List<(DateTime, string)>();
    var joinDateString = new List<string>();
    var leaveDateString = new List<string>();
    foreach (var a in listAnalytics)
    {
      if (a.timestamp is not null && a.timestamp != "")
      {
        var originalTimeStamp = Util.ParseIso8601(a.timestamp.Replace("\"", "")).UtcDateTime;
        var timestamp = new DateTime(originalTimeStamp.Year, originalTimeStamp.Month, originalTimeStamp.Day, originalTimeStamp.Hour, originalTimeStamp.Minute, 0, originalTimeStamp.Kind);
        if ((a.event_type == "join_voice_channel" || a.event_type == "join_call") && !joinDates.Any(item => item.Item1 == timestamp))
        {
          joinDates.Add((timestamp, a.channel_id));
          joinDateString.Add($"j {a.timestamp}: {a.voice_state_count}");
        }
        if ((a.event_type == "leave_voice_channel" || a.event_type == "session_end" || a.event_type == "voice_disconnect") && !leaveDates.Any(item => item.Item1 == timestamp))
        {
          leaveDates.Add((timestamp, a.channel_id));
          leaveDateString.Add($"l {a.timestamp}: {a.voice_state_count}");
        }
      }
    }
    var count = 0;
    for (int j = 0; j < joinDates.Count; j++)
    {
      var jd = joinDates[j];
      if (leaveDates.IndexOf(jd) != -1)
      {
        leaveDates.RemoveAt(leaveDates.IndexOf(jd));
        //oinDates.RemoveAt(j);
        count++;
      }
    }
    var allDateString = joinDateString.Concat(leaveDateString);
    allDateString = allDateString.OrderBy(item => item.Replace("j", "").Replace("l", "")).ToList();
    File.WriteAllLines("dates.txt", allDateString);
    return (joinDates, leaveDates);
  }
  private static List<(double, DateTime, DateTime, string)> getIntervals(List<(DateTime, string)> joinDates, List<(DateTime, string)> leaveDate)
  {
    var dateInterval = new List<(double, DateTime, DateTime, string)>();
    var current = DateTime.MinValue;
    foreach (var join in joinDates)
    {
      foreach (var leave in leaveDate)
      {
        if (join.Item1 > current)
        {
          if (join.Item1 < leave.Item1)
          {
            var inter = leave.Item1.Subtract(join.Item1);
            if (inter.TotalMinutes > 1 && inter.TotalDays <= 2)
            {
              dateInterval.Add((inter.TotalMinutes, join.Item1, leave.Item1, join.Item2));
              current = leave.Item1;
              break;
            }
          }
        }
      }
    }
    return dateInterval;
  }

  public static double getTotalMinutes(List<(double, DateTime, DateTime)> dateInterval)
  {
    double total = 0;
    foreach (var inter in dateInterval)
    {
      total += inter.Item1;
    }
    return total;
  }
  public static async Task Main()
  {
    Console.WriteLine("Placez votre dossier package dézippé dans le même dossier que ce programme. Quand c'est fait appuyez sur entrée");
    Console.ReadLine();
    Console.WriteLine("Analyse des fichiers...");
    List<Analytics> analyticss = await JsonConverter<Analytics>(@"package/activity/analytics/events-2023-00000-of-00001.json");
    string jsonString = File.ReadAllText(@"package/messages/index.json");
    var convs = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);
    //WriteEveontsTypesFile(analyticss);
    var (joinDates, leaveDate) = WriteDatesFile(analyticss);
    var dateIntervalChannel = getIntervals(joinDates, leaveDate);
    var dateInterval = dateIntervalChannel.Select(item => (item.Item1, item.Item2, item.Item3)).ToList();
    var sortedIntervals = dateIntervalChannel.OrderBy(item => item.Item1).ToList();
    /*
    List<string> dateIntervalStrings = new List<string>();
    foreach (var interval in dateIntervalChannel)
      dateIntervalStrings.Add(interval.Item1.ToString() + " " + interval.Item2.ToString() + " " + interval.Item3.ToString());
    File.WriteAllLines("intevals.txt", dateIntervalStrings);
    */
    printBasicStats(ref dateInterval, ref sortedIntervals, ref convs);
    printTopChannels(dateIntervalChannel, ref convs);
    Console.ReadLine();
  }

  private static void printBasicStats(ref List<(double, DateTime, DateTime)> dateInterval, ref List<(double, DateTime, DateTime, string)> sortedIntervals, ref Dictionary<string, string> convs)
  {
    (double, DateTime, DateTime, string) mostLong = sortedIntervals[sortedIntervals.Count() - 1];
    Console.WriteLine($"Vous avez passé environ {Math.Round((getTotalMinutes(dateInterval) / 60 / 24), 2)} jours en tvocal Discord");
    Console.WriteLine($"Le plus long vocal sans interruption dur {mostLong.Item1} mn, soit {mostLong.Item1 / 60} heures du {mostLong.Item2} au {mostLong.Item3}: {convs.GetValueOrDefault(mostLong.Item4, "channel inconnu")}");
    for (int i = 1; i < 10; i++)
    {
      (double, DateTime, DateTime, string) interval = sortedIntervals[sortedIntervals.Count() - (1+i)];
      Console.WriteLine($"Le {i+1}e vocal plus long: {interval.Item1} mn, soit {Math.Round(interval.Item1 / 60,2)} heures du {interval.Item2} au {interval.Item3}: {convs.GetValueOrDefault(mostLong.Item4, "channel inconnu")}");
    }
  }

  private static void printTopChannels(List<(double, DateTime, DateTime, string)> dateIntervalChannel, ref Dictionary<string, string> convs)
  {
    var groupByChannel = dateIntervalChannel.GroupBy(item => item.Item4).OrderByDescending(group =>
    {
      double fullTime = 0;
      foreach (var interval in group)
      {
        fullTime += interval.Item1;
      }
      return fullTime;
    });
    foreach (var group in groupByChannel.Take(10))
    {
      double fullTime = 0;
      foreach (var interval in group)
      {
        fullTime += interval.Item1;
      }
      Console.WriteLine($"{convs.GetValueOrDefault(group.Key.ToString(), "inconnu")}: {Math.Round(fullTime / 60, 2)}");
    }
    Console.Write("Voulez-vous voir la suite (o/n) ? ");
    string response = Console.ReadLine();
    if (response.ToLower() == "o")
    {
      foreach (var group in groupByChannel.Skip(10))
      {
        double fullTime = 0;
        foreach (var interval in group)
        {
          fullTime += interval.Item1;
        }
        Console.WriteLine($"{convs.GetValueOrDefault(group.Key.ToString(), "inconnu")}: {Math.Round(fullTime / 60, 2)}");
      }
    }
  }
}