using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace SEN_T_PAZAR.Hubs;

public class ChatHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Send a message to a specific conversation.
    /// For now, this is a simple broadcast to the conversation participants.
    /// </summary>
    public async Task SendMessageToConversation(string conversationId, string message, string senderName)
    {
        // In a full implementation, we would validate the user has access to this conversation
        // and persist the message to the database.

        await Clients.Group($"conversation-{conversationId}").SendAsync("ReceiveMessage", new
        {
            SenderName = senderName,
            Message = message,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task JoinConversation(string conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation-{conversationId}");
    }

    public async Task LeaveConversation(string conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation-{conversationId}");
    }
}
