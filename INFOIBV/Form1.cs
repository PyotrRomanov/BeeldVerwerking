﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace INFOIBV
{
    public partial class INFOIBV : Form
    {
        private Bitmap InputImage;
        private Bitmap OutputImage;

        public INFOIBV()
        {
            InitializeComponent();
        }

        private void LoadImageButton_Click(object sender, EventArgs e)
        {
           if (openImageDialog.ShowDialog() == DialogResult.OK)             // Open File Dialog
            {
                string file = openImageDialog.FileName;                     // Get the file name
                imageFileName.Text = file;                                  // Show file name
                if (InputImage != null) InputImage.Dispose();               // Reset image
                InputImage = new Bitmap(file);                              // Create new Bitmap from file
                if (InputImage.Size.Height <= 0 || InputImage.Size.Width <= 0 ||
                    InputImage.Size.Height > 512 || InputImage.Size.Width > 512) // Dimension check
                    MessageBox.Show("Error in image dimensions (have to be > 0 and <= 512)");
                else
                    pictureBox1.Image = (Image) InputImage;                 // Display input image
            }
        }

        private void applyButton_Click(object sender, EventArgs e)
        {
            if (InputImage == null) return;                                 // Get out if no input image
            if (OutputImage != null) OutputImage.Dispose();                 // Reset output image
            OutputImage = new Bitmap(InputImage.Size.Width, InputImage.Size.Height); // Create new output image
            Color[,] Image = new Color[InputImage.Size.Width, InputImage.Size.Height]; // Create array to speed-up operations (Bitmap functions are very slow)
            Color[,] newImage = new Color[InputImage.Size.Width, InputImage.Size.Height];

            // Setup progress bar
            progressBar.Visible = true;
            progressBar.Minimum = 1;
            progressBar.Maximum = InputImage.Size.Width * InputImage.Size.Height;
            progressBar.Value = 1;
            progressBar.Step = 1;

            // Copy input Bitmap to array            
            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    Image[x, y] = InputImage.GetPixel(x, y);                // Set pixel color in array at (x,y)
                }
            }

            //==========================================================================================
            // TODO: include your own code here
            // example: create a negative image
            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    // Windowslicing
                    Color pixelColor = Image[x, y];                         // Get the pixel color at coordinate (x,y)
                    int greyValue = (pixelColor.R + pixelColor.G + pixelColor.B) / 3;
                    Color updatedColor;
                    if (greyValue > 160)
                    {
                        updatedColor = Color.FromArgb(255, 255, 255);
                    }
                    else {
                        updatedColor = Color.FromArgb(0, 0, 0); 
                    }
                    Image[x, y] = updatedColor;                             // Set the new pixel color at coordinate (x,y)

                    progressBar.PerformStep();                              // Increment progress bar
                }

            }
            for (int x = 3; x < InputImage.Size.Width-3; x++)
            {
                for (int y = 3; y < InputImage.Size.Height-3; y++)
                {
                    // Dilate
                    byte[,] dilateKernel = new byte[5, 5]
                    {
                        {0,0,1,0,0},
                        {0,1,1,1,0},
                        {1,1,1,1,1},
                        {0,1,1,1,0},
                        {0,0,1,0,0}
                    };
                    Color newColor = Color.FromArgb(0,0,0);
                    bool foundColor = false;
                    for (int i = 0; i < 5 && !foundColor; i++)
                    {
                        for (int j = 0; j < 5 && !foundColor; j++)
                        {;
                            if (Image[x - 2 + i, y - 2 + j].R * dilateKernel[i, j] > 0)
                            {
                                newColor = Color.FromArgb(255, 255, 255);
                                foundColor = true;
                            }
                        }
                    }
                    newImage[x, y] = newColor;
                }
            }
            //==========================================================================================

            // Copy array to output Bitmap
            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    OutputImage.SetPixel(x, y, newImage[x, y]);               // Set the pixel color at coordinate (x,y)
                }
            }
            
            pictureBox2.Image = (Image)OutputImage;                         // Display output image
            progressBar.Visible = false;                                    // Hide progress bar
        }
        
        private void saveButton_Click(object sender, EventArgs e)
        {
            if (OutputImage == null) return;                                // Get out if no output image
            if (saveImageDialog.ShowDialog() == DialogResult.OK)
                OutputImage.Save(saveImageDialog.FileName);                 // Save the output image
        }

    }
}
