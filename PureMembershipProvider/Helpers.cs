using System;
using MySql.Data.MySqlClient;

namespace PureDev.Common
{
    public class MembershipHelper : IDisposable
    {
        private readonly string _connectionString;
        
        public MembershipHelper(string connString)
        {
            _connectionString = connString;
        }

        public object ExecuteOdbcScalar(string query, MySqlParameter[] parameters)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddRange(parameters);
                    conn.Open();
                    return cmd.ExecuteScalar();
                }
            }
        }

        public object ExecuteOdbcScalar(string query)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                using (var cmd = new MySqlCommand(query, conn))
                {
                    conn.Open();
                    return cmd.ExecuteScalar();
                }
            }
        }

        public int ExecuteOdbcQuery(string query)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                using (var cmd = new MySqlCommand(query, conn))
                {
                    conn.Open();
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        public int ExecuteOdbcQuery(string query, MySqlParameter[] parameters)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddRange(parameters);
                    conn.Open();
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        public MySqlDataReader ExecuteMySqlReader(string query, MySqlParameter[] parameters)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddRange(parameters);
                    conn.Open();
                    return cmd.ExecuteReader();
                }
            }
        }

        public void ExecuteMySqlReader(string query, Action<MySqlDataReader> action)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                using (var cmd = new MySqlCommand(query, conn))
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        action(reader);
                    }
                }
            }
        }

        public void ExecuteMySqlReader2(string query, MySqlParameter[] parameters, Action<MySqlDataReader> action)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddRange(parameters);

                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        action(reader);
                    }
                }
            }
        }

        public void ExecuteMySqlReader3(string query, MySqlParameter[] parameters, Action<MySqlDataReader> action, out int totalRecords)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddRange(parameters);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        action(reader);
                    }

                    cmd.CommandText = "SELECT FOUND_ROWS() AS `total_items`;";
                    var obj = cmd.ExecuteScalar();
                    totalRecords = obj == DBNull.Value ? 0 : Convert.ToInt32(obj);
                }
            }
        }

        public void ExecuteMySqlReader3(string query, Action<MySqlDataReader> action, out int totalRecords)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                using (var cmd = new MySqlCommand(query, conn))
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        action(reader);
                    }

                    cmd.CommandText = "SELECT FOUND_ROWS() AS `total_items`;";
                    var obj = cmd.ExecuteScalar();
                    totalRecords = obj == DBNull.Value ? 0 : Convert.ToInt32(obj);
                }
            }
        }

        public void Dispose()
        {
            
        }
    }
}
