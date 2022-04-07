using System;
using Newtonsoft.Json;

namespace Tolltech.KonturPaymentsLib
{
    public class ChatDto
    {
        //"name": "Платежи.Moira.Alerts",
        //"type": "private_supergroup",
        //"id": 1445762816,
        //"messages": [
        //{
        //    "id": 18262,
        //    "type": "message",
        //    "date": "2022-04-04T03:23:25",
        //    "from": "Kontur Moira",
        //    "from_id": "user119557778",
        //    "text": [
        //    "⭕ERROR Проблема доступности аггрегаторов на Production [BillyPayments] (1)\n\n02:23: Проблема доступности аггрегаторов на Production = — (ERROR to ERROR). This metric has been in bad state for more than 24 hours - please, fix.\n\n",
        //    {
        //        "type": "link",
        //        "text": "https://moira.skbkontur.ru/trigger/ebbf47ad-76e8-44b2-9889-2bf706109f84"
        //    },
        //    ""
        //        ]
        //}

        [JsonProperty("messages")] public MessageDto[] Messages { get; set; }
    }

    public class MessageDto
    {
        [JsonProperty("id")] public int Id { get; set; }

        [JsonProperty("date")] public DateTime Date { get; set; }

        [JsonProperty("type")] public string Type { get; set; }

        [JsonProperty("from")] public string From { get; set; }

        [JsonProperty("text")] public object Text { get; set; }

    }
}