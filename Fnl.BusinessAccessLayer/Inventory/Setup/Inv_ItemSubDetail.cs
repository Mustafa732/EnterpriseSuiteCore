using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Setup;
using FnI.BusinessAccessLayer.Inventory.Transaction;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class Inv_ItemSubDetail : BaseClass
    {
        #region Class Methods

        public static bool InsertSubItem_Detail(int companyID, int PeriodID, List<Inv_ItemSubDetail> ColAddItemSubDetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("Detail");

            try
            {


                foreach (Inv_ItemSubDetail Detail in ColAddItemSubDetail)
                    Detail.Insert();

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
                    SqlHelper.RollBackTransaction(sqltrans, "Detail");
            }

            return result;
        }


        public static DataTable GetDataByItemID(int companyID, int ItemID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@companyID", companyID);
                sqlparams[1] = new SqlParameter("@ItemID", ItemID);

                string Query_GetDataByColumn = @"select ROW_NUMBER() over (order by sd.SubDetailID) as 'RowUniqueID',sd.SubDetailID,(select SubProductName from Fabrics_SET_SubProduct where SubProductID=sd.SubProductID) as [SubProductName],
sd.UnitWeight as [Weight],(select UOMName from SET_UnitofMeasure where UOMID=sd.PrimaryUOMCode) as UOMName,
(select KnittingMethod from SET_KnittingMethod where KnittingMethodID=sd.KnittingMethod) as [KnittingMethod],
(select [Length] from SET_Length where LengthID=sd.Length) as [Length],(select Width from SET_Width where WidthID=sd.Width) as Width,
(select Embellishment from SET_Embellishment where EmbellishmentID=sd.Embellishment) as Embellishment,
(select CompositionName from SET_Composition where CompositionID=sd.Composition) as CompositionName,(select LayerName from SET_Layer where LayerID=sd.Layer) as LayerName,
(select ColorName from SET_Color where ColorID=sd.ColorID) as [ColorName],(select StyleName from Fabrics_SET_Style where StyleID=sd.StyleID) as [Style],sd.Quantity,sd.Fringe as [IsActive],
-1 as SerialNo,SubProductID,[PrimaryUOMCode] As UOMID,KnittingMethod as KnittingMethodID,Length as LengthID,Width as WidthID,Embellishment as EmbellishmentID,Composition as CompositionID,Layer as LayerID,ColorID,StyleID,sd.Fringe ,'' as Action

 
from Inv_ItemSubDetail as sd

 where CompanyID=@companyID and ItemID=@ItemID";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDataByColumn), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        #endregion
    }
}
