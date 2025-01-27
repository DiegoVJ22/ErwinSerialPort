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

        private void Form1_Load(object sender, EventArgs e)
        {
            objTxRx = new classTransRecep();
            objTxRx.Inicializa("COM2");
            objTxRx.LlegoMensaje += new classTransRecep.HandlerTxRx(objTxRx_LlegoMensaje);
        }

        private void objTxRx_LlegoMensaje(object o, string mm)
        {
            // Usamos Invoke para actualizar UI desde el hilo de recepción
            Invoke(new Action(() =>
            {
                rchConversacion.Text += "\n" + mm;
            }));
        }

        private void btnEnviar_Click(object sender, EventArgs e)
        {
            objTxRx.Enviar(rchMensajes.Text.Trim());
            rchMensajes.Text = "";
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
                string rutaArchivo = openFileDialog.FileName;
                objTxRx.IniciaEnvioArchivo(rutaArchivo);
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
