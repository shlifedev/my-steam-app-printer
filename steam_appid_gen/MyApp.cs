using Newtonsoft.Json.Linq;

namespace steam_appid_gen;

public class MyApp
{
    private static readonly HttpClient client = new HttpClient();
    public Config config; 
    public List<AppData> loaded = new List<AppData>();
    public List<AppData> added = new List<AppData>();
    public const int MAX_COUNT = 32;

    public async Task Initialize()
    {
        await VerifyConfigAndLoad();
    }

    async Task AppLogic()
    {
        while (true)
        {
            PrintMyGames();
            PrintAddedGames();
            WaitInput();
        }
    }

    void WaitInput()
    {
        string input = Console.ReadLine();
        if (int.TryParse(input, out int index))
        {
            AddByIndex(index);
        }
        else
        {
            AddByName(input);
        }
    }
    
    List<AppData> GetPrintList()
    {
        return loaded.FindAll(x => !added.Select(y => y.AppId).Contains(x.AppId)).OrderBy(x => x.PlayMinute).ToList();
    }
    void PrintMyGames()
    {
        var print = GetPrintList();
        for (int i = 0; i < print.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {print[i].AppName} ({print[i].PlayHour} 시간 플레이1)");
        }
    }

    void PrintAddedGames()
    {
        Console.WriteLine(string.Join(",",added.Select(x=>x.AppId)));
    }
    
    
    async Task VerifyConfigAndLoad()
    {
        if (System.IO.File.Exists("appConfig.json"))
        {
            config = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(await System.IO.File.ReadAllTextAsync("appConfig.json"));
        }
        else
        { 
            Console.WriteLine("API Key를 입력하세요.");
                string apiKey = Console.ReadLine();
                Console.WriteLine("Steam ID를 입력하세요.");
                string steamId = Console.ReadLine();

                config = new Config()
                {
                    apiKey = apiKey,
                    steamId = steamId
                };
                await System.IO.File.WriteAllTextAsync("appConfig.json",
                    Newtonsoft.Json.JsonConvert.SerializeObject(config));
        }

        try
        {
            loaded = await GetOwnedGames(config.apiKey, config.steamId);
            await AppLogic();
            
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            if( System.IO.File.Exists("appConfig.json"))
                System.IO.File.Delete("appConfig.json");

            Console.WriteLine("Restart Config..");
            await Task.Delay(3000);
            // Restart Logic
            await VerifyConfigAndLoad();
        }
 
    }
    
    public bool IsAppAdded(string appId)
    {  
        return added.Any(x => x.AppId == appId);
    }
    
    public async Task AddByName(string name)
    { 
        List<AppData> finded = loaded.FindAll(x => x.AppName.Contains(name));
        if (finded.Count == 0)
        {
            Console.WriteLine("아무것도 추가되지 않음.");
            await Task.Delay(1000);
            Console.Clear();
            return;
        }
        else
        {  
            if(finded.Count == 1) added.Add(finded[0]);
            else
            {
                Console.Clear();
                for (int i = 0; i < finded.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {finded[i].AppName} ({finded[i].PlayHour} 시간 플레이)");
                }
                string input2 = Console.ReadLine();
                if (int.TryParse(input2, out int index2) && index2 > 0 && index2 <= finded.Count)
                {
                    if (IsAppAdded(finded[index2 - 1].AppId))
                    {
                        Console.WriteLine("이미 추가된 게임입니다.");
                        await Task.Delay(1000);
                        Console.Clear();
                        return;
                    }
                    added.Add(finded[index2 - 1]);
                }
            }
        }
    }
    public void AddByIndex(int index)
    {

        var list = GetPrintList();
        
        if (index > 0 && index <= list.Count)
        {
            if (IsAppAdded(list[index - 1].AppId))
            {
                Console.WriteLine("이미 추가된 게임입니다.");
                return;
            }
            added.Add(list[index - 1]);
        }
    }

    #region apis
    private static async Task<List<AppData>> GetOwnedGames(string apiKey, string steamId)
    {
        string url = $"https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/?key={apiKey}&steamid={steamId}&include_appinfo=true&format=json";
        var response = await client.GetStringAsync(url);
        var json = JObject.Parse(response);

        List<AppData> appIds = new List<AppData>();
        foreach (var game in json["response"]["games"])
        {
            var data = new AppData()
            {
                AppId = game["appid"].ToString(),
                AppName = game["name"].ToString(),
                PlayMinute = int.Parse(game["playtime_forever"].ToString()) 
            };
             
            appIds.Add(data);

        }

        return appIds;
    }
    

    #endregion
}