using System;
using MySql.Data.MySqlClient;

namespace PureDev.Common
{
    public class MembershipHelper : IDisposable
    {
        private readonly string _connectionString;
        private readonly MySqlConnection _connection;

        public MembershipHelper(string connString)
        {
            _connectionString = connString;
            _connection = new MySqlConnection(_connectionString);
            _connection.Open();
        }

        public object ExecuteOdbcScalar(string query, MySqlParameter[] parameters)
        {
            using (var cmd = new MySqlCommand(query, _connection))
            {
                cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteScalar();
            }

        }

        public object ExecuteOdbcScalar(string query)
        {
            using (var cmd = new MySqlCommand(query, _connection))
            {
                return cmd.ExecuteScalar();
            }
        }

        public int ExecuteOdbcQuery(string query)
        {
            using (var cmd = new MySqlCommand(query, _connection))
            {
                return cmd.ExecuteNonQuery();
            }
        }

        public int ExecuteOdbcQuery(string query, MySqlParameter[] parameters)
        {
            using (var cmd = new MySqlCommand(query, _connection))
            {
                cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteNonQuery();
            }
        }

        public MySqlDataReader ExecuteMySqlReader(string query, MySqlParameter[] parameters)
        {
            using (var cmd = new MySqlCommand(query, _connection))
            {
                cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteReader();
            }
        }

        public void ExecuteMySqlReader(string query, Action<MySqlDataReader> action)
        {
            using (var cmd = new MySqlCommand(query, _connection))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    action(reader);
                }
            }
        }

        public void ExecuteMySqlReader2(string query, MySqlParameter[] parameters, Action<MySqlDataReader> action)
        {
            using (var cmd = new MySqlCommand(query, _connection))
            {
                cmd.Parameters.AddRange(parameters);

                using (var reader = cmd.ExecuteReader())
                {
                    action(reader);
                }
            }
        }

        public void ExecuteMySqlReader3(string query, MySqlParameter[] parameters, Action<MySqlDataReader> action, out int totalRecords)
        {

            using (var cmd = new MySqlCommand(query, _connection))
            {
                cmd.Parameters.AddRange(parameters);
                using (var reader = cmd.ExecuteReader())
                {
                    action(reader);
                }

                cmd.CommandText = "SELECT FOUND_ROWS() AS `total_items`;";
                var obj = cmd.ExecuteScalar();
                totalRecords = obj == DBNull.Value ? 0 : Convert.ToInt32(obj);
            }
        }

        public void Dispose()
        {
            _connection.Close();
            _connection.Dispose();
        }
    }
}
