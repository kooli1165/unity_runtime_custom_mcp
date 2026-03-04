using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Runtime.MCP.Server;
using Runtime.MCP.Transport;
using UnityEngine;

namespace Runtime.MCP.Client
{
    /// <summary>
    /// MCP客户端传输启动器
    /// </summary>
    public class ATClientTransport : IClientTransport
    {
        public string Name { get; private set; }
        
        private ATClientSessionTransport m_sessionTransport;
        
        public ATClientTransport(string name)
        {
            Name = name;
        }

        /// <summary>
        /// 启动MCP服务器，返回传输会话
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<ITransport> ConnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // 添加传输通道，用于交换消息
                ATTransportManager.Instance.AddChannel(Name);

                // 启动服务器
                var server = new ATMCPServer(cancellationToken);
                Task.Run(server.Run, cancellationToken);
                Debug.Log("Server started");
                
                // 创建会话
                m_sessionTransport = new ATClientSessionTransport(Name, null);
                return Task.FromResult<ITransport>(m_sessionTransport);
            }
            catch (Exception e)
            {
                Dispose();
                Debug.LogError(e);
                throw;
            }
        }

        public void Dispose()
        {
            ATTransportManager.Instance.RemoveChannel(Name);
        }

    }
}