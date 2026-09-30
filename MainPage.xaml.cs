using System.Globalization;

namespace Calculatrice;

public partial class MainPage : ContentPage
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private const int MaxDigits = 15;

    private string _current = "0";   // nombre en cours de saisie
    private decimal? _left;          // opérande de gauche
    private string? _op;             // opérateur en attente
    private bool _startNew;          // le prochain chiffre remplace l'affichage
    private bool _error;             // état d'erreur (ex. division par zéro)

    public MainPage()
    {
        InitializeComponent();
    }

    // ---------- Saisie ----------
    private void OnDigit(object sender, EventArgs e)
    {
        if (_error) Reset();
        if (_startNew) { _current = "0"; _startNew = false; }

        string d = ((Button)sender).Text;
        if (_current == "0") _current = d;
        else if (_current == "-0") _current = "-" + d;
        else if (_current.Count(char.IsDigit) < MaxDigits) _current += d;
        Refresh();
    }

    private void OnDecimal(object sender, EventArgs e)
    {
        if (_error) Reset();
        if (_startNew) { _current = "0"; _startNew = false; }
        if (!_current.Contains('.')) _current += ".";
        Refresh();
    }

    private void OnClear(object sender, EventArgs e)
    {
        Reset();
        Refresh();
    }

    private void OnBackspace(object sender, EventArgs e)
    {
        if (_error) { Reset(); Refresh(); return; }
        if (_startNew) return;
        _current = _current.Length > 1 ? _current[..^1] : "0";
        if (_current == "-") _current = "0";
        Refresh();
    }

    private void OnSign(object sender, EventArgs e)
    {
        if (_error || _current == "0") return;
        _current = _current.StartsWith('-') ? _current[1..] : "-" + _current;
        Refresh();
    }

    private void OnPercent(object sender, EventArgs e)
    {
        if (_error) return;
        decimal value = Parse(_current);
        // 200 + 10 %  =>  10 % de 200 = 20 ; sinon simple division par 100
        if (_left.HasValue && (_op == "+" || _op == "−"))
            value = _left.Value * value / 100m;
        else
            value /= 100m;
        _current = Format(value);
        _startNew = false;
        Refresh();
    }

    // ---------- Calcul ----------
    private void OnOperator(object sender, EventArgs e)
    {
        if (_error) return;
        string newOp = ((Button)sender).Text;
        decimal value = Parse(_current);

        if (_left.HasValue && _op != null && !_startNew)
        {
            decimal? r = Compute(_left.Value, _op, value);
            if (r == null) return;              // erreur déjà affichée
            _left = r;
            _current = Format(r.Value);
        }
        else if (!_left.HasValue || !_startNew)
        {
            _left = value;
        }

        _op = newOp;
        _startNew = true;
        ExpressionLabel.Text = $"{Format(_left!.Value)} {_op}";
        Refresh();
    }

    private void OnEquals(object sender, EventArgs e)
    {
        if (_error || _left == null || _op == null) return;
        decimal right = Parse(_current);
        string expr = $"{Format(_left.Value)} {_op} {Format(right)} =";

        decimal? r = Compute(_left.Value, _op, right);
        if (r == null) return;

        ExpressionLabel.Text = expr;
        _current = Format(r.Value);
        _left = null;
        _op = null;
        _startNew = true;
        Refresh();
    }

    private decimal? Compute(decimal a, string op, decimal b)
    {
        try
        {
            switch (op)
            {
                case "+": return a + b;
                case "−": return a - b;
                case "×": return a * b;
                case "÷":
                    if (b == 0) { ShowError("Division par zéro impossible"); return null; }
                    return a / b;
            }
        }
        catch (OverflowException)
        {
            ShowError("Nombre trop grand");
        }
        return null;
    }

    // ---------- Utilitaires ----------
    private static decimal Parse(string s) =>
        decimal.Parse(s, NumberStyles.Float, Inv);

    private static string Format(decimal v) => v.ToString("G29", Inv);

    private void ShowError(string message)
    {
        _error = true;
        _left = null;
        _op = null;
        _current = "Erreur";
        ExpressionLabel.Text = message;
        Refresh();
    }

    private void Reset()
    {
        _current = "0";
        _left = null;
        _op = null;
        _startNew = false;
        _error = false;
        ExpressionLabel.Text = "";
    }

    private async void Refresh()
    {
        ResultLabel.Text = _current;
        // réduit la police pour les longs nombres
        ResultLabel.FontSize = _current.Length switch
        {
            > 14 => 28,
            > 10 => 36,
            _ => 48
        };
        // garde la fin du nombre visible
        await Task.Delay(30);
        await ResultScroll.ScrollToAsync(ResultLabel, ScrollToPosition.End, false);
    }
}
