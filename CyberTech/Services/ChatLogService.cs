using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CyberTech.Services
{
    public class ChatLogService : IChatLogService
    {
        private readonly ILogger<ChatLogService> _logger;
        private readonly string _logDirectory;

        public ChatLogService(ILogger<ChatLogService> logger)
        {
            _logger = logger;
            _logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "ChatLogs");

            // Create the directory if it doesn't exist
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public async Task LogChatAsync(string userInput, string botResponse, string userEmail = null)
        {
            try
            {
                var logEntry = new ChatLogEntry
                {
                    Timestamp = DateTime.Now,
                    UserInput = userInput,
                    BotResponse = botResponse,
                    UserIdentifier = !string.IsNullOrEmpty(userEmail) ? userEmail : "anonymous"
                };

                // Create a filename based on date and user identifier
                string fileName = GetLogFileName(logEntry.UserIdentifier);
                string filePath = Path.Combine(_logDirectory, fileName);

                // Read existing logs if the file exists
                List<ChatLogEntry> logs = new List<ChatLogEntry>();
                if (File.Exists(filePath))
                {
                    string existingJson = await File.ReadAllTextAsync(filePath);
                    if (!string.IsNullOrEmpty(existingJson))
                    {
                        logs = JsonSerializer.Deserialize<List<ChatLogEntry>>(existingJson) ?? new List<ChatLogEntry>();
                    }
                }

                // Add new log entry
                logs.Add(logEntry);

                // Write back to file
                string jsonContent = JsonSerializer.Serialize(logs, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                await File.WriteAllTextAsync(filePath, jsonContent);

                _logger.LogInformation("Chat log saved for user {UserIdentifier}", logEntry.UserIdentifier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving chat log");
            }
        }

        public async Task<List<ChatLogEntry>> GetChatHistoryAsync(string userIdentifier, int maxEntries = 50)
        {
            try
            {
                string fileName = GetLogFileName(userIdentifier);
                string filePath = Path.Combine(_logDirectory, fileName);

                if (!File.Exists(filePath))
                {
                    _logger.LogInformation("No chat history found for user {UserIdentifier}", userIdentifier);
                    return new List<ChatLogEntry>();
                }

                string jsonContent = await File.ReadAllTextAsync(filePath);
                var logs = JsonSerializer.Deserialize<List<ChatLogEntry>>(jsonContent) ?? new List<ChatLogEntry>();

                // Return most recent logs up to maxEntries
                return logs.OrderByDescending(l => l.Timestamp).Take(maxEntries).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving chat history for user {UserIdentifier}", userIdentifier);
                return new List<ChatLogEntry>();
            }
        }

        private string GetLogFileName(string userIdentifier)
        {
            // Sanitize the user identifier to make it safe for a filename
            string sanitizedIdentifier = string.Join("_", userIdentifier.Split(Path.GetInvalidFileNameChars()));

            // Use the current date in the filename
            string dateStr = DateTime.Now.ToString("yyyy-MM-dd");
            return $"chat_log_{sanitizedIdentifier}_{dateStr}.json";
        }
    }
}