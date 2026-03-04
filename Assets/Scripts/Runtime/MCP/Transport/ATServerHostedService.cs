using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;

namespace Runtime.MCP.Transport
{
    /// <summary>
    /// C# MCP服务器后台服务启动器
    /// </summary>
    public class ATServerHostedService : BackgroundService
    {
        private IMcpServer session;
        private IHostApplicationLifetime lifetime;
        
        public ATServerHostedService(IMcpServer session, IHostApplicationLifetime lifetime = null)
        {
            this.session = session;
            this.lifetime = lifetime;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await session.RunAsync(stoppingToken).ConfigureAwait(false);
            }
            finally
            {
                lifetime?.StopApplication();
            }
        }
    }
}