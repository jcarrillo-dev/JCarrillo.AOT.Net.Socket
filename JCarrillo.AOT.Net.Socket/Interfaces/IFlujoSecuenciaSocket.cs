namespace JCarrillo.AOT.Net.Socket.Interfaces
{
    using Socket = System.Net.Sockets.Socket;

    /// <summary>
    /// Abstracción que define el comportamiento para un flujo continuo basado en flujos de datos (TCP).
    /// </summary>
    public interface IFlujoSecuenciaSocket
    {
        /// <summary>
        /// Tamaño sugerido para el buffer de recepción.
        /// </summary>
        int TamañoBuffer { get; }

        /// <summary>
        /// Configura opciones del socket antes de iniciar la recepción.
        /// </summary>
        void AlConfigurar(Socket socket);

        /// <summary>
        /// Se invoca cuando se leen datos de la red.
        /// Los consumidores no deben retener la referencia al buffer de memoria tras completarse el ValueTask retornado.
        /// </summary>
        ValueTask AlRecibirAsync(ReadOnlyMemory<byte> datos, CancellationToken cancellationToken);

        /// <summary>
        /// Se invoca en caso de error durante la ejecución del bucle.
        /// </summary>
        void AlOcurrirError(Exception excepcion);

        /// <summary>
        /// Se invoca cuando la conexión se ha completado/cerrado limpiamente.
        /// </summary>
        void AlCompletar();
    }
}
