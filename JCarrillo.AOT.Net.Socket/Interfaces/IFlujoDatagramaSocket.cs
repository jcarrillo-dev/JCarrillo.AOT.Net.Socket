using System.Net;

namespace JCarrillo.AOT.Net.Socket.Interfaces
{
    using Socket = System.Net.Sockets.Socket;
    /// <summary>
    /// Abstracción para el flujo de sockets orientados a datagramas sin conexión (UDP).
    /// </summary>
    public interface IFlujoDatagramaSocket
    {
        /// <summary>
        /// Tamaño máximo de buffer de datagrama.
        /// </summary>
        int TamañoBuffer { get; }

        /// <summary>
        /// Endpoint local en el que el socket UDP realizará el Bind.
        /// </summary>
        EndPoint ExtremoLocal { get; }

        /// <summary>
        /// Configura opciones de red de datagrama en el socket físico.
        /// </summary>
        void AlConfigurar(Socket socket);

        /// <summary>
        /// Se invoca al recibir un datagrama individual desde un host remoto.
        /// Los consumidores no deben retener la referencia al buffer tras completarse el ValueTask.
        /// </summary>
        ValueTask AlRecibirDatagramaAsync(ReadOnlyMemory<byte> datos, EndPoint extremoRemoto, CancellationToken cancellationToken);

        /// <summary>
        /// Se invoca ante fallas críticas en la recepción.
        /// </summary>
        void AlOcurrirError(Exception excepcion);
    }
}
