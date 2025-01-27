using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace winproySerialPort
{
    class classTransRecep
    {
        // Delegado y evento para avisar llegada de mensajes de texto
        public delegate void HandlerTxRx(object oo, string mensRec);
        public event HandlerTxRx LlegoMensaje;

        // Información sobre archivos (enviando / recibiendo)
        private ClassArchivoEnviando archivoEnviar;
        private ClassArchivoEnviando archivoRecibir;

        // Streams para envío
        private FileStream FlujoArchivoEnviar;
        private BinaryReader LectorArchivo;

        // Streams para recepción
        private FileStream FlujoArchivoRecibir;
        private BinaryWriter EscritorArchivo;

        // Puerto serie
        private SerialPort puerto;

        // Buffer para recibir 1024 bytes
        private byte[] TramaRecibida;

        // Ruta donde se guardarán los archivos recibidos
        private string rutaDescarga = "E:\\PRUEBA\\1\\";

        // ----------------------------------------------------------------------------
        // A) Cola y sincronización para el envío (un solo hilo escritor)
        // ----------------------------------------------------------------------------
        private ConcurrentQueue<byte[]> colaTramas;    // Cola hilo-segura con las tramas a enviar
        private AutoResetEvent hayTrama;               // Evento para avisar al hiloTx que hay algo en cola
        private Thread hiloTx;                         // Hilo escritor que atiende la cola

        // ----------------------------------------------------------------------------
        // Constructor
        // ----------------------------------------------------------------------------
        public classTransRecep()
        {
            // Preparamos buffers
            TramaRecibida = new byte[1024];

            // Instancia de objetos para envío/recepción de archivos
            archivoEnviar = new ClassArchivoEnviando();
            archivoRecibir = new ClassArchivoEnviando();

            // Inicializamos la cola y el evento
            colaTramas = new ConcurrentQueue<byte[]>();
            hayTrama = new AutoResetEvent(false);
        }

        // ----------------------------------------------------------------------------
        // Métodos para la ruta de descarga
        // ----------------------------------------------------------------------------
        public string getRutaDescarga()
        {
            return rutaDescarga;
        }

        public void modificarRutaDescarga(string nuevaRuta)
        {
            rutaDescarga = nuevaRuta;
        }

        // ----------------------------------------------------------------------------
        // Inicialización del puerto serie y arranque del hilo escritor
        // ----------------------------------------------------------------------------
        public void Inicializa(string NombrePuerto)
        {
            // Configuramos y abrimos el puerto serie
            puerto = new SerialPort(NombrePuerto, 57600, Parity.Even, 8, StopBits.Two);
            puerto.ReceivedBytesThreshold = 1024;
            puerto.DataReceived += new SerialDataReceivedEventHandler(puerto_DataReceived);
            puerto.Open();

            // Iniciamos el hilo escritor (encargado de consumir la colaTramas)
            IniciarHiloEscritura();

            MessageBox.Show("Puerto abierto: " + puerto.PortName);
        }

        // ----------------------------------------------------------------------------
        // Hilo escritor que consume las tramas de la cola y las envía por el puerto
        // ----------------------------------------------------------------------------
        private void IniciarHiloEscritura()
        {
            hiloTx = new Thread(() =>
            {
                while (true)
                {
                    // Esperar hasta que haya al menos una trama en la cola
                    hayTrama.WaitOne();

                    // Extraer todas las tramas disponibles y enviarlas
                    while (colaTramas.TryDequeue(out byte[] tramaAEnviar))
                    {
                        // Enviamos la trama de 1024 bytes de manera atómica
                        // Usar un lock (puerto) si deseas mayor seguridad, aunque normalmente no colisionará
                        puerto.Write(tramaAEnviar, 0, tramaAEnviar.Length);
                    }
                }
            });

            hiloTx.IsBackground = true;
            hiloTx.Start();
        }

        // ----------------------------------------------------------------------------
        // Evento de recepción de datos (1024 bytes)
        // ----------------------------------------------------------------------------
        private void puerto_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // Verificamos que haya suficientes bytes
            if (puerto.BytesToRead >= 1024)
            {
                // Leemos la trama de 1024 bytes
                puerto.Read(TramaRecibida, 0, 1024);

                // Primer byte indica la tarea
                string TAREA = Encoding.UTF8.GetString(TramaRecibida, 0, 1);

                // Procesamos según el tipo
                switch (TAREA)
                {
                    case "M":
                        // Mensaje de texto
                        RecibiendoMensaje();
                        break;

                    case "D":
                        // Metadatos de archivo (nombre-tamaño)
                        RecibirMetadatosArchivo();
                        break;

                    case "A":
                        // Bloque de datos de archivo
                        ConstruirArchivo();
                        break;

                    case "I":
                        // Podrías usarlo para otras funciones
                        break;

                    default:
                        MessageBox.Show("Trama no reconocida");
                        break;
                }
            }
        }

        // ----------------------------------------------------------------------------
        // Recepción de mensaje de texto (M)
        // ----------------------------------------------------------------------------
        private void RecibiendoMensaje()
        {
            // Extraer longitud (4 bytes: posiciones [1..4])
            string strLong = Encoding.UTF8.GetString(TramaRecibida, 1, 4);
            int longMensaje = Convert.ToInt16(strLong);

            // Extraer el texto (desde posición 5, con esa longitud)
            string textoRecibido = Encoding.UTF8.GetString(TramaRecibida, 5, longMensaje);

            // Disparamos el evento para notificar al Form
            mensRecibido = textoRecibido;
            OnLlegoMensaje();
        }

        private string mensRecibido; // buffer interno para notificar

        protected virtual void OnLlegoMensaje()
        {
            if (LlegoMensaje != null)
                LlegoMensaje(this, mensRecibido);
        }

        // ----------------------------------------------------------------------------
        // Recepción de metadatos de archivo (D)
        // ----------------------------------------------------------------------------
        private void RecibirMetadatosArchivo()
        {
            // Lee la longitud de la trama (4 bytes: posiciones [1..4])
            string strLong = Encoding.UTF8.GetString(TramaRecibida, 1, 4);
            int longitud = Convert.ToInt16(strLong);

            // Metadatos: "nombreArchivo-tamaño"
            string metadatos = Encoding.UTF8.GetString(TramaRecibida, 5, longitud);

            InicioConstruirArchivo(metadatos);
        }

        // ----------------------------------------------------------------------------
        // Preparar el FileStream al recibir metadatos
        // ----------------------------------------------------------------------------
        private void InicioConstruirArchivo(string metadatos)
        {
            string[] partes = metadatos.Split('-');
            string nombre = partes[0];
            string bytes = partes[1];

            FlujoArchivoRecibir = new FileStream(rutaDescarga + nombre, FileMode.Create, FileAccess.Write);
            EscritorArchivo = new BinaryWriter(FlujoArchivoRecibir);

            archivoRecibir.Nombre = nombre;
            archivoRecibir.Num = 1;
            archivoRecibir.Tamaño = long.Parse(bytes);
            archivoRecibir.Avance = 0;
        }

        // ----------------------------------------------------------------------------
        // Recepción de bloques de archivo (A)
        // ----------------------------------------------------------------------------
        private void ConstruirArchivo()
        {
            // Ver si aún faltan >= 1019 bytes para completar
            if (archivoRecibir.Avance <= archivoRecibir.Tamaño - 1019)
            {
                EscritorArchivo.Write(TramaRecibida, 5, 1019);
                archivoRecibir.Avance += 1019;
            }
            else
            {
                // Último bloque
                int tamanito = (int)(archivoRecibir.Tamaño - archivoRecibir.Avance);
                EscritorArchivo.Write(TramaRecibida, 5, tamanito);

                // Cerrar el archivo
                EscritorArchivo.Close();
                FlujoArchivoRecibir.Close();

                MessageBox.Show("Archivo recibido: " + archivoRecibir.Nombre);
            }
        }

        // ----------------------------------------------------------------------------
        // MÉTODOS PÚBLICOS PARA ENVIAR MENSAJES O ARCHIVOS
        // ----------------------------------------------------------------------------

        // Enviar mensaje de texto
        public void Enviar(string mensaje, string tipo = "M")
        {
            if (mensaje.Length > 1019)
            {
                MessageBox.Show("Mensaje demasiado largo (máx 1019 caracteres)");
                return;
            }

            // Construir la trama de 1024 bytes
            byte[] trama = new byte[1024];

            // 1. Primer byte = tipo (M, D, A, etc.)
            byte[] bTipo = Encoding.UTF8.GetBytes(tipo);
            Array.Copy(bTipo, 0, trama, 0, 1);

            // 2. Siguientes 4 bytes = longitud del mensaje en decimal con 4 dígitos
            string strLong = mensaje.Length.ToString("D4"); // Ej: "0012"
            byte[] bLong = Encoding.UTF8.GetBytes(strLong);
            Array.Copy(bLong, 0, trama, 1, 4);

            // 3. Mensaje en bytes
            byte[] bMensaje = Encoding.UTF8.GetBytes(mensaje);
            Array.Copy(bMensaje, 0, trama, 5, bMensaje.Length);

            // (lo que quede en [5..1023] se queda en 0 o relleno si quieres)
            // Por ejemplo, si deseas rellenar con '@':
            // for(int i = 5 + bMensaje.Length; i < 1024; i++) trama[i] = 64; // '@' = 64

            // Encolar la trama y avisar al hiloTx
            EncolarTrama(trama);
        }

        // ----------------------------------------------------------------------------
        // Iniciar el envío de un archivo (lee en un hilo, encola tramas)
        // ----------------------------------------------------------------------------
        public void IniciaEnvioArchivo(string rutaArchivo)
        {
            // Abrimos el archivo en modo lectura
            FlujoArchivoEnviar = new FileStream(rutaArchivo, FileMode.Open, FileAccess.Read);
            LectorArchivo = new BinaryReader(FlujoArchivoEnviar);

            archivoEnviar.Ruta = rutaArchivo;
            archivoEnviar.Tamaño = FlujoArchivoEnviar.Length;
            archivoEnviar.Avance = 0;
            archivoEnviar.Num = 1;

            // Extraemos solo el nombre (ej: "imagen.png")
            int indiceUltimaBarra = rutaArchivo.LastIndexOf('\\');
            string nombreArchivo = rutaArchivo.Substring(indiceUltimaBarra + 1);
            archivoEnviar.Nombre = nombreArchivo;

            // Preparamos metadatos: "nombreArchivo-tamaño"
            string metadatos = nombreArchivo + "-" + FlujoArchivoEnviar.Length;

            // 1) Enviamos primero la trama de metadatos (tipo "D")
            Enviar(metadatos, "D");

            // 2) Lanzamos un hilo para leer y encolar trozos del archivo
            Thread hiloLecturaArchivo = new Thread(() =>
            {
                EnviarBloquesArchivo();
            });
            hiloLecturaArchivo.IsBackground = true;
            hiloLecturaArchivo.Start();
        }

        // ----------------------------------------------------------------------------
        // Lectura en bloques de 1019 bytes y encolado de tramas (tipo "A")
        // ----------------------------------------------------------------------------
        private void EnviarBloquesArchivo()
        {
            // Cada bloque = 1024 bytes totales: 1 byte tipo + 4 bytes longitud + 1019 de datos
            byte[] bufferLectura = new byte[1019];
            int leidos = 0;

            try
            {
                // Mientras queden >= 1019 bytes, enviamos bloque completo
                while (archivoEnviar.Avance <= archivoEnviar.Tamaño - 1019)
                {
                    leidos = LectorArchivo.Read(bufferLectura, 0, 1019);
                    if (leidos <= 0) break; // Fin de archivo

                    archivoEnviar.Avance += leidos;

                    // Armar la trama de 1024 bytes (tipo 'A')
                    byte[] trama = ConstruirTramaArchivo(bufferLectura, leidos);

                    // Encolar
                    EncolarTrama(trama);
                }

                // Enviamos el último bloque (si quedó menos de 1019)
                long restantes = archivoEnviar.Tamaño - archivoEnviar.Avance;
                if (restantes > 0)
                {
                    byte[] ultimos = new byte[restantes];
                    LectorArchivo.Read(ultimos, 0, (int)restantes);
                    archivoEnviar.Avance += restantes;

                    byte[] tramaFinal = ConstruirTramaArchivo(ultimos, (int)restantes);
                    EncolarTrama(tramaFinal);
                }
            }
            finally
            {
                // Cerramos el archivo tras terminar la lectura
                LectorArchivo.Close();
                FlujoArchivoEnviar.Close();
            }

            MessageBox.Show("Archivo enviado: " + archivoEnviar.Nombre);
        }

        // ----------------------------------------------------------------------------
        // Construir la trama (tipo 'A') para un bloque de archivo
        // ----------------------------------------------------------------------------
        private byte[] ConstruirTramaArchivo(byte[] datos, int tam)
        {
            // 1024 = 1 byte (tipo) + 4 bytes (long) + 1019 datos
            byte[] trama = new byte[1024];

            // 1) tipo 'A'
            trama[0] = Encoding.UTF8.GetBytes("A")[0];

            // 2) longitud en 4 dígitos
            //    Ojo: para archivos, a veces se ponen 1019 o 'tam' en la cabecera.
            //    Tu protocolo actual usa 'A' + 4 bytes de longitud = ??? 
            //    Lo habitual sería poner "1019" en la cabecera de cada bloque,
            //    o la longitud real que lleva este bloque. Usaremos la real:
            string strLong = tam.ToString("D4"); // p.ej. "0256"
            byte[] bLong = Encoding.UTF8.GetBytes(strLong);
            Array.Copy(bLong, 0, trama, 1, 4);

            // 3) Copiamos 'tam' bytes de datos a partir de la posición 5
            Array.Copy(datos, 0, trama, 5, tam);

            // 4) (Opcional) Rellenar el resto con '@'
            for (int i = 5 + tam; i < 1024; i++)
            {
                trama[i] = 64; // '@'
            }

            return trama;
        }

        // ----------------------------------------------------------------------------
        // Encolar una trama de 1024 bytes y avisar al hiloTx
        // ----------------------------------------------------------------------------
        private void EncolarTrama(byte[] trama)
        {
            colaTramas.Enqueue(trama);
            hayTrama.Set();
        }
    }
}
