using System.Drawing;
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
        await PreloadAdded();
        await VerifyConfigAndLoad(); 
    }
    
    async Task SaveAdded()
    {
        await System.IO.File.WriteAllTextAsync("added.json", Newtonsoft.Json.JsonConvert.SerializeObject(added));
    }
    async Task PreloadAdded()
    {
        if (System.IO.File.Exists("added.json"))
        {
            added = Newtonsoft.Json.JsonConvert.DeserializeObject<List<AppData>>(await System.IO.File.ReadAllTextAsync("added.json"));
        }
    }

    async Task AppLogic()
    {
        while (true)
        {
            Console.Clear();
            PrintMyGames();
            PrintAddedGamesWithName();
            await WaitInput();
        }
    }
    

    async Task WaitInput()
    {
        
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"추가할 게임의 이름 또는 인덱스를 입력하세요. (현재 {this.added.Count}/{MAX_COUNT}개)");
        Console.WriteLine($"그 외 추가 명령어 :");
        Console.WriteLine($"-r [appId] : 추가된 게임을 제거합니다."); 
        Console.WriteLine($"-c : 추가된 게임을 모두 제거합니다.");
        Console.WriteLine($"-p : 추가된 게임을 ',' 로 구분하여 출력합니다.");
        string input = Console.ReadLine();
        if(input.StartsWith("-r"))
        {
            string appId = input.Split(" ")[1];
            await RemoveByAppId(appId);
            await SaveAdded();
        }
        else if(input.StartsWith("-c"))
        {
            added.Clear();
            await SaveAdded();
        }
        else if(input.StartsWith("-p"))
        {
            PrintAddedGames();
            Console.ReadLine();
        }
        else if (int.TryParse(input, out int index))
        {
            AddByIndex(index);
            await SaveAdded();
        }
        else
        {
            await AddByName(input);
            await SaveAdded();
        }
    }
    
    List<AppData> GetPrintList()
    {
        return loaded.FindAll(x => !added.Select(y => y.AppId).Contains(x.AppId)).OrderBy(x => x.PlayMinute).ToList();
    }
    void PrintMyGames()
    { 
        var print = GetPrintList();
        int cnt = 0;
        for (int i = 0; i < print.Count; i++)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"{i + 1}."); 
            Console.ForegroundColor = ConsoleColor.Yellow;
            int findMaxPadding =  print.Max(x => x.AppName.Length);
            string appInfo = ($"{print[i].AppName} ({print[i].PlayHour}H)").PadRight(findMaxPadding);

            Console.Write(appInfo);
            cnt++;
            if (cnt!= 0 && cnt % 3 == 0)
            {
                Console.WriteLine();
            }
        } 
    }
    void PrintAddedGamesWithName()
    { 
        Console.WriteLine("\n");
        var sort = added.OrderBy(x => x.AppName).ToList();
        for (var index = 0; index < sort.Count; index++)
        { 
            var app = sort[index];
            
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"{app.AppName}");
            
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"({app.AppId})".PadRight(5));
            Console.Write("\t");
            if (index != 0 && index % 3 == 0)
            {
                Console.WriteLine();
            }
        }
    }
    void PrintAddedGames()
    {
        Console.WriteLine("\n");
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
    
    
    public AppData FindByAppId(string appId)
    {
        return loaded.Find(x => x.AppId == appId);
    }
    
     
    public async Task RemoveByAppId(string appId)
    {
         var find = FindByAppId(appId);
            if (find != null)
            {
                if(IsAppAdded(appId))
                    added.Remove(find);
            }
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
            if (finded.Count == 1)
            {
                if(!IsAppAdded(finded[0].AppId))
                    added.Add(finded[0]);
                else
                {
                    Console.WriteLine("이미 추가된 게임입니다.");
                    await Task.Delay(1000);
                    Console.Clear();
                    return;
                }
            }
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
        string url = $"https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/?key={apiKey}&steamid={steamId}&include_appinfo=true&format=json&include_played_free_games=1&skip_unvetted_apps=false";
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