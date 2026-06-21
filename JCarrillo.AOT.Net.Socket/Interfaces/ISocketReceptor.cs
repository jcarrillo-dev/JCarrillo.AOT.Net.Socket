using JCarrillo.AOT.Core.Colecciones.Pooled;
using JCarrillo.AOT.Core.Colecciones.Pooled.Ref;

namespace JCarrillo.AOT.Net.Socket.Interfaces
{
    /// <summary>
    /// Define el contrato para la recepción de datos orientada a alto rendimiento.
    /// </summary>
    public interface ISocketReceptor
    {
        /// <summary>
        /// Recibe datos de forma asíncrona en el buffer de memoria.
        /// </summary>
        ValueTask<int> RecibirAsync(Memory<byte> buffer, CancellationToken cancellationToken = default);

        /// <summary>
        /// Recibe datos de forma síncrona en el tramo de pila (Span).
        /// </summary>
        int Recibir(Span<byte> buffer);

        /// <summary>
        /// Recibe datos de forma asíncrona directamente en el buffer alquilado de tipo PooledArray.
        /// </summary>
        ValueTask<int> RecibirAsync(ref PooledArray<byte> buffer, CancellationToken cancellationToken = default);

        /// <summary>
        /// Recibe datos de forma síncrona directamente en el buffer alquilado de tipo PooledArray.
        /// </summary>
        int Recibir(ref PooledArray<byte> buffer);

        /// <summary>
        /// Recibe datos de forma síncrona directamente en el buffer alquilado en pila de tipo PooledArrayRef.
        /// </summary>
        int Recibir(ref PooledArrayRef<byte> buffer);
    }
}
