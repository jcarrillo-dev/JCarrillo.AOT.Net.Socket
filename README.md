# JCarrillo.AOT.Net.Socket

`JCarrillo.AOT.Net.Socket` es una biblioteca de sockets de red de bajo nivel y alto rendimiento para **.NET 8, 9 y 10**. Ha sido diseñada bajo un paradigma estricto de **cero asignaciones (Zero-Allocation)** en memoria y optimizada exhaustivamente para la compilación **Native AOT** (Ahead-Of-Time) y el recorte de código (**Trimming**).

Esta biblioteca proporciona una envoltura inmutable y segura que actúa como un guardián de rendimiento, aislando al desarrollador del uso incorrecto o ineficiente de las API tradicionales de sockets de .NET.

---

## El Wrapper SocketAot (API Guardian)

El tipo principal de la biblioteca es `SocketAot`, una estructura de solo lectura (`readonly struct`) que encapsula el `System.Net.Sockets.Socket` estándar del sistema. 

### Barrera en Tiempo de Compilación
Para garantizar el máximo rendimiento y evitar allocations accidentales en rutas calientes de ejecución, `SocketAot` funciona como un guardián rígido:
- **Visibilidad `internal` del Socket nativo**: La propiedad `SocketSubyacente` que expone el socket físico de .NET está marcada como `internal`. Esto impide que los consumidores del paquete realicen llamadas directas no compatibles con AOT o que generen asignaciones implícitas en el montón (heap).
- **Inlining Agresivo**: Las propiedades y métodos de consulta críticos (como `Conectado`, `ExtremoLocal`, `ExtremoRemoto`, `NoDelay`, etc.) están decorados con `[MethodImpl(MethodImplOptions.AggressiveInlining)]` para eliminar la sobrecarga de llamada de método, permitiendo al compilador sustituir la invocación directamente por el acceso al miembro interno.
- **Firma Limpia con `SocketFlags` Opcionales**: Las firmas de lectura y escritura exponen sobrecargas optimizadas que permiten pasar `SocketFlags` explístos de forma opcional (por defecto `SocketFlags.None`), ofreciendo total control sobre el comportamiento de transmisión de red de bajo nivel.

---

## Interfaces y Orquestación de Flujos de Red

La biblioteca promueve una arquitectura basada en **flujos de datos orientados a eventos** mediante interfaces desacopladas. En lugar de gestionar manualmente subprocesos o bucles de recepción asíncronos complejos, el desarrollador implementa interfaces de flujo y delega la ejecución en los orquestadores de `SocketAot`.

### Interfaces de Flujo

*   **`IFlujoSecuenciaSocket`**: Abstracción base para flujos de datos continuos orientados a conexión (TCP). Define el tamaño de buffer deseado, la inicialización del socket y los manejadores de recepción de datos, errores y cierre.
*   **`IFlujoClienteSecuenciaSocket`**: Extiende `IFlujoSecuenciaSocket` añadiendo la propiedad `ExtremoRemoto`, permitiendo orquestar flujos de conexión automáticos en clientes TCP.
*   **`IFlujoDatagramaSocket`**: Abstracción para sockets orientados a datagramas sin conexión (UDP). Orquesta la recepción desde múltiples orígenes a través de un endpoint local vinculado.
*   **`IFlujoAceptarSocket`**: Abstracción para sockets servidores de escucha (TCP Listener) encargados de aceptar conexiones entrantes de clientes.

### Bucles de Red Optimizados

`SocketAot` implementa motores de bucles de red internos asíncronos y altamente optimizados. Estos métodos ocultan la complejidad de la recepción y la gestión de memoria intermedia de manera transparente:

