using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Universe.SqlInsights.Shared;

namespace Universe.SqlInsights.W3Api.Controllers
{
    [ApiController]
    [Route("api/" + ApiVer.V1 + "/FakeHealthCheck/[action]")]
    public class FakeHealthCheckController : ControllerBase
    {
        private DbOptions DbOptions;

        public FakeHealthCheckController(DbOptions dbOptions)
        {
            DbOptions = dbOptions;
        }

        [HttpGet]
        public ActionResult IsOK()
        {
            using (SqlConnection con = new SqlConnection(DbOptions.ConnectionString))
            {
                con.Open();
                ErrorsSandbox.CheckHealth(con);
            }

            return Ok(new {OK = true});
        }

        public static class ErrorsSandbox
        {
            static int CaseCounter = -1;
            static object Sync = new object();

            public static void CheckHealth(IDbConnection con)
            {
                if (con.State != ConnectionState.Open) con.Open();
                var ver = con.Query($"Select @@Version as [Ping] /* {CaseCounter:n0} */").FirstOrDefault();

                int next;
                lock (Sync) next = CaseCounter = (CaseCounter + 1) % 10;

                if (next == 0)
                {
                    Debug.WriteLine("Raise Argument Out Of Range Exception");
                    throw new ArgumentOutOfRangeException("shipmentDate", "Some argument is out of range");
                }

                if (next == 1)
                    RaiseSqlDeadLock(con);

                if (next == 2)
                    RaiseSqlCommandTimeout(con);

                if (next == 3)
                    RaiseUniqueConstraintViolation(con);

                if (next == 4)
                    RaiseSqlForeignKeyViolation(con);
            }

            static void RaiseUniqueConstraintViolation(IDbConnection con)
            {
                Debug.WriteLine("Raise Unique Constraint Violation");

                var cmds = new[]
                {
                    "If OBJECT_ID('UniqueUserEmailSandbox') Is Null Create Table UniqueUserEmailSandbox(email nvarchar(123), CONSTRAINT UNIQUE_USER_EMAIL UNIQUE(email))",
                    "Insert UniqueUserEmailSandbox(email) Values('john.doe@gmail.com')",
                    "Insert UniqueUserEmailSandbox(email) Values('john.doe@gmail.com')",
                };

                foreach (var cmd in cmds)
                {
                    con.Execute(cmd);
                }
            }

            static void RaiseSqlCommandTimeout(IDbConnection con)
            {
                Debug.WriteLine("Raise Sql Command Timeout");

                var cmd = @"
Declare @now datetime; 
Set @now = GETDATE(); 
While DATEDIFF(second,  @now, GetDate()) < 42 
Begin 
    Declare @nop int 
End;";
                con.Execute(cmd, commandTimeout: 1);
            }

            static void RaiseSqlDeadLock(IDbConnection con)
            {
                Debug.WriteLine("Raise Dead Lock");

                var cmds = new[]
                {
                    "Begin Tran",
                    "CREATE TYPE dbo.GodType_42_31415926 AS TABLE(Value0 Int NOT NULL, Value1 Int NOT NULL)",
                    "Declare @myPK dbo.GodType_42_31415926",
                    "Rollback"
                };

                foreach (var cmd in cmds)
                {
                    con.Execute(cmd);
                }
            }

            static void RaiseSqlForeignKeyViolation(IDbConnection con)
            {
                Debug.WriteLine("Raise Dead Lock");

                var cmds = new[]
                {
                    "If Object_ID('Parent_Sandbox') Is Null Create Table Parent_Sandbox(Id UniqueIdentifier Primary Key)",
                    "If Object_ID('Children_Sandbox') Is Null Create Table Children_Sandbox(IdParent UniqueIdentifier, CONSTRAINT [FK_Parent_Child_Id] FOREIGN KEY ([IdParent]) REFERENCES [Parent_Sandbox] ([Id]))",
                    "Insert Children_Sandbox(IdParent) Select NewId()",
                };

                foreach (var cmd in cmds)
                {
                    con.Execute(cmd);
                }
            }
        }
    }
}