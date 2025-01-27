using System;
using System.IO.Ports;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Text;

namespace winproySerialPort
{
    // 1) Definimos 2 delegados:
    //    - Uno para mensajes de texto
    //    - Otro para avisar que llegó un archivo (metadatos)
    public delegate void HandlerTxRx(object sender, string mensRec);
    public delegate void HandlerTxRxArchivo(object sender, string nombreArchivo, long tamañoArchivo);

    class classTransRecep
    {
        // 2) Definimos los eventos que el Form suscribirá
        public event HandlerTxRx LlegoMensaje;
        public event HandlerTxRxArchivo LlegoArchivo;

        // Modelo para archivos en envío/recepción
        private ClassArchivoEnviando arhivoEnviar;
        private ClassArchivoEnviando arhivoRecibir;

        // Streams para envío
        private FileStream FlujoArchivoEnviar;
        private BinaryReader LeyendoArchivo;

        // Streams para recepción
        private FileStream FlujoArchivoRecibir;
        private BinaryWriter EscribiendoArchivo;

        // Hilos
        Thread procesoEnvio;
        Thread procesoEnvioArchivo;
        Thread procesoVerificaSalida;
        Thread procesoRecibirMensaje;

        // Puerto serie
        private SerialPort puerto;

        // Buffers y estados
        private string mensajeEnviar;
        private string mensRecibido;
        private bool BufferSalidaVacio;

        private byte[] TramaEnvio;
        private byte[] TramCabaceraEnvio;
        private byte[] tramaRelleno;
        private byte[] TramaRecibida;

        // Ruta por defecto donde guardar archivos
        private string rutaDescarga = "E:\\PRUEBA\\1\\";

        // lock para escritura concurrente (si decides usarlo)
        private object lockEscritura = new object();

        // --------------------------------------------------------------------
        // Constructor
        // --------------------------------------------------------------------
        public classTransRecep()
        {
            TramaEnvio = new byte[1024];
            TramCabaceraEnvio = new byte[5];
            tramaRelleno = new byte[1024];
            TramaRecibida = new byte[1024];

            // Rellenamos el buffer de relleno con '@'
            for (int i = 0; i < 1024; i++)
            {
                tramaRelleno[i] = 64; // caracter ASCII '@'
            }

            arhivoEnviar = new ClassArchivoEnviando();
            arhivoRecibir = new ClassArchivoEnviando();
        }

        // --------------------------------------------------------------------
        // Getters/Setters para rutaDescarga
        // --------------------------------------------------------------------
        public string getRutaDescarga()
        {
            return rutaDescarga;
        }

        public void modificarRutaDescarga(string nuevaRuta)
        {
            rutaDescarga = nuevaRuta;
        }

        // --------------------------------------------------------------------
        // Inicialización del puerto serie
        // --------------------------------------------------------------------
        public void Inicializa(string NombrePuerto)
        {
            puerto = new SerialPort(NombrePuerto, 57600, Parity.Even, 8, StopBits.Two);
            puerto.ReceivedBytesThreshold = 1024;
            puerto.DataReceived += puerto_DataReceived;
            puerto.Open();

            BufferSalidaVacio = true;

            // Hilo que verifica el buffer de salida (opcional)
            procesoVerificaSalida = new Thread(VerificandoSalida);
            procesoVerificaSalida.IsBackground = true;
            procesoVerificaSalida.Start();

            MessageBox.Show("Puerto abierto: " + puerto.PortName);
        }

        // --------------------------------------------------------------------
        // Evento DataReceived: Se dispara cuando hay >= 1024 bytes disponibles
        // --------------------------------------------------------------------
        private void puerto_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (puerto.BytesToRead >= 1024)
            {
                // Leemos 1024 bytes en TramaRecibida
                puerto.Read(TramaRecibida, 0, 1024);

                // Primer byte indica la tarea (M, D, A, etc.)
                string TAREA = Encoding.UTF8.GetString(TramaRecibida, 0, 1);

                switch (TAREA)
                {
                    case "M":
                        // Mensaje de texto
                        procesoRecibirMensaje = new Thread(RecibiendoMensaje);
                        procesoRecibirMensaje.IsBackground = true;
                        procesoRecibirMensaje.Start();
                        break;

                    case "D":
                        // Metadatos de archivo
                        // Inicia la construcción del archivo O
                        // ... o disparamos un evento LlegoArchivo
                        RecibirMetadatosArchivo();
                        break;

                    case "A":
                        // Bloque de archivo
                        ConstruirArchivo();
                        break;

                    default:
                        // "I"? "trama no reconocida"? etc.
                        // Dejar como sea conveniente
                        break;
                }
            }
        }

