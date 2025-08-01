## About ##
This example shows how you can create various micro services to create composed views from EventStore into various data storage solutions on the market.  The following are covered:

* SQL Server
* Mongo db
* Redis

Along with direct writers, a set of services have also been implemented for using kafka as a broker, where messages that are stored within EventStore are published onto a Kafka stream, then received by a micro-service to store the projected information into each aforementioned store.

## Prerequisites ##

* Docker / docker-compose
* dotnet 6 sdk

## Setup and Usage ##

1. Run `docker-compose up -d` to bring the database services online.
2. Run `dotnet run --project .\source\DbInitializer\DbInitializer.csproj` to initialize EventStore/SQL Server/Etc.
3. [From within Visual Studio / JetBrains Rider] Run the following projects:
   * RTI.Writers.Kafka
   * RTI.Writers.MongoDB
   * RTI.Writers.RDBMS
   * RTI.Writers.Redis
   * RTI.KafkaConsumer.ToMongo
   * RTI.KafkaConsumer.ToRDBMS
   * RTI.KafkaConsumer.ToRedis
   * RTI.UI
4. After all services are running and the web ui can be accessed, then run `MockInvoiceGenerator` to start pushing data into EventStore.

As new invoices are created, items are added, payments are made, etc., you can navigate the app to see how each data storage product can indeed hold and present the same information as EventStore.  The item to be observed in each scenario is how EventStore's position vs. each downstream data storage product.
