using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using Runtime.ATGame;
using Runtime.MCP.Transport;
using UnityEngine;

namespace Runtime.MCP.Client
{
    /// <summary>
    /// MCP客户端传输会话，通过通道与服务器通信
    /// </summary>
    public class ATClientSessionTransport : TransportBase, ITransportUpdate
    {
        public string ChannelName { get => m_channelName; }
        private string m_channelName;
        
        private ATTransportChannel m_channel;
        
        public ATClientSessionTransport(string name, ILoggerFactory loggerFactory) : base(name, loggerFactory)
        {
            m_channelName = name;
            m_channel = ATTransportManager.Instance.GetChannel(m_channelName);
            ATTransportManager.Instance.AddTransportUpdate(this);
            
            Debug.Log("[client][SetConnected]");
            SetConnected();
        }

        /// <summary>
        /// 将MCP客户端消息发送到通道
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task SendMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken = new CancellationToken())
        {
            var json = JsonSerializer.Serialize(message, McpJsonUtilities.DefaultOptions.GetTypeInfo(typeof(JsonRpcMessage)));
            if (!string.IsNullOrEmpty(json))
            {
                Debug.Log("[client][send]" + json);
                m_channel?.AddServerInput(json);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 断开通信连接
        /// </summary>
        /// <returns></returns>
        public override ValueTask DisposeAsync()
        {
            ATTransportManager.Instance.RemoveTransportUpdate(this);
            m_channel = null;
            
            Debug.Log("[client][SetDisconnected]");
            SetDisconnected();
            
            return new ValueTask();
        }
        
        /// <summary>
        /// 从服务器输出读取数据，写入到MCP客户端
        /// </summary>
        /// <param name="cancellationToken"></param>
        private void ReadMessages(CancellationToken cancellationToken)
        {
            var line = m_channel.GetServerOutput();
            if (!string.IsNullOrEmpty(line))
            {
                Debug.Log("[client][Read]" + line);
                var message = (JsonRpcMessage)JsonSerializer.Deserialize(line, McpJsonUtilities.DefaultOptions.GetTypeInfo(typeof(JsonRpcMessage)));
                if (message != null)
                {
                    Debug.Log("[client][write message]" + message);
                    WriteMessageAsync(message, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }
        }
        
        public void Update(CancellationToken cancellationToken)
        {
            ReadMessages(cancellationToken);
        }
    }
}