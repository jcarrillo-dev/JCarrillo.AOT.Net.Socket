using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using JCarrillo.AOT.Core.Colecciones.Pooled;
using JCarrillo.AOT.Core.Colecciones.Pooled.Ref;

namespace JCarrillo.AOT.Net.Socket.Benchmarks
{
    using Socket = System.Net.Sockets.Socket;

    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net10_0, baseline: true)]
    [SimpleJob(RuntimeMoniker.NativeAot10_0)]
    public class SocketBenchmarks
    {
        private Socket? _serverListener;
        private Socket? _serverConnected;
        private Socket? _standardClient;
        
        private SocketAot _aotClient;
        private SocketAot _aotServerConnected;
        
        private IPEndPoint? _endPoint;
        
        [GlobalSetup]
        public void Setup()
        {
            _endPoint = new IPEndPoint(IPAddress.Loopback, 0);
            
            // Inicializar Listener
            _serverListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _serverListener.Bind(_endPoint);
            _serverListener.Listen(1);
            
            int port = ((IPEndPoint)_serverListener.LocalEndPoint!).Port;
            var connectEndPoint = new IPEndPoint(IPAddress.Loopback, port);
            
            // Establecer conexiones estándar
            _standardClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _standardClient.Connect(connectEndPoint);
            _serverConnected = _serverListener.Accept();
            
            // Establecer conexiones optimizadas AotSocket
            var aotClientRaw = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            aotClientRaw.Connect(connectEndPoint);
            var aotServerConnectedRaw = _serverListener.Accept();
            
            _aotClient = new SocketAot(aotClientRaw);
            _aotServerConnected = new SocketAot(aotServerConnectedRaw);
        }
        
        [GlobalCleanup]
        public void Cleanup()
        {
            _standardClient?.Dispose();
            _serverConnected?.Dispose();
            _aotClient.Dispose();
            _aotServerConnected.Dispose();
            _serverListener?.Dispose();
        }

        #region Grupo 1: Sockets Estándar (.NET)

        [Benchmark(Baseline = true)]
        public void StandardSocket_Array_Sync()
        {
            byte[] bufferToSend = new byte[128];
            byte[] bufferToReceive = new byte[128];
            
            _standardClient!.Send(bufferToSend);
            int read = 0;
            while (read < 128)
            {
                read += _serverConnected!.Receive(bufferToReceive, read, 128 - read, SocketFlags.None);
            }
        }

        [Benchmark]
        public async ValueTask StandardSocket_Array_Async()
        {
            byte[] bufferToSend = new byte[128];
            byte[] bufferToReceive = new byte[128];
            
            await _standardClient!.SendAsync(bufferToSend.AsMemory(), SocketFlags.None);
            int read = 0;
            while (read < 128)
            {
                read += await _serverConnected!.ReceiveAsync(bufferToReceive.AsMemory(read, 128 - read), SocketFlags.None);
            }
        }

        [Benchmark]
        public async ValueTask StandardSocket_PooledArray_Async()
        {
            var bufferToSend = new PooledArray<byte>(128);
            var bufferToReceive = new PooledArray<byte>(128);
            try
            {
                await _standardClient!.SendAsync(bufferToSend.Memory, SocketFlags.None);
                int read = 0;
                while (read < 128)
                {
                    read += await _serverConnected!.ReceiveAsync(bufferToReceive.Memory[read..128], SocketFlags.None);
                }
            }
            finally
            {
                bufferToSend.Dispose();
                bufferToReceive.Dispose();
            }
        }

        [Benchmark]
        public void StandardSocket_PooledArrayRef_Sync()
        {
            var bufferToSend = new PooledArrayRef<byte>(128);
            var bufferToReceive = new PooledArrayRef<byte>(128);
            try
            {
                _standardClient!.Send(bufferToSend.Span);
                int read = 0;
                while (read < 128)
                {
                    read += _serverConnected!.Receive(bufferToReceive.Span[read..128]);
                }
            }
            finally
            {
                bufferToSend.Dispose();
                bufferToReceive.Dispose();
            }
        }

        #endregion

        #region Grupo 2: Sockets Optimizados AOT (SocketAot)

        [Benchmark]
        public void AotSocket_Array_Sync()
        {
            byte[] bufferToSend = new byte[128];
            byte[] bufferToReceive = new byte[128];
            
            _aotClient.Enviar(bufferToSend);
            int read = 0;
            while (read < 128)
            {
                read += _aotServerConnected.Recibir(bufferToReceive.AsSpan(read, 128 - read));
            }
        }

        [Benchmark]
        public async ValueTask AotSocket_Array_Async()
        {
            byte[] bufferToSend = new byte[128];
            byte[] bufferToReceive = new byte[128];
            
            await _aotClient.EnviarAsync(bufferToSend.AsMemory());
            int read = 0;
            while (read < 128)
            {
                read += await _aotServerConnected.RecibirAsync(bufferToReceive.AsMemory(read, 128 - read));
            }
        }

        [Benchmark]
        public async ValueTask AotSocket_PooledArray_Async()
        {
            var bufferToSend = new PooledArray<byte>(128);
            var bufferToReceive = new PooledArray<byte>(128);
            try
            {
                await _aotClient.EnviarAsync(ref bufferToSend);
                int read = 0;
                while (read < 128)
                {
                    read += await _aotServerConnected.RecibirAsync(bufferToReceive.Memory[read..128]);
                }
            }
            finally
            {
                bufferToSend.Dispose();
                bufferToReceive.Dispose();
            }
        }

        [Benchmark]
        public void AotSocket_PooledArrayRef_Sync()
        {
            var bufferToSend = new PooledArrayRef<byte>(128);
            var bufferToReceive = new PooledArrayRef<byte>(128);
            try
            {
                _aotClient.Enviar(ref bufferToSend);
                int read = 0;
                while (read < 128)
                {
                    read += _aotServerConnected.Recibir(ref bufferToReceive);
                }
            }
            finally
            {
                bufferToSend.Dispose();
                bufferToReceive.Dispose();
            }
        }

        #endregion
    }
}
