using SqlSugar;
using System.Reflection;

namespace ERP_Web.Repository
{
    public class SqlClient
    {
        public static string connStr
        {
            get
            {
                ConfigurationBuilder cb = new ConfigurationBuilder();
                cb.SetBasePath(Directory.GetCurrentDirectory())
                     .AddJsonFile("appsettings.json");
                var config = cb.Build();
                return config.GetConnectionString("DefaultConnection");
            }
        }

        public SqlSugarClient Db = new SqlSugarClient(
            new ConnectionConfig()
            {
                //ConnectionString = "datasource=demo.db",
                ConnectionString = connStr,
                DbType = DbType.SqlServer,
                IsAutoCloseConnection = true,  //自动释放
                                               //写代码就不需要考虑 open close 直接用就行了
                                               //情况1：没有事务的情况 ，每次操作自动调用 open和close
                                               //情况2： 有事务的情况下 ，开启事务调用 open  提交或者回滚事务调用 close
                ConfigureExternalServices = new ConfigureExternalServices
                {
                    //注意:  这儿AOP设置不能少
                    EntityService = (c, p) =>
                    {
                        /***低版本C#写法***/
                        // int?  decimal?这种 isnullable=true 不支持string(下面.NET 7支持)
                        if (p.IsPrimarykey == false && c.PropertyType.IsGenericType &&
                        c.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            p.IsNullable = true;
                        }

                        /***高版C#写法***/
                        //支持string?和string  
                        if (p.IsPrimarykey == false && new NullabilityInfoContext()
                         .Create(c).WriteState is NullabilityState.Nullable)
                        {
                            p.IsNullable = true;
                        }
                    }
                }
            },
            db =>
            {
                //(A)全局生效配置点，一般AOP和程序启动的配置扔这里面 ，所有上下文生效
                //调试SQL事件，可以删掉
                db.Aop.OnLogExecuting = (sql, pars) =>
                {
                    //获取原生SQL推荐 5.1.4.63  性能OK
                    Console.WriteLine(UtilMethods.GetNativeSql(sql, pars));

                    //获取无参数化SQL 对性能有影响，特别大的SQL参数多的，调试使用
                    //Console.WriteLine(UtilMethods.GetSqlString(DbType.SqlServer,sql,pars))
                };
                //多个配置就写下面
                //db.Ado.IsDisableMasterSlaveSeparation=true;

                //注意多租户 有几个设置几个
                //db.GetConnection(i).Aop
            }
        );
    }
}
