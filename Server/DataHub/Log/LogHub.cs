using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

public class LogHub : Hub
{
    public Task SendMessage(string user, string message)
    {
        return Clients.All.SendAsync("ReceiveMessage", user, message);
    }

}