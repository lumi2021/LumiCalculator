using System.Numerics;
using ImGuiNET;

namespace Calculator.Screens;

public class Generic
{
    private static bool _isMenuOpen = false;
    private static Vector2 _originPosition = new(0, 0);

    public static void Update(double delta)
    {
        _originPosition = ImGui.GetCursorPos();
        ImGui.BeginChild("Menu", new Vector2(0, 30), ImGuiChildFlags.None);

        if (ImGui.Button("Menu")) _isMenuOpen = true; ImGui.SameLine();
        ImGui.Text(Program.GetScreenName());

        ImGui.EndChild();

        if (_isMenuOpen)
        {
            var style = ImGui.GetStyle();

            ImGui.SetNextWindowPos(_originPosition);
            var windowSize = ImGui.GetWindowSize() - _originPosition;
            windowSize.Y -= style.DisplaySafeAreaPadding.Y;

            ImGui.SetNextWindowSize(new Vector2(windowSize.X - 60, windowSize.Y));
            ImGui.Begin("Menu", ref _isMenuOpen, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse);

            var regionAvail = ImGui.GetContentRegionAvail();

            for (var i = 0; i < Program._screens.Length; i++)
            {
                var screen = Program._screens[i];

                if (ImGui.Button(screen.name, new(regionAvail.X, 30)))
                {
                    Program.SetScreen(i);
                    _isMenuOpen = false;
                }
            }

            ImGui.End();
        }
    }

}