-   **`EjecutarFlujoAceptarAsync<TFlujo>(TFlujo flujo)`**: Enlaza el puerto local, configura el socket del listener y el de los clientes aceptados, y acepta conexiones concurrentes delegándolas al manejador.
-   **`EjecutarFlujoSecuenciaAsync<TFlujo>(TFlujo flujo)`**: Orquesta el ciclo de lectura continua de bytes de un socket TCP entrante, notificando los fragmentos leídos y cerrando el socket al finalizar.
-   **`EjecutarFlujoClienteSecuenciaAsync<TFlujo>(TFlujo flujo)`**: Realiza la conexión activa al host remoto y seguidamente inicia el bucle continuo de recepción de datos.
-   **`EjecutarFlujoDatagramaAsync<TFlujo>(TFlujo flujo)`**: Levanta el puerto UDP de escucha local y procesa continuamente la recepción de datagramas individuales junto a sus metadatos de dirección remota origen.

---

## Ejemplos Prácticos de Implementación

A continuación se presentan ejemplos de código semánticamente correctos respecto a las interfaces de la biblioteca.

### 1. Servidor TCP (IFlujoAceptarSocket)

Representa el socket oyente del servidor que gestiona y delega la aceptación de cada cliente entrante.

```csharp
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using JCarrillo.AOT.Net.Socket;
using JCarrillo.AOT.Net.Socket.Interfaces;

public sealed class ServidorTcpFlujo(EndPoint extremoLocal) : IFlujoAceptarSocket
{
    public EndPoint ExtremoLocal { get; } = extremoLocal;
    public int ColaConexiones => 100; // Capacidad del backlog

    public void AlConfigurarEscucha(Socket socketEscucha)
    {
        // Configuración de bajo nivel del socket oyente
        socketEscucha.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
    }

    public void AlConfigurarAceptado(Socket socketAceptado)
    {
        // Optimización del socket cliente aceptado
        socketAceptado.NoDelay = true;
    }

    public async ValueTask AlAceptarConexionAsync(SocketAot socketAceptado, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[Servidor] Cliente conectado desde: {socketAceptado.ExtremoRemoto}");

        // Delegamos el ciclo de vida del cliente a un flujo de secuencia de conexión individual
        var manejadorConexion = new ManejadorConexionFlujo();
        
        // Ejecutamos el bucle asíncrono optimizado de forma estructurada e independiente
        await socketAceptado.EjecutarFlujoSecuenciaAsync(manejadorConexion, cancellationToken).ConfigureAwait(false);
    }

    public void AlOcurrirError(Exception excepcion)
    {
        Console.WriteLine($"[Servidor] Error crítico: {excepcion.Message}");
    }
}
```

### 2. Manejador de Conexión TCP (IFlujoSecuenciaSocket)

Controla la recepción continua de bytes de una conexión individual (utilizado tanto en el servidor tras aceptar un cliente, como de forma directa).

```csharp
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using JCarrillo.AOT.Net.Socket.Interfaces;

public sealed class ManejadorConexionFlujo : IFlujoSecuenciaSocket
{
    public int TamañoBuffer => 4096; // Buffer de recepción recomendado de 4KB

    public void AlConfigurar(Socket socket)
    {
        // Configuración inicial opcional de la conexión física
    }

    public ValueTask AlRecibirAsync(ReadOnlyMemory<byte> datos, CancellationToken cancellationToken)
    {
        // ADVERTENCIA: Los datos recibidos residen en memoria rentada de un pool intermedio.
        // NO guarde referencias directas de 'datos' fuera del ámbito de esta llamada asíncrona.
        Console.WriteLine($"[Conexión] Recibidos {datos.Length} bytes.");
        
        // Procesar los datos (ej: Parsear protocolo de aplicación, decodificar mensajes...)
        return ValueTask.CompletedTask;
    }

    public void AlOcurrirError(Exception excepcion)
    {
        Console.WriteLine($"[Conexión] Error detectado: {excepcion.Message}");
    }

    public void AlCompletar()
    {
        Console.WriteLine("[Conexión] El extremo remoto cerró la conexión ordenadamente.");
    }
}
```