        // --------------------------------------------------------------------
        // Procesar un mensaje de texto
        // --------------------------------------------------------------------
        private void RecibiendoMensaje()
        {
            // Cabecera = [1..4] -> longitud en decimal
            string CabRec = Encoding.UTF8.GetString(TramaRecibida, 1, 4);
            int LongMensRec = Convert.ToInt16(CabRec);

            // Leemos el texto [5..(5+LongMensRec)]
            mensRecibido = Encoding.UTF8.GetString(TramaRecibida, 5, LongMensRec);

            // Lanzamos el evento
            OnLlegoMensaje();
        }

        protected virtual void OnLlegoMensaje()
        {
            if (LlegoMensaje != null)
                LlegoMensaje(this, mensRecibido);
        }

        // --------------------------------------------------------------------
        // Procesar metadatos de archivo ("D")
        // Ej: "nombreArchivo-tamaño"
        // --------------------------------------------------------------------
        private void RecibirMetadatosArchivo()
        {
            // Leemos 4 bytes de longitud
            string strLong = Encoding.UTF8.GetString(TramaRecibida, 1, 4);
            int longDato = Convert.ToInt16(strLong);

            // Leemos la parte real [5..5+longDato]
            string metadatos = Encoding.UTF8.GetString(TramaRecibida, 5, longDato);

            // metadatos -> "nombreArchivo-tamaño"
            string[] partes = metadatos.Split('-');
            string nombre = partes[0];
            long tam = long.Parse(partes[1]);

            // Disparamos el evento LlegoArchivo -> que el Form decida si hace un SaveFileDialog
            OnLlegoArchivo(nombre, tam);
        }

        protected virtual void OnLlegoArchivo(string nombreArchivo, long tamaño)
        {
            if (LlegoArchivo != null)
                LlegoArchivo(this, nombreArchivo, tamaño);
        }

        // --------------------------------------------------------------------
        // Método para construir el archivo cuando llegan bloques "A"
        // --------------------------------------------------------------------
        private void ConstruirArchivo()
        {
            if (arhivoRecibir.Avance <= arhivoRecibir.Tamaño - 1019)
            {
                // Escribimos 1019 bytes
                EscribiendoArchivo.Write(TramaRecibida, 5, 1019);
                arhivoRecibir.Avance += 1019;
            }
            else
            {
                // Último bloque
                int tamanito = (int)(arhivoRecibir.Tamaño - arhivoRecibir.Avance);
                EscribiendoArchivo.Write(TramaRecibida, 5, tamanito);

                // Cerramos
                EscribiendoArchivo.Close();
                FlujoArchivoRecibir.Close();

                MessageBox.Show("Archivo recibido: " + arhivoRecibir.Nombre);
            }
        }

        // --------------------------------------------------------------------
        // El form (o quien sea) llamará a este cuando decida "crear" el archivo
        // --------------------------------------------------------------------
        public void InicioConstruirArchivo(string rutaDestino, string nombreArchivo, long tamArchivo)
        {
            FlujoArchivoRecibir = new FileStream(rutaDestino, FileMode.Create, FileAccess.Write);
            EscribiendoArchivo = new BinaryWriter(FlujoArchivoRecibir);

            arhivoRecibir.Nombre = nombreArchivo;
            arhivoRecibir.Tamaño = tamArchivo;
            arhivoRecibir.Avance = 0;
        }

        // --------------------------------------------------------------------
        // Enviar un mensaje
        // --------------------------------------------------------------------
        public void Enviar(string mensaje, string tipo = "M")
        {
            if (mensaje.Length > 1019)
            {
                MessageBox.Show("Mensaje demasiado largo (máx 1019 caracteres)");
                return;
            }

            mensajeEnviar = mensaje;

            byte[] bMensaje = Encoding.UTF8.GetBytes(mensajeEnviar);
            // Cabecera: (tipo) + (longitud de 4 dígitos)
            string sCab = tipo + bMensaje.Length.ToString("D4");
            TramCabaceraEnvio = Encoding.UTF8.GetBytes(sCab);

            // Guardamos la parte real
            TramaEnvio = bMensaje;

            // Lanzamos un hilo para enviar
            procesoEnvio = new Thread(EnviandoMensaje);
            procesoEnvio.IsBackground = true;
            procesoEnvio.Start();
        }

