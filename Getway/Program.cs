using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.ServiceProcess;

namespace GetWay
{
    class SocketHelper
    {
        TcpClient msClient;
        string mstrMessage;
        string mstrResponse;
        byte[] bytesSent;
        public void processMsg(TcpClient client, NetworkStream stream, byte[] bytesRecibidos)
        {
            msClient = client;
            byte[] tramaFinal = null;
            if (mstrMessage != "")
            {
                Console.WriteLine("Mensaje del POS RECIBIDO:");
                mstrMessage = Encoding.ASCII.GetString(bytesRecibidos, 0, bytesRecibidos.Length);
                Console.WriteLine(mstrMessage);
                Console.WriteLine(Convert.ToBase64String(bytesRecibidos));
                byte[] longitud = new byte[] { bytesRecibidos[0], bytesRecibidos[1] };
                byte[] longitudSalida = new byte[] { 0, 61 };
                //byte[] longitudSalida = new byte[] { 0, 77 };
                byte[] transaccionFinanciera = new byte[] { bytesRecibidos[2] };
                byte[] destinoNII = new byte[] { bytesRecibidos[3], bytesRecibidos[4] };
                byte[] origen = new byte[] { bytesRecibidos[5], bytesRecibidos[6] };
                byte[] MTIrespuesta = new byte[] { bytesRecibidos[7], 16 };
                byte[] MTIconsulta = new byte[] { bytesRecibidos[7], bytesRecibidos[8] };
                byte[] bitmap = new byte[] { bytesRecibidos[9], bytesRecibidos[10], bytesRecibidos[11], bytesRecibidos[12], bytesRecibidos[13], bytesRecibidos[14], bytesRecibidos[15], bytesRecibidos[16] };
                byte[] longde63 = new byte[] { bytesRecibidos[17], bytesRecibidos[18] };
                byte[] longde63_envio = new byte[] { 0, 68 };
                byte[] numTerminal = new byte[] { bytesRecibidos[19], bytesRecibidos[20], bytesRecibidos[21], bytesRecibidos[22], bytesRecibidos[23], bytesRecibidos[24], bytesRecibidos[25], bytesRecibidos[26] };
                byte[] fecha = new byte[] { bytesRecibidos[27], bytesRecibidos[28], bytesRecibidos[29], bytesRecibidos[30] };
                byte[] hora = new byte[] { bytesRecibidos[31], bytesRecibidos[32], bytesRecibidos[33], bytesRecibidos[34] };
                byte[] sysTraceNumber = new byte[] { bytesRecibidos[35], bytesRecibidos[36], bytesRecibidos[37], bytesRecibidos[38], bytesRecibidos[39], bytesRecibidos[40] };
                string numeroCelular = mstrMessage.Substring(41, 8);
                string claveMovil = mstrMessage.Substring(49, 4);
                string monto = mstrMessage.Substring(57, 10);
                decimal montoDecimal = Decimal.Parse(monto) / 100;
                string currency = mstrMessage.Substring(69, 1);
                string commerce = mstrMessage.Substring(70, 6);
                byte[] reference_number = numTerminal.Concat(fecha).ToArray().Concat(hora).ToArray().Concat(sysTraceNumber).ToArray();
                string str_reference_number = Encoding.ASCII.GetString(reference_number);
                Console.WriteLine("numero celular:" + numeroCelular);
                Console.WriteLine("Clave movil:" + claveMovil);
                Console.WriteLine("monto:" + montoDecimal.ToString());
                Console.WriteLine("moneda:" + currency);
                Console.WriteLine("commerce:" + commerce);
                Console.WriteLine("str_reference_number" + str_reference_number);
                Console.WriteLine("Enviando a BISA...");
                RespuestaServicioBISA respuestaBisa = ListenerATC.LLamarServicioBisa(montoDecimal, commerce, currency, numeroCelular, claveMovil, str_reference_number);
                /*Program.RespuestaServicioBISA respuestaBisa = new Program.RespuestaServicioBISA();
                respuestaBisa.code = "00";
                respuestaBisa.cardNumber = "1234567890123456";
                respuestaBisa.expiration = "9922";*/
                Console.WriteLine("Codigo de respuesta:" + respuestaBisa.code);
                Console.WriteLine("Card Number:" + respuestaBisa.cardNumber);
                Console.WriteLine("ExpirationDate:" + respuestaBisa.expiration);
                Console.WriteLine(".....Fin de mensaje.....");
                Console.WriteLine("Armado de trama de respuesta");

                string header = mstrMessage.Substring(1, 39);
                string tramaNumeroTarjeta = (respuestaBisa.cardNumber == null) ? "0".PadLeft(16, '0') : respuestaBisa.cardNumber;
                string tramaFechaVencimiento = (respuestaBisa.expiration == null) ? "0000" : respuestaBisa.expiration;
                string tramaCodigoRespuesta = respuestaBisa.code.PadLeft(2, '0');

                //tramaFinal = longitudSalida.Concat(transaccionFinanciera).ToArray().Concat(destinoNII).ToArray().Concat(origen).ToArray().Concat(MTIrespuesta).ToArray().Concat(bitmap).ToArray().Concat(longde63_envio).ToArray().Concat(numTerminal).ToArray().Concat(fecha).ToArray().Concat(hora).ToArray().Concat(sysTraceNumber).ToArray();
                tramaFinal = longitudSalida.Concat(transaccionFinanciera).ToArray().Concat(origen).ToArray().Concat(destinoNII).ToArray().Concat(MTIrespuesta).ToArray().Concat(bitmap).ToArray().Concat(longde63_envio).ToArray().Concat(numTerminal).ToArray().Concat(fecha).ToArray().Concat(hora).ToArray().Concat(sysTraceNumber).ToArray();
                //tramaFinal = tramaFinal.Concat(new byte[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}).ToArray();
                string tramaEnvio = tramaNumeroTarjeta + tramaFechaVencimiento + tramaCodigoRespuesta;
                Console.WriteLine("Trama armada:" + tramaEnvio);
                mstrResponse = tramaEnvio;
            }
            bytesSent = tramaFinal.Concat(Encoding.ASCII.GetBytes(mstrResponse)).ToArray();
            //bytesSent = bytesSent.Concat(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }).ToArray();
            stream.Write(bytesSent, 0, bytesSent.Length);
            stream.Flush();
            Console.WriteLine("Respuesta enviada");
        }
    }
    public class RespuestaServicioBISA
    {
        public string referenceNumber { get; set; }
        public string cardNumber { get; set; }
        public string expiration { get; set; }
        public string code { get; set; }


    }

