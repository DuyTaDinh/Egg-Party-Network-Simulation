using GridSystem.Core;
using Network.Client;
using Network.Server;
using Network.Transport;
namespace Network
{
	public class SimulatedNetworkFactory : INetworkFactory
	{
		private readonly NetworkSettings settings;

		public SimulatedNetworkFactory(NetworkSettings settings)
		{
			this.settings = settings;
		}

		public INetworkTransport CreateTransport()
		{
			var transport = new SimulatedNetworkTransport();
			transport.SetLatency(settings.minLatency, settings.maxLatency);
			transport.PacketLossChance = settings.packetLossChance;
			transport.DebugMode = settings.debugMode;
			return transport;
		}

		public IServer CreateServer(MapGridData mapData)
		{
			var server = new ServerSimulator(mapData)
			{
				DebugMode = settings.debugMode
			};
			return server;
		}

		public IClient CreateClient(MapGridData mapData)
		{
			ClientSimulator client = new ClientSimulator(mapData)
			{
				DebugMode = settings.debugMode,
				EnablePrediction = settings.enablePrediction,
				EnableReconciliation = settings.enableReconciliation,
				EnableInterpolation = settings.enableInterpolation,
				InterpolationDelay = settings.interpolationDelay
			};
			return client;
		}
	}
}