        private void EnviandoMensaje()
        {
            // lock(lockEscritura) si deseas sincronizar
            lock (lockEscritura)
            {
                // Escribimos la cabecera (5 bytes)
                puerto.Write(TramCabaceraEnvio, 0, 5);

                // Escribimos el contenido
                puerto.Write(TramaEnvio, 0, TramaEnvio.Length);

                // Relleno
                int faltan = 1019 - TramaEnvio.Length;
                if (faltan > 0)
                {
                    puerto.Write(tramaRelleno, 0, faltan);
                }
            }
        }

        // --------------------------------------------------------------------
        // Inicia envío de archivo (envía metadatos "D" con "nombreArchivo-tamaño")
        // --------------------------------------------------------------------
        public void IniciaEnvioArchivo(string rutaArchivo)
        {
            FlujoArchivoEnviar = new FileStream(rutaArchivo, FileMode.Open, FileAccess.Read);
            LeyendoArchivo = new BinaryReader(FlujoArchivoEnviar);

            arhivoEnviar.Ruta = rutaArchivo;
            arhivoEnviar.Tamaño = FlujoArchivoEnviar.Length;
            arhivoEnviar.Avance = 0;
            arhivoEnviar.Num = 1;

            // Obtenemos el nombre (para metadatos)
            int idx = rutaArchivo.LastIndexOf('\\');
            string nombreFile = rutaArchivo.Substring(idx + 1);
            arhivoEnviar.Nombre = nombreFile;

            // Preparamos "nombreArchivo-tamaño"
            string metadatos = nombreFile + "-" + arhivoEnviar.Tamaño;

            // Enviamos la trama de metadatos, tipo "D"
            Enviar(metadatos, "D");

            // Iniciamos hilo para mandar los bloques (tipo "A")
            procesoEnvioArchivo = new Thread(EnviandoArchivo);
            procesoEnvioArchivo.IsBackground = true;
            procesoEnvioArchivo.Start();
        }

        // --------------------------------------------------------------------
        // Envía los bloques "A" (1019 bytes cada uno, con relleno al final)
        // --------------------------------------------------------------------
        private void EnviandoArchivo()
        {
            // Cabecera: "A" + "1019" (por ejemplo) -> "A1019"
            // o puedes usar "A" + la longitud real del trozo. Aquí usaremos 1019 
            byte[] cab = Encoding.UTF8.GetBytes("A1019");

            byte[] buffer = new byte[1019];
            int leidos = 0;
            long total = arhivoEnviar.Tamaño;

            while (arhivoEnviar.Avance <= total - 1019)
            {
                // Leer 1019
                leidos = LeyendoArchivo.Read(buffer, 0, 1019);
                arhivoEnviar.Avance += leidos;

                lock (lockEscritura)
                {
                    // Espera (opcional) si usas BufferSalidaVacio
                    while (!BufferSalidaVacio) { /* busy wait? */ }

                    puerto.Write(cab, 0, 5);
                    puerto.Write(buffer, 0, leidos);
                }
            }

            // Último bloque
            int faltan = (int)(total - arhivoEnviar.Avance);
            if (faltan > 0)
            {
                byte[] ultimo = new byte[faltan];
                LeyendoArchivo.Read(ultimo, 0, faltan);
                arhivoEnviar.Avance += faltan;

                // Cabecera con la longitud real: p.ej. "A00XX"
                string sLong = faltan.ToString("D4");
                byte[] cabFinal = Encoding.UTF8.GetBytes("A" + sLong);

                lock (lockEscritura)
                {
                    while (!BufferSalidaVacio) { /* busy wait? */ }

                    puerto.Write(cabFinal, 0, 5);
                    puerto.Write(ultimo, 0, faltan);

                    // Relleno
                    int rell = 1019 - faltan;
                    if (rell > 0) puerto.Write(tramaRelleno, 0, rell);
                }
            }

            LeyendoArchivo.Close();
            FlujoArchivoEnviar.Close();

            MessageBox.Show("Archivo enviado: " + arhivoEnviar.Nombre);
        }

        // --------------------------------------------------------------------
        // Hilo de verificación del buffer de salida
        // --------------------------------------------------------------------
        private void VerificandoSalida()
        {
            while (true)
            {
                if (puerto.BytesToWrite > 0)
                    BufferSalidaVacio = false;
                else
                    BufferSalidaVacio = true;

                // Para no saturar CPU
                Thread.Sleep(10);
            }
        }
    }
}
