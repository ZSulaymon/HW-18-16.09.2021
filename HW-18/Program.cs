using System;
using System.Data.SqlClient;

namespace HW_18
{
    class Program
    {
        static void Main(string[] args)
        {
            var conString = ""+
               "Data source=localhost; " +
                "Initial catalog=AcademyHW; " +
                "user id=sa; " +
                "password=123456";

            var conn = new SqlConnection(conString);
            conn.Open();

            Console.Write("1. Create account\n2. Show account\n3. Transfer\nChoice:");
            int.TryParse(Console.ReadLine(), out var choice);

            switch (choice)
            {
                case 1:
                    {
                        CreateAccount(conString);
                    }
                    break;
                case 2:
                    {
                        ShowAccounts(conString);
                    }
                    break;
                case 3:
                    {
                        var fromAcc = "";
                        var fromAccId = 0;
                        var count = 0;
                        while (fromAccId == 0)
                        {
                            if (count == 3) { Console.WriteLine("Попробуйте через 5-мин"); return; }
                            count++;
                            Console.Write("From account:");
                            fromAcc = Console.ReadLine();
                            fromAccId = GetAccountId(fromAcc, conString);
                            if (fromAccId == 0) { Console.WriteLine($"Account { fromAcc } not found"); }
                        }

                        var toAcc = "";
                        var toAccId = 0;
                        count = 0;
                        while (toAccId == 0)
                        {
                            if (count == 3) { Console.WriteLine("Попробуйте через 5-мин"); return; }
                            count++;
                            Console.Write("To account:");
                            toAcc = Console.ReadLine();
                            toAccId = GetAccountId(toAcc, conString);
                            if (count == 4) { Console.WriteLine("Попробуйте через 5-мин"); return; }
                        }
                        Console.Write("Amount:");
                        decimal.TryParse(Console.ReadLine(), out var amount);

                        TransferFromToAcc(fromAcc, toAcc, amount, conString);

                    }
                    break;
                
                default:
                    Console.WriteLine("Wrong choose.");
                    break;
            }


        }

        private static void TransferFromToAcc(string fromAcc, string toAcc, decimal amount, string conString)
        {
            if (string.IsNullOrEmpty(fromAcc) || string.IsNullOrEmpty(toAcc) || amount == 0)
            {
                Console.WriteLine("Something went wrong.");
                return;
            }

            var conn = new SqlConnection(conString);
            conn.Open();

            if (!(conn.State == System.Data.ConnectionState.Open))
            {
                return;
            }

            SqlTransaction sqlTransaction = conn.BeginTransaction();

            var command = conn.CreateCommand();

            command.Transaction = sqlTransaction;

            try
            {
                var fromAccBalance = GetAccountBalance(conString, fromAcc);
                var fromAccId = GetAccountId(fromAcc, conString);

                if (fromAccBalance <= 0 || (fromAccBalance - amount) < 0)
                {
                    if (fromAccBalance <= 0)
                    {
                        command.CommandText = "UPDATE [dbo].[Accounts] SET [IS_Active] = 0 WHERE Id = @accountId";
                        command.Parameters.AddWithValue("@accountId", fromAccId);
                        command.ExecuteNonQuery();
                        sqlTransaction.Commit();
                        //Console.WriteLine($"Balance this account {fromAccBalance}");
                        throw new Exception($"Balance this account {fromAccBalance}"); 
                    }
                    throw new Exception("From account balance not enough amount");
                }


                if (fromAccId == 0)
                {
                    throw new Exception("Account not found");
                }

                command.CommandText = "INSERT INTO [dbo].[Transactions]([Amount] ,[Type] ,[Created_At] ,[Account_Id]) VALUES (@amount , 'C' , @createdAt, @accountId)";
                command.Parameters.AddWithValue("@amount", amount);
                command.Parameters.AddWithValue("@createdAt", DateTime.Now);
                command.Parameters.AddWithValue("@accountId", fromAccId);

                var result1 = command.ExecuteNonQuery();

                var toAccId = GetAccountId(toAcc, conString);

                if (toAccId == 0)
                {
                    throw new Exception("Account not found");
                }

                command.Parameters.Clear();

                //if (fromAccBalance > 0)
                //{
                //    command.CommandText = "UPDATE [dbo].[Accounts] SET [IS_Active] = 1 WHERE Id = @accountId";
                //    command.Parameters.AddWithValue("@accountId", fromAccId);
                //    command.ExecuteNonQuery();
                //    sqlTransaction.Commit();
                //    //Console.WriteLine($"Balance this account {fromAccBalance}");
                //}
                command.CommandText = "INSERT INTO [dbo].[Transactions]([Amount] ,[Type] ,[Created_At] ,[Account_Id]) VALUES (@amount , 'D' , @createdAt, @accountId)";
                command.Parameters.AddWithValue("@amount", amount);
                command.Parameters.AddWithValue("@createdAt", DateTime.Now);
                command.Parameters.AddWithValue("@accountId", toAccId);


                var result2 = command.ExecuteNonQuery();


                if (result1 == 0 || result2 == 0)
                {
                    throw new Exception("Something went wrong");
                }

                sqlTransaction.Commit();
                Console.WriteLine("All done");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                sqlTransaction.Rollback();
            }
            finally
            {
                conn.Close();
            }
        }


