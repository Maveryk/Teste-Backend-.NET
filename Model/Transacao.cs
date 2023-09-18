namespace TesteBackend.Model;

public class Transacao
{
    public int id { get; set; }
    public int numeroContaOrigem { get; set; }
    public int numeroContaDestino { get; set; }
    public double valor { get; set; }
    public DateTime data { get; set; }
}