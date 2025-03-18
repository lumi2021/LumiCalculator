using Silk.NET.Input;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using ImGuiNET;
using Window = Silk.NET.Windowing.Window;
using Silk.NET.Maths;
using Silk.NET.GLFW;
using System.Numerics;

internal class Program
{

    public static IWindow window = null!;
    public static GL gl = null!;
    public static Glfw glfw = null!;
    public static IInputContext input = null!;
    public static ImGuiController imgui = null!;

    private static StandardCursor _cursor = StandardCursor.Default;
    private static Vector2D<int>? _cursorOffset = null;

    private static Vector2D<int> mousepos = new(0, 0);

    private static void Main(string[] args)
    {
        WindowOptions windowOptions = WindowOptions.Default with
        {
            Size = new(300, 400),
            Title = "Calculator",
            WindowBorder = WindowBorder.Hidden,
            TransparentFramebuffer = true,
            VSync = false
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

        gl.Enable(EnableCap.Blend);
        gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        gl.ColorMask(true, true, true, true);
        gl.ClearColor(0f, 0f, 0f, 0f);
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

        ImGui.Begin("calculator_window", dockSpaceFlags);

        HandleWindow();

        ImGui.End();

        ImGui.End();

        if (ImGui.IsAnyItemHovered()) TrySetCursor(StandardCursor.Hand);
    }
    private static unsafe void PoolEvents(float delta)
    {
        imgui.Update(delta);

        double mx, my;
        glfw.GetCursorPos((WindowHandle*)window.Handle, out mx, out my);
        mousepos.X = (int)mx; mousepos.Y = (int)my;
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
        draw.AddText(winpos + new Vector2(10, 10),
            (*ImGui.GetStyleColorVec4(ImGuiCol.Text)).ColorVec2UInt(), "Calculator");

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

        ImGui.Begin("Debug");
        ImGui.Text($"mouse position: {mousepos.X}, {mousepos.Y}");
        ImGui.Text($"window position: {window.Position}");
        ImGui.Text($"window size:     {window.Size}");

        if (
            (mousepos.X < 2 && (mousepos.X > -4)) ||
            (mousepos.X > (window.Size.X - 2) && mousepos.X < (window.Size.X + 4)) ||
            (mousepos.Y < 2 && (mousepos.Y > -4)) ||
            (mousepos.Y > (window.Size.Y - 2) && mousepos.Y < (window.Size.Y + 4))
        )
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 1, 0, 1));
            ImGui.Text("Mouse is hovering window edge");
            ImGui.PopStyleColor();


        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0, 0, 1));
            ImGui.Text("Mouse not on window edge");
            ImGui.PopStyleColor();
        }

        if (
            mousepos.X >= 2 && mousepos.X <= window.Size.X - 90 &&
            mousepos.Y >= 2 && mousepos.Y <= 30
        )
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 1, 0, 1));
            ImGui.Text("Mouse is hovering window title");
            ImGui.PopStyleColor();

            if (input.Mice[0].IsButtonPressed(Silk.NET.Input.MouseButton.Left))
            {
                if (!_cursorOffset.HasValue) _cursorOffset = mousepos - window.Position;

                TrySetCursor(StandardCursor.ResizeAll);
                window.Position = mousepos - _cursorOffset.Value;
            }
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0, 0, 1));
            ImGui.Text("Mouse not on window title");
            ImGui.PopStyleColor();
        }
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
