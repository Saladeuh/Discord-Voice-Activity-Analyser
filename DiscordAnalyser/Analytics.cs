
public class Analytics
{
  public string event_type { get; set; }
  public string event_id { get; set; }
  public string event_source { get; set; }
  public string user_id { get; set; }
  public string domain { get; set; }
  public string freight_hostname { get; set; }
  public string ip { get; set; }
  public string day { get; set; }
  public string chosen_locale { get; set; }
  public string detected_locale { get; set; }
  public string browser { get; set; }
  public string device { get; set; }
  public string cfduid { get; set; }
  public string device_vendor_id { get; set; }
  public string os { get; set; }
  public string client_build_number { get; set; }
  public string release_channel { get; set; }
  public string client_version { get; set; }
  public string city { get; set; }
  public string country_code { get; set; }
  public string region_code { get; set; }
  public string time_zone { get; set; }
  public string channel_id { get; set; }
  public string channel_type { get; set; }
  public string rtc_connection_id { get; set; }
  public string voice_state_count { get; set; }
  public object[] accepted_languages { get; set; }
  public object[] accepted_languages_weighted { get; set; }
  public DateTime _hour_pt { get; set; }
  public DateTime _hour_utc { get; set; }
  public DateTime _day_pt { get; set; }
  public DateTime _day_utc { get; set; }
  public string client_send_timestamp { get; set; }
  public string client_track_timestamp { get; set; }
  public string timestamp { get; set; }
}