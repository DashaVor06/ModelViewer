using System.Numerics;
using ModelExplorerLibrary.Models;
using ModelExplorerLibrary.Parser;
using ModelExplorerLibrary.Render;

namespace ModelExplorer
{
    public partial class Form1 : Form
    {
        private ModelClass _model = new ModelClass();
        private Parser _parser = new Parser();
        private Render _render = new Render();
        private Bitmap _backBuffer;
        private SettingsClass _settings;
        private CameraClass _camera;

        public Form1()
        {
            InitializeComponent();

            this.DoubleBuffered = true;
            this.KeyPreview = true;
            this.BackColor = Color.White;

            _backBuffer = new Bitmap(this.ClientSize.Width, this.ClientSize.Height);

            _settings = new SettingsClass
            {
                X = 0f,
                Y = 0f,
                Z = 0f,
                ScaleX = 1,
                ScaleY = 1,
                ScaleZ = 1,
                RotX = 0,
                RotY = 0,
                RotZ = 0,
                AmbientColor = new Vector3(0.1f, 0.1f, 0.1f),
                SpecularStrength = 0.5f,
                Shininess = 32
            };

            _camera = new CameraClass
            {
                X = 0,
                Y = 0,
                Z = 10,
                Fov = (float)(Math.PI / 3),
                Near = 0.1f,
                Far = 100f,
                Eye = new Vector3(0, 0, 10),
                Target = new Vector3(0, 0, 0),
                Up = new Vector3(0, 1, 0)
            };
        }

        private void CenterAndScaleModel()
        {
            if (_model.Vertices.Count == 0) return;

            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);

            foreach (var vertex in _model.Vertices)
            {
                min = Vector3.Min(min, vertex);
                max = Vector3.Max(max, vertex);
            }

            Vector3 center = (min + max) / 2;
            _settings.X = -center.X;
            _settings.Y = -center.Y;
            _settings.Z = -center.Z;

            float size = Math.Max(max.X - min.X, Math.Max(max.Y - min.Y, max.Z - min.Z));
            if (size > 0)
            {
                float scale = 2.0f / size;
                _settings.ScaleX = scale;
                _settings.ScaleY = scale;
                _settings.ScaleZ = scale;
            }
        }

        private void Form1_DoubleClick(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog();
            dialog.Filter = "OBJ files|*.obj";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _model = _parser.Load(dialog.FileName);
                CenterAndScaleModel();
                _render.RenderModel(_backBuffer, _model, _settings, _camera);
                this.Invalidate();
            }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImage(_backBuffer, 0, 0);
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            float rotSpeed = 0.1f;
            float zoomSpeed = 0.1f;

            switch (e.KeyCode)
            {
                case Keys.Left:
                    _settings.RotY += rotSpeed;
                    break;

                case Keys.Right:
                    _settings.RotY -= rotSpeed;
                    break;

                case Keys.Up:
                    _settings.RotX += rotSpeed;
                    break;

                case Keys.Down:
                    _settings.RotX -= rotSpeed;
                    break;

                case Keys.W:
                    _camera.Z -= zoomSpeed;
                    _camera.Eye = new Vector3(_camera.X, _camera.Y, _camera.Z);
                    break;

                case Keys.S:
                    _camera.Z += zoomSpeed;
                    _camera.Eye = new Vector3(_camera.X, _camera.Y, _camera.Z);
                    break;
            }

            Redraw();
        }

        private void Redraw()
        {
            _render.RenderModel(_backBuffer, _model, _settings, _camera);
            Invalidate();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            _backBuffer?.Dispose();
            _backBuffer = new Bitmap(ClientSize.Width, ClientSize.Height);
            Redraw();
        }
    }
}
