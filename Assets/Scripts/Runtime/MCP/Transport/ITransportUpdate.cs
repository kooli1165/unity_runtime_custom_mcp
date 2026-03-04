using System.Threading;

namespace Runtime.MCP.Transport
{
    public interface ITransportUpdate
    {
        public void Update(CancellationToken cancellationToken);
    }
}