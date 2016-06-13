using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Security.Cryptography;
using System.Collections;
using SecureSocket;
using System.Runtime.Serialization.Formatters.Binary;
namespace WindowsFormsApplication4
{
    public partial class Form1 : Form
    {
        Socket sk;
        EndPoint epLocal, epRemote;
        delegate void SetTextCallback(string text);
        delegate string combocheck();
        //////////////////////////////////////public key
        private TcpClient client;
        private RSACryptoServiceProvider dec, enc;
        private BinaryFormatter formatter = new BinaryFormatter();

        /////diffie area
        public static byte[] alicePublicKey;
        public static byte[] aliceKey;
        ECDiffieHellmanCng alice = new ECDiffieHellmanCng();


        private void SetText(string text)
        {
            //tell windows we are interested in drawing items in ListBox on our own
            

            //tell windows we are interested in providing  item size
            this.listBox1.MeasureItem +=
              new System.Windows.Forms.MeasureItemEventHandler(this.MeasureItemHandler);
            if (this.listBox1.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.listBox1.Items.Add(text);
                if (this.listBox1.Items.Count % 2 == 0)
                {

                    this.listBox1.ForeColor = Color.BlueViolet;

                }
                else
                {
                    this.listBox1.ForeColor = Color.Brown;
                }

            }
        }
        private string combo()
        {
            if (this.comboBox1.InvokeRequired)
            {
                combocheck d = new combocheck(combo);
                this.Invoke(d, new object[] {});
                return this.comboBox1.Text;
            }
            else
            {
               return this.comboBox1.Text;
            }
        }
        public Form1()
        {
            InitializeComponent();
            sk = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sk.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
           // txtClient1IP.Text = GetLocalIP();


            
        }


        private void MeasureItemHandler(object sender, MeasureItemEventArgs e)
        {
            e.ItemHeight = 22;
        }
        // in taabe yek araye az byteha daryaft mikone va ba estefade az algorithm AES ramzesh mikone
        public byte[] AES_Encrypt(byte[] bytesToBeEncrypted, byte[] passwordBytes)
        {
            try
            {
                byte[] encryptedBytes = null;


                byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

                using (MemoryStream ms = new MemoryStream())
                {
                    using (RijndaelManaged AES = new RijndaelManaged())
                    {
                        AES.KeySize = 256;
                        AES.BlockSize = 128;

                        var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                        AES.Key = key.GetBytes(AES.KeySize / 8);
                        AES.IV = key.GetBytes(AES.BlockSize / 8);

                        AES.Mode = CipherMode.CBC;

                        using (var cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
                            cs.Close();
                        }
                        encryptedBytes = ms.ToArray();
                    }
                }

                return encryptedBytes;
            }
            catch (Exception exc)
            {

                MessageBox.Show(exc.ToString());
                return null;
            }
        }
        // in taabe yek araye az bytehaye ramz shode tavasote AES daryaft mikone va ramz goshaeesh mikone

