namespace MockInvoiceGenerator {
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    using ReactiveDomain;
    using ReactiveDomain.Messaging;

    using RTI;
    using RTI.ReactiveDomain;

    internal class Main : BackgroundService {
        private readonly ICorrelatedRepository _repository;
        private readonly LookupsRm _rm;
        private readonly ICorrelatedMessage _msg;
        private readonly Random _rng = new();
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _log;

        public Main(IConfiguredConnection conn, LookupsRm rm, ILoggerFactory loggerFactory) {
            _repository = conn.GetCorrelatedRepository();
            _rm = rm;
            _msg = MessageBuilder.New(() => new DefaultCommand());
            _loggerFactory = loggerFactory;
            _log = loggerFactory.CreateLogger<Main>();
        }


        protected override async Task ExecuteAsync(CancellationToken token) {
            do {
                _log.LogInformation("Producing 10 invoices.");
                List<Task> generators = new();
                foreach (var _ in Enumerable.Range(1, 10)) {
                    generators.Add(GenerateAnInvoice(_, CancellationToken.None));
                    await Task.Delay(500);
                }
                await Task.WhenAll(generators);

                _log.LogInformation("10 invoices have been produced.");
                await Task.Delay(TimeSpan.FromSeconds(10), token);
            } while (!token.IsCancellationRequested);
        }

        async Task GenerateAnInvoice(int threadNumber, CancellationToken token = default) {
            var log = _loggerFactory.CreateLogger($"Thread: {threadNumber}");
            try {
                var acct = _rm.GetRandomAccount(_msg);
                var terms = _rm.GetRandomPaymentTerms(_msg);
                var nbrOfItems = _rng.Next(1, 10);
                var pmtId = Guid.NewGuid();

                var invoice = new Invoice(Guid.NewGuid(), acct, DateTime.Now, _msg);
                invoice.Set(terms);
                StoreAggregate(invoice);
                log.LogInformation("Invoice {@invoiceId} has been generated", invoice.Id);

                for (var i = 0; i < nbrOfItems; i++) {
                    var lineItemId = Guid.NewGuid();

                    invoice.Add(lineItemId, _rm.GetRandomItem(_msg), _rng.Next(1, 10));
                    StoreAggregate(invoice);
                    log.LogInformation("Line item {@lineItemId} has been added from {@invoiceId}", lineItemId, invoice.Id);

                    await Task.Delay(TimeSpan.FromSeconds(2), token);

                    if (_rng.Next(1, 100) % 3 == 0) {
                        invoice.Remove(lineItemId);
                        StoreAggregate(invoice);
                        log.LogInformation("Line item {@lineItemId} has been removed from {@invoiceId}", lineItemId, invoice.Id);

                        await Task.Delay(TimeSpan.FromSeconds(2), token);
                    }
                }

                invoice.ChangeInvoiceStatus(InvoiceMsgs.Status.Invoiced);
                StoreAggregate(invoice);
                await Task.Delay(TimeSpan.FromSeconds(2), token);

                log.LogInformation("Payment is being made for {@lineItemId:N2} on {@invoiceId}", invoice.Balance, invoice.Id);
                invoice.ApplyPayment(pmtId, invoice.Balance, DateTime.UtcNow);
                StoreAggregate(invoice);

                await Task.Delay(TimeSpan.FromSeconds(2), token);

                if ((_rng.Next(1, 100) % 3) == 0) {
                    invoice.VoidPayment(pmtId, DateTime.UtcNow, DateTime.UtcNow);
                    StoreAggregate(invoice);
                    log.LogInformation("Payment on {@invoiceId} has been removed", invoice.Id);

                    await Task.Delay(TimeSpan.FromSeconds(2), token);

                    invoice.ApplyPayment(Guid.NewGuid(), invoice.Balance, DateTime.UtcNow);
                    StoreAggregate(invoice);
                    log.LogInformation("Payment has been re-applied to {@invoiceId}", invoice.Id);
                }

                invoice.Close(DateTime.Now);
                StoreAggregate(invoice);
            } catch (Exception exc) {
                log.LogCritical(exc, "Invoice generation failed");
                throw;
            }
        }

        void StoreAggregate(RTI.ReactiveDomain.AggregateRoot agg) {
            _repository.Save(agg);
            ((ICorrelatedEventSource)agg).Source = _msg;
        }
    }
}
