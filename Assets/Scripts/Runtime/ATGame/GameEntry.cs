using System;
using System.Threading;
using Runtime.ATGame.DataConfig;
using Runtime.MCP.Client;
using Runtime.MCP.Server;
using Runtime.MCP.Transport;
using TMPro;
using UnityEngine;

namespace Runtime.ATGame
{
    public class GameEntry : MonoBehaviour
    {
        public TMP_Text debugUIText;
        
        private CancellationTokenSource m_source = new CancellationTokenSource();
        
        private async void Start()
        {
            DataConfigManager.Instance.Init();
            
            ATTransportManager.Instance.Init();

            try
            {
                var client = new ATMCPClient(Print);
                await client.Run(m_source.Token);
            }
            catch (Exception e)
            {
                m_source.Cancel();
                Debug.LogError(e);
                throw;
            }
        }

        private void Print(string msg)
        {
            if (debugUIText)
            {
                var text = debugUIText.text;
                text += msg;
                debugUIText.text = text;
            }
        }

        private void OnDestroy()
        {
            m_source.Cancel();
            
            ATTransportManager.Instance.ClearData();
        }

        private void Update()
        {
            ATTransportManager.Instance.Update(m_source.Token);
        }
    }
}