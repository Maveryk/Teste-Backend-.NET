using Dapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using TesteBackend.Model;
using TesteBackend.Services;

namespace TesteBackend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly string _connectionString;

    public AuthController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    /// <summary>
    /// Endpoint para realizar o login com e-mail e senha
    /// </summary>
    /// <response code="200">Returna o Token</response>
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginModel login)
    {
        using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
        {
            var query = @"SELECT *
                      FROM Conta
                      WHERE Email = @Email AND Senha = @Senha";

            var parameters = new
            {
                Email = login.email,
                Senha = login.senha
            };

            var conta = connection.QueryFirstOrDefault<Conta>(query, parameters);

            if (conta != null)
            {
                var token = TokenService.GenerateToken(conta);
                return Ok(token);
            }
            else
            {
                return BadRequest("E-mail ou senha inválidos.");
            }
        }
    }
}