using System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

public class SignalRLogger : ILogger
{
    private readonly IServiceProvider ServiceProvider;

    public SignalRLogger(IServiceProvider ServiceProvider)
    {
        this.ServiceProvider = ServiceProvider;
    }

    public IDisposable BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        var hub = ServiceProvider.GetService<IHubContext<LogHub>>();

        if (hub != null)
        {
            var msg = $"[{logLevel}] {formatter(state, exception)}";

            hub.Clients.All.SendAsync("LogEvent", msg);
        }
    }
}