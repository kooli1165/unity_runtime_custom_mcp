using Runtime.ATGame.Prompts;
using UnityEngine;

namespace Runtime.ATGame.DataConfig
{
    public class DataConfigManager : Singleton<DataConfigManager>
    {
        public PromptsConfig PromptsConfig => m_promptsConfig;
        private PromptsConfig m_promptsConfig;
        
        public void Init()
        {
            m_promptsConfig = Resources.Load<PromptsConfig>(PathDefine.PromptsConfig);
            
            
        }
    }
    
    
}