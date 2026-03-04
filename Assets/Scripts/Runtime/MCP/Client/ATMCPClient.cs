using System;
using System.ClientModel;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI;
using Runtime.ATGame.DataConfig;
using UnityEngine;

namespace Runtime.MCP.Client
{
    /// <summary>
    /// C# MCP客户端 - 改进版
    /// </summary>
    public class ATMCPClient
    {
        private ATClientTransport m_clientTransport;
        
        private IMcpClient m_mcpClient;

        private Action<string> m_onPrintLog;
        
        public List<McpClientTool> AvailableTools => m_availableTools;
        
        //private ATClientTransport m_clientTransport;
        //private IMcpClient m_mcpClient;
        private IChatClient m_chatClient;
        private List<ChatMessage> m_messages;
        private List<McpClientTool> m_availableTools;
        private CancellationTokenSource m_cancellationTokenSource;
        
        // 事件回调
        //private Action<string> m_onPrintLog;
        private Action<ChatMessage> m_onMessageReceived;
        private Action<ChatMessage> m_onMessageSent;
        private Action<Exception> m_onError;
        private Action m_onInitialized;
        private Action m_onDisconnected;

        private Action m_onMsgReceiveStart;
        private Action<string> m_onMsgReceiving;
        private Action<string> m_onMsgReceiveEnd;

        private static readonly AIAPIGroup DeepSeek = new AIAPIGroup()
        {
            url = "your-api",
            key = "your-api-key",
            model = "your-api-model",
        };

        // 状态管理
        public bool IsInitialized { get; private set; } = false;
        public bool IsConnected { get; private set; } = false;
        public bool IsProcessing { get; private set; } = false;

