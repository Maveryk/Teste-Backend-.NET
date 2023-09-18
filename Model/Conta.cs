using System.Text.RegularExpressions;

namespace TesteBackend.Model;

public class Conta
{
    public int numeroConta { get; set; }
    public string nome { get; set; }
    public string email { get; set; }
    public string senha { get; set; }
    public double saldo { get; set; }

    private readonly Random _random = new Random();

    public Conta()
    { }

    public Conta(int numeroConta, string email)
    {
        this.numeroConta = numeroConta;
        this.email = email;
    }

    public bool IsValidEmail()
    {
        // Use uma expressão regular para validar o email
        string emailPattern = @"^[\w-]+(\.[\w-]+)*@([\w-]+\.)+[a-zA-Z]{2,7}$";
        return Regex.IsMatch(email, emailPattern);
    }

    public int GenerateRandomAccountNumber()
    {
        // Gere um número de conta aleatório, por exemplo, entre 1000 e 9999
        return _random.Next(1000, 10000);
    }
}