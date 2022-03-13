using EvoComputerTechService.Models.Payment;

namespace EvoComputerTechService.Services
{
    public interface IPaymentService
    {
        public InstallmentModel CheckInstallments(string binNumber, decimal price);

        public PaymentResponseModel Pay(PaymentModel model);
    }
}