        public ATMCPClient(Action<string> onPrintLog = null, 
                          Action<ChatMessage> onMessageReceived = null,
                          Action<ChatMessage> onMessageSent = null,
                          Action<Exception> onError = null,
                          Action onInitialized = null,
                          Action onDisconnected = null,
                          Action onMsgReceiveStart = null,
                          Action<string> onMsgReceiving = null,
                          Action<string> onMsgReceiveEnd = null)
        {
            m_onPrintLog = onPrintLog;
            m_onMessageReceived = onMessageReceived;
            m_onMessageSent = onMessageSent;
            m_onError = onError;
            m_onInitialized = onInitialized;
            m_onDisconnected = onDisconnected;
            m_onMsgReceiveStart = onMsgReceiveStart;
            m_onMsgReceiving = onMsgReceiving;
            m_onMsgReceiveEnd = onMsgReceiveEnd;
            
            m_messages = new List<ChatMessage>();
            m_cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// 初始化MCP客户端
        /// </summary>
        public async Task<bool> InitializeAsync()
        {
            try
            {
                m_onPrintLog?.Invoke("正在初始化MCP客户端...");

                var llm = DeepSeek;
                
                // 创建基于OpenAI协议的聊天客户端
                var clientAIOptions = new OpenAIClientOptions();
                clientAIOptions.Endpoint = new Uri(llm.url);
                m_chatClient = new OpenAI.OpenAIClient(new ApiKeyCredential(llm.key), clientAIOptions)
                    .GetChatClient(llm.model)
                    .AsIChatClient()
                    .AsBuilder()
                    .UseFunctionInvocation()
                    .Build();

                // 创建MCP客户端
                m_clientTransport = new ATClientTransport("AT");
                m_mcpClient = await McpClientFactory.CreateAsync(m_clientTransport, 
                    cancellationToken: m_cancellationTokenSource.Token);

                // 获取可用工具
                m_availableTools = (List<McpClientTool>)await m_mcpClient.ListToolsAsync();
                
                m_onPrintLog?.Invoke($"获取到 {m_availableTools.Count} 个可用工具:");
                foreach (McpClientTool tool in m_availableTools)
                {
                    m_onPrintLog?.Invoke($"- {tool}");
                }

                // 初始化消息历史
                InitializeMessages();
                
                IsInitialized = true;
                IsConnected = true;
                
                m_onPrintLog?.Invoke("MCP客户端初始化成功");
                m_onInitialized?.Invoke();
                
                return true;
            }
            catch (Exception ex)
            {
                m_onError?.Invoke(ex);
                m_onPrintLog?.Invoke($"MCP客户端初始化失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 初始化消息历史
        /// </summary>
        private void InitializeMessages()
        {
            m_messages.Clear();
            
            // 添加系统初始提示词
            var initPrompt = DataConfigManager.Instance.PromptsConfig?.initPrompt;
            if (initPrompt != null)
            {
                var systemMessage = new ChatMessage(initPrompt.GetChatRole(), initPrompt.content);
                m_messages.Add(systemMessage);
                m_onPrintLog?.Invoke($"添加系统提示词: {initPrompt.content}");
            }
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        public async Task<bool> SendMessageAsync(string message, ChatRole role, ChatToolMode toolMode = null)
        {
            if (!IsConnected || !IsInitialized || IsProcessing)
            {
                m_onPrintLog?.Invoke($"客户端未就绪或正在处理中! IsConnected:{IsConnected}, IsInitialized:{IsInitialized}, IsProcessing:{IsProcessing}");
                return false;
            }

            try
            {
                IsProcessing = true;
                
                // 添加用户消息
                var userMessage = new ChatMessage(role, message);
                m_messages.Add(userMessage);
                m_onMessageSent?.Invoke(userMessage);
                m_onPrintLog?.Invoke($"{role}: {message}");

                // 获取AI响应
                await GetAIResponseAsync(toolMode);
                
                return true;
            }
            catch (Exception ex)
            {
                m_onError?.Invoke(ex);
                m_onPrintLog?.Invoke($"发送消息失败: {ex.Message}");
                return false;
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// 获取AI响应
        /// </summary>
        private async Task GetAIResponseAsync(ChatToolMode toolMode = null)
        {
            try
            {
                List<ChatResponseUpdate> updates = new List<ChatResponseUpdate>();
                string fullResponse = "";
                
                m_onMsgReceiveStart?.Invoke();
                
                await foreach (ChatResponseUpdate update in m_chatClient
                                   .GetStreamingResponseAsync(m_messages, 
                                       new()
                                       {
                                           Tools = new List<AITool>(m_availableTools),
                                           ToolMode = toolMode,
                                       }, 
                                       cancellationToken: m_cancellationTokenSource.Token))
                {
                    if (m_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        return;
                    }
                    
                    // 处理流式响应
                    if (update.Text != null)
                    {
                        fullResponse += update.Text;
                        m_onPrintLog?.Invoke(update.Text);
                        m_onMsgReceiving?.Invoke(update.Text);
                    }
                    
                    updates.Add(update);
                }
                
                var lastMessageId = m_messages.Count;

                // 添加完整响应到消息历史
                m_messages.AddMessages(updates);
                
                // 获取最后的AI消息
                // var lastMessage = GetLastAIMessage();
                // if (lastMessage != null)
                // {
                //     m_onMessageReceived?.Invoke(lastMessage);
                // }

                for (int i = lastMessageId; i < m_messages.Count; i++)
                {
                    m_onMessageReceived?.Invoke(m_messages[i]);
                }
                
                m_onMsgReceiveEnd?.Invoke(fullResponse);
            }
            catch (Exception ex)
            {
                m_onError?.Invoke(ex);
                m_onPrintLog?.Invoke($"获取AI响应失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取最后的AI消息
        /// </summary>
        private ChatMessage GetLastAIMessage()
        {
            for (int i = m_messages.Count - 1; i >= 0; i--)
            {
                if (m_messages[i].Role == ChatRole.Assistant)
                {
                    return m_messages[i];
                }
            }
            return null;
        }

        /// <summary>
        /// 发送系统消息
        /// </summary>
        public async Task<bool> SendSystemMessageAsync(string message)
        {
            return await SendMessageAsync(message, ChatRole.System);
        }

        /// <summary>
        /// 清空对话历史
        /// </summary>
        public void ClearHistory()
        {
            InitializeMessages();
            m_onPrintLog?.Invoke("对话历史已清空");
        }

        /// <summary>
        /// 获取对话历史
        /// </summary>
        public List<ChatMessage> GetMessageHistory()
        {
            return new List<ChatMessage>(m_messages);
        }

        /// <summary>
        /// 获取可用工具列表
        /// </summary>
        public List<McpClientTool> GetAvailableTools()
        {
            return new List<McpClientTool>(m_availableTools ?? new List<McpClientTool>());
        }

        /// <summary>
        /// 设置API Key
        /// </summary>
        public void SetApiKey(string apiKey)
        {
            if (IsInitialized)
            {
                m_onPrintLog?.Invoke("警告: 客户端已初始化，无法更改API Key");
                return;
            }
            
            // 这里需要重新创建客户端来使用新的API Key
            m_onPrintLog?.Invoke("API Key已更新，请重新初始化客户端");
        }

        /// <summary>
        /// 设置模型端点
        /// </summary>
        public void SetEndpoint(string endpoint)
        {
            if (IsInitialized)
            {
                m_onPrintLog?.Invoke("警告: 客户端已初始化，无法更改端点");
                return;
            }
            
            m_onPrintLog?.Invoke($"端点已设置为: {endpoint}");
        }

        /// <summary>
        /// 停止当前处理
        /// </summary>
        public void StopProcessing()
        {
            if (IsProcessing)
            {
                m_cancellationTokenSource.Cancel();
                m_cancellationTokenSource = new CancellationTokenSource();
                IsProcessing = false;
                m_onPrintLog?.Invoke("已停止当前处理");
            }
        }

        /// <summary>
        /// 重连
        /// </summary>
        public async Task<bool> ReconnectAsync()
        {
            m_onPrintLog?.Invoke("正在重连...");
            
            await DisposeAsync();
            
            IsInitialized = false;
            IsConnected = false;
            IsProcessing = false;
            
            return await InitializeAsync();
        }

        /// <summary>
        /// 获取连接状态信息
        /// </summary>
        public string GetStatusInfo()
        {
            return $"初始化: {IsInitialized}, 已连接: {IsConnected}, 处理中: {IsProcessing}, " +
                   $"消息数: {m_messages?.Count ?? 0}, 工具数: {m_availableTools?.Count ?? 0}";
        }

        /// <summary>
        /// 异步运行
        /// </summary>
        public async Task Run(CancellationToken cancellationToken)
        {
            if (!await InitializeAsync())
            {
                return;
            }

            // 发送初始消息
            await SendMessageAsync("开始游戏", ChatRole.User);

            // 保持运行状态，等待外部调用 SendMessageAsync
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                m_onPrintLog?.Invoke("运行已取消");
            }
            finally
            {
                await DisposeAsync();
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public async Task DisposeAsync()
        {
            try
            {
                IsConnected = false;
                
                if (m_mcpClient != null)
                {
                    await m_mcpClient.DisposeAsync();
                    m_mcpClient = null;
                }

                m_clientTransport?.Dispose();
                m_clientTransport = null;
                
                m_cancellationTokenSource?.Cancel();
                m_cancellationTokenSource?.Dispose();
                
                m_onPrintLog?.Invoke("MCP客户端已断开连接");
                m_onDisconnected?.Invoke();
            }
            catch (Exception ex)
            {
                m_onError?.Invoke(ex);
                m_onPrintLog?.Invoke($"释放资源时出错: {ex.Message}; {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 同步版本的SendMessage (原方法的实现)
        /// </summary>
        public void SendMessage(ChatRole role, string message)
        {
            // 异步调用但不等待，适合Unity主线程调用
            _ = Task.Run(async () => await SendMessageAsync(message, role));
        }
    }

    public class AIAPIGroup
    {
        public string url;
        public string key;
        public string model;
    }
}