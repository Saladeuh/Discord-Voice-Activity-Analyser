using System.Globalization;

internal class Util
{
  public static DateTimeOffset ParseIso8601(string iso8601String)
  {
    return DateTimeOffset.ParseExact(
        iso8601String,
        new string[] { "yyyy-MM-d'T'HH:mm:ssZ", "yyyy-MM-d'T'HH:mm:ss.fffZ" }, // 2018 - 01 - 05T15:18:00.137Z 
        CultureInfo.InvariantCulture,
        DateTimeStyles.AdjustToUniversal);
  }
}