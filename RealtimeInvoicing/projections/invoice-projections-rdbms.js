var sendHeader = function(s) {
    emit('invoice_header', 'record', {
        id: s.id,
        accountId: s.accountId,
        accountName: s.accountName,
        paymentTermsId: s.paymentTermsId,
        date: s.date,
        status: s.status,
        total: s.total ?? 0,
        paymentsTotal: s.paymentsTotal ?? 0,
        balanceDue: s.balanceDue ?? 0
    }, { });
};
var sendLineItem = function(item) {
    emit('invoice_item', 'record', item, { });
};
var sendPaymentItem = function(pmt) {
    emit('invoice_pmt', 'record', pmt, { });
};
var runCalculations = function(s) {
    s.total = 0;
    s.paymentsTotal = 0;
    s.balanceDue = 0;

    for (var i = 0; i < s.items.length; i++) {
        var item = s.items[i];
        if(!item.removed) s.total += item.subtotal;
    }
    for (var i = 0; i < s.payments.length; i++) {
        var item = s.payments[i];
        if(!item.voided) s.paymentsTotal += item.amount;
    }

    s.balanceDue = s.total - s.paymentsTotal;

    sendHeader(s);
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
                status: '',
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

            sendHeader(s);
        },
        PaymentTermsApplied: function(s, e) {
            s.paymentTermsId = e.data.PaymentTermsId;
            
            sendHeader(s);
        },
        ItemAdded: function(s, e) {
            var item = {
                id: e.data.Id,
                lineItemId: e.data.LineItemId,
                itemId: e.data.ItemId,
                sku: e.data.SKU,
                description: e.data.Description,
                cost: e.data.Cost,
                unitPrice: e.data.UnitPrice,
                quantity: e.data.Quantity,
                subtotal: e.data.UnitPrice * e.data.Quantity,
                removed: false
            };
            s.items.push(item);

            sendLineItem(item);
            
            runCalculations(s);
        },
        ItemRemoved: function(s, e) {
            for (var i = 0; i < s.items.length; i++) {
                var item = s.items[i];
                if (item.lineItemId === e.data.LineItemId) { 
                    item.removed = true;
                    sendLineItem(item);
                }
            }

            runCalculations(s);
        },
        Issued: function(s, e) {
            s.status = 'Issued';
            s.date = e.data.Date;

            sendHeader(s);
        },
        ReIssued: function(s, e) {
            s.status = 'Re-issued';
            s.date = e.data.Date;

            sendHeader(s);
        },
        PaymentApplied: function(s, e) {
            var pmt = {
                id: e.data.Id,
                paymentId: e.data.PaymentId,
                amount: e.data.Amount ?? 0,
                received: e.data.Received,
                voided: false
            };
            s.payments.push(pmt);

            sendPaymentItem(pmt);

            runCalculations(s);
        },
        PaymentVoided: function(s, e) {
            for(var i = 0; i < s.payments.length; i++) {
                var pmt = s.payments[i];
                if (pmt.paymentId === e.data.PaymentId) { 
                    pmt.voided = true;
                    sendPaymentItem(pmt);
                }
            }

            runCalculations(s);
        },
        StatusChanged: function(s, e) {
            s.status = e.data.Status;

            sendHeader(s);
        },
        Closed: function(s, e) {
            s.status = 'Closed';

            sendHeader(s);
        }
    });