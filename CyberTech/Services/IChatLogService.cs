using CyberTech.Models;

namespace CyberTech.Services
{
    public interface IChatLogService
    {
        /// <summary>
        /// Logs a chat message to a file in the Logs folder
        /// </summary>
        /// <param name="userInput">The message from the user</param>
        /// <param name="botResponse">The response from the bot</param>
        /// <param name="userEmail">Optional user email for identification</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task LogChatAsync(string userInput, string botResponse, string userEmail = null);

        /// <summary>
        /// Gets chat history for a specific user or session
        /// </summary>
        /// <param name="userIdentifier">User email or session ID</param>
        /// <param name="maxEntries">Maximum number of entries to retrieve</param>
        /// <returns>List of chat log entries</returns>
        Task<List<ChatLogEntry>> GetChatHistoryAsync(string userIdentifier, int maxEntries = 50);
    }

    public class ChatLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string UserInput { get; set; }
        public string BotResponse { get; set; }
        public string UserIdentifier { get; set; }
    }
}