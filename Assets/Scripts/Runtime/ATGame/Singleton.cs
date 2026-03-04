namespace Runtime.ATGame
{
    public abstract class Singleton<T> where T : class, new()
    {
        private static object s_lockObject = new object();
        protected static T s_instance;

        public static T Instance
        {
            get
            {
                if (s_instance == null)
                {
                    lock (s_lockObject)
                    {
                        if (s_instance == null)
                        {
                            s_instance = new T();
                        }
                    }
                }

                return s_instance;
            }
        }

        public static T Current => s_instance;

        public static void Free()
        {
            s_instance = null;
        }

        /// <summary>
        /// 清空数据
        /// </summary>
        public virtual void ClearData() { }

    }
}