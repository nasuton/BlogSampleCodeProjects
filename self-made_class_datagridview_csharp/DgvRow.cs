namespace WinFormsTest
{
    internal class DgvRow : DataGridViewDataClassBase
    {
        private int? _id;
        private string? _title;
        private int? _age;
        private Bitmap texture;

        public int? Id
        {
            get { return this._id; }
            set { this.SetValue(out this._id, value, "Id"); }
        }
        public string? Title
        {
            get { return this._title; }
            set { this.SetValue(out this._title, value, "Title"); }
        }
        public int? Age
        {
            get { return this._age; }
            set { this.SetValue(out this._age, value, "Age"); }
        }

        public Bitmap Texture
        {
            get => texture;
            set { this.SetValue(out this.texture, value, "Image"); }
        }
    }
}
