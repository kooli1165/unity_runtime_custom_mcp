using System;
using Microsoft.Extensions.AI;
using UnityEngine;

namespace Runtime.ATGame.Prompts
{
    [CreateAssetMenu(menuName = "ATGame/PromptsConfig")]
    public class PromptsConfig : ScriptableObject
    {
        public ChatPrompt initPrompt = new ChatPrompt();
        
        
    }

    [Serializable]
    public class ChatPrompt
    {
        public ATChatRole role;
        
        [TextArea(3, 50)]
        public string content;

        public ChatRole GetChatRole()
        {
            switch (role)
            {
                case ATChatRole.System:
                    return ChatRole.System;
                case ATChatRole.Assistant:
                    return ChatRole.Assistant;
                case ATChatRole.User:
                    return ChatRole.User;
                case ATChatRole.Tool:
                    return ChatRole.Tool;
            }
            
            return ChatRole.System;
        }
    }

    [Serializable]
    public enum ATChatRole
    {
        System = 0,
        Assistant = 1,
        User = 2,
        Tool = 3,
    }
}