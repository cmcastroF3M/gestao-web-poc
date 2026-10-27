using Gestao;
using Microsoft.Data.SqlClient;
using Xunit;

public class ClienteRepositorioTests : IDisposable
{
    private readonly string _ligacao;
    private readonly ClienteRepositorio _repo;

    public ClienteRepositorioTests()
    {
        var baseLigacao = Environment.GetEnvironmentVariable("GESTAO_TEST_CONNECTION")
            ?? @"Server=.\SQLEXPRESS;Integrated Security=True;TrustServerCertificate=True";

        var b = new SqlConnectionStringBuilder(baseLigacao)
        {
            InitialCatalog = "Teste_" + Guid.NewGuid().ToString("N")
        };
        _ligacao = b.ToString();

        // O SQL Server de teste pode demorar a ficar pronto: tenta durante cerca de 1 minuto.
        for (var i = 0; ; i++)
        {
            try { BaseDados.Garantir(_ligacao); break; }
            catch (SqlException) when (i < 30) { Thread.Sleep(2000); }
        }

        _repo = new ClienteRepositorio(_ligacao);
    }

    public void Dispose() => BaseDados.Eliminar(_ligacao);

    [Fact]
    public void Adicionar_e_listar_devolve_o_cliente()
    {
        _repo.Adicionar(new Cliente { Nome = "Ana Silva", Contacto = "912345678" });

        var lista = _repo.Listar();

        Assert.Single(lista);
        Assert.Equal("Ana Silva", lista[0].Nome);
    }

    [Fact]
    public void Atualizar_altera_os_dados()
    {
        var id = _repo.Adicionar(new Cliente { Nome = "Ana" });

        var ok = _repo.Atualizar(new Cliente { Id = id, Nome = "Ana Maria", Notas = "VIP" });

        var c = _repo.Listar().Single();
        Assert.True(ok);
        Assert.Equal("Ana Maria", c.Nome);
        Assert.Equal("VIP", c.Notas);
    }

    [Fact]
    public void Remover_apaga_o_cliente()
    {
        var id = _repo.Adicionar(new Cliente { Nome = "Rui" });

        Assert.True(_repo.Remover(id));
        Assert.Empty(_repo.Listar());
    }

    [Fact]
    public void Pesquisa_filtra_por_nome()
    {
        _repo.Adicionar(new Cliente { Nome = "Ana Silva" });
        _repo.Adicionar(new Cliente { Nome = "Rui Costa" });

        var lista = _repo.Listar("costa");

        Assert.Single(lista);
        Assert.Equal("Rui Costa", lista[0].Nome);
    }

    [Fact]
    public void Nome_vazio_e_rejeitado()
    {
        Assert.Throws<ArgumentException>(() => _repo.Adicionar(new Cliente { Nome = "   " }));
    }
}
