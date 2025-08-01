using System.Data.Common;

using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

using Microsoft.Data.SqlClient;

using MongoDB.Driver;

using RTI;
using RTI.ReactiveDomain;
using RTI.UI;

using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
       .AddControllersWithViews();

// setup of database connections used in the application.
builder.Services.AddSingleton(sp => {
    var credentials = new UserCredentials("admin", "changeit");
    var connectionSettings = ConnectionSettings.Create()
        .SetDefaultUserCredentials(credentials);
    var eventStore = EventStoreConnection.Create(Strings.DatabaseConnections.EventStore, connectionSettings, "UI - Web Console");
    eventStore.ConnectAsync().Wait();
    return eventStore;
});
builder.Services.AddSingleton<IConfiguredConnection>(sp => new ConfiguredConnection(
            new global::ReactiveDomain.EventStore.EventStoreConnectionWrapper(sp.GetRequiredService<IEventStoreConnection>()),
            new global::ReactiveDomain.Foundation.PrefixedCamelCaseStreamNameBuilder(),
            new global::ReactiveDomain.Foundation.JsonMessageSerializer()));
builder.Services.AddScoped<DbConnection>(_ => {
    var connection = new SqlConnection(Strings.DatabaseConnections.SqlServer);
    connection.Open();
    return connection;
});
builder.Services.AddSingleton(sp => {
    var redis = ConnectionMultiplexer.Connect(Strings.DatabaseConnections.Redis); // kafka models
    return redis.GetDatabase();
});
builder.Services.AddSingleton(sp => {
    var db = new MongoClient(new MongoUrl(Strings.DatabaseConnections.MongoDb));
    return db.GetDatabase("demo");
});

builder.Services.AddSingleton<InvoiceListRm>();
builder.Services.AddSingleton<CurrentBackplaneVm>(sp => {
    var vm = new CurrentBackplaneVm(sp.GetRequiredService<IEventStoreConnection>());
    vm.StartAsync().GetAwaiter().GetResult();
    return vm;
});

builder.Services.AddSingleton<ICheckpoints, EventStoreCheckpoints>();
builder.Services.AddSingleton<ICheckpoints, MongodbCheckpoints>();
builder.Services.AddScoped<ICheckpoints, RelationalDBCheckpoints>();
builder.Services.AddSingleton<ICheckpoints, RedisCheckpoints>();


var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
