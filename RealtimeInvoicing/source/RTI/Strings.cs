namespace RTI {
    public static class Strings {
        public static class DatabaseConnections {
            public static string SqlServerAdmin = "Server=localhost;Database=master;User Id=sa;Password=9x1P:JsNhEC%42ZWO;MultipleActiveResultSets=true;TrustServerCertificate=True;";
            public static string SqlServer = "Server=localhost;Database=demo;User Id=sa;Password=9x1P:JsNhEC%42ZWO;MultipleActiveResultSets=true;TrustServerCertificate=True;";
            public static string Kafka = "bootstrap.servers=localhost:9092";
            public static string Redis = "localhost:6379";
            public static string MongoDb = "mongodb://admin:changeit@localhost:27017/";
            public static string EventStore = "ConnectTo=tcp://localhost:1113;HeartbeatTimeout=300000;HeartbeatInterval=5000";
        }

        public static class Streams {
            public static string InvoiceDocuments = "invoice_documents";
        }

        public static class Collections {
            public static string Invoice = "invoice";
            public static string Checkpoints = "checkpoints";
        }

        public static class Kafka {
            public static class Topics {
                public static string MongoInvoiceDocuments = "invoice_documents_mongo";
                public static string RedisInvoiceDocuments = "invoice_documents_redis";
                public static string RDBMSInvoiceHeaders = "invoice_header_rdbms";
                public static string RDBMSInvoiceItems = "invoice_item_rdbms";
                public static string RDBMSInvoicePayments = "invoice_pmt_rdbms";
            }
            public static class Groups {
                public static string MongoInvoiceDocuments = "invoice_documents_mongo_group";
                public static string RedisInvoiceDocuments = "invoice_documents_redis_group";
                public static string RDBMSInvoiceHeaders = "invoice_header_rdbms_group";
                public static string RDBMSInvoiceItems = "invoice_item_rdbms_group";
                public static string RDBMSInvoicePayments = "invoice_pmt_rdbms_group";
            }
        }
    }
}
