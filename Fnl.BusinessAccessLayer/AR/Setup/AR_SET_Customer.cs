using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.AR.Setup
{
    public partial class AR_SET_Customer : BaseClass
    {
        #region Constant Strings

        const string Query_InsertSETCustomerPF = @" INSERT INTO {0}.dbo.[AR_SET_Customer] ([CompanyID], [PeriodID], [CustomerID], [CustomerName], [AddressLine1], [CityID], [ZipCode], [PhoneNumber], [ExtensionNumber], [ContactNumber], [FaxNumber], [EmailAddress], [CNICNumber], [NTNNumber], [STRNNumber], [ChartofAccountID], [CustomerAccountCCID], [ContactPerson], [Collector], [CollectorContactNumber], [CreditAnalyst], [CreditChecking], [NextCreditReviewDate], [Tolerance], [DiscountTerms], [DunningLetters], [DunningLetterSetID], [InterestCharges], [PrefFunctionalCurrencyID], [SendStatements], [CreditBalanceStatements], [CreditHold], [ProfileClassID], [CreditRating], [RiskID], [StandardTermID], [OverrideTerms], [InterestPeriodDays], [PaymentGraceDays], [DiscountGraceDays], [StatementCycleID], [PercentCollectable], [AutoRecInclDisputedFlag], [TaxPrintingOptionID], [ChargeOnFinanceChargeFlag], [GroupingRuleID], [ClearingDays], [ConsInvFlag], [ConsInvTypeID], [Status], [CreatedBy], [CreateDate], [CreatedByValue], [IsActive], [FatherName], [ServiceItemTaxExempted], [ServiceItemTaxRate], [ServiceItemTaxCOA], [SuppliesTaxExempted], [SuppliesTaxRate], [SuppliesTaxCOA], [ExemptedAmount], [ContactPersonName], [ContactPersonAddress], [TransporterTaxExempted], [TransporterTaxRate], [TransporterExceedLimit], [TransporterTaxCOA], [AdditionalDeductionTaxExempted], [AdditionalDeductionTaxRate], [AdditionalDeductionCOA], [FK_BankID], [FK_BankBranchID], [AccountNumber], [IsStopPayment], [Districtid], [Provinceid], [FixedDiscount], [PaymentType], [FK_BankAccountID], [Remarks], [AddressLine2], [AddressLine3], [AddressLine4], [IsRegistered], [AreaID], [ROUTEPLANID], [OSCode], [CountryID], [IsLoyalFlag], [SecurityDeposite], [SpecialDeal], [TaxCardTypeID], [CustomerCategoryID], [Filer], [TaxationStatusID], [TaxationStatus],[EmployeeCode], [TaxStatementCodeID], [TaxStatementExemptionCodeID], [BrokerID], [IsExporter], [ISBulkSupplies], [PRANo], [CreditLimit])
		VALUES(@CompanyID, @PeriodID, @CustomerID, @CustomerName, @AddressLine1, @CityID, @ZipCode, @PhoneNumber, @ExtensionNumber, @ContactNumber, @FaxNumber, @EmailAddress, @CNICNumber, @NTNNumber, @STRNNumber, @ChartofAccountID, @CustomerAccountCCID, @ContactPerson, @Collector, @CollectorContactNumber, @CreditAnalyst, @CreditChecking, @NextCreditReviewDate, @Tolerance, @DiscountTerms, @DunningLetters, @DunningLetterSetID, @InterestCharges, @PrefFunctionalCurrencyID, @SendStatements, @CreditBalanceStatements, @CreditHold, @ProfileClassID, @CreditRating, @RiskID, @StandardTermID, @OverrideTerms, @InterestPeriodDays, @PaymentGraceDays, @DiscountGraceDays, @StatementCycleID, @PercentCollectable, @AutoRecInclDisputedFlag, @TaxPrintingOptionID, @ChargeOnFinanceChargeFlag, @GroupingRuleID, @ClearingDays, @ConsInvFlag, @ConsInvTypeID, @Status, @CreatedBy, @CreateDate, @CreatedByValue, @IsActive, @FatherName, @ServiceItemTaxExempted, @ServiceItemTaxRate, @ServiceItemTaxCOA, @SuppliesTaxExempted, @SuppliesTaxRate, @SuppliesTaxCOA, @ExemptedAmount, @ContactPersonName, @ContactPersonAddress, @TransporterTaxExempted, @TransporterTaxRate, @TransporterExceedLimit, @TransporterTaxCOA, @AdditionalDeductionTaxExempted, @AdditionalDeductionTaxRate, @AdditionalDeductionCOA, @FK_BankID, @FK_BankBranchID, @AccountNumber, @IsStopPayment, @Districtid, @Provinceid, @FixedDiscount, @PaymentType, @FK_BankAccountID, @Remarks, @AddressLine2, @AddressLine3, @AddressLine4, @IsRegistered, @AreaID, @ROUTEPLANID, @OSCode, @CountryID, @IsLoyalFlag, @SecurityDeposite, @SpecialDeal, @TaxCardTypeID, @CustomerCategoryID, @Filer, @TaxationStatusID, @TaxationStatus, @EmployeeCode, @TaxStatementCodeID, @TaxStatementExemptionCodeID, @BrokerID, @IsExporter, @ISBulkSupplies, @PRANo, @CreditLimit) ";

        #endregion

        #region Constructor

        public AR_SET_Customer(int businessPartnerID)
        {
            LoadBP(businessPartnerID);
        }


        #endregion

        #region Class Methods



        public int GetNewCustomerIDWithMinMaxLimit(SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                //sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                //sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                string Query_Get_MaxId_WithMinMaxLimitNew = @" SELECT IsNull(max(CustomerID), 0) + 1 as CustomerID FROM AR_SET_Customer 
WHERE  CustomerID
 Between IsNull((Select FieldValue1 From Set_Parameter Where FieldName = 'BusinessPartnerIDsRange'), 1) 
    AND IsNull((Select FieldValue2 From Set_Parameter Where FieldName = 'BusinessPartnerIDsRange'), 5000000) ";

                if (objTransaction != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Get_MaxId_WithMinMaxLimitNew, sqlparams));
                else if (objConnection != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_Get_MaxId_WithMinMaxLimitNew, sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_MaxId_WithMinMaxLimitNew, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static int GetNewCustomerID(SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;
            const string Query_Get_MaxIdOnlyonCustomerID = @" SELECT IsNull(max(CustomerID), 0) + 1 as CustomerID FROM AR_SET_Customer ";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[0];
                //sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                //sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (objTransaction != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Get_MaxIdOnlyonCustomerID, sqlparams));
                else if (objConnection != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_Get_MaxIdOnlyonCustomerID, sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_MaxIdOnlyonCustomerID, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }


        public bool InsertSETCustomerPF(string DatabaseName, SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                StringBuilder sb = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[105];
                sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[2] = new SqlParameter("@CustomerID", f_CustomerID);
                sqlparams[3] = new SqlParameter("@CustomerName", f_CustomerName);

                if (!string.IsNullOrEmpty(f_AddressLine1))
                    sqlparams[4] = new SqlParameter("@AddressLine1", f_AddressLine1);
                else
                    sqlparams[4] = new SqlParameter("@AddressLine1", DBNull.Value);

                if (f_CityID.HasValue)
                    sqlparams[5] = new SqlParameter("@CityID", f_CityID.Value);
                else
                    sqlparams[5] = new SqlParameter("@CityID", DBNull.Value);

                if (!string.IsNullOrEmpty(f_ZipCode))
                    sqlparams[6] = new SqlParameter("@ZipCode", f_ZipCode);
                else
                    sqlparams[6] = new SqlParameter("@ZipCode", DBNull.Value);

                if (!string.IsNullOrEmpty(f_PhoneNumber))
                    sqlparams[7] = new SqlParameter("@PhoneNumber", f_PhoneNumber);
                else
                    sqlparams[7] = new SqlParameter("@PhoneNumber", DBNull.Value);

                if (!string.IsNullOrEmpty(f_ExtensionNumber))
                    sqlparams[8] = new SqlParameter("@ExtensionNumber", f_ExtensionNumber);
                else
                    sqlparams[8] = new SqlParameter("@ExtensionNumber", DBNull.Value);

                if (!string.IsNullOrEmpty(f_ContactNumber))
                    sqlparams[9] = new SqlParameter("@ContactNumber", f_ContactNumber);
                else
                    sqlparams[9] = new SqlParameter("@ContactNumber", DBNull.Value);

                if (!string.IsNullOrEmpty(f_FaxNumber))
                    sqlparams[10] = new SqlParameter("@FaxNumber", f_FaxNumber);
                else
                    sqlparams[10] = new SqlParameter("@FaxNumber", DBNull.Value);

                if (!string.IsNullOrEmpty(f_EmailAddress))
                    sqlparams[11] = new SqlParameter("@EmailAddress", f_EmailAddress);
                else
                    sqlparams[11] = new SqlParameter("@EmailAddress", DBNull.Value);

                if (!string.IsNullOrEmpty(f_CNICNumber))
                    sqlparams[12] = new SqlParameter("@CNICNumber", f_CNICNumber);
                else
                    sqlparams[12] = new SqlParameter("@CNICNumber", DBNull.Value);

                if (!string.IsNullOrEmpty(f_NTNNumber))
                    sqlparams[13] = new SqlParameter("@NTNNumber", f_NTNNumber);
                else
                    sqlparams[13] = new SqlParameter("@NTNNumber", DBNull.Value);

                if (!string.IsNullOrEmpty(f_STRNNumber))
                    sqlparams[14] = new SqlParameter("@STRNNumber", f_STRNNumber);
                else
                    sqlparams[14] = new SqlParameter("@STRNNumber", DBNull.Value);

                sqlparams[15] = new SqlParameter("@ChartofAccountID", f_ChartofAccountID);

                if (!string.IsNullOrEmpty(f_CustomerAccountCCID))
                    sqlparams[16] = new SqlParameter("@CustomerAccountCCID", f_CustomerAccountCCID);
                else
                    sqlparams[16] = new SqlParameter("@CustomerAccountCCID", DBNull.Value);

                if (!string.IsNullOrEmpty(f_ContactPerson))
                    sqlparams[17] = new SqlParameter("@ContactPerson", f_ContactPerson);
                else
                    sqlparams[17] = new SqlParameter("@ContactPerson", DBNull.Value);

                sqlparams[18] = new SqlParameter("@Collector", f_Collector);

                if (!string.IsNullOrEmpty(f_CollectorContactNumber))
                    sqlparams[19] = new SqlParameter("@CollectorContactNumber", f_CollectorContactNumber);
                else
                    sqlparams[19] = new SqlParameter("@CollectorContactNumber", DBNull.Value);

                if (!string.IsNullOrEmpty(f_CreditAnalyst))
                    sqlparams[20] = new SqlParameter("@CreditAnalyst", f_CreditAnalyst);
                else
                    sqlparams[20] = new SqlParameter("@CreditAnalyst", DBNull.Value);

                sqlparams[21] = new SqlParameter("@CreditChecking", f_CreditChecking);

                if (f_NextCreditReviewDate.HasValue)
                    sqlparams[22] = new SqlParameter("@NextCreditReviewDate", f_NextCreditReviewDate.Value);
                else
                    sqlparams[22] = new SqlParameter("@NextCreditReviewDate", DBNull.Value);

                sqlparams[23] = new SqlParameter("@Tolerance", f_Tolerance);
                sqlparams[24] = new SqlParameter("@DiscountTerms", f_DiscountTerms);
                sqlparams[25] = new SqlParameter("@DunningLetters", f_DunningLetters);

                if (f_DunningLetterSetID.HasValue)
                    sqlparams[26] = new SqlParameter("@DunningLetterSetID", f_DunningLetterSetID.Value);
                else
                    sqlparams[26] = new SqlParameter("@DunningLetterSetID", DBNull.Value);

                sqlparams[27] = new SqlParameter("@InterestCharges", f_InterestCharges);

                if (f_PrefFunctionalCurrencyID.HasValue)
                    sqlparams[28] = new SqlParameter("@PrefFunctionalCurrencyID", f_PrefFunctionalCurrencyID.Value);
                else
                    sqlparams[28] = new SqlParameter("@PrefFunctionalCurrencyID", DBNull.Value);

                sqlparams[29] = new SqlParameter("@SendStatements", f_SendStatements);
                sqlparams[30] = new SqlParameter("@CreditBalanceStatements", f_CreditBalanceStatements);
                sqlparams[31] = new SqlParameter("@CreditHold", f_CreditHold);

                if (f_ProfileClassID.HasValue)
                    sqlparams[32] = new SqlParameter("@ProfileClassID", f_ProfileClassID.Value);
                else
                    sqlparams[32] = new SqlParameter("@ProfileClassID", DBNull.Value);

                if (!string.IsNullOrEmpty(f_CreditRating))
                    sqlparams[33] = new SqlParameter("@CreditRating", f_CreditRating);
                else
                    sqlparams[33] = new SqlParameter("@CreditRating", DBNull.Value);

                if (f_RiskID.HasValue)
                    sqlparams[34] = new SqlParameter("@RiskID", f_RiskID.Value);
                else
                    sqlparams[34] = new SqlParameter("@RiskID", DBNull.Value);

                if (f_StandardTermID.HasValue)
                    sqlparams[35] = new SqlParameter("@StandardTermID", f_StandardTermID.Value);
                else
                    sqlparams[35] = new SqlParameter("@StandardTermID", DBNull.Value);

                sqlparams[36] = new SqlParameter("@OverrideTerms", f_OverrideTerms);

                if (f_InterestPeriodDays.HasValue)
                    sqlparams[37] = new SqlParameter("@InterestPeriodDays", f_InterestPeriodDays.Value);
                else
                    sqlparams[37] = new SqlParameter("@InterestPeriodDays", DBNull.Value);

                if (f_PaymentGraceDays.HasValue)
                    sqlparams[38] = new SqlParameter("@PaymentGraceDays", f_PaymentGraceDays.Value);
                else
                    sqlparams[38] = new SqlParameter("@PaymentGraceDays", DBNull.Value);

                if (f_DiscountGraceDays.HasValue)
                    sqlparams[39] = new SqlParameter("@DiscountGraceDays", f_DiscountGraceDays.Value);
                else
                    sqlparams[39] = new SqlParameter("@DiscountGraceDays", DBNull.Value);

                if (f_StatementCycleID.HasValue)
                    sqlparams[40] = new SqlParameter("@StatementCycleID", f_StatementCycleID.Value);
                else
                    sqlparams[40] = new SqlParameter("@StatementCycleID", DBNull.Value);

                if (f_PercentCollectable.HasValue)
                    sqlparams[41] = new SqlParameter("@PercentCollectable", f_PercentCollectable.Value);
                else
                    sqlparams[41] = new SqlParameter("@PercentCollectable", DBNull.Value);

                sqlparams[42] = new SqlParameter("@AutoRecInclDisputedFlag", f_AutoRecInclDisputedFlag);

                if (f_TaxPrintingOptionID.HasValue)
                    sqlparams[43] = new SqlParameter("@TaxPrintingOptionID", f_TaxPrintingOptionID.Value);
                else
                    sqlparams[43] = new SqlParameter("@TaxPrintingOptionID", DBNull.Value);

                sqlparams[44] = new SqlParameter("@ChargeOnFinanceChargeFlag", f_ChargeOnFinanceChargeFlag);

                if (f_GroupingRuleID.HasValue)
                    sqlparams[45] = new SqlParameter("@GroupingRuleID", f_GroupingRuleID.Value);
                else
                    sqlparams[45] = new SqlParameter("@GroupingRuleID", DBNull.Value);

                if (f_ClearingDays.HasValue)
                    sqlparams[46] = new SqlParameter("@ClearingDays", f_ClearingDays.Value);
                else
                    sqlparams[46] = new SqlParameter("@ClearingDays", DBNull.Value);

                sqlparams[47] = new SqlParameter("@ConsInvFlag", f_ConsInvFlag);

                if (f_ConsInvTypeID.HasValue)
                    sqlparams[48] = new SqlParameter("@ConsInvTypeID", f_ConsInvTypeID.Value);
                else
                    sqlparams[48] = new SqlParameter("@ConsInvTypeID", DBNull.Value);

                sqlparams[49] = new SqlParameter("@Status", f_Status);
                sqlparams[50] = new SqlParameter("@CreatedBy", f_CreatedBy);
                sqlparams[51] = new SqlParameter("@CreateDate", f_CreateDate);
                sqlparams[52] = new SqlParameter("@CreatedByValue", f_CreatedByValue);
                sqlparams[53] = new SqlParameter("@IsActive", f_IsActive);

                if (!string.IsNullOrEmpty(f_FatherName))
                    sqlparams[54] = new SqlParameter("@FatherName", f_FatherName);
                else
                    sqlparams[54] = new SqlParameter("@FatherName", DBNull.Value);

                sqlparams[55] = new SqlParameter("@ServiceItemTaxExempted", f_ServiceItemTaxExempted);

                if (f_ServiceItemTaxRate.HasValue)
                    sqlparams[56] = new SqlParameter("@ServiceItemTaxRate", f_ServiceItemTaxRate.Value);
                else
                    sqlparams[56] = new SqlParameter("@ServiceItemTaxRate", DBNull.Value);

                if (!string.IsNullOrEmpty(f_ServiceItemTaxCOA))
                    sqlparams[57] = new SqlParameter("@ServiceItemTaxCOA", f_ServiceItemTaxCOA);
                else
                    sqlparams[57] = new SqlParameter("@ServiceItemTaxCOA", DBNull.Value);

                sqlparams[58] = new SqlParameter("@SuppliesTaxExempted", f_SuppliesTaxExempted);

                if (f_SuppliesTaxRate.HasValue)
                    sqlparams[59] = new SqlParameter("@SuppliesTaxRate", f_SuppliesTaxRate.Value);
                else
                    sqlparams[59] = new SqlParameter("@SuppliesTaxRate", DBNull.Value);

                if (!string.IsNullOrEmpty(f_SuppliesTaxCOA))
                    sqlparams[60] = new SqlParameter("@SuppliesTaxCOA", f_SuppliesTaxCOA);
                else
                    sqlparams[60] = new SqlParameter("@SuppliesTaxCOA", DBNull.Value);

                sqlparams[61] = new SqlParameter("@ExemptedAmount", f_ExemptedAmount);

                if (!string.IsNullOrEmpty(f_ContactPersonName))
                    sqlparams[62] = new SqlParameter("@ContactPersonName", f_ContactPersonName);
                else
                    sqlparams[62] = new SqlParameter("@ContactPersonName", DBNull.Value);

                if (!string.IsNullOrEmpty(f_ContactPersonAddress))
                    sqlparams[63] = new SqlParameter("@ContactPersonAddress", f_ContactPersonAddress);
                else
                    sqlparams[63] = new SqlParameter("@ContactPersonAddress", DBNull.Value);

                if (f_TransporterTaxExempted.HasValue)
                    sqlparams[64] = new SqlParameter("@TransporterTaxExempted", f_TransporterTaxExempted.Value);
                else
                    sqlparams[64] = new SqlParameter("@TransporterTaxExempted", DBNull.Value);

                if (f_TransporterTaxRate.HasValue)
                    sqlparams[65] = new SqlParameter("@TransporterTaxRate", f_TransporterTaxRate.Value);
                else
                    sqlparams[65] = new SqlParameter("@TransporterTaxRate", DBNull.Value);

                if (f_TransporterExceedLimit.HasValue)
                    sqlparams[66] = new SqlParameter("@TransporterExceedLimit", f_TransporterExceedLimit.Value);
                else
                    sqlparams[66] = new SqlParameter("@TransporterExceedLimit", DBNull.Value);

                if (!string.IsNullOrEmpty(f_TransporterTaxCOA))
                    sqlparams[67] = new SqlParameter("@TransporterTaxCOA", f_TransporterTaxCOA);
                else
                    sqlparams[67] = new SqlParameter("@TransporterTaxCOA", DBNull.Value);

                if (f_AdditionalDeductionTaxExempted.HasValue)
                    sqlparams[68] = new SqlParameter("@AdditionalDeductionTaxExempted", f_AdditionalDeductionTaxExempted.Value);
                else
                    sqlparams[68] = new SqlParameter("@AdditionalDeductionTaxExempted", DBNull.Value);

                if (f_AdditionalDeductionTaxRate.HasValue)
                    sqlparams[69] = new SqlParameter("@AdditionalDeductionTaxRate", f_AdditionalDeductionTaxRate.Value);
                else
                    sqlparams[69] = new SqlParameter("@AdditionalDeductionTaxRate", DBNull.Value);

                if (!string.IsNullOrEmpty(f_AdditionalDeductionCOA))
                    sqlparams[70] = new SqlParameter("@AdditionalDeductionCOA", f_AdditionalDeductionCOA);
                else
                    sqlparams[70] = new SqlParameter("@AdditionalDeductionCOA", DBNull.Value);

                if (f_FK_BankID.HasValue)
                    sqlparams[71] = new SqlParameter("@FK_BankID", f_FK_BankID.Value);
                else
                    sqlparams[71] = new SqlParameter("@FK_BankID", DBNull.Value);

                if (f_FK_BankBranchID.HasValue)
                    sqlparams[72] = new SqlParameter("@FK_BankBranchID", f_FK_BankBranchID.Value);
                else
                    sqlparams[72] = new SqlParameter("@FK_BankBranchID", DBNull.Value);

                if (!string.IsNullOrEmpty(f_AccountNumber))
                    sqlparams[73] = new SqlParameter("@AccountNumber", f_AccountNumber);
                else
                    sqlparams[73] = new SqlParameter("@AccountNumber", DBNull.Value);

                if (f_IsStopPayment.HasValue)
                    sqlparams[74] = new SqlParameter("@IsStopPayment", f_IsStopPayment.Value);
                else
                    sqlparams[74] = new SqlParameter("@IsStopPayment", DBNull.Value);

                if (f_Districtid.HasValue)
                    sqlparams[75] = new SqlParameter("@Districtid", f_Districtid.Value);
                else
                    sqlparams[75] = new SqlParameter("@Districtid", DBNull.Value);

                if (f_Provinceid.HasValue)
                    sqlparams[76] = new SqlParameter("@Provinceid", f_Provinceid.Value);
                else
                    sqlparams[76] = new SqlParameter("@Provinceid", DBNull.Value);

                if (f_FixedDiscount.HasValue)
                    sqlparams[77] = new SqlParameter("@FixedDiscount", f_FixedDiscount.Value);
                else
                    sqlparams[77] = new SqlParameter("@FixedDiscount", DBNull.Value);

                if (!string.IsNullOrEmpty(f_PaymentType))
                    sqlparams[78] = new SqlParameter("@PaymentType", f_PaymentType);
                else
                    sqlparams[78] = new SqlParameter("@PaymentType", DBNull.Value);

                if (f_FK_BankAccountID.HasValue)
                    sqlparams[79] = new SqlParameter("@FK_BankAccountID", f_FK_BankAccountID.Value);
                else
                    sqlparams[79] = new SqlParameter("@FK_BankAccountID", DBNull.Value);

                if (!string.IsNullOrEmpty(f_Remarks))
                    sqlparams[80] = new SqlParameter("@Remarks", f_Remarks);
                else
                    sqlparams[80] = new SqlParameter("@Remarks", DBNull.Value);

                if (!string.IsNullOrEmpty(f_AddressLine2))
                    sqlparams[81] = new SqlParameter("@AddressLine2", f_AddressLine2);
                else
                    sqlparams[81] = new SqlParameter("@AddressLine2", DBNull.Value);

                if (!string.IsNullOrEmpty(f_AddressLine3))
                    sqlparams[82] = new SqlParameter("@AddressLine3", f_AddressLine3);
                else
                    sqlparams[82] = new SqlParameter("@AddressLine3", DBNull.Value);

                if (!string.IsNullOrEmpty(f_AddressLine4))
                    sqlparams[83] = new SqlParameter("@AddressLine4", f_AddressLine4);
                else
                    sqlparams[83] = new SqlParameter("@AddressLine4", DBNull.Value);

                if (f_IsRegistered.HasValue)
                    sqlparams[84] = new SqlParameter("@IsRegistered", f_IsRegistered.Value);
                else
                    sqlparams[84] = new SqlParameter("@IsRegistered", DBNull.Value);

                if (f_AreaID.HasValue)
                    sqlparams[85] = new SqlParameter("@AreaID", f_AreaID.Value);
                else
                    sqlparams[85] = new SqlParameter("@AreaID", DBNull.Value);

                if (f_ROUTEPLANID.HasValue)
                    sqlparams[86] = new SqlParameter("@ROUTEPLANID", f_ROUTEPLANID.Value);
                else
                    sqlparams[86] = new SqlParameter("@ROUTEPLANID", DBNull.Value);

                if (!string.IsNullOrEmpty(f_OSCode))
                    sqlparams[87] = new SqlParameter("@OSCode", f_OSCode);
                else
                    sqlparams[87] = new SqlParameter("@OSCode", DBNull.Value);

                if (f_CountryID.HasValue)
                    sqlparams[88] = new SqlParameter("@CountryID", f_CountryID.Value);
                else
                    sqlparams[88] = new SqlParameter("@CountryID", DBNull.Value);

                if (f_IsLoyalFlag.HasValue)
                    sqlparams[89] = new SqlParameter("@IsLoyalFlag", f_IsLoyalFlag.Value);
                else
                    sqlparams[89] = new SqlParameter("@IsLoyalFlag", DBNull.Value);

                if (f_SecurityDeposite.HasValue)
                    sqlparams[90] = new SqlParameter("@SecurityDeposite", f_SecurityDeposite.Value);
                else
                    sqlparams[90] = new SqlParameter("@SecurityDeposite", DBNull.Value);

                if (f_SpecialDeal.HasValue)
                    sqlparams[91] = new SqlParameter("@SpecialDeal", f_SpecialDeal.Value);
                else
                    sqlparams[91] = new SqlParameter("@SpecialDeal", DBNull.Value);

                if (f_TaxCardTypeID.HasValue)
                    sqlparams[92] = new SqlParameter("@TaxCardTypeID", f_TaxCardTypeID.Value);
                else
                    sqlparams[92] = new SqlParameter("@TaxCardTypeID", DBNull.Value);

                if (f_CustomerCategoryID.HasValue)
                    sqlparams[93] = new SqlParameter("@CustomerCategoryID", f_CustomerCategoryID.Value);
                else
                    sqlparams[93] = new SqlParameter("@CustomerCategoryID", DBNull.Value);

                if (f_Filer.HasValue)
                    sqlparams[94] = new SqlParameter("@Filer", f_Filer.Value);
                else
                    sqlparams[94] = new SqlParameter("@Filer", DBNull.Value);

                if (f_TaxationStatusID.HasValue)
                    sqlparams[95] = new SqlParameter("@TaxationStatusID", f_TaxationStatusID.Value);
                else
                    sqlparams[95] = new SqlParameter("@TaxationStatusID", DBNull.Value);

                if (!string.IsNullOrEmpty(f_TaxationStatus))
                    sqlparams[96] = new SqlParameter("@TaxationStatus", f_TaxationStatus);
                else
                    sqlparams[96] = new SqlParameter("@TaxationStatus", DBNull.Value);

                if (f_TaxStatementCodeID.HasValue)
                    sqlparams[97] = new SqlParameter("@TaxStatementCodeID", f_TaxStatementCodeID.Value);
                else
                    sqlparams[97] = new SqlParameter("@TaxStatementCodeID", DBNull.Value);

                if (f_TaxStatementExemptionCodeID.HasValue)
                    sqlparams[98] = new SqlParameter("@TaxStatementExemptionCodeID", f_TaxStatementExemptionCodeID.Value);
                else
                    sqlparams[98] = new SqlParameter("@TaxStatementExemptionCodeID", DBNull.Value);

                if (f_BrokerID.HasValue)
                    sqlparams[99] = new SqlParameter("@BrokerID", f_BrokerID.Value);
                else
                    sqlparams[99] = new SqlParameter("@BrokerID", DBNull.Value);

                if (f_IsExporter.HasValue)
                    sqlparams[100] = new SqlParameter("@IsExporter", f_IsExporter.Value);
                else
                    sqlparams[100] = new SqlParameter("@IsExporter", DBNull.Value);

                if (!string.IsNullOrEmpty(f_EmployeeCode))
                    sqlparams[101] = new SqlParameter("@EmployeeCode", f_EmployeeCode);
                else
                    sqlparams[101] = new SqlParameter("@EmployeeCode", DBNull.Value);

                if (!string.IsNullOrEmpty(f_PRANo))
                    sqlparams[102] = new SqlParameter("@PRANo", f_PRANo);
                else
                    sqlparams[102] = new SqlParameter("@PRANo", DBNull.Value);

                if (f_CreditLimit.HasValue)
                    sqlparams[103] = new SqlParameter("@CreditLimit", f_CreditLimit);
                else
                    sqlparams[103] = new SqlParameter("@CreditLimit", DBNull.Value);

                if (f_ISBulkSupplies.HasValue)
                    sqlparams[104] = new SqlParameter("@ISBulkSupplies", f_ISBulkSupplies.Value);
                else
                    sqlparams[104] = new SqlParameter("@ISBulkSupplies", DBNull.Value);

                sb.Append(DatabaseName);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, string.Format(Query_InsertSETCustomerPF, sb.ToString()), sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, string.Format(Query_InsertSETCustomerPF, sb.ToString()), sqlparams) > 0)
                        result = true;
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }


        private void LoadBP(int businessPartnerID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow drRecord = GetDataByID(businessPartnerID, objConnection, objTransaction);

            if (drRecord != null)
            {
                this.CustomerID = Convert.ToInt32(drRecord["CustomerID"]);
                this.CustomerName = Convert.ToString(drRecord["CustomerName"]);
                this.CNICNumber = Convert.ToString(drRecord["CNICNumber"]);

                if (drRecord["CustomerCategoryID"] != null && drRecord["CustomerCategoryID"].ToString() != "")
                    this.CustomerCategoryID = Convert.ToInt32(drRecord["CustomerCategoryID"]);
            }
        }

        private static DataRow GetDataByID(int businessPartnerID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataSet ds = null;
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@BusinessPartnerID", businessPartnerID);


                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, FnI.QueryResourceLibrary.AR.AR_Set_Customer.Query_GetData_byID, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.AR.AR_Set_Customer.Query_GetData_byID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_Set_Customer.Query_GetData_byID, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dr = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }

        public DataTable GetBusnessPartnerReportSelectionOfBusnissPartner(int CompanyId, int CustomerId, int PeriodId)
        {
            DataTable dt;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CustomerID", CustomerId);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyId);
                sqlparams[2] = new SqlParameter("@PeriodID", PeriodId);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_Set_Customer.Query_For_BusnessPartnerReportSelectionOfBusnissPartner,
                    sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable GetSupplierForAgingVendor(int VendorId, int CompanyId, int PeriodId)
        {
            DataTable dt;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@VendorID", VendorId);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyId);
                sqlparams[2] = new SqlParameter("@PeriodID", PeriodId);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_Set_Customer.Query_GetSupplierForAgingVendor, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable GetBusinessPartnerStatusDataSummary(int companyID, int periodID, string businessPartnerType, string businessPartnerIDs, DateTime fromDate,
  DateTime toDate, string TransactionType, bool discardReversedDocuments, Guid userID, Nullable<short> documentGroupID = null,
  bool documentTypeAuthentication = false, int? customerID = null, int? StatusCode = null)
        {
            #region Function Variables

            DataTable dtToReturn = null;
            string qry_SalesInvoiceFilter = string.Empty, qry_ReceiptFilter = string.Empty, qry_PurchaseInvoiceFilter = string.Empty,
                qry_PaymentFilter = string.Empty, qry_docuTypeAuthenticate = string.Empty, businessPartnerTypeFilter = string.Empty,
                businessPartnerTypeFilter2 = string.Empty, customerFilter = string.Empty, customerFilter2 = string.Empty, filter = string.Empty, filter2 = string.Empty
                , StatusCodeFilter = string.Empty;


            #endregion

            if (StatusCode != null)
            {
                if (StatusCode != 1)
                    StatusCodeFilter = " and FinalTAB.StatusCode <> 1 ";
                else
                    StatusCodeFilter = " and FinalTAB.StatusCode =  " + StatusCode;
            }

            #region Filter Reversed Documents

            if (discardReversedDocuments)
            {
                qry_SalesInvoiceFilter = " And IsNull(Sal_InvoiceHeader.IsReversed, 0) = 0 And Sal_InvoiceHeader.ReversalID is null ";
                qry_ReceiptFilter = " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                qry_PurchaseInvoiceFilter = " And IsNull(AR_InvoiceHeader.IsReversed, 0) = 0 And AR_InvoiceHeader.ReversalID is null ";
                qry_PaymentFilter = " And AP_PaymentHeader.ReversalComments is null ";
            }

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);

                #region Filteration
                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                    businessPartnerTypeFilter = " AND AR_SET_BusinessPartnerTypes.TypeCode in (" + businessPartnerType + ") ";

                if (!string.IsNullOrEmpty(businessPartnerIDs) && !businessPartnerIDs.Equals("-1"))
                    customerFilter = " AND AR_Set_Customer.CustomerID " + businessPartnerIDs + " ";

                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                    businessPartnerTypeFilter2 = " AND TABInvoice.TypeCode in (" + businessPartnerType + ") ";

                if (!string.IsNullOrEmpty(businessPartnerIDs) && !businessPartnerIDs.Equals("-1"))
                    customerFilter2 = " AND TABInvoice.CustomerID " + businessPartnerIDs + " ";

                #region Filter by Transaction Type

                if (TransactionType == "Credit")
                    filter = " where TAb.Amount < 0";
                else if (TransactionType == "Debit")
                    filter = " where TAb.Amount > 0";


                if (TransactionType == "Credit")
                    filter2 = " where TABopening.Amount < 0";
                else if (TransactionType == "Debit")
                    filter2 = " where TABopening.Amount > 0";

                #endregion


                #endregion



                #region Additional Trade

                string additionalTrade = "";

                if (customerID.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@customerid", customerID);

                    additionalTrade = @"	        union all 
	        
	        --Start Trade Back
	        
	   SELECT distinct SET_DocumentType.DocumentType, GL_TransactionHeader.DocumentDate as CreateDate, 
            AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            GL_TransactionDetail.SerialNo + 1 as SourceID,
            (GL_TransactionDetail.Amount * -1) as Amount, 
            '0' AS InstrumentNo, 
            '' AS Comments, 
            '' as InvoiceNumbers, '' as InvoiceDocumentNo, ISNULL(INV_StockTransactionHeader.DocumentNo, 0) AS DocumentNo, 
            GL_TransactionHeader.DocumentDate AS DocumentDate, 
            0 AS FreightAmount, 
            0 As ForAlhajAmount 
            ,GL_TransactionHeader.StatusCode
 FROM GL_TransactionHeader 

INNER JOIN SET_DocumentType ON GL_TransactionHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID and SET_DocumentType.DocumentType like '%trade%'
And SET_DocumentType.Status= 1
INNER JOIN GL_TransactionDetail ON GL_TransactionDetail.TransactionID =GL_TransactionHeader.TransactionID 
and GL_TransactionDetail.TransactionType ='Credit'
And GL_TransactionDetail.Status= 1
INNER JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = GL_TransactionDetail.SourceHeaderID
And INV_StockTransactionHeader.status= 1
INNER JOIN AR_SET_Customer ON INV_StockTransactionHeader.BusinessPartnerID = AR_SET_Customer.CustomerID 

AND AR_Set_Customer.CustomerID  between @customerid AND  @customerid 
AND AR_SET_Customer.Status = 1 

	        WHERE GL_TransactionHeader.CompanyID = @companyId AND GL_TransactionHeader.PeriodID = @PeriodId 
            AND GL_TransactionHeader.DocumentDate BETWEEN @FromDate AND @ToDate 
            And GL_TransactionHeader.Status= 1
	        
	        --End Trade Back ";

                }

                #endregion

                #region Document Group



                if (documentGroupID.HasValue)
                {
                    qry_SalesInvoiceFilter += " AND Set_DocumentType.GroupID = " + documentGroupID;
                    qry_ReceiptFilter += " AND Set_DocumentType.GroupID = " + documentGroupID;// +" or  Set_DocumentType.DocumentType like '%Opening%'";
                    qry_PurchaseInvoiceFilter += " AND Set_DocumentType.GroupID = " + documentGroupID;// +" or  Set_DocumentType.DocumentType like '%Opening%'";
                    qry_PaymentFilter += " AND Set_DocumentType.GroupID = " + documentGroupID;// +" or  Set_DocumentType.DocumentType like '%Opening%'";
                }

                //if (documentGroupID.HasValue)
                //{
                //    qry_SalesInvoiceFilter += " AND Set_DocumentType.GroupID = " + documentGroupID ;
                //    qry_ReceiptFilter += " AND Set_DocumentType.GroupID = " + documentGroupID ;
                //    qry_PurchaseInvoiceFilter += " AND Set_DocumentType.GroupID = " + documentGroupID ;
                //    qry_PaymentFilter += " AND Set_DocumentType.GroupID = " + documentGroupID ;
                //}

                #endregion


                if (documentTypeAuthentication)
                {
                    sqlparams[5] = new SqlParameter("@UserID", userID);
                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                }

                #region Append Opening Query For Others Groups

                string appendingOpeningQueryForOthers = " ";

                if (documentGroupID != 1)
                {
                    appendingOpeningQueryForOthers = @" union all 
                    -----------Sale Invoice Header-----------  New Opening
        SELECT 'Opening' as DocumentType, SAL_InvoiceHeader.CreateDate, 
            AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            SAL_InvoiceHeader.InvoiceID as SourceID,
            case when SET_DocumentType.DocumentType = 'Credit Note' then SAL_InvoiceHeader.AmountPaid * -1 else
            SAL_InvoiceHeader.AmountPaid end as Amount,
    '0' AS InstrumentNo, '' AS Comments, '' as InvoiceNumbers, '' as InvoiceDocumentNo, 
            ISNULL(SAL_InvoiceHeader.InvoiceNumber, 0) AS DocumentNo, SAL_InvoiceHeader.InvoiceDate AS DocumentDate, 
            CASE WHEN GL_TransactionDetail.TransactionType = 'Debit' THEN ISNULL(SAL_InvoiceHeader.FreightAmount * -1, 0) else ISNULL(SAL_InvoiceHeader.FreightAmount, 0) 
                end as FreightAmount , 0 As ForAlhajAmount,SAL_InvoiceHeader.StatusCode
        FROM SET_DocumentType 
            INNER JOIN SAL_InvoiceHeader ON SET_DocumentType.CompanyID = SAL_InvoiceHeader.CompanyID AND SET_DocumentType.PeriodID = SAL_InvoiceHeader.PeriodID 
                AND SET_DocumentType.DocumentTypeID = SAL_InvoiceHeader.InvoiceType
                And SAL_InvoiceHeader.Status= 1 
            INNER JOIN AR_SET_Customer ON SAL_InvoiceHeader.CustomerID = AR_SET_Customer.CustomerID --AND SAL_InvoiceHeader.CompanyID = AR_SET_Customer.CompanyID 
                AND SAL_InvoiceHeader.PeriodID = AR_SET_Customer.PeriodID AND SAL_InvoiceHeader.Status = 1 --AND SAL_InvoiceHeader.StatusCode = 1 
                AND AR_SET_Customer.Status = 1
				  {customer_filter}
            INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
                --AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID 
                AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
                 
            INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
                
            LEFT JOIN GL_TransactionDetail ON GL_TransactionDetail.CodeCombinationID = SAL_InvoiceHeader.FreightCode 
                AND GL_TransactionDetail.SourceHeaderID = SAL_InvoiceHeader.InvoiceID AND GL_TransactionDetail.CompanyID = SAL_InvoiceHeader.CompanyID 
                AND GL_TransactionDetail.PeriodID = SAL_InvoiceHeader.PeriodID
               AND GL_TransactionDetail.status= 1
        WHERE --AR_SET_Customer.CompanyID = @CompanyID AND 
        AR_SET_Customer.PeriodID = @PeriodID AND SAL_InvoiceHeader.InvoiceDate < @FromDate 
             and Set_DocumentType.DocumentType like '%opening%'
        GROUP BY SAL_InvoiceHeader.CreateDate, AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName, SAL_InvoiceHeader.InvoiceID, SAL_InvoiceHeader.AmountPaid, SAL_InvoiceHeader.InvoiceNumber, SAL_InvoiceHeader.InvoiceDate, 
            SET_DocumentType.DocumentType, GL_TransactionDetail.TransactionType, SAL_InvoiceHeader.FreightAmount ,SAL_InvoiceHeader.StatusCode

";
                    if (CustomerID != null && CustomerID > 0)
                        appendingOpeningQueryForOthers = appendingOpeningQueryForOthers.Replace("{customer_filter}", " AND AR_Set_Customer.CustomerID " + businessPartnerIDs + " ");
                    else
                        appendingOpeningQueryForOthers = appendingOpeningQueryForOthers.Replace("{customer_filter}", "");

                }


                #endregion

                string query = string.Format(FnI.QueryResourceLibrary.AR.AR_Set_Customer.Query_GetBusinessPartnerStatusSummary, businessPartnerTypeFilter, customerFilter, filter, qry_SalesInvoiceFilter,
                    qry_ReceiptFilter, qry_PurchaseInvoiceFilter, qry_PaymentFilter, qry_docuTypeAuthenticate, businessPartnerTypeFilter2, customerFilter2, additionalTrade, appendingOpeningQueryForOthers, StatusCodeFilter);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

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

        public DataTable GetBusinessPartnerStatusData(int companyID, int periodID, string businessPartnerType, string businessPartnerIDs, DateTime fromDate,
            DateTime toDate, string TransactionType, Guid userID, bool isAutoVehicle = false, bool discardReversedDocuments = false,
            bool documentTypeAuthentication = false)
        {
            #region Function Variables

            DataTable dtToReturn = null;
            StringBuilder filter = new StringBuilder();
            string query = string.Empty, qry_SalesInvoiceFilter = string.Empty, qry_ReceiptFilter = string.Empty, qry_PurchaseInvoiceFilter = string.Empty,
                qry_PaymentFilter = string.Empty, qry_docuTypeAuthenticate = string.Empty;

            #endregion

            #region Filter Reversed Documents

            if (discardReversedDocuments)
            {
                qry_SalesInvoiceFilter = " And IsNull(Sal_InvoiceHeader.IsReversed, 0) = 0 And Sal_InvoiceHeader.ReversalID is null ";
                qry_ReceiptFilter = " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                qry_PurchaseInvoiceFilter = " And IsNull(AR_InvoiceHeader.IsReversed, 0) = 0 And AR_InvoiceHeader.ReversalID is null ";
                qry_PaymentFilter = " And AP_PaymentHeader.ReversalComments is null ";
            }

            #endregion

            try
            {
                #region SQL Parameters

                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);

                sqlparams[5] = new SqlParameter("@customerid", businessPartnerIDs);





                #endregion

                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                    filter.Append(" AND AR_SET_BusinessPartnerTypes.TypeCode in (" + businessPartnerType + ") ");

                if (!string.IsNullOrEmpty(businessPartnerIDs) && !businessPartnerIDs.Equals("-1"))
                    filter.Append(" AND AR_Set_Customer.CustomerID " + businessPartnerIDs + " ");

                if (TransactionType != "-1")
                    filter.Append(" AND Set_DocumentType.TransactionType = '" + TransactionType + "'");

                if (documentTypeAuthentication)
                {
                    sqlparams[4] = new SqlParameter("@UserID", userID);
                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                }

                #region Query

                string Query_GetBusinessPartnerStatus = @" 
SELECT FINTAB.DocumentType, Fintab.CreateDate, FINTAB.Name, SUM(FINTAB.Amount) as Amount, FINTAB.InstrumentNo, FINTAB.Comments, FINTAB.InvoiceNumbers, 
    FINTAB.InvoiceDocumentNo, FINTAB.DocumentNo, FINTAB.DocumentDate, FINTAB.FreightAmount , FINTAB.ForAlhajAmount
FROM
(
	Select 'Opening' as DocumentType, @FromDate AS CreateDate, openingTAB.Name, sum(openingTAB.Amount) as Amount, '' as InstrumentNo, '' as Comments, 
        '' as InvoiceNumbers, '' as InvoiceDocumentNo, '' as DocumentNo, @FromDate as DocumentDate, 0 AS FreightAmount  , 0 As ForAlhajAmount
	From
	(   ---------- OPENING BALANCE VENDOR INVOICE ----------
        Select isnull(Sum(AR_InvoiceHeader.AmountPaid), 0) * -1 As Amount, AR_SET_Customer.CustomerName AS Name From AR_SET_Customer 
            left Join AR_InvoiceHeader ON AR_InvoiceHeader.VendorId = AR_SET_Customer.CustomerID --And AR_SET_Customer.CompanyId = AR_InvoiceHeader.CompanyId 
                And AR_SET_Customer.PeriodID = AR_InvoiceHeader.PeriodID AND AR_InvoiceHeader.Status = 1 And AR_InvoiceHeader.StatusCode = 1 
                {4} 
            left join SET_DocumentType on SET_DocumentType.DocumentTypeID = AR_InvoiceHeader.InvoiceType and SET_DocumentType.CompanyID = AR_InvoiceHeader.CompanyID 
                and SET_DocumentType.PeriodID = AR_InvoiceHeader.PeriodID 
            INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
            --and AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID 
            AND AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
            {6} 
        where AR_InvoiceHeader.InvoiceDate < @FromDate --and AR_SET_Customer.CompanyID = @CompanyID 
        and AR_SET_Customer.PeriodID = @PeriodId 
            and AR_SET_Customer.Status = 1 
            {0} 
        group by AR_SET_Customer.CustomerName 

        Union All
                    --OPENING BALANCE PAYMENT
        Select isnull(Sum(AP_PaymentHeader.Amount), 0) As Amount, AR_SET_Customer.CustomerName AS Name 
        From AR_SET_Customer 
			left Join AP_PaymentHeader ON AP_PaymentHeader.VendorId = AR_SET_Customer.CustomerID --And AR_SET_Customer.CompanyId = AP_PaymentHeader.CompanyId 
                And AR_SET_Customer.PeriodID = AP_PaymentHeader.PeriodID AND AP_PaymentHeader.Status = 1 And AP_PaymentHeader.StatusCode = 1 
                {5} 
            left join SET_DocumentType on SET_DocumentType.DocumentTypeID = AP_PaymentHeader.DocumentTypeID 
                and SET_DocumentType.CompanyID = AP_PaymentHeader.CompanyID and SET_DocumentType.PeriodID = AP_PaymentHeader.PeriodID 
            INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID AND AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
            --and AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID 
            {6} 
        where AP_PaymentHeader.DocumentDate < @FromDate --and AR_SET_Customer.CompanyID = @CompanyId 
        and AR_SET_Customer.PeriodID = @PeriodId 
			AND AR_SET_Customer.Status = 1 
            {0} 
        group by AR_Set_Customer.CustomerName 

        UNION ALL
                    --OPENING BALANCE SALES INVOICE 
        Select (isnull(Sum(Sal_InvoiceHeader.AmountPaid), 0) - isnull(Sum(Sal_InvoiceHeader.FreightAmount), 0)) As Amount, AR_SET_Customer.CustomerName AS Name 
        From AR_SET_Customer 
            left Join Sal_InvoiceHeader ON Sal_InvoiceHeader.CustomerId = AR_SET_Customer.CustomerID --And AR_SET_Customer.CompanyId = Sal_InvoiceHeader.CompanyId 
                And AR_SET_Customer.PeriodID = Sal_InvoiceHeader.PeriodID AND Sal_InvoiceHeader.Status = 1 And Sal_InvoiceHeader.StatusCode = 1 
                {2} 
            left join SET_DocumentType on SET_DocumentType.DocumentTypeID = Sal_InvoiceHeader.InvoiceType and SET_DocumentType.CompanyID = Sal_InvoiceHeader.CompanyID 
                and SET_DocumentType.PeriodID = Sal_InvoiceHeader.PeriodID 
            INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID AND AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
            --and AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID 
            {6} 
        where Sal_InvoiceHeader.InvoiceDate < @FromDate and AR_SET_Customer.PeriodID = @PeriodID  --and AR_SET_Customer.CompanyID = @CompanyID 
            and AR_SET_Customer.Status = 1 
            {0} 
        group by AR_SET_Customer.CustomerName 

        Union All 
                    --OPENING BALANCE RECEIPT
        Select isnull(Sum(AR_ReceiptTransactionHeader.Amount), 0) * -1 As Amount, AR_SET_Customer.CustomerName AS Name 
        From AR_SET_Customer 
            left Join AR_ReceiptTransactionHeader ON AR_ReceiptTransactionHeader.CustomerID = AR_SET_Customer.CustomerID 
                And AR_ReceiptTransactionHeader.CompanyId = AR_ReceiptTransactionHeader.CompanyId 
                And AR_ReceiptTransactionHeader.PeriodID = AR_ReceiptTransactionHeader.PeriodID AND AR_ReceiptTransactionHeader.Status = 1 
                And AR_ReceiptTransactionHeader.StatusCode = 1 
                {3} 
            left join SET_DocumentType on SET_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID 
                and SET_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID and SET_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
            INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID AND AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
            --and AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID 
            {6} 
        where AR_ReceiptTransactionHeader.DocumentDate < @FromDate and AR_ReceiptTransactionHeader.CompanyID = @CompanyID 
            and AR_ReceiptTransactionHeader.PeriodID = @PeriodID AND AR_ReceiptTransactionHeader.Status = 1 
            {0} 
        group by AR_SET_Customer.CustomerName
    ) OpeningTAB 
    GROUP BY OpeningTAB.Name 

    UNION ALL 
                -- vendor invoice
    SELECT SET_DocumentType.DocumentType, AR_InvoiceHeader.CreateDate, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
        Case when AR_Invoice_AccountsDetail.Amount is null then AR_InvoiceHeader.AmountPaid * -1 
            else (isnull(sum(case when AR_Invoice_AccountsDetail.TransactionType = 'Debit' then AR_Invoice_AccountsDetail.Amount * -1 
            else AR_Invoice_AccountsDetail.Amount end), 0) + isnull(sum(AR_InvoiceDetail.NetAmount), 0)
        ) end, 
        '0' AS InstrumentNo, '' AS Comments, '' as InvoiceNumbers, '' as InvoiceDocumentNo, ISNULL(AR_InvoiceHeader.InvoiceNumber, 0) AS DocumentNo, 
        AR_InvoiceHeader.InvoiceDate AS DocumentDate, 0 AS FreightAmount, 0 As ForAlhajAmount 
    FROM SET_DocumentType 
        INNER JOIN AR_InvoiceHeader ON SET_DocumentType.CompanyID = AR_InvoiceHeader.CompanyID AND SET_DocumentType.PeriodID = AR_InvoiceHeader.PeriodID 
            AND SET_DocumentType.DocumentTypeID = AR_InvoiceHeader.InvoiceType AND AR_InvoiceHeader.Status = 1 AND AR_InvoiceHeader.StatusCode = 1 
	    LEFT JOIN AR_Invoice_AccountsDetail on AR_Invoice_AccountsDetail.CompanyID = AR_InvoiceHeader.CompanyID 
            and AR_Invoice_AccountsDetail.PeriodID = AR_InvoiceHeader.PeriodID and AR_Invoice_AccountsDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID 
	    LEFT JOIN AR_InvoiceDetail on AR_InvoiceDetail.CompanyID = AR_InvoiceHeader.CompanyID and AR_InvoiceDetail.PeriodID = AR_InvoiceHeader.PeriodID 
            and AR_InvoiceDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID 
	    INNER JOIN AR_SET_Customer ON AR_InvoiceHeader.VendorID = AR_SET_Customer.CustomerID --AND AR_InvoiceHeader.CompanyID = AR_SET_Customer.CompanyID 
            AND AR_InvoiceHeader.PeriodID = AR_SET_Customer.PeriodID AND AR_SET_Customer.Status = 1 
	    INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
            --AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID 
            AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
	    INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
            AND AR_SET_BusinessPartner.BusinessPartnerName in ('Vendor', 'Supplier', 'Employee') 
            {0} 
        {6} 
	WHERE AR_InvoiceHeader.CompanyID = @companyId AND AR_InvoiceHeader.PeriodID = @PeriodId AND AR_InvoiceHeader.InvoiceDate BETWEEN @FromDate AND @ToDate 
        AND SET_DocumentType.DocumentType != 'Debit Invoice' 
        {4} 
	GROUP BY AR_InvoiceHeader.CreateDate, AR_SET_Customer.CustomerName, AR_InvoiceHeader.AmountPaid, AR_Invoice_AccountsDetail.Amount, AR_InvoiceHeader.InvoiceNumber, 
        AR_InvoiceHeader.InvoiceDate, SET_DocumentType.DocumentType 

    UNION ALL
    
	---------- Debit Invoice ----------
    SELECT SET_DocumentType.DocumentType, AR_InvoiceHeader.CreateDate, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
        Case when AR_Invoice_AccountsDetail.Amount is null then AR_InvoiceHeader.AmountPaid * 1 else 
            (isnull(sum(case when AR_Invoice_AccountsDetail.TransactionType = 'Debit' then AR_Invoice_AccountsDetail.Amount * -1 
                else AR_Invoice_AccountsDetail.Amount end), 0) + isnull(sum(AR_InvoiceDetail.NetAmount), 0)) end, '0' AS InstrumentNo, '' AS Comments, 
        '' as InvoiceNumbers, '' as InvoiceDocumentNo, ISNULL(AR_InvoiceHeader.InvoiceNumber, 0) AS DocumentNo, AR_InvoiceHeader.InvoiceDate AS DocumentDate, 
        0 AS FreightAmount, 0 As ForAlhajAmount 
    FROM SET_DocumentType 
        INNER JOIN AR_InvoiceHeader ON SET_DocumentType.CompanyID = AR_InvoiceHeader.CompanyID AND SET_DocumentType.PeriodID = AR_InvoiceHeader.PeriodID 
            AND SET_DocumentType.DocumentTypeID = AR_InvoiceHeader.InvoiceType AND AR_InvoiceHeader.Status = 1 AND AR_InvoiceHeader.StatusCode = 1 
	    LEFT JOIN AR_Invoice_AccountsDetail on AR_Invoice_AccountsDetail.CompanyID = AR_InvoiceHeader.CompanyID 
            and AR_Invoice_AccountsDetail.PeriodID = AR_InvoiceHeader.PeriodID and AR_Invoice_AccountsDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID 
	    LEFT JOIN AR_InvoiceDetail on AR_InvoiceDetail.CompanyID = AR_InvoiceHeader.CompanyID and AR_InvoiceDetail.PeriodID = AR_InvoiceHeader.PeriodID 
            and AR_InvoiceDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID 
	    INNER JOIN AR_SET_Customer ON AR_InvoiceHeader.VendorID = AR_SET_Customer.CustomerID --AND AR_InvoiceHeader.CompanyID = AR_SET_Customer.CompanyID 
            AND AR_InvoiceHeader.PeriodID = AR_SET_Customer.PeriodID AND AR_SET_Customer.Status = 1 
	INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
		--AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID 
		AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
	INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
		AND AR_SET_BusinessPartner.BusinessPartnerName in ('Vendor', 'Supplier', 'Employee') 
             {0} 
    {6} 
	WHERE AR_InvoiceHeader.CompanyID = @companyId 
        AND AR_InvoiceHeader.PeriodID = @PeriodId 
        AND AR_InvoiceHeader.InvoiceDate BETWEEN @FromDate AND @ToDate 
        and SET_DocumentType.DocumentType = 'Debit Invoice' 
        {4} 
	GROUP BY AR_InvoiceHeader.CreateDate, 
        AR_SET_Customer.CustomerName, 
        AR_InvoiceHeader.AmountPaid, 
        [AR_Invoice_AccountsDetail].Amount,
        AR_InvoiceHeader.InvoiceNumber, 
        AR_InvoiceHeader.InvoiceDate, 
        SET_DocumentType.DocumentType 
      
    UNION ALL
                -- Payment
    SELECT SET_DocumentType.DocumentType, 
    AP_PaymentHeader.CreateDate, 
    AR_SET_Customer.CustomerName AS BusinessPartnerName, 
    --AP_PaymentHeader.Amount, 
    (Case when AP_PaymentHeader.CustomerTransactionType is not null
			then AP_PaymentHeader.Amount * -1 
			else AP_PaymentHeader.Amount
		end) as Amount,
    AP_PaymentHeader.PaymentServerOrderNo AS InstrumentNo, 
    AP_PaymentHeader.Comments + (Case When AP_PaymentHeader.ReversalComments IS NULL Then '' Else ' ' + AP_PaymentHeader.ReversalComments End) AS Comments, 
    case when AP_PaymentHeader.POHeaderID is null then (ISNULL(
        (
            SELECT STUFF(
            (
                SELECT  AR_InvoiceHeader.InvoiceNumber 
                FROM AP_PaymentHeader childPayment 
                    LEFT JOIN AP_PaymentInvoicesDetail ON AP_PaymentInvoicesDetail.PaymentHeaderID = childPayment.PaymentID 
                        AND AP_PaymentInvoicesDetail.CompanyID = childPayment.CompanyID AND AP_PaymentInvoicesDetail.PeriodID = childPayment.PeriodID 
                    LEFT JOIN AR_InvoiceHeader ON AR_InvoiceHeader.InvoiceID = AP_PaymentInvoicesDetail.InvoiceHeaderID 
                        AND AR_InvoiceHeader.CompanyID = AP_PaymentInvoicesDetail.CompanyID AND AR_InvoiceHeader.PeriodID = AP_PaymentInvoicesDetail.PeriodID 
                    LEFT JOIN AR_InvoiceDetail ON AR_InvoiceDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID 
                        AND AR_InvoiceDetail.CompanyID = AR_InvoiceHeader.CompanyID AND AR_InvoiceDetail.PeriodID = AR_InvoiceHeader.PeriodID 
                where childPayment.PaymentID = payment.PaymentID and childPayment.CompanyID = payment.CompanyID and childPayment.PeriodID = payment.PeriodID 
                ORDER BY AR_InvoiceHeader.InvoiceNumber FOR XML PATH(''), TYPE
            ).value('.', 'varchar(max)'), 1, 2, '') AS InvoiceNumbers
            FROM AP_PaymentHeader payment 
            where payment.PaymentID = AP_PaymentHeader.PaymentID 
				and payment.CompanyID = AP_PaymentHeader.CompanyID 
				and payment.PeriodID = AP_PaymentHeader.PeriodID								
				
        ), '')) else (Select DocumentNumber from AP_PurchaseOrderHeader where AP_PurchaseOrderHeader.POHeaderID = AP_PaymentHeader.POHeaderID
						and AP_PurchaseOrderHeader.CompanyID = AP_PaymentHeader.CompanyID
						and AP_PurchaseOrderHeader.PeriodID = AP_PaymentHeader.PeriodID
			) end as InvoiceNumbers, 
        ISNULL(
        (
            SELECT STUFF(
		    (
                SELECT ', ' + AP_PaymentDetail.InvoiceDocumentNo 
                FROM AP_PaymentDetail 
                WHERE payment1.PaymentID = AP_PaymentDetail.PaymentHeaderID 
                ORDER BY AP_PaymentDetail.InvoiceDocumentNo FOR XML PATH(''), TYPE
            ).value('.', 'varchar(max)'), 1, 2, '') AS InvoiceDocumentNo
            FROM AP_PaymentHeader payment1 
            where payment1.PaymentID = AP_PaymentHeader.PaymentID and payment1.CompanyID = AP_PaymentHeader.CompanyID and payment1.PeriodID = AP_PaymentHeader.PeriodID
        ), '') as InvoiceDocumentNo, 
        ISNULL(AP_PaymentHeader.DocumentNo, 0) AS DocumentNo, AP_PaymentHeader.DocumentDate AS DocumentDate, 0 AS FreightAmount
        , IsNull(AP_Payment_AccountsDetail.Amount, 0) * -1 As ForAlhajAmount 
    FROM SET_DocumentType 
        INNER JOIN AP_PaymentHeader ON SET_DocumentType.CompanyID = AP_PaymentHeader.CompanyID AND SET_DocumentType.PeriodID = AP_PaymentHeader.PeriodID 
            AND SET_DocumentType.DocumentTypeID = AP_PaymentHeader.DocumentTypeID AND AP_PaymentHeader.Status = 1 AND AP_PaymentHeader.StatusCode = 1 
        INNER JOIN AR_SET_Customer ON AP_PaymentHeader.VendorID = AR_SET_Customer.CustomerID --AND AP_PaymentHeader.CompanyID = AR_SET_Customer.CompanyID 
            AND AP_PaymentHeader.PeriodID = AR_SET_Customer.PeriodID AND AR_SET_Customer.Status = 1 
        INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
            --AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID
            AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
        INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
            --AND AR_SET_BusinessPartner.BusinessPartnerName in ('Vendor', 'Supplier', 'Employee') 
            
            left join AP_Payment_AccountsDetail on AP_Payment_AccountsDetail.PaymentHeaderID = AP_PaymentHeader.PaymentID
             and AP_Payment_AccountsDetail.IsBP <> 0
             and AP_Payment_AccountsDetail.TransactionType = 'Credit'
            {6} 
            
             
    WHERE AP_PaymentHeader.CompanyID = @CompanyId AND AP_PaymentHeader.PeriodID = @PeriodId AND AP_PaymentHeader.DocumentDate BETWEEN @FromDate AND @ToDate 
    {0}
        {5} 
	GROUP BY AP_PaymentHeader.CreateDate, AR_SET_Customer.CustomerName, AP_PaymentHeader.CustomerTransactionType, AP_PaymentHeader.Amount, AP_PaymentHeader.DocumentNo, AP_PaymentHeader.DocumentDate, 
        SET_DocumentType.DocumentType, AP_PaymentHeader.PaymentID, AP_PaymentHeader.CompanyID, AP_PaymentHeader.PeriodID,AP_PaymentHeader.Comments, AP_PaymentHeader.POHeaderID,AP_PaymentHeader.PaymentServerOrderNo
        ,AP_Payment_AccountsDetail.Amount, AP_PaymentHeader.ReversalComments 

    UNION ALL
                -----------Sale Invoice Header-----------
    SELECT SET_DocumentType.DocumentType, SAL_InvoiceHeader.CreateDate, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
        SAL_InvoiceHeader.AmountPaid as Amount, '0' AS InstrumentNo, '' AS Comments, '' as InvoiceNumbers, '' as InvoiceDocumentNo, 
        ISNULL(SAL_InvoiceHeader.InvoiceNumber, 0) AS DocumentNo, SAL_InvoiceHeader.InvoiceDate AS DocumentDate, 
        CASE WHEN GL_TransactionDetail.TransactionType = 'Debit' THEN ISNULL(SAL_InvoiceHeader.FreightAmount * -1, 0) else ISNULL(SAL_InvoiceHeader.FreightAmount, 0) 
            end as FreightAmount , 0 As ForAlhajAmount
    FROM SET_DocumentType 
        INNER JOIN SAL_InvoiceHeader ON SET_DocumentType.CompanyID = SAL_InvoiceHeader.CompanyID AND SET_DocumentType.PeriodID = SAL_InvoiceHeader.PeriodID 
            AND SET_DocumentType.DocumentTypeID = SAL_InvoiceHeader.InvoiceType 
        INNER JOIN AR_SET_Customer ON SAL_InvoiceHeader.CustomerID = AR_SET_Customer.CustomerID -- AND SAL_InvoiceHeader.CompanyID = AR_SET_Customer.CompanyID 
            AND SAL_InvoiceHeader.PeriodID = AR_SET_Customer.PeriodID AND SAL_InvoiceHeader.Status = 1 AND SAL_InvoiceHeader.StatusCode = 1 
            AND AR_SET_Customer.Status = 1 
        INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
            --AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID 
            AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
            {0} 
        INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
            AND AR_SET_BusinessPartner.BusinessPartnerName = 'Customer' 
        LEFT JOIN GL_TransactionDetail ON GL_TransactionDetail.CodeCombinationID = SAL_InvoiceHeader.FreightCode 
            AND GL_TransactionDetail.SourceHeaderID = SAL_InvoiceHeader.InvoiceID AND GL_TransactionDetail.CompanyID = SAL_InvoiceHeader.CompanyID 
            AND GL_TransactionDetail.PeriodID = SAL_InvoiceHeader.PeriodID 
        {6} 
    WHERE --AR_SET_Customer.CompanyID = @CompanyID AND 
    AR_SET_Customer.PeriodID = @PeriodID AND SAL_InvoiceHeader.InvoiceDate BETWEEN @FromDate AND @ToDate 
        {2} 
    GROUP BY SAL_InvoiceHeader.CreateDate, AR_SET_Customer.CustomerName, SAL_InvoiceHeader.AmountPaid, SAL_InvoiceHeader.InvoiceNumber, SAL_InvoiceHeader.InvoiceDate, 
        SET_DocumentType.DocumentType, GL_TransactionDetail.TransactionType, SAL_InvoiceHeader.FreightAmount 

    union all
                --RECEIPT TRANSACTION
    SELECT SET_DocumentType.DocumentType, AR_ReceiptTransactionHeader.CreateDate, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
        Case when 
        (
            select Amount from AR_ReceiptAccountsDetail rad 
            where rad.TransactionType = 'Credit' and rad.ReceiptHeaderID = AR_ReceiptAccountsDetail.ReceiptHeaderID 
                and rad.ReceiptAccountID = AR_ReceiptAccountsDetail.ReceiptAccountID
	    ) is null then AR_ReceiptTransactionHeader.Amount * -1 else AR_ReceiptTransactionHeader.Amount * -1 end as Amount, 
        '0' AS InstrumentNo, 
        AR_ReceiptTransactionHeader.Comments + (Case When AR_ReceiptTransactionHeader.ReversalComments IS NULL Then '' Else ' ' 
            + AR_ReceiptTransactionHeader.ReversalComments End) AS Comments, 
        '' as InvoiceNumbers, '' as InvoiceDocumentNo, ISNULL(AR_ReceiptTransactionHeader.DocumentNo, 0) AS DocumentNo, 
        AR_ReceiptTransactionHeader.DocumentDate AS DocumentDate, 0 AS FreightAmount, 0 As ForAlhajAmount 
    FROM SET_DocumentType 
        INNER JOIN AR_ReceiptTransactionHeader ON SET_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
            AND SET_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID AND SET_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID 
            AND AR_ReceiptTransactionHeader.Status = 1 AND AR_ReceiptTransactionHeader.StatusCode = 1 
        INNER JOIN AR_SET_Customer ON AR_ReceiptTransactionHeader.CustomerID = AR_SET_Customer.CustomerID 
            --AND AR_ReceiptTransactionHeader.CompanyID = AR_SET_Customer.CompanyID
            AND AR_ReceiptTransactionHeader.PeriodID = AR_SET_Customer.PeriodID 
            AND AR_SET_Customer.Status = 1 
        INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
            --AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID 
            AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
            {0} 
        INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
        LEFT Join AR_ReceiptAccountsDetail on AR_ReceiptAccountsDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
            AND AR_ReceiptAccountsDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
            AND AR_ReceiptAccountsDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID	
        {6} 
    WHERE --AR_SET_Customer.CompanyID = @CompanyID AND 
        AR_SET_Customer.PeriodID = @PeriodID AND AR_ReceiptTransactionHeader.DocumentDate BETWEEN @FromDate AND @ToDate 
        {3} 
    GROUP BY AR_ReceiptTransactionHeader.CreateDate, AR_SET_Customer.CustomerName, AR_ReceiptAccountsDetail.ReceiptHeaderID, 
        AR_ReceiptAccountsDetail.ReceiptAccountID, AR_ReceiptAccountsDetail.Amount, AR_ReceiptTransactionHeader.Amount, AR_ReceiptTransactionHeader.DocumentNo, 
        AR_ReceiptTransactionHeader.DocumentDate, SET_DocumentType.DocumentType, Comments, AR_ReceiptTransactionHeader.ReversalComments 

    {1}

    {7}
) AS FINTAB 
GROUP BY DocumentType, CreateDate, NAME, InstrumentNo, Comments, InvoiceNumbers, InvoiceDocumentNo, DocumentNo, DocumentDate, FreightAmount, ForAlhajAmount 
Order By DocumentDate ";

                #endregion

                #region Auto Vehicles ammendment

                string additionalQuery = @" union All 
        
---------- PDI SERVICES (For Vehicle) ----------
SELECT distinct SET_DocumentType.DocumentType, AS_ServicePerformanceHeader.CreateDate, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
    AS_ServicePerformanceDetail.Amount * -1 as Amount, '0' AS InstrumentNo, AS_ServicePerformanceHeader.remarks AS Comments, '' as InvoiceNumbers, 
    '' as InvoiceDocumentNo, ISNULL(AS_ServicePerformanceHeader.ServiceNo, 0) AS DocumentNo, AS_ServicePerformanceHeader.ServiceDate AS DocumentDate, 
    0 AS FreightAmount, 0 As ForAlhajAmount 
FROM SET_DocumentType 
    INNER JOIN AS_ServicePerformanceHeader ON SET_DocumentType.CompanyID = AS_ServicePerformanceHeader.CompanyID 
        AND SET_DocumentType.PeriodID = AS_ServicePerformanceHeader.PeriodID AND SET_DocumentType.DocumentTypeID = AS_ServicePerformanceHeader.DocumentTypeID 
        AND AS_ServicePerformanceHeader.CurentStatusCode = 4 
    INNER JOIN AR_SET_Customer ON AS_ServicePerformanceHeader.DealerID = AR_SET_Customer.CustomerID 
        --AND AS_ServicePerformanceHeader.CompanyID = AR_SET_Customer.CompanyID 
        AND AS_ServicePerformanceHeader.PeriodID = AR_SET_Customer.PeriodID 
        AND AR_SET_Customer.Status = 1 
    INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
        --AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID 
        AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
         
    INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
    LEFT Join AS_ServicePerformanceDetail on AS_ServicePerformanceDetail.CompanyID = AS_ServicePerformanceHeader.CompanyID 
        AND AS_ServicePerformanceDetail.PeriodID = AS_ServicePerformanceHeader.PeriodID 
        AND AS_ServicePerformanceDetail.ServicePerformanceID = AS_ServicePerformanceHeader.ServicePerformanceID 
    {6} 
WHERE --AR_SET_Customer.CompanyID = @CompanyID AND 
    AR_SET_Customer.PeriodID = @PeriodID AND AS_ServicePerformanceHeader.ServiceDate BETWEEN @FromDate AND @ToDate ";

                #endregion

                #region Trade Additional Query



                string additionalTrade = @"	        union all 
	        
	        --Start Trade Back
	        
	   SELECT distinct SET_DocumentType.DocumentType, GL_TransactionHeader.DocumentDate as CreateDate, 
            AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            GL_TransactionDetail.SerialNo + 1 as SourceID,
            (GL_TransactionDetail.Amount * -1) as Amount, 
            '0' AS InstrumentNo, 
            '' AS Comments, 
            '' as InvoiceNumbers, '' as InvoiceDocumentNo, ISNULL(INV_StockTransactionHeader.DocumentNo, 0) AS DocumentNo, 
            GL_TransactionHeader.DocumentDate AS DocumentDate, 
            0 AS FreightAmount, 
            0 As ForAlhajAmount 
            
 FROM GL_TransactionHeader 

INNER JOIN SET_DocumentType ON GL_TransactionHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID and SET_DocumentType.DocumentType like '%trade%'
INNER JOIN GL_TransactionDetail ON GL_TransactionDetail.TransactionID =GL_TransactionHeader.TransactionID and GL_TransactionDetail.TransactionType ='Credit'
INNER JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = GL_TransactionDetail.SourceHeaderID
INNER JOIN AR_SET_Customer ON INV_StockTransactionHeader.BusinessPartnerID = AR_SET_Customer.CustomerID 
AND AR_Set_Customer.CustomerID  between @customerid AND  @customerid AND AR_SET_Customer.Status = 1 

	        WHERE GL_TransactionHeader.CompanyID = @companyId AND GL_TransactionHeader.PeriodID = @PeriodId AND GL_TransactionHeader.DocumentDate BETWEEN @FromDate AND @ToDate 
	        
	        --End Trade Back ";

                #endregion

                if (isAutoVehicle)
                {
                    query = string.Format(Query_GetBusinessPartnerStatus, filter.ToString(), additionalQuery, qry_SalesInvoiceFilter, qry_ReceiptFilter,
                        qry_PurchaseInvoiceFilter, qry_PaymentFilter, qry_docuTypeAuthenticate, additionalTrade);
                }
                else
                {
                    query = string.Format(Query_GetBusinessPartnerStatus, filter.ToString(), string.Empty, qry_SalesInvoiceFilter, qry_ReceiptFilter,
                        qry_PurchaseInvoiceFilter, qry_PaymentFilter, qry_docuTypeAuthenticate, additionalTrade);
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

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

        public DataTable GetMultipleBusinessPartnerStatusData(int companyID, int periodID, string businessPartnerType, string businessPartnerIDs, DateTime fromDate,
            DateTime toDate, string TransactionType, bool discardReversedDocuments, Guid userID, Nullable<short> documentGroupID = null,
            bool documentTypeAuthentication = false, int? customerID = null)
        {
            #region Function Variables

            DataTable dtToReturn = null;
            string qry_SalesInvoiceFilter = string.Empty, qry_ReceiptFilter = string.Empty, qry_PurchaseInvoiceFilter = string.Empty,
                qry_PaymentFilter = string.Empty, qry_docuTypeAuthenticate = string.Empty, businessPartnerTypeFilter = string.Empty,
                businessPartnerTypeFilter2 = string.Empty, customerFilter = string.Empty, customerFilter2 = string.Empty, filter = string.Empty;

            #endregion

            #region Filter Reversed Documents

            if (discardReversedDocuments)
            {
                qry_SalesInvoiceFilter = " And IsNull(Sal_InvoiceHeader.IsReversed, 0) = 0 And Sal_InvoiceHeader.ReversalID is null ";
                qry_ReceiptFilter = " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                qry_PurchaseInvoiceFilter = " And IsNull(AR_InvoiceHeader.IsReversed, 0) = 0 And AR_InvoiceHeader.ReversalID is null ";
                qry_PaymentFilter = " And AP_PaymentHeader.ReversalComments is null ";
            }

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);

                #region Filteration
                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                    businessPartnerTypeFilter = " AND AR_SET_BusinessPartnerTypes.TypeCode in (" + businessPartnerType + ") ";

                if (!string.IsNullOrEmpty(businessPartnerIDs) && !businessPartnerIDs.Equals("-1"))
                    customerFilter = " AND AR_Set_Customer.CustomerID " + businessPartnerIDs + " ";

                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                    businessPartnerTypeFilter2 = " AND TABInvoice.TypeCode in (" + businessPartnerType + ") ";

                if (!string.IsNullOrEmpty(businessPartnerIDs) && !businessPartnerIDs.Equals("-1"))
                    customerFilter2 = " AND TABInvoice.CustomerID " + businessPartnerIDs + " ";

                #region Filter by Transaction Type

                if (TransactionType == "Credit")
                    filter = " where TAb.Amount < 0";
                else if (TransactionType == "Debit")
                    filter = " where TAb.Amount > 0";
                #endregion


                #endregion

                #region Query Business Partner Status

                string Query_GetMultipleBusinessPartnerStatus = @" select * from 
(
    select 'Opening' AS DocumentType, @FromDate AS CreateDate, TABopening.CustomerID, TABopening.Name, -1 AS SourceID, SUM(TABopening.Amount) as Amount, 
		'' AS InstrumentNo, '' Comments, '' AS InvoiceNumbers, '' AS InvoiceDocumentNo, '' AS DocumentNo, @FromDate AS DocumentDate, 
		0 AS FreightAmount, 0 AS ForAlhajAmount 
    from 
    (
        SELECT FINTAB.DocumentType, Fintab.CreateDate, FINTAB.CustomerID, FINTAB.Name, FinTab.SourceID, SUM(FINTAB.Amount) as Amount, FINTAB.InstrumentNo, 
			FINTAB.Comments, FINTAB.InvoiceNumbers, FINTAB.InvoiceDocumentNo, FINTAB.DocumentNo, FINTAB.DocumentDate, FINTAB.FreightAmount, 
			FINTAB.ForAlhajAmount 
        FROM 
        (
            Select 'Opening' as DocumentType, @FromDate AS CreateDate, openingTAB.CustomerID, openingTAB.Name, -1 AS SourceID, sum(openingTAB.Amount) as Amount, 
				'' as InstrumentNo, '' as Comments, '' as InvoiceNumbers, '' as InvoiceDocumentNo, '' as DocumentNo, @FromDate as DocumentDate, 
				0 AS FreightAmount, 0 As ForAlhajAmount 
	        From 
	        (               ---------- OPENING BALANCE VENDOR INVOICE ----------
                SELECT 0 as Amount, AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName AS Name, AR_ReceiptCustomerDetail.ReceiptCustomerID as SourceID 
				FROM SET_DocumentType 
                    INNER JOIN AR_ReceiptTransactionHeader ON SET_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                        AND SET_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
                        AND SET_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID AND AR_ReceiptTransactionHeader.Status = 1 
                        AND AR_ReceiptTransactionHeader.StatusCode = 1 
                    INNER JOIN AR_ReceiptCustomerDetail on 
						AR_ReceiptCustomerDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID 
                        and AR_ReceiptCustomerDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                        and AR_ReceiptCustomerDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
                    INNER JOIN AR_SET_Customer ON AR_ReceiptCustomerDetail.CustomerID = AR_SET_Customer.CustomerID 
                        --AND AR_ReceiptCustomerDetail.CompanyID = AR_SET_Customer.CompanyID 
						AND AR_ReceiptCustomerDetail.PeriodID = AR_SET_Customer.PeriodID AND AR_SET_Customer.Status = 1 
						
                    INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
                        --AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID 
						AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
                        AND AR_SET_BusinessPartnerTypes.TypeCode = AR_ReceiptCustomerDetail.BusinessPartnerTypeID 
                    INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
                    LEFT Join AR_ReceiptAccountsDetail on AR_ReceiptAccountsDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                        AND AR_ReceiptAccountsDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
                        AND AR_ReceiptAccountsDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID	
                    {7} 
				WHERE AR_SET_Customer.PeriodID = @PeriodID  --AND AR_SET_Customer.CompanyID = @CompanyID 
					AND AR_ReceiptTransactionHeader.DocumentDate < @FromDate 
					{4} 
				GROUP BY AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName, AR_ReceiptCustomerDetail.TransactionType, AR_ReceiptCustomerDetail.ReceiptCustomerID
			) OpeningTAB 
			GROUP BY OpeningTAB.CustomerID ,OpeningTAB.Name 

        UNION ALL 

        select distinct TABInvoice.DocumentType, TABInvoice.CreateDate, TABInvoice.CustomerID, TABInvoice.BusinessPartnerName, TABInvoice.SourceID, TABInvoice.Amount, 
            TABInvoice.InstrumentNo, TABInvoice.Comments, TABInvoice.InvoiceNumbers, TABInvoice.InvoiceDocumentNo, TABInvoice.DocumentNo, TABInvoice.DocumentDate, 
            TABInvoice.FreightAmount, TABInvoice.ForAlhajAmount 
        from 
        (
            ---------- Vendor Invoice ----------
            SELECT distinct AR_InvoiceHeader.companyID, Set_DocumentType.TransactionType, case when AR_SET_BusinessPartnerTypes.TypeCode is null then 
                    (
                        select top 1 CUS.TypeCode from AR_SET_BusinessPartnerTypes CUS 
                        where AR_InvoiceSupplierDetail.SupplierID = CUS.BusinessPartnerID AND AR_InvoiceSupplierDetail.CompanyID = CUS.CompanyID 
                            AND AR_InvoiceSupplierDetail.PeriodID = CUS.PeriodID
                            and AR_InvoiceSupplierDetail.BusinessPartnerTypeID = CUS.TypeCode
		            ) 
                    else AR_SET_BusinessPartnerTypes.TypeCode end as TypeCode, 
                case when AR_InvoiceHeader.vendorID is null then 
                    (
                        select CUS.CustomerID from AR_SET_Customer CUS 
                        where AR_InvoiceSupplierDetail.SupplierID = CUS.CustomerID --AND AR_InvoiceSupplierDetail.CompanyID = CUS.CompanyID 
                            AND AR_InvoiceSupplierDetail.PeriodID = CUS.PeriodID
		            ) 
                    else 
                    (
                        select CUS.CustomerID from AR_SET_Customer CUS 
                        where AR_InvoiceHeader.vendorID = CUS.CustomerID  AND AR_InvoiceHeader.PeriodID = CUS.PeriodID
--AND AR_InvoiceHeader.CompanyID = CUS.CompanyID
                    ) end as CustomerID, 
                SET_DocumentType.DocumentType, AR_InvoiceHeader.CreateDate, case when AR_InvoiceHeader.vendorID is null then 
                    (
                        select CUS.CustomerName from AR_SET_Customer CUS 
                        where AR_InvoiceSupplierDetail.SupplierID = CUS.CustomerID --AND AR_InvoiceSupplierDetail.CompanyID = CUS.CompanyID 
                            AND AR_InvoiceSupplierDetail.PeriodID = CUS.PeriodID
                    ) else 
                    (
                        select CUS.CustomerName from AR_SET_Customer CUS 
                        where AR_InvoiceHeader.vendorID = CUS.CustomerID  AND AR_InvoiceHeader.PeriodID = CUS.PeriodID
--AND AR_InvoiceHeader.CompanyID = CUS.CompanyID
                    ) end as BusinessPartnerName, 
                AR_InvoiceSupplierDetail.InvoiceSupplierID as SourceID, case when AR_InvoiceHeader.vendorID is null then 
                    (Case when AR_InvoiceSupplierDetail.TransactionType = 'Debit' then AR_InvoiceSupplierDetail.Amount else AR_InvoiceSupplierDetail.Amount * -1 end) 
                    else 
                    (
                        Case when AR_InvoiceSupplierDetail.TransactionType = 'Debit' then AR_InvoiceHeader.AmountPaid 
                            when SET_DocumentType.DocumentType = 'Debit Note' then AR_InvoiceHeader.AmountPaid 
    else (AR_InvoiceHeader.AmountPaid+AR_InvoiceHeader.FreightAmount - isnull(AR_InvoiceHeader.OtherAmount,0)) * -1 end
                    ) end as Amount, 
                '0' AS InstrumentNo, AR_InvoiceHeader.Description AS Comments, AR_InvoiceHeader.VendorReferenceNumber as InvoiceNumbers, '' as InvoiceDocumentNo, 
                ISNULL(AR_InvoiceHeader.InvoiceNumber, 0) AS DocumentNo, AR_InvoiceHeader.InvoiceDate AS DocumentDate, 0 AS FreightAmount, 0 As ForAlhajAmount 
            FROM SET_DocumentType 
                INNER JOIN AR_InvoiceHeader ON SET_DocumentType.CompanyID = AR_InvoiceHeader.CompanyID AND SET_DocumentType.PeriodID = AR_InvoiceHeader.PeriodID 
                    AND SET_DocumentType.DocumentTypeID = AR_InvoiceHeader.InvoiceType AND AR_InvoiceHeader.Status = 1 AND AR_InvoiceHeader.StatusCode = 1 
	            LEFT JOIN AR_InvoiceDetail on AR_InvoiceDetail.CompanyID = AR_InvoiceHeader.CompanyID and AR_InvoiceDetail.PeriodID = AR_InvoiceHeader.PeriodID 
                    and AR_InvoiceDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID 
                LEFT JOIN AR_InvoiceSupplierDetail on AR_InvoiceSupplierDetail.CompanyID = AR_InvoiceHeader.CompanyID 
                    and AR_InvoiceSupplierDetail.PeriodID = AR_InvoiceHeader.PeriodID and AR_InvoiceSupplierDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID 
	            LEFT JOIN AR_SET_Customer ASCUS ON AR_InvoiceSupplierDetail.SupplierID = ASCUS.CustomerID 
--AND AR_InvoiceSupplierDetail.CompanyID = ASCUS.CompanyID 
                    AND AR_InvoiceSupplierDetail.PeriodID = ASCUS.PeriodID AND ASCUS.Status = 1 
	            left JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_InvoiceHeader.vENDORid 
                    AND AR_SET_BusinessPartnerTypes.CompanyID = AR_InvoiceHeader.CompanyID AND AR_SET_BusinessPartnerTypes.PeriodID = AR_InvoiceHeader.PeriodID 
	            LEFT JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
                {7} 
	        WHERE AR_InvoiceHeader.CompanyID = @companyId AND AR_InvoiceHeader.PeriodID = @PeriodId AND AR_InvoiceHeader.InvoiceDate < @FromDate 
                {5} 
	        --and SET_DocumentType.documenttype not like '%Opening%'
        ) TABInvoice
        where TABInvoice.companyID = @companyId 
            {8} 
			{9} 
        
        UNION ALL 
            ---------- Payment ----------
        SELECT SET_DocumentType.DocumentType, AP_PaymentHeader.CreateDate, AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            AP_PaymentHeader.PaymentID as SourceID, 
            (Case AP_PaymentSupplierDetail.TransactionType when 'Debit' then AP_PaymentSupplierDetail.Amount else AP_PaymentSupplierDetail.Amount * -1 end) as Amount, 
    AP_PaymentHeader.PaymentServerOrderNo AS InstrumentNo, 
        IsNull(AP_PaymentHeader.Comments, '') + (Case When AP_PaymentHeader.ReversalComments IS NULL Then '' Else ' ' + AP_PaymentHeader.ReversalComments End) AS Comments, 
        case when AP_PaymentHeader.POHeaderID is null then (ISNULL(
            (
                SELECT STUFF(
                (
                    SELECT  AR_InvoiceHeader.InvoiceNumber 
                    FROM AP_PaymentHeader childPayment 
                        LEFT JOIN AP_PaymentInvoicesDetail ON AP_PaymentInvoicesDetail.PaymentHeaderID = childPayment.PaymentID 
                            AND AP_PaymentInvoicesDetail.CompanyID = childPayment.CompanyID AND AP_PaymentInvoicesDetail.PeriodID = childPayment.PeriodID 
                        LEFT JOIN AR_InvoiceHeader ON AR_InvoiceHeader.InvoiceID = AP_PaymentInvoicesDetail.InvoiceHeaderID 
                            AND AR_InvoiceHeader.CompanyID = AP_PaymentInvoicesDetail.CompanyID AND AR_InvoiceHeader.PeriodID = AP_PaymentInvoicesDetail.PeriodID 
                        LEFT JOIN AR_InvoiceDetail ON AR_InvoiceDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID 
                            AND AR_InvoiceDetail.CompanyID = AR_InvoiceHeader.CompanyID AND AR_InvoiceDetail.PeriodID = AR_InvoiceHeader.PeriodID 
                    where childPayment.PaymentID = payment.PaymentID and childPayment.CompanyID = payment.CompanyID and childPayment.PeriodID = payment.PeriodID 
                    ORDER BY AR_InvoiceHeader.InvoiceNumber FOR XML PATH(''), TYPE
                ).value('.', 'varchar(max)'), 1, 2, '') AS InvoiceNumbers
                FROM AP_PaymentHeader payment 
                where payment.PaymentID = AP_PaymentHeader.PaymentID 
				    and payment.CompanyID = AP_PaymentHeader.CompanyID 
				    and payment.PeriodID = AP_PaymentHeader.PeriodID								
				
            ), '')) else (Select DocumentNumber from AP_PurchaseOrderHeader where AP_PurchaseOrderHeader.POHeaderID = AP_PaymentHeader.POHeaderID
						    and AP_PurchaseOrderHeader.CompanyID = AP_PaymentHeader.CompanyID
						    and AP_PurchaseOrderHeader.PeriodID = AP_PaymentHeader.PeriodID
			    ) end as InvoiceNumbers, 
            ISNULL(
            (
                SELECT STUFF(
		        (
                    SELECT ', ' + AP_PaymentDetail.InvoiceDocumentNo 
                    FROM AP_PaymentDetail 
                    WHERE payment1.PaymentID = AP_PaymentDetail.PaymentHeaderID 
                    ORDER BY AP_PaymentDetail.InvoiceDocumentNo FOR XML PATH(''), TYPE
                ).value('.', 'varchar(max)'), 1, 2, '') AS InvoiceDocumentNo
                FROM AP_PaymentHeader payment1 
                where payment1.PaymentID = AP_PaymentHeader.PaymentID and payment1.CompanyID = AP_PaymentHeader.CompanyID and payment1.PeriodID = AP_PaymentHeader.PeriodID
            ), '') as InvoiceDocumentNo, 
            ISNULL(AP_PaymentHeader.DocumentNo, 0) AS DocumentNo, AP_PaymentHeader.DocumentDate AS DocumentDate, 0 AS FreightAmount
            , IsNull(AP_Payment_AccountsDetail.Amount, 0) * -1 As ForAlhajAmount 
    FROM SET_DocumentType 
    INNER JOIN AP_PaymentHeader ON SET_DocumentType.CompanyID = AP_PaymentHeader.CompanyID AND SET_DocumentType.PeriodID = AP_PaymentHeader.PeriodID 
	    AND SET_DocumentType.DocumentTypeID = AP_PaymentHeader.DocumentTypeID AND AP_PaymentHeader.Status = 1 AND AP_PaymentHeader.StatusCode = 1     
    INNER Join AP_PaymentSupplierDetail on AP_PaymentSupplierDetail.PaymentHeaderID = AP_PaymentHeader.PaymentID
	    and AP_PaymentSupplierDetail.CompanyID = AP_PaymentHeader.CompanyID
	    and AP_PaymentSupplierDetail.PeriodID = AP_PaymentHeader.PeriodID
        and AP_PaymentSupplierDetail.Status = 1
            INNER JOIN AR_SET_Customer ON AP_PaymentSupplierDetail.SupplierID = AR_SET_Customer.CustomerID 
                --AND AP_PaymentSupplierDetail.CompanyID = AR_SET_Customer.CompanyID
                AND AP_PaymentSupplierDetail.PeriodID = AR_SET_Customer.PeriodID 
                AND AR_SET_Customer.Status = 1 
            left join AP_Payment_AccountsDetail on AP_Payment_AccountsDetail.PaymentHeaderID = AP_PaymentHeader.PaymentID and AP_Payment_AccountsDetail.IsBP <> 0 
                and AP_Payment_AccountsDetail.TransactionType = 'Credit' 
        
            INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
		    --AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID 
		    AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
             and AR_SET_BusinessPartnerTypes.TypeCode = AP_PaymentSupplierDetail.BusinessPartnerTypeID
        WHERE AP_PaymentHeader.CompanyID = @CompanyId AND AP_PaymentHeader.PeriodID = @PeriodId AND AP_PaymentHeader.DocumentDate < @FromDate 
            {0}
			{1}	
            {6} 
        GROUP BY AP_PaymentHeader.CreateDate, AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName,
            AP_PaymentSupplierDetail.PaymentSupplierID, AP_PaymentSupplierDetail.TransactionType, AP_PaymentSupplierDetail.Amount, 
            AP_PaymentHeader.DocumentNo, AP_PaymentHeader.DocumentDate, SET_DocumentType.DocumentType, AP_PaymentHeader.PaymentID, AP_PaymentHeader.CompanyID, 
            AP_PaymentHeader.PeriodID, AP_PaymentHeader.Comments, AP_PaymentHeader.POHeaderID, AP_PaymentHeader.PaymentServerOrderNo, AP_Payment_AccountsDetail.Amount, 
            AP_PaymentHeader.ReversalComments 
    UNION ALL

    -- PAYMENT
    -- SINGLE BUSINESS PARTNER CASE
    SELECT SET_DocumentType.DocumentType, 
        AP_PaymentHeader.CreateDate, 
        AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
        AP_PaymentHeader.PaymentID as SourceID,
        (Case when AP_PaymentHeader.CustomerTransactionType is not null
			    then AP_PaymentHeader.Amount * -1 
			    else AP_PaymentHeader.Amount
		    end) as Amount,
        AP_PaymentHeader.PaymentServerOrderNo AS InstrumentNo, 
        AP_PaymentHeader.Comments + (Case When AP_PaymentHeader.ReversalComments IS NULL Then '' Else ' ' + AP_PaymentHeader.ReversalComments End) AS Comments, 
        case when AP_PaymentHeader.POHeaderID is null then (ISNULL(
            (
                SELECT STUFF(
                (
                    SELECT distinct  AR_InvoiceHeader.InvoiceNumber 
                    FROM AP_PaymentHeader childPayment 
                        LEFT JOIN AP_PaymentInvoicesDetail ON AP_PaymentInvoicesDetail.PaymentHeaderID = childPayment.PaymentID 
                            AND AP_PaymentInvoicesDetail.CompanyID = childPayment.CompanyID AND AP_PaymentInvoicesDetail.PeriodID = childPayment.PeriodID 
                        LEFT JOIN AR_InvoiceHeader ON AR_InvoiceHeader.InvoiceID = AP_PaymentInvoicesDetail.InvoiceHeaderID 
                            AND AR_InvoiceHeader.CompanyID = AP_PaymentInvoicesDetail.CompanyID AND AR_InvoiceHeader.PeriodID = AP_PaymentInvoicesDetail.PeriodID 
                        LEFT JOIN AR_InvoiceDetail ON AR_InvoiceDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID 
                            AND AR_InvoiceDetail.CompanyID = AR_InvoiceHeader.CompanyID AND AR_InvoiceDetail.PeriodID = AR_InvoiceHeader.PeriodID 
                    where childPayment.PaymentID = payment.PaymentID and childPayment.CompanyID = payment.CompanyID and childPayment.PeriodID = payment.PeriodID 
                    ORDER BY AR_InvoiceHeader.InvoiceNumber FOR XML PATH(''), TYPE
                ).value('.', 'varchar(max)'), 1, 0, '') AS InvoiceNumbers
                FROM AP_PaymentHeader payment 
                where payment.PaymentID = AP_PaymentHeader.PaymentID 
				    and payment.CompanyID = AP_PaymentHeader.CompanyID 
				    and payment.PeriodID = AP_PaymentHeader.PeriodID								
				
            ), '')) else (Select DocumentNumber from AP_PurchaseOrderHeader where AP_PurchaseOrderHeader.POHeaderID = AP_PaymentHeader.POHeaderID
						    and AP_PurchaseOrderHeader.CompanyID = AP_PaymentHeader.CompanyID
						    and AP_PurchaseOrderHeader.PeriodID = AP_PaymentHeader.PeriodID
			    ) end as InvoiceNumbers, 
            ISNULL(
            (
                SELECT STUFF(
		        (
                    SELECT ', ' + AP_PaymentDetail.InvoiceDocumentNo 
                    FROM AP_PaymentDetail 
                    WHERE payment1.PaymentID = AP_PaymentDetail.PaymentHeaderID 
                    ORDER BY AP_PaymentDetail.InvoiceDocumentNo FOR XML PATH(''), TYPE
                ).value('.', 'varchar(max)'), 1, 2, '') AS InvoiceDocumentNo
                FROM AP_PaymentHeader payment1 
                where payment1.PaymentID = AP_PaymentHeader.PaymentID and payment1.CompanyID = AP_PaymentHeader.CompanyID and payment1.PeriodID = AP_PaymentHeader.PeriodID
            ), '') as InvoiceDocumentNo, 
            ISNULL(AP_PaymentHeader.DocumentNo, 0) AS DocumentNo, AP_PaymentHeader.DocumentDate AS DocumentDate, 0 AS FreightAmount
            , IsNull(AP_Payment_AccountsDetail.Amount, 0) * -1 As ForAlhajAmount 
        FROM SET_DocumentType 
            INNER JOIN AP_PaymentHeader ON SET_DocumentType.CompanyID = AP_PaymentHeader.CompanyID AND SET_DocumentType.PeriodID = AP_PaymentHeader.PeriodID 
                AND SET_DocumentType.DocumentTypeID = AP_PaymentHeader.DocumentTypeID AND AP_PaymentHeader.Status = 1 AND AP_PaymentHeader.StatusCode = 1 
            INNER JOIN AR_SET_Customer ON AP_PaymentHeader.VendorID = AR_SET_Customer.CustomerID --AND AP_PaymentHeader.CompanyID = AR_SET_Customer.CompanyID 
                AND AP_PaymentHeader.PeriodID = AR_SET_Customer.PeriodID AND AR_SET_Customer.Status = 1 
            INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
                --AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID 
                AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
            INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
            
                left join AP_Payment_AccountsDetail on AP_Payment_AccountsDetail.PaymentHeaderID = AP_PaymentHeader.PaymentID
                 and AP_Payment_AccountsDetail.IsBP <> 0
                 and AP_Payment_AccountsDetail.TransactionType = 'Credit' 
                {7} 
        WHERE AP_PaymentHeader.CompanyID = @CompanyId AND AP_PaymentHeader.PeriodID = @PeriodId AND AP_PaymentHeader.DocumentDate < @FromDate 
         {0} 
		 {1}
            {6} 
	    GROUP BY AP_PaymentHeader.CreateDate, AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName, AP_PaymentHeader.CustomerTransactionType, AP_PaymentHeader.Amount, AP_PaymentHeader.DocumentNo, AP_PaymentHeader.DocumentDate, 
            SET_DocumentType.DocumentType, AP_PaymentHeader.PaymentID, AP_PaymentHeader.CompanyID, AP_PaymentHeader.PeriodID,AP_PaymentHeader.Comments, AP_PaymentHeader.POHeaderID,AP_PaymentHeader.PaymentServerOrderNo
            ,AP_Payment_AccountsDetail.Amount, AP_PaymentHeader.ReversalComments 
        UNION ALL
                    -----------Sale Invoice Header-----------
        SELECT SET_DocumentType.DocumentType, SAL_InvoiceHeader.CreateDate, 
            AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            SAL_InvoiceHeader.InvoiceID as SourceID,
            case when SET_DocumentType.DocumentType = 'Credit Note' then SAL_InvoiceHeader.AmountPaid * -1 else
            SAL_InvoiceHeader.AmountPaid end as Amount,
    '0' AS InstrumentNo, '' AS Comments, '' as InvoiceNumbers, '' as InvoiceDocumentNo, 
            ISNULL(SAL_InvoiceHeader.InvoiceNumber, 0) AS DocumentNo, SAL_InvoiceHeader.InvoiceDate AS DocumentDate, 
            CASE WHEN GL_TransactionDetail.TransactionType = 'Debit' THEN ISNULL(SAL_InvoiceHeader.FreightAmount * -1, 0) else ISNULL(SAL_InvoiceHeader.FreightAmount, 0) 
                end as FreightAmount , 0 As ForAlhajAmount
        FROM SET_DocumentType 
            INNER JOIN SAL_InvoiceHeader ON SET_DocumentType.CompanyID = SAL_InvoiceHeader.CompanyID AND SET_DocumentType.PeriodID = SAL_InvoiceHeader.PeriodID 
                AND SET_DocumentType.DocumentTypeID = SAL_InvoiceHeader.InvoiceType 
            INNER JOIN AR_SET_Customer ON SAL_InvoiceHeader.CustomerID = AR_SET_Customer.CustomerID --AND SAL_InvoiceHeader.CompanyID = AR_SET_Customer.CompanyID 
                AND SAL_InvoiceHeader.PeriodID = AR_SET_Customer.PeriodID AND SAL_InvoiceHeader.Status = 1 AND SAL_InvoiceHeader.StatusCode = 1 
                AND AR_SET_Customer.Status = 1 
				{1}
            INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
                --AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID 
                AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
                {0} 
            INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
                --AND AR_SET_BusinessPartner.BusinessPartnerName = 'Customer' 
            LEFT JOIN GL_TransactionDetail ON GL_TransactionDetail.CodeCombinationID = SAL_InvoiceHeader.FreightCode 
                AND GL_TransactionDetail.SourceHeaderID = SAL_InvoiceHeader.InvoiceID AND GL_TransactionDetail.CompanyID = SAL_InvoiceHeader.CompanyID 
                AND GL_TransactionDetail.PeriodID = SAL_InvoiceHeader.PeriodID 
            {7} 
        WHERE --AR_SET_Customer.CompanyID = @CompanyID AND 
            AR_SET_Customer.PeriodID = @PeriodID AND SAL_InvoiceHeader.InvoiceDate < @FromDate 
            {3} 
        GROUP BY SAL_InvoiceHeader.CreateDate, AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName, SAL_InvoiceHeader.InvoiceID, SAL_InvoiceHeader.AmountPaid, SAL_InvoiceHeader.InvoiceNumber, SAL_InvoiceHeader.InvoiceDate, 
            SET_DocumentType.DocumentType, GL_TransactionDetail.TransactionType, SAL_InvoiceHeader.FreightAmount 

        union all
                    --RECEIPT TRANSACTION
        SELECT distinct SET_DocumentType.DocumentType, AR_ReceiptTransactionHeader.CreateDate, 
            AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            AR_ReceiptTransactionHeader.ReceiptTransactionID as SourceID,
            Case when 
            (
                select Amount from AR_ReceiptAccountsDetail rad 
                where rad.TransactionType = 'Credit' and rad.ReceiptHeaderID = AR_ReceiptAccountsDetail.ReceiptHeaderID 
                    and rad.ReceiptAccountID = AR_ReceiptAccountsDetail.ReceiptAccountID
	        ) is null then AR_ReceiptTransactionHeader.Amount * -1 else AR_ReceiptTransactionHeader.Amount * -1 end as Amount, 
            '0' AS InstrumentNo, 
            IsNull(AR_ReceiptTransactionHeader.Comments, '') + (Case When AR_ReceiptTransactionHeader.ReversalComments IS NULL Then '' Else ' ' 
                + AR_ReceiptTransactionHeader.ReversalComments End) AS Comments, 
            '' as InvoiceNumbers, '' as InvoiceDocumentNo, ISNULL(AR_ReceiptTransactionHeader.DocumentNo, 0) AS DocumentNo, 
            AR_ReceiptTransactionHeader.DocumentDate AS DocumentDate, 0 AS FreightAmount, 0 As ForAlhajAmount 
        FROM SET_DocumentType 
            INNER JOIN AR_ReceiptTransactionHeader ON SET_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                AND SET_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID AND SET_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID 
                AND AR_ReceiptTransactionHeader.Status = 1 AND AR_ReceiptTransactionHeader.StatusCode = 1 
            INNER JOIN AR_SET_Customer ON AR_ReceiptTransactionHeader.CustomerID = AR_SET_Customer.CustomerID 
                --AND AR_ReceiptTransactionHeader.CompanyID = AR_SET_Customer.CompanyID 
                AND AR_ReceiptTransactionHeader.PeriodID = AR_SET_Customer.PeriodID 
                AND AR_SET_Customer.Status = 1  
				{1}
            INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
                --AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID 
                AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
                {0} 
            INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
            Left Join AR_ReceiptAccountsDetail on AR_ReceiptAccountsDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                AND AR_ReceiptAccountsDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
                AND AR_ReceiptAccountsDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID	
            {7} 
        WHERE --AR_SET_Customer.CompanyID = @CompanyID AND 
            AR_SET_Customer.PeriodID = @PeriodID AND AR_ReceiptTransactionHeader.DocumentDate < @FromDate
        and AR_ReceiptTransactionHeader.ReceiptTransactionID not in 
        (
			select ReceiptHeaderID from AR_ReceiptCustomerDetail
			inner join AR_ReceiptTransactionHeader RecHDR on RecHDR.ReceiptTransactionID = AR_ReceiptTransactionHeader.ReceiptTransactionID
			and RecHDR.CompanyID = AR_ReceiptTransactionHeader.CompanyID
			and RecHDR.PeriodID = AR_ReceiptTransactionHeader.PeriodID
			inner join set_documentType on set_documentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID
			and set_documentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID
			and set_documentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
            {7} 
			where AR_ReceiptCustomerDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID
			and AR_ReceiptCustomerDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID
			and AR_ReceiptCustomerDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID
			and set_documentType.documentType not like 'Carriage Contractor Adjustment'
        ) 
            {4} 
        GROUP BY AR_ReceiptTransactionHeader.CreateDate, AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName, AR_ReceiptTransactionHeader.ReceiptTransactionID, AR_ReceiptAccountsDetail.ReceiptHeaderID, 
            AR_ReceiptAccountsDetail.ReceiptAccountID,AR_ReceiptAccountsDetail.Amount, AR_ReceiptTransactionHeader.Amount, AR_ReceiptTransactionHeader.DocumentNo, 
            AR_ReceiptTransactionHeader.DocumentDate, SET_DocumentType.DocumentType, Comments, ReversalComments 

        UNION ALL
    -- Receipt Transaction Without Opening
        SELECT SET_DocumentType.DocumentType, AR_ReceiptTransactionHeader.CreateDate, 
            AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            AR_ReceiptTransactionHeader.ReceiptTransactionID as SourceID,
            Case when AR_ReceiptCustomerDetail.TransactionType = 'Debit' then AR_ReceiptCustomerDetail.Amount else AR_ReceiptCustomerDetail.Amount * -1 end as Amount, 
            '0' AS InstrumentNo, 
            IsNull(AR_ReceiptTransactionHeader.Comments, '') + (Case When AR_ReceiptTransactionHeader.ReversalComments IS NULL Then '' Else ' ' 
                + AR_ReceiptTransactionHeader.ReversalComments End) AS Comments, 
            '' as InvoiceNumbers, '' as InvoiceDocumentNo, ISNULL(AR_ReceiptTransactionHeader.DocumentNo, 0) AS DocumentNo, AR_ReceiptTransactionHeader.DocumentDate, 
            0 AS FreightAmount, 0 As ForAlhajAmount 
        FROM SET_DocumentType 
            INNER JOIN AR_ReceiptTransactionHeader ON SET_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                AND SET_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID AND SET_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID 
                AND AR_ReceiptTransactionHeader.Status = 1 AND AR_ReceiptTransactionHeader.StatusCode = 1 
            INNER JOIN AR_ReceiptCustomerDetail on AR_ReceiptCustomerDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID 
                and AR_ReceiptCustomerDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                and AR_ReceiptCustomerDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
            INNER JOIN AR_SET_Customer ON AR_ReceiptCustomerDetail.CustomerID = AR_SET_Customer.CustomerID 
                --AND AR_ReceiptCustomerDetail.CompanyID = AR_SET_Customer.CompanyID 
                AND AR_ReceiptCustomerDetail.PeriodID = AR_SET_Customer.PeriodID 
                AND AR_SET_Customer.Status = 1  
				{1}
            INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
                --AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID 
                AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
	            {0} 
            INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
            LEFT Join AR_ReceiptAccountsDetail on AR_ReceiptAccountsDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                AND AR_ReceiptAccountsDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
                AND AR_ReceiptAccountsDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID	
            {7} 
        WHERE --AR_SET_Customer.CompanyID = @CompanyID AND 
                AR_SET_Customer.PeriodID = @PeriodID AND AR_ReceiptTransactionHeader.DocumentDate < @FromDate
              and SET_DocumentType.DocumentType not like '%opening%' 
            {4} 
        GROUP BY AR_ReceiptTransactionHeader.CreateDate, AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName, AR_ReceiptCustomerDetail.ReceiptCustomerID,
            AR_ReceiptCustomerDetail.TransactionType, AR_ReceiptCustomerDetail.Amount, 
            AR_ReceiptTransactionHeader.Amount, AR_ReceiptTransactionHeader.DocumentNo, AR_ReceiptTransactionHeader.DocumentDate, SET_DocumentType.DocumentType, Comments, 
            AR_ReceiptTransactionHeader.ReversalComments,AR_ReceiptTransactionHeader.ReceiptTransactionID

    UNION ALL
        -- Receipt Transaction [Only Opening]

        SELECT SET_DocumentType.DocumentType, AR_ReceiptTransactionHeader.CreateDate, 
            AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            AR_ReceiptCustomerDetail.ReceiptCustomerID as SourceID,
            Case when AR_ReceiptCustomerDetail.TransactionType = 'Debit' then AR_ReceiptCustomerDetail.Amount else AR_ReceiptCustomerDetail.Amount * -1 end as Amount, 
            '0' AS InstrumentNo, 
            IsNull(AR_ReceiptTransactionHeader.Comments, '') + (Case When AR_ReceiptTransactionHeader.ReversalComments IS NULL Then '' Else ' ' 
                + AR_ReceiptTransactionHeader.ReversalComments End) AS Comments, 
            '' as InvoiceNumbers, '' as InvoiceDocumentNo, ISNULL(AR_ReceiptTransactionHeader.DocumentNo, 0) AS DocumentNo, AR_ReceiptTransactionHeader.DocumentDate, 
            0 AS FreightAmount, 0 As ForAlhajAmount 
        FROM SET_DocumentType 
            INNER JOIN AR_ReceiptTransactionHeader ON SET_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                AND SET_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID AND SET_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID 
                AND AR_ReceiptTransactionHeader.Status = 1 AND AR_ReceiptTransactionHeader.StatusCode = 1 
            INNER JOIN AR_ReceiptCustomerDetail on AR_ReceiptCustomerDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID 
                and AR_ReceiptCustomerDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                and AR_ReceiptCustomerDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
            INNER JOIN AR_SET_Customer ON AR_ReceiptCustomerDetail.CustomerID = AR_SET_Customer.CustomerID 
                --AND AR_ReceiptCustomerDetail.CompanyID = AR_SET_Customer.CompanyID 
                AND AR_ReceiptCustomerDetail.PeriodID = AR_SET_Customer.PeriodID 
                AND AR_SET_Customer.Status = 1  
				{1}
            INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
                --AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID 
                AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
	            {0} 
            INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
            LEFT Join AR_ReceiptAccountsDetail on AR_ReceiptAccountsDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                AND AR_ReceiptAccountsDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
                AND AR_ReceiptAccountsDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID	
            {7} 
        WHERE AR_SET_Customer.PeriodID = @PeriodID  --AR_SET_Customer.CompanyID = @CompanyID AND 
              AND AR_ReceiptTransactionHeader.DocumentDate < @FromDate

              and SET_DocumentType.DocumentType like '%opening%' 
            {4} 
        GROUP BY AR_ReceiptTransactionHeader.CreateDate, AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName, AR_ReceiptCustomerDetail.ReceiptCustomerID,
            AR_ReceiptCustomerDetail.TransactionType, AR_ReceiptCustomerDetail.Amount, 
            AR_ReceiptTransactionHeader.Amount, AR_ReceiptTransactionHeader.DocumentNo, AR_ReceiptTransactionHeader.DocumentDate, SET_DocumentType.DocumentType, Comments, 
            AR_ReceiptTransactionHeader.ReversalComments

	    UNION ALL
	    -- Commission On Receipt
	    SELECT SET_DocumentType.DocumentType, AR_ReceiptTransactionHeader.CreateDate, 
            AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            AR_ReceiptTransactionHeader.ReceiptTransactionID as SourceID,	    
		    ((AR_ReceiptTransactionHeader.Amount * AR_ReceiptTransactionHeader.CommissionPercentage)/100) * -1 as Amount,
            '0' AS InstrumentNo, 
            IsNull(AR_ReceiptTransactionHeader.Comments, '') + (Case When AR_ReceiptTransactionHeader.ReversalComments IS NULL Then '' Else ' ' 
                + AR_ReceiptTransactionHeader.ReversalComments End) AS Comments, 
            '' as InvoiceNumbers, '' as InvoiceDocumentNo, ISNULL(AR_ReceiptTransactionHeader.DocumentNo, 0) AS DocumentNo, 
            AR_ReceiptTransactionHeader.DocumentDate AS DocumentDate, 0 AS FreightAmount, 0 As ForAlhajAmount 
        FROM SET_DocumentType 
            INNER JOIN AR_ReceiptTransactionHeader ON SET_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                AND SET_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID AND SET_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID 
                AND AR_ReceiptTransactionHeader.Status = 1 AND AR_ReceiptTransactionHeader.StatusCode = 1 
            INNER JOIN AR_SET_Customer ON AR_ReceiptTransactionHeader.EmployeeID = AR_SET_Customer.CustomerID 
                --AND AR_ReceiptTransactionHeader.CompanyID = AR_SET_Customer.CompanyID 
                AND AR_ReceiptTransactionHeader.PeriodID = AR_SET_Customer.PeriodID 
                AND AR_SET_Customer.Status = 1  
				{1}
            INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
                --AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID 
AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
                 {0} 
            INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
            Left Join AR_ReceiptAccountsDetail on AR_ReceiptAccountsDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                AND AR_ReceiptAccountsDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
                AND AR_ReceiptAccountsDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID	
            {7} 
        WHERE --AR_SET_Customer.CompanyID = @CompanyID AND 
            AR_SET_Customer.PeriodID = @PeriodID AND AR_ReceiptTransactionHeader.DocumentDate < @FromDate 
            {4} 
        GROUP BY AR_ReceiptTransactionHeader.CreateDate, AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName, 
		    AR_ReceiptTransactionHeader.ReceiptTransactionID, AR_ReceiptTransactionHeader.CommissionPercentage, AR_ReceiptAccountsDetail.ReceiptHeaderID, 
            AR_ReceiptAccountsDetail.ReceiptAccountID,AR_ReceiptAccountsDetail.Amount, AR_ReceiptTransactionHeader.Amount, AR_ReceiptTransactionHeader.DocumentNo, 
            AR_ReceiptTransactionHeader.DocumentDate, SET_DocumentType.DocumentType, Comments, ReversalComments 

     -------- SALE DISCOUNT -----------
        
            union All
            SELECT distinct SET_DocumentType.DocumentType, SAL_DiscountTransactionHeader.CreateDate, 
            AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            SAL_DiscountTransactionHeader.DiscountTransactionHeaderID as SourceID,
            (SAL_DiscountTransactionHeader.CalculatedAmount * -1) as Amount, 
            '0' AS InstrumentNo, 
            '' AS Comments, 
            '' as InvoiceNumbers, '' as InvoiceDocumentNo, ISNULL(SAL_DiscountTransactionHeader.DiscountTransactionHeaderID, 0) AS DocumentNo, 
            SAL_DiscountTransactionHeader.RunDateTime AS DocumentDate, 0 AS FreightAmount, 0 As ForAlhajAmount 
        FROM SET_DocumentType 
            INNER JOIN SAL_DiscountTransactionHeader ON SET_DocumentType.CompanyID = SAL_DiscountTransactionHeader.CompanyID 
                AND SET_DocumentType.PeriodID = SAL_DiscountTransactionHeader.PeriodID 
                AND SET_DocumentType.DocumentTypeID = SAL_DiscountTransactionHeader.DocumentTypeID
            INNER JOIN AR_SET_Customer ON SAL_DiscountTransactionHeader.CustomerID = AR_SET_Customer.CustomerID 
                --AND SAL_DiscountTransactionHeader.CompanyID = AR_SET_Customer.CompanyID 
AND SAL_DiscountTransactionHeader.PeriodID = AR_SET_Customer.PeriodID 
                AND AR_SET_Customer.Status = 1  
				{1}
            INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
                --AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID 
AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
                  {0} 
            INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
            {7} 
        WHERE AR_SET_Customer.PeriodID = @PeriodID  --And AR_SET_Customer.CompanyID = @CompanyID 
        AND SAL_DiscountTransactionHeader.RunDateTime < @FromDate
    ) AS FINTAB
    GROUP BY DocumentType, CreateDate, CustomerID, NAME, SourceID, InstrumentNo, Comments, InvoiceNumbers, InvoiceDocumentNo, DocumentNo, DocumentDate, FreightAmount, ForAlhajAmount 
) as TABopening
-- For Transation type Filter
{2}
GROUP BY 
TABopening.CustomerID, TABopening.Name


UNION ALL

select * from 
(
    SELECT FINTAB.DocumentType, Fintab.CreateDate, FINTAB.CustomerID, FINTAB.Name, FinTab.SourceID, SUM(FINTAB.Amount) as Amount, FINTAB.InstrumentNo, FINTAB.Comments, 
        FINTAB.InvoiceNumbers, FINTAB.InvoiceDocumentNo, FINTAB.DocumentNo, FINTAB.DocumentDate, FINTAB.FreightAmount, FINTAB.ForAlhajAmount 
    FROM
    (
	    Select 'Opening' as DocumentType, @FromDate AS CreateDate, OpeningTAB.CustomerID, openingTAB.Name, -1 AS SourceID, sum(openingTAB.Amount) as Amount, '' as InstrumentNo, 
            '' as Comments, '' as InvoiceNumbers, '' as InvoiceDocumentNo, '' as DocumentNo, @FromDate as DocumentDate, 0 AS FreightAmount, 0 As ForAlhajAmount 
	    From 
	    (        
            SELECT 0 as Amount,  0 AS CustomerID,  '' AS Name, 0 as SourceID 
            FROM SET_DocumentType 
                INNER JOIN AR_ReceiptTransactionHeader ON SET_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                    AND SET_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID AND SET_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID 
                    AND AR_ReceiptTransactionHeader.Status = 1 AND AR_ReceiptTransactionHeader.StatusCode = 1 
                INNER JOIN AR_ReceiptCustomerDetail on AR_ReceiptCustomerDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID 
                    and AR_ReceiptCustomerDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                    and AR_ReceiptCustomerDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
                INNER JOIN AR_SET_Customer ON AR_ReceiptCustomerDetail.CustomerID = AR_SET_Customer.CustomerID 
                    --AND AR_ReceiptCustomerDetail.CompanyID = AR_SET_Customer.CompanyID 
                    AND AR_ReceiptCustomerDetail.PeriodID = AR_SET_Customer.PeriodID 
                    AND AR_SET_Customer.Status = 1  
                INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
                    --AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID 
                    AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
                    AND AR_SET_BusinessPartnerTypes.TypeCode = AR_ReceiptCustomerDetail.BusinessPartnerTypeID 
                INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
                LEFT Join AR_ReceiptAccountsDetail on AR_ReceiptAccountsDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                    AND AR_ReceiptAccountsDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
                    AND AR_ReceiptAccountsDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID	
                {7} 
            WHERE AR_SET_Customer.PeriodID = @PeriodID AND AR_ReceiptTransactionHeader.DocumentDate < @FromDate  --AR_SET_Customer.CompanyID = @CompanyID AND 
                {0} 
				{1}
                {4} 
            GROUP BY AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName, AR_ReceiptCustomerDetail.TransactionType, AR_ReceiptCustomerDetail.ReceiptCustomerID
        ) OpeningTAB
        GROUP BY OpeningTAB.CustomerID, OpeningTAB.Name 

        UNION ALL 

        select distinct TABInvoice.DocumentType, TABInvoice.CreateDate, TABInvoice.CustomerID, TABInvoice.BusinessPartnerName, TABInvoice.SourceID, TABInvoice.Amount, TABInvoice.InstrumentNo, 
            TABInvoice.Comments, TABInvoice.InvoiceNumbers, TABInvoice.InvoiceDocumentNo, TABInvoice.DocumentNo, TABInvoice.DocumentDate, TABInvoice.FreightAmount, 
            TABInvoice.ForAlhajAmount 
        from 
        (
            ---------- Vendor Invoice ----------
            SELECT distinct AR_InvoiceHeader.companyID, Set_DocumentType.TransactionType, case when AR_SET_BusinessPartnerTypes.TypeCode is null then 
                    (
                        select top 1 CUS.TypeCode from AR_SET_BusinessPartnerTypes CUS 
                        where AR_InvoiceSupplierDetail.SupplierID = CUS.BusinessPartnerID AND AR_InvoiceSupplierDetail.CompanyID = CUS.CompanyID 
                            AND AR_InvoiceSupplierDetail.PeriodID = CUS.PeriodID
                            and AR_InvoiceSupplierDetail.BusinessPartnerTypeID = CUS.TypeCode
		            ) 
                    else AR_SET_BusinessPartnerTypes.TypeCode end as TypeCode, 
                case when AR_InvoiceHeader.vendorID is null then 
                    (
                        select CUS.CustomerID from AR_SET_Customer CUS 
                        where AR_InvoiceSupplierDetail.SupplierID = CUS.CustomerID --AND AR_InvoiceSupplierDetail.CompanyID = CUS.CompanyID 
                            AND AR_InvoiceSupplierDetail.PeriodID = CUS.PeriodID
		            ) 
                    else 
                    (
                        select CUS.CustomerID from AR_SET_Customer CUS 
                        where AR_InvoiceHeader.vendorID = CUS.CustomerID  AND AR_InvoiceHeader.PeriodID = CUS.PeriodID
--AND AR_InvoiceHeader.CompanyID = CUS.CompanyID
                    ) end as CustomerID, 
                SET_DocumentType.DocumentType, AR_InvoiceHeader.CreateDate, case when AR_InvoiceHeader.vendorID is null then 
                    (
                        select CUS.CustomerName from AR_SET_Customer CUS 
                        where AR_InvoiceSupplierDetail.SupplierID = CUS.CustomerID --AND AR_InvoiceSupplierDetail.CompanyID = CUS.CompanyID 
                            AND AR_InvoiceSupplierDetail.PeriodID = CUS.PeriodID
                    ) else 
                    (
                        select CUS.CustomerName from AR_SET_Customer CUS 
                        where AR_InvoiceHeader.vendorID = CUS.CustomerID  AND AR_InvoiceHeader.PeriodID = CUS.PeriodID
--AND AR_InvoiceHeader.CompanyID = CUS.CompanyID
                    ) end as BusinessPartnerName, 
                AR_InvoiceSupplierDetail.InvoiceSupplierID as SourceID, case when AR_InvoiceHeader.vendorID is null then 
                    (Case when AR_InvoiceSupplierDetail.TransactionType = 'Debit' then AR_InvoiceSupplierDetail.Amount 
                    else AR_InvoiceSupplierDetail.Amount * -1 end) 
                    else 
                    (
                        Case when AR_InvoiceSupplierDetail.TransactionType = 'Debit' 
                        then AR_InvoiceHeader.AmountPaid 
                            when SET_DocumentType.DocumentType = 'Debit Note' 
                            then AR_InvoiceHeader.AmountPaid 
                            else (AR_InvoiceHeader.AmountPaid+AR_InvoiceHeader.FreightAmount - isnull(AR_InvoiceHeader.OtherAmount,0)) * -1 end
                    ) end as Amount, 
                '0' AS InstrumentNo, AR_InvoiceHeader.Description AS Comments, AR_InvoiceHeader.VendorReferenceNumber as InvoiceNumbers, '' as InvoiceDocumentNo, 
                ISNULL(AR_InvoiceHeader.InvoiceNumber, 0) AS DocumentNo, AR_InvoiceHeader.InvoiceDate AS DocumentDate, 0 AS FreightAmount, 0 As ForAlhajAmount 
            FROM SET_DocumentType 
                INNER JOIN AR_InvoiceHeader ON SET_DocumentType.CompanyID = AR_InvoiceHeader.CompanyID AND SET_DocumentType.PeriodID = AR_InvoiceHeader.PeriodID 
                    AND SET_DocumentType.DocumentTypeID = AR_InvoiceHeader.InvoiceType AND AR_InvoiceHeader.Status = 1 AND AR_InvoiceHeader.StatusCode = 1 
	            LEFT JOIN AR_InvoiceDetail on AR_InvoiceDetail.CompanyID = AR_InvoiceHeader.CompanyID and AR_InvoiceDetail.PeriodID = AR_InvoiceHeader.PeriodID 
                    and AR_InvoiceDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID 
                LEFT JOIN AR_InvoiceSupplierDetail on AR_InvoiceSupplierDetail.CompanyID = AR_InvoiceHeader.CompanyID 
                    and AR_InvoiceSupplierDetail.PeriodID = AR_InvoiceHeader.PeriodID and AR_InvoiceSupplierDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID 
	            LEFT JOIN AR_SET_Customer ASCUS ON AR_InvoiceSupplierDetail.SupplierID = ASCUS.CustomerID 
--AND AR_InvoiceSupplierDetail.CompanyID = ASCUS.CompanyID 
                    AND AR_InvoiceSupplierDetail.PeriodID = ASCUS.PeriodID AND ASCUS.Status = 1 
	            left JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_InvoiceHeader.vENDORid 
                    AND AR_SET_BusinessPartnerTypes.CompanyID = AR_InvoiceHeader.CompanyID AND AR_SET_BusinessPartnerTypes.PeriodID = AR_InvoiceHeader.PeriodID 
	            LEFT JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
                {7} 
	        WHERE AR_InvoiceHeader.CompanyID = @companyId AND AR_InvoiceHeader.PeriodID = @PeriodId AND AR_InvoiceHeader.InvoiceDate BETWEEN @FromDate AND @ToDate 
                {5}
        ) TABInvoice
        where TABInvoice.companyID = @companyId 
            {8} 
			{9}
        
        UNION ALL 
            ---------- Payment ----------
        SELECT SET_DocumentType.DocumentType, AP_PaymentHeader.CreateDate, AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            AP_PaymentSupplierDetail.PaymentSupplierID as SourceID, 
            (Case AP_PaymentSupplierDetail.TransactionType when 'Debit' then AP_PaymentSupplierDetail.Amount else AP_PaymentSupplierDetail.Amount * -1 end) as Amount, 
    AP_PaymentHeader.PaymentServerOrderNo AS InstrumentNo, 
        IsNull(AP_PaymentHeader.Comments, '') + (Case When AP_PaymentHeader.ReversalComments IS NULL Then '' Else ' ' + AP_PaymentHeader.ReversalComments End) AS Comments, 
        case when AP_PaymentHeader.POHeaderID is null then (ISNULL(
            (
                SELECT STUFF(
                (
                    SELECT  AR_InvoiceHeader.InvoiceNumber 
                    FROM AP_PaymentHeader childPayment 
                        LEFT JOIN AP_PaymentInvoicesDetail ON AP_PaymentInvoicesDetail.PaymentHeaderID = childPayment.PaymentID 
                            AND AP_PaymentInvoicesDetail.CompanyID = childPayment.CompanyID AND AP_PaymentInvoicesDetail.PeriodID = childPayment.PeriodID 
                        LEFT JOIN AR_InvoiceHeader ON AR_InvoiceHeader.InvoiceID = AP_PaymentInvoicesDetail.InvoiceHeaderID 
                            AND AR_InvoiceHeader.CompanyID = AP_PaymentInvoicesDetail.CompanyID AND AR_InvoiceHeader.PeriodID = AP_PaymentInvoicesDetail.PeriodID 
                        LEFT JOIN AR_InvoiceDetail ON AR_InvoiceDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID 
                            AND AR_InvoiceDetail.CompanyID = AR_InvoiceHeader.CompanyID AND AR_InvoiceDetail.PeriodID = AR_InvoiceHeader.PeriodID 
                    where childPayment.PaymentID = payment.PaymentID and childPayment.CompanyID = payment.CompanyID and childPayment.PeriodID = payment.PeriodID 
                    ORDER BY AR_InvoiceHeader.InvoiceNumber FOR XML PATH(''), TYPE
                ).value('.', 'varchar(max)'), 1, 2, '') AS InvoiceNumbers
                FROM AP_PaymentHeader payment 
                where payment.PaymentID = AP_PaymentHeader.PaymentID 
				    and payment.CompanyID = AP_PaymentHeader.CompanyID 
				    and payment.PeriodID = AP_PaymentHeader.PeriodID								
				
            ), '')) else (Select DocumentNumber from AP_PurchaseOrderHeader where AP_PurchaseOrderHeader.POHeaderID = AP_PaymentHeader.POHeaderID
						    and AP_PurchaseOrderHeader.CompanyID = AP_PaymentHeader.CompanyID
						    and AP_PurchaseOrderHeader.PeriodID = AP_PaymentHeader.PeriodID
			    ) end as InvoiceNumbers, 
            ISNULL(
            (
                SELECT STUFF(
		        (
                    SELECT ', ' + AP_PaymentDetail.InvoiceDocumentNo 
                    FROM AP_PaymentDetail 
                    WHERE payment1.PaymentID = AP_PaymentDetail.PaymentHeaderID 
                    ORDER BY AP_PaymentDetail.InvoiceDocumentNo FOR XML PATH(''), TYPE
                ).value('.', 'varchar(max)'), 1, 2, '') AS InvoiceDocumentNo
                FROM AP_PaymentHeader payment1 
                where payment1.PaymentID = AP_PaymentHeader.PaymentID and payment1.CompanyID = AP_PaymentHeader.CompanyID and payment1.PeriodID = AP_PaymentHeader.PeriodID
            ), '') as InvoiceDocumentNo, 
            ISNULL(AP_PaymentHeader.DocumentNo, 0) AS DocumentNo, AP_PaymentHeader.DocumentDate AS DocumentDate, 0 AS FreightAmount
            , IsNull(AP_Payment_AccountsDetail.Amount, 0) * -1 As ForAlhajAmount 
    FROM SET_DocumentType 
    INNER JOIN AP_PaymentHeader ON SET_DocumentType.CompanyID = AP_PaymentHeader.CompanyID AND SET_DocumentType.PeriodID = AP_PaymentHeader.PeriodID 
	    AND SET_DocumentType.DocumentTypeID = AP_PaymentHeader.DocumentTypeID AND AP_PaymentHeader.Status = 1 AND AP_PaymentHeader.StatusCode = 1     
    INNER Join AP_PaymentSupplierDetail on AP_PaymentSupplierDetail.PaymentHeaderID = AP_PaymentHeader.PaymentID
	    and AP_PaymentSupplierDetail.CompanyID = AP_PaymentHeader.CompanyID
	    and AP_PaymentSupplierDetail.PeriodID = AP_PaymentHeader.PeriodID
        and AP_PaymentSupplierDetail.Status = 1
            INNER JOIN AR_SET_Customer ON AP_PaymentSupplierDetail.SupplierID = AR_SET_Customer.CustomerID 
                --AND AP_PaymentSupplierDetail.CompanyID = AR_SET_Customer.CompanyID 
AND AP_PaymentSupplierDetail.PeriodID = AR_SET_Customer.PeriodID 
                AND AR_SET_Customer.Status = 1  
            left join AP_Payment_AccountsDetail on AP_Payment_AccountsDetail.PaymentHeaderID = AP_PaymentHeader.PaymentID and AP_Payment_AccountsDetail.IsBP <> 0 
                and AP_Payment_AccountsDetail.TransactionType = 'Credit' 
        
            INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
		    --AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID 
		    AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
            and AR_SET_BusinessPartnerTypes.TypeCode = AP_PaymentSupplierDetail.BusinessPartnerTypeID
            {7} 
        WHERE AP_PaymentHeader.CompanyID = @CompanyId AND AP_PaymentHeader.PeriodID = @PeriodId AND AP_PaymentHeader.DocumentDate BETWEEN @FromDate AND @ToDate 
            {0} 
			{1}
            {6} 
        GROUP BY AP_PaymentHeader.CreateDate, AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName,
            AP_PaymentSupplierDetail.PaymentSupplierID, AP_PaymentSupplierDetail.TransactionType, AP_PaymentSupplierDetail.Amount, 
            AP_PaymentHeader.DocumentNo, AP_PaymentHeader.DocumentDate, SET_DocumentType.DocumentType, AP_PaymentHeader.PaymentID, AP_PaymentHeader.CompanyID, 
            AP_PaymentHeader.PeriodID, AP_PaymentHeader.Comments, AP_PaymentHeader.POHeaderID, AP_PaymentHeader.PaymentServerOrderNo, AP_Payment_AccountsDetail.Amount, 
            AP_PaymentHeader.ReversalComments 
    UNION ALL

    -- PAYMENT
    -- SINGLE BUSINESS PARTNER CASE
    SELECT SET_DocumentType.DocumentType, 
        AP_PaymentHeader.CreateDate, 
        AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
        AP_PaymentHeader.PaymentID as SourceID,
        (Case when AP_PaymentHeader.CustomerTransactionType is not null
			    then AP_PaymentHeader.Amount * -1 
			    else AP_PaymentHeader.Amount
		    end) as Amount,
        AP_PaymentHeader.PaymentServerOrderNo AS InstrumentNo, 
        AP_PaymentHeader.Comments + (Case When AP_PaymentHeader.ReversalComments IS NULL Then '' Else ' ' + AP_PaymentHeader.ReversalComments End) AS Comments, 
        case when AP_PaymentHeader.POHeaderID is null then (ISNULL(
            (
                SELECT STUFF(
                (
                    SELECT distinct  AR_InvoiceHeader.InvoiceNumber 
                    FROM AP_PaymentHeader childPayment 
                        LEFT JOIN AP_PaymentInvoicesDetail ON AP_PaymentInvoicesDetail.PaymentHeaderID = childPayment.PaymentID 
                            AND AP_PaymentInvoicesDetail.CompanyID = childPayment.CompanyID AND AP_PaymentInvoicesDetail.PeriodID = childPayment.PeriodID 
                        LEFT JOIN AR_InvoiceHeader ON AR_InvoiceHeader.InvoiceID = AP_PaymentInvoicesDetail.InvoiceHeaderID 
                            AND AR_InvoiceHeader.CompanyID = AP_PaymentInvoicesDetail.CompanyID AND AR_InvoiceHeader.PeriodID = AP_PaymentInvoicesDetail.PeriodID 
                        LEFT JOIN AR_InvoiceDetail ON AR_InvoiceDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID 
                            AND AR_InvoiceDetail.CompanyID = AR_InvoiceHeader.CompanyID AND AR_InvoiceDetail.PeriodID = AR_InvoiceHeader.PeriodID 
                    where childPayment.PaymentID = payment.PaymentID and childPayment.CompanyID = payment.CompanyID and childPayment.PeriodID = payment.PeriodID 
                    ORDER BY AR_InvoiceHeader.InvoiceNumber FOR XML PATH(''), TYPE
                ).value('.', 'varchar(max)'), 1, 0, '') AS InvoiceNumbers
                FROM AP_PaymentHeader payment 
                where payment.PaymentID = AP_PaymentHeader.PaymentID 
				    and payment.CompanyID = AP_PaymentHeader.CompanyID 
				    and payment.PeriodID = AP_PaymentHeader.PeriodID								
				
            ), '')) else (Select DocumentNumber from AP_PurchaseOrderHeader where AP_PurchaseOrderHeader.POHeaderID = AP_PaymentHeader.POHeaderID
						    and AP_PurchaseOrderHeader.CompanyID = AP_PaymentHeader.CompanyID
						    and AP_PurchaseOrderHeader.PeriodID = AP_PaymentHeader.PeriodID
			    ) end as InvoiceNumbers, 
            ISNULL(
            (
                SELECT STUFF(
		        (
                    SELECT ', ' + AP_PaymentDetail.InvoiceDocumentNo 
                    FROM AP_PaymentDetail 
                    WHERE payment1.PaymentID = AP_PaymentDetail.PaymentHeaderID 
                    ORDER BY AP_PaymentDetail.InvoiceDocumentNo FOR XML PATH(''), TYPE
                ).value('.', 'varchar(max)'), 1, 2, '') AS InvoiceDocumentNo
                FROM AP_PaymentHeader payment1 
                where payment1.PaymentID = AP_PaymentHeader.PaymentID and payment1.CompanyID = AP_PaymentHeader.CompanyID and payment1.PeriodID = AP_PaymentHeader.PeriodID
            ), '') as InvoiceDocumentNo, 
            ISNULL(AP_PaymentHeader.DocumentNo, 0) AS DocumentNo, AP_PaymentHeader.DocumentDate AS DocumentDate, 0 AS FreightAmount
            , IsNull(AP_Payment_AccountsDetail.Amount, 0) * -1 As ForAlhajAmount 
        FROM SET_DocumentType 
            INNER JOIN AP_PaymentHeader ON SET_DocumentType.CompanyID = AP_PaymentHeader.CompanyID AND SET_DocumentType.PeriodID = AP_PaymentHeader.PeriodID 
                AND SET_DocumentType.DocumentTypeID = AP_PaymentHeader.DocumentTypeID AND AP_PaymentHeader.Status = 1 AND AP_PaymentHeader.StatusCode = 1 
            INNER JOIN AR_SET_Customer ON AP_PaymentHeader.VendorID = AR_SET_Customer.CustomerID --AND AP_PaymentHeader.CompanyID = AR_SET_Customer.CompanyID 
                AND AP_PaymentHeader.PeriodID = AR_SET_Customer.PeriodID AND AR_SET_Customer.Status = 1  
            INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
                --AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID 
AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
            INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
            
                left join AP_Payment_AccountsDetail on AP_Payment_AccountsDetail.PaymentHeaderID = AP_PaymentHeader.PaymentID
                 and AP_Payment_AccountsDetail.IsBP <> 0
                 and AP_Payment_AccountsDetail.TransactionType = 'Credit' 
                {7} 
        WHERE AP_PaymentHeader.CompanyID = @CompanyId AND AP_PaymentHeader.PeriodID = @PeriodId AND AP_PaymentHeader.DocumentDate BETWEEN @FromDate AND @ToDate 
         {0} 
		 {1}
            {6} 
	    GROUP BY AP_PaymentHeader.CreateDate, AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName, AP_PaymentHeader.CustomerTransactionType, AP_PaymentHeader.Amount, AP_PaymentHeader.DocumentNo, AP_PaymentHeader.DocumentDate, 
            SET_DocumentType.DocumentType, AP_PaymentHeader.PaymentID, AP_PaymentHeader.CompanyID, AP_PaymentHeader.PeriodID,AP_PaymentHeader.Comments, AP_PaymentHeader.POHeaderID,AP_PaymentHeader.PaymentServerOrderNo
            ,AP_Payment_AccountsDetail.Amount, AP_PaymentHeader.ReversalComments 
        UNION ALL
                    -----------Sale Invoice Header-----------
        SELECT SET_DocumentType.DocumentType, SAL_InvoiceHeader.CreateDate, 
            AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            SAL_InvoiceHeader.InvoiceID as SourceID,
            case when SET_DocumentType.DocumentType = 'Credit Note' then SAL_InvoiceHeader.AmountPaid * -1 else
            SAL_InvoiceHeader.AmountPaid end as Amount,
    '0' AS InstrumentNo, '' AS Comments, '' as InvoiceNumbers, '' as InvoiceDocumentNo, 
            ISNULL(SAL_InvoiceHeader.InvoiceNumber, 0) AS DocumentNo, SAL_InvoiceHeader.InvoiceDate AS DocumentDate, 
            CASE WHEN GL_TransactionDetail.TransactionType = 'Debit' THEN ISNULL(SAL_InvoiceHeader.FreightAmount * -1, 0) else ISNULL(SAL_InvoiceHeader.FreightAmount, 0) 
                end as FreightAmount , 0 As ForAlhajAmount
        FROM SET_DocumentType 
            INNER JOIN SAL_InvoiceHeader ON SET_DocumentType.CompanyID = SAL_InvoiceHeader.CompanyID AND SET_DocumentType.PeriodID = SAL_InvoiceHeader.PeriodID 
                AND SET_DocumentType.DocumentTypeID = SAL_InvoiceHeader.InvoiceType 
            INNER JOIN AR_SET_Customer ON SAL_InvoiceHeader.CustomerID = AR_SET_Customer.CustomerID --AND SAL_InvoiceHeader.CompanyID = AR_SET_Customer.CompanyID 
                AND SAL_InvoiceHeader.PeriodID = AR_SET_Customer.PeriodID AND SAL_InvoiceHeader.Status = 1 AND SAL_InvoiceHeader.StatusCode = 1 
                AND AR_SET_Customer.Status = 1  
				{1}
            INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
                --AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID 
AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
                {0} 
            INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
                --AND AR_SET_BusinessPartner.BusinessPartnerName = 'Customer' 
            LEFT JOIN GL_TransactionDetail ON GL_TransactionDetail.CodeCombinationID = SAL_InvoiceHeader.FreightCode 
                AND GL_TransactionDetail.SourceHeaderID = SAL_InvoiceHeader.InvoiceID AND GL_TransactionDetail.CompanyID = SAL_InvoiceHeader.CompanyID 
                AND GL_TransactionDetail.PeriodID = SAL_InvoiceHeader.PeriodID 
            {7} 
        WHERE AR_SET_Customer.PeriodID = @PeriodID AND SAL_InvoiceHeader.InvoiceDate BETWEEN @FromDate AND @ToDate --AR_SET_Customer.CompanyID = @CompanyID AND 
            {3} 
        GROUP BY SAL_InvoiceHeader.CreateDate, AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName, SAL_InvoiceHeader.InvoiceID, SAL_InvoiceHeader.AmountPaid, SAL_InvoiceHeader.InvoiceNumber, SAL_InvoiceHeader.InvoiceDate, 
            SET_DocumentType.DocumentType, GL_TransactionDetail.TransactionType, SAL_InvoiceHeader.FreightAmount 

        UNION ALL
                    -----------Sale Invoice Header Detail Data-----------
        SELECT SET_DocumentType.DocumentType, SAL_InvoiceHeader.CreateDate, 
            AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            SAL_InvoiceHeader.InvoiceID as SourceID,
            case when SET_DocumentType.DocumentType = 'Credit Note' then Sal_InvoiceCustomerDetail.Amount * -1 else Sal_InvoiceCustomerDetail.Amount end as Amount,
    '0' AS InstrumentNo, '' AS Comments, '' as InvoiceNumbers, '' as InvoiceDocumentNo, ISNULL(SAL_InvoiceHeader.InvoiceNumber, 0) AS DocumentNo, SAL_InvoiceHeader.InvoiceDate AS DocumentDate, 
            CASE WHEN GL_TransactionDetail.TransactionType = 'Debit' THEN ISNULL(SAL_InvoiceHeader.FreightAmount * -1, 0) else ISNULL(SAL_InvoiceHeader.FreightAmount, 0) 
                end as FreightAmount , 0 As ForAlhajAmount
            FROM SET_DocumentType INNER JOIN SAL_InvoiceHeader ON SET_DocumentType.CompanyID = SAL_InvoiceHeader.CompanyID AND SET_DocumentType.PeriodID = SAL_InvoiceHeader.PeriodID 
            AND SET_DocumentType.DocumentTypeID = SAL_InvoiceHeader.InvoiceType 
            inner join Sal_InvoiceCustomerDetail on Sal_InvoiceCustomerDetail.InvoiceHeaderID = SAL_InvoiceHeader.InvoiceID
            and Sal_InvoiceCustomerDetail.CompanyID = SAL_InvoiceHeader.CompanyID and Sal_InvoiceCustomerDetail.PeriodID = SAL_InvoiceHeader.PeriodID    
            INNER JOIN AR_SET_Customer ON Sal_InvoiceCustomerDetail.CustomerID = AR_SET_Customer.CustomerID --AND Sal_InvoiceCustomerDetail.CompanyID = AR_SET_Customer.CompanyID 
                AND Sal_InvoiceCustomerDetail.PeriodID = AR_SET_Customer.PeriodID AND SAL_InvoiceHeader.Status = 1 AND SAL_InvoiceHeader.StatusCode = 1  AND AR_SET_Customer.Status = 1  
				{1}
            INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
                --AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID 
AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
                {0} 
            inner JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
            LEFT JOIN GL_TransactionDetail ON GL_TransactionDetail.CodeCombinationID = SAL_InvoiceHeader.FreightCode 
                AND GL_TransactionDetail.SourceHeaderID = SAL_InvoiceHeader.InvoiceID AND GL_TransactionDetail.CompanyID = SAL_InvoiceHeader.CompanyID 
                AND GL_TransactionDetail.PeriodID = SAL_InvoiceHeader.PeriodID 
            {7} 
        WHERE AR_SET_Customer.PeriodID = @PeriodID AND SAL_InvoiceHeader.InvoiceDate BETWEEN @FromDate AND @ToDate --AR_SET_Customer.CompanyID = @CompanyID AND 
            {3} 
        GROUP BY SAL_InvoiceHeader.CreateDate, AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName, SAL_InvoiceHeader.InvoiceID, Sal_InvoiceCustomerDetail.Amount, SAL_InvoiceHeader.InvoiceNumber, SAL_InvoiceHeader.InvoiceDate, 
            SET_DocumentType.DocumentType, GL_TransactionDetail.TransactionType, SAL_InvoiceHeader.FreightAmount 


        union all
                    --RECEIPT TRANSACTION
        SELECT distinct SET_DocumentType.DocumentType, AR_ReceiptTransactionHeader.CreateDate, 
            AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            AR_ReceiptTransactionHeader.ReceiptTransactionID as SourceID,
            Case when 
            (
                select Amount from AR_ReceiptAccountsDetail rad 
                where rad.TransactionType = 'Credit' and rad.ReceiptHeaderID = AR_ReceiptAccountsDetail.ReceiptHeaderID 
                    and rad.ReceiptAccountID = AR_ReceiptAccountsDetail.ReceiptAccountID
	        ) is null then AR_ReceiptTransactionHeader.Amount * -1 else AR_ReceiptTransactionHeader.Amount * -1 end as Amount, 
            '0' AS InstrumentNo, 
            IsNull(AR_ReceiptTransactionHeader.Comments, '') + (Case When AR_ReceiptTransactionHeader.ReversalComments IS NULL Then '' Else ' ' 
                + AR_ReceiptTransactionHeader.ReversalComments End) AS Comments, 
            '' as InvoiceNumbers, '' as InvoiceDocumentNo, ISNULL(AR_ReceiptTransactionHeader.DocumentNo, 0) AS DocumentNo, 
            AR_ReceiptTransactionHeader.DocumentDate AS DocumentDate, 0 AS FreightAmount, 0 As ForAlhajAmount 
        FROM SET_DocumentType 
            INNER JOIN AR_ReceiptTransactionHeader ON SET_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                AND SET_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID AND SET_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID 
                AND AR_ReceiptTransactionHeader.Status = 1 AND AR_ReceiptTransactionHeader.StatusCode = 1 
            INNER JOIN AR_SET_Customer ON AR_ReceiptTransactionHeader.CustomerID = AR_SET_Customer.CustomerID 
                --AND AR_ReceiptTransactionHeader.CompanyID = AR_SET_Customer.CompanyID 
AND AR_ReceiptTransactionHeader.PeriodID = AR_SET_Customer.PeriodID 
                AND AR_SET_Customer.Status = 1  
				{1}
            INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
                --AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID 
AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
                {0} 
            INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
            Left Join AR_ReceiptAccountsDetail on AR_ReceiptAccountsDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                AND AR_ReceiptAccountsDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
                AND AR_ReceiptAccountsDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID	
            {7} 
        WHERE AR_SET_Customer.PeriodID = @PeriodID AND AR_ReceiptTransactionHeader.DocumentDate BETWEEN @FromDate AND @ToDate --AR_SET_Customer.CompanyID = @CompanyID AND 
        and AR_ReceiptTransactionHeader.ReceiptTransactionID not in 
        (
			select ReceiptHeaderID from AR_ReceiptCustomerDetail
			inner join AR_ReceiptTransactionHeader RecHDR on RecHDR.ReceiptTransactionID = AR_ReceiptTransactionHeader.ReceiptTransactionID
			and RecHDR.CompanyID = AR_ReceiptTransactionHeader.CompanyID
			and RecHDR.PeriodID = AR_ReceiptTransactionHeader.PeriodID
			inner join set_documentType on set_documentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID
			and set_documentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID
			and set_documentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
            {7} 
			where AR_ReceiptCustomerDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID
			and AR_ReceiptCustomerDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID
			and AR_ReceiptCustomerDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID
			and set_documentType.documentType not like 'Carriage Contractor Adjustment'
        ) 
            {4} 
        GROUP BY AR_ReceiptTransactionHeader.CreateDate, AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName, AR_ReceiptTransactionHeader.ReceiptTransactionID, AR_ReceiptAccountsDetail.ReceiptHeaderID, 
            AR_ReceiptAccountsDetail.ReceiptAccountID,AR_ReceiptAccountsDetail.Amount, AR_ReceiptTransactionHeader.Amount, AR_ReceiptTransactionHeader.DocumentNo, 
            AR_ReceiptTransactionHeader.DocumentDate, SET_DocumentType.DocumentType, Comments, ReversalComments 

        UNION ALL
    -- Receipt Transaction Without Opening
        SELECT SET_DocumentType.DocumentType, AR_ReceiptTransactionHeader.CreateDate, 
            AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            AR_ReceiptCustomerDetail.ReceiptCustomerID as SourceID,
            Case when AR_ReceiptCustomerDetail.TransactionType = 'Debit' then AR_ReceiptCustomerDetail.Amount else AR_ReceiptCustomerDetail.Amount * -1 end as Amount, 
            '0' AS InstrumentNo, 
            IsNull(AR_ReceiptTransactionHeader.Comments, '') + (Case When AR_ReceiptTransactionHeader.ReversalComments IS NULL Then '' Else ' ' 
                + AR_ReceiptTransactionHeader.ReversalComments End) AS Comments, 
            '' as InvoiceNumbers, '' as InvoiceDocumentNo, ISNULL(AR_ReceiptTransactionHeader.DocumentNo, 0) AS DocumentNo, AR_ReceiptTransactionHeader.DocumentDate, 
            0 AS FreightAmount, 0 As ForAlhajAmount 
        FROM SET_DocumentType 
            INNER JOIN AR_ReceiptTransactionHeader ON SET_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                AND SET_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID AND SET_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID 
                AND AR_ReceiptTransactionHeader.Status = 1 AND AR_ReceiptTransactionHeader.StatusCode = 1 
            INNER JOIN AR_ReceiptCustomerDetail on AR_ReceiptCustomerDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID 
                and AR_ReceiptCustomerDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                and AR_ReceiptCustomerDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
            INNER JOIN AR_SET_Customer ON AR_ReceiptCustomerDetail.CustomerID = AR_SET_Customer.CustomerID 
                --AND AR_ReceiptCustomerDetail.CompanyID = AR_SET_Customer.CompanyID 
                AND AR_ReceiptCustomerDetail.PeriodID = AR_SET_Customer.PeriodID 
                AND AR_SET_Customer.Status = 1  
				{1}
            INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
                --AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID 
AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
	            {0} 
            INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
            LEFT Join AR_ReceiptAccountsDetail on AR_ReceiptAccountsDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                AND AR_ReceiptAccountsDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
                AND AR_ReceiptAccountsDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID	
            {7} 
        WHERE AR_SET_Customer.PeriodID = @PeriodID AND AR_ReceiptTransactionHeader.DocumentDate BETWEEN @FromDate AND @ToDate --AR_SET_Customer.CompanyID = @CompanyID AND 
              and SET_DocumentType.DocumentType not like '%opening%' 
            {4} 
        GROUP BY AR_ReceiptTransactionHeader.CreateDate, AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName, AR_ReceiptCustomerDetail.ReceiptCustomerID,
            AR_ReceiptCustomerDetail.TransactionType, AR_ReceiptCustomerDetail.Amount, 
            AR_ReceiptTransactionHeader.Amount, AR_ReceiptTransactionHeader.DocumentNo, AR_ReceiptTransactionHeader.DocumentDate, SET_DocumentType.DocumentType, Comments, 
            AR_ReceiptTransactionHeader.ReversalComments

    UNION ALL
        -- Receipt Transaction [Only Opening]

        SELECT SET_DocumentType.DocumentType, AR_ReceiptTransactionHeader.CreateDate, 
            AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            AR_ReceiptCustomerDetail.ReceiptCustomerID as SourceID,
            Case when AR_ReceiptCustomerDetail.TransactionType = 'Debit' then AR_ReceiptCustomerDetail.Amount else AR_ReceiptCustomerDetail.Amount * -1 end as Amount, 
            '0' AS InstrumentNo, 
            IsNull(AR_ReceiptTransactionHeader.Comments, '') + (Case When AR_ReceiptTransactionHeader.ReversalComments IS NULL Then '' Else ' ' 
                + AR_ReceiptTransactionHeader.ReversalComments End) AS Comments, 
            '' as InvoiceNumbers, '' as InvoiceDocumentNo, ISNULL(AR_ReceiptTransactionHeader.DocumentNo, 0) AS DocumentNo, AR_ReceiptTransactionHeader.DocumentDate, 
            0 AS FreightAmount, 0 As ForAlhajAmount 
        FROM SET_DocumentType 
            INNER JOIN AR_ReceiptTransactionHeader ON SET_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                AND SET_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID AND SET_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID 
                AND AR_ReceiptTransactionHeader.Status = 1 AND AR_ReceiptTransactionHeader.StatusCode = 1 
            INNER JOIN AR_ReceiptCustomerDetail on AR_ReceiptCustomerDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID 
                and AR_ReceiptCustomerDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                and AR_ReceiptCustomerDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
            INNER JOIN AR_SET_Customer ON AR_ReceiptCustomerDetail.CustomerID = AR_SET_Customer.CustomerID 
                --AND AR_ReceiptCustomerDetail.CompanyID = AR_SET_Customer.CompanyID 
AND AR_ReceiptCustomerDetail.PeriodID = AR_SET_Customer.PeriodID 
                AND AR_SET_Customer.Status = 1  
				{1}
            INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
                --AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID 
AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
	            {0} 
            INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
            LEFT Join AR_ReceiptAccountsDetail on AR_ReceiptAccountsDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                AND AR_ReceiptAccountsDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
                AND AR_ReceiptAccountsDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID	
            {7} 
        WHERE AR_SET_Customer.PeriodID = @PeriodID  --AR_SET_Customer.CompanyID = @CompanyID AND 
              AND AR_ReceiptTransactionHeader.DocumentDate between @FromDate and @Todate
              and SET_DocumentType.DocumentType like '%opening%' 
            {4} 
        GROUP BY AR_ReceiptTransactionHeader.CreateDate, AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName, AR_ReceiptCustomerDetail.ReceiptCustomerID,
            AR_ReceiptCustomerDetail.TransactionType, AR_ReceiptCustomerDetail.Amount, 
            AR_ReceiptTransactionHeader.Amount, AR_ReceiptTransactionHeader.DocumentNo, AR_ReceiptTransactionHeader.DocumentDate, SET_DocumentType.DocumentType, Comments, 
            AR_ReceiptTransactionHeader.ReversalComments

	    UNION ALL
	    -- Commission On Receipt
	    SELECT SET_DocumentType.DocumentType, AR_ReceiptTransactionHeader.CreateDate, 
            AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            AR_ReceiptTransactionHeader.ReceiptTransactionID as SourceID,	    
		    ((AR_ReceiptTransactionHeader.Amount * AR_ReceiptTransactionHeader.CommissionPercentage)/100) * -1 as Amount,
            '0' AS InstrumentNo, 
            IsNull(AR_ReceiptTransactionHeader.Comments, '') + (Case When AR_ReceiptTransactionHeader.ReversalComments IS NULL Then '' Else ' ' 
                + AR_ReceiptTransactionHeader.ReversalComments End) AS Comments, 
            '' as InvoiceNumbers, '' as InvoiceDocumentNo, ISNULL(AR_ReceiptTransactionHeader.DocumentNo, 0) AS DocumentNo, 
            AR_ReceiptTransactionHeader.DocumentDate AS DocumentDate, 0 AS FreightAmount, 0 As ForAlhajAmount 
        FROM SET_DocumentType 
            INNER JOIN AR_ReceiptTransactionHeader ON SET_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                AND SET_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID AND SET_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID 
                AND AR_ReceiptTransactionHeader.Status = 1 AND AR_ReceiptTransactionHeader.StatusCode = 1 
            INNER JOIN AR_SET_Customer ON AR_ReceiptTransactionHeader.EmployeeID = AR_SET_Customer.CustomerID 
                --AND AR_ReceiptTransactionHeader.CompanyID = AR_SET_Customer.CompanyID 
AND AR_ReceiptTransactionHeader.PeriodID = AR_SET_Customer.PeriodID 
                AND AR_SET_Customer.Status = 1  
				{1}
            INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
                --AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID 
AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
                 {0} 
            INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
            Left Join AR_ReceiptAccountsDetail on AR_ReceiptAccountsDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
                AND AR_ReceiptAccountsDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
                AND AR_ReceiptAccountsDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID	
            {7} 
        WHERE AR_SET_Customer.PeriodID = @PeriodID AND AR_ReceiptTransactionHeader.DocumentDate BETWEEN @FromDate AND @ToDate --AR_SET_Customer.CompanyID = @CompanyID AND 
            {4} 
        GROUP BY AR_ReceiptTransactionHeader.CreateDate, AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName, 
		    AR_ReceiptTransactionHeader.ReceiptTransactionID, AR_ReceiptTransactionHeader.CommissionPercentage, AR_ReceiptAccountsDetail.ReceiptHeaderID, 
            AR_ReceiptAccountsDetail.ReceiptAccountID,AR_ReceiptAccountsDetail.Amount, AR_ReceiptTransactionHeader.Amount, AR_ReceiptTransactionHeader.DocumentNo, 
            AR_ReceiptTransactionHeader.DocumentDate, SET_DocumentType.DocumentType, Comments, ReversalComments 

     -------- SALE DISCOUNT -----------
        
            union All
            SELECT distinct SET_DocumentType.DocumentType, SAL_DiscountTransactionHeader.CreateDate, 
            AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            SAL_DiscountTransactionHeader.DiscountTransactionHeaderID as SourceID,
            (SAL_DiscountTransactionHeader.CalculatedAmount * -1) as Amount, 
            '0' AS InstrumentNo, 
            '' AS Comments, 
            '' as InvoiceNumbers, '' as InvoiceDocumentNo, ISNULL(SAL_DiscountTransactionHeader.DiscountTransactionHeaderID, 0) AS DocumentNo, 
            SAL_DiscountTransactionHeader.RunDateTime AS DocumentDate, 0 AS FreightAmount, 0 As ForAlhajAmount 
        FROM SET_DocumentType 
            INNER JOIN SAL_DiscountTransactionHeader ON SET_DocumentType.CompanyID = SAL_DiscountTransactionHeader.CompanyID 
                AND SET_DocumentType.PeriodID = SAL_DiscountTransactionHeader.PeriodID 
                AND SET_DocumentType.DocumentTypeID = SAL_DiscountTransactionHeader.DocumentTypeID
            INNER JOIN AR_SET_Customer ON SAL_DiscountTransactionHeader.CustomerID = AR_SET_Customer.CustomerID 
                --AND SAL_DiscountTransactionHeader.CompanyID = AR_SET_Customer.CompanyID 
AND SAL_DiscountTransactionHeader.PeriodID = AR_SET_Customer.PeriodID 
                AND AR_SET_Customer.Status = 1  
				{1}
            INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
                --AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID 
AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
                  {0} 
            INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
            {7} 
        WHERE AR_SET_Customer.PeriodID = @PeriodID --AR_SET_Customer.CompanyID = @CompanyID AND 
        AND SAL_DiscountTransactionHeader.RunDateTime BETWEEN @FromDate AND @ToDate 

 union all
        
        ------------------------------------- KNON of Advance 
        SELECT distinct SET_DocumentType.DocumentType, AR_InvoiceHeader.CreateDate, 
            AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            GL_TransactionDetail.SerialNo as SourceID,
            (GL_TransactionDetail.Amount) as Amount, 
            '0' AS InstrumentNo, 
            '' AS Comments, 
            '' as InvoiceNumbers, '' as InvoiceDocumentNo, ISNULL(AR_InvoiceHeader.invoiceNumber, 0) AS DocumentNo, 
            AR_InvoiceHeader.invoiceDate AS DocumentDate, 0 AS FreightAmount, 0 As ForAlhajAmount 
            
 FROM SET_DocumentType 
                INNER JOIN AR_InvoiceHeader ON SET_DocumentType.CompanyID = AR_InvoiceHeader.CompanyID AND SET_DocumentType.PeriodID = AR_InvoiceHeader.PeriodID 
                    AND SET_DocumentType.DocumentTypeID = AR_InvoiceHeader.InvoiceType AND AR_InvoiceHeader.Status = 1 AND AR_InvoiceHeader.StatusCode = 1 
 INNER JOIN AR_SET_Customer ON AR_InvoiceHeader.VendorID = AR_SET_Customer.CustomerID 
                --AND AR_InvoiceHeader.CompanyID = AR_SET_Customer.CompanyID 
                AND AR_InvoiceHeader.PeriodID = AR_SET_Customer.PeriodID 
                AND AR_SET_Customer.Status = 1  
                {1} 
 inner join GL_TransactionHeader on GL_TransactionHeader.DocumentNo = AR_InvoiceHeader.InvoiceNumber 
 and GL_TransactionHeader.DocumentTypeID = AR_InvoiceHeader.InvoiceType
 INNER JOIN dbo.GL_TransactionDetail ON dbo.GL_TransactionHeader.TransactionID = dbo.GL_TransactionDetail.TransactionID AND 
							  dbo.GL_TransactionHeader.PeriodID = dbo.GL_TransactionDetail.PeriodID AND 
							  dbo.GL_TransactionHeader.CompanyID = dbo.GL_TransactionDetail.CompanyID
 
 AND (dbo.GL_TransactionDetail.CodeCombinationID In  (SELECT Distinct DefaultAccountCode FROM dbo.SET_DocumentType WHERE (DocumentType Like 'Advance Against PO%') AND (CompanyID = @CompanyID) AND (PeriodID = @PeriodID)))
                     AND 
							  (dbo.GL_TransactionDetail.TransactionType = 'Credit')
                    
	            LEFT JOIN AR_InvoiceDetail on AR_InvoiceDetail.CompanyID = AR_InvoiceHeader.CompanyID and AR_InvoiceDetail.PeriodID = AR_InvoiceHeader.PeriodID 
                    and AR_InvoiceDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID 
                LEFT JOIN AR_InvoiceSupplierDetail on AR_InvoiceSupplierDetail.CompanyID = AR_InvoiceHeader.CompanyID 
                    and AR_InvoiceSupplierDetail.PeriodID = AR_InvoiceHeader.PeriodID and AR_InvoiceSupplierDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID 
	            LEFT JOIN AR_SET_Customer ASCUS ON AR_InvoiceSupplierDetail.SupplierID = ASCUS.CustomerID 
--AND AR_InvoiceSupplierDetail.CompanyID = ASCUS.CompanyID 
                    AND AR_InvoiceSupplierDetail.PeriodID = ASCUS.PeriodID AND ASCUS.Status = 1 
	            left JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_InvoiceHeader.vENDORid 
                    AND AR_SET_BusinessPartnerTypes.CompanyID = AR_InvoiceHeader.CompanyID AND AR_SET_BusinessPartnerTypes.PeriodID = AR_InvoiceHeader.PeriodID 
                    {0}
	            LEFT JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
                 
	        WHERE AR_InvoiceHeader.CompanyID = @companyId AND AR_InvoiceHeader.PeriodID = @PeriodId AND AR_InvoiceHeader.InvoiceDate BETWEEN @FromDate AND @ToDate 
        {5}


union all
        
        ------------------------------------- KNON of Advance  cOPY OF 2
        SELECT distinct SET_DocumentType.DocumentType, AR_InvoiceHeader.CreateDate, 
            AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            GL_TransactionDetail.SerialNo + 1 as SourceID,
            (GL_TransactionDetail.Amount * -1) as Amount, 
            '0' AS InstrumentNo, 
            '' AS Comments, 
            '' as InvoiceNumbers, '' as InvoiceDocumentNo, ISNULL(AR_InvoiceHeader.invoiceNumber, 0) AS DocumentNo, 
            AR_InvoiceHeader.invoiceDate AS DocumentDate, 0 AS FreightAmount, 0 As ForAlhajAmount 
            
 FROM SET_DocumentType 
                INNER JOIN AR_InvoiceHeader ON SET_DocumentType.CompanyID = AR_InvoiceHeader.CompanyID AND SET_DocumentType.PeriodID = AR_InvoiceHeader.PeriodID 
                    AND SET_DocumentType.DocumentTypeID = AR_InvoiceHeader.InvoiceType AND AR_InvoiceHeader.Status = 1 AND AR_InvoiceHeader.StatusCode = 1 
 INNER JOIN AR_SET_Customer ON AR_InvoiceHeader.VendorID = AR_SET_Customer.CustomerID 
                --AND AR_InvoiceHeader.CompanyID = AR_SET_Customer.CompanyID 
                AND AR_InvoiceHeader.PeriodID = AR_SET_Customer.PeriodID 
                AND AR_SET_Customer.Status = 1  
                {1} 
 inner join GL_TransactionHeader on GL_TransactionHeader.DocumentNo = AR_InvoiceHeader.InvoiceNumber 
 and GL_TransactionHeader.DocumentTypeID = AR_InvoiceHeader.InvoiceType
 INNER JOIN dbo.GL_TransactionDetail ON dbo.GL_TransactionHeader.TransactionID = dbo.GL_TransactionDetail.TransactionID AND 
							  dbo.GL_TransactionHeader.PeriodID = dbo.GL_TransactionDetail.PeriodID AND 
							  dbo.GL_TransactionHeader.CompanyID = dbo.GL_TransactionDetail.CompanyID
 
 AND (dbo.GL_TransactionDetail.CodeCombinationID In  (SELECT Distinct DefaultAccountCode FROM dbo.SET_DocumentType WHERE (DocumentType Like 'Advance Against PO%') AND (CompanyID = @CompanyID) AND (PeriodID = @PeriodID)))
                     AND 
							  (dbo.GL_TransactionDetail.TransactionType = 'Credit')
                    
	            LEFT JOIN AR_InvoiceDetail on AR_InvoiceDetail.CompanyID = AR_InvoiceHeader.CompanyID and AR_InvoiceDetail.PeriodID = AR_InvoiceHeader.PeriodID 
                    and AR_InvoiceDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID 
                LEFT JOIN AR_InvoiceSupplierDetail on AR_InvoiceSupplierDetail.CompanyID = AR_InvoiceHeader.CompanyID 
                    and AR_InvoiceSupplierDetail.PeriodID = AR_InvoiceHeader.PeriodID and AR_InvoiceSupplierDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID 
	            LEFT JOIN AR_SET_Customer ASCUS ON AR_InvoiceSupplierDetail.SupplierID = ASCUS.CustomerID 
--AND AR_InvoiceSupplierDetail.CompanyID = ASCUS.CompanyID 
                    AND AR_InvoiceSupplierDetail.PeriodID = ASCUS.PeriodID AND ASCUS.Status = 1 
	            left JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_InvoiceHeader.vENDORid 
                    AND AR_SET_BusinessPartnerTypes.CompanyID = AR_InvoiceHeader.CompanyID AND AR_SET_BusinessPartnerTypes.PeriodID = AR_InvoiceHeader.PeriodID 
                    {0}
	            LEFT JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
                 
	        WHERE AR_InvoiceHeader.CompanyID = @companyId AND AR_InvoiceHeader.PeriodID = @PeriodId AND AR_InvoiceHeader.InvoiceDate BETWEEN @FromDate AND @ToDate 
        {5}

{10}

{11}

    ) AS FINTAB
    GROUP BY DocumentType, CreateDate, CustomerID, NAME, SourceID, InstrumentNo, Comments, InvoiceNumbers, InvoiceDocumentNo, DocumentNo, DocumentDate, FreightAmount, ForAlhajAmount 
) as TAb
-- For Transation type Filter
{2}
) as FinalTAB
where FinalTAB.Amount <> 0
Order By DocumentDate ";
                #endregion

                #region Additional Trade

                string additionalTrade = "";

                if (customerID.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@customerid", customerID);

                    additionalTrade = @"	        union all 
	        
	        --Start Trade Back
	        
	   SELECT distinct SET_DocumentType.DocumentType, GL_TransactionHeader.DocumentDate as CreateDate, 
            AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            GL_TransactionDetail.SerialNo + 1 as SourceID,
            (GL_TransactionDetail.Amount * -1) as Amount, 
            '0' AS InstrumentNo, 
            '' AS Comments, 
            '' as InvoiceNumbers, '' as InvoiceDocumentNo, ISNULL(INV_StockTransactionHeader.DocumentNo, 0) AS DocumentNo, 
            GL_TransactionHeader.DocumentDate AS DocumentDate, 
            0 AS FreightAmount, 
            0 As ForAlhajAmount 
            
 FROM GL_TransactionHeader 

INNER JOIN SET_DocumentType ON GL_TransactionHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID and SET_DocumentType.DocumentType like '%trade%'
INNER JOIN GL_TransactionDetail ON GL_TransactionDetail.TransactionID =GL_TransactionHeader.TransactionID and GL_TransactionDetail.TransactionType ='Credit'
INNER JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = GL_TransactionDetail.SourceHeaderID
INNER JOIN AR_SET_Customer ON INV_StockTransactionHeader.BusinessPartnerID = AR_SET_Customer.CustomerID 
 AND AR_Set_Customer.CustomerID  between @customerid AND  @customerid AND AR_SET_Customer.Status = 1 

	        WHERE GL_TransactionHeader.CompanyID = @companyId AND GL_TransactionHeader.PeriodID = @PeriodId AND GL_TransactionHeader.DocumentDate BETWEEN @FromDate AND @ToDate 
	        
	        --End Trade Back ";

                }

                #endregion

                #region Document Group



                if (documentGroupID.HasValue)
                {
                    qry_SalesInvoiceFilter += " AND Set_DocumentType.GroupID = " + documentGroupID;
                    qry_ReceiptFilter += " AND Set_DocumentType.GroupID = " + documentGroupID;// +" or  Set_DocumentType.DocumentType like '%Opening%'";
                    qry_PurchaseInvoiceFilter += " AND Set_DocumentType.GroupID = " + documentGroupID;// +" or  Set_DocumentType.DocumentType like '%Opening%'";
                    qry_PaymentFilter += " AND Set_DocumentType.GroupID = " + documentGroupID;// +" or  Set_DocumentType.DocumentType like '%Opening%'";
                }

                //if (documentGroupID.HasValue)
                //{
                //    qry_SalesInvoiceFilter += " AND Set_DocumentType.GroupID = " + documentGroupID ;
                //    qry_ReceiptFilter += " AND Set_DocumentType.GroupID = " + documentGroupID ;
                //    qry_PurchaseInvoiceFilter += " AND Set_DocumentType.GroupID = " + documentGroupID ;
                //    qry_PaymentFilter += " AND Set_DocumentType.GroupID = " + documentGroupID ;
                //}

                #endregion


                if (documentTypeAuthentication)
                {
                    sqlparams[5] = new SqlParameter("@UserID", userID);
                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                }

                #region Append Opening Query For Others Groups

                string appendingOpeningQueryForOthers = " ";

                if (documentGroupID != 1)
                {
                    appendingOpeningQueryForOthers = @" union all 
                    -----------Sale Invoice Header-----------  New Opening
        SELECT 'Opening' as DocumentType, SAL_InvoiceHeader.CreateDate, 
            AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            SAL_InvoiceHeader.InvoiceID as SourceID,
            case when SET_DocumentType.DocumentType = 'Credit Note' then SAL_InvoiceHeader.AmountPaid * -1 else
            SAL_InvoiceHeader.AmountPaid end as Amount,
    '0' AS InstrumentNo, '' AS Comments, '' as InvoiceNumbers, '' as InvoiceDocumentNo, 
            ISNULL(SAL_InvoiceHeader.InvoiceNumber, 0) AS DocumentNo, SAL_InvoiceHeader.InvoiceDate AS DocumentDate, 
            CASE WHEN GL_TransactionDetail.TransactionType = 'Debit' THEN ISNULL(SAL_InvoiceHeader.FreightAmount * -1, 0) else ISNULL(SAL_InvoiceHeader.FreightAmount, 0) 
                end as FreightAmount , 0 As ForAlhajAmount
        FROM SET_DocumentType 
            INNER JOIN SAL_InvoiceHeader ON SET_DocumentType.CompanyID = SAL_InvoiceHeader.CompanyID AND SET_DocumentType.PeriodID = SAL_InvoiceHeader.PeriodID 
                AND SET_DocumentType.DocumentTypeID = SAL_InvoiceHeader.InvoiceType 
            INNER JOIN AR_SET_Customer ON SAL_InvoiceHeader.CustomerID = AR_SET_Customer.CustomerID --AND SAL_InvoiceHeader.CompanyID = AR_SET_Customer.CompanyID 
                AND SAL_InvoiceHeader.PeriodID = AR_SET_Customer.PeriodID AND SAL_InvoiceHeader.Status = 1 AND SAL_InvoiceHeader.StatusCode = 1 
                AND AR_SET_Customer.Status = 1  
				  {customer_filter}
            INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
                --AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID 
AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
                 
            INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
                
            LEFT JOIN GL_TransactionDetail ON GL_TransactionDetail.CodeCombinationID = SAL_InvoiceHeader.FreightCode 
                AND GL_TransactionDetail.SourceHeaderID = SAL_InvoiceHeader.InvoiceID AND GL_TransactionDetail.CompanyID = SAL_InvoiceHeader.CompanyID 
                AND GL_TransactionDetail.PeriodID = SAL_InvoiceHeader.PeriodID 
        WHERE AR_SET_Customer.PeriodID = @PeriodID AND SAL_InvoiceHeader.InvoiceDate < @FromDate  --AR_SET_Customer.CompanyID = @CompanyID AND 
             and Set_DocumentType.DocumentType like '%opening%'
        GROUP BY SAL_InvoiceHeader.CreateDate, AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName, SAL_InvoiceHeader.InvoiceID, SAL_InvoiceHeader.AmountPaid, SAL_InvoiceHeader.InvoiceNumber, SAL_InvoiceHeader.InvoiceDate, 
            SET_DocumentType.DocumentType, GL_TransactionDetail.TransactionType, SAL_InvoiceHeader.FreightAmount 

";
                    if (CustomerID != null && CustomerID > 0)
                        appendingOpeningQueryForOthers = appendingOpeningQueryForOthers.Replace("{customer_filter}", " AND AR_Set_Customer.CustomerID " + businessPartnerIDs + " ");
                    else
                        appendingOpeningQueryForOthers = appendingOpeningQueryForOthers.Replace("{customer_filter}", "");

                }


                #endregion

                string query = string.Format(Query_GetMultipleBusinessPartnerStatus, businessPartnerTypeFilter, customerFilter, filter, qry_SalesInvoiceFilter,
                    qry_ReceiptFilter, qry_PurchaseInvoiceFilter, qry_PaymentFilter, qry_docuTypeAuthenticate, businessPartnerTypeFilter2, customerFilter2, additionalTrade, appendingOpeningQueryForOthers);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

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

        public DataTable GetMultipleBusinessPartnerAllStatusData(int companyID, int periodID, string businessPartnerType, string businessPartnerIDs, DateTime fromDate,
          DateTime toDate, string TransactionType, bool discardReversedDocuments, Guid userID, Nullable<short> documentGroupID = null,
          bool documentTypeAuthentication = false, int? customerID = null, int? StatusCode = null, int? BrokerID = null)
        {
            #region Function Variables

            DataTable dtToReturn = null;
            string qry_SalesInvoiceFilter = string.Empty, qry_ReceiptFilter = string.Empty, qry_PurchaseInvoiceFilter = string.Empty,
                qry_PaymentFilter = string.Empty, qry_docuTypeAuthenticate = string.Empty, businessPartnerTypeFilter = string.Empty,
                businessPartnerTypeFilter2 = string.Empty, customerFilter = string.Empty, customerFilter2 = string.Empty, filter = string.Empty, filter2 = string.Empty
                , StatusCodeFilter = string.Empty;


            #endregion

            if (StatusCode != null)
            {
                if (StatusCode != 1)
                    StatusCodeFilter = " and FinalTAB.StatusCode <> 1 ";
                else
                    StatusCodeFilter = " and FinalTAB.StatusCode =  " + StatusCode;
            }

            #region Filter Reversed Documents

            if (discardReversedDocuments)
            {
                qry_SalesInvoiceFilter = " And IsNull(Sal_InvoiceHeader.IsReversed, 0) = 0 And Sal_InvoiceHeader.ReversalID is null ";
                qry_ReceiptFilter = " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                qry_PurchaseInvoiceFilter = " And IsNull(AR_InvoiceHeader.IsReversed, 0) = 0 And AR_InvoiceHeader.ReversalID is null ";
                qry_PaymentFilter = " And AP_PaymentHeader.ReversalComments is null ";
            }

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);

                #region Filteration
                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                    businessPartnerTypeFilter = " AND AR_SET_BusinessPartnerTypes.TypeCode in (" + businessPartnerType + ") ";

                if (!string.IsNullOrEmpty(businessPartnerIDs) && !businessPartnerIDs.Equals("-1"))
                    customerFilter = " AND AR_Set_Customer.CustomerID " + businessPartnerIDs + " ";

                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                    businessPartnerTypeFilter2 = " AND TABInvoice.TypeCode in (" + businessPartnerType + ") ";

                if (!string.IsNullOrEmpty(businessPartnerIDs) && !businessPartnerIDs.Equals("-1"))
                    customerFilter2 = " AND TABInvoice.CustomerID " + businessPartnerIDs + " ";

                if (BrokerID.HasValue)
                {
                    customerFilter2 = " AND TABInvoice.CustomerID in (select CustomerID from AR_Set_Customer where  BrokerID = " + BrokerID + " )";
                    customerFilter = " AND AR_Set_Customer.BrokerID  = " + BrokerID + " ";
                }
                #region Filter by Transaction Type

                if (TransactionType == "Credit")
                    filter = " where TAb.Amount < 0";
                else if (TransactionType == "Debit")
                    filter = " where TAb.Amount > 0";


                if (TransactionType == "Credit")
                    filter2 = " where TABopening.Amount < 0";
                else if (TransactionType == "Debit")
                    filter2 = " where TABopening.Amount > 0";

                #endregion


                #endregion



                #region Additional Trade

                string additionalTrade = "";

                if (customerID.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@customerid", customerID);

                    additionalTrade = @"	        union all 
	        
	        --Start Trade Back
	        
	   SELECT distinct SET_DocumentType.DocumentType, GL_TransactionHeader.DocumentDate as CreateDate, 
            AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            GL_TransactionDetail.SerialNo + 1 as SourceID,
            (GL_TransactionDetail.Amount * -1) as Amount, 
            '0' AS InstrumentNo, 
            '' AS Comments, 
            '' as InvoiceNumbers, '' as InvoiceDocumentNo, ISNULL(INV_StockTransactionHeader.DocumentNo, 0) AS DocumentNo, 
            GL_TransactionHeader.DocumentDate AS DocumentDate, 
            0 AS FreightAmount, 
            0 As ForAlhajAmount 
            ,GL_TransactionHeader.StatusCode, '' AS FAX, '' AS SheetNo
 FROM GL_TransactionHeader 

INNER JOIN SET_DocumentType ON GL_TransactionHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID and SET_DocumentType.DocumentType like '%trade%'
And SET_DocumentType.Status= 1
INNER JOIN GL_TransactionDetail ON GL_TransactionDetail.TransactionID =GL_TransactionHeader.TransactionID 
and GL_TransactionDetail.TransactionType ='Credit'
And GL_TransactionDetail.Status= 1
INNER JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = GL_TransactionDetail.SourceHeaderID
And INV_StockTransactionHeader.status= 1
INNER JOIN AR_SET_Customer ON INV_StockTransactionHeader.BusinessPartnerID = AR_SET_Customer.CustomerID 
--- 
AND AR_Set_Customer.CustomerID  between @customerid AND  @customerid 
AND AR_SET_Customer.Status = 1 

	        WHERE GL_TransactionHeader.CompanyID = @companyId AND GL_TransactionHeader.PeriodID = @PeriodId 
            AND GL_TransactionHeader.DocumentDate BETWEEN @FromDate AND @ToDate 
            And GL_TransactionHeader.Status= 1
	        
	        --End Trade Back ";

                }

                #endregion

                #region Document Group



                if (documentGroupID.HasValue)
                {
                    qry_SalesInvoiceFilter += " AND Set_DocumentType.GroupID = " + documentGroupID;
                    qry_ReceiptFilter += " AND Set_DocumentType.GroupID = " + documentGroupID;// +" or  Set_DocumentType.DocumentType like '%Opening%'";
                    qry_PurchaseInvoiceFilter += " AND Set_DocumentType.GroupID = " + documentGroupID;// +" or  Set_DocumentType.DocumentType like '%Opening%'";
                    qry_PaymentFilter += " AND Set_DocumentType.GroupID = " + documentGroupID;// +" or  Set_DocumentType.DocumentType like '%Opening%'";
                }

                //if (documentGroupID.HasValue)
                //{
                //    qry_SalesInvoiceFilter += " AND Set_DocumentType.GroupID = " + documentGroupID ;
                //    qry_ReceiptFilter += " AND Set_DocumentType.GroupID = " + documentGroupID ;
                //    qry_PurchaseInvoiceFilter += " AND Set_DocumentType.GroupID = " + documentGroupID ;
                //    qry_PaymentFilter += " AND Set_DocumentType.GroupID = " + documentGroupID ;
                //}

                #endregion


                if (documentTypeAuthentication)
                {
                    sqlparams[5] = new SqlParameter("@UserID", userID);
                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                }

                #region Append Opening Query For Others Groups

                string appendingOpeningQueryForOthers = " ";

                if (documentGroupID != 1)
                {
                    appendingOpeningQueryForOthers = @" union all 
                    -----------Sale Invoice Header-----------  New Opening
        SELECT 'Opening' as DocumentType, SAL_InvoiceHeader.CreateDate, 
            AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            SAL_InvoiceHeader.InvoiceID as SourceID,
            case when SET_DocumentType.DocumentType = 'Credit Note' then SAL_InvoiceHeader.AmountPaid * -1 else
            SAL_InvoiceHeader.AmountPaid end as Amount,
    '0' AS InstrumentNo, '' AS Comments, '' as InvoiceNumbers, '' as InvoiceDocumentNo, 
            ISNULL(SAL_InvoiceHeader.InvoiceNumber, 0) AS DocumentNo, SAL_InvoiceHeader.InvoiceDate AS DocumentDate, 
            CASE WHEN GL_TransactionDetail.TransactionType = 'Debit' THEN ISNULL(SAL_InvoiceHeader.FreightAmount * -1, 0) else ISNULL(SAL_InvoiceHeader.FreightAmount, 0) 
                end as FreightAmount , 0 As ForAlhajAmount,SAL_InvoiceHeader.StatusCode, '' AS FAX, '' AS SheetNo
        FROM SET_DocumentType 
            INNER JOIN SAL_InvoiceHeader ON SET_DocumentType.CompanyID = SAL_InvoiceHeader.CompanyID AND SET_DocumentType.PeriodID = SAL_InvoiceHeader.PeriodID 
                AND SET_DocumentType.DocumentTypeID = SAL_InvoiceHeader.InvoiceType
                And SAL_InvoiceHeader.Status= 1 
            INNER JOIN AR_SET_Customer ON SAL_InvoiceHeader.CustomerID = AR_SET_Customer.CustomerID --AND SAL_InvoiceHeader.CompanyID = AR_SET_Customer.CompanyID 
                AND SAL_InvoiceHeader.PeriodID = AR_SET_Customer.PeriodID AND SAL_InvoiceHeader.Status = 1 --AND SAL_InvoiceHeader.StatusCode = 1 
                AND AR_SET_Customer.Status = 1 -- 
				  {customer_filter}
            INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
                --AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID 
AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
                 
            INNER JOIN AR_SET_BusinessPartner ON AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
                
            LEFT JOIN GL_TransactionDetail ON GL_TransactionDetail.CodeCombinationID = SAL_InvoiceHeader.FreightCode 
                AND GL_TransactionDetail.SourceHeaderID = SAL_InvoiceHeader.InvoiceID AND GL_TransactionDetail.CompanyID = SAL_InvoiceHeader.CompanyID 
                AND GL_TransactionDetail.PeriodID = SAL_InvoiceHeader.PeriodID
               AND GL_TransactionDetail.status= 1
        WHERE AR_SET_Customer.PeriodID = @PeriodID AND SAL_InvoiceHeader.InvoiceDate < @FromDate --AR_SET_Customer.CompanyID = @CompanyID AND 
             and Set_DocumentType.DocumentType like '%opening%'
        GROUP BY SAL_InvoiceHeader.CreateDate, AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName, SAL_InvoiceHeader.InvoiceID, SAL_InvoiceHeader.AmountPaid, SAL_InvoiceHeader.InvoiceNumber, SAL_InvoiceHeader.InvoiceDate, 
            SET_DocumentType.DocumentType, GL_TransactionDetail.TransactionType, SAL_InvoiceHeader.FreightAmount ,SAL_InvoiceHeader.StatusCode

";
                    if (CustomerID != null && CustomerID > 0)
                        appendingOpeningQueryForOthers = appendingOpeningQueryForOthers.Replace("{customer_filter}", " AND AR_Set_Customer.CustomerID " + businessPartnerIDs + " ");
                    else
                        appendingOpeningQueryForOthers = appendingOpeningQueryForOthers.Replace("{customer_filter}", "");

                }


                #endregion

                string query = string.Format(FnI.QueryResourceLibrary.Setup.BusinessPartnerType.Query_BusinessPartnerStatusAll, businessPartnerTypeFilter, customerFilter, filter, qry_SalesInvoiceFilter,
                    qry_ReceiptFilter, qry_PurchaseInvoiceFilter, qry_PaymentFilter, qry_docuTypeAuthenticate, businessPartnerTypeFilter2, customerFilter2, additionalTrade, appendingOpeningQueryForOthers, StatusCodeFilter);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

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


        public DataTable GetMultipleBusinessPartnerStatusAgingData(int companyID, int periodID, string businessPartnerIDs, DateTime fromDate,
          DateTime toDate, string TransactionType, bool discardReversedDocuments, Guid userID, Nullable<short> documentGroupID = null,
          bool documentTypeAuthentication = false, int? customerID = null)
        {
            #region Function Variables

            DataTable dtToReturn = null;
            string qry_SalesInvoiceFilter = string.Empty, qry_ReceiptFilter = string.Empty, qry_PurchaseInvoiceFilter = string.Empty,
                qry_PaymentFilter = string.Empty, qry_docuTypeAuthenticate = string.Empty, businessPartnerTypeFilter = string.Empty,
                businessPartnerTypeFilter2 = string.Empty, customerFilter = string.Empty, customerFilter2 = string.Empty, filter = string.Empty;

            #endregion

            #region Filter Reversed Documents

            if (discardReversedDocuments)
            {
                qry_SalesInvoiceFilter = " And IsNull(Sal_InvoiceHeader.IsReversed, 0) = 0 And Sal_InvoiceHeader.ReversalID is null ";
                qry_ReceiptFilter = " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                qry_PurchaseInvoiceFilter = " And IsNull(AR_InvoiceHeader.IsReversed, 0) = 0 And AR_InvoiceHeader.ReversalID is null ";
                qry_PaymentFilter = " And AP_PaymentHeader.ReversalComments is null ";
            }

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);


                //if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                //    businessPartnerTypeFilter = " AND AR_SET_BusinessPartnerTypes.TypeCode in (" + businessPartnerType + ") ";

                if (!string.IsNullOrEmpty(businessPartnerIDs) && !businessPartnerIDs.Equals("-1"))
                    customerFilter = " AND AR_Set_Customer.CustomerID " + businessPartnerIDs + " ";

                //if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                //    businessPartnerTypeFilter2 = " AND TABInvoice.TypeCode in (" + businessPartnerType + ") ";

                if (!string.IsNullOrEmpty(businessPartnerIDs) && !businessPartnerIDs.Equals("-1"))
                    customerFilter2 = " AND TABInvoice.CustomerID " + businessPartnerIDs + " ";

                #region Filter by Transaction Type

                if (TransactionType == "Credit")
                    filter = " where TAb.Amount < 0";
                else if (TransactionType == "Debit")
                    filter = " where TAb.Amount > 0";

                #endregion

                #region Query

                string Query_GetMultipleBusinessPartnerStatus = @FnI.QueryResourceLibrary.AR.AR_Set_Customer.Query_GetMultipleBusinessPartnerStatusAging;

                #endregion

                string additionalTrade = "";

                if (customerID.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@customerid", customerID);

                    additionalTrade = @"	        ";

                }


                if (documentGroupID.HasValue)
                {
                    qry_SalesInvoiceFilter += " AND Set_DocumentType.GroupID = " + documentGroupID;
                    qry_ReceiptFilter += " AND Set_DocumentType.GroupID = " + documentGroupID;
                    qry_PurchaseInvoiceFilter += " AND Set_DocumentType.GroupID = " + documentGroupID;
                    qry_PaymentFilter += " AND Set_DocumentType.GroupID = " + documentGroupID;
                }

                if (documentTypeAuthentication)
                {
                    sqlparams[5] = new SqlParameter("@UserID", userID);
                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                }




                string query = string.Format(Query_GetMultipleBusinessPartnerStatus, businessPartnerTypeFilter, customerFilter, filter, qry_SalesInvoiceFilter,
                    qry_ReceiptFilter, qry_PurchaseInvoiceFilter, qry_PaymentFilter, qry_docuTypeAuthenticate, businessPartnerTypeFilter2, customerFilter2, additionalTrade);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

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

        public static DataTable GetCurrentAccountOutStanding(int companyID, int periodID, string businessPartnerType, string businessPartnerIDs,
            DateTime fromDate, DateTime toDate, string TransactionType, string Query)
        {
            DataTable dtToReturn = null;
            StringBuilder filter = new StringBuilder();
            StringBuilder filter2 = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);

                if (!string.IsNullOrEmpty(businessPartnerIDs) && !businessPartnerIDs.Equals("-1"))
                    filter.Append(" AND AR_Set_Customer.CustomerID in (" + businessPartnerIDs + ") ");

                string query = string.Format(Query, filter.ToString(), filter2.ToString());

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

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

        public DataTable CustomerStatusData(int companyID, int periodID, string customerIDs, short customerTypeCode, DateTime dateFrom, DateTime dateTo)
        {
            DataTable dtToReturn = null;
            StringBuilder additionalfilter = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Fromdate", dateFrom);
                sqlparams[3] = new SqlParameter("@Dateto", dateTo);
                sqlparams[4] = new SqlParameter("@BPTypeCode", customerTypeCode);

                if (customerIDs != string.Empty && !customerIDs.Equals("-1"))
                    additionalfilter.Append(" AND AR_SET_Customer.CustomerID in (" + customerIDs + ") ");

                additionalfilter.Append(" AND AR_SET_BusinessPartnerTypes.TypeCode = @BPTypeCode ");

                #region Query


                #endregion

                string query = FnI.QueryResourceLibrary.AR.AR_Set_Customer.Query_GetCustomerStatusDetail.Replace("{Filter}", additionalfilter.ToString());

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

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

        public DataSet CustomerStatusProductWiseData(int companyID, int periodID, string customerIDs, short customerTypeCode, DateTime dateFrom, DateTime dateTo,
            int CatageoryID, int ItemID)
        {
            DataSet dtToReturn = null;
            StringBuilder additionalfilter = new StringBuilder();
            StringBuilder ForItemSearch = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Fromdate", dateFrom);
                sqlparams[3] = new SqlParameter("@Dateto", dateTo);
                sqlparams[4] = new SqlParameter("@BPTypeCode", customerTypeCode);

                if (CatageoryID > 0)
                    ForItemSearch.Append("AND [INV_SET_Category].CategoryID = " + CatageoryID + "");

                if (ItemID > 0)
                    ForItemSearch.Append("AND INV_Item.itemID = " + ItemID + "");

                if (customerIDs != string.Empty && !customerIDs.Equals("-1"))
                    additionalfilter.Append(" AND AR_SET_Customer.CustomerID in (" + customerIDs + ") ");

                additionalfilter.Append(" AND AR_SET_BusinessPartnerTypes.TypeCode = @BPTypeCode ");

                #region Query


                #endregion

                string query = FnI.QueryResourceLibrary.Setup.BusinessPartnerType.Query_GetCustomerStatusProdcutWiseDetail.Replace("{Filter}", additionalfilter.ToString());
                query = string.Format(query, ForItemSearch);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

                dtToReturn = ds;
                //    if (ds != null && ds.Tables.Count > 0)
                //      dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public DataTable CustomerDocumentStatusDetail(int companyID, int periodID, string BPartner, Nullable<int> customerTypeCode, Nullable<int> CustomerID,
            Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> documentTypePayment, Nullable<int> documentTypeReceipt)
        {
            StringBuilder sbPayment = new StringBuilder();
            StringBuilder sbReceipt = new StringBuilder();
            DataTable dt = null;
            string filter = "";

            try
            {
                #region Filter by CompanyID, PeriodID and BusinessPartnerID

                sbPayment.Append(" and EmployeePayment.CompanyID = " + companyID + " and  EmployeePayment.PeriodID =  " + periodID
                    + " and  EmployeePayment.CustomerID in" + BPartner);

                sbReceipt.Append(" and  EmployeeReceipt.CompanyID = " + companyID + " and  EmployeeReceipt.PeriodID =  " + periodID
                    + " and  EmployeeReceipt.CustomerID in " + BPartner);

                #endregion

                #region Filter by CreateDate

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    sbPayment.Append(" and Cast(EmployeePayment.CreateDate as Date) between '" + dateFrom.Value.ToString("MM-dd-yyyy") + "' and '"
                        + dateTo.Value.ToString("MM-dd-yyyy") + "'");

                    sbReceipt.Append(" and Cast(EmployeeReceipt.CreateDate as Date) between '" + dateFrom.Value.ToString("MM-dd-yyyy") + "' and '"
                        + dateTo.Value.ToString("MM-dd-yyyy") + "'");
                }
                else if (dateFrom.HasValue)
                {
                    sbPayment.Append(" and Cast(EmployeePayment.CreateDate as Date) >= '" + dateFrom.Value.ToString("MM-dd-yyyy") + "'");
                    sbReceipt.Append(" and Cast(EmployeeReceipt.CreateDate as Date) >= '" + dateFrom.Value.ToString("MM-dd-yyyy") + "'");
                }
                else if (dateTo.HasValue)
                {
                    sbPayment.Append(" and Cast(EmployeePayment.CreateDate as Date) <= '" + dateTo.Value.ToString("MM-dd-yyyy") + "'");
                    sbReceipt.Append(" and Cast(EmployeeReceipt.CreateDate as Date) <= '" + dateTo.Value.ToString("MM-dd-yyyy") + "'");
                }

                #endregion

                #region Filter by DocumentTypeID

                if (documentTypePayment.HasValue && documentTypeReceipt.HasValue)
                {
                    sbPayment.Append(" and PaymentHeader.DocumentTypeID =" + documentTypePayment.Value);
                    sbReceipt.Append(" and ReceiptHeader.DocumentTypeID =" + documentTypeReceipt.Value);
                }

                #endregion

                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                //if (customerTypeCode.HasValue)
                //    sqlparams[7] = new SqlParameter("@BPTypeCode", customerTypeCode.Value);
                //else
                //    sqlparams[7] = new SqlParameter("@BPTypeCode", DBNull.Value);


                if (BPartner != string.Empty)
                {
                    filter = " AND AR_SET_Customer.CustomerID in (" + BPartner + ") ";
                }
                //if (businessPartner == -1)
                //    filter = "";
                //else
                //    filter = "And AR_SET_Customer.CustomerID = @BPID or @BPID is null";

                string query = FnI.QueryResourceLibrary.Setup.BusinessPartnerType.Query_GetCustomerDocumentStatus.Replace("{Filter}", filter);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, sbPayment.ToString(), sbReceipt.ToString()), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable CustomerStatusDataSummary(int companyID, int periodID, short businessPartnerType, string businessPartnerID, DateTime fromDate,
            DateTime toDate)
        {
            DataTable dtToReturn = null;
            StringBuilder additionalfilter = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Fromdate", fromDate);
                sqlparams[3] = new SqlParameter("@Dateto", toDate);
                sqlparams[4] = new SqlParameter("@BPTypeCode", businessPartnerType);

                if (businessPartnerID != string.Empty && !businessPartnerID.Equals("-1"))
                    additionalfilter.Append(" AND AR_SET_Customer.CustomerID in (" + businessPartnerID + ") ");

                additionalfilter.Append(" AND AR_SET_BusinessPartnerTypes.TypeCode = @BPTypeCode ");

                #region Query

                string Query_GetCustomerStatusSummary = @"
select null As CreateDate, B As BusinessPartnerName, null As PaymentAmount, null As ReceiptAmount, null As InstrumentNo, null As Comments, null As Type, 
    null As DocumentNo, null As DocumentDate, sum(A) as sum 
from 
(
    Select isnull(Sum(SAL_InvoiceHeader.AmountPaid), 0) 
            + sum(isnull(Case When GL_TransactionDetail.TransactionType = 'Debit' Then FreightAmount * -1 Else FreightAmount End, 0)) As A, 
        AR_SET_Customer.CustomerName AS B 
    From AR_SET_Customer 
        Inner Join AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_Set_Customer.CustomerID 
            --And AR_SET_BusinessPartnerTypes.CompanyID = AR_Set_Customer.CompanyID 
And AR_SET_BusinessPartnerTypes.PeriodID = AR_Set_Customer.PeriodID 
        Inner Join AR_SET_BusinessPartner On AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
        left Join SAL_InvoiceHeader ON SAL_InvoiceHeader.CustomerID = AR_SET_Customer.CustomerID --And AR_SET_Customer.CompanyId = SAL_InvoiceHeader.CompanyId 
            And AR_SET_Customer.PeriodID = SAL_InvoiceHeader.PeriodID AND SAL_InvoiceHeader.Status = 1 And SAL_InvoiceHeader.StatusCode = 1 
        Left Join GL_TransactionHeader On GL_TransactionHeader.CompanyID = Sal_InvoiceHeader.CompanyID AND GL_TransactionHeader.PeriodID = Sal_InvoiceHeader.PeriodID 
            AND GL_TransactionHeader.DocumentTypeID = Sal_InvoiceHeader.InvoiceType AND GL_TransactionHeader.DocumentNo = Sal_InvoiceHeader.InvoiceNumber 
		Left Join GL_TransactionDetail On GL_TransactionHeader.CompanyID = GL_TransactionDetail.CompanyID 
            AND GL_TransactionHeader.PeriodID = GL_TransactionDetail.PeriodID AND GL_TransactionHeader.TransactionID = GL_TransactionDetail.TransactionID 
            AND Sal_InvoiceHeader.FreightCode = GL_TransactionDetail.CodeCombinationID 
    Where AR_SET_Customer.PeriodId = @PeriodId and AR_SET_Customer.status = 1 --AR_SET_Customer.CompanyID = @CompanyID and 
        and SAL_InvoiceHeader.InvoiceNumber is not null and dbo.SAL_InvoiceHeader.AmountPaid >= 0 AND SAL_InvoiceHeader.InvoiceDate <= @Dateto 
        {Filter} 
    group by AR_SET_Customer.CustomerName 
  
    Union All

    Select isnull(sum(AR_ReceiptTransactionHeader.Amount), 0) * -1 As A, AR_SET_Customer.CustomerName AS B 
    From AR_SET_Customer 
        Inner Join AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_Set_Customer.CustomerID 
            --And AR_SET_BusinessPartnerTypes.CompanyID = AR_Set_Customer.CompanyID 
And AR_SET_BusinessPartnerTypes.PeriodID = AR_Set_Customer.PeriodID 
        Inner Join AR_SET_BusinessPartner On AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
        left Join AR_ReceiptTransactionHeader ON AR_ReceiptTransactionHeader.CustomerID = AR_SET_Customer.CustomerID 
            --And AR_SET_Customer.CompanyId = AR_ReceiptTransactionHeader.CompanyId 
And AR_SET_Customer.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
            And AR_ReceiptTransactionHeader.Status = 1 And AR_ReceiptTransactionHeader.StatusCode = 1 
        Inner JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID 
            AND Set_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID AND Set_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
            AND Set_DocumentType.Status = 1 
        INNER JOIN ApplicationMenu ON ApplicationMenu.MenuID = Set_DocumentType.MenuID 
            AND ApplicationMenu.MenuPath in ('/AR/Transaction/Receipt_Transaction_frm.aspx', '/AR/Transaction/Partial_Receipt_frm.aspx') 
    Where AR_SET_Customer.PeriodId = @PeriodId and AR_SET_Customer.status = 1 --AR_SET_Customer.CompanyID = @CompanyID and 
        and AR_ReceiptTransactionHeader.DocumentNo is not null AND AR_ReceiptTransactionHeader.DocumentDate <= @Dateto 
        {Filter} 
    group by AR_Set_Customer.CustomerName
) tb 
group by B 
order by B Asc ";

                #endregion

                string query = Query_GetCustomerStatusSummary.Replace("{Filter}", additionalfilter.ToString());
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

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

        public static DataTable GetAllCustomers(string strWhere)
        {
            DataTable dt = null;
            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectAll_WithPaging + strWhere)).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }



        public static DataTable GetAllRetailers(string strWhere)
        {
            DataTable dt = null;
            try
            {
                string query = @"
                  SELECT distinct * FROM 
                AR_SET_Customer
                left join AR_SET_BUSINESSPARTNERTYPES on AR_SET_Customer.CustomerID = AR_SET_BUSINESSPARTNERTYPES.BusinessPartnerID
                left join AR_SET_BUSINESSPARTNER on AR_SET_BUSINESSPARTNER.BusinessPartnerID = AR_SET_BUSINESSPARTNERTYPES.TypeCode
                left join SET_Area on AR_SET_Customer.AreaID = set_area.AreaID
                where AR_SET_BUSINESSPARTNER.BusinessPartnerName like '%retailer%'";
                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query + strWhere)).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetCustomerWithArea(string strWhere)
        {
            DataTable dt = null;
            try
            {
                string Query_Select = @" select * from ar_set_customer,set_area where  ar_set_customer.areaid = set_area.areaid and ar_set_customer.status=1 ";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_Select + strWhere)).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataRow GETAccountCodeofEmployee(int PersonID, int CompanyID, string query = "", SqlTransaction sqltrans = null)
        {
            DataRow dr = null;
            try
            {
                DataTable dt;
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PersonID", PersonID);
                if (sqltrans == null)
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, query, sqlparams).Tables[0];

                dr = dt.Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }

        public static DataTable GetCustomerNameByID(int CustomerID, int CompanyID, int PeriodID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@CustomerID", CustomerID);

                string Query_GetCustomerNameByID = @" 
--Select CustomerID, CustomerName from AR_SET_Customer 
--where AR_SET_Customer.CompanyID = @CompanyID and AR_SET_Customer.PeriodID = @PeriodID and Status = 1 and CustomerID = @CustomerID


SELECT PersonID as CustomerID,FullName as CustomerName FROM [HRMS_Set_Employee]  inner join SET_Department on 
SET_Department.DepartmentID=HRMS_Set_Employee.FK_DepartmentID
left join SET_JobDesignation on SET_JobDesignation.JobDesignationID  =  HRMS_Set_Employee.FK_JobDesignationID
WHERE HRMS_Set_Employee.Status = 1 AND ([HRMS_Set_Employee].[CompanyID]=@CompanyID OR [HRMS_Set_Employee].[CompanyID] is null)
and PersonID=@CustomerID

";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetCustomerNameByID, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetCustomerDetailByID(int CustomerID, int CompanyID, int PeriodID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@CustomerID", CustomerID);

                string Query_GetCustomerDetailByID = @" 
SELECT * FROM AR_SET_Customer 
WHERE Status = 1 AND CustomerID = @CustomerID and CompanyID = @CompanyID and PeriodID = @PeriodID ";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetCustomerDetailByID, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAR_SET_BusinessPartnerTypesData(int CustomerID, int CompanyID, int PeriodID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@CustomerID", CustomerID);

                string Query_GetBusinessPartnerByID = @" 
SELECT * FROM AR_SET_BusinessPartnerTypes 
WHERE BusinessPartnerID = @CustomerID and CompanyID = @CompanyID and PeriodID = @PeriodID ";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetBusinessPartnerByID, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetCustomerNameByIDFromCustomerTable(int CustomerID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CustomerID", CustomerID);

                string Query_GetCustomerNameByIDFromCustomerTable = @" 
SELECT AR_SET_Customer.* FROM AR_SET_Customer
WHERE AR_SET_Customer.CustomerID = @CustomerID and AR_SET_Customer.Status = 1

";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetCustomerNameByIDFromCustomerTable, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetCustomeIDByName(string CustomerName, int CompanyID, int PeriodID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@CustomerName", CustomerName);

                string Query_GetCustomerIDByName = @" 
Select CustomerID, CustomerName from AR_SET_Customer 
where AR_SET_Customer.PeriodID = @PeriodID and Status = 1 and CustomerName like @CustomerName --AR_SET_Customer.CompanyID = @CompanyID and 
";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetCustomerIDByName, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable GetNTNSTRNByCustomerID(int CompanyID, int PeriodID, int CustomerID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@CustomerID", CustomerID);

                string Query_GetNTNSTRNByCustomerID = @" 
SELECT ISNULL(ServiceItemTaxRate, 0) as ServiceItemTaxRate, ISNULL(SuppliesTaxRate, 0) as SuppliesTaxRate, ISNULL(TransporterTaxRate, 0) as TransporterTaxRate, 
    ISNULL(AdditionalDeductionTaxRate, 0) as AdditionalDeductionTaxRate, ISNULL(ServiceItemTaxExempted, 0) ServiceItemTaxExempted, 
    ISNULL(SuppliesTaxExempted, 0) SuppliesTaxExempted, ISNULL(TransporterTaxExempted, 0) TransporterTaxExempted, 
    ISNULL(AdditionalDeductionTaxExempted, 0) AdditionalDeductionTaxExempted, STRNNumber, NTNNumber 
FROM AR_SET_Customer 
WHERE CompanyID = @CompanyID and PeriodID = @PeriodID and CustomerID = @CustomerID ";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetNTNSTRNByCustomerID, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllActiveData(Nullable<int> companyID, Nullable<int> periodID, SqlTransaction sqlTrans, Nullable<bool> isActive = true)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (isActive.HasValue)
                    sqlparams[2] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[2] = new SqlParameter("@IsActive", DBNull.Value);

                if (sqlTrans == null)
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll, sqlparams).Tables[0];
                //ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_MaxId, sqlparams));
                else
                    dt = SqlHelper.ExecuteDataset(sqlTrans, CommandType.Text, Query_SelectAll, sqlparams).Tables[0];
                //ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Get_MaxId, sqlparams));

                //dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable GetDealerForDTRLookup(int companyID, int periodID, string Where)
        {
            DataTable dtPaymentReceipt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                #region Query

                string Query_GetDealerForDTRLookup = @" Select * From 
(
    SELECT DISTINCT AS_ServicePerformanceDetail.currentStatus, AS_ServicePerformanceHeader.ServicePerformanceID, ISNULL(AS_ServicePerformanceHeader.DTRID, 0) DTRID, 
        AR_SET_Customer.CustomerName AS DealerName, AS_ServicePerformanceHeader.ServiceNo, SAL_DOVehicleInformation.Chassis AS ChassisNo, 
        Case Set_DocumentType.DocumentType WHEN 'Dealer Technical Review' then 'DTR' ELSE 'W/C' END as DocumentType 
    FROM AS_ServicePerformanceHeader 
        INNER JOIN AS_ServicePerformanceDetail ON AS_ServicePerformanceDetail.ServicePerformanceID = AS_ServicePerformanceHeader.ServicePerformanceID 
            AND AS_ServicePerformanceDetail.CompanyID = AS_ServicePerformanceHeader.CompanyID 
            AND AS_ServicePerformanceDetail.PeriodID = AS_ServicePerformanceHeader.PeriodID 
        INNER JOIN SAL_DOVehicleInformation ON SAL_DOVehicleInformation.DOVehicleInformationID = AS_ServicePerformanceHeader.VehicleID 
            AND SAL_DOVehicleInformation.CompanyID = AS_ServicePerformanceHeader.CompanyID 
            AND SAL_DOVehicleInformation.PeriodID = AS_ServicePerformanceHeader.PeriodID 
        INNER JOIN AR_SET_Customer ON AR_SET_Customer.CustomerID = AS_ServicePerformanceHeader.DealerID 
            --AND AR_SET_Customer.CompanyID = AS_ServicePerformanceHeader.CompanyID 
AND AR_SET_Customer.PeriodID = AS_ServicePerformanceHeader.PeriodID 
        JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = AS_ServicePerformanceHeader.DocumentTypeID 
            AND Set_DocumentType.CompanyID = AS_ServicePerformanceHeader.CompanyID AND Set_DocumentType.PeriodID = AS_ServicePerformanceHeader.PeriodID 
    WHERE Set_DocumentType.DocumentType LIKE ('Dealer Technical Review') OR Set_DocumentType.DocumentType LIKE ('%WARRANTY%') 
        AND AS_ServicePerformanceHeader.CompanyID = @CompanyID AND AS_ServicePerformanceHeader.PeriodID = @PeriodID
)tab 
Where tab.ServicePerformanceID not in 
    (
        SELECT DISTINCT ISNULL(AS_ServicePerformanceHeader.DTRID, 0) DTRID FROM AS_ServicePerformanceHeader 
            INNER JOIN AS_ServicePerformanceDetail ON AS_ServicePerformanceDetail.ServicePerformanceID = AS_ServicePerformanceHeader.ServicePerformanceID 
                AND AS_ServicePerformanceDetail.CompanyID = AS_ServicePerformanceHeader.CompanyID 
                AND AS_ServicePerformanceDetail.PeriodID = AS_ServicePerformanceHeader.PeriodID 
            INNER JOIN SAL_DOVehicleInformation ON SAL_DOVehicleInformation.DOVehicleInformationID = AS_ServicePerformanceHeader.VehicleID 
                AND SAL_DOVehicleInformation.CompanyID = AS_ServicePerformanceHeader.CompanyID 
                AND SAL_DOVehicleInformation.PeriodID = AS_ServicePerformanceHeader.PeriodID 
            INNER JOIN AR_SET_Customer ON AR_SET_Customer.CustomerID = AS_ServicePerformanceHeader.DealerID 
                --AND AR_SET_Customer.CompanyID = AS_ServicePerformanceHeader.CompanyID 
AND AR_SET_Customer.PeriodID = AS_ServicePerformanceHeader.PeriodID 
            JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = AS_ServicePerformanceHeader.DocumentTypeID 
                AND Set_DocumentType.CompanyID = AS_ServicePerformanceHeader.CompanyID AND Set_DocumentType.PeriodID = AS_ServicePerformanceHeader.PeriodID 
        WHERE Set_DocumentType.DocumentType LIKE ('Dealer Technical Review') OR Set_DocumentType.DocumentType LIKE ('%WARRANTY%') 
AND AS_ServicePerformanceHeader.CompanyID = @CompanyID 
    AND AS_ServicePerformanceHeader.PeriodID = @PeriodID)
{0}";

                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDealerForDTRLookup, Where), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtPaymentReceipt = ds.Tables[0];
            }
            catch (Exception exp)
            {
                SqlHelper.LogException(exp);
                throw;
            }

            return dtPaymentReceipt;
        }

        public DataTable GetCustomerNameList(string customerName)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CustomerName", customerName);

                string Query_GetCustomerNameList = @" select [CustomerName] from AR_SET_Customer where [CustomerName] like @CustomerName + '%' ";
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetCustomerNameList, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public int GetNewCustomerIDWithMinMaxLimit(int companyID, int periodID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                string Query_Get_MaxId_WithMinMaxLimit = @" SELECT IsNull(max(CustomerID), 0) + 1 as CustomerID FROM AR_SET_Customer 
WHERE PeriodID = @PeriodID 
AND CustomerID
 Between IsNull((Select FieldValue1 From Set_Parameter Where FieldName = 'BusinessPartnerIDsRange'), 1) 
    AND IsNull((Select FieldValue2 From Set_Parameter Where FieldName = 'BusinessPartnerIDsRange'), 5000000) ";

                if (objTransaction != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Get_MaxId_WithMinMaxLimit, sqlparams));
                else if (objConnection != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_Get_MaxId_WithMinMaxLimit, sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_MaxId_WithMinMaxLimit, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public DataSet Get_CountOfTransactionPassed(int companyID, int periodID, int customerID, SqlConnection objConnection = null)
        {
            DataSet dsToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@BusinessPartnerID", customerID);

                string Query_Get_CountOfTransactionPassed = @"
Select Count(*) As TransactionCount From SAL_DeliveryOrderHeader Where PeriodID = @PeriodID And CustomerID = @BusinessPartnerID;
Select Count(*) As TransactionCount From INV_StockTransactionHeader Where PeriodID = @PeriodID And BusinessPartnerID = @BusinessPartnerID;
Select Count(*) As TransactionCount From AP_PaymentHeader Where PeriodID = @PeriodID And VendorID = @BusinessPartnerID;
Select Count(*) As TransactionCount From AR_ReceiptTransactionHeader Where PeriodID = @PeriodID And CustomerID = @BusinessPartnerID;
Select Count(*) As TransactionCount From AR_ReceiptCustomerDetail Where PeriodID = @PeriodID And CustomerID = @BusinessPartnerID;
Select Count(*) As TransactionCount From AP_PurchaseOrderHeader Where PeriodID = @PeriodID And VendorID = @BusinessPartnerID;
Select Count(*) As TransactionCount From AR_InvoiceHeader Where PeriodID = @PeriodID And VendorID = @BusinessPartnerID;
Select Count(*) As TransactionCount From SAL_OrderHeader Where PeriodID = @PeriodID And CustomerID = @BusinessPartnerID;
Select Count(*) As TransactionCount From Set_FinancialDetail Where BusinessPartnerID = @BusinessPartnerID;
Select Count(*) As TransactionCount From SAL_DispatchHeader Where PeriodID = @PeriodID And CustomerID = @BusinessPartnerID;
Select Count(*) As TransactionCount From SAL_InvoiceHeader Where PeriodID = @PeriodID And CustomerID = @BusinessPartnerID; 
Select Count(*) As TransactionCount From AR_SaleTransactionHeader Where PeriodID = @PeriodID And BillToCustomerID = @BusinessPartnerID;
Select Count(*) As TransactionCount From AP_PaymentSupplierDetail Where PeriodID = @PeriodID And SupplierID = @BusinessPartnerID;
Select Count(*) As TransactionCount From PUR_EnquirySupplier Where PeriodID <= @PeriodID And SupplierID = @BusinessPartnerID; ";

                if (objConnection != null)
                    dsToReturn = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_Get_CountOfTransactionPassed, sqlparams);
                else
                    dsToReturn = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_CountOfTransactionPassed, sqlparams);
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        #endregion

        public static DataTable GetCustomerAddressesbyID(int CompanyID, int PeriodID, int CustomerID)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@BusinessPartnerID", CustomerID);
                #region Query
                string Query_getCustomerAddress = @"

select t.Address from (
		  select addressline1 as Address  from AR_SET_Customer where CompanyID=@CompanyID AND PeriodID=@PeriodID and CustomerID =@BusinessPartnerID
          union
          select addressline2 as Address  from AR_SET_Customer where CompanyID=@CompanyID AND PeriodID=@PeriodID and CustomerID =@BusinessPartnerID
          union
          select AddressLine3 as Address  from AR_SET_Customer where CompanyID=@CompanyID AND PeriodID=@PeriodID and CustomerID =@BusinessPartnerID
          union
          select addressline4 as Address  from AR_SET_Customer where  CompanyID=@CompanyID AND PeriodID=@PeriodID and CustomerID =@BusinessPartnerID)t
          where t.Address is not null

";
                #endregion
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_getCustomerAddress, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllData(Nullable<int> companyID, Nullable<int> periodID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                #region Query
                string Query_SelectAll = @"
SELECT * FROM [AR_SET_Customer] WHERE [Status] = 1 AND ([CompanyID] = @CompanyID OR @CompanyID is null) AND ([PeriodID] = @PeriodID OR @PeriodID is null)
";
                #endregion

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable GetExpenseAndSalary(string companyID, int periodID, string businessPartnerType, string businessPartnerIDs, DateTime fromDate,
           DateTime toDate, string TransactionType, bool discardReversedDocuments, Guid userID, string documentGroupID = "", Nullable<int> DocumentTypeiD = null,
           bool documentTypeAuthentication = false, int? customerID = null, int? CustomerCategoryID = null, string VendorPayment = "", int LocationID = 0, int CostCenterID = 0)
        {
            #region Function Variables

            DataTable dtToReturn = null;
            string qry_SalesInvoiceFilter = string.Empty, qry_ReceiptFilter = string.Empty, qry_PurchaseInvoiceFilter = string.Empty,
                qry_PaymentFilter = string.Empty, qry_docuTypeAuthenticate = string.Empty, businessPartnerTypeFilter = string.Empty,
                businessPartnerTypeFilter2 = string.Empty, customerFilter = string.Empty, customerFilter2 = string.Empty, filter = string.Empty, LocationFillter = string.Empty;
            StringBuilder sbCC = new StringBuilder();
            StringBuilder sbCC2 = new StringBuilder();
            StringBuilder sbCC3 = new StringBuilder();

            #endregion

            #region Filter Reversed Documents

            if (discardReversedDocuments)
            {
                qry_SalesInvoiceFilter = " And IsNull(Sal_InvoiceHeader.IsReversed, 0) = 0 And Sal_InvoiceHeader.ReversalID is null ";
                qry_ReceiptFilter = " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                qry_PurchaseInvoiceFilter = " And IsNull(AR_InvoiceHeader.IsReversed, 0) = 0 And AR_InvoiceHeader.ReversalID is null ";
                qry_PaymentFilter = " And AP_PaymentHeader.ReversalComments is null ";
            }

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);

                if (DocumentTypeiD.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@DocumentTypeiD", DocumentTypeiD);
                }
                else
                {
                    sqlparams[4] = new SqlParameter("@DocumentTypeiD", DBNull.Value);
                }

                #region Filteration
                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                    businessPartnerTypeFilter = " AND AR_SET_BusinessPartnerTypes.TypeCode in (" + businessPartnerType + ") ";

                if (!string.IsNullOrEmpty(businessPartnerIDs) && !businessPartnerIDs.Equals("-1"))
                    customerFilter = " AND AR_Set_Customer.CustomerID " + businessPartnerIDs + " ";

                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                    businessPartnerTypeFilter2 = " AND TABInvoice.TypeCode in (" + businessPartnerType + ") ";

                if (!string.IsNullOrEmpty(businessPartnerIDs) && !businessPartnerIDs.Equals("-1"))
                    customerFilter2 = " AND TABInvoice.CustomerID " + businessPartnerIDs + " ";

                #region Filter by Transaction Type

                if (TransactionType == "Credit")
                    filter = " where TAb.Amount < 0";
                else if (TransactionType == "Debit")
                    filter = " where TAb.Amount > 0";
                #endregion


                #endregion

                if (CustomerCategoryID.HasValue)
                {
                    sbCC.Append(" left join AR_SET_Customer as CusCat on CusCat.CustomerID = FinalTAB.CustomerID ");
                    sbCC2.Append("and CusCat.CustomerCategoryID = " + CustomerCategoryID.ToString() + "");
                }
                if (!string.IsNullOrEmpty(VendorPayment))
                {
                    if (VendorPayment == "Positive")
                    {
                        sbCC3.Append(", 'Positive' as VendorPayment");
                    }
                    if (VendorPayment == "Negative")
                    {
                        sbCC3.Append(", 'Negative' as VendorPayment");
                        //sbCC3.Append(" And  FinalTAB.Amount < 0");
                    }
                }
                else
                {
                    sbCC3.Append(", '' as VendorPayment");
                }
                #region Query Business Partner Status

                ///string Query_GetMultipleBusinessPartnerStatus = FnI.QueryResourceLibrary.Setup.BusinessPartnerType.Query_MISBusinessPartnerReport;
                //   string Query_GetMultipleBusinessPartnerStatus = FnI.QueryResourceLibrary.Setup.BusinessPartnerType.Query_MISBusinessPartnerReport_New;

                string Query_GetExpenseAndSalary = FnI.QueryResourceLibrary.Setup.BusinessPartnerType2.Query_ExpensesAndSalary;

                #endregion

                #region Additional Trade

                string additionalTrade = "";

                if (customerID.HasValue)
                {
                    sqlparams[5] = new SqlParameter("@customerid", customerID);

                    additionalTrade = @"	        union all 
	        
	        --Start Trade Back
	        
	   SELECT distinct SET_DocumentType.DocumentType, GL_TransactionHeader.DocumentDate as CreateDate, GL_TransactionHeader.CompanyID,
            AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            GL_TransactionDetail.SerialNo + 1 as SourceID,
            (GL_TransactionDetail.Amount * -1) as Amount, 
            '0' AS InstrumentNo, 
            '' AS Comments, 
            '' AS DetailNarration,
            '' as InvoiceNumbers, '' as InvoiceDocumentNo, ISNULL(INV_StockTransactionHeader.DocumentNo, 0) AS DocumentNo, 
            GL_TransactionHeader.DocumentDate AS DocumentDate, 
            0 AS FreightAmount, 
            0 As ForAlhajAmount 
            ,'' as BPType,'' as GRN_NO,'' as PO_NO, '' AS SheetNo, '' AS FAX
 FROM GL_TransactionHeader 

INNER JOIN SET_DocumentType ON GL_TransactionHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID and SET_DocumentType.DocumentType like '%trade%'
INNER JOIN GL_TransactionDetail ON GL_TransactionDetail.TransactionID =GL_TransactionHeader.TransactionID and GL_TransactionDetail.TransactionType ='Credit'
INNER JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = GL_TransactionDetail.SourceHeaderID
INNER JOIN AR_SET_Customer ON INV_StockTransactionHeader.BusinessPartnerID = AR_SET_Customer.CustomerID 
AND AR_Set_Customer.CustomerID  between @customerid AND  @customerid AND AR_SET_Customer.Status = 1 

	        WHERE GL_TransactionHeader.CompanyID in @CompanyID AND GL_TransactionHeader.PeriodID = @PeriodId AND GL_TransactionHeader.DocumentDate BETWEEN @FromDate AND @ToDate 
	        
	        --End Trade Back ";

                }

                #endregion

                #region Document Group new group
                if (documentGroupID != "")
                {
                    qry_SalesInvoiceFilter += " AND Set_DocumentType.GroupID in " + documentGroupID;
                    qry_ReceiptFilter += " AND Set_DocumentType.GroupID in " + documentGroupID;// +" or  Set_DocumentType.DocumentType like '%Opening%'";
                    qry_PurchaseInvoiceFilter += " AND Set_DocumentType.GroupID in " + documentGroupID;// +" or  Set_DocumentType.DocumentType like '%Opening%'";
                    qry_PaymentFilter += " AND Set_DocumentType.GroupID in " + documentGroupID;// +" or  Set_DocumentType.DocumentType like '%Opening%'";
                }

                //if (documentGroupID.HasValue)
                //{
                //    qry_SalesInvoiceFilter += " AND Set_DocumentType.GroupID = " + documentGroupID ;
                //    qry_ReceiptFilter += " AND Set_DocumentType.GroupID = " + documentGroupID ;
                //    qry_PurchaseInvoiceFilter += " AND Set_DocumentType.GroupID = " + documentGroupID ;
                //    qry_PaymentFilter += " AND Set_DocumentType.GroupID = " + documentGroupID ;
                //}

                #endregion


                if (documentTypeAuthentication && (documentGroupID != ""))
                {
                    sqlparams[6] = new SqlParameter("@UserID", userID);
                    //                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
                    //AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID    ";
                }

                #region Append Opening Query For Others Groups

                string appendingOpeningQueryForOthers = " ";


                #endregion

                #region Location/CostCenter Region

                if (LocationID > 0)
                    LocationFillter = "and GL_TransactionHeader.LocationID = " + LocationID + "";

                #endregion



                string query = string.Format(Query_GetExpenseAndSalary, businessPartnerTypeFilter, customerFilter, filter, qry_SalesInvoiceFilter,
                    qry_ReceiptFilter, qry_PurchaseInvoiceFilter, qry_PaymentFilter, qry_docuTypeAuthenticate, businessPartnerTypeFilter2, customerFilter2, additionalTrade, appendingOpeningQueryForOthers, LocationFillter, sbCC.ToString(), sbCC2.ToString(), sbCC3.ToString());

                companyID = "(" + companyID + ")";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query.ToString().Replace("@CompanyID", companyID), sqlparams);

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


        public DataTable GetMISBusinessPartner(string companyID, int periodID, string businessPartnerType, string businessPartnerIDs, DateTime fromDate,
           DateTime toDate, string TransactionType, bool discardReversedDocuments, Guid userID, string documentGroupID = "", Nullable<int> DocumentTypeiD = null,
           bool documentTypeAuthentication = false, int? customerID = null, int? CustomerCategoryID = null, string VendorPayment = "", int LocationID = 0, int CostCenterID = 0
            , bool IsSummary = false)
        {
            #region Function Variables

            DataTable dtToReturn = null;
            string qry_SalesInvoiceFilter = string.Empty, qry_ReceiptFilter = string.Empty, qry_PurchaseInvoiceFilter = string.Empty,
                qry_PaymentFilter = string.Empty, qry_docuTypeAuthenticate = string.Empty, businessPartnerTypeFilter = string.Empty,
                businessPartnerTypeFilter2 = string.Empty, customerFilter = string.Empty, customerFilter2 = string.Empty, filter = string.Empty, LocationFillter = string.Empty;
            StringBuilder sbCC = new StringBuilder();
            StringBuilder sbCC2 = new StringBuilder();
            StringBuilder sbCC3 = new StringBuilder();
            
            #endregion

            #region Filter Reversed Documents

            if (discardReversedDocuments)
            {
                qry_SalesInvoiceFilter = " And IsNull(Sal_InvoiceHeader.IsReversed, 0) = 0 And Sal_InvoiceHeader.ReversalID is null ";
                qry_ReceiptFilter = " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                qry_PurchaseInvoiceFilter = " And IsNull(AR_InvoiceHeader.IsReversed, 0) = 0 And AR_InvoiceHeader.ReversalID is null ";
                qry_PaymentFilter = " And AP_PaymentHeader.ReversalComments is null ";
            }

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);

                if (DocumentTypeiD.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@DocumentTypeiD", DocumentTypeiD);
                }
                else
                {
                    sqlparams[4] = new SqlParameter("@DocumentTypeiD", DBNull.Value);
                }

                #region Filteration
                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                    businessPartnerTypeFilter = " AND AR_SET_BusinessPartnerTypes.TypeCode in (" + businessPartnerType + ") ";

                if (!string.IsNullOrEmpty(businessPartnerIDs) && !businessPartnerIDs.Equals("-1"))
                    customerFilter = " AND AR_Set_Customer.CustomerID " + businessPartnerIDs + " ";

                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                    businessPartnerTypeFilter2 = " AND TABInvoice.TypeCode in (" + businessPartnerType + ") ";

                if (!string.IsNullOrEmpty(businessPartnerIDs) && !businessPartnerIDs.Equals("-1"))
                    customerFilter2 = " AND TABInvoice.CustomerID " + businessPartnerIDs + " ";

                #region Filter by Transaction Type

                if (TransactionType == "Credit")
                    filter = " where TAb.Amount < 0";
                else if (TransactionType == "Debit")
                    filter = " where TAb.Amount > 0";
                #endregion


                #endregion

                if (CustomerCategoryID.HasValue)
                {
                    sbCC.Append(" left join AR_SET_Customer as CusCat on CusCat.CustomerID = FinalTAB.CustomerID ");
                    sbCC2.Append("and CusCat.CustomerCategoryID = " + CustomerCategoryID.ToString() + "");
                }
                if (!string.IsNullOrEmpty(VendorPayment))
                {
                    if (VendorPayment == "Positive")
                    {
                        sbCC3.Append(", 'Positive' as VendorPayment");
                    }
                    if (VendorPayment == "Negative")
                    {
                        sbCC3.Append(", 'Negative' as VendorPayment");
                        //sbCC3.Append(" And  FinalTAB.Amount < 0");
                    }
                }
                else
                {
                    sbCC3.Append(", '' as VendorPayment");
                }
                #region Query Business Partner Status

                ///string Query_GetMultipleBusinessPartnerStatus = FnI.QueryResourceLibrary.Setup.BusinessPartnerType.Query_MISBusinessPartnerReport;
                //   string Query_GetMultipleBusinessPartnerStatus = FnI.QueryResourceLibrary.Setup.BusinessPartnerType.Query_MISBusinessPartnerReport_New;

                string Query_GetMultipleBusinessPartnerStatus = FnI.QueryResourceLibrary.Setup.BusinessPartnerType2.Query_MISBusinessPartnerReport_One;

                string Query_GetMultipleBusinessPartnerStatusFiscal = FnI.QueryResourceLibrary.Setup.BusinessPartnerType.Query_MISBusinessPartnerReport_Fiscal;

                if(IsSummary)
                    Query_GetMultipleBusinessPartnerStatus = FnI.QueryResourceLibrary.Setup.BusinessPartnerType2.Query_MISBusinessPartnerReport_OneSummary;


                #endregion

                #region Additional Trade

                string additionalTrade = "";

                if (customerID.HasValue)
                {
                    sqlparams[5] = new SqlParameter("@customerid", customerID);

                    additionalTrade = @"	        union all 
	        
	        --Start Trade Back
	        
	   SELECT distinct SET_DocumentType.DocumentType, GL_TransactionHeader.DocumentDate as CreateDate, GL_TransactionHeader.CompanyID,
            AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            GL_TransactionDetail.SerialNo + 1 as SourceID,
            (GL_TransactionDetail.Amount * -1) as Amount, 
            '0' AS InstrumentNo, 
            '' AS Comments, 
            '' AS DetailNarration,
            '' as InvoiceNumbers, '' as InvoiceDocumentNo, ISNULL(INV_StockTransactionHeader.DocumentNo, 0) AS DocumentNo, 
            GL_TransactionHeader.DocumentDate AS DocumentDate, 
            0 AS FreightAmount, 
            0 As ForAlhajAmount 
            ,'' as BPType,'' as GRN_NO,'' as PO_NO, '' AS SheetNo, '' AS FAX
 FROM GL_TransactionHeader 

INNER JOIN SET_DocumentType ON GL_TransactionHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID and SET_DocumentType.DocumentType like '%trade%'
INNER JOIN GL_TransactionDetail ON GL_TransactionDetail.TransactionID =GL_TransactionHeader.TransactionID and GL_TransactionDetail.TransactionType ='Credit'
INNER JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = GL_TransactionDetail.SourceHeaderID
INNER JOIN AR_SET_Customer ON INV_StockTransactionHeader.BusinessPartnerID = AR_SET_Customer.CustomerID 
AND AR_Set_Customer.CustomerID  between @customerid AND  @customerid AND AR_SET_Customer.Status = 1 

	        WHERE GL_TransactionHeader.CompanyID in @CompanyID AND GL_TransactionHeader.PeriodID = @PeriodId AND GL_TransactionHeader.DocumentDate BETWEEN @FromDate AND @ToDate 
	        
	        --End Trade Back ";

                }

                #endregion

                #region Document Group new group
                if (documentGroupID != "" )
                {
                    qry_SalesInvoiceFilter += " AND Set_DocumentType.GroupID in " + documentGroupID;
                    qry_ReceiptFilter += " AND Set_DocumentType.GroupID in " + documentGroupID;// +" or  Set_DocumentType.DocumentType like '%Opening%'";
                    qry_PurchaseInvoiceFilter += " AND Set_DocumentType.GroupID in " + documentGroupID;// +" or  Set_DocumentType.DocumentType like '%Opening%'";
                    qry_PaymentFilter += " AND Set_DocumentType.GroupID in " + documentGroupID;// +" or  Set_DocumentType.DocumentType like '%Opening%'";
                }

                //if (documentGroupID.HasValue)
                //{
                //    qry_SalesInvoiceFilter += " AND Set_DocumentType.GroupID = " + documentGroupID ;
                //    qry_ReceiptFilter += " AND Set_DocumentType.GroupID = " + documentGroupID ;
                //    qry_PurchaseInvoiceFilter += " AND Set_DocumentType.GroupID = " + documentGroupID ;
                //    qry_PaymentFilter += " AND Set_DocumentType.GroupID = " + documentGroupID ;
                //}

                #endregion


                if (documentTypeAuthentication && (documentGroupID != ""))
                {
                    sqlparams[6] = new SqlParameter("@UserID", userID);
                    //                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
                    //AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID    ";
                }

                #region Append Opening Query For Others Groups

                string appendingOpeningQueryForOthers = " ";


                #endregion

                #region Location/CostCenter Region

                if (LocationID > 0)
                    LocationFillter = "and GL_TransactionHeader.LocationID = " + LocationID + "";

                #endregion



                string query = string.Format(Query_GetMultipleBusinessPartnerStatus, businessPartnerTypeFilter, customerFilter, filter, qry_SalesInvoiceFilter,
                    qry_ReceiptFilter, qry_PurchaseInvoiceFilter, qry_PaymentFilter, qry_docuTypeAuthenticate, businessPartnerTypeFilter2, customerFilter2, additionalTrade, appendingOpeningQueryForOthers, LocationFillter, sbCC.ToString(), sbCC2.ToString(), sbCC3.ToString());

                companyID = "(" + companyID + ")";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query.ToString().Replace("@CompanyID", companyID), sqlparams);

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

        public DataTable GetMISBusinessPartnerFor_RSM(string companyID, int periodID, string businessPartnerType, string businessPartnerIDs, DateTime fromDate,
           DateTime toDate, string TransactionType, bool discardReversedDocuments, Guid userID, string documentGroupID = "", Nullable<int> DocumentTypeiD = null,
           bool documentTypeAuthentication = false, int? customerID = null, int? CustomerCategoryID = null, string VendorPayment = "", int LocationID = 0, int CostCenterID = 0
            , bool IsSummary = false)
        {
            #region Function Variables

            DataTable dtToReturn = null;
            string qry_SalesInvoiceFilter = string.Empty, qry_ReceiptFilter = string.Empty, qry_PurchaseInvoiceFilter = string.Empty,
                qry_PaymentFilter = string.Empty, qry_docuTypeAuthenticate = string.Empty, businessPartnerTypeFilter = string.Empty,
                businessPartnerTypeFilter2 = string.Empty, customerFilter = string.Empty, customerFilter2 = string.Empty, filter = string.Empty, LocationFillter = string.Empty;
            StringBuilder sbCC = new StringBuilder();
            StringBuilder sbCC2 = new StringBuilder();
            StringBuilder sbCC3 = new StringBuilder();

            #endregion

            #region Filter Reversed Documents

            if (discardReversedDocuments)
            {
                qry_SalesInvoiceFilter = " And IsNull(Sal_InvoiceHeader.IsReversed, 0) = 0 And Sal_InvoiceHeader.ReversalID is null ";
                qry_ReceiptFilter = " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                qry_PurchaseInvoiceFilter = " And IsNull(AR_InvoiceHeader.IsReversed, 0) = 0 And AR_InvoiceHeader.ReversalID is null ";
                qry_PaymentFilter = " And AP_PaymentHeader.ReversalComments is null ";
            }

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);

                if (DocumentTypeiD.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@DocumentTypeiD", DocumentTypeiD);
                }
                else
                {
                    sqlparams[4] = new SqlParameter("@DocumentTypeiD", DBNull.Value);
                }

                #region Filteration
                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                    businessPartnerTypeFilter = " AND AR_SET_BusinessPartnerTypes.TypeCode in (" + businessPartnerType + ") ";

                if (!string.IsNullOrEmpty(businessPartnerIDs) && !businessPartnerIDs.Equals("-1"))
                    customerFilter = " AND AR_Set_Customer.CustomerID " + businessPartnerIDs + " ";

                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                    businessPartnerTypeFilter2 = " AND TABInvoice.TypeCode in (" + businessPartnerType + ") ";

                if (!string.IsNullOrEmpty(businessPartnerIDs) && !businessPartnerIDs.Equals("-1"))
                    customerFilter2 = " AND TABInvoice.CustomerID " + businessPartnerIDs + " ";

                #region Filter by Transaction Type

                if (TransactionType == "Credit")
                    filter = " where TAb.Amount < 0";
                else if (TransactionType == "Debit")
                    filter = " where TAb.Amount > 0";
                #endregion


                #endregion

                if (CustomerCategoryID.HasValue)
                {
                    sbCC.Append(" left join AR_SET_Customer as CusCat on CusCat.CustomerID = FinalTAB.CustomerID ");
                    sbCC2.Append("and CusCat.CustomerCategoryID = " + CustomerCategoryID.ToString() + "");
                }
                if (!string.IsNullOrEmpty(VendorPayment))
                {
                    if (VendorPayment == "Positive")
                    {
                        sbCC3.Append(", 'Positive' as VendorPayment");
                    }
                    if (VendorPayment == "Negative")
                    {
                        sbCC3.Append(", 'Negative' as VendorPayment");
                        //sbCC3.Append(" And  FinalTAB.Amount < 0");
                    }
                }
                else
                {
                    sbCC3.Append(", '' as VendorPayment");
                }
                #region Query Business Partner Status

                ///string Query_GetMultipleBusinessPartnerStatus = FnI.QueryResourceLibrary.Setup.BusinessPartnerType.Query_MISBusinessPartnerReport;
                //   string Query_GetMultipleBusinessPartnerStatus = FnI.QueryResourceLibrary.Setup.BusinessPartnerType.Query_MISBusinessPartnerReport_New;

                string Query_GetMultipleBusinessPartnerStatus = FnI.QueryResourceLibrary.Setup.BusinessPartnerType2.Query_MISBusinessPartnerReport_One;

                string Query_GetMultipleBusinessPartnerStatusFiscal = FnI.QueryResourceLibrary.Setup.BusinessPartnerType.Query_MISBusinessPartnerReport_Fiscal;

                if (IsSummary)
                    Query_GetMultipleBusinessPartnerStatus = FnI.QueryResourceLibrary.Setup.BusinessPartnerType2.Query_MISBusinessPartnerReport_OneSummary2;


                #endregion

                #region Additional Trade

                string additionalTrade = "";

                if (customerID.HasValue)
                {
                    sqlparams[5] = new SqlParameter("@customerid", customerID);

                    additionalTrade = @"	        union all 
	        
	        --Start Trade Back
	        
	   SELECT distinct SET_DocumentType.DocumentType, GL_TransactionHeader.DocumentDate as CreateDate, GL_TransactionHeader.CompanyID,
            AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            GL_TransactionDetail.SerialNo + 1 as SourceID,
            (GL_TransactionDetail.Amount * -1) as Amount, 
            '0' AS InstrumentNo, 
            '' AS Comments, 
            '' AS DetailNarration,
            '' as InvoiceNumbers, '' as InvoiceDocumentNo, ISNULL(INV_StockTransactionHeader.DocumentNo, 0) AS DocumentNo, 
            GL_TransactionHeader.DocumentDate AS DocumentDate, 
            0 AS FreightAmount, 
            0 As ForAlhajAmount 
            ,'' as BPType,'' as GRN_NO,'' as PO_NO, '' AS SheetNo, '' AS FAX
 FROM GL_TransactionHeader 

INNER JOIN SET_DocumentType ON GL_TransactionHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID and SET_DocumentType.DocumentType like '%trade%'
INNER JOIN GL_TransactionDetail ON GL_TransactionDetail.TransactionID =GL_TransactionHeader.TransactionID and GL_TransactionDetail.TransactionType ='Credit'
INNER JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = GL_TransactionDetail.SourceHeaderID
INNER JOIN AR_SET_Customer ON INV_StockTransactionHeader.BusinessPartnerID = AR_SET_Customer.CustomerID 
AND AR_Set_Customer.CustomerID  between @customerid AND  @customerid AND AR_SET_Customer.Status = 1 

	        WHERE GL_TransactionHeader.CompanyID in @CompanyID AND GL_TransactionHeader.PeriodID = @PeriodId AND GL_TransactionHeader.DocumentDate BETWEEN @FromDate AND @ToDate 
	        
	        --End Trade Back ";

                }

                #endregion

                #region Document Group new group
                if (documentGroupID != "")
                {
                    qry_SalesInvoiceFilter += " AND Set_DocumentType.GroupID in " + documentGroupID;
                    qry_ReceiptFilter += " AND Set_DocumentType.GroupID in " + documentGroupID;// +" or  Set_DocumentType.DocumentType like '%Opening%'";
                    qry_PurchaseInvoiceFilter += " AND Set_DocumentType.GroupID in " + documentGroupID;// +" or  Set_DocumentType.DocumentType like '%Opening%'";
                    qry_PaymentFilter += " AND Set_DocumentType.GroupID in " + documentGroupID;// +" or  Set_DocumentType.DocumentType like '%Opening%'";
                }

                #endregion


                if (documentTypeAuthentication && (documentGroupID != ""))
                {
                    sqlparams[6] = new SqlParameter("@UserID", userID);
                }

                #region Append Opening Query For Others Groups

                string appendingOpeningQueryForOthers = " ";


                #endregion

                #region Location/CostCenter Region

                if (LocationID > 0)
                    LocationFillter = "and GL_TransactionHeader.LocationID = " + LocationID + "";

                #endregion



                string query = string.Format(Query_GetMultipleBusinessPartnerStatus, businessPartnerTypeFilter, customerFilter, filter, qry_SalesInvoiceFilter,
                    qry_ReceiptFilter, qry_PurchaseInvoiceFilter, qry_PaymentFilter, qry_docuTypeAuthenticate, businessPartnerTypeFilter2, customerFilter2, additionalTrade, appendingOpeningQueryForOthers, LocationFillter, sbCC.ToString(), sbCC2.ToString(), sbCC3.ToString());

                companyID = "(" + companyID + ")";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query.ToString().Replace("@CompanyID", companyID), sqlparams);

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

        public DataTable GetMISBusinessPartnerFor_DKSM(string companyID, int periodID, string businessPartnerType, string businessPartnerIDs, DateTime fromDate,
           DateTime toDate, string TransactionType, bool discardReversedDocuments, Guid userID, string documentGroupID = "", Nullable<int> DocumentTypeiD = null,
           bool documentTypeAuthentication = false, int? customerID = null, int? CustomerCategoryID = null, string VendorPayment = "", int LocationID = 0, int CostCenterID = 0, bool IsPattoki = false)
        {
            #region Function Variables

            DataTable dtToReturn = null;
            string qry_SalesInvoiceFilter = string.Empty, qry_ReceiptFilter = string.Empty, qry_PurchaseInvoiceFilter = string.Empty,
                qry_PaymentFilter = string.Empty, qry_docuTypeAuthenticate = string.Empty, businessPartnerTypeFilter = string.Empty,
                businessPartnerTypeFilter2 = string.Empty, customerFilter = string.Empty, customerFilter2 = string.Empty, filter = string.Empty, LocationFillter = string.Empty;
            StringBuilder sbCC = new StringBuilder();
            StringBuilder sbCC2 = new StringBuilder();
            StringBuilder sbCC3 = new StringBuilder();

            #endregion

            #region Filter Reversed Documents

            if (discardReversedDocuments)
            {
                qry_SalesInvoiceFilter = " And IsNull(Sal_InvoiceHeader.IsReversed, 0) = 0 And Sal_InvoiceHeader.ReversalID is null ";
                qry_ReceiptFilter = " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                qry_PurchaseInvoiceFilter = " And IsNull(AR_InvoiceHeader.IsReversed, 0) = 0 And AR_InvoiceHeader.ReversalID is null ";
                qry_PaymentFilter = " And AP_PaymentHeader.ReversalComments is null ";
            }

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);

                if (DocumentTypeiD.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@DocumentTypeiD", DocumentTypeiD);
                }
                else
                {
                    sqlparams[4] = new SqlParameter("@DocumentTypeiD", DBNull.Value);
                }

                #region Filteration
                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                    businessPartnerTypeFilter = " AND AR_SET_BusinessPartnerTypes.TypeCode in (" + businessPartnerType + ") ";

                if (!string.IsNullOrEmpty(businessPartnerIDs) && !businessPartnerIDs.Equals("-1"))
                    customerFilter = " AND AR_Set_Customer.CustomerID " + businessPartnerIDs + " ";

                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                    businessPartnerTypeFilter2 = " AND TABInvoice.TypeCode in (" + businessPartnerType + ") ";

                if (!string.IsNullOrEmpty(businessPartnerIDs) && !businessPartnerIDs.Equals("-1"))
                    customerFilter2 = " AND TABInvoice.CustomerID " + businessPartnerIDs + " ";

                #region Filter by Transaction Type

                if (TransactionType == "Credit")
                    filter = " where TAb.Amount < 0";
                else if (TransactionType == "Debit")
                    filter = " where TAb.Amount > 0";
                #endregion


                #endregion

                if (CustomerCategoryID.HasValue)
                {
                    sbCC.Append(" left join AR_SET_Customer as CusCat on CusCat.CustomerID = FinalTAB.CustomerID ");
                    sbCC2.Append("and CusCat.CustomerCategoryID = " + CustomerCategoryID.ToString() + "");
                }
                if (!string.IsNullOrEmpty(VendorPayment))
                {
                    if (VendorPayment == "Positive")
                    {
                        sbCC3.Append(", 'Positive' as VendorPayment");
                    }
                    if (VendorPayment == "Negative")
                    {
                        sbCC3.Append(", 'Negative' as VendorPayment");
                        //sbCC3.Append(" And  FinalTAB.Amount < 0");
                    }
                }
                else
                {
                    sbCC3.Append(", '' as VendorPayment");
                }
                #region Query Business Partner Status

                ///string Query_GetMultipleBusinessPartnerStatus = FnI.QueryResourceLibrary.Setup.BusinessPartnerType.Query_MISBusinessPartnerReport;
                //   string Query_GetMultipleBusinessPartnerStatus = FnI.QueryResourceLibrary.Setup.BusinessPartnerType.Query_MISBusinessPartnerReport_New;

                string Query_GetMultipleBusinessPartnerStatus = "";
                
                if (IsPattoki == true)
                    Query_GetMultipleBusinessPartnerStatus = FnI.QueryResourceLibrary.Setup.BusinessPartnerType2.Query_MISBusinessPartnerReport_One_ForPattoki;
                else
                    Query_GetMultipleBusinessPartnerStatus = FnI.QueryResourceLibrary.Setup.BusinessPartnerType2.Query_MISBusinessPartnerReport_One_ForDKSM;

                string Query_GetMultipleBusinessPartnerStatusFiscal = FnI.QueryResourceLibrary.Setup.BusinessPartnerType2.Query_MISBusinessPartnerReport_Fiscal_ForDKSM;


                #endregion

                #region Additional Trade

                string additionalTrade = "";

                if (customerID.HasValue)
                {
                    sqlparams[5] = new SqlParameter("@customerid", customerID);

                    additionalTrade = @"	        union all 
	        
	        --Start Trade Back
	        
	   SELECT distinct SET_DocumentType.DocumentType, GL_TransactionHeader.DocumentDate as CreateDate, GL_TransactionHeader.CompanyID,
            AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            GL_TransactionDetail.SerialNo + 1 as SourceID,
            (GL_TransactionDetail.Amount * -1) as Amount, 
            '0' AS InstrumentNo, 
            '' AS Comments, 
            '' AS DetailNarration,
            '' as InvoiceNumbers, '' as InvoiceDocumentNo, ISNULL(INV_StockTransactionHeader.DocumentNo, 0) AS DocumentNo, 
            GL_TransactionHeader.DocumentDate AS DocumentDate, 
            0 AS FreightAmount, 
            0 As ForAlhajAmount 
            ,'' as BPType,'' as GRN_NO,'' as PO_NO, '' AS SheetNo, '' AS FAX
 FROM GL_TransactionHeader 

INNER JOIN SET_DocumentType ON GL_TransactionHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID and SET_DocumentType.DocumentType like '%trade%'
INNER JOIN GL_TransactionDetail ON GL_TransactionDetail.TransactionID =GL_TransactionHeader.TransactionID and GL_TransactionDetail.TransactionType ='Credit'
INNER JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = GL_TransactionDetail.SourceHeaderID
INNER JOIN AR_SET_Customer ON INV_StockTransactionHeader.BusinessPartnerID = AR_SET_Customer.CustomerID 
AND AR_Set_Customer.CustomerID  between @customerid AND  @customerid AND AR_SET_Customer.Status = 1 

	        WHERE GL_TransactionHeader.CompanyID in @CompanyID AND GL_TransactionHeader.PeriodID = @PeriodId AND GL_TransactionHeader.DocumentDate BETWEEN @FromDate AND @ToDate 
	        
	        --End Trade Back ";

                }

                #endregion

                #region Document Group new group
                if (documentGroupID != "")
                {
                    qry_SalesInvoiceFilter += " AND Set_DocumentType.GroupID in " + documentGroupID;
                    qry_ReceiptFilter += " AND Set_DocumentType.GroupID in " + documentGroupID;// +" or  Set_DocumentType.DocumentType like '%Opening%'";
                    qry_PurchaseInvoiceFilter += " AND Set_DocumentType.GroupID in " + documentGroupID;// +" or  Set_DocumentType.DocumentType like '%Opening%'";
                    qry_PaymentFilter += " AND Set_DocumentType.GroupID in " + documentGroupID;// +" or  Set_DocumentType.DocumentType like '%Opening%'";
                }

                //if (documentGroupID.HasValue)
                //{
                //    qry_SalesInvoiceFilter += " AND Set_DocumentType.GroupID = " + documentGroupID ;
                //    qry_ReceiptFilter += " AND Set_DocumentType.GroupID = " + documentGroupID ;
                //    qry_PurchaseInvoiceFilter += " AND Set_DocumentType.GroupID = " + documentGroupID ;
                //    qry_PaymentFilter += " AND Set_DocumentType.GroupID = " + documentGroupID ;
                //}

                #endregion


                if (documentTypeAuthentication && (documentGroupID != ""))
                {
                    sqlparams[6] = new SqlParameter("@UserID", userID);
                    //                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
                    //AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID    ";
                }

                #region Append Opening Query For Others Groups

                string appendingOpeningQueryForOthers = " ";


                #endregion

                #region Location/CostCenter Region

                if (LocationID > 0)
                    LocationFillter = "and GL_TransactionHeader.LocationID = " + LocationID + "";

                #endregion



                string query = string.Format(Query_GetMultipleBusinessPartnerStatus, businessPartnerTypeFilter, customerFilter, filter, qry_SalesInvoiceFilter,
                    qry_ReceiptFilter, qry_PurchaseInvoiceFilter, qry_PaymentFilter, qry_docuTypeAuthenticate, businessPartnerTypeFilter2, customerFilter2, additionalTrade, appendingOpeningQueryForOthers, LocationFillter, sbCC.ToString(), sbCC2.ToString(), sbCC3.ToString());

                companyID = "(" + companyID + ")";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query.ToString().Replace("@CompanyID", companyID), sqlparams);

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

        public DataTable GetMISBusinessPartner_Fiscal_Opening(int companyID, int periodID, string businessPartnerType, string businessPartnerIDs, DateTime fromDate,
          DateTime toDate, string TransactionType, bool discardReversedDocuments, Guid userID, Nullable<short> documentGroupID = null,
          bool documentTypeAuthentication = false, int? customerID = null)
        {
            #region Function Variables

            DataTable dtToReturn = null;
            string qry_SalesInvoiceFilter = string.Empty, qry_ReceiptFilter = string.Empty, qry_PurchaseInvoiceFilter = string.Empty,
                qry_PaymentFilter = string.Empty, qry_docuTypeAuthenticate = string.Empty, businessPartnerTypeFilter = string.Empty,
                businessPartnerTypeFilter2 = string.Empty, customerFilter = string.Empty, customerFilter2 = string.Empty, filter = string.Empty;

            #endregion

            #region Filter Reversed Documents

            if (discardReversedDocuments)
            {
                qry_SalesInvoiceFilter = " And IsNull(Sal_InvoiceHeader.IsReversed, 0) = 0 And Sal_InvoiceHeader.ReversalID is null ";
                qry_ReceiptFilter = " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                qry_PurchaseInvoiceFilter = " And IsNull(AR_InvoiceHeader.IsReversed, 0) = 0 And AR_InvoiceHeader.ReversalID is null ";
                qry_PaymentFilter = " And AP_PaymentHeader.ReversalComments is null ";
            }

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);

                #region Filteration
                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                    businessPartnerTypeFilter = " AND AR_SET_BusinessPartnerTypes.TypeCode in (" + businessPartnerType + ") ";

                if (!string.IsNullOrEmpty(businessPartnerIDs) && !businessPartnerIDs.Equals("-1"))
                    customerFilter = " AND AR_Set_Customer.CustomerID " + businessPartnerIDs + " ";

                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                    businessPartnerTypeFilter2 = " AND TABInvoice.TypeCode in (" + businessPartnerType + ") ";

                if (!string.IsNullOrEmpty(businessPartnerIDs) && !businessPartnerIDs.Equals("-1"))
                    customerFilter2 = " AND TABInvoice.CustomerID " + businessPartnerIDs + " ";

                #region Filter by Transaction Type

                if (TransactionType == "Credit")
                    filter = " where TAb.Amount < 0";
                else if (TransactionType == "Debit")
                    filter = " where TAb.Amount > 0";
                #endregion


                #endregion

                #region Query Business Partner Status

                //string Query_GetMultipleBusinessPartnerStatus = FnI.QueryResourceLibrary.Setup.BusinessPartnerType.Query_MISBusinessPartnerReport;

                string Query_GetMultipleBusinessPartnerStatus = FnI.QueryResourceLibrary.Setup.BusinessPartnerType.Query_MISBusinessPartnerReport_Fiscal;


                #endregion

                #region Additional Trade

                string additionalTrade = "";

                if (customerID.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@customerid", customerID);

                    additionalTrade = @"	        union all 
	        
	        --Start Trade Back
	        
	   SELECT distinct SET_DocumentType.DocumentType, GL_TransactionHeader.DocumentDate as CreateDate, 
            AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            GL_TransactionDetail.SerialNo + 1 as SourceID,
            (GL_TransactionDetail.Amount * -1) as Amount, 
            '0' AS InstrumentNo, 
            '' AS Comments, 
            '' as InvoiceNumbers, '' as InvoiceDocumentNo, ISNULL(INV_StockTransactionHeader.DocumentNo, 0) AS DocumentNo, 
            GL_TransactionHeader.DocumentDate AS DocumentDate, 
            0 AS FreightAmount, 
            0 As ForAlhajAmount 
            
 FROM GL_TransactionHeader 

INNER JOIN SET_DocumentType ON GL_TransactionHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID and SET_DocumentType.DocumentType like '%trade%'
INNER JOIN GL_TransactionDetail ON GL_TransactionDetail.TransactionID =GL_TransactionHeader.TransactionID and GL_TransactionDetail.TransactionType ='Credit'
INNER JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = GL_TransactionDetail.SourceHeaderID
INNER JOIN AR_SET_Customer ON INV_StockTransactionHeader.BusinessPartnerID = AR_SET_Customer.CustomerID 
 AND AR_Set_Customer.CustomerID  between @customerid AND  @customerid AND AR_SET_Customer.Status = 1 

	        WHERE GL_TransactionHeader.CompanyID = @companyId AND GL_TransactionHeader.PeriodID = @PeriodId AND GL_TransactionHeader.DocumentDate BETWEEN @FromDate AND @ToDate 
	        
	        --End Trade Back ";

                }

                #endregion

                #region Document Group new group
                if (documentGroupID.HasValue && documentGroupID.Value != -1)
                {
                    qry_SalesInvoiceFilter += " AND Set_DocumentType.GroupID = " + documentGroupID;
                    qry_ReceiptFilter += " AND Set_DocumentType.GroupID = " + documentGroupID;// +" or  Set_DocumentType.DocumentType like '%Opening%'";
                    qry_PurchaseInvoiceFilter += " AND Set_DocumentType.GroupID = " + documentGroupID;// +" or  Set_DocumentType.DocumentType like '%Opening%'";
                    qry_PaymentFilter += " AND Set_DocumentType.GroupID = " + documentGroupID;// +" or  Set_DocumentType.DocumentType like '%Opening%'";
                }

                //if (documentGroupID.HasValue)
                //{
                //    qry_SalesInvoiceFilter += " AND Set_DocumentType.GroupID = " + documentGroupID ;
                //    qry_ReceiptFilter += " AND Set_DocumentType.GroupID = " + documentGroupID ;
                //    qry_PurchaseInvoiceFilter += " AND Set_DocumentType.GroupID = " + documentGroupID ;
                //    qry_PaymentFilter += " AND Set_DocumentType.GroupID = " + documentGroupID ;
                //}

                #endregion


                if (documentTypeAuthentication && (documentGroupID.HasValue && documentGroupID.Value != -1))
                {
                    sqlparams[5] = new SqlParameter("@UserID", userID);
                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                }

                #region Append Opening Query For Others Groups

                string appendingOpeningQueryForOthers = " ";



                #endregion

                string query = string.Format(Query_GetMultipleBusinessPartnerStatus, businessPartnerTypeFilter, customerFilter, filter, qry_SalesInvoiceFilter,
                    qry_ReceiptFilter, qry_PurchaseInvoiceFilter, qry_PaymentFilter, qry_docuTypeAuthenticate, businessPartnerTypeFilter2, customerFilter2, additionalTrade, appendingOpeningQueryForOthers);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

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



        public DataTable GetVendorStatusAgingData(int companyID, int periodID, string businessPartnerType, string businessPartnerIDs, DateTime fromDate,
        DateTime toDate, string TransactionType, bool discardReversedDocuments, Guid userID, Nullable<short> documentGroupID = null,
        bool documentTypeAuthentication = false, int? customerID = null)
        {
            #region Function Variables

            DataTable dtToReturn = null;
            string qry_SalesInvoiceFilter = string.Empty, qry_ReceiptFilter = string.Empty, qry_PurchaseInvoiceFilter = string.Empty,
                qry_PaymentFilter = string.Empty, qry_docuTypeAuthenticate = string.Empty, businessPartnerTypeFilter = string.Empty,
                businessPartnerTypeFilter2 = string.Empty, customerFilter = string.Empty, customerFilter2 = string.Empty, filter = string.Empty;

            #endregion

            #region Filter Reversed Documents

            if (discardReversedDocuments)
            {
                qry_SalesInvoiceFilter = " And IsNull(Sal_InvoiceHeader.IsReversed, 0) = 0 And Sal_InvoiceHeader.ReversalID is null ";
                qry_ReceiptFilter = " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                qry_PurchaseInvoiceFilter = " And IsNull(AR_InvoiceHeader.IsReversed, 0) = 0 And AR_InvoiceHeader.ReversalID is null ";
                qry_PaymentFilter = " And AP_PaymentHeader.ReversalComments is null ";
            }

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);


                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                    businessPartnerTypeFilter = " AND AR_SET_BusinessPartnerTypes.TypeCode in (" + businessPartnerType + ") ";

                if (!string.IsNullOrEmpty(businessPartnerIDs) && !businessPartnerIDs.Equals("-1"))
                    customerFilter = " AND AR_Set_Customer.CustomerID " + businessPartnerIDs + " ";

                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                    businessPartnerTypeFilter2 = " AND TABInvoice.TypeCode in (" + businessPartnerType + ") ";

                if (!string.IsNullOrEmpty(businessPartnerIDs) && !businessPartnerIDs.Equals("-1"))
                    customerFilter2 = " AND TABInvoice.CustomerID " + businessPartnerIDs + " ";

                #region Filter by Transaction Type

                if (TransactionType == "Credit")
                    filter = " where TAb.Amount < 0";
                else if (TransactionType == "Debit")
                    filter = " where TAb.Amount > 0";

                #endregion

                #region Query

                string Query_GetMultipleVendorStatusAging = @FnI.QueryResourceLibrary.AR.AR_Set_Customer.Query_GetMultipleVendorStatusAgingReport;

                #endregion

                string additionalTrade = "";

                if (customerID.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@customerid", customerID);

                    additionalTrade = @"	        ";

                }


                if (documentGroupID.HasValue)
                {
                    qry_SalesInvoiceFilter += " AND Set_DocumentType.GroupID = " + documentGroupID;
                    qry_ReceiptFilter += " AND Set_DocumentType.GroupID = " + documentGroupID;
                    qry_PurchaseInvoiceFilter += " AND Set_DocumentType.GroupID = " + documentGroupID;
                    qry_PaymentFilter += " AND Set_DocumentType.GroupID = " + documentGroupID;
                }

                if (documentTypeAuthentication)
                {
                    sqlparams[5] = new SqlParameter("@UserID", userID);
                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID ";
                }




                string query = string.Format(Query_GetMultipleVendorStatusAging, businessPartnerTypeFilter, customerFilter, filter, qry_SalesInvoiceFilter,
                    qry_ReceiptFilter, qry_PurchaseInvoiceFilter, qry_PaymentFilter, qry_docuTypeAuthenticate, businessPartnerTypeFilter2, customerFilter2, additionalTrade);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

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

        public DataTable GetAgingSummaryForPayableOfGST(int companyID, int periodID, int CurrentFiscalID, DateTime fromDate, DateTime toDate)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_Set_Customer.Query_AgingSummaryForPayableOfGST, sqlparams);

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

        public DataTable CostOfSugarcanePurchased(int companyID, int periodID, int CurrentFiscalID, DateTime fromDate, DateTime toDate)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_Set_Customer.Query_CostOfSugarcanePurchased, sqlparams);

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


        public static DataTable Get_BusinessPartnerCodeList(int CompanyID, int PeriodID, int? businessPartnerTypeID = null)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@BusinessPartnerID", businessPartnerTypeID);
                #region Query
                string Query_BusinessCodeList = @"";
                #endregion
                dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_Set_Customer.Query_BusinessCodeList, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static bool UpdateCustomerAreaByCustomerID(int CustomerID, int AreaID, SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CustomerID", CustomerID);
                sqlparams[1] = new SqlParameter("@AreaID", AreaID);

                string Query_Update_ByKey = @"update AR_SET_Customer set AreaID = @AreaID where CustomerID = @CustomerID";

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_ByKey, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_ByKey, sqlparams) > 0)
                        result = true;
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }
        public DataTable GetBusinessPartner_Alimdad(int companyID, int periodID, string businessPartnerType, string businessPartnerIDs, DateTime fromDate,
          DateTime toDate, string TransactionType, bool discardReversedDocuments, Guid userID, Nullable<short> documentGroupID = null,
          bool documentTypeAuthentication = false, int? customerID = null)
        {
            #region Function Variables

            DataTable dtToReturn = null;
            string qry_SalesInvoiceFilter = string.Empty, qry_ReceiptFilter = string.Empty, qry_PurchaseInvoiceFilter = string.Empty,
                qry_PaymentFilter = string.Empty, qry_docuTypeAuthenticate = string.Empty, businessPartnerTypeFilter = string.Empty,
                businessPartnerTypeFilter2 = string.Empty, customerFilter = string.Empty, customerFilter2 = string.Empty, filter = string.Empty;

            #endregion

            #region Filter Reversed Documents

            if (discardReversedDocuments)
            {
                qry_SalesInvoiceFilter = " And IsNull(Sal_InvoiceHeader.IsReversed, 0) = 0 And Sal_InvoiceHeader.ReversalID is null ";
                qry_ReceiptFilter = " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                qry_PurchaseInvoiceFilter = " And IsNull(AR_InvoiceHeader.IsReversed, 0) = 0 And AR_InvoiceHeader.ReversalID is null ";
                qry_PaymentFilter = " And AP_PaymentHeader.ReversalComments is null ";
            }

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);

                #region Filteration
                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                    businessPartnerTypeFilter = " AND AR_SET_BusinessPartnerTypes.TypeCode in (" + businessPartnerType + ") ";

                if (!string.IsNullOrEmpty(businessPartnerIDs) && !businessPartnerIDs.Equals("-1"))
                    customerFilter = " AND AR_Set_Customer.CustomerID " + businessPartnerIDs + " ";

                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                    businessPartnerTypeFilter2 = " AND TABInvoice.TypeCode in (" + businessPartnerType + ") ";

                if (!string.IsNullOrEmpty(businessPartnerIDs) && !businessPartnerIDs.Equals("-1"))
                    customerFilter2 = " AND TABInvoice.CustomerID " + businessPartnerIDs + " ";

                #region Filter by Transaction Type

                if (TransactionType == "Credit")
                    filter = " where Amount < 0";
                else if (TransactionType == "Debit")
                    filter = " where Amount > 0";
                #endregion


                #endregion

                #region Query Business Partner Status

                string Query_GetMultipleBusinessPartnerStatus = FnI.QueryResourceLibrary.Setup.BusinessPartnerType.Query_BusinessPartnerStatus_Alimdad;

                string Query_GetMultipleBusinessPartnerStatusFiscal = FnI.QueryResourceLibrary.Setup.BusinessPartnerType.Query_MISBusinessPartnerReport_Fiscal;


                #endregion

                #region Additional Trade

                string additionalTrade = "";

                if (customerID.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@customerid", customerID);

                    additionalTrade = @"	        union all 
	        
	        --Start Trade Back
	        
	   SELECT distinct SET_DocumentType.DocumentType, GL_TransactionHeader.DocumentDate as CreateDate, 
            AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            GL_TransactionDetail.SerialNo + 1 as SourceID,
            (GL_TransactionDetail.Amount * -1) as Amount, 
            '0' AS InstrumentNo, 
            '' AS Comments, 
            '' AS DetailNarration,
            '' as InvoiceNumbers, '' as InvoiceDocumentNo, ISNULL(INV_StockTransactionHeader.DocumentNo, 0) AS DocumentNo, 
            GL_TransactionHeader.DocumentDate AS DocumentDate, 
            0 AS FreightAmount, 
            0 As ForAlhajAmount 
            
 FROM GL_TransactionHeader 

INNER JOIN SET_DocumentType ON GL_TransactionHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID and SET_DocumentType.DocumentType like '%trade%'
INNER JOIN GL_TransactionDetail ON GL_TransactionDetail.TransactionID =GL_TransactionHeader.TransactionID and GL_TransactionDetail.TransactionType ='Credit'
INNER JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = GL_TransactionDetail.SourceHeaderID
INNER JOIN AR_SET_Customer ON INV_StockTransactionHeader.BusinessPartnerID = AR_SET_Customer.CustomerID 
 AND AR_Set_Customer.CustomerID  between @customerid AND  @customerid AND AR_SET_Customer.Status = 1 

	        WHERE GL_TransactionHeader.CompanyID = @companyId AND GL_TransactionHeader.PeriodID = @PeriodId AND GL_TransactionHeader.DocumentDate BETWEEN @FromDate AND @ToDate 
	        
	        --End Trade Back ";

                }

                #endregion

                #region Document Group new group
                if (documentGroupID.HasValue && documentGroupID.Value != -1)
                {
                    qry_SalesInvoiceFilter += " AND Set_DocumentType.GroupID = " + documentGroupID;
                    qry_ReceiptFilter += " AND Set_DocumentType.GroupID = " + documentGroupID;// +" or  Set_DocumentType.DocumentType like '%Opening%'";
                    qry_PurchaseInvoiceFilter += " AND Set_DocumentType.GroupID = " + documentGroupID;// +" or  Set_DocumentType.DocumentType like '%Opening%'";
                    qry_PaymentFilter += " AND Set_DocumentType.GroupID = " + documentGroupID;// +" or  Set_DocumentType.DocumentType like '%Opening%'";
                }


                #endregion


                if (documentTypeAuthentication && (documentGroupID.HasValue && documentGroupID.Value != -1))
                {
                    sqlparams[5] = new SqlParameter("@UserID", userID);
                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID    ";
                }

                #region Append Opening Query For Others Groups

                string appendingOpeningQueryForOthers = " ";


                #endregion

                string query = string.Format(Query_GetMultipleBusinessPartnerStatus, businessPartnerTypeFilter, customerFilter, filter, qry_SalesInvoiceFilter,
                    qry_ReceiptFilter, qry_PurchaseInvoiceFilter, qry_PaymentFilter, qry_docuTypeAuthenticate, businessPartnerTypeFilter2, customerFilter2, additionalTrade, appendingOpeningQueryForOthers);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

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
        public DataTable GetBPStatusDataSummary_Alimdad(int companyID, int periodID, string businessPartnerType, string businessPartnerIDs, DateTime fromDate,
DateTime toDate, string TransactionType, bool discardReversedDocuments, Guid userID, Nullable<short> documentGroupID = null,
bool documentTypeAuthentication = false, int? customerID = null, int? StatusCode = null)
        {


            #region Function Variables

            DataTable dtToReturn = null;
            string qry_SalesInvoiceFilter = string.Empty, qry_ReceiptFilter = string.Empty, qry_PurchaseInvoiceFilter = string.Empty,
                qry_PaymentFilter = string.Empty, qry_docuTypeAuthenticate = string.Empty, businessPartnerTypeFilter = string.Empty,
                businessPartnerTypeFilter2 = string.Empty, customerFilter = string.Empty, customerFilter2 = string.Empty, filter = string.Empty;

            #endregion

            #region Filter Reversed Documents

            if (discardReversedDocuments)
            {
                qry_SalesInvoiceFilter = " And IsNull(Sal_InvoiceHeader.IsReversed, 0) = 0 And Sal_InvoiceHeader.ReversalID is null ";
                qry_ReceiptFilter = " And AR_ReceiptTransactionHeader.ReversalComments is null ";
                qry_PurchaseInvoiceFilter = " And IsNull(AR_InvoiceHeader.IsReversed, 0) = 0 And AR_InvoiceHeader.ReversalID is null ";
                qry_PaymentFilter = " And AP_PaymentHeader.ReversalComments is null ";
            }

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);

                #region Filteration
                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                    businessPartnerTypeFilter = " AND AR_SET_BusinessPartnerTypes.TypeCode in (" + businessPartnerType + ") ";

                if (!string.IsNullOrEmpty(businessPartnerIDs) && !businessPartnerIDs.Equals("-1"))
                    customerFilter = " AND AR_Set_Customer.CustomerID " + businessPartnerIDs + " ";

                if (!string.IsNullOrEmpty(businessPartnerType) && !businessPartnerType.Equals("-1"))
                    businessPartnerTypeFilter2 = " AND TABInvoice.TypeCode in (" + businessPartnerType + ") ";

                if (!string.IsNullOrEmpty(businessPartnerIDs) && !businessPartnerIDs.Equals("-1"))
                    customerFilter2 = " AND TABInvoice.CustomerID " + businessPartnerIDs + " ";

                #region Filter by Transaction Type

                if (TransactionType == "Credit")
                    filter = " where Amount < 0";
                else if (TransactionType == "Debit")
                    filter = " where Amount > 0";
                #endregion


                #endregion

                #region Query Business Partner Status

                string Query_GetMultipleBusinessPartnerSummaryStatus = FnI.QueryResourceLibrary.Setup.BusinessPartnerType.Query_BusinessPartnerStatusSummary_Alimdad;

                #endregion

                #region Additional Trade

                string additionalTrade = "";

                if (customerID.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@customerid", customerID);

                    additionalTrade = @"	        union all 
	        
	        --Start Trade Back
	        
	   SELECT distinct SET_DocumentType.DocumentType, GL_TransactionHeader.DocumentDate as CreateDate, 
            AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName AS BusinessPartnerName, 
            GL_TransactionDetail.SerialNo + 1 as SourceID,
            (GL_TransactionDetail.Amount * -1) as Amount, 
            '0' AS InstrumentNo, 
            '' AS Comments, 
            '' AS DetailNarration,
            '' as InvoiceNumbers, '' as InvoiceDocumentNo, ISNULL(INV_StockTransactionHeader.DocumentNo, 0) AS DocumentNo, 
            GL_TransactionHeader.DocumentDate AS DocumentDate, 
            0 AS FreightAmount, 
            0 As ForAlhajAmount 
            
 FROM GL_TransactionHeader 

INNER JOIN SET_DocumentType ON GL_TransactionHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID and SET_DocumentType.DocumentType like '%trade%'
INNER JOIN GL_TransactionDetail ON GL_TransactionDetail.TransactionID =GL_TransactionHeader.TransactionID and GL_TransactionDetail.TransactionType ='Credit'
INNER JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = GL_TransactionDetail.SourceHeaderID
INNER JOIN AR_SET_Customer ON INV_StockTransactionHeader.BusinessPartnerID = AR_SET_Customer.CustomerID 
AND AR_Set_Customer.CustomerID  between @customerid AND  @customerid AND AR_SET_Customer.Status = 1 

	        WHERE GL_TransactionHeader.CompanyID = @companyId AND GL_TransactionHeader.PeriodID = @PeriodId AND GL_TransactionHeader.DocumentDate BETWEEN @FromDate AND @ToDate 
	        
	        --End Trade Back ";

                }

                #endregion

                #region Document Group new group
                if (documentGroupID.HasValue && documentGroupID.Value != -1)
                {
                    qry_SalesInvoiceFilter += " AND Set_DocumentType.GroupID = " + documentGroupID;
                    qry_ReceiptFilter += " AND Set_DocumentType.GroupID = " + documentGroupID;// +" or  Set_DocumentType.DocumentType like '%Opening%'";
                    qry_PurchaseInvoiceFilter += " AND Set_DocumentType.GroupID = " + documentGroupID;// +" or  Set_DocumentType.DocumentType like '%Opening%'";
                    qry_PaymentFilter += " AND Set_DocumentType.GroupID = " + documentGroupID;// +" or  Set_DocumentType.DocumentType like '%Opening%'";
                }


                #endregion


                if (documentTypeAuthentication && (documentGroupID.HasValue && documentGroupID.Value != -1))
                {
                    sqlparams[5] = new SqlParameter("@UserID", userID);
                    qry_docuTypeAuthenticate = @" INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID    ";
                }

                #region Append Opening Query For Others Groups

                string appendingOpeningQueryForOthers = " ";


                #endregion

                string query = string.Format(Query_GetMultipleBusinessPartnerSummaryStatus, businessPartnerTypeFilter, customerFilter, filter, qry_SalesInvoiceFilter,
                    qry_ReceiptFilter, qry_PurchaseInvoiceFilter, qry_PaymentFilter, qry_docuTypeAuthenticate, businessPartnerTypeFilter2, customerFilter2, additionalTrade, appendingOpeningQueryForOthers);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

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

        public AR_SET_Customer(int customerID, int periodID, SqlConnection objConnection = null)
        {
            LoadData(customerID, periodID, objConnection);
        }

       

        private void LoadData(int customerID, int periodID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow drRecord = GetDataByCustomerID(customerID, periodID, objConnection, objTransaction);

            if (drRecord != null)
            {
                this.f_IsDataLoaded = true;
                this.f_CompanyID = Convert.ToInt32(drRecord["CompanyID"]);
                this.f_PeriodID = Convert.ToInt32(drRecord["PeriodID"]);
                this.f_CustomerID = Convert.ToInt32(drRecord["CustomerID"]);
                this.f_CustomerName = Convert.ToString(drRecord["CustomerName"]);

                if (drRecord["AddressLine1"] != DBNull.Value)
                    this.f_AddressLine1 = Convert.ToString(drRecord["AddressLine1"]);


                if (drRecord["CityID"] != DBNull.Value)
                    this.f_CityID = Convert.ToInt32(drRecord["CityID"]);


                if (drRecord["ZipCode"] != DBNull.Value)
                    this.f_ZipCode = Convert.ToString(drRecord["ZipCode"]);


                if (drRecord["PhoneNumber"] != DBNull.Value)
                    this.f_PhoneNumber = Convert.ToString(drRecord["PhoneNumber"]);


                if (drRecord["ExtensionNumber"] != DBNull.Value)
                    this.f_ExtensionNumber = Convert.ToString(drRecord["ExtensionNumber"]);


                if (drRecord["ContactNumber"] != DBNull.Value)
                    this.f_ContactNumber = Convert.ToString(drRecord["ContactNumber"]);


                if (drRecord["FaxNumber"] != DBNull.Value)
                    this.f_FaxNumber = Convert.ToString(drRecord["FaxNumber"]);


                if (drRecord["EmailAddress"] != DBNull.Value)
                    this.f_EmailAddress = Convert.ToString(drRecord["EmailAddress"]);


                if (drRecord["CNICNumber"] != DBNull.Value)
                    this.f_CNICNumber = Convert.ToString(drRecord["CNICNumber"]);


                if (drRecord["NTNNumber"] != DBNull.Value)
                    this.f_NTNNumber = Convert.ToString(drRecord["NTNNumber"]);


                if (drRecord["STRNNumber"] != DBNull.Value)
                    this.f_STRNNumber = Convert.ToString(drRecord["STRNNumber"]);

                this.f_ChartofAccountID = Convert.ToInt32(drRecord["ChartofAccountID"]);

                if (drRecord["CustomerAccountCCID"] != DBNull.Value)
                    this.f_CustomerAccountCCID = Convert.ToString(drRecord["CustomerAccountCCID"]);


                if (drRecord["ContactPerson"] != DBNull.Value)
                    this.f_ContactPerson = Convert.ToString(drRecord["ContactPerson"]);

                this.f_Collector = Convert.ToString(drRecord["Collector"]);

                if (drRecord["CollectorContactNumber"] != DBNull.Value)
                    this.f_CollectorContactNumber = Convert.ToString(drRecord["CollectorContactNumber"]);


                if (drRecord["CreditAnalyst"] != DBNull.Value)
                    this.f_CreditAnalyst = Convert.ToString(drRecord["CreditAnalyst"]);

                this.f_CreditChecking = Convert.ToBoolean(drRecord["CreditChecking"]);

                if (drRecord["NextCreditReviewDate"] != DBNull.Value)
                    this.f_NextCreditReviewDate = Convert.ToDateTime(drRecord["NextCreditReviewDate"]);

                this.f_Tolerance = Convert.ToDouble(drRecord["Tolerance"]);
                this.f_DiscountTerms = Convert.ToBoolean(drRecord["DiscountTerms"]);
                this.f_DunningLetters = Convert.ToBoolean(drRecord["DunningLetters"]);

                if (drRecord["DunningLetterSetID"] != DBNull.Value)
                    this.f_DunningLetterSetID = Convert.ToInt32(drRecord["DunningLetterSetID"]);

                this.f_InterestCharges = Convert.ToBoolean(drRecord["InterestCharges"]);

                if (drRecord["PrefFunctionalCurrencyID"] != DBNull.Value)
                    this.f_PrefFunctionalCurrencyID = Convert.ToInt32(drRecord["PrefFunctionalCurrencyID"]);

                this.f_SendStatements = Convert.ToBoolean(drRecord["SendStatements"]);
                this.f_CreditBalanceStatements = Convert.ToBoolean(drRecord["CreditBalanceStatements"]);
                this.f_CreditHold = Convert.ToBoolean(drRecord["CreditHold"]);

                if (drRecord["ProfileClassID"] != DBNull.Value)
                    this.f_ProfileClassID = Convert.ToInt32(drRecord["ProfileClassID"]);


                if (drRecord["CreditRating"] != DBNull.Value)
                    this.f_CreditRating = Convert.ToString(drRecord["CreditRating"]);


                if (drRecord["RiskID"] != DBNull.Value)
                    this.f_RiskID = Convert.ToInt32(drRecord["RiskID"]);


                if (drRecord["StandardTermID"] != DBNull.Value)
                    this.f_StandardTermID = Convert.ToInt32(drRecord["StandardTermID"]);

                this.f_OverrideTerms = Convert.ToBoolean(drRecord["OverrideTerms"]);

                if (drRecord["InterestPeriodDays"] != DBNull.Value)
                    this.f_InterestPeriodDays = Convert.ToDouble(drRecord["InterestPeriodDays"]);


                if (drRecord["PaymentGraceDays"] != DBNull.Value)
                    this.f_PaymentGraceDays = Convert.ToDouble(drRecord["PaymentGraceDays"]);


                if (drRecord["DiscountGraceDays"] != DBNull.Value)
                    this.f_DiscountGraceDays = Convert.ToDouble(drRecord["DiscountGraceDays"]);


                if (drRecord["StatementCycleID"] != DBNull.Value)
                    this.f_StatementCycleID = Convert.ToInt32(drRecord["StatementCycleID"]);


                if (drRecord["PercentCollectable"] != DBNull.Value)
                    this.f_PercentCollectable = Convert.ToDouble(drRecord["PercentCollectable"]);

                this.f_AutoRecInclDisputedFlag = Convert.ToBoolean(drRecord["AutoRecInclDisputedFlag"]);

                if (drRecord["TaxPrintingOptionID"] != DBNull.Value)
                    this.f_TaxPrintingOptionID = Convert.ToInt32(drRecord["TaxPrintingOptionID"]);

                this.f_ChargeOnFinanceChargeFlag = Convert.ToBoolean(drRecord["ChargeOnFinanceChargeFlag"]);

                if (drRecord["GroupingRuleID"] != DBNull.Value)
                    this.f_GroupingRuleID = Convert.ToInt32(drRecord["GroupingRuleID"]);


                if (drRecord["ClearingDays"] != DBNull.Value)
                    this.f_ClearingDays = Convert.ToInt32(drRecord["ClearingDays"]);

                this.f_ConsInvFlag = Convert.ToBoolean(drRecord["ConsInvFlag"]);

                if (drRecord["ConsInvTypeID"] != DBNull.Value)
                    this.f_ConsInvTypeID = Convert.ToInt32(drRecord["ConsInvTypeID"]);

                this.f_Status = Convert.ToBoolean(drRecord["Status"]);
                this.f_CreatedBy = Guid.Parse(drRecord["CreatedBy"].ToString());
                this.f_CreateDate = Convert.ToDateTime(drRecord["CreateDate"]);

                if (drRecord["UpdatedBy"] != DBNull.Value)
                    this.f_UpdatedBy = Guid.Parse(drRecord["UpdatedBy"].ToString());


                if (drRecord["UpdateDate"] != DBNull.Value)
                    this.f_UpdateDate = Convert.ToDateTime(drRecord["UpdateDate"]);

                this.f_CreatedByValue = Convert.ToString(drRecord["CreatedByValue"]);

                if (drRecord["UpdatedByValue"] != DBNull.Value)
                    this.f_UpdatedByValue = Convert.ToString(drRecord["UpdatedByValue"]);

                this.f_IsActive = Convert.ToBoolean(drRecord["IsActive"]);

                if (drRecord["FatherName"] != DBNull.Value)
                    this.f_FatherName = Convert.ToString(drRecord["FatherName"]);

                this.f_ServiceItemTaxExempted = Convert.ToBoolean(drRecord["ServiceItemTaxExempted"]);

                if (drRecord["ServiceItemTaxRate"] != DBNull.Value)
                    this.f_ServiceItemTaxRate = Convert.ToDecimal(drRecord["ServiceItemTaxRate"]);


                if (drRecord["ServiceItemTaxCOA"] != DBNull.Value)
                    this.f_ServiceItemTaxCOA = Convert.ToString(drRecord["ServiceItemTaxCOA"]);

                this.f_SuppliesTaxExempted = Convert.ToBoolean(drRecord["SuppliesTaxExempted"]);

                if (drRecord["SuppliesTaxRate"] != DBNull.Value)
                    this.f_SuppliesTaxRate = Convert.ToDecimal(drRecord["SuppliesTaxRate"]);


                if (drRecord["SuppliesTaxCOA"] != DBNull.Value)
                    this.f_SuppliesTaxCOA = Convert.ToString(drRecord["SuppliesTaxCOA"]);

                this.f_ExemptedAmount = Convert.ToDecimal(drRecord["ExemptedAmount"]);

                if (drRecord["ContactPersonName"] != DBNull.Value)
                    this.f_ContactPersonName = Convert.ToString(drRecord["ContactPersonName"]);


                if (drRecord["ContactPersonAddress"] != DBNull.Value)
                    this.f_ContactPersonAddress = Convert.ToString(drRecord["ContactPersonAddress"]);


                if (drRecord["TransporterTaxExempted"] != DBNull.Value)
                    this.f_TransporterTaxExempted = Convert.ToBoolean(drRecord["TransporterTaxExempted"]);


                if (drRecord["TransporterTaxRate"] != DBNull.Value)
                    this.f_TransporterTaxRate = Convert.ToDecimal(drRecord["TransporterTaxRate"]);


                if (drRecord["TransporterExceedLimit"] != DBNull.Value)
                    this.f_TransporterExceedLimit = Convert.ToDecimal(drRecord["TransporterExceedLimit"]);


                if (drRecord["TransporterTaxCOA"] != DBNull.Value)
                    this.f_TransporterTaxCOA = Convert.ToString(drRecord["TransporterTaxCOA"]);


                if (drRecord["AdditionalDeductionTaxExempted"] != DBNull.Value)
                    this.f_AdditionalDeductionTaxExempted = Convert.ToBoolean(drRecord["AdditionalDeductionTaxExempted"]);


                if (drRecord["AdditionalDeductionTaxRate"] != DBNull.Value)
                    this.f_AdditionalDeductionTaxRate = Convert.ToDecimal(drRecord["AdditionalDeductionTaxRate"]);


                if (drRecord["AdditionalDeductionCOA"] != DBNull.Value)
                    this.f_AdditionalDeductionCOA = Convert.ToString(drRecord["AdditionalDeductionCOA"]);


                if (drRecord["FK_BankID"] != DBNull.Value)
                    this.f_FK_BankID = Convert.ToInt32(drRecord["FK_BankID"]);


                if (drRecord["FK_BankBranchID"] != DBNull.Value)
                    this.f_FK_BankBranchID = Convert.ToInt32(drRecord["FK_BankBranchID"]);


                if (drRecord["AccountNumber"] != DBNull.Value)
                    this.f_AccountNumber = Convert.ToString(drRecord["AccountNumber"]);


                if (drRecord["IsStopPayment"] != DBNull.Value)
                    this.f_IsStopPayment = Convert.ToBoolean(drRecord["IsStopPayment"]);


                if (drRecord["Districtid"] != DBNull.Value)
                    this.f_Districtid = Convert.ToInt32(drRecord["Districtid"]);


                if (drRecord["Provinceid"] != DBNull.Value)
                    this.f_Provinceid = Convert.ToInt16(drRecord["Provinceid"]);


                if (drRecord["FixedDiscount"] != DBNull.Value)
                    this.f_FixedDiscount = Convert.ToDecimal(drRecord["FixedDiscount"]);


                if (drRecord["PaymentType"] != DBNull.Value)
                    this.f_PaymentType = Convert.ToString(drRecord["PaymentType"]);


                if (drRecord["FK_BankAccountID"] != DBNull.Value)
                    this.f_FK_BankAccountID = Convert.ToInt32(drRecord["FK_BankAccountID"]);


                if (drRecord["Remarks"] != DBNull.Value)
                    this.f_Remarks = Convert.ToString(drRecord["Remarks"]);


                if (drRecord["AddressLine2"] != DBNull.Value)
                    this.f_AddressLine2 = Convert.ToString(drRecord["AddressLine2"]);


                if (drRecord["AddressLine3"] != DBNull.Value)
                    this.f_AddressLine3 = Convert.ToString(drRecord["AddressLine3"]);


                if (drRecord["AddressLine4"] != DBNull.Value)
                    this.f_AddressLine4 = Convert.ToString(drRecord["AddressLine4"]);


                if (drRecord["IsRegistered"] != DBNull.Value)
                    this.f_IsRegistered = Convert.ToBoolean(drRecord["IsRegistered"]);


                if (drRecord["AreaID"] != DBNull.Value)
                    this.f_AreaID = Convert.ToInt32(drRecord["AreaID"]);


                if (drRecord["ROUTEPLANID"] != DBNull.Value)
                    this.f_ROUTEPLANID = Convert.ToInt32(drRecord["ROUTEPLANID"]);


                if (drRecord["OSCode"] != DBNull.Value)
                    this.f_OSCode = Convert.ToString(drRecord["OSCode"]);


                if (drRecord["CountryID"] != DBNull.Value)
                    this.f_CountryID = Convert.ToInt32(drRecord["CountryID"]);


                if (drRecord["IsLoyalFlag"] != DBNull.Value)
                    this.f_IsLoyalFlag = Convert.ToBoolean(drRecord["IsLoyalFlag"]);


                if (drRecord["Filer"] != DBNull.Value)
                    this.f_Filer = Convert.ToBoolean(drRecord["Filer"]);

                if (drRecord["SecurityDeposite"] != DBNull.Value)
                    this.f_SecurityDeposite = Convert.ToDecimal(drRecord["SecurityDeposite"]);


                if (drRecord["SpecialDeal"] != DBNull.Value)
                    this.f_SpecialDeal = Convert.ToDecimal(drRecord["SpecialDeal"]);

                if (drRecord["TaxCardTypeID"] != DBNull.Value)
                    this.f_TaxCardTypeID = Convert.ToInt16(drRecord["TaxCardTypeID"]);

                if (drRecord["CustomerCategoryID"] != DBNull.Value)
                    this.f_CustomerCategoryID = Convert.ToInt16(drRecord["CustomerCategoryID"]);

                if (drRecord["TaxationStatusID"] != DBNull.Value)
                    this.f_TaxationStatusID = Convert.ToInt32(drRecord["TaxationStatusID"]);


                if (drRecord["TaxationStatus"] != DBNull.Value)
                    this.f_TaxationStatus = Convert.ToString(drRecord["TaxationStatus"]);


                if (drRecord["TaxStatementCodeID"] != DBNull.Value)
                    this.f_TaxStatementCodeID = Convert.ToInt32(drRecord["TaxStatementCodeID"]);


                if (drRecord["TaxStatementExemptionCodeID"] != DBNull.Value)
                    this.f_TaxStatementExemptionCodeID = Convert.ToInt32(drRecord["TaxStatementExemptionCodeID"]);

                if (drRecord["BrokerID"] != DBNull.Value)
                    this.f_BrokerID = Convert.ToInt32(drRecord["BrokerID"]);

                if (drRecord["IsExporter"] != DBNull.Value)
                    this.f_IsExporter = Convert.ToBoolean(drRecord["IsExporter"]);

                if (drRecord["ISBulkSupplies"] != DBNull.Value)
                    this.f_ISBulkSupplies = Convert.ToBoolean(drRecord["ISBulkSupplies"]);

                if (drRecord["EmployeeCode"] != DBNull.Value)
                    this.f_EmployeeCode = drRecord["EmployeeCode"].ToString();

                if (drRecord["PRANo"] != DBNull.Value)
                    this.f_PRANo = drRecord["PRANo"].ToString();


                if (drRecord["CreditLimit"] != DBNull.Value)
                    this.f_CreditLimit = Convert.ToDecimal(drRecord["CreditLimit"]); 
            }
        }

        
        public static DataRow GetDataByCustomerID(int customerID, int periodID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CustomerID", customerID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                const string Query_GetData_byKey = @" SELECT * FROM [AR_SET_Customer] WHERE [CustomerID] = @CustomerID and [PeriodID] = @PeriodID ";
                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byKey, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byKey, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byKey, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dr = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }


        public static DataTable GetCustomerTransaction(int CustomerID, string COA, Nullable<int> BusinessPartnerTypeID = null)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@BusinessPartnerID", CustomerID);

                if (BusinessPartnerTypeID != null)
                {
                    sqlparams[1] = new SqlParameter("@BusinessPartnerTypeID", BusinessPartnerTypeID);
                }
                else { sqlparams[1] = new SqlParameter("@BusinessPartnerTypeID", DBNull.Value); }

                if (COA != string.Empty)
                {
                    sqlparams[2] = new SqlParameter("@COA", COA);
                }
                else { sqlparams[2] = new SqlParameter("@COA", DBNull.Value); }


                #region Query

                string Query_CheckCustomerTransaction = @"
select count(*) as count from (
select VendorID from AP_PaymentHeader where VendorID = @BusinessPartnerID and Status = 1 AND (AP_PaymentHeader.BusinessPartnerTypeID = @BusinessPartnerTypeID or @BusinessPartnerTypeID is null)
union all
select SupplierID from AP_PaymentSupplierDetail where SupplierID = @BusinessPartnerID and Status = 1 AND (AP_PaymentSupplierDetail.BusinessPartnerTypeID = @BusinessPartnerTypeID or @BusinessPartnerTypeID is null)
union all
select VendorID from AR_InvoiceHeader where VendorID = @BusinessPartnerID and Status = 1 AND (AR_InvoiceHeader.BusinessPartnerTypeID = @BusinessPartnerTypeID or @BusinessPartnerTypeID is null)
union all
select SupplierID from AR_InvoiceSupplierDetail where SupplierID = @BusinessPartnerID and Status = 1 AND (AR_InvoiceSupplierDetail.BusinessPartnerTypeID = @BusinessPartnerTypeID or @BusinessPartnerTypeID is null)
union all
select CustomerID from AR_ReceiptTransactionHeader where CustomerID = @BusinessPartnerID and Status = 1 AND (AR_ReceiptTransactionHeader.BusinessPartnerTypeID = @BusinessPartnerTypeID or @BusinessPartnerTypeID is null)
union all
select CustomerID from AR_ReceiptCustomerDetail where CustomerID = @BusinessPartnerID and Status = 1 AND (AR_ReceiptCustomerDetail.BusinessPartnerTypeID = @BusinessPartnerTypeID or @BusinessPartnerTypeID is null)
union all
select CustomerID from SAL_InvoiceHeader where CustomerID = @BusinessPartnerID and Status = 1 AND (2 = @BusinessPartnerTypeID or @BusinessPartnerTypeID is null)
union all
select CustomerID from Sal_InvoiceCustomerDetail where CustomerID = @BusinessPartnerID and Status = 1 AND (Sal_InvoiceCustomerDetail.BusinessPartnerTypeID = @BusinessPartnerTypeID or @BusinessPartnerTypeID is null)
) tab
";
                #endregion

                #region QueryWithAccountCode
                if (COA != string.Empty)
                {
                    Query_CheckCustomerTransaction = @"
                    select count(*) as count from (
                    select SupplierID from AP_PaymentSupplierDetail where SupplierID = @BusinessPartnerID and Status = 1 AND (AP_PaymentSupplierDetail.BusinessPartnerTypeID = @BusinessPartnerTypeID or @BusinessPartnerTypeID is null)
                    AND (AP_PaymentSupplierDetail.SupplierAccountCode = @COA or @COA is null)
                    union all
                    select SupplierID from AR_InvoiceSupplierDetail where SupplierID = @BusinessPartnerID and Status = 1 AND (AR_InvoiceSupplierDetail.BusinessPartnerTypeID = @BusinessPartnerTypeID or @BusinessPartnerTypeID is null)
                    AND (AR_InvoiceSupplierDetail.SupplierAccountCode = @COA or @COA is null)
                    union all
                    select CustomerID from AR_ReceiptCustomerDetail where CustomerID = @BusinessPartnerID and Status = 1 AND (AR_ReceiptCustomerDetail.BusinessPartnerTypeID = @BusinessPartnerTypeID or @BusinessPartnerTypeID is null)
                    AND (AR_ReceiptCustomerDetail.CustomerAccountCode = @COA or @COA is null)
                    union all
                    select CustomerID from Sal_InvoiceCustomerDetail where CustomerID = @BusinessPartnerID and Status = 1 AND (Sal_InvoiceCustomerDetail.BusinessPartnerTypeID = @BusinessPartnerTypeID or @BusinessPartnerTypeID is null)
                    AND (Sal_InvoiceCustomerDetail.CustomerAccountCode = @COA or @COA is null)
                    ) tab
                    ";
                }

                #endregion
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_CheckCustomerTransaction, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetYearlyMonthlyLimits(int CustomerID, int DocumentTypeID, int CompanyID, int PeriodID, int @Month, int @Year, string CNIC)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CustomerID", CustomerID);
                sqlparams[1] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[2] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[3] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[4] = new SqlParameter("@Month", @Month);
                sqlparams[5] = new SqlParameter("@Year", @Year);
                sqlparams[6] = new SqlParameter("@CNIC", @CNIC);

                string query = @"
                
select Sum(final.Yearly) as Yearly, Sum(final.Monthly) as Monthly from (
-----Yearly
select isnull(SUM(ABS(isnull(TransactionQuantity,0) * isnull(TransactionCost,0))),0)  as Yearly , 0 as Monthly 
from SAL_DispatchHeader 
inner join INV_StockTransactionHeader on INV_StockTransactionHeader.DocumentTypeID = SAL_DispatchHeader.DocumentTypeID
and INV_StockTransactionHeader.DocumentNo = SAL_DispatchHeader.DispatchNo
and INV_StockTransactionHeader.CompanyID = SAL_DispatchHeader.CompanyID
and INV_StockTransactionHeader.PeriodID = SAL_DispatchHeader.PeriodID
and INV_StockTransactionHeader.Status = SAL_DispatchHeader.Status
and INV_StockTransactionHeader.FiscalID = SAL_DispatchHeader.FiscalID
left join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID 
and INV_StockTransactionDetail.Status = 1
and INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID
and INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
inner join AR_SET_Customer on AR_SET_Customer.CustomerID = SAL_DispatchHeader.CustomerID
and AR_SET_Customer.Status = SAL_DispatchHeader.Status
inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID and SET_DocumentType.Status = 1
and SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID
and SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
where 
--AR_SET_Customer.CustomerID = @CustomerID 
SAL_DispatchHeader.CustomerCNICII = @CNIC
and AR_SET_Customer.IsRegistered = 0
and SAL_DispatchHeader.status = 1
and Cast(SAL_DispatchHeader.DispatchDate as Date) between
(select max(Cast(HRMS_SET_TaxYear.FromDate as Date)) from HRMS_SET_TaxYear where HRMS_SET_TaxYear.Status  = 1 ) 
and (select max(Cast(HRMS_SET_TaxYear.ToDate as Date)) from HRMS_SET_TaxYear where HRMS_SET_TaxYear.Status  = 1)
and SET_DocumentType.DocumentTypeID = @DocumentTypeID
and SAL_DispatchHeader.CompanyID = @CompanyID
and SAL_DispatchHeader.PeriodID = @PeriodID

union All
 
-----Monthly
select 0  as Yearly ,isnull(SUM(ABS(isnull(TransactionQuantity,0) * isnull(TransactionCost,0))),0)  as Monthly
from SAL_DispatchHeader 
inner join INV_StockTransactionHeader on INV_StockTransactionHeader.DocumentTypeID = SAL_DispatchHeader.DocumentTypeID
and INV_StockTransactionHeader.DocumentNo = SAL_DispatchHeader.DispatchNo
and INV_StockTransactionHeader.CompanyID = SAL_DispatchHeader.CompanyID
and INV_StockTransactionHeader.PeriodID = SAL_DispatchHeader.PeriodID
and INV_StockTransactionHeader.Status = SAL_DispatchHeader.Status
and INV_StockTransactionHeader.FiscalID = SAL_DispatchHeader.FiscalID
left join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID 
and INV_StockTransactionDetail.Status = 1
and INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID
and INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
inner join AR_SET_Customer on AR_SET_Customer.CustomerID = SAL_DispatchHeader.CustomerID
and AR_SET_Customer.Status = SAL_DispatchHeader.Status
inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID and SET_DocumentType.Status = 1
and SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID
and SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
where 
--AR_SET_Customer.CustomerID = @CustomerID 
SAL_DispatchHeader.CustomerCNICII = @CNIC
and AR_SET_Customer.IsRegistered = 0
and SAL_DispatchHeader.status = 1
and Cast(SAL_DispatchHeader.DispatchDate as Date) between
(select max(Cast(HRMS_SET_TaxYear.FromDate as Date)) from HRMS_SET_TaxYear where HRMS_SET_TaxYear.Status  = 1) 
and (select max(Cast(HRMS_SET_TaxYear.ToDate as Date)) from HRMS_SET_TaxYear where HRMS_SET_TaxYear.Status  = 1)
and Month(Cast(SAL_DispatchHeader.DispatchDate as Date)) = @Month  and Year(Cast(SAL_DispatchHeader.DispatchDate as Date)) = @Year
and SET_DocumentType.DocumentTypeID = @DocumentTypeID
and SAL_DispatchHeader.CompanyID = @CompanyID
and SAL_DispatchHeader.PeriodID = @PeriodID
)final

                ";
                dt = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetYearlyMonthlyLimitsForGateIn(DateTime GateInDate, int DocumentTypeID, int CompanyID, int PeriodID, int @Month, int @Year, string CNIC)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@GateInDate", GateInDate);
                sqlparams[1] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[2] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[3] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[4] = new SqlParameter("@Month", @Month);
                sqlparams[5] = new SqlParameter("@Year", @Year);
                sqlparams[6] = new SqlParameter("@CNIC", @CNIC);

                string query = @"
                
select Sum(final.Yearly) as Yearly, Sum(final.Monthly) as Monthly from (
-----Yearly
select isnull(SUM(ABS(isnull(TransactionQuantity,0) * isnull(SAL_OrderDetail.Rate,0))),0)  as Yearly , 0 as Monthly 
from SAL_DispatchHeader 
inner join INV_StockTransactionHeader on INV_StockTransactionHeader.DocumentTypeID = SAL_DispatchHeader.DocumentTypeID
and INV_StockTransactionHeader.DocumentNo = SAL_DispatchHeader.DispatchNo
and INV_StockTransactionHeader.CompanyID = SAL_DispatchHeader.CompanyID
and INV_StockTransactionHeader.PeriodID = SAL_DispatchHeader.PeriodID
and INV_StockTransactionHeader.Status = SAL_DispatchHeader.Status
and INV_StockTransactionHeader.FiscalID = SAL_DispatchHeader.FiscalID
left join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID 
and INV_StockTransactionDetail.Status = 1
and INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID
and INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
LEft JOIN Sal_DeliveryOrderDetail on Sal_DeliveryOrderDetail.DOHeaderID = [INV_StockTransactionDetail].SourceHeaderID
		And Sal_DeliveryOrderDetail.DODetailID = [INV_StockTransactionDetail].SourceLineID
		AND Sal_DeliveryOrderDetail.CompanyID = [INV_StockTransactionDetail].CompanyID 
		AND Sal_DeliveryOrderDetail.PeriodID = [INV_StockTransactionDetail].PeriodID
		and Sal_DeliveryOrderDetail.status = 1

left join SAL_OrderDetail On SAL_DeliveryOrderDetail.CompanyID = SAL_OrderDetail.CompanyID 
		And SAL_DeliveryOrderDetail.PeriodID = SAL_OrderDetail.PeriodID 
		And SAL_DeliveryOrderDetail.SalesOrderHeaderID = SAL_OrderDetail.OrderID 
		And SAL_DeliveryOrderDetail.SalesOrderDetailID = SAL_OrderDetail.OrderDetailID
		and SAL_OrderDetail.status = 1
inner join AR_SET_Customer on AR_SET_Customer.CustomerID = SAL_DispatchHeader.CustomerID
and AR_SET_Customer.Status = SAL_DispatchHeader.Status
inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID and SET_DocumentType.Status = 1
and SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID
and SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
where 
--AR_SET_Customer.CustomerID = @CustomerID 
SAL_DispatchHeader.DriverCNIC = @CNIC
--and AR_SET_Customer.IsRegistered = 0
and SAL_DispatchHeader.status = 1
and Cast(SAL_DispatchHeader.DispatchDate as Date) between
(select Cast(HRMS_SET_TaxYear.FromDate as Date) from HRMS_SET_TaxYear where @GateInDate between FromDate and ToDate ) and (select Cast(HRMS_SET_TaxYear.ToDate as Date) from HRMS_SET_TaxYear where @GateInDate between FromDate and ToDate)
--and SET_DocumentType.DocumentType = @DocumentTypeID
and (SET_DocumentType.DocumentType like '%dispatch%sugar%' OR SET_DocumentType.DocumentType like '%dispatch%billet%')
and SAL_DispatchHeader.CompanyID = @CompanyID
and SAL_DispatchHeader.PeriodID = @PeriodID

union All
 
-----Monthly
select 0  as Yearly ,isnull(SUM(ABS(isnull(TransactionQuantity,0) * isnull(SAL_OrderDetail.Rate,0))),0)  as Monthly
from SAL_DispatchHeader 
inner join INV_StockTransactionHeader on INV_StockTransactionHeader.DocumentTypeID = SAL_DispatchHeader.DocumentTypeID
and INV_StockTransactionHeader.DocumentNo = SAL_DispatchHeader.DispatchNo
and INV_StockTransactionHeader.CompanyID = SAL_DispatchHeader.CompanyID
and INV_StockTransactionHeader.PeriodID = SAL_DispatchHeader.PeriodID
and INV_StockTransactionHeader.Status = SAL_DispatchHeader.Status
and INV_StockTransactionHeader.FiscalID = SAL_DispatchHeader.FiscalID
left join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID 
and INV_StockTransactionDetail.Status = 1
and INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID
and INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
LEft JOIN Sal_DeliveryOrderDetail on Sal_DeliveryOrderDetail.DOHeaderID = [INV_StockTransactionDetail].SourceHeaderID
		And Sal_DeliveryOrderDetail.DODetailID = [INV_StockTransactionDetail].SourceLineID
		AND Sal_DeliveryOrderDetail.CompanyID = [INV_StockTransactionDetail].CompanyID 
		AND Sal_DeliveryOrderDetail.PeriodID = [INV_StockTransactionDetail].PeriodID
		and Sal_DeliveryOrderDetail.status = 1

left join SAL_OrderDetail On SAL_DeliveryOrderDetail.CompanyID = SAL_OrderDetail.CompanyID 
		And SAL_DeliveryOrderDetail.PeriodID = SAL_OrderDetail.PeriodID 
		And SAL_DeliveryOrderDetail.SalesOrderHeaderID = SAL_OrderDetail.OrderID 
		And SAL_DeliveryOrderDetail.SalesOrderDetailID = SAL_OrderDetail.OrderDetailID
		and SAL_OrderDetail.status = 1
inner join AR_SET_Customer on AR_SET_Customer.CustomerID = SAL_DispatchHeader.CustomerID
and AR_SET_Customer.Status = SAL_DispatchHeader.Status
inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID and SET_DocumentType.Status = 1
and SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID
and SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
where 
--AR_SET_Customer.CustomerID = @CustomerID 
SAL_DispatchHeader.DriverCNIC = @CNIC
--and AR_SET_Customer.IsRegistered = 0
and SAL_DispatchHeader.status = 1
and Cast(SAL_DispatchHeader.DispatchDate as Date) between
(select Cast(HRMS_SET_TaxYear.FromDate as Date) from HRMS_SET_TaxYear where @GateInDate between FromDate and ToDate) 
and (select Cast(HRMS_SET_TaxYear.ToDate as Date) from HRMS_SET_TaxYear where @GateInDate between FromDate and ToDate)
and Month(Cast(SAL_DispatchHeader.DispatchDate as Date)) = @Month  and Year(Cast(SAL_DispatchHeader.DispatchDate as Date)) = @Year
--and SET_DocumentType.DocumentTypeID = @DocumentTypeID
and (SET_DocumentType.DocumentType like '%dispatch%sugar%' OR SET_DocumentType.DocumentType like '%dispatch%billet%')
and SAL_DispatchHeader.CompanyID = @CompanyID
and SAL_DispatchHeader.PeriodID = @PeriodID
)final

                ";
                dt = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetYearlyMonthlyLimitsForGateInByCustomerParty(DateTime GateInDate, int DocumentTypeID, int CompanyID, int PeriodID, int @Month, int @Year, int CustomerPartyID)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@GateInDate", GateInDate);
                sqlparams[1] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[2] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[3] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[4] = new SqlParameter("@Month", @Month);
                sqlparams[5] = new SqlParameter("@Year", @Year);
                sqlparams[6] = new SqlParameter("@CustomerPartyID", CustomerPartyID);

                string query = @"
                
select Sum(final.Yearly) as Yearly, Sum(final.Monthly) as Monthly from (
-----Yearly
select isnull(SUM(ABS(isnull(TransactionQuantity,0) * isnull(SAL_OrderDetail.Rate,0))),0)  as Yearly , 0 as Monthly 
from SAL_DispatchHeader 
inner join INV_StockTransactionHeader on INV_StockTransactionHeader.DocumentTypeID = SAL_DispatchHeader.DocumentTypeID
and INV_StockTransactionHeader.DocumentNo = SAL_DispatchHeader.DispatchNo
and INV_StockTransactionHeader.CompanyID = SAL_DispatchHeader.CompanyID
and INV_StockTransactionHeader.PeriodID = SAL_DispatchHeader.PeriodID
and INV_StockTransactionHeader.Status = SAL_DispatchHeader.Status
and INV_StockTransactionHeader.FiscalID = SAL_DispatchHeader.FiscalID
left join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID 
and INV_StockTransactionDetail.Status = 1
and INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID
and INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
LEft JOIN Sal_DeliveryOrderDetail on Sal_DeliveryOrderDetail.DOHeaderID = [INV_StockTransactionDetail].SourceHeaderID
		And Sal_DeliveryOrderDetail.DODetailID = [INV_StockTransactionDetail].SourceLineID
		AND Sal_DeliveryOrderDetail.CompanyID = [INV_StockTransactionDetail].CompanyID 
		AND Sal_DeliveryOrderDetail.PeriodID = [INV_StockTransactionDetail].PeriodID
		and Sal_DeliveryOrderDetail.status = 1

left join SAL_OrderDetail On SAL_DeliveryOrderDetail.CompanyID = SAL_OrderDetail.CompanyID 
		And SAL_DeliveryOrderDetail.PeriodID = SAL_OrderDetail.PeriodID 
		And SAL_DeliveryOrderDetail.SalesOrderHeaderID = SAL_OrderDetail.OrderID 
		And SAL_DeliveryOrderDetail.SalesOrderDetailID = SAL_OrderDetail.OrderDetailID
		and SAL_OrderDetail.status = 1
inner join AR_SET_Customer on AR_SET_Customer.CustomerID = SAL_DispatchHeader.CustomerID
and AR_SET_Customer.Status = SAL_DispatchHeader.Status
inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID and SET_DocumentType.Status = 1
and SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID
and SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
where 
--AR_SET_Customer.CustomerID = @CustomerID 
--SAL_DispatchHeader.DriverCNIC = @CNIC
SAL_DispatchHeader.CustomerPartyID = @CustomerPartyID

--and AR_SET_Customer.IsRegistered = 0
and SAL_DispatchHeader.status = 1
and Cast(SAL_DispatchHeader.DispatchDate as Date) between
(select Cast(HRMS_SET_TaxYear.FromDate as Date) from HRMS_SET_TaxYear where @GateInDate between FromDate and ToDate ) and (select Cast(HRMS_SET_TaxYear.ToDate as Date) from HRMS_SET_TaxYear where @GateInDate between FromDate and ToDate)
--and SET_DocumentType.DocumentType = @DocumentTypeID
and (SET_DocumentType.DocumentType like '%dispatch%sugar%' OR SET_DocumentType.DocumentType like '%dispatch%billet%')
and SAL_DispatchHeader.CompanyID = @CompanyID
and SAL_DispatchHeader.PeriodID = @PeriodID

union All
 
-----Monthly
select 0  as Yearly ,isnull(SUM(ABS(isnull(TransactionQuantity,0) * isnull(SAL_OrderDetail.Rate,0))),0)  as Monthly
from SAL_DispatchHeader 
inner join INV_StockTransactionHeader on INV_StockTransactionHeader.DocumentTypeID = SAL_DispatchHeader.DocumentTypeID
and INV_StockTransactionHeader.DocumentNo = SAL_DispatchHeader.DispatchNo
and INV_StockTransactionHeader.CompanyID = SAL_DispatchHeader.CompanyID
and INV_StockTransactionHeader.PeriodID = SAL_DispatchHeader.PeriodID
and INV_StockTransactionHeader.Status = SAL_DispatchHeader.Status
and INV_StockTransactionHeader.FiscalID = SAL_DispatchHeader.FiscalID
left join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID 
and INV_StockTransactionDetail.Status = 1
and INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID
and INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
LEft JOIN Sal_DeliveryOrderDetail on Sal_DeliveryOrderDetail.DOHeaderID = [INV_StockTransactionDetail].SourceHeaderID
		And Sal_DeliveryOrderDetail.DODetailID = [INV_StockTransactionDetail].SourceLineID
		AND Sal_DeliveryOrderDetail.CompanyID = [INV_StockTransactionDetail].CompanyID 
		AND Sal_DeliveryOrderDetail.PeriodID = [INV_StockTransactionDetail].PeriodID
		and Sal_DeliveryOrderDetail.status = 1

left join SAL_OrderDetail On SAL_DeliveryOrderDetail.CompanyID = SAL_OrderDetail.CompanyID 
		And SAL_DeliveryOrderDetail.PeriodID = SAL_OrderDetail.PeriodID 
		And SAL_DeliveryOrderDetail.SalesOrderHeaderID = SAL_OrderDetail.OrderID 
		And SAL_DeliveryOrderDetail.SalesOrderDetailID = SAL_OrderDetail.OrderDetailID
		and SAL_OrderDetail.status = 1
inner join AR_SET_Customer on AR_SET_Customer.CustomerID = SAL_DispatchHeader.CustomerID
and AR_SET_Customer.Status = SAL_DispatchHeader.Status
inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID and SET_DocumentType.Status = 1
and SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID
and SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
where 
--AR_SET_Customer.CustomerID = @CustomerID 
--SAL_DispatchHeader.DriverCNIC = @CNIC
SAL_DispatchHeader.CustomerPartyID = @CustomerPartyID
--and AR_SET_Customer.IsRegistered = 0
and SAL_DispatchHeader.status = 1
and Cast(SAL_DispatchHeader.DispatchDate as Date) between
(select Cast(HRMS_SET_TaxYear.FromDate as Date) from HRMS_SET_TaxYear where @GateInDate between FromDate and ToDate) 
and (select Cast(HRMS_SET_TaxYear.ToDate as Date) from HRMS_SET_TaxYear where @GateInDate between FromDate and ToDate)
and Month(Cast(SAL_DispatchHeader.DispatchDate as Date)) = @Month  and Year(Cast(SAL_DispatchHeader.DispatchDate as Date)) = @Year
--and SET_DocumentType.DocumentTypeID = @DocumentTypeID
and (SET_DocumentType.DocumentType like '%dispatch%sugar%' OR SET_DocumentType.DocumentType like '%dispatch%billet%')
and SAL_DispatchHeader.CompanyID = @CompanyID
and SAL_DispatchHeader.PeriodID = @PeriodID
)final

                ";
                dt = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable ReconcillationReportJK(int companyID, int periodID, DateTime fromDate,
            DateTime toDate)
        {
            DataTable dtToReturn = null;
            StringBuilder additionalfilter = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);

                #region Query

                string Query_GetCustomerStatusSummary = @"
 ";

                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text,FnI.QueryResourceLibrary.GL.GL_CodeCombinationDetail.Query_GetCustomerStatusSummary, sqlparams);

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


        public DataSet GeBusinessPartnerTypesReportData(int companyID, int periodID, Nullable<int> CustomerID, string Query_Partner_Type_Report, string customerName, Nullable<int> customerType = null, 
            Nullable<int> customerStatus = null, string dateFrom = null, string dateTo = null)
        {
            DataSet ds1 = new DataSet();
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                //sqlparams[2] = new SqlParameter("@CustomerCategoryWiseTaxID", CustomerCategoryWiseTaxID);
                sqlparams[2] = new SqlParameter("@CustomerID", CustomerID);
                sqlparams[3] = new SqlParameter("@CustomerName", customerName);

                if (customerType.HasValue)
                    sqlparams[4] = new SqlParameter("@CustomerType", customerType.Value);
                else
                    sqlparams[4] = new SqlParameter("@CustomerType", DBNull.Value);

                if (customerStatus.HasValue)
                    sqlparams[5] = new SqlParameter("@CustomerStatus", customerStatus.Value);
                else
                    sqlparams[5] = new SqlParameter("@CustomerStatus", DBNull.Value);
                
                if (dateFrom != null && dateTo != null)
                    sb.Append(" and AR_SET_Customer.CreateDate between '" + dateFrom + " 00:00:00'" + " and '" + dateTo + " 23:59:59' ");

                //if (BrokerID.HasValue && BrokerID > 0)
                //    sb.Append(" AND tbl_Broker.BrokerID = " + BrokerID);
                #region Query

              

                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_Partner_Type_Report, sb.ToString()), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                {
                    ds1 = ds;
                    ds.Tables[0].TableName = "Table";
                    ds.Tables[1].TableName = "Table1";
                }
            }
            catch (Exception exp)
            {
                SqlHelper.LogException(exp);
                throw;
            }

            return ds1;
        }


        public static DataTable GetYearlyMonthlyLimitsForDo(DateTime DoDate, int DocumentTypeID, int CompanyID, int PeriodID, int Month, int Year, int CustomerID, int DoHeaderID)
        {
            DataTable dt = null;
            string Fillter = "";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@DoDate", DoDate);
                sqlparams[1] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[2] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[3] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[4] = new SqlParameter("@Month", Month);
                sqlparams[5] = new SqlParameter("@Year", Year);
                sqlparams[6] = new SqlParameter("CustomerID", CustomerID);

                string query = @"
    select Sum(final.Yearly) as Yearly, Sum(final.Monthly) as Monthly from (
 
-----Monthly
select 0  as Yearly ,isnull(SUM(ABS(isnull(Sal_DeliveryOrderDetail.Quantity ,0) * isnull(SAL_OrderDetail.Rate,0))),0)  as Monthly
from Sal_DeliveryOrderDetail
inner join SAL_DeliveryOrderHeader on SAL_DeliveryOrderHeader.DeliveryOrderID = Sal_DeliveryOrderDetail.DOHeaderID
and SAL_DeliveryOrderHeader.CompanyID = Sal_DeliveryOrderDetail.CompanyID
and SAL_DeliveryOrderHeader.PeriodID = Sal_DeliveryOrderDetail.PeriodID
and SAL_DeliveryOrderHeader.Status = 1 and SAL_DeliveryOrderHeader.StatusCode = 1

left join SAL_OrderDetail On SAL_DeliveryOrderDetail.CompanyID = SAL_OrderDetail.CompanyID 
		And SAL_DeliveryOrderDetail.PeriodID = SAL_OrderDetail.PeriodID 
		And SAL_DeliveryOrderDetail.SalesOrderHeaderID = SAL_OrderDetail.OrderID 
		And SAL_DeliveryOrderDetail.SalesOrderDetailID = SAL_OrderDetail.OrderDetailID
		and SAL_OrderDetail.status = 1
inner join AR_SET_Customer on AR_SET_Customer.CustomerID = SAL_DeliveryOrderHeader.CustomerID

inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID and SET_DocumentType.Status = 1
and SET_DocumentType.CompanyID = SAL_DeliveryOrderHeader.CompanyID
and SET_DocumentType.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
where 
AR_SET_Customer.CustomerID = @CustomerID  and Sal_DeliveryOrderDetail.status = 1
and Cast(SAL_DeliveryOrderHeader.DODate as Date) between
(select Cast(HRMS_SET_TaxYear.FromDate as Date) from HRMS_SET_TaxYear where @DoDate between FromDate and ToDate) 
and (select Cast(HRMS_SET_TaxYear.ToDate as Date) from HRMS_SET_TaxYear where @DoDate between FromDate and ToDate)
and Month(Cast(SAL_DeliveryOrderHeader.DODate as Date)) = @Month  and Year(Cast(SAL_DeliveryOrderHeader.DODate as Date)) = @Year
and SET_DocumentType.DocumentType like '%deformed%' 
and SAL_DeliveryOrderHeader.CompanyID = @CompanyID
and SAL_DeliveryOrderHeader.PeriodID = @PeriodID
{0}
)final ";


                if (DoHeaderID > 0)
                    Fillter = " and SAL_DeliveryOrderHeader.DeliveryOrderID <> " + DoHeaderID + "";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, Fillter), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetPendingDoAmountBrokerWise(DateTime DoDate, int DocumentTypeID, int CompanyID, int PeriodID, int BrokerID)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@DoDate", DoDate);
                sqlparams[1] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[2] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[3] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[4] = new SqlParameter("BrokerID", BrokerID);

                string query = @"
    select Sum(final.PendingAmount) as PendingAmount from (
 
-----Monthly
select  isnull((ABS( (isnull(Sal_DeliveryOrderDetail.Quantity ,0) - abs(isnull(INV_StockTransactionDetail.TransactionQuantity,0)) ) * isnull(SAL_OrderDetail.Rate,0))),0)  as PendingAmount
from Sal_DeliveryOrderDetail
inner join SAL_DeliveryOrderHeader on SAL_DeliveryOrderHeader.DeliveryOrderID = Sal_DeliveryOrderDetail.DOHeaderID
and SAL_DeliveryOrderHeader.CompanyID = Sal_DeliveryOrderDetail.CompanyID
and SAL_DeliveryOrderHeader.PeriodID = Sal_DeliveryOrderDetail.PeriodID
and SAL_DeliveryOrderHeader.Status = 1 and SAL_DeliveryOrderHeader.StatusCode = 1

left join SAL_OrderDetail On SAL_DeliveryOrderDetail.CompanyID = SAL_OrderDetail.CompanyID 
		And SAL_DeliveryOrderDetail.PeriodID = SAL_OrderDetail.PeriodID 
		And SAL_DeliveryOrderDetail.SalesOrderHeaderID = SAL_OrderDetail.OrderID 
		And SAL_DeliveryOrderDetail.SalesOrderDetailID = SAL_OrderDetail.OrderDetailID
		and SAL_OrderDetail.status = 1
inner join AR_SET_Customer on AR_SET_Customer.CustomerID = SAL_DeliveryOrderHeader.CustomerID

inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID and SET_DocumentType.Status = 1
and SET_DocumentType.CompanyID = SAL_DeliveryOrderHeader.CompanyID
and SET_DocumentType.PeriodID = SAL_DeliveryOrderHeader.PeriodID 

Left JOIN INV_StockTransactionDetail on Sal_DeliveryOrderDetail.DOHeaderID = INV_StockTransactionDetail.SourceHeaderID
        And Sal_DeliveryOrderDetail.DODetailID = INV_StockTransactionDetail.SourceLineID
		AND Sal_DeliveryOrderDetail.CompanyID = INV_StockTransactionDetail.CompanyID
        AND Sal_DeliveryOrderDetail.PeriodID = INV_StockTransactionDetail.PeriodID
		AND SAL_DeliveryOrderHeader.DocumentTypeID = INV_StockTransactionDetail.SourceDocumentTypeID
		and INV_StockTransactionDetail.status = 1

where 
AR_SET_Customer.BrokerID = @BrokerID and Sal_DeliveryOrderDetail.status = 1
and SET_DocumentType.DocumentType like '%deformed%' 
and SAL_DeliveryOrderHeader.CompanyID = @CompanyID
and SAL_DeliveryOrderHeader.PeriodID = @PeriodID
)final ";
                dt = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataRow GetBusinessPartnerTypeIDbyCode(int BusinessPartnerID, string AccountCode, SqlTransaction sqltrans = null)
        {
            DataRow dr = null;
            try
            {
                DataSet ds;
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@BusinessPartnerID", BusinessPartnerID);
                sqlparams[1] = new SqlParameter("@AccountCode", AccountCode);


                string Query_GetBusinessPartnerTypeIDbyCode = @"
select * from AR_SET_BusinessPartnerTypes where BusinessPartnerID = @BusinessPartnerID and FK_CodeCombinationID = @AccountCode
";

                //DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetBusinessPartnerTypeIDbyCode, sqlparams);

                //if (ds != null && ds.Tables.Count > 0)
                //    dtToReturn = ds.Tables[0];


                if (sqltrans == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetBusinessPartnerTypeIDbyCode, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, Query_GetBusinessPartnerTypeIDbyCode, sqlparams);


                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    dr = ds.Tables[0].Rows[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }

    }
}
