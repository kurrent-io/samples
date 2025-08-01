var outputStreamName = 'invoice_documents';
var runCalculations = function(s) {
    s.total = 0;
    s.paymentsTotal = 0;
    s.balanceDue = 0;

    for (var i = 0; i < s.items.length; i++) {
        s.total += s.items[i].subtotal ?? 0;
    }
    for (var i = 0; i < s.payments.length; i++) {
        s.paymentsTotal += s.payments[i].amount ?? 0;
    }

    s.balanceDue = s.total - s.paymentsTotal;
};

fromCategory('invoice')
    .foreachStream()
    .when({
        $init: function() {
            return {
                id: undefined,
                accountId: undefined,
                accountName: undefined,
                paymentTermsId: undefined,
                date: undefined,
                status: undefined,
                items: [],
                payments: [],
                total: 0,
                paymentsTotal: 0,
                balanceDue: 0
            }
        },
        Generated: function(s, e) {
            s.id = e.data.Id,
            s.accountId = e.data.AccountId;
            s.accountName = e.data.AccountName;
            s.date = e.data.Date;
            s.status = 'Opened';

            emit(outputStreamName, 'document', s, {});
        },
        PaymentTermsApplied: function(s, e) {
            s.paymentTermsId = e.data.PaymentTermsId;

            emit(outputStreamName, 'document', s, {});
        },
        ItemAdded: function(s, e) {
            var item = {
                id: e.data.LineItemId,
                itemId: e.data.ItemId,
                sku: e.data.SKU,
                description: e.data.Description,
                cost: e.data.Cost,
                unitPrice: e.data.UnitPrice,
                quantity: e.data.Quantity,
                subtotal: e.data.UnitPrice * e.data.Quantity
            };
            s.items.push(item);
            
            runCalculations(s);

            emit(outputStreamName, 'document', s, {});
        },
        ItemRemoved: function(s, e) {
            for (var i = s.items.length - 1; i >= 0; i--) {
                if (s.items[i].id === e.data.LineItemId) { s.items.splice(i, 1); }
            }

            runCalculations(s);
            
            emit(outputStreamName, 'document', s, {});
        },
        Issued: function(s, e) {
            s.status = 'Issued';
            s.date = e.data.Date;

            emit(outputStreamName, 'document', s, {});
        },
        ReIssued: function(s, e) {
            s.status = 'Re-issued';
            s.date = e.data.Date;

            emit(outputStreamName, 'document', s, {});
        },
        PaymentApplied: function(s, e) {
            var pmt = {
                id: e.data.PaymentId,
                amount: e.data.Amount ?? 0,
                received: e.data.Received
            };
            s.payments.push(pmt);

            runCalculations(s);

            emit(outputStreamName, 'document', s, {});
        },
        PaymentVoided: function(s, e) {
            for(var i = s.payments.length - 1; i >= 0; i--) {
                if (s.payments[i].id === e.data.PaymentId) { s.payments.splice(i, 1); }
            }

            runCalculations(s);

            emit(outputStreamName, 'document', s, {});
        },
        StatusChanged: function(s, e) {
            s.status = e.data.Status;

            emit(outputStreamName, 'document', s, {});
        },
        Closed: function(s, e) {
            s.status = 'Closed';

            emit(outputStreamName, 'document', s, {});
        }
    });