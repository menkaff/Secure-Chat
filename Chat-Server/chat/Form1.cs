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
        
        /////////////////////
        //////RSA
        private TcpListener server;
        private TcpClient client;
        private BinaryFormatter formatter = new BinaryFormatter();
        private RSACryptoServiceProvider enc, dec;
        //////////////////////
        /////diffie area
        public static byte[] alicePublicKey;
        public static byte[] aliceKey;
        ECDiffieHellmanCng alice = new ECDiffieHellmanCng();



        private void SetText(string text)
        {
            if (this.listBox1.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.listBox1.Items.Add(text);
            }
        }
        private string combo()
        {
            if (this.comboBox1.InvokeRequired)
            {
                combocheck d = new combocheck(combo);
                this.Invoke(d, new object[] { });

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
            //txtClient1IP.Text = GetLocalIP();


        }
        /////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////
        ///////////////////


        private byte[] key;     // the seed key. size will be 4 * keySize from ctor.
        private byte[,] Sbox;   // Substitution box
        private byte[,] iSbox;  // inverse Substitution box 
        private byte[,] w;      // key schedule array. 
        private byte[,] Rcon;   // Round constants.
        private byte[,] State;  // State matrix
        public void Cipher(byte[] input, byte[] output)  // encipher 16-bit input
        {
        int Nb=128;         // block size in 32-bit words.  Always 4 for AES.  (128 bits).
        int Nr=10;         // number of rounds. 10, 12, 14.
            this.State = new byte[4, Nb];  // always [4,4]
            for (int i = 0; i < (4 * Nb); ++i)
            {
                this.State[i % 4, i / 4] = input[i];
            }

            AddRoundKey(0);

            for (int round = 1; round <= (Nr - 1); ++round)  // main round loop
            {
                SubBytes();
                ShiftRows();
                MixColumns();
                AddRoundKey(round);
            }  // main round loop

            SubBytes();
            ShiftRows();
            AddRoundKey(Nr);

            for (int i = 0; i < (4 * Nb); ++i)
            {
                output[i] = this.State[i % 4, i / 4];
            }

        }  // Cipher()

        public void InvCipher(byte[] input, byte[] output)  // decipher 16-bit input
        {
            int Nb = 128;         // block size in 32-bit words.  Always 4 for AES.  (128 bits).
            int Nr = 10;         // number of rounds. 10, 12, 14.
            this.State = new byte[4, Nb];  // always [4,4]
            for (int i = 0; i < (4 * Nb); ++i)
            {
                this.State[i % 4, i / 4] = input[i];
            }

            AddRoundKey(Nr);

            for (int round = Nr - 1; round >= 1; --round)  // main round loop
            {
                InvShiftRows();
                InvSubBytes();
                AddRoundKey(round);
                InvMixColumns();
            }  // end main round loop for InvCipher

            InvShiftRows();
            InvSubBytes();
            AddRoundKey(0);

            for (int i = 0; i < (4 * Nb); ++i)
            {
                output[i] = this.State[i % 4, i / 4];
            }

        }  // InvCipher()

       

        private void BuildSbox()
        {
            this.Sbox = new byte[16, 16] {  // populate the Sbox matrix
    /* 0     1     2     3     4     5     6     7     8     9     a     b     c     d     e     f */
    /*0*/  {0x63, 0x7c, 0x77, 0x7b, 0xf2, 0x6b, 0x6f, 0xc5, 0x30, 0x01, 0x67, 0x2b, 0xfe, 0xd7, 0xab, 0x76},
    /*1*/  {0xca, 0x82, 0xc9, 0x7d, 0xfa, 0x59, 0x47, 0xf0, 0xad, 0xd4, 0xa2, 0xaf, 0x9c, 0xa4, 0x72, 0xc0},
    /*2*/  {0xb7, 0xfd, 0x93, 0x26, 0x36, 0x3f, 0xf7, 0xcc, 0x34, 0xa5, 0xe5, 0xf1, 0x71, 0xd8, 0x31, 0x15},
    /*3*/  {0x04, 0xc7, 0x23, 0xc3, 0x18, 0x96, 0x05, 0x9a, 0x07, 0x12, 0x80, 0xe2, 0xeb, 0x27, 0xb2, 0x75},
    /*4*/  {0x09, 0x83, 0x2c, 0x1a, 0x1b, 0x6e, 0x5a, 0xa0, 0x52, 0x3b, 0xd6, 0xb3, 0x29, 0xe3, 0x2f, 0x84},
    /*5*/  {0x53, 0xd1, 0x00, 0xed, 0x20, 0xfc, 0xb1, 0x5b, 0x6a, 0xcb, 0xbe, 0x39, 0x4a, 0x4c, 0x58, 0xcf},
    /*6*/  {0xd0, 0xef, 0xaa, 0xfb, 0x43, 0x4d, 0x33, 0x85, 0x45, 0xf9, 0x02, 0x7f, 0x50, 0x3c, 0x9f, 0xa8},
    /*7*/  {0x51, 0xa3, 0x40, 0x8f, 0x92, 0x9d, 0x38, 0xf5, 0xbc, 0xb6, 0xda, 0x21, 0x10, 0xff, 0xf3, 0xd2},
    /*8*/  {0xcd, 0x0c, 0x13, 0xec, 0x5f, 0x97, 0x44, 0x17, 0xc4, 0xa7, 0x7e, 0x3d, 0x64, 0x5d, 0x19, 0x73},
    /*9*/  {0x60, 0x81, 0x4f, 0xdc, 0x22, 0x2a, 0x90, 0x88, 0x46, 0xee, 0xb8, 0x14, 0xde, 0x5e, 0x0b, 0xdb},
    /*a*/  {0xe0, 0x32, 0x3a, 0x0a, 0x49, 0x06, 0x24, 0x5c, 0xc2, 0xd3, 0xac, 0x62, 0x91, 0x95, 0xe4, 0x79},
    /*b*/  {0xe7, 0xc8, 0x37, 0x6d, 0x8d, 0xd5, 0x4e, 0xa9, 0x6c, 0x56, 0xf4, 0xea, 0x65, 0x7a, 0xae, 0x08},
    /*c*/  {0xba, 0x78, 0x25, 0x2e, 0x1c, 0xa6, 0xb4, 0xc6, 0xe8, 0xdd, 0x74, 0x1f, 0x4b, 0xbd, 0x8b, 0x8a},
    /*d*/  {0x70, 0x3e, 0xb5, 0x66, 0x48, 0x03, 0xf6, 0x0e, 0x61, 0x35, 0x57, 0xb9, 0x86, 0xc1, 0x1d, 0x9e},
    /*e*/  {0xe1, 0xf8, 0x98, 0x11, 0x69, 0xd9, 0x8e, 0x94, 0x9b, 0x1e, 0x87, 0xe9, 0xce, 0x55, 0x28, 0xdf},
    /*f*/  {0x8c, 0xa1, 0x89, 0x0d, 0xbf, 0xe6, 0x42, 0x68, 0x41, 0x99, 0x2d, 0x0f, 0xb0, 0x54, 0xbb, 0x16} };

        }  // BuildSbox() 

        private void BuildInvSbox()
        {
            this.iSbox = new byte[16, 16] {  // populate the iSbox matrix
    /* 0     1     2     3     4     5     6     7     8     9     a     b     c     d     e     f */
    /*0*/  {0x52, 0x09, 0x6a, 0xd5, 0x30, 0x36, 0xa5, 0x38, 0xbf, 0x40, 0xa3, 0x9e, 0x81, 0xf3, 0xd7, 0xfb},
    /*1*/  {0x7c, 0xe3, 0x39, 0x82, 0x9b, 0x2f, 0xff, 0x87, 0x34, 0x8e, 0x43, 0x44, 0xc4, 0xde, 0xe9, 0xcb},
    /*2*/  {0x54, 0x7b, 0x94, 0x32, 0xa6, 0xc2, 0x23, 0x3d, 0xee, 0x4c, 0x95, 0x0b, 0x42, 0xfa, 0xc3, 0x4e},
    /*3*/  {0x08, 0x2e, 0xa1, 0x66, 0x28, 0xd9, 0x24, 0xb2, 0x76, 0x5b, 0xa2, 0x49, 0x6d, 0x8b, 0xd1, 0x25},
    /*4*/  {0x72, 0xf8, 0xf6, 0x64, 0x86, 0x68, 0x98, 0x16, 0xd4, 0xa4, 0x5c, 0xcc, 0x5d, 0x65, 0xb6, 0x92},
    /*5*/  {0x6c, 0x70, 0x48, 0x50, 0xfd, 0xed, 0xb9, 0xda, 0x5e, 0x15, 0x46, 0x57, 0xa7, 0x8d, 0x9d, 0x84},
    /*6*/  {0x90, 0xd8, 0xab, 0x00, 0x8c, 0xbc, 0xd3, 0x0a, 0xf7, 0xe4, 0x58, 0x05, 0xb8, 0xb3, 0x45, 0x06},
    /*7*/  {0xd0, 0x2c, 0x1e, 0x8f, 0xca, 0x3f, 0x0f, 0x02, 0xc1, 0xaf, 0xbd, 0x03, 0x01, 0x13, 0x8a, 0x6b},
    /*8*/  {0x3a, 0x91, 0x11, 0x41, 0x4f, 0x67, 0xdc, 0xea, 0x97, 0xf2, 0xcf, 0xce, 0xf0, 0xb4, 0xe6, 0x73},
    /*9*/  {0x96, 0xac, 0x74, 0x22, 0xe7, 0xad, 0x35, 0x85, 0xe2, 0xf9, 0x37, 0xe8, 0x1c, 0x75, 0xdf, 0x6e},
    /*a*/  {0x47, 0xf1, 0x1a, 0x71, 0x1d, 0x29, 0xc5, 0x89, 0x6f, 0xb7, 0x62, 0x0e, 0xaa, 0x18, 0xbe, 0x1b},
    /*b*/  {0xfc, 0x56, 0x3e, 0x4b, 0xc6, 0xd2, 0x79, 0x20, 0x9a, 0xdb, 0xc0, 0xfe, 0x78, 0xcd, 0x5a, 0xf4},
    /*c*/  {0x1f, 0xdd, 0xa8, 0x33, 0x88, 0x07, 0xc7, 0x31, 0xb1, 0x12, 0x10, 0x59, 0x27, 0x80, 0xec, 0x5f},
    /*d*/  {0x60, 0x51, 0x7f, 0xa9, 0x19, 0xb5, 0x4a, 0x0d, 0x2d, 0xe5, 0x7a, 0x9f, 0x93, 0xc9, 0x9c, 0xef},
    /*e*/  {0xa0, 0xe0, 0x3b, 0x4d, 0xae, 0x2a, 0xf5, 0xb0, 0xc8, 0xeb, 0xbb, 0x3c, 0x83, 0x53, 0x99, 0x61},
    /*f*/  {0x17, 0x2b, 0x04, 0x7e, 0xba, 0x77, 0xd6, 0x26, 0xe1, 0x69, 0x14, 0x63, 0x55, 0x21, 0x0c, 0x7d} };

        }  // BuildInvSbox()

        private void BuildRcon()
        {
            this.Rcon = new byte[11, 4] { {0x00, 0x00, 0x00, 0x00},  
                                   {0x01, 0x00, 0x00, 0x00},
                                   {0x02, 0x00, 0x00, 0x00},
                                   {0x04, 0x00, 0x00, 0x00},
                                   {0x08, 0x00, 0x00, 0x00},
                                   {0x10, 0x00, 0x00, 0x00},
                                   {0x20, 0x00, 0x00, 0x00},
                                   {0x40, 0x00, 0x00, 0x00},
                                   {0x80, 0x00, 0x00, 0x00},
                                   {0x1b, 0x00, 0x00, 0x00},
                                   {0x36, 0x00, 0x00, 0x00} };
        }  // BuildRcon()

        private void AddRoundKey(int round)
        {

            for (int r = 0; r < 4; ++r)
            {
                for (int c = 0; c < 4; ++c)
                {
                    this.State[r, c] = (byte)((int)this.State[r, c] ^ (int)w[(round * 4) + c, r]);
                }
            }
        }  // AddRoundKey()

        private void SubBytes()
        {
            for (int r = 0; r < 4; ++r)
            {
                for (int c = 0; c < 4; ++c)
                {
                    this.State[r, c] = this.Sbox[(this.State[r, c] >> 4), (this.State[r, c] & 0x0f)];
                }
            }
        }  // SubBytes

        private void InvSubBytes()
        {
            for (int r = 0; r < 4; ++r)
            {
                for (int c = 0; c < 4; ++c)
                {
                    this.State[r, c] = this.iSbox[(this.State[r, c] >> 4), (this.State[r, c] & 0x0f)];
                }
            }
        }  // InvSubBytes

        private void ShiftRows()
        {
            int Nb = 128;         // block size in 32-bit words.  Always 4 for AES.  (128 bits).
          
            byte[,] temp = new byte[4, 4];
            for (int r = 0; r < 4; ++r)  // copy State into temp[]
            {
                for (int c = 0; c < 4; ++c)
                {
                    temp[r, c] = this.State[r, c];
                }
            }

            for (int r = 1; r < 4; ++r)  // shift temp into State
            {
                for (int c = 0; c < 4; ++c)
                {
                    this.State[r, c] = temp[r, (c + r) % Nb];
                }
            }
        }  // ShiftRows()

        private void InvShiftRows()
        {
            int Nb = 128;         // block size in 32-bit words.  Always 4 for AES.  (128 bits).
            byte[,] temp = new byte[4, 4];
            for (int r = 0; r < 4; ++r)  // copy State into temp[]
            {
                for (int c = 0; c < 4; ++c)
                {
                    temp[r, c] = this.State[r, c];
                }
            }
            for (int r = 1; r < 4; ++r)  // shift temp into State
            {
                for (int c = 0; c < 4; ++c)
                {
                    this.State[r, (c + r) % Nb] = temp[r, c];
                }
            }
        }  // InvShiftRows()

        private void MixColumns()
        {
            byte[,] temp = new byte[4, 4];
            for (int r = 0; r < 4; ++r)  // copy State into temp[]
            {
                for (int c = 0; c < 4; ++c)
                {
                    temp[r, c] = this.State[r, c];
                }
            }


        }  // MixColumns

        private void InvMixColumns()
        {
            byte[,] temp = new byte[4, 4];
            for (int r = 0; r < 4; ++r)  // copy State into temp[]
            {
                for (int c = 0; c < 4; ++c)
                {
                    temp[r, c] = this.State[r, c];
                }
            }


        }  // InvMixColumns

        //////////////////
        ////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////
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
            try
            {
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

                    tdsAlg.Key = key_maker.GetBytes(tdsAlg.KeySize / 8);
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
                MessageBox.Show("ridimdec");
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
        ////////////////////////////////////////////// diffie area
        private static byte[] D_Encrypt(string secretMessage,byte[] Key)
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
        private static string D_Decrypt(byte[] encryptedMessage,byte[] Key)
        {
            try
            {
                string message=null;
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
                else if (combo() == "DES")
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
          //  foreach (IPAddress ip in host.AddressList)
           // {
             //   if (ip.AddressFamily == AddressFamily.InterNetwork)
               //     return ip.ToString();
            //}
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
                SocketAsyncEventArgs s = new SocketAsyncEventArgs();

                Listen(Convert.ToInt32(txtClient2Port.Text));
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

                string encryptedMessage = EncryptText(txtmessage.Text, txtKey.Text);
                msg = ens.GetBytes(encryptedMessage);
                sk.Send(msg);
                listBox1.Items.Add("" + txtmessage.Text);
                txtmessage.Clear();


            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }
        }
        ///////////////////
        ///////send public key



        public void Listen(int port)
        {
            server = new TcpListener(IPAddress.Parse(GetLocalIP()), port);
            server.Start();
            client = server.AcceptTcpClient();
            GetConfirmation();

            Confirm();
            GetPublicKey();
            SendPublicKey();
            Get_D();
            Send_D();

            GetConfirmation();
        }

        private void SendSpecial(Envelope ev)
        {
            BinaryFormatter f = new BinaryFormatter();
            f.Serialize(client.GetStream(), ev);
        }

        private void Confirm()
        {
            SendSpecial(new Envelope("SM_CONFIRM", ""));
        }

        private void GetConfirmation()
        {
            Envelope ev = (Envelope)formatter.Deserialize(client.GetStream());//get SM_HELO reply		            
            if (ev.Name != "SM_CONFIRM")
                client.Close();
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

        public void Send_D()
        {


                alice.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
                alice.HashAlgorithm = CngAlgorithm.Sha256;
                alicePublicKey = alice.PublicKey.ToByteArray();
                //alice.SignatureAlgorithm=
                //CngKey k = CngKey.Import(alicePublicKey, CngKeyBlobFormat.EccPublicBlob);
                SendSpecial(new Envelope("SM_PUBLICKEY_D", alicePublicKey));
                //aliceKey = alice.DeriveKeyMaterial(CngKey.Import(bobPublicKey, CngKeyBlobFormat.EccPublicBlob));

            
        }

        public void Get_D()
        {
            Envelope ev;
            ev = (Envelope)formatter.Deserialize(client.GetStream());//get SM_HELO reply		
            if (ev.Name == "SM_PUBLICKEY_D")
            {
         


                    aliceKey = alice.DeriveKeyMaterial(CngKey.Import((byte[])ev.Data, CngKeyBlobFormat.EccPublicBlob));

                
            }
            else
            {
                client.Close();
            }
            
        }















    }
}