### 3. Cliente TCP (IFlujoClienteSecuenciaSocket)

Maneja la conexión activa contra un servidor remoto e implementa la lectura secuencial de la respuesta.

```csharp
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using JCarrillo.AOT.Net.Socket.Interfaces;

public sealed class ClienteTcpFlujo(EndPoint extremoRemoto) : IFlujoClienteSecuenciaSocket
{
    public EndPoint ExtremoRemoto { get; } = extremoRemoto;
    public int TamañoBuffer => 4096;

    public void AlConfigurar(Socket socket)
    {
        socket.NoDelay = true;
    }

    public ValueTask AlRecibirAsync(ReadOnlyMemory<byte> datos, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[Cliente] Recibidos {datos.Length} bytes desde el servidor.");
        return ValueTask.CompletedTask;
    }

    public void AlOcurrirError(Exception excepcion)
    {
        Console.WriteLine($"[Cliente] Error en el flujo del cliente: {excepcion.Message}");
    }

    public void AlCompletar()
    {
        Console.WriteLine("[Cliente] Servidor desconectado.");
    }
}
```

### 4. Receptor UDP (IFlujoDatagramaSocket)

Escucha puertos UDP locales de manera no orientada a conexión para la recepción asíncrona de datagramas independientes.

```csharp
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using JCarrillo.AOT.Net.Socket.Interfaces;

public sealed class ReceptorUdpFlujo(EndPoint extremoLocal) : IFlujoDatagramaSocket
{
    public EndPoint ExtremoLocal { get; } = extremoLocal;
    public int TamañoBuffer => 2048; // Adecuado para contener MTUs estándar

    public void AlConfigurar(Socket socket)
    {
        // Configuraciones específicas de UDP (ej: reutilización de puertos, buffers del OS)
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
    }

    public ValueTask AlRecibirDatagramaAsync(ReadOnlyMemory<byte> datos, EndPoint extremoRemoto, CancellationToken cancellationToken)
    {
        // ADVERTENCIA: Los datos recibidos residen en memoria rentada de un pool intermedio.
        // NO retenga referencias directas de 'datos' fuera del retorno de este método.
        Console.WriteLine($"[UDP] Recibido datagrama de {datos.Length} bytes desde {extremoRemoto}");
        return ValueTask.CompletedTask;
    }

    public void AlOcurrirError(Exception excepcion)
    {
        Console.WriteLine($"[UDP] Error crítico en socket UDP: {excepcion.Message}");
    }
}
```

### Inicialización y Arranque de los Flujos

Para ejecutar cualquiera de los flujos de red anteriores de forma segura y eficiente:

```csharp
using System.Net;
using System.Net.Sockets;
using System.Threading;
using JCarrillo.AOT.Net.Socket;

// --- Ejemplo Servidor TCP ---
var endPointEscucha = new IPEndPoint(IPAddress.Any, 8080);
using var socketServidor = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
var socketServidorAot = new SocketAot(socketServidor);
var flujoServidor = new ServidorTcpFlujo(endPointEscucha);

// Arranca el bucle infinito de aceptación de conexiones
await socketServidorAot.EjecutarFlujoAceptarAsync(flujoServidor, cancellationToken);

// --- Ejemplo Cliente TCP ---
var endPointRemoto = new IPEndPoint(IPAddress.Loopback, 8080);
using var socketCliente = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
var socketClienteAot = new SocketAot(socketCliente);
var flujoCliente = new ClienteTcpFlujo(endPointRemoto);

// Se conecta e inicia la recepción continua
await socketClienteAot.EjecutarFlujoClienteSecuenciaAsync(flujoCliente, cancellationToken);
```

---

## Principio de Zero-Allocation y Native AOT

La meta fundamental de esta biblioteca es eliminar por completo la asignación de memoria dinámica en el montón (Heap Allocations) durante la comunicación de red activa. Esto reduce a cero el trabajo del Recolector de Basura (Garbage Collector), evitando pausas no deseadas (*GC pauses*) en entornos de alta concurrencia.

