using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using Calculator.Exceptions;

namespace Calculator;

public partial struct Number(BigInteger mantissa, int exponent)
{

    private static readonly int precision = 20; // Decimal places precision

    private BigInteger _mantissa = mantissa;
    private int _exponent = exponent;

    public static Number Parse(string input)
    {
        if (!NumberPattern().IsMatch(input)) throw new SyntaxException();

        int exponent = 0;

        int decimalIndex = input.IndexOf('.');
        bool hasDecimal = decimalIndex != -1;


        string integerSide = hasDecimal ? input[0 .. decimalIndex].TrimStart('0') : input;
        string decimalSide = hasDecimal ? input[(decimalIndex + 1)..].TrimEnd('0') : "";

        exponent -= decimalSide.Length;
        input = integerSide + decimalSide;

        var mantissa = BigInteger.Parse(input);

        return new Number(mantissa, exponent);
    }


    private static (BigInteger, int) Normalize(Number a, Number b)
    {
        int expDiff = a._exponent - b._exponent;
        if (expDiff > 0)
        {
            BigInteger bMantissa = b._mantissa * BigInteger.Pow(10, expDiff);
            return (bMantissa, a._exponent);
        }
        else if (expDiff < 0)
        {
            BigInteger aMantissa = a._mantissa * BigInteger.Pow(10, -expDiff);
            return (aMantissa, b._exponent);
        }

        return (a._mantissa, a._exponent);
    }


    public Number Add(Number b) => Add(this, b);
    public static Number Add(Number a, Number b)
    {
        var (aMantissa, exp) = Normalize(a, b);
        BigInteger sum = aMantissa + b._mantissa;
        return new Number(sum, exp);
    }


    public Number Sub(Number b) => Sub(this, b);
    public static Number Sub(Number a, Number b)
    {
        var (aMantissa, exp) = Normalize(a, b);
        BigInteger diff = aMantissa - b._mantissa;
        return new Number(diff, exp);
    }


    public Number Mul(Number b) => Mul(this, b);
    public static Number Mul(Number a, Number b)
    {
        BigInteger resultMantissa = a._mantissa * b._mantissa;
        int resultExponent = a._exponent + b._exponent;
        return new Number(resultMantissa, resultExponent);
    }


    public Number Div(Number b) => Div(this, b);
    public static Number Div(Number a, Number b)
    {
        if (b._mantissa == 0) throw new MathException();

        int exponentDiff = a._exponent - b._exponent;
        BigInteger scale = BigInteger.Pow(10, Math.Abs(exponentDiff) + precision);
        BigInteger numerator = a._mantissa * scale;
        BigInteger resultMantissa = numerator / b._mantissa;

        return new Number(resultMantissa, -precision);
    }


    public override string ToString()
    {
        StringBuilder sb = new();

        var mantissaString = _mantissa.ToString();
        var dotIndex = (mantissaString.Length + _exponent);

        if (dotIndex <= 0) sb.Append("0." + new string('0', -dotIndex));

        for (var i = 0; i < mantissaString.Length; i++)
        {
            if (i != 0 && i == dotIndex) sb.Append('.');
            sb.Append(mantissaString[i]);
        }

        if (dotIndex - mantissaString.Length > 0) sb.Append(new string('0', dotIndex - mantissaString.Length));

        var res = sb.ToString();
        var didx = res.IndexOf('.');
        if (sb.Length - dotIndex > 8) res = res[.. (didx + 8)];

        return res;
    }


    [GeneratedRegex(@"^-?\d+(\.\d+)?$")]
    private static partial Regex NumberPattern();
}
