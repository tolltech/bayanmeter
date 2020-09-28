using System;
using Newtonsoft.Json;

namespace Migration
{
    public class ChatDto
    {
        //{
        //    "name": "ВЕСЕЛУХА ДЛЯ СВОИХ",
        //    "type": "private_supergroup",
        //    "id": 9851555733,
        //    "messages": [
            //{
            //    "id": 11546,
            //    "type": "message",
            //    "date": "2020-09-28T16:31:39",
            //    "from": "Катя Зеленина",
            //    "from_id": 167301535,
            //    "photo": "photos/photo_5920@28-09-2020_16-31-39.jpg",
            //    "width": 588,
            //    "height": 590,
            //    "text": ""
            //}

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("messages")]
            public MessageDto[] Messages { get; set; }
    }

    public class MessageDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("date")] 
        public DateTime Date { get; set; }

        [JsonProperty("from_id")]
        public int FromUserId { get; set; }

        [JsonProperty("photo")]
        public string PhotoPath { get; set; }
        
        [JsonProperty("type")]
        public string Type { get; set; }
    }
}