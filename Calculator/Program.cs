using Silk.NET.Input;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using ImGuiNET;
using Window = Silk.NET.Windowing.Window;
using Silk.NET.Maths;
using Silk.NET.GLFW;
using System.Numerics;
using MouseButton = Silk.NET.Input.MouseButton;
using Calculator.Screens;

internal class Program
{

    public static IWindow window = null!;
    public static GL gl = null!;
    public static Glfw glfw = null!;
    public static IInputContext input = null!;
    public static ImGuiController imgui = null!;

    private static string windowTitle = "Calculator";
    private static StandardCursor _cursor = StandardCursor.Default;

    private static bool _holdingHeader = false;
    private static Vector2D<int> _cursorOffset = new(0, 0);

    private static readonly (string name, Action<double> update)[] _screens = [
        ( "Standard", Standard.Update )
    ];
    private static int _currentScreen = 0;

    public static ImFontPtr fontTiny = IntPtr.Zero;
    public static ImFontPtr fontDefault = IntPtr.Zero;
    public static ImFontPtr fontBig = IntPtr.Zero;
    public static ImFontPtr fontBigger = IntPtr.Zero;

    private unsafe static Vector2D<int> Mousepos {
        get
        {
            double mx, my;
            glfw.GetCursorPos((WindowHandle*)window.Handle, out mx, out my);
            return window.Position + new Vector2D<int>((int)mx, (int)my);
        }
    }

    private static void Main(string[] args)
    {
        WindowOptions windowOptions = WindowOptions.Default with
        {
            Size = new(450, 550),
            Title = "Calculator",
            WindowBorder = WindowBorder.Hidden,
            TransparentFramebuffer = true,
            VSync = false,
            IsEventDriven = true,
        };

        window = Window.Create(windowOptions);

        window.Load += OnLoad;
        window.Closing += OnClose;
        window.Update += OnUpdate;
        window.Render += OnRender;

        window.FramebufferResize += (s) => gl.Viewport(0, 0, (uint)s.X, (uint)s.Y);

        window.Run();
    }

    private static unsafe void OnLoad()
    {
        gl = window.CreateOpenGL();
        glfw = GlfwProvider.GLFW.Value;
        input = window.CreateInput();

        imgui = new(gl, window, input);

        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;

        io.Fonts.Clear();

        fontTiny = io.Fonts.AddFontDefault();
        fontDefault = io.Fonts.AddFontFromFileTTF("Assets/FragmentMono-Regular.ttf", 24f);
        fontBig = io.Fonts.AddFontFromFileTTF("Assets/FragmentMono-Regular.ttf", 48f);
        fontBigger = io.Fonts.AddFontFromFileTTF("Assets/FragmentMono-Regular.ttf", 76f);

        io.Fonts.Build();
        io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height, out int bytesPerPixel);

        var _fontTexture = gl.GenTexture();
        gl.BindTexture(GLEnum.Texture2D, _fontTexture);

#pragma warning disable CS9193
        gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Linear);
        gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Linear);
        gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.ClampToEdge);
        gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.ClampToEdge);
