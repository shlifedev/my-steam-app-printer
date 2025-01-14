 

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using steam_appid_gen;

class Program
{ 
    static async Task Main(string[] args)
    { 
        await new MyApp().Initialize();
    }

}