### 1. Pooling de Tareas Asíncronas (`PoolingAsyncValueTaskMethodBuilder`)
C# normalmente asigna objetos `Task` o envoltorios de máquinas de estados asíncronas al usar `async/await`. 
`SocketAot` sortea esta limitación decorando todos sus métodos asíncronos principales de flujo con:
```csharp
[AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
```
Esta anotación le indica al compilador de C# que utilice un constructor de métodos asíncronos basado en un pool interno de estructuras reutilizables para los estados de `ValueTask`, logrando que las llamadas asíncronas no asignen memoria si se completan síncronamente o si sus constructores de máquina de estado son reutilizados de manera eficiente.

### 2. Uso del Pool de Buffers (`PooledArray` y `PooledArrayRef`)
La asignación y liberación constante de arrays de bytes (`byte[]`) para operaciones de lectura/escritura es una de las mayores fuentes de fragmentación de memoria en red. `JCarrillo.AOT.Net.Socket` mitiga esto utilizando la infraestructura de arrays rentados de alto rendimiento expuesta en `JCarrillo.AOT.Core`:

*   **`PooledArray<byte>`**: Array de bytes alquilado a partir de un pool optimizado global. Expone una propiedad `.Memory` (`Memory<byte>`) ideal para operaciones de socket asíncronas.
*   **`PooledArrayRef<byte>`**: Una estructura por referencia (`ref struct`) optimizada para allocations eficientes en la pila (Stack-Allocated / Thread-Safe). Expone una propiedad `.Span` (`Span<byte>`) perfecta para lecturas/escrituras síncronas de bajísima latencia.

#### Liberación Obligatoria de Memoria (Dispose)
> [!IMPORTANT]
> Dado que ambos tipos (`PooledArray<T>` y `PooledArrayRef<T>`) alquilan memoria subyacente que debe retornar al pool para ser reutilizada, **es de carácter crítico invocar a `.Dispose()`** o utilizar la instrucción `using` al instanciarlos. De no hacerlo, se producirá una **fuga de buffers (buffer leak)** que agotará rápidamente la memoria disponible en el pool.

Ejemplo de uso correcto de envío manual de datos utilizando buffer pooling:

```csharp
using JCarrillo.AOT.Core.Colecciones.Pooled;
using JCarrillo.AOT.Net.Socket;

public async ValueTask EnviarMensajeManualAsync(SocketAot socketAot, CancellationToken cancellationToken)
{
    // Alquilamos un buffer de 1024 bytes del pool optimizado
    using var bufferRentado = new PooledArray<byte>(1024);
    
    // Escribir los datos en el Span expuesto por el buffer rentado
    bufferRentado.Span[0] = 0xAA; // Byte de cabecera de ejemplo
    bufferRentado.Span[1] = 0xBB;
    
    // El método EnviarAsync por referencia evita allocations y copias adicionales
    await socketAot.EnviarAsync(ref bufferRentado, cancellationToken);
    
    // Al salir del método, el bloque "using" invoca automáticamente a bufferRentado.Dispose()
    // devolviendo la memoria física al pool sin costes para el GC.
}
```

De igual forma, para lecturas de alto rendimiento síncronas que no salen de la pila:

```csharp
using JCarrillo.AOT.Core.Colecciones.Pooled.Ref;
using JCarrillo.AOT.Net.Socket;

public void RecibirMensajeSincronoManual(SocketAot socketAot)
{
    // Usamos la variante ref struct en pila
    using var bufferEnPila = new PooledArrayRef<byte>(256);
    
    // Llenamos el buffer directamente desde el socket
    int bytesRecibidos = socketAot.Recibir(ref bufferEnPila);
    
    if (bytesRecibidos > 0)
    {
        Console.WriteLine($"Datos síncronos leídos: {bytesRecibidos} bytes.");
    }
}
```
