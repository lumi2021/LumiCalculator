﻿using System.Numerics;
using System.Text;
using ImGuiNET;
using System.Text.RegularExpressions;
using Calculator.Exceptions;

namespace Calculator.Screens;

public static partial class Programmer
{

    private static List<string> expressionTokens = [];
    private static StringBuilder entryBuilder = new();
    private static bool showingResult = false;

    public static void Update(double delta)
    {

        ImGui.BeginChild("Screen", new Vector2(0, 100), ImGuiChildFlags.Border);

        string expression = string.Join(" ", expressionTokens);
        string entry = entryBuilder.ToString();
        var canvasSize = ImGui.GetContentRegionAvail();

        ImGui.PushFont(Program.fontDefault);
        var expressionSize = ImGui.CalcTextSize(expression);
        ImGui.SetCursorPosX(canvasSize.X - expressionSize.X);
        ImGui.TextColored(new(0.7f, 0.7f, 0.7f, 1f), expression);
        ImGui.PopFont();

        ImGui.PushFont(Program.fontBig);
        var entrySize = ImGui.CalcTextSize(entry);
        ImGui.SetCursorPosX(canvasSize.X - entrySize.X);
        ImGui.Text(entry);
        ImGui.PopFont();

        ImGui.EndChild();

        var padding = ImGui.GetStyle().WindowPadding;
        var spacing = ImGui.GetStyle().ItemInnerSpacing;

        var buttonSizeX = (ImGui.GetContentRegionAvail().X - padding.X * 2 - spacing.X * 4) / 5;
        var buttonSizeY = (ImGui.GetContentRegionAvail().Y - spacing.Y * 5) / 6;

        ImGui.PushFont(Program.fontBig);


        if (ImGui.Button("A", new(buttonSizeX, buttonSizeY))) RegisterValue("A"); ImGui.SameLine();
        PushButtonStyles();
        if (ImGui.Button("<<", new(buttonSizeX, buttonSizeY))) RegisterValue("shl"); ImGui.SameLine();
        if (ImGui.Button(">>", new(buttonSizeX, buttonSizeY))) RegisterValue("shr"); ImGui.SameLine();
        if (ImGui.Button("C", new(buttonSizeX, buttonSizeY))) RegisterValue("clear"); ImGui.SameLine();
        if (ImGui.Button("BS", new(buttonSizeX, buttonSizeY))) RegisterValue("Backspace");
        ImGui.PopStyleColor(3);

        if (ImGui.Button("B", new(buttonSizeX, buttonSizeY))) RegisterValue("B"); ImGui.SameLine();
        PushButtonStyles();
        if (ImGui.Button("(", new(buttonSizeX, buttonSizeY))) RegisterValue("("); ImGui.SameLine();
        if (ImGui.Button(")", new(buttonSizeX, buttonSizeY))) RegisterValue(")"); ImGui.SameLine();
        if (ImGui.Button("%", new(buttonSizeX, buttonSizeY))) RegisterValue("%"); ImGui.SameLine();
        if (ImGui.Button("÷", new(buttonSizeX, buttonSizeY))) RegisterValue("÷");
        ImGui.PopStyleColor(3);

        if (ImGui.Button("C", new(buttonSizeX, buttonSizeY))) RegisterValue("C"); ImGui.SameLine();
        if (ImGui.Button("7", new(buttonSizeX, buttonSizeY))) RegisterValue("7"); ImGui.SameLine();
        if (ImGui.Button("8", new(buttonSizeX, buttonSizeY))) RegisterValue("8"); ImGui.SameLine();
        if (ImGui.Button("9", new(buttonSizeX, buttonSizeY))) RegisterValue("9"); ImGui.SameLine();
        PushButtonStyles();
        if (ImGui.Button("×", new(buttonSizeX, buttonSizeY))) RegisterValue("*");
        ImGui.PopStyleColor(3);

        if (ImGui.Button("D", new(buttonSizeX, buttonSizeY))) RegisterValue("4"); ImGui.SameLine();
        if (ImGui.Button("4", new(buttonSizeX, buttonSizeY))) RegisterValue("4"); ImGui.SameLine();
        if (ImGui.Button("5", new(buttonSizeX, buttonSizeY))) RegisterValue("5"); ImGui.SameLine();
        if (ImGui.Button("6", new(buttonSizeX, buttonSizeY))) RegisterValue("6"); ImGui.SameLine();
        PushButtonStyles();
        if (ImGui.Button("-", new(buttonSizeX, buttonSizeY))) RegisterValue("-");
        ImGui.PopStyleColor(3);

        if (ImGui.Button("E", new(buttonSizeX, buttonSizeY))) RegisterValue("1"); ImGui.SameLine();
        if (ImGui.Button("1", new(buttonSizeX, buttonSizeY))) RegisterValue("1"); ImGui.SameLine();
        if (ImGui.Button("2", new(buttonSizeX, buttonSizeY))) RegisterValue("2"); ImGui.SameLine();
        if (ImGui.Button("3", new(buttonSizeX, buttonSizeY))) RegisterValue("3"); ImGui.SameLine();
        PushButtonStyles();
        if (ImGui.Button("+", new(buttonSizeX, buttonSizeY))) RegisterValue("+");
        ImGui.PopStyleColor(3);

        if (ImGui.Button("F", new(buttonSizeX, buttonSizeY))) RegisterValue("00"); ImGui.SameLine();
        if (ImGui.Button("00", new(buttonSizeX, buttonSizeY))) RegisterValue("00"); ImGui.SameLine();
        if (ImGui.Button("0", new(buttonSizeX, buttonSizeY))) RegisterValue("0"); ImGui.SameLine();
        if (ImGui.Button(".", new(buttonSizeX, buttonSizeY))) RegisterValue("."); ImGui.SameLine();

        PushButtonStyles();
        if (ImGui.Button("=", new(buttonSizeX, buttonSizeY))) RegisterValue("=");
        ImGui.PopStyleColor(3);

        ImGui.PopFont();

    }

