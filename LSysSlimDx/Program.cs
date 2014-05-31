using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Text;

using System.Drawing;

using SlimDX;
using SlimDX.DXGI;
using SlimDX.Direct3D11;
using SlimDX.Direct2D;
using SlimDX.Windows;

namespace LSysSlimDx
{
    class State
    {
        public double size;

        public double angle;

        public double x;

        public double y;

        public double dir;

        public State Clone() { return (State)this.MemberwiseClone(); }
    }

    static class Program
    {
        static string Rewrite(Dictionary<char, string> tbl, string str)
        {
            var sb = new StringBuilder();

            foreach (var elt in str)
            {
                if (tbl.ContainsKey(elt))
                    sb.Append(tbl[elt]);
                else
                    sb.Append(elt);
            }

            return sb.ToString();
        }

        [STAThread]
        static void Main()
        {
            var states = new Stack<State>();

            var str = "L";

            {
                var tbl = new Dictionary<char, string>();

                tbl.Add('L', "|-S!L!Y");
                tbl.Add('S', "[F[FF-YS]F)G]+");
                tbl.Add('Y', "--[F-)<F-FG]-");
                tbl.Add('G', "FGF[Y+>F]+Y");

                for (var i = 0; i < 12; i++) str = Rewrite(tbl, str);
            }

            var sizeGrowth = -1.359672;
            var angleGrowth = -0.138235;

            State state;

            var lines = new List<Point>();

            var pen = new Pen(Brushes.Black, 0.25F);

            var initAngle = -3963.7485;

            Action buildLines = () =>
            {
                lines.Clear();

                state = new State()
                {
                    x = 400,
                    y = 400,
                    dir = 0,
                    size = 14.11,
                    angle = initAngle
                };

                foreach (var elt in str)
                {
                    if (elt == 'F')
                    {
                        var new_x = state.x + state.size * Math.Cos(state.dir * Math.PI / 180.0);
                        var new_y = state.y + state.size * Math.Sin(state.dir * Math.PI / 180.0);

                        lines.Add(new Point((int)state.x, (int)state.y));
                        lines.Add(new Point((int)new_x, (int)new_y));

                        state.x = new_x;
                        state.y = new_y;
                    }
                    else if (elt == '+') state.dir += state.angle;

                    else if (elt == '-') state.dir -= state.angle;

                    else if (elt == '>') state.size *= (1.0 - sizeGrowth);

                    else if (elt == '<') state.size *= (1.0 + sizeGrowth);

                    else if (elt == ')') state.angle *= (1 + angleGrowth);

                    else if (elt == '(') state.angle *= (1 - angleGrowth);

                    else if (elt == '[') states.Push(state.Clone());

                    else if (elt == ']') state = states.Pop();

                    else if (elt == '!') state.angle *= -1.0;

                    else if (elt == '|') state.dir += 180.0;
                }
            };


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var form = new RenderForm("LSys - Q/A: angleGrowth - W/S: initAngle");

            var swapChainDescription = new SwapChainDescription()
            {
                BufferCount = 2,
                Usage = Usage.RenderTargetOutput,
                OutputHandle = form.Handle,
                IsWindowed = true,
                ModeDescription = new ModeDescription(0, 0, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                SampleDescription = new SampleDescription(1, 0),
                Flags = SwapChainFlags.AllowModeSwitch,
                SwapEffect = SwapEffect.Discard
            };

            SlimDX.Direct3D11.Device device;
            SwapChain swapChain;

            SlimDX.Direct3D11.Device.CreateWithSwapChain(
                DriverType.Hardware,
                DeviceCreationFlags.BgraSupport,
                swapChainDescription,
                out device,
                out swapChain
                );

            Surface backBuffer = Surface.FromSwapChain(swapChain, 0);

            RenderTarget renderTarget;

            using (var factory = new SlimDX.Direct2D.Factory())
            {
                var dpi = factory.DesktopDpi;

                Console.WriteLine("dpi {0} {1}", dpi.Width, dpi.Height);

                renderTarget = RenderTarget.FromDXGI(
                    factory,
                    backBuffer,
                    new RenderTargetProperties()
                    {
                        HorizontalDpi = dpi.Width,
                        VerticalDpi = dpi.Height,

                        MinimumFeatureLevel = SlimDX.Direct2D.FeatureLevel.Default,
                        PixelFormat = new PixelFormat(Format.Unknown, AlphaMode.Ignore),
                        Type = RenderTargetType.Default,
                        Usage = RenderTargetUsage.None
                    });
            }

            using (var factory = swapChain.GetParent<SlimDX.DXGI.Factory>())
                factory.SetWindowAssociation(form.Handle, WindowAssociationFlags.IgnoreAltEnter);

            form.KeyDown += (sender, args) =>
            {
                if (args.Alt && args.KeyCode == Keys.Enter)
                    swapChain.IsFullScreen = !swapChain.IsFullScreen;

                if (args.KeyCode == Keys.Q) angleGrowth += 0.0001;

                if (args.KeyCode == Keys.A) angleGrowth -= 0.0001;

                if (args.KeyCode == Keys.W) initAngle += 0.2;

                if (args.KeyCode == Keys.S) initAngle -= 0.2;

                buildLines();
            };

            form.Size = new Size(800, 600);

            Console.WriteLine("renderTarget.Size {0} {1}", renderTarget.Size.Width, renderTarget.Size.Height);

            // form.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            // form.AutoScaleDimensions = new SizeF(2000, 800);

            buildLines();

            MessagePump.Run(
                form,
                () =>
                {
                    renderTarget.BeginDraw();
                    renderTarget.Transform = Matrix3x2.Identity;
                    renderTarget.Clear(Color.Black);

                    using (var brush = new SolidColorBrush(renderTarget, new Color4(Color.White)))
                    {
                        for (var i = 0; i < lines.Count; i += 2)
                        {
                            var a = lines[i];
                            var b = lines[i + 1];

                            renderTarget.DrawLine(brush, a.X, a.Y, b.X, b.Y, 0.1f);
                        }
                    }

                    renderTarget.EndDraw();

                    swapChain.Present(0, PresentFlags.None);
                });

            renderTarget.Dispose();
            swapChain.Dispose();
            device.Dispose();
        }


    }
}
