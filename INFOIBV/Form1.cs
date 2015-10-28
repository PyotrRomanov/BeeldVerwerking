using System;
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
        int objectCount;
        Color[,] objectMap;
        private List<PictureObject> Objects = new List<PictureObject>();

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
                    pictureBox1.Image = (Image)InputImage;                 // Display input image
            }
        }

        private void applyButton_Click(object sender, EventArgs e)
        {
            if (InputImage == null) return;                                 // Get out if no input image
            if (OutputImage != null) OutputImage.Dispose();                 // Reset output image
            OutputImage = new Bitmap(InputImage.Size.Width, InputImage.Size.Height); // Create new output image
            Color[,] Image = new Color[InputImage.Size.Width, InputImage.Size.Height]; // Create array to speed-up operations (Bitmap functions are very slow)
            objectCount = 0;
            objectMap = new Color[InputImage.Size.Width, InputImage.Size.Height];
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

            Image = WindowSlice(Image);
            
            for(int a = 0; a < 1; a++ ){
                //Image = Erode(Image);
            }
            

            for (int a = 0; a < 11; a++ )
            {
                //Image = Dilate(Image);
            }

            MarkObjects(Image);

            CalculatePerimeters(objectMap);

            CalculateAreas(objectMap);

            CalculateCompactness();

            for (int k = 0; k < Objects.Count; k++)
            {
                //Console.WriteLine("De compactness van " + k + "is: " + Objects[k].Compactness);
                if ((1 / Objects[k].Compactness) < 0.8)
                {
                    Console.WriteLine("Object " + k + " moet weg");
                    DeleteObject(Objects[k].Objectnr);
                }
            }

            //==========================================================================================

            // Copy array to output Bitmap
            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    OutputImage.SetPixel(x, y, objectMap[x, y]);               // Set the pixel color at coordinate (x,y)
                }
            }

            pictureBox2.Image = (Image)OutputImage;                         // Display output image
            progressBar.Visible = false;                                    // Hide progress bar
            Console.WriteLine(objectCount);
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (OutputImage == null) return;                                // Get out if no output image
            if (saveImageDialog.ShowDialog() == DialogResult.OK)
                OutputImage.Save(saveImageDialog.FileName);                 // Save the output image
        }

        Color[,] WindowSlice(Color[,] image) { 
            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    // Windowslicing
                    Color pixelColor = image[x, y];                         // Get the pixel color at coordinate (x,y)
                    int greyValue = (pixelColor.R + pixelColor.G + pixelColor.B) / 3;
                    Color updatedColor;
                    if (greyValue > 160)
                    {
                        updatedColor = Color.FromArgb(255, 255, 255);
                    }
                    else
                    {
                        updatedColor = Color.FromArgb(0, 0, 0);
                    }
                    image[x, y] = updatedColor;                             // Set the new pixel color at coordinate (x,y)

                    progressBar.PerformStep();                              // Increment progress bar
                }

            }
            return image;
        }

        Color[,] Dilate(Color[,] image)
        {
            Color[,] newImage = new Color[InputImage.Size.Width, InputImage.Size.Height];
            byte[,] dilateKernel = new byte[5, 5]
                    {
                        {0,0,1,0,0},
                        {0,1,1,1,0},
                        {1,1,1,1,1},
                        {0,1,1,1,0},
                        {0,0,1,0,0}
                    };

            for (int x = 3; x < InputImage.Size.Width - 3; x++)
            {
                for (int y = 3; y < InputImage.Size.Height - 3; y++)
                {
                    Color newColor = Color.FromArgb(0, 0, 0);
                    bool foundColor = false;
                    for (int i = 0; i < 5 && !foundColor; i++)
                    {
                        for (int j = 0; j < 5 && !foundColor; j++)
                        {
                            if (image[x - 2 + i, y - 2 + j].R * dilateKernel[i, j] > 0)
                            {
                                newColor = Color.FromArgb(255, 255, 255);
                                foundColor = true;
                            }
                        }
                    }
                    newImage[x, y] = newColor;
                }
            }
            return newImage;
        }

        Color[,] Erode(Color[,] image)
        {
            Color[,] newImage = new Color[InputImage.Size.Width, InputImage.Size.Height];
            byte[,] erodeKernel = new byte[3, 3]
                    {
                        {1,1,1},
                        {1,1,1},
                        {1,1,1}
                    };
            
            for (int x = 1; x < InputImage.Size.Width - 1; x++)
            {
                for (int y = 1; y < InputImage.Size.Height - 1; y++)
                {
                    int counter = 0;

                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            if (image[x - 1 + i, y - 1 + j].R > 0 && erodeKernel[i, j] == 1)
                            {
                                counter++;
                            }
                        }
                    }
                    if (counter == 9)
                    {
                        newImage[x, y] = Color.FromArgb(255, 255, 255);
                    }
                    else {
                        newImage[x, y] = Color.FromArgb(0, 0, 0);
                    }
                }
            }
            return newImage;
        }

        void MarkObjects(Color[,] image) 
        {
            Color[,] newImage = new Color[InputImage.Size.Width, InputImage.Size.Height];
            for (int x = 1; x < InputImage.Size.Width - 1; x++)
            {
                for (int y = 1; y < InputImage.Size.Height - 1; y++)
                { 
                    if(image[x,y].R > 0 && objectMap[x,y].R == 0){
                        objectCount++;
                        PictureObject p = new PictureObject(objectCount);
                        Objects.Add(p);
                        CheckNeighbouringPixels(image, x,y);
                    }
                }
            }
        }

        void CheckNeighbouringPixels(Color[,] image, int x, int y) {
            if (image[x, y].R > 0 && objectMap[x, y].R == 0) {
                objectMap[x, y] = Color.FromArgb(objectCount, objectCount, objectCount);
                CheckNeighbouringPixels(image, x + 1, y);
                CheckNeighbouringPixels(image, x - 1, y);
                CheckNeighbouringPixels(image, x, y + 1);
                CheckNeighbouringPixels(image, x, y - 1);
            }
        }

        public void CalculatePerimeters(Color[,] whiteObject)
        {
            for (int a = 0; a < Objects.Count; a++)
            {
                int count = 0;
                byte[,] perimeterKernel = new byte[3, 3]
            {
                {1,1,1},
                {1,1,1},
                {1,1,1}
            };
                for (int x = 3; x < InputImage.Size.Width - 3; x++)
                {
                    for (int y = 3; y < InputImage.Size.Height - 3; y++)
                    {
                        int tempcount = 0;
                        for (int i = 0; i < 3; i++)
                        {
                            for (int j = 0; j < 3; j++)
                            {
                                if (whiteObject[x - 1 + i, y - 1 + j].R == Objects[a].Objectnr)
                                {
                                    tempcount++;
                                }
                            }
                        }

                        if (tempcount < 9 && tempcount > 0 && whiteObject[x, y].R == Objects[a].Objectnr)
                        {
                            count++;
                        }
                    }
                }
                Objects[a].Perimeter = count;
            }
        }

        public void CalculateAreas(Color[,] whiteObject)
        {
            for (int i = 0; i < Objects.Count; i++)
            {
                int area = 0;
                for (int x = 0; x < InputImage.Size.Width; x++)
                {
                    for (int y = 0; y < InputImage.Size.Height; y++)
                    {
                        if (whiteObject[x, y].R == Objects[i].Objectnr)
                        {
                            area++;
                        }
                    }
                }

                Objects[i].Area = area;
            }
        }

        private void CalculateCompactness()
        {
            for (int i = 0; i < Objects.Count; i++)
            {
                double compactness = 0;
                compactness = (Objects[i].Perimeter * Objects[i].Perimeter) / (4 * Math.PI * Objects[i].Area);

                Objects[i].Compactness = compactness;
            }
        }

        public class PictureObject
        {
            public int Objectnr, Perimeter, Area;
            public double Compactness;

            public PictureObject(int Objectnr)
            {
                this.Objectnr = Objectnr;
            }
        }

        private void DeleteObject(int toBeRemoved) {
            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    if (objectMap[x, y].R == toBeRemoved)
                    {
                        objectMap[x, y] = Color.FromArgb(255, 255, 255);
                    }
                    else {
                        objectMap[x, y] = Color.FromArgb(0, 0, 0);
                    }
                }
            }
        }

    }
}
