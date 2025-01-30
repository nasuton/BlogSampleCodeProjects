namespace WinFormsTest
{
    public partial class Form1 : Form
    {
        private List<string> imageFiles = new List<string>();
        private string imageFolderPath = @"画像があるフォルダのパス";
        private int imageIndex = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            imageFiles.AddRange(Directory.GetFiles(imageFolderPath, "*.png"));
            imageFiles.AddRange(Directory.GetFiles(imageFolderPath, "*.jpg"));
            imageFiles.AddRange(Directory.GetFiles(imageFolderPath, "*.jpeg"));

            imageFiles.Sort();
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom; // 画像を中心に表示する
            pictureBox1.Image = ReSize(imageFiles[0]);

            timer1.Start();
            timer1.Interval = 3000; // 3秒ごとに次の画像を表示する

            // FlowLayoutPanelに画像を追加
            foreach (var file in imageFiles)
            {
                // サムネイル用のPictureBoxを作成
                var thumbnail = new PictureBox
                {
                    Image = Image.FromFile(file),
                    Width = 100,  // サムネイルの幅
                    Height = 70, // サムネイルの高さ
                    SizeMode = PictureBoxSizeMode.Zoom, // サムネイルのサイズ調整
                    Margin = new Padding(1) // サムネイル間の余白
                };

                // サムネイルをクリックしたときのイベントを設定
                thumbnail.Click += (s, e) =>
                {
                    pictureBox1.Image.Dispose();
                    pictureBox1.Image = ReSize(file);
                    imageIndex = Array.IndexOf(imageFiles.ToArray(), file); // 選択された画像のインデックスを設定
                    ResetTimer();
                };

                thumbnail.BorderStyle = BorderStyle.Fixed3D; // 現在の画像を目立たせる

                // FlowLayoutPanelに追加
                flowLayoutPanel1.Controls.Add(thumbnail);
            }
            flowLayoutPanel1.AutoScroll = true; // スクロールバーを表示
        }

        private Bitmap ReSize(string path)
        {
            var image = Image.FromFile(path);
            int resizeWidth = (int)(image.Width * 1.2);
            int resizeHeight = (int)(image.Height * 1.2);
            var resized = new Bitmap(resizeWidth, resizeHeight);
            using (var g = Graphics.FromImage(resized))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(image, 0, 0, resizeWidth, resizeHeight);
            }
            return resized;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            // 画像がない場合は何もしない
            if (imageFiles.Count == 0)
            {
                return;
            }

            // 現在表示している画像の次の画像を表示する
            if (imageIndex == imageFiles.Count - 1)
            {
                imageIndex = 0;
            }
            else
            {
                imageIndex++;
            }

            pictureBox1.Image = ReSize(imageFiles[imageIndex]);
        }

        private void ResetTimer()
        {
            timer1.Stop();
            timer1.Start();
        }
    }
}
