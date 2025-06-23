using OpenAI.ObjectModels.RequestModels;
using System.Collections.Generic;

namespace AITest2.Models
{
    public class SessionModel
    {
        public string title { get; set; }
        public List<ChatMessage> listMessage { get; set; }
    }
}
