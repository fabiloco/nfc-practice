using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;// UTILIZADO PARA LAS EXPRESIONES REGULARES
using NFC_Wrapper_Sample;

namespace nfc_project3
{
    public partial class Form1 : Form
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll", EntryPoint = "LoadLibraryA")]
        static extern int LoadLibrary(string lpLibFileName); // CARGAMOS LA LIBRERIA

        private TNFCWrapper NFCWrapper;

        private string Mensaje = null; //VARIABLE MENSAJE GLOBAL


        public Form1()
        {
            InitializeComponent();
        }

        public static bool ValidateUrl(string url) // METODO CON EXPRESION REGULAR QUE VALIDA LAS URL
        {
            if (url == null || url == "") return false;
            Regex oRegExp = new Regex(@"(http|ftp|https)://([\w-]+\.)+(/[\w- ./?%&=]*)?", RegexOptions.IgnoreCase);
            return oRegExp.Match(url).Success;
        }

        private void LogMessage(string Msg) // FORMATO DE MENSAJES
        {
            textBox2.Text += Msg;
            textBox2.Text += Environment.NewLine;
            textBox2.Refresh();
            textBox2.SelectionStart = textBox2.Text.Length;
            textBox2.ScrollToCaret();
        }

        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")] // PERMISOS
        protected override void WndProc(ref Message aMessage) // TIPOS DE MENSAJES Y EVENTOS
        {
            if (aMessage.Msg == TNFCWrapper.WM_NFC_NOTIFY)
            {
                string s = "";
                Int32 wParam = aMessage.WParam.ToInt32();
                switch (wParam)
                {
                    case TNFCWrapper.NFC_NDEF_FOUND:
                        s = "NFC_NDEF_FOUND Size = " +
                       aMessage.LParam.ToString();
                        break;
                    case TNFCWrapper.NFC_DEVICE_CHANGED:
                        s = "Dispositivo NFC Cargado";
                        break;
                    case TNFCWrapper.NFC_UNKNOWN_SERVICE:
                        s = "Servicio NFC desconocido";
                        break;
                    case TNFCWrapper.NFC_CONNECTED:
                        s = "Tag NFC Conectado";
                        break;
                    case TNFCWrapper.NFC_DISCONNECTED:
                        s = "NFC Desconectado";
                        break;
                    case TNFCWrapper.NFC_IDLE:
                        s = "NFC_IDLE";
                        break;
                    default:
                        s = "Desconocido";
                        break;
                }
                LogMessage(s);
                if (wParam == TNFCWrapper.NFC_DEVICE_CHANGED)
                    DeviceCountLabel.Text = aMessage.LParam.ToString();
                if (wParam == TNFCWrapper.NFC_NDEF_FOUND)
                    ReadNDEF();
            }
            base.WndProc(ref aMessage);
        }

