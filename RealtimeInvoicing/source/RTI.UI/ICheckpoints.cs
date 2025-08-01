namespace RTI.UI {
    using System.Data.Common;

    using Dapper;

    using MongoDB.Driver;

    using RTI.Models;

    using StackExchange.Redis;

    public interface ICheckpoints {
        string Name { get; }
        Checkpoint[] GetCheckpoints();

    }

    internal class EventStoreCheckpoints : ICheckpoints {
        private readonly InvoiceListRm _invoiceListRm;

        public string Name => "EventStore";

        public EventStoreCheckpoints(InvoiceListRm invoiceListRm) {
            _invoiceListRm = invoiceListRm ?? throw new ArgumentNullException(nameof(invoiceListRm));
        }

        public Checkpoint[] GetCheckpoints() => new[] { new Checkpoint { Id = "$ce-invoice", Position = _invoiceListRm.Position ?? 0 } };
    }

    internal class MongodbCheckpoints : ICheckpoints {
        IMongoCollection<Checkpoint> _checkpoints;

        public string Name => "Mongo DB";

        public MongodbCheckpoints(IMongoDatabase mongo) {
            _checkpoints = mongo.GetCollection<Checkpoint>("checkpoints");
        }

        public Checkpoint[] GetCheckpoints() => _checkpoints.AsQueryable().ToArray();
    }

    internal class RelationalDBCheckpoints : ICheckpoints {
        private readonly DbConnection _connection;

        public string Name => "Relational DB (Sql Server)";

        public RelationalDBCheckpoints(DbConnection connection) {
            _connection = connection;
        }

        public Checkpoint[] GetCheckpoints() =>_connection.Query<Checkpoint>("SELECT [StreamName] as [Id], [Position] FROM [Checkpoints] WHERE [StreamName] = 'invoice_header';").ToArray();
    }

    internal class RedisCheckpoints : ICheckpoints {
        private readonly IDatabase _redis;

        public string Name => "Redis";

        public RedisCheckpoints(IDatabase redis) {
            _redis = redis;
        }

        public Checkpoint[] GetCheckpoints() => _redis.HashGetAll("checkpoints")
                .Select(he => new Checkpoint { Id = he.Name, Position = Convert.ToInt64(he.Value) })
                .ToArray();
    }
}
