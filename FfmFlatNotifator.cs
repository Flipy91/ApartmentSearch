using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace FrankfurtWohnungsSuchApp
{
    public class FfmFlatNotifator
    {
        [FunctionName("FfmFlatNotifatorFunction")]
        public async Task Run([TimerTrigger("*/5 * * * * *")]TimerInfo myTimer, ILogger log, ExecutionContext context)
        {

            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = config.GetConnectionString("ApartmentStore");
            var telegramConfig = config.GetSection("TelegramBotClient").Get<TelegramConfig>();

            var telegramBotClient = new TelegramBotClient(telegramConfig.BotKey);

            try
            {
                var httpClient = new HttpClient();
                var vonoviaCrawler = new VonoviaCrawler(httpClient);
                var gwhCrawler = new GwhCrawler(httpClient);
                var nasshauischeCrawler = new NassauischHeimCrawler(httpClient,log);

                var tableClient = new TableClient(connectionString, "FlatData");
                var notificator = new TelegramNotifator(tableClient, telegramBotClient,telegramConfig.ChatId);

                var vonoviaTask = new ScraperConfiguration(vonoviaCrawler, "Vonovia", x => x.Name + x.Prize + x.Id);
                var gwhTask = new ScraperConfiguration(gwhCrawler, "Gwh", x => x.Name + x.Id);
                var nhTask = new ScraperConfiguration(nasshauischeCrawler, "Nassauische", x => x.Name + x.Id);

                await RunScrapers(notificator, vonoviaTask, gwhTask, nhTask);
            }

            catch (Exception ex)
            {
                await telegramBotClient.SendTextMessageAsync(telegramConfig.ErrorChatId, ex.Message + "\n" + ex.StackTrace);
            }
        }

        private static async Task RunScrapers(TelegramNotifator notificator, params ScraperConfiguration[] scraperConfigurations)
        {
            var scraperRunConfiguration = scraperConfigurations.Select(CreateScraperResult);
            var results = await Task.WhenAll(scraperRunConfiguration);

            var notificationsTasks = results.SelectMany(x => NotifyAsync(notificator, x));
            await Task.WhenAll(notificationsTasks); 
        }

        private static IEnumerable<Task> NotifyAsync(TelegramNotifator notificator, ScraperRunResult result) => result
            .Result
            .Select(x => notificator.CheckAndSendAsync(result.Provider, result.Id(x), x))
            .ToList();

        private static async Task<ScraperRunResult> CreateScraperResult(ScraperConfiguration x) => new ScraperRunResult(await x.Crawler.GetFlats(), x.Provider, x.Id);

        record ScraperConfiguration(IApartmentCrawler Crawler, string Provider, Func<IApartmentData,string> Id);
        record ScraperRunResult(List<IApartmentData> Result, string Provider, Func<IApartmentData, string> Id);
        record TelegramConfig()
        {
            public string BotKey { get; set; }
            public long ChatId { get; set; }
            public long ErrorChatId { get; set; }
        };
    }
}
