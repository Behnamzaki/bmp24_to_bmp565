using System;
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


// By Behnam Zakizadeh (AVR64.com) @ 2022-01-31 [1400/11/11]
namespace bmp24_to_bmp565
{
    public partial class Form1 : Form
    {
        private FileStream input;
        int bmp24_width = 0;
        int bmp24_height = 0;
        int bmp24_fileSize = 0;


        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            button_convert.Enabled = false;
        }

        private void button_open_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "Bitmap 24-bit files (*.bmp)|*.bmp|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = openFileDialog.FileName;

                    textBox_fileName.Text = filePath;
                    pictureBox1.Load(filePath);

                    //textBox1.Clear();
                    //textBox2.Clear();

                    input = new FileStream(filePath, FileMode.Open, FileAccess.Read);

                    // skip first 2 byte 
                    for (int i = 0; i < 2; i++)
                    {
                        int x = input.ReadByte();
                    }

                    byte[] bytes = new byte[4];
                    // byte 2 to 5 contain file size
                    bytes[3] = (byte)input.ReadByte();
                    bytes[2] = (byte)input.ReadByte();
                    bytes[1] = (byte)input.ReadByte();
                    bytes[0] = (byte)input.ReadByte();

                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(bytes);

                    bmp24_fileSize = BitConverter.ToInt32(bytes, 0);

                    // skip 12 byte 
                    for (int i = 0; i < 12; i++)
                    {
                        int x = input.ReadByte();
                    }

                    // byte 18 to 21 contain width
                    bytes[3] = (byte)input.ReadByte();
                    bytes[2] = (byte)input.ReadByte();
                    bytes[1] = (byte)input.ReadByte();
                    bytes[0] = (byte)input.ReadByte();

                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(bytes);

                    bmp24_width = BitConverter.ToInt32(bytes, 0);


                    // byte 22 to 25 contain height
                    bytes[3] = (byte)input.ReadByte();
                    bytes[2] = (byte)input.ReadByte();
                    bytes[1] = (byte)input.ReadByte();
                    bytes[0] = (byte)input.ReadByte();

                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(bytes);

                    bmp24_height = BitConverter.ToInt32(bytes, 0);


                    label_w.Text = bmp24_width.ToString();
                    label_h.Text = bmp24_height.ToString();

                    button_convert.Enabled = true;
                }
            }
        }

        private void button_convert_Click(object sender, EventArgs e)
        {
            // calculate Padding for 4 byte alignment (https://en.wikipedia.org/wiki/BMP_file_format)
            int w3 = bmp24_width * 3;
            int res = w3 % 4;
            int res2 = 0;
            if (res == 3) res2 = 1;
            if (res == 1) res2 = 3;

            // skip to arrive to file contenet start byte [byte 54]
            for (int i = 0; i < 28; i++)
            {
                int x = input.ReadByte();
            }

            int[,] bmp24 = new int[bmp24_width * 3, bmp24_height];
            int[,] bmp24_reverse = new int[bmp24_width * 3, bmp24_height];
            int[,] bmp16 = new int[bmp24_width, bmp24_height];

            //read main bmp24 data to buffer
            for (int h1 = 0; h1 < bmp24_height; h1++)
            {
                for (int w1 = 0; w1 < bmp24_width * 3; w1++)
                {
                    bmp24[w1, h1] = input.ReadByte();
                }
                for (int k = 0; k < res2; k++)
                {
                    int x = input.ReadByte();
                }
            }

            //reverse data (because bmp24 files stored from buttom left to up)
            for (int h1 = 0; h1 < bmp24_height; h1++)
            {
                for (int w1 = 0; w1 < bmp24_width * 3; w1++)
                {
                    bmp24_reverse[w1, bmp24_height - 1 - h1] = bmp24[w1, h1];
                }
            }

            //now we have a matrix (bmp24_reverse) as same as bmp24 image



            //savfe file Dialog
            SaveFileDialog save = new SaveFileDialog();
            save.FileName = "image.h";
            save.Filter = "c heder file | *.h";
            if (save.ShowDialog() == DialogResult.OK)
            {
                StreamWriter writer = new StreamWriter(save.OpenFile());

                // convert bmp to rgb565 and write to file:

                writer.WriteLine("#ifndef _IMAGE_H");
                writer.WriteLine("#define _IMAGE_H");
                writer.WriteLine("const uint16_t image1 [" + bmp24_height + "][" + bmp24_width + "] PROGMEM = { // h x w");
                for (int h1 = 0; h1 < bmp24_height; h1++)
                {
                    writer.Write("{");
                    for (int w1 = 0; w1 < bmp24_width * 3;)
                    {
                        int blue = (bmp24_reverse[w1, h1] >> 3);
                        w1++;
                        int green = (bmp24_reverse[w1, h1] >> 2);
                        w1++;
                        int red = (bmp24_reverse[w1, h1] >> 3);
                        w1++;

                        ushort f = (ushort)(((ushort)red << 11) | ((ushort)green << 5) | ((ushort)blue));

                        if (w1 == bmp24_width * 3) // last element in each width
                        {
                            writer.Write("0x" + f.ToString("X4"));
                        }
                        else
                        {
                            writer.Write("0x" + f.ToString("X4") + ",");
                        }
                    }
                    if(h1==bmp24_height-1) // last element in end of file
                    {
                        writer.WriteLine("}");
                    }
                    else
                    {
                        writer.WriteLine("},");
                    }
                    
                }

                writer.WriteLine("};");
                writer.WriteLine("#endif");

                writer.Dispose();
                writer.Close();
                MessageBox.Show("Completed!", "Completed", MessageBoxButtons.OK);
            }




        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            //System.Diagnostics.Process.Start("https://www.avr64.com");
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "http://avr64.com",
                UseShellExecute = true
            };
            Process.Start(psi);

        }
    }
}
