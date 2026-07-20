using BIA.DAL.Repositories;
using BIA.Entity.RequestEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.BLL.BLLServices
{
    public class BLLFTRRestriction
    {
        private readonly DALBiometricRepo _dataManager;

        public BLLFTRRestriction(DALBiometricRepo dataManager)
        {
            _dataManager = dataManager;
        }
        public async Task<string> GetRetailerItopUpNumber(string userName)
        {
            try
            {
                return await _dataManager.GetRetailerItopUpNumber(userName);
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task FTR_UPdateData(FTRDBUpdateModel model)
        {
            await _dataManager.FTR_UpdateData(model);
        }

        public async Task LUS_FTR_UpdateData(FTRDBUpdateModel model)
        {
            await _dataManager.LUS_FTR_UpdateData(model);
        }
    }
}
