using System.Data;

namespace AdminShell.Infrastructure.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}