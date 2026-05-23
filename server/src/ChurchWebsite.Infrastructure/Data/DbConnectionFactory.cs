using System.Data;
using Npgsql;

namespace ChurchWebsite.Infrastructure.Data;

public class DbConnectionFactory(string connectionString)
{
    public IDbConnection CreateConnection() => new NpgsqlConnection(connectionString);
}
