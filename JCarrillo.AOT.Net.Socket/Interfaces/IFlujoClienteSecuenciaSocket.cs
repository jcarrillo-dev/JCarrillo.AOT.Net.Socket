using System.Net;

namespace JCarrillo.AOT.Net.Socket.Interfaces
{
    /// <summary>
    /// Abstracción que define un flujo continuo cliente que maneja la conexión al host remoto.
    /// </summary>
    public interface IFlujoClienteSecuenciaSocket : IFlujoSecuenciaSocket
    {
        /// <summary>
        /// Dirección remota a la que debe conectarse el socket.
        /// </summary>
        EndPoint ExtremoRemoto { get; }
    }
}
