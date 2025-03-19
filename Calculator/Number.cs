using System.Text;

namespace Calculator;

public struct Number
{
    private string _mantissa;
    private int _exponent;

    public Number(string mantissa, int exponent)
    {
        _mantissa = mantissa;
        _exponent = exponent;
    }
    public Number(string number)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < number.Length; i++)
        {
            if (number[i] == '.') _exponent = i;
            else sb.Append(number[i]);
        }
    }

    public override string ToString()
    {
        StringBuilder sb = new();

        for (var i = 0; i < _mantissa.Length; i++)
        {
            sb.Append(_mantissa[i]);
            if (i == _exponent) sb.Append('.');
        }

        return sb.ToString();
    }
}
