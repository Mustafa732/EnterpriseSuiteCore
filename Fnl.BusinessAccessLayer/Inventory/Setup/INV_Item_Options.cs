using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DAL.DataAccessLayer;
using Microsoft.Data.SqlClient;
using Common.Utils;

namespace FnI.BusinessAccessLayer.Inventory.Setup
{
    public partial class INV_Item_Options
    {
        public static bool InsertItemOptions(List<INV_Item_Options> ColItemOtions, int CompanyID, int PeriodID)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertItemOtions");
            try
            {
                int INV_Item_OptionsId = INV_Item_Options.GetNewINV_Item_OptionsId(sqltrans);

                foreach (INV_Item_Options Itemi in ColItemOtions)
                {
                    Itemi.INV_Item_OptionsId = INV_Item_OptionsId++;
                    Itemi.CompanyID = CompanyID;
                    Itemi.Status = true;
                    Itemi.CreateDate = DateTime.Now;
                    Itemi.Insert(sqltrans);

                }
                result = true;
            }
            catch (BusinessLogicException ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            finally
            {
                if (result)
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "InsertItemOtions");
            }

            return result;

        }
    }
}

