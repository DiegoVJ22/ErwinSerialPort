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
        delegate void MostrarOtroProceso(string mensaje);
        MostrarOtroProceso delegadoMostrar;
        
        public Form1()
        {
            InitializeComponent();
        }

        private void btnEnviar_Click(object sender, EventArgs e)
        {
            objTxRx.Enviar(rchMensajes.Text.Trim());
            rchMensajes.Text = ""; 
           
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            objTxRx = new classTransRecep();
            objTxRx.Inicializa("COM2");
            objTxRx.LlegoMensaje += new classTransRecep.HandlerTxRx(objTxRx_LlegoMensaje);
            delegadoMostrar = new MostrarOtroProceso(MostrandoMensaje); 
        }


        private void objTxRx_LlegoMensaje(object o, string mm)
        {
            Invoke(delegadoMostrar , mm);
        }

        private void MostrandoMensaje(string textMens)
        {
            rchConversacion.Text += "\n" + textMens;
        }

        private void BtnEnviarArchivo_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Selecciona un archivo";
            openFileDialog.Filter = "Todos los archivos (*.*)|*.*";
            openFileDialog.Multiselect = false;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // Obtener la ruta del archivo seleccionado
                string rutaArchivo = openFileDialog.FileName;

                objTxRx.IniciaEnvioArchivo(rutaArchivo);

                //MessageBox.Show("Archivo seleccionado: " + rutaArchivo + "\nTamaño:" + bytesArchivo);
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
