using BIA.DAL.Repositories;
using System.Threading.Tasks;

namespace BIA.BLL.BLLServices
{
    public class BLLFirstRecharge
    {
        private readonly DALBiometricRepo _dataManager;

        public BLLFirstRecharge(DALBiometricRepo dataManager)
        {
            _dataManager = dataManager;
        }
        public async Task<long> UpdateOrderFirstRechargeStatus(string requestId)
        {
            long response = 0;
            try
            {
                long bi_token_number = String.IsNullOrEmpty(requestId) ? 0 : Convert.ToInt64(requestId.Trim());
                response = await _dataManager.UpdateOrderFirstRechargeStatus(bi_token_number);
            }
            catch (Exception)
            {
                throw;
            }
            return response;
        }
    }
}
