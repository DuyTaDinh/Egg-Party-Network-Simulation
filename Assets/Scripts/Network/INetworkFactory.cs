using GridSystem.Core;
using Network.Client;
using Network.Server;
using Network.Transport;
namespace Network
{
	public interface INetworkFactory
	{
		INetworkTransport CreateTransport();
		IServer CreateServer(MapGridData mapData);
		IClient CreateClient(MapGridData mapData);
	}
}