        private void ReadNDEF()
        {
            UInt32 DeviceCount = 0;
            UInt32 MessageCount = 0;
            UInt32 NextMessageSize = 0;
            UInt32 Result;
            if (NFCWrapper == null) return;
            // Get information about the message queue
            Result = TNFCWrapper.GetNDEFQueueInfo(ref DeviceCount, ref MessageCount, ref NextMessageSize);
            LogMessage("GetNDEFQueueInfo: " + NFCWrapper.NFCWrapperErrorToString(Result));

            if (Result != TNFCWrapper.ERR_SUCCESS) return;

            LogMessage(" DeviceCount = " + DeviceCount.ToString());

            LogMessage(" MessageCount = " + MessageCount.ToString());

            LogMessage(" NextMessageSize = " + NextMessageSize.ToString());

            //Resize the NDEF buffer accordingly to the site of the next message in the queue

            byte[] NDEF = new byte[NextMessageSize];
            UInt32 NDEFSize = NextMessageSize;
            TNFCAddress NFCAddress = new TNFCAddress();
            TMessageInfo MessageInfo = new TMessageInfo();

            //Read the NDEF message from the message queue
            Result = TNFCWrapper.ReadNDEF(ref NFCAddress, ref
            MessageInfo, ref NDEF[0], ref NDEFSize);
            LogMessage("ReadNDEF: " + NFCWrapper.NFCWrapperErrorToString(Result));
            if (Result != TNFCWrapper.ERR_SUCCESS) return;
            
            // CONVERTIR A XML
            string XML = "";
            string XML1 = null;
            string XML2 = null;
            Result = NFCWrapper.NDEF2XML(ref NDEF[0], NDEFSize, ref XML);
            LogMessage("NDEF2XML: " + NFCWrapper.NFCWrapperErrorToString(Result));
            if (Result != TNFCWrapper.ERR_SUCCESS) return;
            // print NDEF as XML
            textBox2.Clear();
            LogMessage(XML);
            // PRIMER FILTRADO del string donde, tomaremos linea por linea el string XML y tomamos como delimitador los saltos de linea
             // es alli donde se encuentra la linea del mensaje o url. este es el resultado: <NDEF_URI:URI>Mensaje</NDEF_URI:URI>
             XML1 = XML.Split('\n')[12];
             // SEGUNDO FILTRADO del string anterior donde, tomaremos linea por linea el string XML1, y tomamos como delimitador ">"
             // este es el resultado en la posicion 1: <NDEF_URI:URI>Mensaje
             XML2 = XML1.Split('<')[1];
             // ULTIMO FILTRADO del string anterior donde, tomaremos linea por linea el string XML2, y tomamos como delimitador ">"
             // este es el resultado en la posicion 1 : Mensaje
             Mensaje = XML2.Split('>')[1];
             if (ValidateUrl(Mensaje) == true) // VALIDAMOS QUE SI MENSAJE ES UNA URL ENTONCES
             {
                 webBrowser1.Navigate(Mensaje); // ENVIAR URL AL NAVEGADOR Y EJECUTARSE
             }
             else
             {
             MessageBox.Show(Mensaje, "Mensaje"); // SINO ES UNA URL ENTONCES ENVIAR ALERTA CON EL MENSAJE
             }
             LogMessage(" ");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Verificar que exista la libreria
            if (LoadLibrary("SCM_NFC.dll") == 0)
            {
                LogMessage("NFC Wrapper no encontrada");
                LogMessage("Asegurate que el archivo SCM_NFC.dll este presente");
                return;
            }
            LogMessage("SCM_NFC.DLL Cargada exitosamente.");
            StartButton.Enabled = false;
            WriteButton.Enabled = true; // SE HABILITA EL BOTON PARA ESCRIBIR EN LA TARJETA
            //Init NFC Wrapper
            
            NFCWrapper = new TNFCWrapper();
            TNFCWrapper.Initialize((UInt32)Handle.ToInt32());
            //DECIRLE A NFC WRAPPER QUE EMPIECE CON LA LECTURA
            TNFCWrapper.StartListening();
            LogMessage("Por favor coloca una tag NFC");
        }

        private void WriteButton_Click(object sender, EventArgs e)
        {
            if (NFCWrapper == null) return;
            UInt32 Result;
            //Create Smart Poster NDEF Message
            // INFORMACION NECESARIO PARA ENVIARLE A TNFCWRAPPER.CS COMO PARAMETRO
            string URI = Texto.Text.ToString();
            string Comment = "";
            string Language = "en-US";
            string TargetType = "";
            UInt32 Size = 0;
            byte Action = 0;
            UInt32 NDEFSize = 1000;
            byte[] NDEF = new byte[NDEFSize];
            Result = TNFCWrapper.CreateNDEFSp(URI, Comment, Language, ref Action, ref Size, TargetType, ref NDEF[0], ref NDEFSize);
            LogMessage("CreateNDEFSp: " + NFCWrapper.NFCWrapperErrorToString(Result));
            if (Result != TNFCWrapper.ERR_SUCCESS) return;
            // convert NDEF into XML
            string XML = "";
            Result = NFCWrapper.NDEF2XML(ref NDEF[0], NDEFSize, ref XML);
            LogMessage("NDEF2XML: " + NFCWrapper.NFCWrapperErrorToString(Result));
            if (Result != TNFCWrapper.ERR_SUCCESS) return;
            // print NDEF as XML
            LogMessage(XML);
            LogMessage(" ");
            // ESCRIBIMOS EL NDEF A LA ETIQUETA
            LogMessage("Ponga una NFC Tag en los proximos 5 segundos...");

            Texto.Clear();
            TNFCAddress NFCAddress = new TNFCAddress();
            TMessageInfo MessageInfo = new TMessageInfo();
            Result = TNFCWrapper.WriteNDEF(ref NFCAddress, ref MessageInfo, ref NDEF[0], ref NDEFSize, false, true, 5);
            LogMessage("NDEF2XML: " + NFCWrapper.NFCWrapperErrorToString(Result));
            LogMessage("");
        }

    }
}
