using Microsoft.Data.SqlClient;

namespace Gestao;

public class ClienteRepositorio
{
    private readonly string _ligacao;

    public ClienteRepositorio(string ligacao) => _ligacao = ligacao;

    private SqlConnection Abrir()
    {
        var con = new SqlConnection(_ligacao);
        con.Open();
        return con;
    }

    private static void Validar(Cliente c)
    {
        if (string.IsNullOrWhiteSpace(c.Nome))
            throw new ArgumentException("O nome é obrigatório.");
    }

    private static void Preencher(SqlCommand cmd, Cliente c)
    {
        cmd.Parameters.AddWithValue("@n", c.Nome.Trim());
        cmd.Parameters.AddWithValue("@c", (c.Contacto ?? "").Trim());
        cmd.Parameters.AddWithValue("@o", (c.Notas ?? "").Trim());
    }

    public long Adicionar(Cliente c)
    {
        Validar(c);
        using var con = Abrir();
        using var cmd = con.CreateCommand();
        cmd.CommandText =
            "INSERT INTO dbo.Clientes (Nome, Contacto, Notas) OUTPUT INSERTED.Id VALUES (@n, @c, @o)";
        Preencher(cmd, c);
        return Convert.ToInt64(cmd.ExecuteScalar());
    }

    public List<Cliente> Listar(string? pesquisa = null)
    {
        var lista = new List<Cliente>();
        using var con = Abrir();
        using var cmd = con.CreateCommand();
        var sql = "SELECT Id, Nome, Contacto, Notas FROM dbo.Clientes";
        if (!string.IsNullOrWhiteSpace(pesquisa))
        {
            var p = pesquisa.Trim().Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
            sql += " WHERE Nome LIKE @p OR Contacto LIKE @p OR Notas LIKE @p";
            cmd.Parameters.AddWithValue("@p", "%" + p + "%");
        }
        cmd.CommandText = sql + " ORDER BY Nome";
        using var leitor = cmd.ExecuteReader();
        while (leitor.Read())
        {
            lista.Add(new Cliente
            {
                Id = leitor.GetInt64(0),
                Nome = leitor.GetString(1),
                Contacto = leitor.GetString(2),
                Notas = leitor.GetString(3)
            });
        }
        return lista;
    }

    public bool Atualizar(Cliente c)
    {
        Validar(c);
        using var con = Abrir();
        using var cmd = con.CreateCommand();
        cmd.CommandText =
            "UPDATE dbo.Clientes SET Nome = @n, Contacto = @c, Notas = @o WHERE Id = @id";
        Preencher(cmd, c);
        cmd.Parameters.AddWithValue("@id", c.Id);
        return cmd.ExecuteNonQuery() > 0;
    }

    public bool Remover(long id)
    {
        using var con = Abrir();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "DELETE FROM dbo.Clientes WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        return cmd.ExecuteNonQuery() > 0;
    }
}
