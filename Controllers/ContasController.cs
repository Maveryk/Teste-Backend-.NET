using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using TesteBackend.Model;
using TesteBackend.Services;

namespace TesteBackend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ContasController : ControllerBase
{
    private readonly string _connectionString;

    public ContasController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    /// <summary>
    /// Endpoint para cadastrar conta com nome, e-mail e senha e gerar o número da conta aleatoriamente pelo banco de dadods
    /// </summary>
    /// <response code="200">Returna o numero da conta</response>

    [HttpPost("cadastro")]
    public IActionResult Cadastro([FromBody] Conta novaConta)
    {
        // Validação do email usando regex
        if (!novaConta.IsValidEmail())
        {
            return BadRequest("Email inválido.");
        }

        // Gere o número da conta
        using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
        {
            connection.Open();
            var query = @"INSERT INTO Conta (Nome, Email, Senha, Saldo) VALUES (@Nome, @Email, @Senha, @Saldo) RETURNING NumeroConta;";

            var parameters = new
            {
                Nome = novaConta.nome,
                Email = novaConta.email,
                Senha = novaConta.senha,
                Saldo = novaConta.saldo
            };

            var numeroConta = connection.QueryFirstOrDefault<int>(query, parameters);

            if (numeroConta != 0)
            {
                novaConta.numeroConta = numeroConta;
            }
        }

        return Ok(novaConta.numeroConta);
        // Retorne uma resposta adequada
    }

    /// <summary>
    /// Endpoint para obter o saldo da conta
    /// </summary>
    /// <response code="200">Returna o saldo da conta</response>
    [Authorize]
    [HttpGet("saldo")]
    public IActionResult Saldo()
    {
        using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
        {
            var token = Request.Headers["Authorization"].ToString().Split(' ')[1];
            var conta = TokenService.ValidateToken(token);

            connection.Open();
            var query = @"SELECT Saldo
                      FROM Conta
                      WHERE NumeroConta = @NumeroConta";

            var parameters = new
            {
                NumeroConta = conta.numeroConta
            };

            var saldo = connection.QueryFirstOrDefault<double>(query, parameters);

            if (saldo != null)
            {
                return Ok(saldo);
            }
            else
            {
                return BadRequest("Conta não encontrada.");
            }
        }
    }

    /// <summary>
    /// Endpoint para obter extrato da conta
    /// </summary>
    /// <response code="200">Returna o extrato da conta</response>
    [Authorize]
    [HttpGet("extrato")]
    public IActionResult Extrato()
    {
        var token = Request.Headers["Authorization"].ToString().Split(' ')[1];
        var conta = TokenService.ValidateToken(token);
        using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
        {
            var query = @"SELECT *
                      FROM Transacao
                      WHERE NumeroContaOrigem = @NumeroConta OR NumeroContaDestino = @NumeroConta
                      ORDER BY Data DESC";

            var parameters = new
            {
                NumeroConta = conta.numeroConta
            };

            var extrato = connection.Query<Transacao>(query, parameters).ToList();

            if (extrato.Count != 0)
            {
                return Ok(extrato);
            }
            else
            {
                return BadRequest("Conta não possui transações.");
            }
        }
    }

    /// <summary>
    /// Endpoint para transferir valores entre contas
    /// </summary>
    /// <returns>Retorna o id da transacao</returns>
    /// <response code="200">transfere valores entre contas e Retorna o id da transacao</response>

    [Authorize]
    [HttpPost("transferencia")]
    public IActionResult Transferencia([FromBody] Transacao transacao)
    {
        var token = Request.Headers["Authorization"].ToString().Split(' ')[1];
        var conta = TokenService.ValidateToken(token);

        if (transacao.numeroContaOrigem == 0)
        {
            transacao.numeroContaOrigem = conta.numeroConta;
        }
        else if (conta.numeroConta != transacao.numeroContaOrigem)
        {
            return BadRequest("conta origem invalida");
        }

        using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
        {
            // Verificar a validade das Contas e saldo
            var query = @"SELECT *
                      FROM Conta
                      WHERE NumeroConta = @NumeroContaOrigem OR NumeroConta = @NumeroContaDestino";

            var parameters = new
            {
                NumeroContaOrigem = transacao.numeroContaOrigem,
                NumeroContaDestino = transacao.numeroContaDestino
            };

            var contas = connection.Query<Conta>(query, parameters).ToList();

            if (contas.Count != 2)
            {
                return BadRequest("Conta(s) inválida(s).");
            }

            var contaOrigem = contas.Single(c => c.numeroConta == transacao.numeroContaOrigem);
            var contaDestino = contas.Single(c => c.numeroConta == transacao.numeroContaDestino);

            if (transacao.valor <= 0)
            {
                return BadRequest("Valor inválido.");
            }

            if (contaOrigem.saldo - transacao.valor < 0)
            {
                return BadRequest("Saldo insuficiente.");
            }

            // Registra a transação
            var insertTransaction = @"INSERT INTO Transacao (NumeroContaOrigem, NumeroContaDestino, Valor, Data) VALUES (@NumeroContaOrigem, @NumeroContaDestino, @Valor, NOW()) RETURNING Id;";

            var transactionParameters = new
            {
                NumeroContaOrigem = transacao.numeroContaOrigem,
                NumeroContaDestino = transacao.numeroContaDestino,
                Valor = transacao.valor
            };

            var transacaoId = connection.QueryFirstOrDefault<int>(insertTransaction, transactionParameters);

            if (transacaoId != 0)
            {
                // Debita da conta de origem e credita na conta de destino
                var updateSaldoOrigem = @"UPDATE Conta SET Saldo = Saldo - @Valor WHERE NumeroConta = @NumeroContaOrigem";
                var updateSaldoDestino = @"UPDATE Conta SET Saldo = Saldo + @Valor WHERE NumeroConta = @NumeroContaDestino";

                var updateParameters = new
                {
                    Valor = transacao.valor,
                    NumeroContaOrigem = transacao.numeroContaOrigem,
                    NumeroContaDestino = transacao.numeroContaDestino
                };

                connection.Execute(updateSaldoOrigem, updateParameters);
                connection.Execute(updateSaldoDestino, updateParameters);

                return Ok(transacaoId);
            }
            else
            {
                return BadRequest("Erro ao registrar transação.");
            }
        }
    }
}