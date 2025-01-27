using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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
        MostrarOtroProceso delegadoMostrar, delegadoMostrarArchivo;
        ArrayList rutasArchivos = new ArrayList();
        public Form1()
        {
            InitializeComponent();
        }

        private void btnEnviar_Click(object sender, EventArgs e)
        {
            byte[] bytesMensajeEnviar = Encoding.UTF8.GetBytes(txtMensaje.Text);
            if (bytesMensajeEnviar.Length > 1019)
            {
                MessageBox.Show("El mensaje es demasiado largo. \nEl máximo permitido: 1019 caracteres.");
                return;
            }
            if (!string.IsNullOrWhiteSpace(txtMensaje.Text))
            {
                objTxRx.Enviar(txtMensaje.Text.Trim());

                Label messageLabel = new Label
                {
                    Text = txtMensaje.Text,
                    AutoSize = true,
                    MaximumSize = new Size(chatContainer.DisplayRectangle.Width - 150, 0),
                    BackColor = Color.LightGreen,
                    Padding = new Padding(5),
                    Margin = new Padding(5),
                    BorderStyle = BorderStyle.None,
                };

                int yOffset = chatContainer.Controls.Count > 0
                ? chatContainer.Controls[chatContainer.Controls.Count - 1].Bottom + 5
                : 10;

                int xOffset = chatContainer.DisplayRectangle.Width - messageLabel.PreferredWidth - 20;

                messageLabel.Location = new Point(xOffset, yOffset);
                chatContainer.Controls.Add(messageLabel);
                chatContainer.ScrollControlIntoView(messageLabel);

                txtMensaje.Clear();
            }

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            objTxRx = new classTransRecep();
            objTxRx.Inicializa("COM2");
            objTxRx.LlegoMensaje += new classTransRecep.HandlerTxRx(objTxRx_LlegoMensaje);
            objTxRx.LlegoArchivo += new classTransRecep.HandlerTxRxArchivo(objTxRx_LlegoArchivo);
            delegadoMostrar = new MostrarOtroProceso(MostrandoMensaje); 
            delegadoMostrarArchivo = new MostrarOtroProceso(MostrandoArchivo);
        }


        private void objTxRx_LlegoMensaje(object o, string mm)
        {
            Invoke(delegadoMostrar , mm);
        }

        private void objTxRx_LlegoArchivo(object o, string ruta)
        {
            Invoke(delegadoMostrarArchivo, ruta);
        }

        private void MostrandoArchivo(string rutaArchivo)
        {
            // Crear un PictureBox para mostrar la imagen
            PictureBox pictureBox = new PictureBox
            {
                Image = Image.FromFile(rutaArchivo),
                Size = new Size(170, 170), // Redimensionar a 170x170
                SizeMode = PictureBoxSizeMode.Zoom, // Ajustar la imagen al tamaño manteniendo la relación de aspecto
                BackColor = Color.Transparent,
                BorderStyle = BorderStyle.FixedSingle, // Borde sencillo
            };

            // Configurar el margen para el borde verde
            pictureBox.Padding = new Padding(2);
            pictureBox.BackColor = Color.LightGray; // Borde gris claro

            int yOffset = chatContainer.Controls.Count > 0
                ? chatContainer.Controls[chatContainer.Controls.Count - 1].Bottom + 5
                : 10;

            pictureBox.Location = new Point(10, yOffset);
            pictureBox.Click += (s, args) => Process.Start(new ProcessStartInfo(rutaArchivo) { UseShellExecute = true });
            // Agregar el PictureBox al contenedor
            chatContainer.Controls.Add(pictureBox);
            chatContainer.ScrollControlIntoView(pictureBox);


        }

        private void MostrandoMensaje(string mensajeRecibido)
        {
            Label messageLabel = new Label
            {
                Text = mensajeRecibido,
                AutoSize = true,
                MaximumSize = new Size(chatContainer.DisplayRectangle.Width - 20, 0),
                BackColor = Color.LightGray,
                Padding = new Padding(5),
                Margin = new Padding(5),
                BorderStyle = BorderStyle.None,
            };

            int yOffset = chatContainer.Controls.Count > 0
                ? chatContainer.Controls[chatContainer.Controls.Count - 1].Bottom + 5
                : 10;

            messageLabel.Location = new Point(10, yOffset);
            chatContainer.Controls.Add(messageLabel);
            chatContainer.ScrollControlIntoView(messageLabel);
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
                rutasArchivos.Add(rutaArchivo);
                objTxRx.IniciaEnvioArchivo(rutaArchivo);

                // Crear un PictureBox para mostrar la imagen
                PictureBox pictureBox = new PictureBox
                {
                    Image = Image.FromFile(rutaArchivo),
                    Size = new Size(170, 170), // Redimensionar a 170x170
                    SizeMode = PictureBoxSizeMode.Zoom, // Ajustar la imagen al tamaño manteniendo la relación de aspecto
                    BackColor = Color.Transparent,
                    BorderStyle = BorderStyle.FixedSingle, // Borde sencillo
                };

                // Configurar el margen para el borde verde
                pictureBox.Padding = new Padding(2);
                pictureBox.BackColor = Color.LightGreen; // Borde verde

                // Calcular la posición del PictureBox en el contenedor
                int yOffset = chatContainer.Controls.Count > 0
                    ? chatContainer.Controls[chatContainer.Controls.Count - 1].Bottom + 5
                    : 10;

                int xOffset = chatContainer.DisplayRectangle.Width - pictureBox.Width - 20;

                pictureBox.Location = new Point(xOffset, yOffset);
                pictureBox.Click += (s, args) => Process.Start(new ProcessStartInfo(rutaArchivo) { UseShellExecute = true });
                // Agregar el PictureBox al contenedor
                chatContainer.Controls.Add(pictureBox);
                chatContainer.ScrollControlIntoView(pictureBox);
                //MessageBox.Show("Archivo seleccionado: " + rutaArchivo + "\nTamaño:" + bytesArchivo);
            }
        }

        //Métodos de prueba

        private void button2_Click(object sender, EventArgs e)
        {
            // Crear un FolderBrowserDialog para seleccionar la carpeta
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "Selecciona la carpeta donde deseas guardar los archivos";
                folderBrowserDialog.ShowNewFolderButton = true; // Permitir crear nuevas carpetas

                // Mostrar el diálogo y verificar si el usuario seleccionó una carpeta
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    // Guardar la ruta seleccionada en la variable rutaDescarga
                    string rutaDescarga = folderBrowserDialog.SelectedPath;
                    objTxRx.modificarRutaDescarga(rutaDescarga);
                }
            }
        }

        private void txtMensaje_TextChanged(object sender, EventArgs e)
        {
            byte[] bytesToWrite = Encoding.UTF8.GetBytes(txtMensaje.Text);
            lblContBytes.Text = $"{bytesToWrite.Length}/1019";
        }

        private void txtMensaje_KeyPress(object sender, KeyPressEventArgs e)
        {
            byte[] bytesToWrite = Encoding.UTF8.GetBytes(txtMensaje.Text);
            if (bytesToWrite.Length >= 1019 && e.KeyChar != (char)Keys.Back)
            {
                e.Handled = true;
            }
        }
    }
}
