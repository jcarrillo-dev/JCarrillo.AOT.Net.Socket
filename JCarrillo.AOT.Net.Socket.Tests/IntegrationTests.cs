using JCarrillo.AOT.Net.Socket.Interfaces;
using System.Net;
using System.Net.Sockets;
using Xunit;

namespace JCarrillo.AOT.Net.Socket.Tests
{
    using Socket = System.Net.Sockets.Socket;
    public class IntegrationTests
    {
        [Fact]
        public async Task Integration_TcpFlow_ZeroAllocationCommunication_Succeeds()
        {
            // Determinar puerto libre vinculando al puerto 0 dinámico
            IPEndPoint targetEndPoint = new(IPAddress.Loopback, 0);
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));

            Socket serverSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            SocketAot serverSocketAot = new(serverSocket);
            FlujoAceptarPruebaServidor serverFlow = new(targetEndPoint);

            // Iniciar servidor en segundo plano
            ValueTask serverTask = serverSocketAot.EjecutarFlujoAceptarAsync(serverFlow, cts.Token);

            // Recuperar el puerto dinámico asignado por el sistema operativo
            int assignedPort = ((IPEndPoint)serverSocket.LocalEndPoint!).Port;
            IPEndPoint assignedEndPoint = new(IPAddress.Loopback, assignedPort);

            Socket clientSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            SocketAot clientSocketAot = new(clientSocket);
            FlujoClienteSecuenciaPrueba clientFlow = new(assignedEndPoint);

            // Iniciar flujo de conexión del cliente en segundo plano
            ValueTask clientTask = clientSocketAot.EjecutarFlujoClienteSecuenciaAsync(clientFlow, cts.Token);

            // Esperar que la conexión sea aceptada por el servidor
            await serverFlow.EsperarAceptadoAsync(cts.Token);

            // Enviar datos del cliente al servidor para verificar que el servidor recibe bytes
            await clientSocketAot.EnviarAsync(new byte[] { 1, 2, 3 }, cts.Token);
            await serverFlow.EsperarBytesRecibidosAsync(3, cts.Token);

            // Enviar datos desde el servidor hacia el cliente aceptado
            await serverFlow.EnviarRespuestaAlClienteAsync(new byte[] { 42, 84, 126 }, cts.Token);

            // Esperar a que el cliente reciba la cantidad esperada de bytes
            await clientFlow.EsperarBytesRecibidosAsync(3, cts.Token);

            // Limpieza y cancelación limpia
            cts.Cancel();
            try
            {
                await serverTask;
            }
            catch (OperationCanceledException) { }

            try
            {
                await clientTask;
            }
            catch (OperationCanceledException) { }

            // Aserciones finales
            Assert.True(serverFlow.CantidadSocketsAceptados > 0, "El servidor debió aceptar al menos una conexión.");
            Assert.True(serverFlow.CantidadBytesRecibidos > 0, "El servidor debió recibir bytes del cliente.");
            Assert.Equal(3, clientFlow.CantidadBytesRecibidos);
            Assert.Equal(42, clientFlow.BufferRecibido[0]);
            Assert.Equal(84, clientFlow.BufferRecibido[1]);
            Assert.Equal(126, clientFlow.BufferRecibido[2]);
        }

        #region Flujos Privados de Prueba

        private sealed class FlujoAceptarPruebaServidor(EndPoint extremoLocal) : IFlujoAceptarSocket
        {
            public EndPoint ExtremoLocal { get; } = extremoLocal;
            public int ColaConexiones => 10;
            public int CantidadSocketsAceptados { get; private set; }
            public long CantidadBytesRecibidos { get; private set; }

            private SocketAot _socketClienteAceptado;
            private readonly SemaphoreSlim _senalAceptado = new SemaphoreSlim(0);
            private readonly SemaphoreSlim _senalBytesRecibidos = new SemaphoreSlim(0);
            private int _bytesEsperados;

            public void AlConfigurarEscucha(Socket socketEscucha)
                => socketEscucha.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            public void AlConfigurarAceptado(Socket socketAceptado)
                => socketAceptado.NoDelay = true;

            public async ValueTask AlAceptarConexionAsync(SocketAot socketAceptado, CancellationToken cancellationToken)
            {
                CantidadSocketsAceptados++;
                _socketClienteAceptado = socketAceptado;
                _senalAceptado.Release();

                FlujoSecuenciaServidorCliente flujoCliente = new(this);
                await socketAceptado.EjecutarFlujoSecuenciaAsync(flujoCliente, cancellationToken).ConfigureAwait(false);
            }

            public void AlOcurrirError(Exception excepcion) { }

            public async ValueTask EsperarAceptadoAsync(CancellationToken cancellationToken)
            {
                await _senalAceptado.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            public async ValueTask EnviarRespuestaAlClienteAsync(ReadOnlyMemory<byte> respuesta, CancellationToken cancellationToken)
            {
                if (_socketClienteAceptado.EsValido)
                    await _socketClienteAceptado.EnviarAsync(respuesta, cancellationToken).ConfigureAwait(false);
            }

            public async ValueTask EsperarBytesRecibidosAsync(int bytesEsperados, CancellationToken cancellationToken)
            {
                _bytesEsperados = bytesEsperados;
                if (CantidadBytesRecibidos >= bytesEsperados)
                    return;

                await _senalBytesRecibidos.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            private sealed class FlujoSecuenciaServidorCliente(FlujoAceptarPruebaServidor padre) : IFlujoSecuenciaSocket
            {
                private readonly FlujoAceptarPruebaServidor _padre = padre;
                public int TamañoBuffer => 128;

                public void AlConfigurar(Socket socket) { }

                public ValueTask AlRecibirAsync(ReadOnlyMemory<byte> datos, CancellationToken cancellationToken)
                {
                    _padre.CantidadBytesRecibidos += datos.Length;
                    if (_padre.CantidadBytesRecibidos >= _padre._bytesEsperados)
                        _padre._senalBytesRecibidos.Release();
                    return ValueTask.CompletedTask;
                }

                public void AlOcurrirError(Exception excepcion) { }
                public void AlCompletar() { }
            }
        }

        private sealed class FlujoClienteSecuenciaPrueba(EndPoint extremoRemoto) : IFlujoClienteSecuenciaSocket
        {
            public EndPoint ExtremoRemoto { get; } = extremoRemoto;
            public int TamañoBuffer => 128;
            public byte[] BufferRecibido { get; } = new byte[128];
            public int CantidadBytesRecibidos { get; private set; }

            private readonly SemaphoreSlim _senalBytesRecibidos = new SemaphoreSlim(0);
            private int _bytesEsperados;

            public void AlConfigurar(Socket socket)
                => socket.NoDelay = true;

            public async ValueTask AlRecibirAsync(ReadOnlyMemory<byte> datos, CancellationToken cancellationToken)
            {
                datos.CopyTo(BufferRecibido.AsMemory(CantidadBytesRecibidos));
                CantidadBytesRecibidos += datos.Length;

                if (CantidadBytesRecibidos >= _bytesEsperados)
                    _senalBytesRecibidos.Release();

                await ValueTask.CompletedTask;
            }

            public async ValueTask EsperarBytesRecibidosAsync(int bytesEsperados, CancellationToken cancellationToken)
            {
                _bytesEsperados = bytesEsperados;
                if (CantidadBytesRecibidos >= bytesEsperados)
                    return;

                await _senalBytesRecibidos.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            public void AlOcurrirError(Exception excepcion) { }
            public void AlCompletar() { }
        }

        #endregion
    }
}