#pragma warning restore CS9193

        gl.TexImage2D(
            GLEnum.Texture2D, 0, (int)GLEnum.Rgba8,
            (uint)width, (uint)height, 0,
            GLEnum.Rgba, GLEnum.UnsignedByte, pixels
        );

        io.Fonts.SetTexID((IntPtr)_fontTexture);
        io.Fonts.ClearTexData();

        ImGui.PushFont(fontDefault);

        gl.Enable(EnableCap.Blend);
        gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        gl.ColorMask(true, true, true, true);
        gl.ClearColor(0f, 0f, 0f, 0f);

        input.Mice[0].MouseDown += OnMouseButtonDown;
        input.Mice[0].MouseUp += OnMouseButtonUp;
    }
    private static void OnClose()
    {
        //imgui.Dispose(); // returning an error for some reason??
        input.Dispose();
        gl.Dispose();
    }


    private static unsafe void OnUpdate(double delta)
    {
        PoolEvents((float)delta);

        var imGuiViewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(imGuiViewport.WorkPos);
        ImGui.SetNextWindowSize(imGuiViewport.WorkSize);
        ImGui.SetNextWindowViewport(imGuiViewport.ID);

        ImGuiWindowFlags dockSpaceFlags =
            ImGuiWindowFlags.NoDocking |
            ImGuiWindowFlags.NoTitleBar |
            ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoBringToFrontOnFocus |
            ImGuiWindowFlags.NoNavFocus |
            ImGuiWindowFlags.NoSavedSettings;

        windowTitle = $"Calculator - {_screens[_currentScreen].name}";

        ImGui.Begin("calculator_window", dockSpaceFlags);
        HandleWindow();

        _screens[_currentScreen].update(delta);

        ImGui.End();

        if (_holdingHeader) TrySetCursor(StandardCursor.ResizeAll);
        else if (ImGui.IsAnyItemHovered()) TrySetCursor(StandardCursor.Hand);
    }
    private static unsafe void PoolEvents(float delta)
    {
        imgui.Update(delta);
    }
    private static unsafe void HandleWindow()
    {
        var draw = ImGui.GetWindowDrawList();
        var winpos = ImGui.GetWindowPos();
        var winsize = ImGui.GetWindowSize();

        // Header Bg
        draw.AddRectFilled(winpos, winpos + new Vector2(winsize.X, 30),
            (*ImGui.GetStyleColorVec4(ImGuiCol.Header)).ColorVec2UInt());
        // Title
        ImGui.PushFont(fontTiny);
        draw.AddText(winpos + new Vector2(10, 10),
            (*ImGui.GetStyleColorVec4(ImGuiCol.Text)).ColorVec2UInt(), windowTitle);
        ImGui.PopFont();

        // Window Controls
        ImGui.SetCursorPos(winpos + new Vector2(winsize.X - 90, 0));
        if (ImGui.InvisibleButton("minimize", new Vector2(30, 30)))
        {
            window.WindowState = WindowState.Minimized;
        }
        if (ImGui.IsItemHovered())
        {
            draw.AddRectFilled(winpos + new Vector2(winsize.X - 90, 0),
                winpos + new Vector2(winsize.X - 60, 30),
                (*ImGui.GetStyleColorVec4(ImGuiCol.ButtonHovered)).ColorVec2UInt());
        }

        draw.AddLine(
            winpos + new Vector2(winsize.X - 60, 30) - new Vector2(10, 15),
            winpos + new Vector2(winsize.X - 60, 30) - new Vector2(20, 15),
            (*ImGui.GetStyleColorVec4(ImGuiCol.Text)).ColorVec2UInt(), 2f
        );

        ImGui.SetCursorPos(winpos + new Vector2(winsize.X - 60, 0));
        if (ImGui.InvisibleButton("window", new Vector2(30, 30)))
        {
            if (window.WindowState == WindowState.Normal)
                window.WindowState = WindowState.Maximized;
            else
                window.WindowState = WindowState.Normal;
        }
        if (ImGui.IsItemHovered())
        {
            draw.AddRectFilled(winpos + new Vector2(winsize.X - 60, 0),
                winpos + new Vector2(winsize.X - 30, 30),
                (*ImGui.GetStyleColorVec4(ImGuiCol.ButtonHovered)).ColorVec2UInt());
        }

        draw.PathLineTo(winpos + new Vector2(winsize.X - 30, 30) - new Vector2(20, 10));
        draw.PathLineTo(winpos + new Vector2(winsize.X - 30, 30) - new Vector2(10, 10));
        draw.PathLineTo(winpos + new Vector2(winsize.X - 30, 30) - new Vector2(10, 20));
        draw.PathLineTo(winpos + new Vector2(winsize.X - 30, 30) - new Vector2(20, 20));
        draw.PathStroke((*ImGui.GetStyleColorVec4(ImGuiCol.Text)).ColorVec2UInt(), ImDrawFlags.Closed, 2f);

        ImGui.SetCursorPos(winpos + new Vector2(winsize.X - 30, 0));
        if (ImGui.InvisibleButton("close", new Vector2(30, 30)))
        {
            window.Close();
        }
        if (ImGui.IsItemHovered())
        {
            draw.AddRectFilled(winpos + new Vector2(winsize.X - 30, 0),
                winpos + new Vector2(winsize.X, 30),
                (*ImGui.GetStyleColorVec4(ImGuiCol.ButtonHovered)).ColorVec2UInt());
        }

        draw.AddLine(
            winpos + new Vector2(winsize.X, 30) - new Vector2(10, 10),
            winpos + new Vector2(winsize.X, 30) - new Vector2(20, 20),
            (*ImGui.GetStyleColorVec4(ImGuiCol.Text)).ColorVec2UInt(), 2f
        );
        draw.AddLine(
            winpos + new Vector2(winsize.X, 30) - new Vector2(20, 10),
            winpos + new Vector2(winsize.X, 30) - new Vector2(10, 20),
            (*ImGui.GetStyleColorVec4(ImGuiCol.Text)).ColorVec2UInt(), 2f
        );

        ImGui.SetCursorPos(new(0, 0));
        ImGui.Dummy(new Vector2(ImGui.GetWindowSize().X, 30));

        if (_holdingHeader) window.Position = Mousepos + _cursorOffset;
    }


    private static void OnMouseButtonDown(IMouse _, MouseButton button)
    {
        if (button == MouseButton.Left)
        {
            var p = Mousepos - window.Position;
            var s = new Vector2(window.FramebufferSize.X, window.FramebufferSize.Y);
            if (p.X >= 0 && p.X <= s.X - 120 && p.Y > 0 && p.Y < 30)
            {
                _holdingHeader = true;
                _cursorOffset = -p;
            }
        }
    }
    private static void OnMouseButtonUp(IMouse m, MouseButton button)
    {
        if (button == MouseButton.Left) _holdingHeader = false;
    }


    private static unsafe void OnRender(double delta)
    {
        gl.Clear(ClearBufferMask.ColorBufferBit);
        foreach (var i in input.Mice) i.Cursor.StandardCursor = _cursor;
        _cursor = StandardCursor.Default;
        imgui.Render();
    }
    private static void TrySetCursor(StandardCursor cursor)
    {
        if (_cursor == StandardCursor.Default || cursor == StandardCursor.Default) _cursor = cursor;
    }
}

public static class Util
{
    public static uint ColorVec2UInt(this Vector4 color)
    {
        uint r = (uint)(color.X * 255);
        uint g = (uint)(color.Y * 255);
        uint b = (uint)(color.Z * 255);
        uint a = (uint)(color.W * 255);
        return (a << 24) | (b << 16) | (g << 8) | r;
    }
}
