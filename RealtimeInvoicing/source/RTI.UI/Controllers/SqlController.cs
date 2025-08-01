namespace RTI.UI.Controllers {
    using System.Data.Common;

    using Dapper;

    using Microsoft.AspNetCore.Mvc;

    public class SqlController : Controller {
        DbConnection _connection;
        ILogger _log;

        public SqlController(DbConnection connection, ILoggerFactory loggerFactory) {
            _connection = connection;
            _log = loggerFactory.CreateLogger<SqlController>();
        }

        public async Task<IActionResult> Index() {
            var multi = await _connection.QueryMultipleAsync("SELECT * FROM Invoices;SELECT [StreamName] as [Id], [Position] FROM [Checkpoints] WHERE [StreamName] = 'invoice_header';");

            var invoices = multi.Read<RTI.Models.Invoice>();
            ViewBag.Checkpoints = multi.Read<RTI.Models.Checkpoint>().ToArray();
            return View("InvoiceList", invoices);
        }

        public async Task<IActionResult> Details(Guid id) {
            var queries = @"SELECT [Id],[AccountId],[AccountName],[PaymentTermsId],[PaymentTermsName],[Date],[Status],[ItemsTotal] as [Total],[PaymentsTotal],[BalanceDue] FROM [dbo].[Invoices] WHERE [Id] = @invoiceId; 
SELECT [LineItemId] as [Id],[ItemId],[SKU],[Description],[UnitPrice],[SubTotal],[HasBeenRemoved],[QTY] as [Quantity] FROM [dbo].[InvoiceItems] where [InvoiceId] = @invoiceId;
SELECT [PaymentId] as [Id],[Amount],[Received],[Voided] FROM [dbo].[InvoicePayments] WHERE [InvoiceId] = @invoiceId;";
            var multi = await _connection.QueryMultipleAsync(queries, new {invoiceId = id});

            var model = multi.Read<RTI.Models.Invoice>().Single();
            model.Items = multi.Read<RTI.Models.InvoiceItem>().ToList();
            model.Payments = multi.Read<RTI.Models.InvoicePayment>().ToList();

            return View("InvoiceDetails", model);
        }
    }
}
