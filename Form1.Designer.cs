namespace winproySerialPort
{
    partial class Form1
    {
        /// <summary>
        /// Variable del diseñador necesaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Limpiar los recursos que se estén usando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados se deben desechar; false en caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        /// <summary>
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido de este método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.btnEnviar = new System.Windows.Forms.Button();
            this.BtnEnviarArchivo = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.btnCfg = new System.Windows.Forms.Button();
            this.chatContainer = new System.Windows.Forms.Panel();
            this.txtMensaje = new System.Windows.Forms.TextBox();
            this.lblContBytes = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnEnviar
            // 
            this.btnEnviar.Image = ((System.Drawing.Image)(resources.GetObject("btnEnviar.Image")));
            this.btnEnviar.Location = new System.Drawing.Point(442, 491);
            this.btnEnviar.Name = "btnEnviar";
            this.btnEnviar.Size = new System.Drawing.Size(35, 35);
            this.btnEnviar.TabIndex = 0;
            this.btnEnviar.UseVisualStyleBackColor = true;
            this.btnEnviar.Click += new System.EventHandler(this.btnEnviar_Click);
            // 
            // BtnEnviarArchivo
            // 
            this.BtnEnviarArchivo.Image = ((System.Drawing.Image)(resources.GetObject("BtnEnviarArchivo.Image")));
            this.BtnEnviarArchivo.Location = new System.Drawing.Point(401, 491);
            this.BtnEnviarArchivo.Name = "BtnEnviarArchivo";
            this.BtnEnviarArchivo.Size = new System.Drawing.Size(35, 35);
            this.BtnEnviarArchivo.TabIndex = 4;
            this.BtnEnviarArchivo.UseVisualStyleBackColor = true;
            this.BtnEnviarArchivo.Click += new System.EventHandler(this.BtnEnviarArchivo_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(320, 497);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 5;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // btnCfg
            // 
            this.btnCfg.Image = ((System.Drawing.Image)(resources.GetObject("btnCfg.Image")));
            this.btnCfg.Location = new System.Drawing.Point(28, 12);
            this.btnCfg.Name = "btnCfg";
            this.btnCfg.Size = new System.Drawing.Size(35, 35);
            this.btnCfg.TabIndex = 6;
            this.btnCfg.UseVisualStyleBackColor = true;
            this.btnCfg.Click += new System.EventHandler(this.button2_Click);
            // 
            // chatContainer
            // 
            this.chatContainer.AutoScroll = true;
            this.chatContainer.BackColor = System.Drawing.SystemColors.Window;
            this.chatContainer.Location = new System.Drawing.Point(28, 62);
            this.chatContainer.Name = "chatContainer";
            this.chatContainer.Size = new System.Drawing.Size(449, 334);
            this.chatContainer.TabIndex = 7;
            // 
            // txtMensaje
            // 
            this.txtMensaje.Location = new System.Drawing.Point(28, 411);
            this.txtMensaje.Multiline = true;
            this.txtMensaje.Name = "txtMensaje";
            this.txtMensaje.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtMensaje.Size = new System.Drawing.Size(449, 74);
            this.txtMensaje.TabIndex = 8;
            this.txtMensaje.TextChanged += new System.EventHandler(this.txtMensaje_TextChanged);
            this.txtMensaje.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtMensaje_KeyPress);
            // 
            // lblContBytes
            // 
            this.lblContBytes.AutoSize = true;
            this.lblContBytes.BackColor = System.Drawing.Color.Transparent;
            this.lblContBytes.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblContBytes.Location = new System.Drawing.Point(28, 497);
            this.lblContBytes.Name = "lblContBytes";
            this.lblContBytes.Size = new System.Drawing.Size(0, 15);
            this.lblContBytes.TabIndex = 9;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.ClientSize = new System.Drawing.Size(506, 538);
            this.Controls.Add(this.lblContBytes);
            this.Controls.Add(this.txtMensaje);
            this.Controls.Add(this.chatContainer);
            this.Controls.Add(this.btnCfg);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.BtnEnviarArchivo);
            this.Controls.Add(this.btnEnviar);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Form Apellidos Nombres";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button btnEnviar;
        private System.Windows.Forms.Button BtnEnviarArchivo;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button btnCfg;
        private System.Windows.Forms.Panel chatContainer;
        private System.Windows.Forms.TextBox txtMensaje;
        private System.Windows.Forms.Label lblContBytes;
    }
}

