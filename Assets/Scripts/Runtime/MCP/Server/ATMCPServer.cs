using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Runtime.MCP.Transport;
using Unity.VisualScripting;

namespace Runtime.MCP.Server
{
    /// <summary>
    /// C# MCP服务器
    /// </summary>
    public class ATMCPServer
    {
        private CancellationToken m_cancellationToken;
        
        public ATMCPServer(CancellationToken cancellationToken)
        {
            m_cancellationToken = cancellationToken;
        }
        
        public async Task Run()
        {
            // 创建一个通用主机构建器，用于依赖注入、日志记录和配置
            var builder = Host.CreateApplicationBuilder(settings: null);

            // 清除所有默认的日志记录提供程序，不然会报错
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole(consoleLogOptions =>
            {
                consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
            });

            // 注册MCP服务器
            var mcpServerBuilder = builder.Services.AddMcpServer();
            
            // 添加自定义的MCP后台服务
            builder.Services.AddHostedService<ATServerHostedService>();
            builder.Services.TryAddSingleton(services =>
            {
                ITransport serverTransport = services.GetRequiredService<ITransport>();
                IOptions<McpServerOptions> options = services.GetRequiredService<IOptions<McpServerOptions>>();
                ILoggerFactory? loggerFactory = services.GetService<ILoggerFactory>();
                return McpServerFactory.Create(serverTransport, options.Value, loggerFactory, services);
            });
            
            // 添加自定义的MCP传输协议
            mcpServerBuilder.Services.AddSingleton<ITransport>(sp =>
            {
                var serverOptions = sp.GetRequiredService<IOptions<McpServerOptions>>();
                var loggerFactory = sp.GetService<ILoggerFactory>();
                return new ATServerTransport("AT", loggerFactory);
            });
            
            // 添加自定义的MCP工具，这里可以替换成任意类
            mcpServerBuilder.WithTools<ATEchoTool>();

            // 构建并运行主机，启动MCP服务器
            await builder.Build().RunAsync(m_cancellationToken);
        }
    }
    
    [McpServerToolType]
    public sealed class ATEchoTool
    {
        [McpServerTool, Description("Echoes the input back to the client.")]
        public static string Echo(string message)
        {
            return "hello " + message;
        }
    }
}