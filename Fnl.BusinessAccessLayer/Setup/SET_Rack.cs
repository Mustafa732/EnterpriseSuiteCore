using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;


namespace FnI.BusinessAccessLayer.Setup
{
   
    public partial class SET_Rack
    {
        #region Constant Strings

        const string Query_GetRack_ByLocationID = @"SELECT * FROM Set_Rack WHERE LocationID = @LocationID OR @LocationID IS NULL";
        const string Query_GetRackDDLValue = @"select 
	SET_Rack.RackID,
	SET_Rack.LocationID,
	SET_RackItems.ItemID,
	INV_Item.Description as ItemDescription,
	INV_Item.ItemTypeID as CategoryID,
	INV_SET_Category.CategoryName
from SET_Rack 
inner join SET_RackItems on SET_Rack.RackID = SET_RackItems.RackID 
inner join INV_Item on INV_Item.ItemID = SET_RackItems.ItemID
	--and INV_Item.CompanyID = SET_RackItems.CompanyID
	
inner join INV_SET_Category on INV_SET_Category.CategoryID = INV_Item.ItemTypeID
	--and INV_Item.CompanyID = INV_SET_Category.CompanyID
where SET_Rack.LocationID = @LocationID
";



const string Query_SelectAllForSearch_WithPaging = @" SELECT [RackID], [RackName], SET_Rack.[Description], 
SET_Rack.[LocationID], SET_Rack.[IsActive], SET_Rack.[Status], SET_Rack.[CreatedBy], SET_Rack.[CreateDate], 
SET_Rack.[CreatedByValue], SET_Rack.[UpdatedBy], SET_Location.LocationName , SET_Rack.[UpdateDate], 
SET_Rack.[UpdatedByValue], ROW_NUMBER() Over (Order by RackName) as 'Row' FROM [SET_Rack] 
               LEft JOIN SET_Location ON SET_Location.LocationID=SET_Rack.LocationID  ";


const string Query_SelectAllForSearch_WithoutPaging = @" SELECT [RackID], [RackName], SET_Rack.[Description], 
SET_Rack.[LocationID], SET_Rack.[IsActive], SET_Rack.[Status], SET_Rack.[CreatedBy], 
SET_Rack.[CreateDate], SET_Rack.[CreatedByValue], SET_Rack.[UpdatedBy], 
 SET_Location.LocationName ,SET_Rack.[UpdateDate], SET_Rack.[UpdatedByValue], ROW_NUMBER() Over (Order by RackName) 
as 'Row' FROM [SET_Rack]       LEft JOIN SET_Location ON SET_Location.LocationID=SET_Rack.LocationID  ";

const string Query_GetRackByItemID = @"Select SET_Rack.RackID, SET_Rack.RackName from SET_Rack
Inner JOin Set_RackItems on Set_RackItems.RackID = SET_Rack.RackID
Where SET_Rack.Status=1 and SET_Rack.IsActive = 1 and  Set_RackItems.ItemID = @ItemID";

        #endregion

        #region Class Methods

        public static DataTable GetRack_ByLocationID(Nullable<int> locationID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                if (locationID.HasValue)
                    sqlparams[0] = new SqlParameter("@LocationID", locationID.Value);
                else
                    sqlparams[0] = new SqlParameter("@LocationID", DBNull.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetRack_ByLocationID, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0) 
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex) { SqlHelper.LogException(ex); }

            return dtToReturn;
        }
        public static DataTable GetRackDDL(Nullable<int> locationID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                if (locationID.HasValue)
                    sqlparams[0] = new SqlParameter("@LocationID", locationID.Value);
                else
                    sqlparams[0] = new SqlParameter("@LocationID", DBNull.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetRackDDLValue, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex) { SqlHelper.LogException(ex); }

            return dtToReturn;
        }
        public static bool InsertRackHeaderandDetail(SET_Rack obj, List<Set_RackItems> colReckItems)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertRackHeaderAndDetail");

            try
            {
                obj.RackID = SET_Rack.GetNewRackID();

                if (!obj.Insert(sqltrans))
                    throw new BusinessLogicException("Record is not inserted in Rack Header");

                foreach (Set_RackItems td in colReckItems)
                {
                    td.RackID = obj.RackID;
                    td.Insert(sqltrans);
                }

                result = true;
            }
            catch (BusinessLogicException ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            finally
            {
                if (result)
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "InsertRackHeaderAndDetail");
            }

            return result;
        }

        public static bool UpdateRackHeaderandDetail(SET_Rack obj, List<Set_RackItems> colReckItems)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertRackHeaderAndDetail");
            try
            {
            //    obj.RackID = (int)SET_Rack.GetNewRackID();

                if (!obj.UpdateDataByKey(sqltrans))
                    throw new BusinessLogicException("Record is not inserted in Rack Header");

                Set_RackItems.delete(obj.RackID,sqltrans);

                foreach (Set_RackItems td in colReckItems)
                {
                    td.RackID = obj.RackID;
                    td.Insert(sqltrans);
                 }
               result = true;
            }

            catch (BusinessLogicException ex)
            {
                SqlHelper.LogException(ex);
                throw ex;

            }

            finally
            {
                if (result)
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "InsertRackHeaderAndDetail");
            }

            return result;
        }



        public static DataTable GetAllDataForSearch(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, SqlTransaction objTransaction = null)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

            try
            {
                if (PageIndex != null)
                {
                    sqlparamsList.Add(new SqlParameter("@PageIndex", PageIndex));
                    sqlparamsList.Add(new SqlParameter("@PageSize", PageSize));
                    sqlparamsList.Add(new SqlParameter("@TotalRows", ReturnTotalRows));

                    SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                    sqlparamTotalRows.Direction = ParameterDirection.Output;

                    SqlParameter[] sqlparams = sqlparamsList.ToArray();

                    if (objTransaction != null)
                    {
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_SelectAllForSearch_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAllForSearch_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    }

                    else
                    {
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAllForSearch_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAllForSearch_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    }

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }


                else

                {
                    if (objTransaction != null)
                    {
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAllForSearch_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    }

                    else
                    {
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllForSearch_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    }

                    }
            }

            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetRack_ByItemID(int ItemID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];


                sqlparams[0] = new SqlParameter("@ItemID", ItemID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetRackByItemID, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex) { SqlHelper.LogException(ex); }

            return dtToReturn;
        }
        #endregion


    }
}
