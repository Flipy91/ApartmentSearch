using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using FrankfurtWohnungsSuchApp.Contracts;
using Telegram.Bot;

namespace FrankfurtWohnungsSuchApp.Implementation;

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
        await _tableClient.CreateIfNotExistsAsync();

        var entity = new TableEntity(provider, id)
        {
            { "data", data.ToString() }
        };

        try
        {
            var _ = await _tableClient.GetEntityAsync<TableEntity>(provider, id);
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