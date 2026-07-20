using BIA.BLL.BLLServices;
using BIA.Entity.Collections;
using BIA.Entity.CommonEntity;
using BIA.Entity.DB_Model;
using BIA.Entity.ENUM;
using BIA.Entity.RequestEntity;
using BIA.Entity.Utility;
using Microsoft.AspNetCore.Mvc;

namespace BIA.Common
{
    public class GeoFencingValidation
    {
        private readonly BLLGeofencing _bLLGeofencing;
        private readonly BLLLog _bllLog;

        public GeoFencingValidation(BLLGeofencing bLLGeofencing, BLLLog bllLog)
        {
            _bLLGeofencing = bLLGeofencing;
            _bllLog = bllLog;
        }
        public async Task<RACommonResponseRevamp> GeoFencingBPUser(RAOrderRequestV2 model)
        {
            GeoFencing geoFencing = new GeoFencing();
            GeofenceReqModel geofenceReqModel = new GeofenceReqModel();
            BIAToDBSSLog log = new BIAToDBSSLog();
            BL_Json bL_Json = new BL_Json();
            RACommonResponseRevamp response = new RACommonResponseRevamp();

            response.isError = false;

            try
            {
                string retailerCode = string.Empty;
                double allowedDistance = SettingsValues.GetallowedDistanceForGeo();
                string BPDeviceLatLonNotFound = SettingsValues.GetBP_DeviceLatlonNot_Found();
                string RetLatLonNotFound = SettingsValues.GetRetLatLonNotFound();
                string CrossesTheArea = SettingsValues.GetCrossesTheArea();

                if (model.latitude == 0 || model.longitude == 0)
                {
                    response.isError = true;
                    response.message = BPDeviceLatLonNotFound;
                    return response;
                }

                if (model.retailer_id != null && model.retailer_id.Substring(0, 1) != "R")
                {
                    retailerCode = "R" + model.retailer_id;
                }

                geofenceReqModel = await _bLLGeofencing.GetLoggedinRetLatLon(retailerCode);

                if (geofenceReqModel != null && geofenceReqModel.retilerLat != 0 && geofenceReqModel.retilerLon != 0)
                {
                    double latitude = Convert.ToDouble(model.latitude);
                    double longitude = Convert.ToDouble(model.longitude);

                    double distance = geoFencing.CalculateDistance(geofenceReqModel.retilerLat, geofenceReqModel.retilerLon, latitude, longitude);

                    if (distance > allowedDistance)
                    {
                        response.isError = true;
                        response.message = CrossesTheArea + " " + distance + " Meter)";
                    }
                    else
                    {
                        response.isError = false;
                        response.message = "BP is in range area!";

                    }
                }
                else
                {
                    response.isError = true;
                    response.message = RetLatLonNotFound;
                }

                return response;

            }
            catch (Exception ex)
            {
                response.isError = true;
                response.message = ex.Message;
                throw new Exception(ex.Message);
            }
            finally
            {
                log.purpose_number = model.purpose_number;
                log.msisdn = _bllLog.FormatMSISDN(model.msisdn);
                log.req_time = DateTime.Now;
                log.res_time = DateTime.Now;
                log.res_blob = bL_Json.GetGenericJsonData(response);
                log.method_name = "GeoFencingBPUser";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BI);
                log.user_id = model.retailer_id;

                await _bllLog.RAToDBSSLog(log);
            }
        }
        public async Task<RACommonResponseRevamp> GeoFencingBPUser(HomeWifiOrderRequest model)
        {
            GeoFencing geoFencing = new GeoFencing();
            GeofenceReqModel geofenceReqModel = new GeofenceReqModel();
            BIAToDBSSLog log = new BIAToDBSSLog();
            BL_Json bL_Json = new BL_Json();
            RACommonResponseRevamp response = new RACommonResponseRevamp();

            response.isError = false;

            try
            {
                string retailerCode = string.Empty;
                double allowedDistance = SettingsValues.GetallowedDistanceForGeo();
                string BPDeviceLatLonNotFound = SettingsValues.GetBP_DeviceLatlonNot_Found();
                string RetLatLonNotFound = SettingsValues.GetRetLatLonNotFound();
                string CrossesTheArea = SettingsValues.GetCrossesTheArea();

                if (model.latitude == 0 || model.longitude == 0)
                {
                    response.isError = true;
                    response.message = BPDeviceLatLonNotFound;
                    return response;
                }

                if (model.retailer_id != null && model.retailer_id.Substring(0, 1) != "R")
                {
                    retailerCode = "R" + model.retailer_id;
                }

                geofenceReqModel = await _bLLGeofencing.GetLoggedinRetLatLon(retailerCode);

                if (geofenceReqModel != null && geofenceReqModel.retilerLat != 0 && geofenceReqModel.retilerLon != 0)
                {
                    double latitude = Convert.ToDouble(model.latitude);
                    double longitude = Convert.ToDouble(model.longitude);

                    double distance = geoFencing.CalculateDistance(geofenceReqModel.retilerLat, geofenceReqModel.retilerLon, latitude, longitude);

                    if (distance > allowedDistance)
                    {
                        response.isError = true;
                        response.message = CrossesTheArea + " " + distance + " Meter)";
                    }
                    else
                    {
                        response.isError = false;
                        response.message = "BP is in range area!";

                    }
                }
                else
                {
                    response.isError = true;
                    response.message = RetLatLonNotFound;
                }

                return response;

            }
            catch (Exception ex)
            {
                response.isError = true;
                response.message = ex.Message;
                throw new Exception(ex.Message);
            }
            finally
            {
                log.purpose_number = model.purpose_number;
                log.msisdn = _bllLog.FormatMSISDN(model.msisdn);
                log.req_time = DateTime.Now;
                log.res_time = DateTime.Now;
                log.res_blob = bL_Json.GetGenericJsonData(response);
                log.method_name = "GeoFencingBPUser";
                log.integration_point_from = Convert.ToDecimal(IntegrationPoints.RA);
                log.integration_point_to = Convert.ToDecimal(IntegrationPoints.BI);
                log.user_id = model.retailer_id;

                await _bllLog.RAToDBSSLog(log);
            }
        }
    }
}
