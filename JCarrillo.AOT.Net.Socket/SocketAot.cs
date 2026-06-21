using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using JCarrillo.AOT.Core.Colecciones.Pooled;
using JCarrillo.AOT.Core.Colecciones.Pooled.Ref;
using JCarrillo.AOT.Net.Socket.Interfaces;

namespace JCarrillo.AOT.Net.Socket
{
    using Socket = System.Net.Sockets.Socket;

    /// <summary>
    /// Envoltura inmutable de tipo struct sobre el socket físico del sistema.
    /// Diseñada como un guardián de API que restringe el acceso únicamente a firmas Zero-Allocation y Native AOT.
    /// </summary>
    /// <param name="socket">El socket físico subyacente del sistema.</param>
    public readonly struct SocketAot(Socket socket) : ISocketConexion
    {
        private readonly Socket? _socket = socket ?? throw new ArgumentNullException(nameof(socket));

        /// <summary>
        /// Indica si la instancia está correctamente inicializada con un socket físico activo.
        /// </summary>
        public bool EsValido => _socket != null;

        /// <summary>
        /// Obtiene si el socket subyacente está conectado al extremo remoto.
        /// </summary>
        public bool Conectado
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _socket?.Connected ?? false;
        }

        /// <summary>
        /// Obtiene el extremo de red local al que está vinculado el socket.
        /// </summary>
        public EndPoint? ExtremoLocal
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _socket?.LocalEndPoint;
        }

        /// <summary>
        /// Obtiene el extremo de red remoto al que está conectado el socket.
        /// </summary>
        public EndPoint? ExtremoRemoto
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _socket?.RemoteEndPoint;
        }

        /// <summary>
        /// Obtiene la familia de direcciones del socket.
        /// </summary>
        public AddressFamily FamiliaDirecciones
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => SocketSeguro.AddressFamily;
        }

        /// <summary>
        /// Obtiene el tipo de socket.
        /// </summary>
        public SocketType TipoSocket
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => SocketSeguro.SocketType;
        }

        /// <summary>
        /// Obtiene el protocolo del socket.
        /// </summary>
        public ProtocolType TipoProtocolo
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => SocketSeguro.ProtocolType;
        }

        /// <summary>
        /// Obtiene o establece si el socket deshabilita el algoritmo de Nagle para transmisiones TCP inmediatas.
        /// </summary>
        public bool NoDelay
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => SocketSeguro.NoDelay;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SocketSeguro.NoDelay = value;
        }

        /// <summary>
        /// Obtiene el socket subyacente del sistema para uso interno de la biblioteca.
        /// </summary>
        internal Socket SocketSubyacente => SocketSeguro;

        private Socket SocketSeguro
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_socket is null)
                    LanzarExcepcionNull();
                return _socket;
            }
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void LanzarExcepcionNull() 
            => throw new InvalidOperationException("El socket no está inicializado.");

        #region Operaciones de bajo nivel directas

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<int> RecibirAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) 
            => RecibirAsync(buffer, SocketFlags.None, cancellationToken);

        /// <summary>
        /// Recibe datos de forma asíncrona en el buffer de memoria.
        /// </summary>
        /// <param name="buffer">El buffer de memoria de destino.</param>
        /// <param name="socketFlags">Los flags de control del socket.</param>
        /// <param name="cancellationToken">El token de cancelación.</param>
        /// <returns>Una tarea que representa la recepción asíncrona con el número de bytes recibidos.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<int> RecibirAsync(Memory<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken = default) 
            => SocketSeguro.ReceiveAsync(buffer, socketFlags, cancellationToken);

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Recibir(Span<byte> buffer) 
            => Recibir(buffer, SocketFlags.None);

        /// <summary>
        /// Recibe datos de forma síncrona en el tramo de pila (Span).
        /// </summary>
        /// <param name="buffer">El tramo de pila de destino.</param>
        /// <param name="socketFlags">Los flags de control del socket.</param>
        /// <returns>El número de bytes recibidos.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Recibir(Span<byte> buffer, SocketFlags socketFlags = SocketFlags.None) 
            => SocketSeguro.Receive(buffer, socketFlags);

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<int> RecibirAsync(ref PooledArray<byte> buffer, CancellationToken cancellationToken = default) 
            => RecibirAsync(ref buffer, SocketFlags.None, cancellationToken);

        /// <summary>
        /// Recibe datos de forma asíncrona directamente en el buffer alquilado de tipo PooledArray.
        /// </summary>
        /// <param name="buffer">El buffer del pool de destino.</param>
        /// <param name="socketFlags">Los flags de control del socket.</param>
        /// <param name="cancellationToken">El token de cancelación.</param>
        /// <returns>Una tarea que representa la recepción asíncrona con el número de bytes recibidos.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<int> RecibirAsync(ref PooledArray<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken = default) 
            => SocketSeguro.ReceiveAsync(buffer.Memory, socketFlags, cancellationToken);

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Recibir(ref PooledArray<byte> buffer) 
            => Recibir(ref buffer, SocketFlags.None);

        /// <summary>
        /// Recibe datos de forma síncrona directamente en el buffer alquilado de tipo PooledArray.
        /// </summary>
        /// <param name="buffer">El buffer del pool de destino.</param>
        /// <param name="socketFlags">Los flags de control del socket.</param>
        /// <returns>El número de bytes recibidos.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Recibir(ref PooledArray<byte> buffer, SocketFlags socketFlags = SocketFlags.None) 
            => SocketSeguro.Receive(buffer.Span, socketFlags);

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Recibir(ref PooledArrayRef<byte> buffer) 
            => Recibir(ref buffer, SocketFlags.None);

        /// <summary>
        /// Recibe datos de forma síncrona directamente en el buffer alquilado en pila de tipo PooledArrayRef.
        /// </summary>
        /// <param name="buffer">El buffer en pila de destino.</param>
        /// <param name="socketFlags">Los flags de control del socket.</param>
        /// <returns>El número de bytes recibidos.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Recibir(ref PooledArrayRef<byte> buffer, SocketFlags socketFlags = SocketFlags.None) 
            => SocketSeguro.Receive(buffer.Span, socketFlags);

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<int> EnviarAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) 
            => EnviarAsync(buffer, SocketFlags.None, cancellationToken);

        /// <summary>
        /// Envía datos de forma asíncrona desde el buffer de memoria.
        /// </summary>
        /// <param name="buffer">El buffer de memoria que contiene los datos.</param>
        /// <param name="socketFlags">Los flags de control del socket.</param>
        /// <param name="cancellationToken">El token de cancelación.</param>
        /// <returns>Una tarea que representa el envío asíncrono con el número de bytes enviados.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<int> EnviarAsync(ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken = default) 
            => SocketSeguro.SendAsync(buffer, socketFlags, cancellationToken);

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Enviar(ReadOnlySpan<byte> buffer) 
            => Enviar(buffer, SocketFlags.None);

        /// <summary>
        /// Envía datos de forma síncrona desde el tramo de pila de sólo lectura (ReadOnlySpan).
        /// </summary>
        /// <param name="buffer">El tramo de pila que contiene los datos.</param>
        /// <param name="socketFlags">Los flags de control del socket.</param>
        /// <returns>El número de bytes enviados.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Enviar(ReadOnlySpan<byte> buffer, SocketFlags socketFlags = SocketFlags.None) 
            => SocketSeguro.Send(buffer, socketFlags);

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<int> EnviarAsync(ref PooledArray<byte> buffer, CancellationToken cancellationToken = default) 
            => EnviarAsync(ref buffer, SocketFlags.None, cancellationToken);

        /// <summary>
        /// Envía datos de forma asíncrona directamente desde el buffer alquilado de tipo PooledArray.
        /// </summary>
        /// <param name="buffer">El buffer del pool que contiene los datos.</param>
        /// <param name="socketFlags">Los flags de control del socket.</param>
        /// <param name="cancellationToken">El token de cancelación.</param>
        /// <returns>Una tarea que representa el envío asíncrono con el número de bytes enviados.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<int> EnviarAsync(ref PooledArray<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken = default) 
            => SocketSeguro.SendAsync(buffer.Memory, socketFlags, cancellationToken);

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Enviar(ref PooledArray<byte> buffer) 
            => Enviar(ref buffer, SocketFlags.None);

        /// <summary>
        /// Envía datos de forma síncrona directamente desde el buffer alquilado de tipo PooledArray.
        /// </summary>
        /// <param name="buffer">El buffer del pool que contiene los datos.</param>
        /// <param name="socketFlags">Los flags de control del socket.</param>
        /// <returns>El número de bytes enviados.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Enviar(ref PooledArray<byte> buffer, SocketFlags socketFlags = SocketFlags.None) 
            => SocketSeguro.Send(buffer.Span, socketFlags);

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Enviar(ref PooledArrayRef<byte> buffer) 
            => Enviar(ref buffer, SocketFlags.None);

        /// <summary>
        /// Envía datos de forma síncrona directamente desde el buffer alquilado en pila de tipo PooledArrayRef.
        /// </summary>
        /// <param name="buffer">El buffer en pila que contiene los datos.</param>
        /// <param name="socketFlags">Los flags de control del socket.</param>
        /// <returns>El número de bytes enviados.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Enviar(ref PooledArrayRef<byte> buffer, SocketFlags socketFlags = SocketFlags.None) 
            => SocketSeguro.Send(buffer.Span, socketFlags);

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Cerrar() 
            => _socket?.Close();

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() 
            => _socket?.Dispose();

        #endregion

        #region Operaciones de Datagramas (UDP) Zero-Allocation

        /// <summary>
        /// Recibe un datagrama asíncronamente y retorna el extremo remoto emisor, utilizando un buffer del pool.
        /// </summary>
        /// <param name="buffer">El buffer alquilado del pool donde escribir los datos recibidos.</param>
        /// <param name="extremoRemoto">El extremo de red remoto de origen.</param>
        /// <param name="cancellationToken">Token de cancelación opcional.</param>
        /// <returns>Una tarea que representa la recepción asíncrona con el número de bytes y el extremo remoto emisor.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<SocketReceiveFromResult> RecibirDesdeAsync(ref PooledArray<byte> buffer, EndPoint extremoRemoto, CancellationToken cancellationToken = default)
            => RecibirDesdeAsync(ref buffer, extremoRemoto, SocketFlags.None, cancellationToken);

        /// <summary>
        /// Recibe un datagrama asíncronamente y retorna el extremo remoto emisor, utilizando un buffer del pool.
        /// </summary>
        /// <param name="buffer">El buffer alquilado del pool donde escribir los datos recibidos.</param>
        /// <param name="extremoRemoto">El extremo de red remoto de origen.</param>
        /// <param name="socketFlags">Los flags de control del socket.</param>
        /// <param name="cancellationToken">Token de cancelación opcional.</param>
        /// <returns>Una tarea que representa la recepción asíncrona con el número de bytes y el extremo remoto emisor.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<SocketReceiveFromResult> RecibirDesdeAsync(ref PooledArray<byte> buffer, EndPoint extremoRemoto, SocketFlags socketFlags, CancellationToken cancellationToken = default)
            => SocketSeguro.ReceiveFromAsync(buffer.Memory, socketFlags, extremoRemoto, cancellationToken);

        /// <summary>
        /// Recibe un datagrama síncronamente y escribe el extremo remoto emisor, utilizando un buffer del pool.
        /// </summary>
        /// <param name="buffer">El buffer alquilado del pool donde escribir los datos recibidos.</param>
        /// <param name="extremoRemoto">El extremo de red remoto de origen.</param>
        /// <param name="socketFlags">Los flags de control del socket.</param>
        /// <returns>La cantidad de bytes recibidos.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int RecibirDesde(ref PooledArray<byte> buffer, ref EndPoint extremoRemoto, SocketFlags socketFlags = SocketFlags.None)
            => SocketSeguro.ReceiveFrom(buffer.Span, socketFlags, ref extremoRemoto);

        /// <summary>
        /// Recibe un datagrama síncronamente y escribe el extremo remoto emisor, utilizando un buffer de pila.
        /// </summary>
        /// <param name="buffer">El buffer alquilado en pila (ref struct) donde escribir los datos recibidos.</param>
        /// <param name="extremoRemoto">El extremo de red remoto de origen.</param>
        /// <param name="socketFlags">Los flags de control del socket.</param>
        /// <returns>La cantidad de bytes recibidos.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int RecibirDesde(ref PooledArrayRef<byte> buffer, ref EndPoint extremoRemoto, SocketFlags socketFlags = SocketFlags.None)
            => SocketSeguro.ReceiveFrom(buffer.Span, socketFlags, ref extremoRemoto);

        /// <summary>
        /// Envía un datagrama asíncronamente a un extremo remoto específico, utilizando un buffer del pool.
        /// </summary>
        /// <param name="buffer">El buffer alquilado del pool que contiene los datos a enviar.</param>
        /// <param name="extremoRemoto">El extremo de red remoto de destino.</param>
        /// <param name="cancellationToken">Token de cancelación opcional.</param>
        /// <returns>Una tarea que representa el envío asíncrono con la cantidad de bytes transmitidos.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<int> EnviarAAsync(ref PooledArray<byte> buffer, EndPoint extremoRemoto, CancellationToken cancellationToken = default)
            => EnviarAAsync(ref buffer, extremoRemoto, SocketFlags.None, cancellationToken);

        /// <summary>
        /// Envía un datagrama asíncronamente a un extremo remoto específico, utilizando un buffer del pool.
        /// </summary>
        /// <param name="buffer">El buffer alquilado del pool que contiene los datos a enviar.</param>
        /// <param name="extremoRemoto">El extremo de red remoto de destino.</param>
        /// <param name="socketFlags">Los flags de control del socket.</param>
        /// <param name="cancellationToken">Token de cancelación opcional.</param>
        /// <returns>Una tarea que representa el envío asíncrono con la cantidad de bytes transmitidos.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<int> EnviarAAsync(ref PooledArray<byte> buffer, EndPoint extremoRemoto, SocketFlags socketFlags, CancellationToken cancellationToken = default)
            => SocketSeguro.SendToAsync(buffer.Memory, socketFlags, extremoRemoto, cancellationToken);

        /// <summary>
        /// Envía un datagrama síncronamente a un extremo remoto específico, utilizando un buffer del pool.
        /// </summary>
        /// <param name="buffer">El buffer alquilado del pool que contiene los datos a enviar.</param>
        /// <param name="extremoRemoto">El extremo de red remoto de destino.</param>
        /// <param name="socketFlags">Los flags de control del socket.</param>
        /// <returns>La cantidad de bytes enviados.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int EnviarA(ref PooledArray<byte> buffer, EndPoint extremoRemoto, SocketFlags socketFlags = SocketFlags.None)
            => SocketSeguro.SendTo(buffer.Span, socketFlags, extremoRemoto);

        /// <summary>
        /// Envía un datagrama síncronamente a un extremo remoto específico, utilizando un buffer de pila.
        /// </summary>
        /// <param name="buffer">El buffer alquilado en pila (ref struct) que contiene los datos a enviar.</param>
        /// <param name="extremoRemoto">El extremo de red remoto de destino.</param>
        /// <param name="socketFlags">Los flags de control del socket.</param>
        /// <returns>La cantidad de bytes enviados.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int EnviarA(ref PooledArrayRef<byte> buffer, EndPoint extremoRemoto, SocketFlags socketFlags = SocketFlags.None)
            => SocketSeguro.SendTo(buffer.Span, socketFlags, extremoRemoto);

        #endregion

        #region Inicialización, Conexión y Opciones de Configuración

        /// <summary>
        /// Vincula el socket a un extremo local específico.
        /// </summary>
        /// <param name="extremoLocal">El extremo de red local.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Vincular(EndPoint extremoLocal) 
            => SocketSeguro.Bind(extremoLocal);

        /// <summary>
        /// Coloca el socket en estado de escucha para conexiones entrantes.
        /// </summary>
        /// <param name="colaConexiones">El tamaño de la cola de conexiones entrantes pendientes.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Escuchar(int colaConexiones) 
            => SocketSeguro.Listen(colaConexiones);

        /// <summary>
        /// Conecta el socket al extremo de red remoto de forma asíncrona.
        /// </summary>
        /// <param name="extremoRemoto">El extremo de red remoto de destino.</param>
        /// <param name="cancellationToken">Token de cancelación opcional.</param>
        /// <returns>Una tarea que representa la operación de conexión asíncrona.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask ConectarAsync(EndPoint extremoRemoto, CancellationToken cancellationToken = default)
            => SocketSeguro.ConnectAsync(extremoRemoto, cancellationToken);

        /// <summary>
        /// Conecta el socket al extremo de red remoto de forma síncrona.
        /// </summary>
        /// <param name="extremoRemoto">El extremo de red remoto de destino.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Conectar(EndPoint extremoRemoto)
            => SocketSeguro.Connect(extremoRemoto);

        /// <summary>
        /// Establece una opción específica del socket físico sin allocations de heap.
        /// </summary>
        /// <param name="nivel">El nivel de opción del socket.</param>
        /// <param name="nombre">El nombre de la opción.</param>
        /// <param name="valor">El valor booleano de la opción.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EstablecerOpcion(SocketOptionLevel nivel, SocketOptionName nombre, bool valor)
            => SocketSeguro.SetSocketOption(nivel, nombre, valor);

        /// <summary>
        /// Establece una opción específica del socket físico sin allocations de heap.
        /// </summary>
        /// <param name="nivel">El nivel de opción del socket.</param>
        /// <param name="nombre">El nombre de la opción.</param>
        /// <param name="valor">El valor entero de la opción.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EstablecerOpcion(SocketOptionLevel nivel, SocketOptionName nombre, int valor)
            => SocketSeguro.SetSocketOption(nivel, nombre, valor);

        /// <summary>
        /// Acepta una conexión entrante de forma asíncrona, retornando una nueva estructura SocketAot protegida.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación opcional.</param>
        /// <returns>Una tarea que representa el socket aceptado envuelto en <see cref="SocketAot"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
        public async ValueTask<SocketAot> AceptarAsync(CancellationToken cancellationToken = default)
        {
            var socketAceptado = await SocketSeguro.AcceptAsync(cancellationToken).ConfigureAwait(false);
            return new SocketAot(socketAceptado);
        }

        /// <summary>
        /// Acepta una conexión entrante de forma síncrona, retornando una nueva estructura SocketAot protegida.
        /// </summary>
        /// <returns>El socket aceptado envuelto en <see cref="SocketAot"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SocketAot Aceptar()
        {
            var socketAceptado = SocketSeguro.Accept();
            return new SocketAot(socketAceptado);
        }

        #endregion

        #region Bucles de Ejecución Genéricos y Orquestadores de Flujo

        /// <summary>
        /// Ejecuta el bucle de recepción continua de datos (Stream) optimizado para TCP.
        /// </summary>
        /// <typeparam name="TFlujo">El tipo del flujo que maneja la lógica de recepción.</typeparam>
        /// <param name="flujo">La instancia del flujo.</param>
        /// <param name="cancellationToken">Token de cancelación opcional.</param>
        /// <returns>Una tarea que representa la ejecución del bucle.</returns>
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        public async ValueTask EjecutarFlujoSecuenciaAsync<TFlujo>(TFlujo flujo, CancellationToken cancellationToken = default)
            where TFlujo : IFlujoSecuenciaSocket
        {
            try
            {
                flujo.AlConfigurar(SocketSeguro);

                using var buffer = new PooledArray<byte>(flujo.TamañoBuffer);
                while (!cancellationToken.IsCancellationRequested)
                {
                    int bytesRead = await SocketSeguro.ReceiveAsync(buffer.Memory, SocketFlags.None, cancellationToken).ConfigureAwait(false);

                    if (bytesRead <= 0)
                    {
                        flujo.AlCompletar();
                        break;
                    }

                    await flujo.AlRecibirAsync(buffer.Memory[..bytesRead], cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Cancelación normal
            }
            catch (Exception ex)
            {
                flujo.AlOcurrirError(ex);
            }
            finally
            {
                Cerrar();
            }
        }

        /// <summary>
        /// Conecta el socket al extremo remoto y delega la ejecución del flujo continuo de datos de forma segura.
        /// </summary>
        /// <typeparam name="TFlujo">El tipo del flujo cliente.</typeparam>
        /// <param name="flujo">La instancia del flujo cliente.</param>
        /// <param name="cancellationToken">Token de cancelación opcional.</param>
        /// <returns>Una tarea que representa la ejecución del bucle cliente.</returns>
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        public async ValueTask EjecutarFlujoClienteSecuenciaAsync<TFlujo>(TFlujo flujo, CancellationToken cancellationToken = default)
            where TFlujo : IFlujoClienteSecuenciaSocket
        {
            try
            {
                flujo.AlConfigurar(SocketSeguro);
                await SocketSeguro.ConnectAsync(flujo.ExtremoRemoto, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                flujo.AlOcurrirError(ex);
                Cerrar();
                throw;
            }

            await EjecutarFlujoSecuenciaSiguienteAsync(flujo, cancellationToken).ConfigureAwait(false);
        }

        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private async ValueTask EjecutarFlujoSecuenciaSiguienteAsync<TFlujo>(TFlujo flujo, CancellationToken cancellationToken)
            where TFlujo : IFlujoSecuenciaSocket
        {
            try
            {
                using var buffer = new PooledArray<byte>(flujo.TamañoBuffer);
                while (!cancellationToken.IsCancellationRequested)
                {
                    int bytesRead = await SocketSeguro.ReceiveAsync(buffer.Memory, SocketFlags.None, cancellationToken).ConfigureAwait(false);

                    if (bytesRead <= 0)
                    {
                        flujo.AlCompletar();
                        break;
                    }

                    await flujo.AlRecibirAsync(buffer.Memory[..bytesRead], cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Cancelación normal
            }
            catch (Exception ex)
            {
                flujo.AlOcurrirError(ex);
            }
            finally
            {
                Cerrar();
            }
        }

        /// <summary>
        /// Ejecuta el bucle de recepción continua de datagramas optimizado para UDP.
        /// </summary>
        /// <typeparam name="TFlujo">El tipo del flujo de datagramas.</typeparam>
        /// <param name="flujo">La instancia del flujo de datagramas.</param>
        /// <param name="cancellationToken">Token de cancelación opcional.</param>
        /// <returns>Una tarea que representa la ejecución del bucle de datagramas.</returns>
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        public async ValueTask EjecutarFlujoDatagramaAsync<TFlujo>(TFlujo flujo, CancellationToken cancellationToken = default)
            where TFlujo : IFlujoDatagramaSocket
        {
            try
            {
                SocketSeguro.Bind(flujo.ExtremoLocal);
                flujo.AlConfigurar(SocketSeguro);

                EndPoint extremoRemoto = SocketSeguro.AddressFamily == AddressFamily.InterNetwork
                    ? new IPEndPoint(IPAddress.Any, 0)
                    : new IPEndPoint(IPAddress.IPv6Any, 0);

                using var buffer = new PooledArray<byte>(flujo.TamañoBuffer);
                while (!cancellationToken.IsCancellationRequested)
                {
                    SocketReceiveFromResult result = await SocketSeguro.ReceiveFromAsync(buffer.Memory, SocketFlags.None, extremoRemoto, cancellationToken).ConfigureAwait(false);

                    if (result.ReceivedBytes > 0)
                        await flujo.AlRecibirDatagramaAsync(buffer.Memory[..result.ReceivedBytes], result.RemoteEndPoint, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Cancelación normal
            }
            catch (Exception ex)
            {
                flujo.AlOcurrirError(ex);
            }
            finally
            {
                Cerrar();
            }
        }

        /// <summary>
        /// Ejecuta el bucle de escucha y aceptación continua de conexiones entrantes (TCP Listener).
        /// </summary>
        /// <typeparam name="TFlujo">El tipo del flujo de aceptación.</typeparam>
        /// <param name="flujo">La instancia del flujo de aceptación.</param>
        /// <param name="cancellationToken">Token de cancelación opcional.</param>
        /// <returns>Una tarea que representa la ejecución de la aceptación de conexiones.</returns>
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        public async ValueTask EjecutarFlujoAceptarAsync<TFlujo>(TFlujo flujo, CancellationToken cancellationToken = default)
            where TFlujo : IFlujoAceptarSocket
        {
            try
            {
                SocketSeguro.Bind(flujo.ExtremoLocal);
                flujo.AlConfigurarEscucha(SocketSeguro);
                SocketSeguro.Listen(flujo.ColaConexiones);

                while (!cancellationToken.IsCancellationRequested)
                {
                    Socket socketAceptado = await SocketSeguro.AcceptAsync(cancellationToken).ConfigureAwait(false);

                    try
                    {
                        flujo.AlConfigurarAceptado(socketAceptado);
                        var socketClienteAot = new SocketAot(socketAceptado);
                        
                        _ = EjecutarSocketAceptadoAsync(flujo, socketClienteAot, cancellationToken);
                    }
                    catch (Exception clientEx)
                    {
                        socketAceptado.Dispose();
                        flujo.AlOcurrirError(clientEx);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Cancelación normal
            }
            catch (Exception ex)
            {
                flujo.AlOcurrirError(ex);
            }
            finally
            {
                Cerrar();
            }
        }

        private static async Task EjecutarSocketAceptadoAsync<TFlujo>(TFlujo flujo, SocketAot socketAceptado, CancellationToken cancellationToken)
            where TFlujo : IFlujoAceptarSocket
        {
            try
            {
                await flujo.AlAceptarConexionAsync(socketAceptado, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                socketAceptado.Dispose();
                flujo.AlOcurrirError(ex);
            }
        }

        #endregion
    }
}
