using System.Collections.Generic;
using System.Threading;
using Runtime.ATGame;

namespace Runtime.MCP.Transport
{
    /// <summary>
    /// MCP传输通道管理器
    /// </summary>
    public class ATTransportManager : Singleton<ATTransportManager>
    {
        private Dictionary<string, ATTransportChannel> m_cannels = new Dictionary<string, ATTransportChannel>();
        
        private List<ITransportUpdate> m_transportUpdates = new List<ITransportUpdate>();

        public void Init()
        {
            
        }

        public override void ClearData()
        {
            base.ClearData();
            
            foreach (var value in m_cannels)
            {
                value.Value.ClearData();
            }
            
            m_cannels.Clear();
            m_transportUpdates.Clear();
        }
        
        public ATTransportChannel AddChannel(string channelName)
        {
            if (!m_cannels.TryGetValue(channelName, out var channel))
            {
                channel = new ATTransportChannel();
                channel.Init();
                m_cannels.Add(channelName, channel);
            }
            
            return channel;
        }

        public ATTransportChannel GetChannel(string channelName)
        {
            if (m_cannels.TryGetValue(channelName, out var channel))
            {
                return channel;
            }
        
            return null;
        }
        
        public void RemoveChannel(string channelName)
        {
            m_cannels.Remove(channelName);
        }

        public void AddTransportUpdate(ITransportUpdate update)
        {
            if (!m_transportUpdates.Contains(update))
            {
                m_transportUpdates.Add(update);
            }
        }

        public void RemoveTransportUpdate(ITransportUpdate update)
        {
            m_transportUpdates.Remove(update);
        }
        
        public void Update(CancellationToken cancellationToken)
        {
            foreach (var transportUpdate in m_transportUpdates)
            {
                if (transportUpdate != null)
                {
                    transportUpdate.Update(cancellationToken);
                }
            }
        }
    }
}