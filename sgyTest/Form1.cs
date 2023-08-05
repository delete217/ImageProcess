using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace sgyTest
{
    public partial class Form1 : Form
    {
        string filePath;

        float[,] SEGYDATA;
        float[,] graySegyData = new float[1751, 6388];


        int numTraces = 6388;
        int numSamplesPerTrace = 1751;       

        int imageWidth = 1000;
        int imageHeight = 700;

        float scale = 2;

        // 设置缩放因子
        float scaleX = 1; // X轴缩放因子
        float scaleY = 1; // Y轴缩放因子

        //记录鼠标位置
        private Point lastMousePos = Point.Empty;

        //originalImage  用于存储原始图像，originalImage2是变换中图像
        Bitmap originalImage = new Bitmap(1000, 700);
        Bitmap originalImage2 = new Bitmap(1000, 700);
        Bitmap adjustedImage = new Bitmap(1000, 700);
        Bitmap filteredImage = new Bitmap(1000, 700);
        Bitmap edgedImage = new Bitmap(1000, 700);

        //灰度图
        Bitmap grayImage = new Bitmap(1000, 700);
        int[] grayHistogram = new int[256];

        public Form1()
        {
            InitializeComponent();
            // 使用 SelectedIndex 设置默认选项
            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
            comboBox3.SelectedIndex = 0; 
            comboBox4.SelectedIndex = 0;

            label5.BringToFront();
        }

        //以画布中心轴 反转
        private void button1_Click(object sender, EventArgs e)
        {
           
            if (comboBox1.SelectedIndex == 0)
            {
                originalImage2.RotateFlip(RotateFlipType.RotateNoneFlipX);

            }
            else
            {
                originalImage2.RotateFlip(RotateFlipType.RotateNoneFlipY);

            }
            pictureBox1.Image = originalImage2;

        }

        private void VisualizeSeismicData(float[,] seismicData)
        {

            float scaleX = (float)1751 / imageWidth;
            float scaleY = (float)6388 / imageHeight;

            float maxValue = seismicData.Cast<float>().Max();
            float minValue = seismicData.Cast<float>().Min();

            // 创建一个 Bitmap 对象来存储图像

            for (int x = 0; x < imageWidth; x++)
            {
                for (int y = 0; y < imageHeight; y++)
                {
                    int originalX = (int)(x * scaleX);
                    int originalY = (int)(y * scaleY);

                    // 裁剪，确保不超出原始地震数据范围
                    originalX = Math.Max(0, Math.Min(originalX, 1750));
                    originalY = Math.Max(0, Math.Min(originalY, 6387));

                    float amplitude = seismicData[originalY, originalX];
                    Color pixelColor = MapAmplitudeToColor(amplitude, maxValue, minValue);

                    originalImage.SetPixel(x, y, pixelColor);
                }
            }


            // 将 Bitmap 显示在 PictureBox 控件上（假设你有一个名为 pictureBox 的 PictureBox 控件）
            pictureBox1.Image = originalImage;
        }

        // 根据地震数据振幅值映射像素颜色的函数（根据需要设置颜色映射规则）
        private Color MapAmplitudeToColor(float amplitude,float maxValue,float minValue)
        {

            // 定义颜色映射的起始颜色和结束颜色
            Color startColor = Color.Blue;  // 负振幅颜色
            Color endColor = Color.Red;    // 正振幅颜色

            // 将振幅值映射到颜色范围
            float t = (amplitude - minValue) / (maxValue - minValue);
            int r = (int)(startColor.R + t * (endColor.R - startColor.R));
            int g = (int)(startColor.G + t * (endColor.G - startColor.G));
            int b = (int)(startColor.B + t * (endColor.B - startColor.B));

            // 创建并返回映射后的颜色
            return Color.FromArgb(r, g, b);


        }

        private void ParseTraceHeader(byte[] traceHeaderBytes, out float xCoordinate, out float yCoordinate)
        {
            byte[] xCoordinateArray = new byte[4];
            byte[] yCoordinateArray = new byte[4];

            //2字节 ---》  int16  /// 4字节  ---》 int32
            Buffer.BlockCopy(traceHeaderBytes, 72, xCoordinateArray, 0, 4);
            Buffer.BlockCopy(traceHeaderBytes, 76, yCoordinateArray, 0, 4);


            xCoordinate = BitConverter.ToUInt32(xCoordinateArray, 0);
            yCoordinate = BitConverter.ToUInt32(yCoordinateArray, 0);

        }

        //图像放大功能
        private void button2_Click(object sender, EventArgs e)
        {
            // 设置缩放因子
            scaleX = scaleX * scale; // X轴缩放因子
            scaleY = scaleY * scale; // Y轴缩放因子

            // 计算缩放后的图像尺寸
            int newWidth = (int)(originalImage2.Width * scaleX);
            int newHeight = (int)(originalImage2.Height * scaleY);

            // 创建缩放后的位图
            Bitmap scaledImage = new Bitmap(newWidth, newHeight);

            // 使用像素级别的矩阵变换进行图像放缩
            using (Graphics graphics = Graphics.FromImage(scaledImage))
            {
                graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.DrawImage(originalImage2, new Rectangle(0, 0, newWidth, newHeight));
            }
            textBox1.Text = Convert.ToString(scaleX);
            //便于错切
            originalImage2 = scaledImage;
            pictureBox1.Image = originalImage2;
        }

        // 图像缩小功能
        private void button3_Click(object sender, EventArgs e)
        {
            // 设置缩放因子
            scaleX = scaleX / scale; // X轴缩放因子
            scaleY = scaleY / scale; // Y轴缩放因子

            // 计算缩放后的图像尺寸
            int newWidth = (int)(originalImage2.Width * scaleX);
            int newHeight = (int)(originalImage2.Height * scaleY);

            // 创建缩放后的位图
            Bitmap scaledImage = new Bitmap(newWidth, newHeight);

            // 使用像素级别的矩阵变换进行图像放缩
            using (Graphics graphics = Graphics.FromImage(scaledImage))
            {
                graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.DrawImage(originalImage2, new Rectangle(0, 0, newWidth, newHeight));
            }
            textBox1.Text = Convert.ToString(scaleX);

            //便于错切
            originalImage2 = scaledImage;
            pictureBox1.Image = originalImage2;
        }


        private void button5_Click(object sender, EventArgs e)
        {
            if (filePath == null)
            {
                MessageBox.Show("请选择.sgy文件!");
            }
            else
            {
                label5.Text = Convert.ToString("地震剖面图重建中...");
                label5.Refresh();

                byte[] TotalsegyData = File.ReadAllBytes(filePath);
                byte[] segyData = new byte[TotalsegyData.Length - 3600];

                Buffer.BlockCopy(TotalsegyData, 3600, segyData, 0, TotalsegyData.Length - 3600);
                //创建一个二维数组存储地震数据
                float[,] seismicData = new float[numSamplesPerTrace, numTraces];

                int bytePerTrace = 4 * numSamplesPerTrace;
                for (int traceIndex = 0; traceIndex < numTraces; traceIndex++)
                {
                    int traceHeaderSize = 240;

                    int traceHeaderOffset = traceIndex * (bytePerTrace + traceHeaderSize);
                    int traceOffset = traceHeaderOffset + traceHeaderSize;
                    //提取道集头信息
                    byte[] traceHeaderBytes = new byte[traceHeaderSize];
                    Buffer.BlockCopy(segyData, traceHeaderOffset, traceHeaderBytes, 0, traceHeaderSize);

                    //解析当前道集信息
                    float xCoordinate;
                    float yCoordinate;
                    ParseTraceHeader(traceHeaderBytes, out xCoordinate, out yCoordinate);

                    //提取当前道集地震信息
                    byte[] traceBytes = new byte[bytePerTrace];
                    Buffer.BlockCopy(segyData, traceOffset, traceBytes, 0, bytePerTrace);

                    //解析道集地震信息
                    for (int sampleIndex = 0; sampleIndex < numSamplesPerTrace; sampleIndex++)
                    {
                        byte[] sampleBytes = new byte[4];
                        Buffer.BlockCopy(traceBytes, sampleIndex * 4, sampleBytes, 0, 4);
                        float sampleValue = BitConverter.ToSingle(sampleBytes, 0);

                        // 将解析得到的采样点值存储到二维数组中
                        seismicData[sampleIndex, traceIndex] = sampleValue;
                        //seismicData[sampleIndex, traceIndex] = sampleValue;
                    }

                }
                SEGYDATA = seismicData;
                
                VisualizeSeismicData2(seismicData);

                
            }
        }

        //读取seismicData数据  重建地震剖面图
        private void VisualizeSeismicData2(float[,] seismicData)
        {
            float scaleX = (float)numTraces / imageWidth;
            float scaleY = (float)numSamplesPerTrace / imageHeight;
            
            float maxValue = seismicData.Cast<float>().Max();
            float minValue = seismicData.Cast<float>().Min();


            // 创建一个 Bitmap 对象来存储图像
            for (int x = 0; x < imageWidth; x++)
            {
                for (int y = 0; y < imageHeight; y++)
                {
                    int originalX = (int)(x * scaleX);
                    int originalY = (int)(y * scaleY);

                    // 裁剪，确保不超出原始地震数据范围
                    originalX = Math.Max(0, Math.Min(originalX, 6387));
                    originalY = Math.Max(0, Math.Min(originalY, 1750));

                    float amplitude = seismicData[originalY, originalX];
                    Color pixelColor = MapAmplitudeToColor(amplitude,maxValue,minValue);
                    Color grayColor = MapAmplitudeToGrayScale(amplitude, maxValue, minValue);
                    originalImage2.SetPixel(x, y, pixelColor);
                    grayImage.SetPixel(x, y, grayColor);
                }
            }
            originalImage = originalImage2;
            filteredImage = originalImage2;
            adjustedImage = grayImage;
            // 将 Bitmap 显示在 PictureBox 控件上（假设你有一个名为 pictureBox 的 PictureBox 控件）
            label5.Text = Convert.ToString("地震剖面图重建完成");
            label5.Refresh();
            pictureBox1.BringToFront();
            pictureBox1.Image = originalImage2;
        }


        private void button7_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = null;
            originalImage2 = originalImage;
            pictureBox1.Image = originalImage2;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            float shearArgsX = float.Parse(textBox3.Text);
            float shearArgsY = float.Parse(textBox2.Text);

            // 计算错切后的图像尺寸
            int newWidth;
            int newHeight;
            if (shearArgsX > 0)
            {
                newWidth = originalImage2.Width + (int)(originalImage.Height * shearArgsX);
            }
            else
            {
                newWidth = originalImage2.Width;
            }

            if (shearArgsY > 0)
            {
                newHeight = originalImage2.Height + (int)(originalImage.Width * shearArgsY);
            }
            else
            {
                newHeight = originalImage2.Height;
            }

            Bitmap shearedMap = new Bitmap(newWidth, newHeight);
            Graphics g = Graphics.FromImage(shearedMap);

            Matrix shearMatrix = new Matrix();
            shearMatrix.Shear(shearArgsX, shearArgsY); // 水平错切

            g.Transform = shearMatrix;
            g.DrawImage(originalImage2, new Point(0, 0));

            originalImage2 = shearedMap;
            pictureBox1.Image = originalImage2;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            // 创建 OpenFileDialog 对象
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // 设置对话框的过滤器，只显示 .segy 文件
            openFileDialog.Filter = "SGY 文件 (*.sgy)|*.sgy";

            // 打开对话框并获取用户选择的文件路径
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                filePath = openFileDialog.FileName;
                // 在这里可以使用 filePath 进行后续处理，比如读取文件内容等
            }
        }

        //绘制灰度值统计直方图
        private void button8_Click(object sender, EventArgs e)
        {
            
        }

        //线性变换
        private void button9_Click(object sender, EventArgs e)
        {
            label7.Text = Convert.ToString("线性变换执行中...");
            label7.Refresh();
            if (comboBox2.SelectedIndex == 2)
            {
                IncreaseBright();
            }else if(comboBox2.SelectedIndex == 3)
            {
                IncreaseContrast();
            }
            else if(comboBox2.SelectedIndex == 4)
            {
                ReverseImage();
            }else if(comboBox2.SelectedIndex == 5)
            {
                transformImage();
            }else if(comboBox2.SelectedIndex == 6)
            {
                equalizeImage();
            }else if(comboBox2.SelectedIndex == 1)
            {
                calGrayImage();
            }
            else
            {
                //adjustedImage = grayImage;
                pictureBox1.Image = grayImage;
                label7.Text = Convert.ToString("原始灰度数据");
            }

            //originalImage2 = adjustedImage;
        }

        //灰度直方图统计
        private void calGrayImage()
        {
            //灰度直方图
            //创建灰度图数组
            int[] histogram = new int[256];

            //初始化灰度图数组
            for (int i = 0; i < histogram.Length; i++)
            {
                histogram[i] = 0;
            }

            //统计像素值
            for (int x = 0; x < originalImage2.Width; x++)
            {
                for (int y = 0; y < originalImage2.Height; y++)
                {
                    int grayValue = adjustedImage.GetPixel(x, y).R;
                    graySegyData[x, y] = grayValue;
                    histogram[grayValue]++;
                    grayHistogram[grayValue]++;
                }
            }

            //创建灰度统计图像
            Bitmap grayBmp = new Bitmap(1024, 700);
            int scale = 50;
            Graphics g = Graphics.FromImage(grayBmp);

            //绘制坐标轴
            g.DrawLine(new Pen(Color.Black, 2), new Point(10, 200), new Point(10, 650)); //Y轴
            // 定义字体和颜色
            Font font = new Font("Arial", 10);
            Brush brush = Brushes.Black;
            Pen p = new Pen(Color.Black, 2);

            g.DrawString("灰度直方图统计", new Font("Arial ", 25), brush, new Point(370, 200));

            //绘制Y轴刻度
            for (int j = 0; j < 6; j++)
            {
                g.DrawLine(p, new Point(10, 650 - j * scale), new Point(30, 650 - j * scale)); //Y轴

                // 定义文本内容和位置
                string text = Convert.ToString(j * 6500);
                Point textPosition = new Point(40, 650 - j * scale - 10);
                if (!text.Equals("0"))
                {
                    // 绘制文本
                    g.DrawString(text, font, brush, textPosition);
                }
            }
            //绘制X轴
            for (int i = 0; i < 5; i++)
            {
                g.DrawLine(p, new Point(i * 256 + 10, 650), new Point(i * 256 + 10, 630));
                string text = Convert.ToString(i * 64);

                Point textPosition = new Point(i * 256 + 5, 665);

                // 绘制文本
                g.DrawString(text, font, brush, textPosition);


            }

            g.DrawLine(new Pen(Color.Black, 2), new Point(10, 650), new Point(pictureBox1.Width, 650));
            // 绘制直方图
            for (int i = 0; i < 256; i++)
            {
                float barHeight = histogram[i] * 5 / 650; // 缩放直方图的高度
                g.DrawLine(new Pen(Color.Red, 4), i * 4 + 10, 650, i * 4 + 10, 650 - barHeight); // 绘制直方图柱状图
            }

            pictureBox1.Image = grayBmp;
            label7.Text = Convert.ToString("灰度直方图统计");
        }

        private void equalizeImage()
        {
            Bitmap equalizedImage = new Bitmap(grayImage.Width, grayImage.Height);

            //计算灰度值累积分布
            int totalPixels = grayImage.Width * grayImage.Height;
            float[] cdf = new float[256];
            cdf[0] = grayHistogram[0] / (float)totalPixels;
            for(int i = 1; i < 256; i++)
            {
                cdf[i] = cdf[i - 1] + grayHistogram[i] / (float)totalPixels;
            }

            //灰度映射到新图像
            for(int x = 0; x < grayImage.Width; x++)
            {
                for(int y = 0; y < grayImage.Height; y++)
                {
                    int originValue = adjustedImage.GetPixel(x, y).R;
                    int newValue = (int)(cdf[originValue] * 255);
                    Color newGray = Color.FromArgb(newValue, newValue, newValue);
                    equalizedImage.SetPixel(x, y, newGray);
                }
            }
            adjustedImage = equalizedImage;
            pictureBox1.Image = equalizedImage;
            label7.Text = Convert.ToString("直方图均衡化");

        }

        //LU窗口变换
        private void transformImage()
        {
            int windowWidth = 50;
            int windowCenter = 100;
            Bitmap tranformedImage = new Bitmap(grayImage.Width, grayImage.Height);

            for(int x = 0;x < grayImage.Width; x++)
            {
                for(int y = 0;y < grayImage.Height; y++)
                {
                    int grayValue = adjustedImage.GetPixel(x, y).R;
                    if(grayValue < windowCenter - windowWidth / 2)
                    {
                        grayValue = 0;
                    }else if(grayValue > windowCenter + windowWidth / 2)
                    {
                        grayValue = 255;
                    }
                    else
                    {
                        grayValue = (int)((grayValue - windowCenter) * (255 / windowWidth) + 127.5);
                    }

                    Color newGray = Color.FromArgb(grayValue, grayValue, grayValue);
                    tranformedImage.SetPixel(x, y, newGray);
                }
            }
            adjustedImage = tranformedImage;
            //grayImage = tranformedImage;
            pictureBox1.Image = tranformedImage;
            label7.Text = Convert.ToString("窗位/150/窗宽/100");
        }

        //图片灰度值反转
        private void ReverseImage()
        {
            Bitmap reverseBmp = new Bitmap(1000, 700);
            for (int i = 0; i < grayImage.Width; i++)
            {
                for (int j = 0; j < grayImage.Height; j++)
                {
                    Color value = adjustedImage.GetPixel(i, j);

                    //反转灰度值
                    int newValue = 255 - value.R;
                    Color newGray = Color.FromArgb(newValue, newValue, newValue);
                    reverseBmp.SetPixel(i, j, newGray);
                }
            }
            adjustedImage = reverseBmp;
            pictureBox1.Image = reverseBmp;
            label7.Text = Convert.ToString("灰度值反转");
        }

        //提高图片对比度
        private void IncreaseContrast()
        {
            float contrastFactor = 1.5f;
            Bitmap adjustImage = new Bitmap(1000, 700);
            for (int y = 0; y < adjustedImage.Height; y++)
            {
                for (int x = 0; x < adjustedImage.Width; x++)
                {
                    Color originalColor = adjustedImage.GetPixel(x, y);
                    int newGrayValue = (int)(originalColor.R * contrastFactor);
                    newGrayValue = Math.Max(0, Math.Min(255, newGrayValue));
                    Color newColor = Color.FromArgb(newGrayValue, newGrayValue, newGrayValue);
                    adjustImage.SetPixel(x, y, newColor);
                }
            }

            adjustedImage = adjustImage;

            pictureBox1.Image = adjustImage;
            label7.Text = Convert.ToString("提高对比度");
        }

        //提高图片亮度
        private void IncreaseBright()
        {
            Bitmap brightImage = new Bitmap(grayImage.Width, grayImage.Height);
            for (int i = 0; i < grayImage.Width; i++)
            {
                for (int j = 0; j < grayImage.Height; j++)
                {
                    Color value = grayImage.GetPixel(i, j);

                    //提升2倍灰度值
                    int newValue = value.R * 2;
                    if (newValue < 256)
                    {
                        Color newGray = Color.FromArgb(newValue, newValue, newValue);
                        brightImage.SetPixel(i, j, newGray);
                    }

                }
            }
            adjustedImage = brightImage;
            pictureBox1.Image = brightImage;
            label7.Text = Convert.ToString("亮度提高2倍");
        }

        private Color MapAmplitudeToGrayScale(float amplitude, float maxValue, float minValue)
        {
            // 将振幅值映射到灰度范围（0-255）
            int grayValue = (int)(255 * (amplitude - minValue) / (maxValue - minValue));

            // 确保灰度值在合法范围内（0-255）
            grayValue = Math.Max(0, Math.Min(255, grayValue));

            // 创建并返回映射后的灰度颜色
            return Color.FromArgb(grayValue, grayValue, grayValue);
        }


        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
           
        }

        private void button8_Click_1(object sender, EventArgs e)
        {

            label6.Text = Convert.ToString("滤波执行中...");
            label6.Refresh();

            if (comboBox3.SelectedIndex == 0)
            {
                GaussFilter();
            }else if(comboBox3.SelectedIndex == 1)
            {
                AdaptiveFilter();
            }
            else
            {
                MiddleFilter();
            }

            originalImage2 = filteredImage;
        }

        private void MiddleFilter()
        {
            Bitmap filterImage = new Bitmap(originalImage.Width, originalImage.Height);
            int neighborhoodSize = 3; // 局域取 3 * 3

            for (int x = 0; x < originalImage.Width; x++)
            {
                for (int y = 0; y < originalImage.Height; y++)
                {
                    Color newColor = CalMiddleColor(adjustedImage, x, y, neighborhoodSize);
                    filterImage.SetPixel(x, y, newColor);
                }
            }
            filteredImage = filterImage;
            pictureBox1.Image = filterImage;

            label6.Text = Convert.ToString("滤波执行完成...");
            label6.Refresh();
        }

        private Color CalMiddleColor(Bitmap originalImage, int x, int y, int neighborhoodSize)
        {
            int halfSize = neighborhoodSize / 2;

            int totalR = 0;
            int totalG = 0;
            int totalB = 0;
            int count = 0;

            for (int i = -halfSize; i <= halfSize; i++)
            {
                for (int j = -halfSize; j <= halfSize; j++)
                {
                    int newX = x + i;
                    int newY = y + j;

                    if (newX >= 0 && newX < originalImage2.Width && newY >= 0 && newY < originalImage2.Height)
                    {
                        Color neighborColor = adjustedImage.GetPixel(newX, newY);
                        totalR += neighborColor.R;
                        totalG += neighborColor.G;
                        totalB += neighborColor.B;
                        count++;
                    }
                }
            }

            //计算局域颜色平均值
            int avgR = totalR / count;
            int avgG = totalG / count;
            int avgB = totalB / count;

            return Color.FromArgb(avgR, avgG, avgB);
        }

        private void AdaptiveFilter()
        {
            Bitmap filterImage = new Bitmap(originalImage.Width, originalImage.Height);
            int neighborhoodSize = 3; // 局域取 3 * 3

            for(int x = 0; x < originalImage.Width; x++)
            {
                for(int y = 0; y < originalImage.Height; y++)
                {
                    Color currentColor = adjustedImage.GetPixel(x, y);
                    Color newColor = CalAdaptiveColor(adjustedImage, x, y, neighborhoodSize, currentColor);
                    filterImage.SetPixel(x, y, newColor);
                }
            }
            filteredImage = filterImage;
            pictureBox1.Image = filteredImage;

            label6.Text = Convert.ToString("滤波执行完成...");
            label6.Refresh();
        }

        //计算颜色平均值
        private Color CalAdaptiveColor(Bitmap originalImage2, int x, int y, int neighborhoodSize, Color currentColor)
        {
            int halfSize = neighborhoodSize / 2;
            //颜色差异阈值
            int threshold = 2;

            int currentR = currentColor.R;
            int currentG = currentColor.G;
            int currentB = currentColor.B;

            int totalR = 0;
            int totalG = 0;
            int totalB = 0;
            int count = 0;

            for(int i = -halfSize;i <= halfSize; i++)
            {
                for(int j = -halfSize;j <= halfSize; j++)
                {
                    int newX = x + i;
                    int newY = y + j;

                    if (newX >= 0 && newX < originalImage2.Width && newY >= 0 && newY < originalImage2.Height)
                    {
                        Color neighborColor = originalImage2.GetPixel(newX, newY);
                        totalR += neighborColor.R;
                        totalG += neighborColor.G;
                        totalB += neighborColor.B;
                        count++;
                    }
                }
            }

            //计算局域颜色平均值
            int avgR = totalR / count;
            int avgG = totalG / count;
            int avgB = totalB / count;

            int diffR = Math.Abs(currentR - avgR);
            int diffG = Math.Abs(currentG - avgG);
            int diffB = Math.Abs(currentB - avgB);

            //比较颜色差异是否大于阈值
            if (diffR <= threshold && diffG <= threshold && diffB <= threshold)
            {
                return Color.FromArgb(currentR, currentG, currentB);
            }
            else
            {
                return Color.FromArgb(avgR, avgG, avgB);
            }
        }

        private void GaussFilter()
        {
            Bitmap blurredImage = new Bitmap(originalImage.Width, originalImage.Height);
            int kernelSize = 5;
            float sigma = 1.5f;

            //创建高斯内核
            float[,] kernel = CreateGuassKenel(kernelSize, sigma);

            for (int x = kernelSize / 2; x < blurredImage.Width - kernelSize / 2; x++)
            {
                for (int y = kernelSize / 2; y < blurredImage.Height - kernelSize / 2; y++)
                {
                    float sumR = 0, sumG = 0, sumB = 0;
                    for (int i = -kernelSize / 2; i <= kernelSize / 2; i++)
                    {
                        for (int j = -kernelSize / 2; j <= kernelSize / 2; j++)
                        {
                            Color pixel = adjustedImage.GetPixel(x + i, y + j);
                            float weight = kernel[i + kernelSize / 2, j + kernelSize / 2];

                            sumR += pixel.R * weight;
                            sumG += pixel.G * weight;
                            sumB += pixel.B * weight;
                        }
                    }

                    blurredImage.SetPixel(x, y, Color.FromArgb((int)sumR, (int)sumG, (int)sumB));
                }
            }
            filteredImage = blurredImage;
            pictureBox1.Image = filteredImage;
            label6.Text = Convert.ToString( "滤波执行完成...");
            label6.Refresh();
        }

        private float[,] CreateGuassKenel(int kernelSize, float sigma)
        {
            float[,] kernel = new float[kernelSize, kernelSize];
            float sum = 0;
            int halfSize = kernelSize / 2;

            for (int x = -halfSize; x <= halfSize; x++)
            {
                for (int y = -halfSize; y <= halfSize; y++)
                {
                    kernel[x + halfSize, y + halfSize] = (float)(Math.Exp(-(x * x + y * y) / (2 * sigma * sigma)) / (2 * Math.PI * sigma * sigma));
                    sum += kernel[x + halfSize, y + halfSize];
                }
            }

            // Normalize the kernel
            for (int x = 0; x < kernelSize; x++)
            {
                for (int y = 0; y < kernelSize; y++)
                {
                    kernel[x, y] /= sum;
                }
            }

            return kernel;
        }

        private void button10_Click(object sender, EventArgs e)
        {
            label8.Text = Convert.ToString("边缘检测执行中...");
            label8.Refresh();
            if(comboBox4.SelectedIndex == 0)
            {
                BasedOnRoberts();
            }else if(comboBox4.SelectedIndex == 1)
            {
                BasedOnPrewitt();
            }else if(comboBox4.SelectedIndex == 2)
            {
                BasedOnSobel();
            }else
            {
                BasedOnLaplacian();
            }

            originalImage2 = edgedImage;

        }

        private void BasedOnLaplacian()
        {
            Bitmap edgeImage = new Bitmap(filteredImage.Width, filteredImage.Height);

            int[,] laplacianKernel = new int[,]
            {
                { -1, -1, -1 },
                { -1,  8, -1 },
                { -1, -1, -1 }
            };

            for (int y = 1; y < filteredImage.Height - 1; y++)
            {
                for (int x = 1; x < filteredImage.Width - 1; x++)
                {
                    int edgeValue = GetLaplacianPixelValue(filteredImage, x, y, laplacianKernel);
                    edgeValue = Math.Max(0, Math.Min(255, edgeValue));

                    edgeImage.SetPixel(x, y, Color.FromArgb(edgeValue, edgeValue, edgeValue));
                }
            }
            edgedImage = edgeImage;
            pictureBox1.Image = edgeImage;

            label8.Text = Convert.ToString("边缘检测完成");
            label8.Refresh();
        }
        private int GetLaplacianPixelValue(Bitmap image, int x, int y, int[,] kernel)
        {
            int pixelValue = 0;

            for (int ky = -1; ky <= 1; ky++)
            {
                for (int kx = -1; kx <= 1; kx++)
                {
                    pixelValue += kernel[ky + 1, kx + 1] * image.GetPixel(x + kx, y + ky).R;
                }
            }

            return pixelValue;
        }

        //基于Sobel算子的边缘检测
        private void BasedOnSobel()
        {
            Bitmap edgeImage = new Bitmap(filteredImage.Width, filteredImage.Height);

            for (int y = 1; y < filteredImage.Height - 1; y++)
            {
                for (int x = 1; x < filteredImage.Width - 1; x++)
                {
                    int gx = GetSobelGradientX(filteredImage, x, y);
                    int gy = GetSobelGradientY(filteredImage, x, y);

                    int edgeValue = Math.Min(255, Math.Abs(gx) + Math.Abs(gy));

                    edgeImage.SetPixel(x, y, Color.FromArgb(edgeValue, edgeValue, edgeValue));
                }
            }

            edgedImage = edgeImage;
            pictureBox1.Image = edgeImage;

            label8.Text = Convert.ToString("边缘检测完成");
            label8.Refresh();
        }
        private int GetSobelGradientX(Bitmap image, int x, int y)
        {
            return (-1 * image.GetPixel(x - 1, y - 1).R) + (-2 * image.GetPixel(x - 1, y).R) + (-1 * image.GetPixel(x - 1, y + 1).R) +
                   (image.GetPixel(x + 1, y - 1).R) + (2 * image.GetPixel(x + 1, y).R) + (image.GetPixel(x + 1, y + 1).R);
        }

        private int GetSobelGradientY(Bitmap image, int x, int y)
        {
            return (-1 * image.GetPixel(x - 1, y - 1).R) + (-2 * image.GetPixel(x, y - 1).R) + (-1 * image.GetPixel(x + 1, y - 1).R) +
                   (image.GetPixel(x - 1, y + 1).R) + (2 * image.GetPixel(x, y + 1).R) + (image.GetPixel(x + 1, y + 1).R);
        }

        //基于Prewit算子的边缘检测
        private void BasedOnPrewitt()
        {
            Bitmap edgeImage = new Bitmap(filteredImage.Width, filteredImage.Height);

            for (int y = 1; y < filteredImage.Height - 1; y++)
            {
                for (int x = 1; x < filteredImage.Width - 1; x++)
                {
                    Color topLeft = filteredImage.GetPixel(x - 1, y - 1);
                    Color top = filteredImage.GetPixel(x, y - 1);
                    Color topRight = filteredImage.GetPixel(x + 1, y - 1);
                    Color left = filteredImage.GetPixel(x - 1, y);
                    Color right = filteredImage.GetPixel(x + 1, y);
                    Color bottomLeft = filteredImage.GetPixel(x - 1, y + 1);
                    Color bottom = filteredImage.GetPixel(x, y + 1);
                    Color bottomRight = filteredImage.GetPixel(x + 1, y + 1);

                    int gx = (-1 * topLeft.R) + (-1 * left.R) + (-1 * bottomLeft.R) +
                             topRight.R + right.R + bottomRight.R;

                    int gy = (-1 * topLeft.R) + (-1 * top.R) + (-1 * topRight.R) +
                             bottomLeft.R + bottom.R + bottomRight.R;

                    int edgeValue = Math.Min(255, Math.Abs(gx) + Math.Abs(gy));

                    edgeImage.SetPixel(x, y, Color.FromArgb(edgeValue, edgeValue, edgeValue));
                }
            }

            edgedImage = edgeImage;
            pictureBox1.Image = edgeImage;

            label8.Text = Convert.ToString("边缘检测完成");
            label8.Refresh();
        }

        //基于Roberts算子的边缘检测
        private void BasedOnRoberts()
        {
            Bitmap edgeImage = new Bitmap(filteredImage.Width, filteredImage.Height);

            for (int y = 0; y < filteredImage.Height - 1; y++)
            {
                for (int x = 0; x < filteredImage.Width - 1; x++)
                {
                    Color topLeft = filteredImage.GetPixel(x, y);
                    Color topRight = filteredImage.GetPixel(x + 1, y);
                    Color bottomLeft = filteredImage.GetPixel(x, y + 1);
                    Color bottomRight = filteredImage.GetPixel(x + 1, y + 1);

                    int gx = Math.Abs(topLeft.R - bottomRight.R) + Math.Abs(topRight.R - bottomLeft.R);
                    int gy = Math.Abs(topLeft.R - topRight.R) + Math.Abs(bottomLeft.R - bottomRight.R);

                    int edgeValue = Math.Min(255, gx + gy);

                    edgeImage.SetPixel(x, y, Color.FromArgb(edgeValue, edgeValue, edgeValue));
                }
            }
            edgedImage = edgeImage;

            pictureBox1.Image = edgeImage; 
            
            label8.Text = Convert.ToString("边缘检测完成");
            label8.Refresh();
        }

        private void button11_Click(object sender, EventArgs e)
        {
            int degrees = 30; // 旋转角度

            // 创建一个与原始图像大小相同的新位图
            Bitmap rotatedImage = new Bitmap(originalImage2.Width, originalImage2.Height);

            // 使用Graphics对象在新位图上绘制旋转后的图像
            using (Graphics graphics = Graphics.FromImage(rotatedImage))
            {
                graphics.TranslateTransform(rotatedImage.Width / 2, rotatedImage.Height / 2); // 设置旋转中心
                graphics.RotateTransform(degrees); // 进行旋转
                graphics.TranslateTransform(-originalImage2.Width / 2, -originalImage2.Height / 2); // 恢复坐标系

                graphics.DrawImage(originalImage2, Point.Empty); // 绘制旋转后的图像
            }

            originalImage2 = rotatedImage;
            // 显示旋转后的图像
            pictureBox1.Image = rotatedImage;
        }
    }
}
