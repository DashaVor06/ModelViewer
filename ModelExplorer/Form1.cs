using System.Numerics;
using System.Reflection;
using ModelExplorerLibrary.Models;
using ModelExplorerLibrary.Parser;
using ModelExplorerLibrary.Render;
using System.Diagnostics;

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
        private bool _isDirty = false;
        private bool _isRendering = false;
        private Bitmap _frontBuffer;
        private Stopwatch _fpsTimer = new Stopwatch();

        public Form1()
        {
            InitializeComponent();

            _backBuffer = new Bitmap(ClientSize.Width, ClientSize.Height);
            _frontBuffer = new Bitmap(ClientSize.Width, ClientSize.Height);
            timer.Start();

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
                Redraw();
            }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            lock (_frontBuffer)
            {
                e.Graphics.DrawImage(_frontBuffer, 0, 0);
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            float rotSpeed = 0.1f;
            float zoomSpeed = 0.1f;

            switch (e.KeyCode)
            {
                case Keys.Left:
                    _settings.RotX -= rotSpeed;
                    break;

                case Keys.Right:
                    _settings.RotX += rotSpeed;
                    break;

                case Keys.Up:
                    _settings.RotY -= rotSpeed;
                    break;

                case Keys.Down:
                    _settings.RotY += rotSpeed;
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

        private async void Redraw()
        {
            // 1. Защита от наложения: если уже рендерим, выходим
            if (_isRendering) return;

            // 2. Ограничение частоты (не чаще 60 FPS)
            if (_fpsTimer.IsRunning && _fpsTimer.ElapsedMilliseconds < 16) return;

            _fpsTimer.Restart();
            _isRendering = true;

            // 3. Копируем переменные для потока
            var model = _model;
            var settings = _settings;
            var camera = _camera;

            // 4. Тяжелый рендеринг в фоновом потоке
            await Task.Run(() => {
                _render.RenderModel(_backBuffer, model, settings, camera);
            });

            // 5. Потокобезопасный "своп" буферов
            lock (_frontBuffer)
            {
                using (Graphics g = Graphics.FromImage(_frontBuffer))
                {
                    g.DrawImage(_backBuffer, 0, 0);
                }
            }

            // 6. Перерисовка
            Invalidate();
            _isRendering = false;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            // Защита от нулевого размера при минимизации
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;

            // Освобождаем старые буферы
            _backBuffer?.Dispose();
            _frontBuffer?.Dispose();

            // Создаем новые
            _backBuffer = new Bitmap(ClientSize.Width, ClientSize.Height);
            _frontBuffer = new Bitmap(ClientSize.Width, ClientSize.Height);

            // Принудительно перерисовываем
            Redraw();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (_isDirty)
            {
                timer.Stop(); 
                Redraw();
                _isDirty = false;
                timer.Start();
            }
        }
    }
}