        public byte[] AES_Decrypt(byte[] bytesToBeDecrypted, byte[] passwordBytes)
        {
            try
            {
                byte[] decryptedBytes = null;


                byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };



                using (MemoryStream ms = new MemoryStream())
                {
                    using (RijndaelManaged AES = new RijndaelManaged())
                    {
                        AES.KeySize = 256;
                        AES.BlockSize = 128;

                        var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                        AES.Key = key.GetBytes(AES.KeySize / 8);
                        AES.IV = key.GetBytes(AES.BlockSize / 8);

                        AES.Mode = CipherMode.CBC;

                        using (var cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(bytesToBeDecrypted, 0, bytesToBeDecrypted.Length);
                            cs.Close();
                        }
                        decryptedBytes = ms.ToArray();
                    }
                }

                return decryptedBytes;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
                return null;
            }
        }
        //////////
        //////////////////////////// DES AREA
        static byte[] DES_Encrypt(string plainText, byte[] Key, byte[] IV)
        {
            try{
            // Check arguments. 
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");
            byte[] encrypted;
            byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            // Create an TripleDESCryptoServiceProvider object 
            // with the specified key and IV. 
            using (TripleDESCryptoServiceProvider tdsAlg = new TripleDESCryptoServiceProvider())
            {
                
                var key_maker = new Rfc2898DeriveBytes(Key, saltBytes, 1000);
                tdsAlg.KeySize = 128;
                tdsAlg.BlockSize = 64;

                tdsAlg.Key = key_maker.GetBytes(tdsAlg.KeySize/8);
                tdsAlg.IV = key_maker.GetBytes(tdsAlg.BlockSize / 8);

                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = tdsAlg.CreateEncryptor(tdsAlg.Key, tdsAlg.IV);

                // Create the streams used for encryption. 
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {

                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }


            // Return the encrypted bytes from the memory stream. 
            return encrypted;
                        }
            catch (Exception exc)
            {
                MessageBox.Show("ridimenc");
                MessageBox.Show(exc.ToString());
                return null;
            }

        }
        /////////////////////// des decrypt
        static string DES_Decrypt(byte[] cipherText, byte[] Key, byte[] IV)
        {
            try
            {
                // Check arguments. 
                if (cipherText == null || cipherText.Length <= 0)
                    throw new ArgumentNullException("cipherText");
                if (Key == null || Key.Length <= 0)
                    throw new ArgumentNullException("Key");
                if (IV == null || IV.Length <= 0)
                    throw new ArgumentNullException("Key");

                // Declare the string used to hold 
                // the decrypted text. 
                string plaintext = null;
                byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
                // Create an TripleDESCryptoServiceProvider object 
                // with the specified key and IV. 
                using (TripleDESCryptoServiceProvider tdsAlg = new TripleDESCryptoServiceProvider())
                {
                    var key_maker = new Rfc2898DeriveBytes(Key, saltBytes, 1000);
                    tdsAlg.KeySize = 128;
                    tdsAlg.BlockSize = 64;

                    tdsAlg.Key = key_maker.GetBytes(tdsAlg.KeySize / 8);
                    tdsAlg.IV = key_maker.GetBytes(tdsAlg.BlockSize / 8);

                    // Create a decrytor to perform the stream transform.
                    ICryptoTransform decryptor = tdsAlg.CreateDecryptor(tdsAlg.Key, tdsAlg.IV);

                    // Create the streams used for decryption. 
                    using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {

                                // Read the decrypted bytes from the decrypting stream 
                                // and place them in a string.
                                plaintext = srDecrypt.ReadToEnd();
                            }
                        }
                    }

                }

                return plaintext;
            }
            catch (Exception exc)
            {
                MessageBox.Show("mdec");
                MessageBox.Show(exc.ToString());
                return null;
            }
        }
        ////////////////////////////////////////
        //////////////////////////////////////////// RSA AREA
        static public byte[] RSAEncrypt(byte[] DataToEncrypt, RSAParameters RSAKeyInfo, bool DoOAEPPadding)
        {
            try
            {
                byte[] encryptedData;
                //Create a new instance of RSACryptoServiceProvider. 
                using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
                {

                    //Import the RSA Key information. This only needs 
                    //toinclude the public key information.
                    RSA.ImportParameters(RSAKeyInfo);

                    //Encrypt the passed byte array and specify OAEP padding.   
                    //OAEP padding is only available on Microsoft Windows XP or 
                    //later.  
                    encryptedData = RSA.Encrypt(DataToEncrypt, DoOAEPPadding);
                }
                return encryptedData;
            }
            //Catch and display a CryptographicException   
            //to the console. 
            catch (CryptographicException e)
            {
                MessageBox.Show(e.ToString());

                return null;
            }

        }

        static public byte[] RSADecrypt(byte[] DataToDecrypt, RSAParameters RSAKeyInfo, bool DoOAEPPadding)
        {
            try
            {
                byte[] decryptedData;
                //Create a new instance of RSACryptoServiceProvider. 
                using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
                {
                    //Import the RSA Key information. This needs 
                    //to include the private key information.
                    RSA.ImportParameters(RSAKeyInfo);

                    //Decrypt the passed byte array and specify OAEP padding.   
                    //OAEP padding is only available on Microsoft Windows XP or 
                    //later.  
                    decryptedData = RSA.Decrypt(DataToDecrypt, DoOAEPPadding);
                }
                return decryptedData;
            }
            //Catch and display a CryptographicException   
            //to the console. 
            catch (CryptographicException e)
            {
                MessageBox.Show(e.ToString());

                return null;
            }

        }
        ///////////////////////////////////////////////////
        ///////////////////////////////////////////////////
        ////////////////////////////////////////////// diffie area
        ///////////////////////////////////////////////////
        ////////////////////////////////////////////// diffie area
        private static byte[] D_Encrypt(string secretMessage, byte[] Key)
        {
            try
            {
                byte[] encryptedMessage;
                byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

                using (Aes aes = new AesCryptoServiceProvider())
                {
                    aes.BlockSize = 128;
                    var key_maker = new Rfc2898DeriveBytes(Key, saltBytes, 1000);
                    aes.IV = key_maker.GetBytes(aes.BlockSize / 8);
                    aes.Key = aliceKey;


                    // Encrypt the message 
                    using (MemoryStream ciphertext = new MemoryStream())
                    using (CryptoStream cs = new CryptoStream(ciphertext, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        byte[] plaintextMessage = Encoding.UTF8.GetBytes(secretMessage);
                        cs.Write(plaintextMessage, 0, plaintextMessage.Length);
                        cs.Close();
                        encryptedMessage = ciphertext.ToArray();
                    }
                }
                return encryptedMessage;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
                return null;
            }
        }
        private static string D_Decrypt(byte[] encryptedMessage, byte[] Key)
        {
            try
            {
                string message = null;
                byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
                using (Aes aes = new AesCryptoServiceProvider())
                {
                    aes.BlockSize = 128;
                    var key_maker = new Rfc2898DeriveBytes(Key, saltBytes, 1000);
                    aes.IV = key_maker.GetBytes(aes.BlockSize / 8);
                    aes.Key = aliceKey;

                    // Decrypt the message 
                    using (MemoryStream plaintext = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(plaintext, aes.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(encryptedMessage, 0, encryptedMessage.Length);
                            cs.Close();
                            message = Encoding.UTF8.GetString(plaintext.ToArray());

                        }
                    }
                }
                return message;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
                return null;
            }

        }
        ///////////////////////////
        // in taabe' hengam ersal payam farakhni mishavad. peygham va kelid ro be shekle string az karbar migire
        //va be arayei az byteha tabdil mikone va sepas taabe AES_Encrypt ro farakhooni mikone
        public string EncryptText(string input, string password)
        {
            try
            {
                byte[] bytesToBeEncrypted = Encoding.UTF8.GetBytes(input);
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

                ////
                int select = 0;
                this.Invoke((MethodInvoker)delegate()
                {
                    select = comboBox2.SelectedIndex;
                });
                if (select == 2)
                    passwordBytes = MD5.Create().ComputeHash(passwordBytes);
                else if (select == 1)
                    passwordBytes = SHA512.Create().ComputeHash(passwordBytes);
                else 
                    passwordBytes = SHA256.Create().ComputeHash(passwordBytes);



                //////////// 
                byte[] bytesEncrypted = aliceKey;
                string result;
                if (combo() == "AES")
                {
                    bytesEncrypted = AES_Encrypt(bytesToBeEncrypted, passwordBytes);
                }
                else if (combo() == "TripleDES")
                {
                    bytesEncrypted = DES_Encrypt(input, passwordBytes, passwordBytes);
                }
                else if (combo() == "RSA")
                {
                    bytesEncrypted = RSAEncrypt(bytesToBeEncrypted, enc.ExportParameters(false), false);
                }
                else if (combo() == "DiffieHellman")
                {
                    bytesEncrypted = D_Encrypt(input, passwordBytes);
                }
                else
                {
                    bytesEncrypted = AES_Encrypt(bytesToBeEncrypted, passwordBytes);
                    //bytesEncrypted = RSAEncrypt(bytesToBeEncrypted, enc.ExportParameters(false), false);
                }


                result = Convert.ToBase64String(bytesEncrypted);



                return result;


            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
                return "";
            }
        }
        // in taabe hengame daryafte payam farakhani mishavad. peygham ramz shode va kelid ra be soorate string daryaft
        //mikonad. oonaro tabdil be arayei az byteha mikone va sepas ba farakhanie taabe AES_Decrypr ramzgoshaii mikone.
        public string DecryptText(string input, string password)
        {
            try
            {
                input = input.Replace("\0", "");
                byte[] bytesToBeDecrypted = Convert.FromBase64String(input);
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                string text = "not working th combo delegate!!!";
                this.Invoke((MethodInvoker)delegate()
                {
                    text = comboBox1.Text;
                });
                ////
                int select = 0;
                this.Invoke((MethodInvoker)delegate()
                {
                    select = comboBox2.SelectedIndex;
                });
                if (select == 2)
                    passwordBytes = MD5.Create().ComputeHash(passwordBytes);
                else if (select == 1)
                    passwordBytes = SHA512.Create().ComputeHash(passwordBytes);
                else
                    passwordBytes = SHA256.Create().ComputeHash(passwordBytes);


                string result;

                byte[] bytesDecrypted = bytesToBeDecrypted;
                if (text == "AES")
                {
                    result = Encoding.UTF8.GetString(AES_Decrypt(bytesToBeDecrypted, passwordBytes));
                }
                else if (text == "DES")
                {
                    result = DES_Decrypt(bytesToBeDecrypted, passwordBytes, passwordBytes);
                }
                else if (text == "RSA")
                {
                    result = Encoding.UTF8.GetString(RSADecrypt(bytesToBeDecrypted, dec.ExportParameters(true), false));
                }
                else if (text == "DiffieHellman")
                {
                    result = D_Decrypt(bytesToBeDecrypted, passwordBytes);
                }
                else
                {
                    //result = Encoding.UTF8.GetString(RSADecrypt(bytesToBeDecrypted, dec.ExportParameters(true), false));
                    result = Encoding.UTF8.GetString(AES_Decrypt(bytesToBeDecrypted, passwordBytes));
                }


                return result;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
                return "";
            }
        }

        // in taabe' IP'e computer ro bar migardoone, agar computer be shabakei vasl nabud IP'e 127.0.0.1 ro bar migardoone
        //ke ip'e locale karte shabakast
        private string GetLocalIP()
        {
            IPHostEntry host;
            host = Dns.GetHostEntry(Dns.GetHostName());
           // foreach (IPAddress ip in host.AddressList)
           // {
             //   if (ip.AddressFamily == AddressFamily.InterNetwork)
              //      return ip.ToString();
           // }
            return "127.0.0.1";

        }

        // in taabe' baraye avalin bar dar taab'e start ejra mishe va bad az oon be tore peyvaste khodesh ro farakhuni
        //mikone (tavasote dastoore akhar) va karesh ine ke age peyghami resid oono daryaft va chap kone
        private void MessageCallBack(IAsyncResult aResult)
        {
            try
            {
                int size = sk.EndReceiveFrom(aResult, ref epRemote);
                if (size > 0)
                {
                    byte[] recievedData;
                    recievedData = (byte[])aResult.AsyncState;
                    ASCIIEncoding end = new ASCIIEncoding();
                    string recievedMessage = end.GetString(recievedData);
                    recievedMessage = recievedMessage.Trim();
                    string decryptedMessage = DecryptText(recievedMessage, txtKey.Text);
                   
                    SetText("" + decryptedMessage);
                }
                byte[] buffer = new byte[1500];
                sk.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref epRemote, new AsyncCallback(MessageCallBack), buffer);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }
        }
       // in taabe baraye injade connection beyne 2 computer neveshte shode ast. 2 EndPoint misaze ke har kodoome ye ip
        //darand va ye port. yeki baraye computere maghsad, yeki ham barayae computere manba
        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                epLocal = new IPEndPoint(IPAddress.Parse(GetLocalIP()), Convert.ToInt32(txtClient1Port.Text));
            sk.Bind(epLocal);
                SocketAsyncEventArgs s=new SocketAsyncEventArgs();

                Connect(Convert.ToInt32(txtClient1Port.Text));
                epRemote = new IPEndPoint(IPAddress.Parse(GetLocalIP()), Convert.ToInt32(txtClient2Port.Text));
                sk.Connect(epRemote);
                byte[] buffer = new byte[1500];
                sk.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref epRemote, new AsyncCallback(MessageCallBack), buffer);
                
                btnStart.Text = "متصل";
                btnStart.Enabled = false;
                btnsend.Enabled = true;
                txtmessage.Focus();
            }
               catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }
            
        }
 // in Taabe baraye ersale payam neveshte shode ast. payam ro ramz karde va ersal mikone.
        private void btnsend_Click(object sender, EventArgs e)
        {
            try
            {
                ASCIIEncoding ens = new ASCIIEncoding();
                byte[] msg;

               string encryptedMessage=  EncryptText(txtmessage.Text, txtKey.Text);
                msg=ens.GetBytes(encryptedMessage);
                sk.Send(msg);
                listBox1.Items.Add("" + txtmessage.Text);
                txtmessage.Clear();


            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }
        }

        //////////////////////////////
        ////////////public key


        public void Connect( int port)
        {
            TcpClient tcp = new TcpClient();
            tcp.Connect(IPAddress.Parse(GetLocalIP()), port);
                
            client = tcp;
            Confirm();
            GetConfirmation();
            SendPublicKey();
            GetPublicKey();
            Send_D();
            Get_D();

            Confirm();
        }

        private void SendPublicKey()
        {
            dec = new RSACryptoServiceProvider();
            SendSpecial(new Envelope("SM_PUBLICKEY", dec.ExportParameters(false)));
        }

        private void GetPublicKey()
        {
            Envelope ev;
            ev = (Envelope)formatter.Deserialize(client.GetStream());//get SM_HELO reply		
            if (ev.Data is RSAParameters)
            {
                enc = new RSACryptoServiceProvider();
                enc.ImportParameters((RSAParameters)ev.Data);
            }
            else
            {
                client.Close();
            }
        }

        private void Confirm()
        {
            SendSpecial(new Envelope("SM_CONFIRM", ""));
        }

        private void GetConfirmation()
        {
            Envelope ev = (Envelope)formatter.Deserialize(client.GetStream());
            if (ev.Name != "SM_CONFIRM")
                client.Close();
        }

        private void SendSpecial(Envelope ev)
        {
            BinaryFormatter f = new BinaryFormatter();
            f.Serialize(client.GetStream(), ev);
        }

        public void Send_D()
        {


                alice.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
                alice.HashAlgorithm = CngAlgorithm.Sha256;
                alicePublicKey = alice.PublicKey.ToByteArray();
                //CngKey k = CngKey.Import(bobPublicKey, CngKeyBlobFormat.EccPublicBlob);
                SendSpecial(new Envelope("SM_PUBLICKEY_D", alicePublicKey));
                

            
        }

        public void Get_D()
        {
            Envelope ev;
            ev = (Envelope)formatter.Deserialize(client.GetStream());//get SM_HELO reply		
            if (ev.Name == "SM_PUBLICKEY_D")
            {

                    aliceKey = alice.DeriveKeyMaterial(CngKey.Import((byte[])ev.Data, CngKeyBlobFormat.EccPublicBlob));
                    //aliceKey = alice.DeriveKeyMaterial(CngKey.Import(bobPublicKey, CngKeyBlobFormat.EccPublicBlob));
                
            }
            else
            {
                client.Close();
            }

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }


    }
}
