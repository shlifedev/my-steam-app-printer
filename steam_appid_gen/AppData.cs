namespace steam_appid_gen;

public class AppData
{
    public string AppId;
    public string AppName;
    public int PlayMinute;
    public int PlayHour => PlayMinute / 60;
}