namespace JCarrillo.AOT.Net.Socket.Interfaces
{
    /// <summary>
    /// Representa una conexión de red activa que soporta envío y recepción bidireccional y control de ciclo de vida.
    /// </summary>
    public interface ISocketConexion : ISocketReceptor, ISocketEmisor, IDisposable
    {
        /// <summary>
        /// Cierra la conexión física del socket.
        /// </summary>
        void Cerrar();
    }
}
