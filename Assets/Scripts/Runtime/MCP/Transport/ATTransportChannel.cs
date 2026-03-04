using System.Collections.Generic;
using UnityEngine;

namespace Runtime.MCP.Transport
{
    /// <summary>
    /// MCP自定义通信的传输通道，用于数据交换
    /// </summary>
    public class ATTransportChannel
    {
        /// <summary>
        /// MCP服务端的输出
        /// </summary>
        private Queue<string> m_serverOutput = new Queue<string>();

        /// <summary>
        /// MCP服务端的输入
        /// </summary>
        private Queue<string> m_serverInput = new Queue<string>();

        
        public void Init()
        {
            
        }
        
        public void ClearData()
        {
            m_serverInput.Clear();
            m_serverInput.Clear();
        }

        public void AddServerOutput(string msg)
        {
            Debug.Log("[ATTransportChannel] AddServerOutput: " + msg);
            m_serverOutput.Enqueue(msg);
        }

        public string GetServerOutput()
        {
            var success = m_serverOutput.TryDequeue(out var result);
            if (success)
            {
                Debug.Log("[ATTransportChannel] GetServerOutput: " + result);
            }

            return success ? result : string.Empty;
        }
        
        public void AddServerInput(string msg)
        {
            Debug.Log("[ATTransportChannel] AddServerInput: " + msg);
            m_serverInput.Enqueue(msg);
        }

        public string GetServerInput()
        {
            var success = m_serverInput.TryDequeue(out var result);
            if (success)
            {
                Debug.Log("[ATTransportChannel] GetServerInput: " + result);
            }
            
            return success ? result : string.Empty;
        }

        
    }
}