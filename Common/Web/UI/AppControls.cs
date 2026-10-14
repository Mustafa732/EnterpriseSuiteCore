using System.Reflection;

namespace Common.Web.UI
{
    public class AppControls
    {
        /// <summary>
        /// .NET 8 version - Form controls ko ReadOnly/Disabled karta hai
        /// WebForms ki jagah ab reflection se kaam karta hai - kisi bhi object pe chalega
        /// </summary>
        public static void ReadOnlyFormControls(object inputControl, bool isReadOnly = true)
        {
            if (inputControl == null) return;

            try
            {
                // Agar iske andar Controls collection hai (Panel, Form, Div jaise)
                var controlsProp = inputControl.GetType().GetProperty("Controls");
                if (controlsProp != null)
                {
                    var controls = controlsProp.GetValue(inputControl) as System.Collections.IEnumerable;
                    if (controls != null)
                    {
                        foreach (var ctl in controls)
                        {
                            SetControlReadOnly(ctl, isReadOnly);
                            // Recursion
                            if (HasControls(ctl))
                                ReadOnlyFormControls(ctl, isReadOnly);
                        }
                        return;
                    }
                }

                // Direct single control
                SetControlReadOnly(inputControl, isReadOnly);
            }
            catch { }
        }

        public static void ResetFormControls(object inputControl)
        {
            if (inputControl == null) return;

            try
            {
                var controlsProp = inputControl.GetType().GetProperty("Controls");
                if (controlsProp != null)
                {
                    var controls = controlsProp.GetValue(inputControl) as System.Collections.IEnumerable;
                    if (controls != null)
                    {
                        foreach (var ctl in controls)
                        {
                            ResetSingleControl(ctl);
                            if (HasControls(ctl))
                                ResetFormControls(ctl);
                        }
                        return;
                    }
                }

                ResetSingleControl(inputControl);
            }
            catch { }
        }

        // Helper - ReadOnly/Enabled set karna
        private static void SetControlReadOnly(object ctl, bool isReadOnly)
        {
            if (ctl == null) return;
            var t = ctl.GetType();

            // Enabled property wale (DropDown, Grid, RadCombo, etc)
            var enabledProp = t.GetProperty("Enabled");
            if (enabledProp != null && enabledProp.CanWrite)
            {
                // CheckBox ko hamesha enabled rehne do - old logic same
                if (t.Name != "CheckBox")
                    enabledProp.SetValue(ctl, !isReadOnly);
                else
                    enabledProp.SetValue(ctl, true);
            }

            // ReadOnly property wale (TextBox)
            var readOnlyProp = t.GetProperty("ReadOnly");
            if (readOnlyProp != null && readOnlyProp.CanWrite)
            {
                readOnlyProp.SetValue(ctl, isReadOnly);
            }
        }

        private static void ResetSingleControl(object ctl)
        {
            if (ctl == null) return;
            var t = ctl.GetType();

            try
            {
                switch (t.Name)
                {
                    case "CheckBox":
                        t.GetProperty("Checked")?.SetValue(ctl, false);
                        break;
                    case "DropDownList":
                    case "RadioButtonList":
                        t.GetProperty("SelectedIndex")?.SetValue(ctl, 0);
                        break;
                    case "TextBox":
                    case "RadMaskedTextBox":
                    case "RadNumericTextBox":
                        t.GetProperty("Text")?.SetValue(ctl, string.Empty);
                        break;
                    case "RadComboBox":
                        t.GetProperty("Text")?.SetValue(ctl, string.Empty);
                        t.GetMethod("ClearSelection")?.Invoke(ctl, null);
                        var itemsProp = t.GetProperty("Items");
                        var items = itemsProp?.GetValue(ctl);
                        items?.GetType().GetMethod("Clear")?.Invoke(items, null);
                        break;
                    case "RadDatePicker":
                        t.GetProperty("SelectedDate")?.SetValue(ctl, null);
                        break;
                    case "RadButton":
                        t.GetMethod("ClearSelection")?.Invoke(ctl, null);
                        t.GetProperty("Checked")?.SetValue(ctl, false);
                        break;
                }
            }
            catch { }
        }

        private static bool HasControls(object ctl)
        {
            return ctl?.GetType().GetProperty("Controls") != null;
        }
    }
}