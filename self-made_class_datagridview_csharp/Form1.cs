using System.Windows.Forms;

namespace WinFormsTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // セルの内容に合わせて、行の高さが自動的に調節されるようにする
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            var width = dataGridView1.Columns["Image"].Width;

            var rowData = new RowData();
            rowData.Id = 1;
            rowData.Title = "Title1";
            rowData.Age = 20;
            var path = @"画像パス\サムネイル画像001.jpg";
            rowData.Texture = ReSize(path, width);
            dataGridView1.Rows.Add(rowData.Id, rowData.Title, rowData.Age, rowData.Texture);

            rowData = new RowData();
            rowData.Id = 2;
            rowData.Title = "Title2";
            rowData.Age = 30;
            path = @"画像パス\サムネイル画像002.png";
            rowData.Texture = ReSize(path, width);
            dataGridView1.Rows.Add(rowData.Id, rowData.Title, rowData.Age, rowData.Texture);
        }

        private Bitmap ReSize(string path, int columnWidth)
        {
            var img = new Bitmap(path);
            // 画像列の幅に合わせて比率を保ったままリサイズ
            float ratio = (float)columnWidth / img.Width;
            int resizeWidth = columnWidth;
            int resizeHeight = (int)(img.Height * ratio);

            var resized = new Bitmap(resizeWidth, resizeHeight);
            using (var g = Graphics.FromImage(resized))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(img, 0, 0, resizeWidth, resizeHeight);
            }
            return resized;
        }
    }
}