    public class ListenerATC
    {
        public static Action<string> EscribirMensaje { get; set; }
        static string localhost_ip;
        static string Get_ip_local_address()
        {
            System.Net.IPHostEntry host;
            string localIP = "?";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                }
            }
            return localIP;
        }
        static string output = string.Empty;

        private static void Escribir(string mensaje)
        {
            if (EscribirMensaje != null)
            {
                EscribirMensaje(mensaje);
            }
        }

        public static void IniciarListenerINAC()
        {
            TcpListener tcpListener = null;
            localhost_ip = Get_ip_local_address();
            IPAddress ipAddress = IPAddress.Parse(localhost_ip);
            //IPAddress ipAddress = Dns.GetHostEntry("localhost").AddressList[0];
            try
            {
                int puerto = Int32.Parse(System.Configuration.ConfigurationManager.AppSettings["puerto"]);
                tcpListener = new TcpListener(ipAddress, puerto);
                tcpListener.Start();
                output = "IP local:" + ipAddress + ",esperando conexion en puerto:" + puerto.ToString() + "...";
                Escribir(output);
                //Console.Write(output);
            }
            catch (Exception ex)
            {
                output = "Error:" + ex.ToString();
                Escribir(output);
                //Console.WriteLine(output);
            }
            while (true)
            {
                tcpListener.Start();
                Escribir("Esperando mensaje del POS...");
                //Console.WriteLine("Esperando mensaje del POS...");
                Thread.Sleep(10);
                TcpClient tcpClient = tcpListener.AcceptTcpClient();
                tcpClient.NoDelay = true;
                byte[] bytes = new byte[256];
                NetworkStream stream = tcpClient.GetStream();
                stream.Read(bytes, 0, bytes.Length);
                SocketHelper helper = new SocketHelper();
                Escribir("Mensaje recibido");
                //Console.Write("Mensaje recibido");
                helper.processMsg(tcpClient, stream, bytes);
                stream.Flush();
                tcpClient.Close();
                Escribir("Fin de ciclo");
            }

        }
        public static string RandomDigits(int length)
        {
            var random = new Random();
            string s = string.Empty;
            for (int i = 0; i < length; i++)
            {
                s = string.Concat(s, random.Next(10).ToString());
            }
            return s;
        }
        public static RespuestaServicioBISA LLamarServicioBisa(decimal amount, string commerce, string currency, string movilNumber, string smsPIN, string str_reference_number)
        {
            RespuestaServicioBISA respuesta = new RespuestaServicioBISA();
            ServicePointManager.ServerCertificateValidationCallback =
           delegate (object s, X509Certificate certificate,
                    X509Chain chain, SslPolicyErrors sslPolicyErrors)
           { return true; };

            BISAService.purchasePOSATCRequest requestPurchase = new BISAService.purchasePOSATCRequest();
            BISAService.aquaClient clienteBisa = new BISAService.aquaClient();
            clienteBisa.ClientCredentials.UserName.UserName = System.Configuration.ConfigurationManager.AppSettings["usuarioBISA_SOAP"];
            clienteBisa.ClientCredentials.UserName.Password = System.Configuration.ConfigurationManager.AppSettings["passwordBISA_SOAP"];
            Random rnd = new Random();
            string numeroAleatorio = RandomDigits(16);
            string referenceNumber = str_reference_number; //16 pos
            //commerce = commerce + numeroAleatorio;
            requestPurchase.amount = amount;
            requestPurchase.commerce = commerce;
            requestPurchase.currency = currency;
            requestPurchase.movilNumber = movilNumber;
            requestPurchase.referenceNumber = referenceNumber;
            requestPurchase.smsPIN = smsPIN;
            Escribir("montoRecibido:" + amount);
            Escribir("commerce:" + commerce);
            Escribir("currency:" + currency);
            Escribir("movilNumber:" + movilNumber);
            Escribir("referenceNumber:" + referenceNumber);
            //Escribir("smsPIN:" + smsPIN);

            BISAService.purchasePOSATCResponse responsePurchase = new BISAService.purchasePOSATCResponse();
            clienteBisa.ClientCredentials.ServiceCertificate.SetDefaultCertificate(StoreLocation.LocalMachine, StoreName.My, X509FindType.FindBySubjectName, System.Configuration.ConfigurationManager.AppSettings["nombreCertificadoBISA_SOAP"]);
            try
            {
                Escribir("Enviando Request a BISA...");
                clienteBisa.Open();
                responsePurchase = clienteBisa.purchasePOSATC(requestPurchase);
                respuesta.code = responsePurchase.code;
                respuesta.cardNumber = responsePurchase.cardNumber;
                respuesta.expiration = responsePurchase.expiration.ToString("yyMM", System.Globalization.CultureInfo.InvariantCulture);
                respuesta.referenceNumber = referenceNumber;
                Escribir("Respuesta exitosa recibida...");
                Escribir("cardNumber: "+ responsePurchase.cardNumber);
                Escribir("expiration: " + respuesta.expiration);
                Escribir("referenceNumber: " + respuesta.referenceNumber);
            }
            catch (Exception ex)
            {
                respuesta.cardNumber = "";
                respuesta.code = "99";
                respuesta.expiration = "";
                respuesta.referenceNumber = referenceNumber;
                Escribir("error en Respuesta BISA...:"+ex.Message);
               
            }

            return respuesta;
        }

    }
}

