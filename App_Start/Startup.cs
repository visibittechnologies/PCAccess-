using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(PCAccess.Startup))]
namespace PCAccess
{
    /// <summary>
    /// OWIN Startup class for PCAccess.
    /// WHAT: Configures OWIN pipeline and registers SignalR real-time messaging.
    /// REASON: Follows the project's established SignalR pattern (as seen in GlobalPda-Ireps)
    /// to provide persistent WebSocket/SignalR connections for PC Agents and Dashboards.
    /// </summary>
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Allow larger WebSocket messages for file chunks
            GlobalHost.Configuration.MaxIncomingWebSocketMessageSize = null;

            // REASON: Maps SignalR hub routes to /signalr for both Agent and Dashboard connections
            app.MapSignalR();
        }
    }
}