        private static void ShowAccounts(string conString)
        {
            Accounts[] accounts = new Accounts[0];

            var connection = new SqlConnection(conString);
            var query = "SELECT [Id],[Account],[IS_Active],[Created_at],[Updated_at] FROM [dbo].[Accounts]";

            var command = connection.CreateCommand();
            command.CommandText = query;

            connection.Open();

            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var acc = new Accounts { };

                acc.Id = int.Parse(reader["Id"].ToString());
                acc.Account = reader["Account"].ToString();
                acc.Is_Active = int.Parse(reader["Is_Active"].ToString());
                acc.Created_At  =DateTime.Parse(reader["Created_At"].ToString());
                //acc.Updated_At = DateTime.Parse(reader["Updated_At"].ToString());

                // acc.Created_At = !string.IsNullOrEmpty(reader["Created_At"]?.ToString()) ? DateTime.Parse(reader["Created_At"].ToString()) ;
                //acc.Updated_At = !string.IsNullOrEmpty(reader["Updated_At"]?.ToString()) ? DateTime.Parse(reader["Updated_At"].ToString()) : null;
                ShowAcc(ref accounts, acc);
            }
            connection.Close();
            foreach (var acc in accounts)
            {
                Console.WriteLine($"ID:{acc.Id}, Account:{acc.Account}, Is_Active:{acc.Is_Active}, CreatedAT:{acc.Created_At}, UpdatedAt:{acc.Updated_At}");
            }
        }

        private static void ShowAcc(ref Accounts[] account, Accounts acc)
        {
            if (account == null)
            {
                return;
            }

            Array.Resize(ref account, account.Length + 1);

            account[account.Length - 1] = acc;
        }

        private static void CreateAccount(string conString)
        {
            var acc = new Accounts();
            acc.Account = Console.ReadLine();
         

            var connection = new SqlConnection(conString);
            var query = "INSERT INTO [dbo].[Accounts]([Account] ) VALUES (@acc)";

            var command = connection.CreateCommand();
            command.CommandText = query;
            command.Parameters.AddWithValue("@acc", acc.Account);
            
            connection.Open();

            var result = command.ExecuteNonQuery();



            if (result > 0)
            {
                Console.WriteLine("Added successfully.");
            }

            connection.Close();
        }


        private static decimal GetAccountBalance(string conString, string account)
        {
            var conn = new SqlConnection(conString);
            conn.Open();
            var command = conn.CreateCommand();
            command.CommandText = "select sum( case when t.Type = 'C' then t.Amount * -1 else t.Amount end) from Transactions t left join Accounts a on t.Account_Id = a.Id where a.account = @fromAcc";
            command.Parameters.AddWithValue("@fromAcc", account);
            var reader = command.ExecuteReader();
            var fromAccBalance = 0m;

            while (reader.Read())
            {
                fromAccBalance = !string.IsNullOrEmpty(reader.GetValue(0)?.ToString()) ? reader.GetDecimal(0) : 0;
            }

            reader.Close();
            command.Parameters.Clear();

            conn.Close();
            return fromAccBalance;
        }



        private static int GetAccountId(string number, string conString)
        {
            var accNumber = 0;
            var connection = new SqlConnection(conString);
            var query = "SELECT [Id] FROM [dbo].[Accounts] WHERE [Account] = @number";

            var command = connection.CreateCommand();
            command.Parameters.AddWithValue("@number", number);
            command.CommandText = query;

            connection.Open();

            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                accNumber = reader.GetInt32(0);
            }
            connection.Close();
            reader.Close();

            return accNumber;
        }
    }

    public class Accounts
    {
        public  int Id { get; set; }
        public string Account { get; set; }
        public  int Is_Active { get; set; }
        public  DateTime Created_At { get; set; }
        public  DateTime? Updated_At { get; set; }

    }

    public class Transaction
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
