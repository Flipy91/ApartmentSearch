using System;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Telegram.Bot;

namespace FrankfurtWohnungsSuchApp
{
    public class TelegramNotifator
    {
        private readonly TableClient _tableClient;
        private readonly TelegramBotClient _telegramBotClient;
        private readonly long _chatId;

        public TelegramNotifator(TableClient tableClient, TelegramBotClient telegramBotClient, long chatId)
        {
            _tableClient = tableClient;
            _telegramBotClient = telegramBotClient;
            _chatId = chatId;
        }

        public async Task CheckAndSendAsync(string provider, string id, IApartmentData data)
        {
            _tableClient.CreateIfNotExists();

            var entity = new TableEntity(provider, id)
            {
                { "data", data.ToString() }
            };

            try
            {
                var result = await _tableClient.GetEntityAsync<TableEntity>(provider, id);
            }

            catch (RequestFailedException ex)
            {
                if(ex.ErrorCode is "ResourceNotFound")
                {
                    var text = $"New flat from {provider}! {data.Size}m² {data.Prize}€ {data.Name} {data.Url}";

                    await Task.WhenAll(_tableClient.AddEntityAsync(entity), _telegramBotClient.SendTextMessageAsync(_chatId, text));
                }
                else throw ex;
            }
        }
    }
}
