using JCarrillo.AOT.Core.Colecciones.Pooled;
using JCarrillo.AOT.Core.Colecciones.Pooled.Ref;

namespace JCarrillo.AOT.Net.Socket.Interfaces
{
    /// <summary>
    /// Define el contrato para el envío de datos orientado a alto rendimiento.
    /// </summary>
    public interface ISocketEmisor
    {
        /// <summary>
        /// Envía datos de forma asíncrona desde el buffer de memoria.
        /// </summary>
        ValueTask<int> EnviarAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default);

        /// <summary>
        /// Envía datos de forma síncrona desde el tramo de pila de sólo lectura (ReadOnlySpan).
        /// </summary>
        int Enviar(ReadOnlySpan<byte> buffer);

        /// <summary>
        /// Envía datos de forma asíncrona directamente desde el buffer alquilado de tipo PooledArray.
        /// </summary>
        ValueTask<int> EnviarAsync(ref PooledArray<byte> buffer, CancellationToken cancellationToken = default);

        /// <summary>
        /// Envía datos de forma síncrona directamente desde el buffer alquilado de tipo PooledArray.
        /// </summary>
        int Enviar(ref PooledArray<byte> buffer);

        /// <summary>
        /// Envía datos de forma síncrona directamente desde el buffer alquilado en pila de tipo PooledArrayRef.
        /// </summary>
        int Enviar(ref PooledArrayRef<byte> buffer);
    }
}
