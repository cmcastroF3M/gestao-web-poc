using Microsoft.Data.SqlClient;

namespace Gestao;

public static class BaseDados
{
    public const string LigacaoPadrao =
        @"Server=.\SQLEXPRESS;Database=Gestao;Integrated Security=True;TrustServerCertificate=True";

    // Se existir um ficheiro "ligacao.txt" ao lado do EXE, usa a ligação que lá estiver.
    public static string ObterLigacao()
    {
        var ficheiro = Path.Combine(AppContext.BaseDirectory, "ligacao.txt");
        if (File.Exists(ficheiro))
        {
            var texto = File.ReadAllText(ficheiro).Trim();
            if (texto.Length > 0) return texto;
        }
        return LigacaoPadrao;
    }

    private static string NomeSeguro(string nome) => "[" + nome.Replace("]", "]]") + "]";

    private static (string nome, string master) Separar(string ligacao)
    {
        var b = new SqlConnectionStringBuilder(ligacao);
        var nome = b.InitialCatalog;
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("A ligação tem de indicar a base de dados (Database=...).");
        b.InitialCatalog = "master";
        return (nome, b.ToString());
    }

    // Cria a base de dados e a tabela, se ainda não existirem.
    public static void Garantir(string ligacao)
    {
        var (nome, master) = Separar(ligacao);

        using (var con = new SqlConnection(master))
        {
            con.Open();
            using var cmd = con.CreateCommand();
            cmd.CommandText = $"IF DB_ID(@n) IS NULL CREATE DATABASE {NomeSeguro(nome)}";
            cmd.Parameters.AddWithValue("@n", nome);
            cmd.ExecuteNonQuery();
        }

        using var con2 = new SqlConnection(ligacao);
        con2.Open();
        using var cmd2 = con2.CreateCommand();
        cmd2.CommandText =
            "IF OBJECT_ID(N'dbo.Clientes', N'U') IS NULL " +
            "CREATE TABLE dbo.Clientes (" +
            "Id BIGINT IDENTITY(1,1) PRIMARY KEY, " +
            "Nome NVARCHAR(200) NOT NULL, " +
            "Contacto NVARCHAR(100) NOT NULL DEFAULT N'', " +
            "Notas NVARCHAR(MAX) NOT NULL DEFAULT N'')";
        cmd2.ExecuteNonQuery();
    }

    // Usado nos testes para apagar a base de dados temporária.
    public static void Eliminar(string ligacao)
    {
        var (nome, master) = Separar(ligacao);
        SqlConnection.ClearAllPools();
        using var con = new SqlConnection(master);
        con.Open();
        using var cmd = con.CreateCommand();
        var n = NomeSeguro(nome);
        cmd.CommandText =
            "IF DB_ID(@n) IS NOT NULL BEGIN " +
            $"ALTER DATABASE {n} SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
            $"DROP DATABASE {n}; END";
        cmd.Parameters.AddWithValue("@n", nome);
        cmd.ExecuteNonQuery();
    }
}
