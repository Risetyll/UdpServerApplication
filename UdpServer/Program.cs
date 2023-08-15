using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UdpServer
{
    class Program
    {
        private static IPacketDevice listenerDevice = null;

        private static ushort _serverPort = 5000;
        private static MacAddress _serverMacAddress = new MacAddress("02:02:02:02:02:02");
        private static IpV4Address _serverIpAddress = new IpV4Address("127.0.0.1");
        
        private static MacAddress _clientMacAddress;
        private static IpV4Address _clientIpAddress;
        private static ushort _clientPort;

        private static Packet BuildUdpPacket()
        {
            EthernetLayer ethernetLayer = new EthernetLayer
            {
                Source = _serverMacAddress,
                Destination = _clientMacAddress,
                EtherType = EthernetType.None, // Will be filled automatically.
            };

            IpV4Layer ipV4Layer = new IpV4Layer
            {
                Source = _serverIpAddress,
                CurrentDestination = _clientIpAddress,
                Fragmentation = IpV4Fragmentation.None,
                HeaderChecksum = null, // Will be filled automatically.
                Identification = 123,
                Options = IpV4Options.None,
                Protocol = null, // Will be filled automatically.
                Ttl = 255,
                TypeOfService = 0,
            };

            UdpLayer udpLayer = new UdpLayer
            {
                SourcePort = _serverPort,
                DestinationPort = _clientPort,
                Checksum = null, // Will be filled automatically.
                CalculateChecksumValue = true,
            };

            PayloadLayer payloadLayer = new PayloadLayer
            {
                Data = new Datagram(Encoding.ASCII.GetBytes(GenerateRandomString())),
            };

            PacketBuilder builder = new PacketBuilder(ethernetLayer, ipV4Layer, udpLayer, payloadLayer);

            return builder.Build(DateTime.Now);
        }

        private static string GenerateRandomString()
        {
            Random random = new Random();
            string randomString = string.Empty;

            for (int i = 0; i < 128 - random.Next(0, 100); i++)
            {
                randomString += (char)random.Next(33, 127);
            }

            return randomString;
        }

        private static void PacketHandler(Packet packet)
        {
            Console.WriteLine($"Получен пакет: {packet}");
            Console.WriteLine();
            ResponsePackets(listenerDevice, packet);
        }
        private static void ReceivePackets(IPacketDevice device)
        {
            using (PacketCommunicator communicator = device.Open(100, PacketDeviceOpenAttributes.Promiscuous, 1000))
            {
                communicator.SetFilter($"udp and ether dst host {_serverMacAddress}");
                Console.WriteLine("Сервер слушает...");

                communicator.ReceivePackets(0, PacketHandler);
            }
        }
        
        private static void ResponsePackets(IPacketDevice device, Packet packet)
        {
            using (PacketCommunicator communicator = device.Open(100, PacketDeviceOpenAttributes.DataTransferUdpRemote, 1000))
            {
                _clientMacAddress = packet.Ethernet.Source;
                _clientIpAddress = packet.Ethernet.IpV4.Source;
                _clientPort = packet.Ethernet.IpV4.Udp.SourcePort;
                communicator.SendPacket(BuildUdpPacket());
            }
        }

        public static void Main(string[] args)
        {
            var allDevices = LivePacketDevice.AllLocalMachine;
            

            foreach (var item in allDevices)
            {
                Console.WriteLine($"{allDevices.IndexOf(item) + 1}. {item.Description}");
            }
            Console.Write("Выберите устройство: ");
            listenerDevice = allDevices[int.Parse(Console.ReadLine()) - 1];

            ReceivePackets(listenerDevice);
        }
    }
}
