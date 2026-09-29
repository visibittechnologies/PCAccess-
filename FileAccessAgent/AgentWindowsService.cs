using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace FileAccessAgent
{
    /// <summary>
    /// Native Windows Service implementation for .NET Framework 4.8.
    /// WHAT: Inherits from System.ServiceProcess.ServiceBase to run natively under Windows Service Control Manager.
    /// REASON: Zero dependency on .NET Core or external hosting libraries; natively supported on all Windows systems.
    /// </summary>
    public class AgentWindowsService : ServiceBase
    {
        private CancellationTokenSource? _cts;

        public AgentWindowsService()
        {
            ServiceName = ServiceManager.ServiceName;
            CanStop = true;
            CanShutdown = true;
            AutoLog = true;
        }

        protected override void OnStart(string[] args)
        {
            AgentLogger.Info("[SERVICE] PCAccessAgent Windows Service starting...");
            _cts = new CancellationTokenSource();
            Task.Run(async () =>
            {
                await Program.StartAgentAsync(_cts.Token);
            });
        }

        protected override void OnStop()
        {
            AgentLogger.Info("[SERVICE] PCAccessAgent received stop signal.");
            try
            {
                _cts?.Cancel();
                Program.StopAgentAsync().GetAwaiter().GetResult();
            }
            catch { }
            finally
            {
                _cts?.Dispose();
                _cts = null;
            }
        }

        protected override void OnShutdown()
        {
            AgentLogger.Info("[SERVICE] PCAccessAgent received system shutdown signal.");
            OnStop();
        }
    }
}
