using Npgsql;
using Dapper;
using UserManagement.DAL.Interfaces;

public class HashIntegrityRepository : IHashIntegrityRepository
{
    private readonly string _connectionString;

    public HashIntegrityRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("CommonLogDBConnection");
    }

    public async Task<bool> VerifyHashChain(string hash, int depth)
    {
        using var conn = new NpgsqlConnection(_connectionString);

        return await conn.ExecuteScalarAsync<bool>(
            "SELECT public.verify_hash_chain_both_sides(@hash, @depth)",
            new { hash, depth }
        );
    }
}