    private static void PushButtonStyles()
    {
        ImGui.PushStyleColor(ImGuiCol.Button, Util.ColorVec2UInt(new(0.2f, 0.2f, 0.2f, 1f)));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Util.ColorVec2UInt(new(0.5f, 0.5f, 0.5f, 1f)));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, Util.ColorVec2UInt(new(0.1f, 0.1f, 0.1f, 1f)));
    }


    private static void RegisterValue(string c)
    {
        if (showingResult)
        {
            ClearAll();
            showingResult = false;
        }

        switch (c)
        {
            case "ClearEntry":
                entryBuilder.Clear();
                break;

            case "Clear":
                ClearAll();
                break;

            case "Backspace":
                if (entryBuilder.Length > 0) entryBuilder.Length--;
                break;

            case "shl" or "shr":
                TokenizeEntry();
                AppendToken(c);
                break;

            case "+" or "-" or "*" or "/" or "1/" or "%":
                TokenizeEntry();
                AppendToken(c);
                break;

            case "**":
                TokenizeEntry();
                AppendToken("**");
                break;
            case "//":
                TokenizeEntry();
                AppendToken("//");
                break;

            case "=":
                TokenizeEntry();
                ResolveExpression();
                showingResult = true;
                break;


            default:
                entryBuilder.Append(c);
                break;
        }
    }
    private static void AppendToken(string c) => expressionTokens.Add(c);
    private static void TokenizeEntry()
    {
        if (entryBuilder.ToString().Trim().Length == 0) return;
        expressionTokens.Add(entryBuilder.ToString().Trim());
        entryBuilder.Clear();
    }


    private static void ClearAll()
    {
        expressionTokens.Clear();
        entryBuilder.Clear();
    }
    private static void SyntaxError()
    {
        entryBuilder.AppendLine("= Syntax Error");
    }
    private static void ResolveExpression()
    {
        var tokens = new Queue<string>(expressionTokens);

        try {
            var res = RecursiveResolve_Level3(tokens);
            entryBuilder.Append($"= {res}");
        }
        catch (MathException)   { entryBuilder.Append("= Math Error"); }
        catch (SyntaxException) { entryBuilder.Append("= Syntax Error"); }

        showingResult = true;
    }


    private static Number RecursiveResolve_Level3(Queue<string> tokens)
    {
        var exp = RecursiveResolve_Level2(tokens);


        return exp;
    }

    private static Number RecursiveResolve_Level2(Queue<string> tokens)
    {
        var exp = RecursiveResolve_Level1(tokens);

        if (tokens.Count > 1)
        {
            if (tokens.Peek() == "*")
            {
                tokens.Dequeue();
                exp = exp.Mul(RecursiveResolve_Level1(tokens));
            }
            else if (tokens.Peek() == "÷")
            {
                tokens.Dequeue();
                exp = exp.Div(RecursiveResolve_Level1(tokens));
            }
        }

        return exp;
    }

    private static Number RecursiveResolve_Level1(Queue<string> tokens)
    {
        var exp = RecursiveResolve_Level0(tokens);

        if (tokens.Count > 1)
        {
            if (tokens.Peek() == "+")
            {
                tokens.Dequeue();
                exp = exp.Add(RecursiveResolve_Level1(tokens));
            }
            else if (tokens.Peek() == "-")
            {
                tokens.Dequeue();
                exp = exp.Sub(RecursiveResolve_Level1(tokens));
            }
        }

        return exp;
    }

    private static Number RecursiveResolve_Level0(Queue<string> tokens)
    {
        if (tokens.Count == 0) throw new SyntaxException();
        if (!NumericPattern().IsMatch(tokens.Peek())) throw new SyntaxException();
        return Number.Parse(tokens.Dequeue());
    }

    [GeneratedRegex(@"^[0-9.]+$")]
    private static partial Regex NumericPattern();
}
