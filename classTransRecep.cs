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
        public delegate void HandlerTxRx(object oo, string mensRec);
        public event HandlerTxRx LlegoMensaje;
        public delegate void HandlerTxRxArchivo(object oo, string mensRec);
        public event HandlerTxRxArchivo LlegoArchivo;

        private ClassArchivoEnviando arhivoEnviar;
        private FileStream FlujoArchivoEnviar;
        private BinaryReader LeyendoArchivo;


        private ClassArchivoEnviando arhivoRecibir;
        private FileStream FlujoArchivoRecibir;
        private BinaryWriter EscribiendoArchivo;


        Thread procesoEnvio;
        Thread procesoVerificaSalida;
        Thread procesoRecibirMensaje;

        Thread procesoEnvioArchivo;
        Thread procesoConstruyeArchivo;

        private Object lockPuerto = new object();

        private SerialPort puerto;
        private string mensajeEnviar;
        private string mensRecibido;
        private string rutaArchivoRecibido;

        private string rutaDescarga = "D:\\PRUEBA\\1\\";

        private Boolean BufferSalidaVacio;

        byte[] TramaEnvio;
        byte[] TramCabaceraEnvio;
        byte[] tramaRelleno;

        byte[] TramaRecibida;

        public classTransRecep()
        {
            TramaEnvio = new byte[1024];
            TramCabaceraEnvio = new byte[5];
            tramaRelleno = new byte[1024];

            TramaRecibida = new byte[1024];

            for (int i = 0; i <= 1023; i++)
            { tramaRelleno[i] = 64; }

        }

        public string getRutaDescarga()
        {
            return rutaDescarga;
        }

        public void modificarRutaDescarga(string nuevaRuta)
        {
            rutaDescarga = nuevaRuta;
        }

        public void Inicializa(string NombrePuerto)
        {
            puerto = new SerialPort(NombrePuerto, 57600, Parity.Even, 8, StopBits.Two);
            puerto.ReceivedBytesThreshold = 1024;
            puerto.DataReceived += new SerialDataReceivedEventHandler(puerto_DataReceived);
            puerto.Open();

            BufferSalidaVacio = true;
            procesoVerificaSalida = new Thread(VerificandoSalida);
            procesoVerificaSalida.Start();

            arhivoEnviar = new ClassArchivoEnviando();
            arhivoRecibir = new ClassArchivoEnviando();

            MessageBox.Show("El puerto " + puerto.PortName + " se abrió correctamente");

        }

        private void puerto_DataReceived(object o, SerialDataReceivedEventArgs sd)
        {
            if (puerto.BytesToRead >= 1024)
            {
                puerto.Read(TramaRecibida, 0, 1024);

                string TAREA = ASCIIEncoding.UTF8.GetString(TramaRecibida, 0, 1);

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
                        //MessageBox.Show(metadatosRecibidos);
                        InicioConstruirArchivo(metadatosRecibidos);
                        break;
                    case "A":
                        ConstruirArchivo();
                        break;
                    case "I":
                        break;
                    default:
                        MessageBox.Show("trama no reconocida");
                        break;
                }
            }
        }

        private void RecibiendoMensaje()
        {
            string CabRec = ASCIIEncoding.UTF8.GetString(TramaRecibida, 1, 4);
            MessageBox.Show(CabRec);
            int LongMensRec = Convert.ToInt16(CabRec);

            mensRecibido = ASCIIEncoding.UTF8.GetString(TramaRecibida, 5, LongMensRec);

            OnLlegoMensaje();

        }

        protected virtual void OnLlegoMensaje()
        {
            if (LlegoMensaje != null)
                LlegoMensaje(this, mensRecibido);
        }

        protected virtual void OnLlegoArchivo()
        {
            if (LlegoArchivo != null)
                LlegoArchivo(this, rutaArchivoRecibido);
        }

        public void Enviar(string mensaje, string tipo = "M")
        {
            if (mensaje.Length > 1019)
            {
                MessageBox.Show("El mensaje es demasiado largo. \nEl máximo permitido: 1019 caracteres.");
                return;
            }

            mensajeEnviar = mensaje;
            byte[] bytesMensaje = Encoding.UTF8.GetBytes(mensajeEnviar);

            string longitudMensaje = tipo + bytesMensaje.Length.ToString("D4");

            TramaEnvio = bytesMensaje;
            TramCabaceraEnvio = ASCIIEncoding.UTF8.GetBytes(longitudMensaje);
            procesoEnvio = new Thread(Enviando);
            procesoEnvio.Start();


        }

        private void Enviando()
        {
            lock (lockPuerto)
            {
                puerto.Write(TramCabaceraEnvio, 0, 5);
                puerto.Write(TramaEnvio, 0, TramaEnvio.Length);
                puerto.Write(tramaRelleno, 0, 1019 - TramaEnvio.Length);
            }
        }

        private void VerificandoSalida()
        {
            while (true)
            {
                if (puerto.BytesToWrite > 0)
                    BufferSalidaVacio = false;
                else
                    BufferSalidaVacio = true;
            }

        }

        public void IniciaEnvioArchivo(string nombre)
        {
            // abrirlo , manejar las excepciones
            // leerlo en stream
            // iniciar una hebra de envio
            FlujoArchivoEnviar = new FileStream(nombre, FileMode.Open, FileAccess.Read);
            LeyendoArchivo = new BinaryReader(FlujoArchivoEnviar);

            arhivoEnviar.Nombre = nombre;
            arhivoEnviar.Tamaño = FlujoArchivoEnviar.Length;
            arhivoEnviar.Avance = 0;
            arhivoEnviar.Num = 1;

            int indiceUltimaBarra = nombre.LastIndexOf('\\');
            string nombreArchivo = nombre.Substring(indiceUltimaBarra + 1);

            string metadatos = nombreArchivo + "-" + FlujoArchivoEnviar.Length;
            //MessageBox.Show(metadatos);
            Enviar(metadatos, "D");
            procesoEnvioArchivo = new Thread(EnviandoArchivo);
            procesoEnvioArchivo.Start();

        }

        private void EnviandoArchivo()
        {
            byte[] TramaEnvioArchivo;
            byte[] TramCabaceraEnvioArchivo;

            TramaEnvioArchivo = new byte[1019];
            TramCabaceraEnvioArchivo = new byte[5];

            //ENVIAR LA PRIMERA TRAMA CON EL NOMBRE ARCHIVO;
            TramCabaceraEnvioArchivo = ASCIIEncoding.UTF8.GetBytes("AC001");
            //ENVIAR LAS TRAMAS DE INFORMACION
            TramCabaceraEnvioArchivo = ASCIIEncoding.UTF8.GetBytes("AI001");

            while (arhivoEnviar.Avance <= arhivoEnviar.Tamaño - 1019)
            {
                LeyendoArchivo.Read(TramaEnvioArchivo, 0, 1019);
                arhivoEnviar.Avance = arhivoEnviar.Avance + 1019;
                //envio de una trama llena de 1019 bytes del archivo
                while (BufferSalidaVacio == false)
                {//esperamos
                }
                lock (lockPuerto)
                {
                    //MessageBox.Show("avance = " + arhivoEnviar.Avance.ToString());
                    puerto.Write(TramCabaceraEnvioArchivo, 0, 5);
                    puerto.Write(TramaEnvioArchivo, 0, 1019);
                    // puerto.Write(tramaRelleno, 0, 1019 - TramaEnvio.Length);
                }

                //Console.WriteLine("Escribiendo archivo...");
                Console.WriteLine("Escribiendo archivo..." + "Avance: " + Math.Round(((double)arhivoEnviar.Avance / arhivoEnviar.Tamaño) * 100) + "%.");
                Console.WriteLine("Avance: " + arhivoEnviar.Avance);
                Console.WriteLine("Tamaño: " + arhivoEnviar.Tamaño);

            }
            int tamanito = Convert.ToInt16(arhivoEnviar.Tamaño - arhivoEnviar.Avance);
            LeyendoArchivo.Read(TramaEnvioArchivo, 0, tamanito);
            arhivoEnviar.Avance = arhivoEnviar.Avance + tamanito;
            //envio de la ultima trama + relleno
            while (BufferSalidaVacio == false)
            {//esperamos
            }
            //MessageBox.Show("avance = " + arhivoEnviar.Avance.ToString() + " t= " + tamanito.ToString());
            lock (lockPuerto)
            {
                puerto.Write(TramCabaceraEnvioArchivo, 0, 5);
                puerto.Write(TramaEnvioArchivo, 0, tamanito);
                puerto.Write(tramaRelleno, 0, 1019 - tamanito);
                //Console.WriteLine("Escribiendo archivo..." + "Avance: " + (arhivoEnviar.Avance / arhivoEnviar.Tamaño) * 100 + "%.");

                //Console.WriteLine("Escribiendo archivo..." + "Avance: " + (arhivoEnviar.Avance / arhivoEnviar.Tamaño) * 100 + "%.");
                //Console.WriteLine("Avance: " + arhivoEnviar.Avance);
                //Console.WriteLine("Tamaño: " + arhivoEnviar.Tamaño);
                Console.WriteLine("Final: " + Math.Round(((double)arhivoEnviar.Avance / arhivoEnviar.Tamaño) * 100) + "%.");
                Console.WriteLine("Archivo escrito totalmente.");
            }
            LeyendoArchivo.Close();
            FlujoArchivoEnviar.Close();
        }

        //debe ser privado, y debe llamarse en una hebra al recibir la primera trama FILE
        // tamaño, nombre, numero identificacion....
        public void InicioConstruirArchivo(string metadatos)
        {
            string[] partes = metadatos.Split('-');
            string nombre = partes[0];
            string bytes = partes[1];
            rutaArchivoRecibido = rutaDescarga + nombre;
            FlujoArchivoRecibir = new FileStream(rutaDescarga + nombre, FileMode.Create, FileAccess.Write);
            EscribiendoArchivo = new BinaryWriter(FlujoArchivoRecibir);
            arhivoRecibir.Nombre = nombre;
            arhivoRecibir.Num = 1;
            arhivoRecibir.Tamaño = long.Parse(bytes);
            arhivoRecibir.Avance = 0;
        }

        private void ConstruirArchivo()
        {
            // debe realizarse en funcion del tamaño 1019 y la ultima será tamanito

            if (arhivoRecibir.Avance <= arhivoRecibir.Tamaño - 1019)
            {
                EscribiendoArchivo.Write(TramaRecibida, 5, 1019);
                arhivoRecibir.Avance = arhivoRecibir.Avance + 1019;
                Console.WriteLine("Escribiendo archivo..." + "Avance: " + Math.Round(((double)arhivoRecibir.Avance / arhivoRecibir.Tamaño) * 100) + "%.");
                //Console.WriteLine("Escribiendo archivo...");
                //Console.WriteLine("Avance: " + arhivoRecibir.Avance);
            }

            else
            {
                int tamanito = Convert.ToInt16(arhivoRecibir.Tamaño - arhivoRecibir.Avance);
                EscribiendoArchivo.Write(TramaRecibida, 5, tamanito);
                Console.WriteLine("Final: " + Math.Round(((double)arhivoRecibir.Avance / arhivoRecibir.Tamaño) * 100) + "%.");
                Console.WriteLine("Archivo recibido.");
                //Console.WriteLine("Avance: " + arhivoRecibir.Avance);
                EscribiendoArchivo.Close();
                FlujoArchivoRecibir.Close();
                OnLlegoArchivo();
            }

        }

    }
}



