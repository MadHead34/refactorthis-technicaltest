using System;
using System.Linq;
using RefactorThis.Persistence;

namespace RefactorThis.Domain
{
    public class InvoiceService
    {
        private readonly InvoiceRepository _invoiceRepository;

        public InvoiceService(InvoiceRepository invoiceRepository)
        {
            _invoiceRepository = invoiceRepository;
        }
        public string ProcessPayment(Payment payment)
        {
            var inv = _invoiceRepository.GetInvoice(payment.Reference);

            if (inv == null)
                throw new InvalidOperationException("There is no invoice matching this payment");

            if (inv.Amount == 0)
            {
                if (inv.Payments == null || !inv.Payments.Any())
                    return "no payment needed";

                throw new InvalidOperationException(
                    "The invoice is in an invalid state, it has an amount of 0 and it has payments.");
            }

            var totalPaid = inv.Payments != null ? inv.Payments.Sum(x => x.Amount) : 0;
            var remainingAmount = inv.Amount - totalPaid;

            if (totalPaid != 0 && remainingAmount == 0)
                return "invoice was already fully paid";

            if (totalPaid != 0 && payment.Amount > remainingAmount)
                return "the payment is greater than the partial amount remaining";

            if (totalPaid == 0 && payment.Amount > inv.Amount)
                return "the payment is greater than the invoice amount";

            bool isFinalPayment = payment.Amount == remainingAmount;

            inv.AmountPaid += payment.Amount;

            if (inv.Payments == null)
            {
                inv.Payments = new System.Collections.Generic.List<Payment>();
            }

            inv.Payments.Add(payment);

            if (inv.Type == InvoiceType.Commercial)
            {
                inv.TaxAmount += payment.Amount * 0.14m;
            }

            inv.Save();
            
            if (isFinalPayment)
                return totalPaid == 0
                    ? "invoice is now fully paid"
                    : "final partial payment received, invoice is now fully paid";

            return totalPaid == 0
                ? "invoice is now partially paid"
                : "another partial payment received, still not fully paid";
        }
    }
}