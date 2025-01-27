using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.IO;
using System.Windows.Forms;
using System.Threading;

namespace winproySerialPort
{
    class classTransRecep
    {
        // Delegado y evento para recibir mensajes de texto
        public delegate void HandlerTxRx(object oo, string mensRec);
        public event HandlerTxRx LlegoMensaje;

        // Objetos para envío y recepción de archivos
        private ClassArchivoEnviando arhivoEnviar;
        private FileStream FlujoArchivoEnviar;
        private BinaryReader LeyendoArchivo;

        private ClassArchivoEnviando arhivoRecibir;
        private FileStream FlujoArchivoRecibir;
        private BinaryWriter EscribiendoArchivo;

        // Hilos
        Thread procesoEnvio;          // Para enviar texto
        Thread procesoVerificaSalida; // Verificación del buffer (si se desea conservar)
        Thread procesoRecibirMensaje; // Para procesar mensaje recibido
        Thread procesoEnvioArchivo;   // Para enviar archivos
        Thread procesoConstruyeArchivo; // Aún no utilizado, pero se deja por si se requiere

        // Puerto serie
        private SerialPort puerto;

        // Buffers de texto
        private string mensajeEnviar;
        private string mensRecibido;

        // Ruta de descarga para archivos
        private string rutaDescarga = "E:\\PRUEBA\\1\\";

        // Control de estado de buffer de salida (si deseas mantenerlo, puedes)
        private Boolean BufferSalidaVacio;

        // Arreglos de bytes para envío/recepción
        byte[] TramaEnvio;
        byte[] TramCabaceraEnvio;
        byte[] tramaRelleno;
        byte[] TramaRecibida;

        // ----------------------------------------------------------------------------
        // Objeto lock para sincronizar escritura (podrías usar lock(puerto), pero es 
        // más seguro usar un objeto privado).
        // ----------------------------------------------------------------------------
        private object lockEscritura = new object();

        // ----------------------------------------------------------------------------
        // Constructor
        // ----------------------------------------------------------------------------
        public classTransRecep()
        {
            TramaEnvio = new byte[1024];
            TramCabaceraEnvio = new byte[5];
            tramaRelleno = new byte[1024];
            TramaRecibida = new byte[1024];

            // Relleno con '@'
            for (int i = 0; i <= 1023; i++)
            {
                tramaRelleno[i] = 64; // caracter ASCII '@'
            }
        }

        // ----------------------------------------------------------------------------
        // Get/Set de la ruta de descarga
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
        // Inicializa el puerto serie
        // ----------------------------------------------------------------------------
        public void Inicializa(string NombrePuerto)
        {
            puerto = new SerialPort(NombrePuerto, 57600, Parity.Even, 8, StopBits.Two);
            puerto.ReceivedBytesThreshold = 1024;
            puerto.DataReceived += new SerialDataReceivedEventHandler(puerto_DataReceived);
            puerto.Open();

            BufferSalidaVacio = true;

            // Lanzamos un hilo que verifique el estado del buffer de salida (opcional)
            // Si no es tan necesario, puedes eliminarlo o darle un Sleep para no saturar CPU
            procesoVerificaSalida = new Thread(VerificandoSalida);
            procesoVerificaSalida.IsBackground = true;
            procesoVerificaSalida.Start();

            // Inicializamos las estructuras para archivos
            arhivoEnviar = new ClassArchivoEnviando();
            arhivoRecibir = new ClassArchivoEnviando();

            MessageBox.Show("Apertura del puerto " + puerto.PortName);
        }

        // ----------------------------------------------------------------------------
        // Evento DataReceived: se dispara cuando llegan 1024 bytes
        // ----------------------------------------------------------------------------
        private void puerto_DataReceived(object o, SerialDataReceivedEventArgs sd)
        {
            if (puerto.BytesToRead >= 1024)
            {
                puerto.Read(TramaRecibida, 0, 1024);

                string TAREA = Encoding.UTF8.GetString(TramaRecibida, 0, 1);

                switch (TAREA)
                {
                    case "M":
                        procesoRecibirMensaje = new Thread(RecibiendoMensaje);
                        procesoRecibirMensaje.Start();
                        break;

                    case "D":
                        string CabeceraRecibida = Encoding.UTF8.GetString(TramaRecibida, 1, 4);
                        int LongitudMensajeRecibido = Convert.ToInt16(CabeceraRecibida);
                        string metadatosRecibidos = Encoding.UTF8.GetString(TramaRecibida, 5, LongitudMensajeRecibido);
                        InicioConstruirArchivo(metadatosRecibidos);
                        break;

                    case "A":
                        ConstruirArchivo();
                        break;

                    case "I":
                        // Caso "I" pendiente de implementar si se desea
                        break;

                    default:
                        MessageBox.Show("Trama no reconocida");
                        break;
                }
            }
        }

