using System.Net;

namespace JCarrillo.AOT.Net.Socket.Interfaces
{
    using Socket = System.Net.Sockets.Socket;
    /// <summary>
    /// Abstracción para el bucle del socket servidor de aceptación de conexiones (TCP Listener).
    /// </summary>
    public interface IFlujoAceptarSocket
    {
        /// <summary>
        /// Dirección local de escucha de conexiones.
        /// </summary>
        EndPoint ExtremoLocal { get; }

        /// <summary>
        /// Longitud de la cola de conexiones en espera.
        /// </summary>
        int ColaConexiones { get; }

        /// <summary>
        /// Configura opciones del socket que escucha conexiones entrantes.
        /// </summary>
        void AlConfigurarEscucha(Socket socketEscucha);

        /// <summary>
        /// Configura opciones del socket cliente aceptado (ej. NoDelay).
        /// </summary>
        void AlConfigurarAceptado(Socket socketAceptado);

        /// <summary>
        /// Se invoca cuando una nueva conexión es establecida.
        /// </summary>
        ValueTask AlAceptarConexionAsync(SocketAot socketAceptado, CancellationToken cancellationToken);

        /// <summary>
        /// Se invoca ante fallos en la aceptación de conexiones.
        /// </summary>
        void AlOcurrirError(Exception excepcion);
    }
}
