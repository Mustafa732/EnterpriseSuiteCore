using System;
using System.Data;
using Microsoft.Data.SqlClient;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Security
{
    public class ApplicationLicense
    {
        #region Queries

        const string Query_GetLicenseTableInfo = @" SELECT table_name from master.INFORMATION_SCHEMA.TABLES Where TABLE_NAME = 'clinformation'";

        const string Query_GetSQLDateInformation = @" SELECT createdate from master.dbo.syslogins where sid = 0x010100000000000512000000";

        const string Query_CreateLicenseTable = @" USE [master] 
SET ANSI_NULLS ON 
SET QUOTED_IDENTIFIER ON 
SET ANSI_PADDING ON 

CREATE TABLE [clinformation] 
(
    [SerialNumber] [varchar](50) NOT NULL, 
    [ActivationKey] [varchar](50) NOT NULL, 
    [Status] [varchar](20) NOT NULL, 
    [MachineKey] [varchar](50) NOT NULL, 
    [ValidUpto] [varchar](50) NOT NULL
) ON [PRIMARY] 

SET ANSI_PADDING OFF ";

        const string Query_DoesLicenseTableExist = @" IF EXISTS (SELECT * FROM master.INFORMATION_SCHEMA.tables where table_name = 'clinformation') 
    SELECT TOP 1 * FROM master.dbo.clinformation ";

        const string Query_DeleteOldLicenseInfo = @" DELETE FROM master.dbo.clinformation ";

        const string Query_InsertLicenseInfo = @" INSERT INTO master.dbo.clinformation Values(@SerialNumber, @ActivationKey, @Status, @MachineKey, @ValidUpto) ";

        const string Query_Get_LicenseInfo = @" SELECT * FROM master.dbo.clinformation ";

        #endregion

        #region Methods

        public static bool GetLicenseTableInfo()
        {
            bool result = false;

            try
            {
                object obj = SqlHelper.ExecuteScalar(CommandType.Text, Query_GetLicenseTableInfo);

                if (obj != null && !string.IsNullOrEmpty(obj.ToString()))
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static string GetSQLDateInformation()
        {
            DataTable dt = null;
            string value;

            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetSQLDateInformation).Tables[0];
                value = dt.Rows[0]["createdate"].ToString();
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return value;
        }

        public static bool DoesLicenseTableExist()
        {
            bool result = false;

            try
            {
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_DoesLicenseTableExist);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static bool CreateLicenseTable()
        {
            bool result = false;

            try
            {
                SqlHelper.ExecuteNonQuery(CommandType.Text, Query_CreateLicenseTable);
                result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static bool DeleteOldLicenseInfo()
        {
            bool result = false;

            try
            {
                SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteOldLicenseInfo);
                result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static bool InsertLicenseInfo(string serialNumber, string activationKey, string status, string machineKey, string validUpto)
        {
            bool result = false;

            try
            {
                SqlParameter[] objParameters = new SqlParameter[5];
                objParameters[0] = new SqlParameter("@SerialNumber", serialNumber);
                objParameters[1] = new SqlParameter("@ActivationKey", activationKey);
                objParameters[2] = new SqlParameter("@Status", status);
                objParameters[3] = new SqlParameter("@MachineKey", machineKey);
                objParameters[4] = new SqlParameter("@ValidUpto", validUpto);

                SqlHelper.ExecuteNonQuery(CommandType.Text, Query_InsertLicenseInfo, objParameters);
                result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static DataRow Get_LicenseInfoRecord()
        {
            DataRow result = null;

            try
            {
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_LicenseInfo);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    result = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        #endregion
    }
}