        // ----------------------------------------------------------------------------
        // Procesar mensaje de texto recibido
        // ----------------------------------------------------------------------------
        private void RecibiendoMensaje()
        {
            string CabRec = Encoding.UTF8.GetString(TramaRecibida, 1, 4);
            int LongMensRec = Convert.ToInt16(CabRec);

            mensRecibido = Encoding.UTF8.GetString(TramaRecibida, 5, LongMensRec);

            OnLlegoMensaje();
        }

        protected virtual void OnLlegoMensaje()
        {
            if (LlegoMensaje != null)
                LlegoMensaje(this, mensRecibido);
        }

        // ----------------------------------------------------------------------------
        // Enviar un mensaje de texto
        // ----------------------------------------------------------------------------
        public void Enviar(string mensaje, string tipo = "M")
        {
            if (mensaje.Length > 1019)
            {
                MessageBox.Show("El mensaje es demasiado largo. Máx permitido: 1019 caracteres.");
                return;
            }

            mensajeEnviar = mensaje;

            // Convertimos el mensaje a bytes
            byte[] bytesMensaje = Encoding.UTF8.GetBytes(mensajeEnviar);

            // Cabecera (tipo + longitud en 4 dígitos)
            string longitudMensaje = tipo + bytesMensaje.Length.ToString("D4");

            // Preparamos la trama
            TramCabaceraEnvio = Encoding.UTF8.GetBytes(longitudMensaje);
            TramaEnvio = bytesMensaje;

            // Lanzamos un hilo para enviar el mensaje (hilo paralelo)
            procesoEnvio = new Thread(Enviando);
            procesoEnvio.IsBackground = true;
            procesoEnvio.Start();
        }

        // ----------------------------------------------------------------------------
        // Hilo que escribe la trama de mensaje (con lock)
        // ----------------------------------------------------------------------------
        private void Enviando()
        {
            lock (lockEscritura)
            {
                // Escribimos 5 bytes de cabecera
                puerto.Write(TramCabaceraEnvio, 0, 5);

                // Escribimos el mensaje
                puerto.Write(TramaEnvio, 0, TramaEnvio.Length);

                // Relleno
                int faltan = 1019 - TramaEnvio.Length;
                if (faltan > 0)
                {
                    puerto.Write(tramaRelleno, 0, faltan);
                }
            }
        }

        // ----------------------------------------------------------------------------
        // Método opcional para recibir texto (no muy usado en tu ejemplo)
        // ----------------------------------------------------------------------------
        public void Recibir()
        {
            mensRecibido = puerto.ReadExisting();
            MessageBox.Show(mensRecibido);
        }

        // ----------------------------------------------------------------------------
        // Hilo que monitoriza el buffer de salida (opcional). Se sugiere un Thread.Sleep
        // ----------------------------------------------------------------------------
        private void VerificandoSalida()
        {
            while (true)
            {
                if (puerto.BytesToWrite > 0)
                    BufferSalidaVacio = false;
                else
                    BufferSalidaVacio = true;

                // Para no consumir CPU al 100%, dormimos un poco
                Thread.Sleep(10);
            }
        }

        // ----------------------------------------------------------------------------
        // Inicia el envío de un archivo
        // ----------------------------------------------------------------------------
        public void IniciaEnvioArchivo(string nombre)
        {
            // Abrimos el archivo
            FlujoArchivoEnviar = new FileStream(nombre, FileMode.Open, FileAccess.Read);
            LeyendoArchivo = new BinaryReader(FlujoArchivoEnviar);

            arhivoEnviar.Nombre = nombre;
            arhivoEnviar.Tamaño = FlujoArchivoEnviar.Length;
            arhivoEnviar.Avance = 0;
            arhivoEnviar.Num = 1;

            int indiceUltimaBarra = nombre.LastIndexOf('\\');
            string nombreArchivo = nombre.Substring(indiceUltimaBarra + 1);

            // Metadatos: "nombreArchivo-tamaño"
            string metadatos = nombreArchivo + "-" + FlujoArchivoEnviar.Length;

            // Enviamos la trama "D" con metadatos (nombre y tamaño)
            Enviar(metadatos, "D");

            // Iniciamos el hilo para enviar el archivo en bloques
            procesoEnvioArchivo = new Thread(EnviandoArchivo);
            procesoEnvioArchivo.IsBackground = true;
            procesoEnvioArchivo.Start();
        }

