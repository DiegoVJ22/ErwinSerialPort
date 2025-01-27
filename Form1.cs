using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace winproySerialPort
{
    public partial class Form1 : Form
    {
        classTransRecep objTxRx;
        
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            objTxRx = new classTransRecep();

            // Suscribimos ambos eventos
            objTxRx.LlegoMensaje += ObjTxRx_LlegoMensaje;
            objTxRx.LlegoArchivo += ObjTxRx_LlegoArchivo;

            objTxRx.Inicializa("COM2"); // o el puerto que quieras
        }

        // --------------------------------------------------------------------
        // 1) Evento de texto recibido
        // --------------------------------------------------------------------
        private void ObjTxRx_LlegoMensaje(object sender, string mensaje)
        {
            // Como llega de un hilo diferente, usar Invoke
            Invoke(new Action(() =>
            {
                rchConversacion.AppendText("[RECEIVED]: " + mensaje + "\n");
            }));
        }

        // --------------------------------------------------------------------
        // 2) Evento de metadatos de archivo
        //    -> el usuario puede elegir dónde guardarlo, y luego
        //       llamamos a "InicioConstruirArchivo"
        // --------------------------------------------------------------------
        private void ObjTxRx_LlegoArchivo(object sender, string nombreArchivo, long tamañoArchivo)
        {
            Invoke(new Action(() =>
            {
                // Por ejemplo, preguntamos si desea guardar
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.FileName = nombreArchivo; // Nombre sugerido
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    // Llamamos a "InicioConstruirArchivo" en la clase
                    objTxRx.InicioConstruirArchivo(sfd.FileName, nombreArchivo, tamañoArchivo);
                }
            }));
        }

        private void btnEnviar_Click(object sender, EventArgs e)
        {
            string mensaje = rchMensajes.Text.Trim();
            if (!string.IsNullOrEmpty(mensaje))
            {
                // Mostrar en la conversación local
                rchConversacion.AppendText("[SENT]: " + mensaje + "\n");
                // Enviar
                objTxRx.Enviar(mensaje);
                rchMensajes.Clear();
            }
        }

        // --------------------------------------------------------------------
        // Botón para enviar un archivo (se selecciona con OpenFileDialog)
        // --------------------------------------------------------------------
        private void BtnEnviarArchivo_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Selecciona un archivo";
            ofd.Filter = "Todos los archivos (*.*)|*.*";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                // Llamamos a IniciaEnvioArchivo
                objTxRx.IniciaEnvioArchivo(ofd.FileName);
            }
        }

        //Métodos de prueba

        private void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show(objTxRx.getRutaDescarga());
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string nuevaRuta = "nuevo";
            objTxRx.modificarRutaDescarga(nuevaRuta);
        }
    }
}
