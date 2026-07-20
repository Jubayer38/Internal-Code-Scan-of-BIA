using BIA.BLL.BLLServices;
using BIA.Entity.Collections;
using BIA.Entity.CommonEntity;
using BIA.Entity.DB_Model;
using BIA.Entity.ResponseEntity;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BIA.Controllers;

[Route("api/Dynamic")]
[ApiController]
public class DynamicController(BLLDynamic bLLDynamic, BLLLog bllLog) : ControllerBase
{
    private readonly BLLDynamic _bLLDynamic = bLLDynamic;
    private readonly BLLLog _bllLog = bllLog;

    [HttpPost]
    [Route("GetHelpData")]
    public async Task<IActionResult> GetHelpData(CancellationToken cancellationToken)
    {
        HelpButtonRespModel respModel = new HelpButtonRespModel();
        try
        {
            List<UserType> userTypeList = await _bLLDynamic.GetUserTypeDropdownValu();
            List<ContentType> contentList = await _bLLDynamic.GetContentTypeDropdownValue();
            List<ContentUrl> contentUrls = await _bLLDynamic.GetContentURL();

            // Optimize list filtering by creating Lookups (O(1) search complexity)
            var contentLookup = contentList.ToLookup(c => c.UserTypeId);
            var urlLookup = contentUrls.ToLookup(u => u.userTypeId);

            foreach (UserType item in userTypeList)
            {
                item.contentTypes = contentLookup[item.UserTypeId];

                foreach (ContentType item2 in item.contentTypes)
                {
                    item2.contentUrl = urlLookup[item2.UserTypeId];
                }
            }
            respModel.data = userTypeList;
            respModel.isError = false;
            respModel.message = MessageCollection.Success;
        }
        catch (Exception ex)
        {
            ErrorDescription error;

            try
            {
                error = await _bllLog.ManageException(ex, ex.HResult, "BIA");

                respModel.data = new List<UserType>();
                respModel.isError = true;
                respModel.message = string.IsNullOrEmpty(error.error_custom_msg) ? error.error_description : error.error_custom_msg;
            }
            catch (Exception)
            {
                respModel.isError = true;
                respModel.message = ex.Message;
            }
        }
        return Ok(respModel);
    }
}