        // ----------------------------------------------------------------------------
        // Hilo que envía el archivo en bloques de 1019 bytes
        // ----------------------------------------------------------------------------
        private void EnviandoArchivo()
        {
            byte[] TramaEnvioArchivo = new byte[1019];
            byte[] TramCabaceraEnvioArchivo = new byte[5];

            // Cabecera "AI" (Archivo Informacion) -> 1 byte para 'A', luego 4 bytes para la longitud
            // Puedes sobrescribir el primer valor en cada bloque con la longitud real
            // o usar "1019" fijo si siempre envías 1019 (menos el último).
            // Aquí usaremos la longitud real en cada bloque.

            // Enviamos en un bucle mientras queden >= 1019 bytes
            while (arhivoEnviar.Avance <= arhivoEnviar.Tamaño - 1019)
            {
                LeyendoArchivo.Read(TramaEnvioArchivo, 0, 1019);
                arhivoEnviar.Avance += 1019;

                // Sin bloqueo de BufferSalidaVacio, simplemente lock para escritura
                lock (lockEscritura)
                {
                    // La longitud en 4 dígitos es "1019"
                    string sLong = "1019";
                    // Tipo A -> primer byte
                    TramCabaceraEnvioArchivo[0] = Encoding.UTF8.GetBytes("A")[0];
                    // Copiamos la longitud "1019"
                    byte[] bLong = Encoding.UTF8.GetBytes(sLong);
                    Array.Copy(bLong, 0, TramCabaceraEnvioArchivo, 1, 4);

                    // Escribimos la cabecera
                    puerto.Write(TramCabaceraEnvioArchivo, 0, 5);

                    // Escribimos los 1019 bytes de datos
                    puerto.Write(TramaEnvioArchivo, 0, 1019);
                }
            }

            // Ahora, el último bloque
            int tamanito = (int)(arhivoEnviar.Tamaño - arhivoEnviar.Avance);
            if (tamanito > 0)
            {
                byte[] ultimoBloque = new byte[tamanito];
                LeyendoArchivo.Read(ultimoBloque, 0, tamanito);

                arhivoEnviar.Avance += tamanito;

                lock (lockEscritura)
                {
                    // Cabecera
                    string sLong = tamanito.ToString("D4");
                    TramCabaceraEnvioArchivo[0] = Encoding.UTF8.GetBytes("A")[0];
                    byte[] bLong = Encoding.UTF8.GetBytes(sLong);
                    Array.Copy(bLong, 0, TramCabaceraEnvioArchivo, 1, 4);

                    puerto.Write(TramCabaceraEnvioArchivo, 0, 5);
                    puerto.Write(ultimoBloque, 0, tamanito);

                    // Relleno
                    int resto = 1019 - tamanito;
                    if (resto > 0)
                    {
                        puerto.Write(tramaRelleno, 0, resto);
                    }
                }
            }

            LeyendoArchivo.Close();
            FlujoArchivoEnviar.Close();

            // Mensaje de confirmación, si gustas
            MessageBox.Show("Archivo enviado completamente: " + arhivoEnviar.Nombre);
        }

        // ----------------------------------------------------------------------------
        // Preparar el archivo al recibir metadatos "D"
        // ----------------------------------------------------------------------------
        public void InicioConstruirArchivo(string metadatos)
        {
            string[] partes = metadatos.Split('-');
            string nombre = partes[0];
            string bytes = partes[1];

            FlujoArchivoRecibir = new FileStream(rutaDescarga + nombre, FileMode.Create, FileAccess.Write);
            EscribiendoArchivo = new BinaryWriter(FlujoArchivoRecibir);

            arhivoRecibir.Nombre = nombre;
            arhivoRecibir.Num = 1;
            arhivoRecibir.Tamaño = long.Parse(bytes);
            arhivoRecibir.Avance = 0;
        }

        // ----------------------------------------------------------------------------
        // Construir el archivo en bloques "A"
        // ----------------------------------------------------------------------------
        private void ConstruirArchivo()
        {
            if (arhivoRecibir.Avance <= arhivoRecibir.Tamaño - 1019)
            {
                EscribiendoArchivo.Write(TramaRecibida, 5, 1019);
                arhivoRecibir.Avance += 1019;
            }
            else
            {
                int tamanito = Convert.ToInt16(arhivoRecibir.Tamaño - arhivoRecibir.Avance);
                EscribiendoArchivo.Write(TramaRecibida, 5, tamanito);
                EscribiendoArchivo.Close();
                FlujoArchivoRecibir.Close();

                MessageBox.Show("Archivo recibido: " + arhivoRecibir.Nombre);
            }
        }
    }
}
