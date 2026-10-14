using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Setup
{
   
    public partial class SET_FabricationProcess
    {
        const string Query_Validate = @"SELECT FabricationProcessName FROM SET_FabricationProcess where FabricationProcessName =  @FabricationProcessName And Status=1 ";
        const string Query_ValidateEditFabricationProcess = @"Select * from SET_FabricationProcess where FabricationProcessID <>@FabricationProcessID AND FabricationProcessName = @FabricationProcessName and Status=1";

        const string Query_GetRateBy_Customer = @"Select distinct SET_FabricationProcess.FabricationProcessID ,SET_FabricationProcess.FabricationProcessName ,Fabric_Rates.Rate from SET_FabricationProcess
inner join Fabric_Rates on Fabric_Rates.ProcessID =SET_FabricationProcess.FabricationProcessID 
AND Fabric_Rates.EffectiveDate = 
            (
                SELECT MAX(EffectiveDate) FROM Fabric_Rates FR
                WHERE ProcessID = Fabric_Rates.ProcessID
                and CustomerID = Fabric_Rates.CustomerID
           )
where Fabric_Rates.Status =1 
--And Fabric_Rates.IsCurrentRate =1
And Fabric_Rates.CustomerID=@CustomerID
Order By  SET_FabricationProcess.FabricationProcessID ";
        public static DataTable ValidateCode(string Code, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                sqlparams[0] = new SqlParameter("@FabricationProcessName", Code);
                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_Validate, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_Validate, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Validate, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable ValidateEditFabricationProcess(int FabricationProcessId, string FabricationProcessName)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@FabricationProcessName", FabricationProcessName);
                sqlparams[1] = new SqlParameter("@FabricationProcessID", FabricationProcessId);
                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_ValidateEditFabricationProcess, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetProcessRateByCustomer(int CustomerID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                sqlparams[0] = new SqlParameter("@CustomerID", CustomerID);
                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetRateBy_Customer, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
        public static DataTable GetAllPrcessWithDyeRate()
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                sqlparams[0] = new SqlParameter("@CustomerID", 0);
                DataSet ds = null;


                const string Query_GetAllPrcessWithDyeRate = @"
select -99 as FabricationProcessID,'Dye Rate' as FabricationProcessName 
union all
select FabricationProcessID,FabricationProcessName from SET_FabricationProcess where Status=1
";
                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllPrcessWithDyeRate, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
       


    }


}

